
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using System;

[assembly: FunctionsStartup(typeof(PeepApi.Startup))]


namespace PeepApi
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAzureClients(azure => azure.AddBlobServiceClient(Environment.GetEnvironmentVariable("ConnectionString")));
        }
    }
}
