using PeepApi;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

using IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddAzureClients(azure => azure.AddBlobServiceClient(Environment.GetEnvironmentVariable("ConnectionString")));
    })
    .Build();

await host.RunAsync();