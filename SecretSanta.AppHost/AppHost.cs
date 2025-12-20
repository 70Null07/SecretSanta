using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.SecretSanta_ApiService>("apiservice")
    .WithHttpHealthCheck("/health").WithEnvironment("ConnectionStrings__Default",
        "Data Source=dmitr;Initial Catalog=SecretSanta;Integrated Security=True;Encrypt=False");

builder.AddProject<Projects.SecretSanta_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
