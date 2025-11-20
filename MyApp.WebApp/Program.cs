using MyApp.WebApp.Components;
using MyApp.WebApp.Clients;
using MyApp.WebApp.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = builder.Configuration["Authentication:OIDC:Authority"];
    options.ClientId = builder.Configuration["Authentication:OIDC:ClientId"];
    options.RequireHttpsMetadata = false;
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.UseTokenLifetime = true;
    options.MapInboundClaims = false;
    options.Scope.Add("api");
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        NameClaimType = "name",
        RoleClaimType = "roles",
    };
    options.PushedAuthorizationBehavior = Microsoft.AspNetCore.Authentication.OpenIdConnect.PushedAuthorizationBehavior.Disable;
    
    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            {
                string? accessToken = null;
                
                if (context.TokenEndpointResponse != null)
                {
                    accessToken = context.TokenEndpointResponse.AccessToken;
                }
                
                if (string.IsNullOrEmpty(accessToken) && context.Properties != null)
                {
                    accessToken = context.Properties.GetTokenValue("access_token");
                }
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var accessTokenJwt = handler.ReadJwtToken(accessToken);
                        var realmAccessClaim = accessTokenJwt.Claims.FirstOrDefault(c => c.Type == "realm_access");
                        
                        if (realmAccessClaim != null)
                        {
                            var realmAccess = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(realmAccessClaim.Value);
                            if (realmAccess.TryGetProperty("roles", out var rolesArray))
                            {
                                var rolesAdded = new List<string>();
                                foreach (var role in rolesArray.EnumerateArray())
                                {
                                    var roleValue = role.GetString();
                                    if (!string.IsNullOrEmpty(roleValue))
                                    {
                                        if (!identity.HasClaim("roles", roleValue))
                                        {
                                            identity.AddClaim(new System.Security.Claims.Claim("roles", roleValue));
                                            rolesAdded.Add(roleValue);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("⚠️ [OnTokenValidated] Claim realm_access non trouvé dans le token d'accès");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ [OnTokenValidated] Erreur lors du parsing du token d'accès: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ [OnTokenValidated] Token d'accès non disponible");
                }
                
                var roleClaims = identity.FindAll("roles").ToList();
                Console.WriteLine($"🔍 [OnTokenValidated] Claims 'roles' dans le principal après extraction: {string.Join(", ", roleClaims.Select(c => c.Value))}");
            }
            return Task.CompletedTask;
        },
        OnTicketReceived = context =>
        {
            if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            {
                var existingRoles = identity.FindAll("roles").ToList();
                
                if (!existingRoles.Any() && context.Properties != null)
                {
                    var accessToken = context.Properties.GetTokenValue("access_token");
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        try
                        {
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            var accessTokenJwt = handler.ReadJwtToken(accessToken);
                            var realmAccessClaim = accessTokenJwt.Claims.FirstOrDefault(c => c.Type == "realm_access");
                            
                            if (realmAccessClaim != null)
                            {
                                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(realmAccessClaim.Value);
                                var rolesAdded = new List<string>();
                                if (realmAccess.TryGetProperty("roles", out var rolesArray))
                                {
                                    foreach (var role in rolesArray.EnumerateArray())
                                    {
                                        var roleValue = role.GetString();
                                        if (!string.IsNullOrEmpty(roleValue) && !identity.HasClaim("roles", roleValue))
                                        {
                                            identity.AddClaim(new System.Security.Claims.Claim("roles", roleValue));
                                            rolesAdded.Add(roleValue);
                                        }
                                    }
                                }
                                if (rolesAdded.Any())
                                {
                                    Console.WriteLine($"✅ [Blazor] Rôles ajoutés dans OnTicketReceived: {string.Join(", ", rolesAdded)}");
                                    context.Principal = new System.Security.Claims.ClaimsPrincipal(identity);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ [Blazor] Erreur lors de l'extraction des rôles dans OnTicketReceived: {ex.Message}");
                        }
                    }
                }
                
                var roleClaims = identity.FindAll("roles").ToList();
                Console.WriteLine($"🎫 [Blazor] Ticket reçu - Claims 'roles' dans le ticket: {string.Join(", ", roleClaims.Select(c => c.Value))}");
            }
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = async context =>
        {
            var oidcOptions = context.Options;
            var configuration = await oidcOptions.ConfigurationManager!.GetConfigurationAsync(context.HttpContext.RequestAborted);
            var endSessionEndpoint = configuration?.EndSessionEndpoint;
            
            if (!string.IsNullOrEmpty(endSessionEndpoint))
            {
                var logoutUri = new UriBuilder(endSessionEndpoint);
                var queryParams = new System.Collections.Specialized.NameValueCollection();
                
                queryParams["client_id"] = oidcOptions.ClientId ?? "";
                if (context.Properties?.RedirectUri != null)
                {
                    queryParams["post_logout_redirect_uri"] = context.Properties.RedirectUri;
                }
                
                var queryString = string.Join("&", 
                    queryParams.AllKeys.SelectMany(key => 
                        queryParams.GetValues(key)?.Select(value => $"{Uri.EscapeDataString(key ?? "")}={Uri.EscapeDataString(value ?? "")}") ?? Array.Empty<string>()));
                
                if (!string.IsNullOrEmpty(queryString))
                {
                    logoutUri.Query = queryString;
                }
                
                context.ProtocolMessage.IssuerAddress = logoutUri.ToString();
                
                Console.WriteLine($"🔓 [Logout] Redirection vers Keycloak pour déconnexion complète: {logoutUri}");
            }
        },
        OnRedirectToIdentityProvider = context =>
        {
            if (context.ProtocolMessage.RequestType == Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectRequestType.Authentication)
            {
                context.ProtocolMessage.SetParameter("prompt", "login");
                Console.WriteLine($"🔐 [Login] Prompt=login ajouté pour forcer une nouvelle authentification");
            }
            
            return Task.CompletedTask;
        }
    };
});

builder.Services.ConfigureCookieOidc(CookieAuthenticationDefaults.AuthenticationScheme, "oidc");
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, TokenCircuitHandler>();
builder.Services.AddScoped<MyApp.WebApp.Auth.TokenHandler>();

var configureHttpClient = (HttpClient client) =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
};

var configureHandler = () =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
};

