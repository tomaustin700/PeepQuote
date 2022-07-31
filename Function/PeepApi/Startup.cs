
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
            builder.Services.AddAzureClients(azure => azure.AddBlobServiceClient("DefaultEndpointsProtocol=https;AccountName=peepqscriptstorage;AccountKey=hyjKwOlgq8VoeTcB74ZI0CHignCOPpUvdWbiZGaSapJlI4x2agy0K/cbzhTSRrI8mS5RJ2uvjquwxIgi6rQAGQ==;EndpointSuffix=core.windows.net"));
        }
    }
}
