
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithRedisCommander();

var api = builder.AddProject<Projects.AspireDemo_Api>("aspiredemo-api")
    .WithReference(cache);

builder.AddProject<Projects.AspireDemo_Web>("aspiredemo-web")
    .WithExternalHttpEndpoints()
    .WithReference(api);

builder.Build().Run();
