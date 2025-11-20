var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 8090)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExternalHttpEndpoints();

var sqlserver = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlserver.AddDatabase("myapp");

var apiService = builder.AddProject<Projects.MyApp_ApiService>("apiservice")
    .WithReference(database)
    .WithReference(keycloak)
    .WaitFor(database)
    .WaitFor(keycloak);

builder.AddProject<Projects.MyApp_WebApp>("webapp")
    .WithReference(apiService)
    .WithReference(keycloak)
    .WaitFor(apiService)
    .WaitFor(keycloak);

builder.Build().Run();
