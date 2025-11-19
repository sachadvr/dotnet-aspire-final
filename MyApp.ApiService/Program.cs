using MyApp.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Configure SQL Server DbContext
builder.AddSqlServerDbContext<MyAppContext>(connectionName: "myapp");

// Configure Authentication JWT
// Si on a une connection string Keycloak (via Aspire), elle contient déjà /realms/myapp
// Sinon, on prend la configuration OIDC Authority, ou on construit l'URL
var keycloakConnectionString = builder.Configuration.GetConnectionString("keycloak");
var keycloakAuthority = !string.IsNullOrEmpty(keycloakConnectionString)
    ? keycloakConnectionString  // Aspire fournit déjà l'URL complète
    : builder.Configuration["Authentication:OIDC:Authority"] ?? "http://localhost:8090/realms/myapp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = keycloakAuthority;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudiences = new[] { "api", "account" },
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            RoleClaimType = "roles",
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ AUTH FAILED ❌");
                Console.WriteLine($"Error: {context.Exception.Message}");
                Console.WriteLine($"Exception Type: {context.Exception.GetType().Name}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {context.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Extraire les rôles depuis realm_access.roles
                if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                {
                    // Chercher le claim realm_access
                    var realmAccessClaim = identity.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        try
                        {
                            // Parser le JSON realm_access
                            var realmAccess = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(realmAccessClaim.Value);
                            if (realmAccess.TryGetProperty("roles", out var rolesArray))
                            {
                                // Ajouter chaque rôle comme un claim individuel de type "roles"
                                foreach (var role in rolesArray.EnumerateArray())
                                {
                                    var roleValue = role.GetString();
                                    if (!string.IsNullOrEmpty(roleValue))
                                    {
                                        identity.AddClaim(new System.Security.Claims.Claim("roles", roleValue));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Erreur lors de l'extraction des rôles: {ex.Message}");
                        }
                    }
                }
                foreach (var claim in context.Principal?.Claims ?? [])
                {
                    Console.WriteLine($"  - {claim.Type}: {claim.Value}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Dashboard API",
        Version = "v1",
        Description = "API pour l'application Dashboard"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Entrez 'Bearer' [espace] puis votre token JWT.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyApp API v1");
        c.RoutePrefix = "swagger";
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Appliquer les migrations EF Core au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyAppContext>();
    try
    {
        Console.WriteLine("=== Application des migrations de base de données ===");

        // Lister toutes les migrations disponibles
        var allMigrations = db.Database.GetMigrations().ToList();
        Console.WriteLine($"Migrations disponibles dans le code: {string.Join(", ", allMigrations)}");

        // Lister les migrations appliquées
        var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
        Console.WriteLine($"Migrations déjà appliquées: {string.Join(", ", appliedMigrations)}");

        // Lister les migrations en attente
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        Console.WriteLine($"Migrations en attente: {string.Join(", ", pendingMigrations)}");

        if (pendingMigrations.Any())
        {
            Console.WriteLine($"Application des migrations en attente: {string.Join(", ", pendingMigrations)}");
            db.Database.Migrate();
            Console.WriteLine("✅ Migrations appliquées avec succès.");
        }
        else
        {
            Console.WriteLine("✅ Toutes les migrations sont déjà appliquées.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur lors de l'application des migrations: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw; // Relancer l'exception pour arrêter l'application si les migrations échouent
    }
}

// PRODUITS
app.MapGet("/api/products", async (MyAppContext db) =>
    await db.Products.Include(p => p.Category).ToListAsync());

app.MapGet("/api/products/{id}", async (int id, MyAppContext db) =>
{
    var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

// COMMANDES
app.MapGet("/api/orders", async (System.Security.Claims.ClaimsPrincipal user, MyAppContext db) =>
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Utilisateur non identifié");

    var orders = await db.Orders
        .Where(o => o.UserId == userId)
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .ThenInclude(p => p.Category)
        .OrderByDescending(o => o.Id)
        .ToListAsync();

    return Results.Ok(orders);
})
.RequireAuthorization();

app.MapGet("/api/orders/{id}", async (int id, System.Security.Claims.ClaimsPrincipal user, MyAppContext db) =>
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Utilisateur non identifié");

    var order = await db.Orders
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .ThenInclude(p => p.Category)
        .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

    return order is null ? Results.NotFound() : Results.Ok(order);
})
.RequireAuthorization();

app.MapPost("/api/orders", async (CreateOrderRequest request, System.Security.Claims.ClaimsPrincipal user, MyAppContext db) =>
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Utilisateur non identifié");

    // Récupérer le nom complet depuis user.Identity.Name (configuré avec NameClaimType = "name")
    var userName = user.Identity?.Name
        ?? user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? user.FindFirst("name")?.Value
        ?? user.FindFirst("preferred_username")?.Value
        ?? "Utilisateur inconnu";

    // Vérifier que tous les produits existent et sont en stock
    var productIds = request.Items.Select(i => i.ProductId).ToList();
    var products = await db.Products
        .Include(p => p.Category)
        .Where(p => productIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);

    if (products.Count != productIds.Count)
    {
        return Results.BadRequest("Un ou plusieurs produits n'existent pas");
    }

    // Vérifier le stock et calculer le total
    decimal totalAmount = 0;
    var orderItems = new List<OrderItem>();

    foreach (var item in request.Items)
    {
        var product = products[item.ProductId];
        if (product.Stock < item.Quantity)
        {
            return Results.BadRequest($"Stock insuffisant pour le produit {product.Name}");
        }

        var unitPrice = product.Price;
        totalAmount += unitPrice * item.Quantity;

        orderItems.Add(new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = unitPrice
        });

        // Réduire le stock
        product.Stock -= item.Quantity;
    }

    var order = new Order
    {
        UserId = userId,
        UserName = userName,
        OrderDate = DateTime.UtcNow,
        Status = OrderStatus.Pending,
        TotalAmount = totalAmount,
        Address = request.Address ?? string.Empty,
        OrderItems = orderItems
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    await db.Entry(order).Collection(o => o.OrderItems).LoadAsync();
    foreach (var orderItem in order.OrderItems)
    {
        await db.Entry(orderItem).Reference(oi => oi.Product).LoadAsync();
        if (orderItem.Product != null)
        {
            await db.Entry(orderItem.Product).Reference(p => p.Category).LoadAsync();
        }
    }

    return Results.Created($"/api/orders/{order.Id}", order);
})
.RequireAuthorization();

// Categories
app.MapGet("/api/categories", async (MyAppContext db) =>
    await db.Categories.OrderBy(c => c.Name).ToListAsync());

app.MapGet("/api/categories/{id}", async (int id, MyAppContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    return category is null ? Results.NotFound() : Results.Ok(category);
});

app.MapGet("/api/admin/categories", async (MyAppContext db) =>
    await db.Categories
        .Include(c => c.Products)
        .OrderBy(c => c.Name)
        .ToListAsync())
    .RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapPost("/api/admin/categories", async (Category category, MyAppContext db) =>
{
    category.CreatedAt = DateTime.UtcNow;
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/categories/{category.Id}", category);
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapPut("/api/admin/categories/{id}", async (int id, Category updatedCategory, MyAppContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null)
    {
        return Results.NotFound();
    }

    category.Name = updatedCategory.Name;
    category.Description = updatedCategory.Description;

    await db.SaveChangesAsync();
    return Results.Ok(category);
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapDelete("/api/admin/categories/{id}", async (int id, MyAppContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null)
    {
        return Results.NotFound();
    }

    // Vérifier s'il y a des produits associés
    var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id);
    if (hasProducts)
    {
        return Results.BadRequest("Impossible de supprimer une catégorie qui a des produits associés");
    }

    db.Categories.Remove(category);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapGet("/api/admin/products", async (MyAppContext db) =>
    await db.Products.Include(p => p.Category).ToListAsync())
    .RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapPost("/api/admin/products", async (Product product, MyAppContext db) =>
{
    product.CreatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync();

    // Recharger avec la catégorie
    await db.Entry(product).Reference(p => p.Category).LoadAsync();
    return Results.Created($"/api/admin/products/{product.Id}", product);
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapPut("/api/admin/products/{id}", async (int id, Product updatedProduct, MyAppContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    product.Name = updatedProduct.Name;
    product.Description = updatedProduct.Description;
    product.Price = updatedProduct.Price;
    product.Stock = updatedProduct.Stock;
    product.ImageUrl = updatedProduct.ImageUrl;
    product.CategoryId = updatedProduct.CategoryId;

    await db.SaveChangesAsync();

    // Recharger avec la catégorie
    await db.Entry(product).Reference(p => p.Category).LoadAsync();
    return Results.Ok(product);
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapDelete("/api/admin/products/{id}", async (int id, MyAppContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    // Vérifier s'il y a des commandes associées
    var hasOrders = await db.OrderItems.AnyAsync(oi => oi.ProductId == id);
    if (hasOrders)
    {
        return Results.BadRequest("Impossible de supprimer un produit qui a des commandes associées");
    }

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapGet("/api/admin/orders", async (MyAppContext db) =>
{
    var orders = await db.Orders
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .ThenInclude(p => p.Category)
        .OrderByDescending(o => o.Id)
        .ToListAsync();

    return Results.Ok(orders);
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapPut("/api/admin/orders/{id}/status", async (int id, UpdateOrderStatusRequest request, MyAppContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null)
    {
        return Results.NotFound();
    }

    if (Enum.TryParse<OrderStatus>(request.Status, true, out var status))
    {
        order.Status = status;
        await db.SaveChangesAsync();
        return Results.Ok(order);
    }

    return Results.BadRequest("Statut invalide");
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.MapPost("/api/admin/orders/{id}/payment-link", async (int id, GeneratePaymentLinkRequest request, MyAppContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null)
    {
        return Results.NotFound();
    }

    order.PaymentLink = request.PaymentLink;
    await db.SaveChangesAsync();

    return Results.Ok(order);
})
.RequireAuthorization(policy => policy.RequireRole("admin"));

app.Run();

// DTOs pour les requêtes
public record CreateOrderRequest(List<CreateOrderItemRequest> Items, string Address);
public record CreateOrderItemRequest(int ProductId, int Quantity);
public record UpdateOrderStatusRequest(string Status);
public record GeneratePaymentLinkRequest(string PaymentLink);
