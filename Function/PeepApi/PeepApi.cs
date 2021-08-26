using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace PeepApi
{
    public class PeepApi
    {
        private readonly BlobContainerClient _blobContainerClient;

        public PeepApi(BlobServiceClient blobServiceClient)
        {
            _blobContainerClient = blobServiceClient.GetBlobContainerClient("scripts");
        }

        [Function(nameof(GetRandomQuote))]
        public async Task<HttpResponseData> GetRandomQuote([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {

            var quotes = new List<string>();
            foreach (BlobItem blob in _blobContainerClient.GetBlobs())
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blob.Name);

                var content = await blobClient.DownloadAsync();
                var text = content.Value.Content;

                using (var streamReader = new StreamReader(text))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        if (!line.StartsWith("["))
                            quotes.Add(line);
                    }
                }
            }

            var rnd = new Random();
            int indexValue = rnd.Next(0, quotes.Count - 1);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(quotes[indexValue]);

            return response;
        }

        [Function(nameof(GetEpisode))]
        public async Task<HttpResponseData> GetEpisode([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var quotes = new List<string>();

            var episode = executionContext.BindingContext.BindingData["episode"].ToString();

            if (episode.Contains("0"))
                episode = episode.Replace("0", "");

            BlobClient blobClient = _blobContainerClient.GetBlobClient(episode + ".txt");

            var content = await blobClient.DownloadAsync();
            var text = content.Value.Content;

            using (var streamReader = new StreamReader(text))
            {
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    quotes.Add(line);
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            foreach (var quote in quotes)
            {
                response.WriteString(quote);
                response.WriteString(Environment.NewLine);
            }


            return response;
        }
    }
}
