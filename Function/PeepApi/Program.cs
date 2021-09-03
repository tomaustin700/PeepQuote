using PeepApi;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using System;

using IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureOpenApi()
    .ConfigureServices((context, services) =>
    {
       services.AddAzureClients(azure => azure.AddBlobServiceClient(Environment.GetEnvironmentVariable("ConnectionString")));
    })
    .Build();

await host.RunAsync();