builder.Services.AddHttpClient<IProductClient, ProductClient>(configureHttpClient)
    .ConfigurePrimaryHttpMessageHandler(configureHandler);

builder.Services.AddHttpClient<ICategoryClient, CategoryClient>(configureHttpClient)
    .ConfigurePrimaryHttpMessageHandler(configureHandler);

builder.Services.AddHttpClient<IOrderClient, OrderClient>(configureHttpClient)
    .ConfigurePrimaryHttpMessageHandler(configureHandler)
    .AddHttpMessageHandler<MyApp.WebApp.Auth.TokenHandler>();

builder.Services.AddHttpClient<IAdminClient, AdminClient>(configureHttpClient)
    .ConfigurePrimaryHttpMessageHandler(configureHandler)
    .AddHttpMessageHandler<MyApp.WebApp.Auth.TokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/login", () => Results.Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties 
{ 
    RedirectUri = "/" 
}, authenticationSchemes: new[] { "oidc" }));

app.MapPost("/logout", async (Microsoft.AspNetCore.Http.HttpContext context) =>
{
    Console.WriteLine($"🔓 [Logout] Début de la déconnexion");
    
    var cookieName = CookieAuthenticationDefaults.AuthenticationScheme;
    var cookieAuthOptions = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<CookieAuthenticationOptions>>().Get(cookieName);
    var actualCookieName = cookieAuthOptions.Cookie.Name ?? cookieName;
    
    var cookieOptions = new CookieOptions
    {
        HttpOnly = cookieAuthOptions.Cookie.HttpOnly,
        Secure = cookieAuthOptions.Cookie.SecurePolicy == CookieSecurePolicy.Always || 
                 (cookieAuthOptions.Cookie.SecurePolicy == CookieSecurePolicy.SameAsRequest && context.Request.IsHttps),
        SameSite = cookieAuthOptions.Cookie.SameSite,
        Path = cookieAuthOptions.Cookie.Path ?? "/",
        Domain = cookieAuthOptions.Cookie.Domain,
        Expires = DateTimeOffset.UtcNow.AddYears(-1)
    };
    
    context.Response.Cookies.Delete(actualCookieName, cookieOptions);
    
    if (!actualCookieName.StartsWith(".AspNetCore"))
    {
        context.Response.Cookies.Delete($".AspNetCore.{actualCookieName}", cookieOptions);
    }
    
    foreach (var cookie in context.Request.Cookies.Keys)
    {
        if (cookie.Contains("auth", StringComparison.OrdinalIgnoreCase) || 
            cookie.Contains("oidc", StringComparison.OrdinalIgnoreCase) ||
            cookie.Contains("keycloak", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Cookies.Delete(cookie, cookieOptions);
        }
    }
    
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties 
    { 
        RedirectUri = "/" 
    };
    
    try
    {
        await context.SignOutAsync("oidc", properties);
        if (context.Response.HasStarted)
        {
            return Results.Empty;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ [Logout] Erreur lors de la déconnexion Keycloak: {ex.Message}");
    }
    
    var html = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>Déconnexion...</title>
        </head>
        <body>
            <script>
                window.location.replace('/');
            </script>
            <noscript>
                <meta http-equiv="refresh" content="0;url=/" />
            </noscript>
            <p>Déconnexion en cours...</p>
        </body>
        </html>
        """;
    
    return Results.Content(html, "text/html");
});

app.Run();
