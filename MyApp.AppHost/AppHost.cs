var builder = DistributedApplication.CreateBuilder(args);

// Ajouter Keycloak
var keycloak = builder.AddKeycloak("keycloak", 8090)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExternalHttpEndpoints();

// Ajouter SQL Server avec volume persistant
var sqlserver = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Créer la base de données
var database = sqlserver.AddDatabase("myapp");

// Ajouter l'API avec référence à la base de données et Keycloak
var apiService = builder.AddProject<Projects.MyApp_ApiService>("apiservice")
    .WithReference(database)
    .WithReference(keycloak)
    .WaitFor(database)
    .WaitFor(keycloak);

// Ajouter l'application Blazor avec référence à l'API et Keycloak
builder.AddProject<Projects.MyApp_WebApp>("webapp")
    .WithReference(apiService)
    .WithReference(keycloak)
    .WaitFor(apiService)
    .WaitFor(keycloak);

builder.Build().Run();
