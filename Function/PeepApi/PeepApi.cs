using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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

            var quotes = new List<(string, string)>();
            foreach (BlobItem blob in _blobContainerClient.GetBlobs())
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blob.Name);

                var content = await blobClient.DownloadAsync();
                var text = content.Value.Content;
                string episodeName = null;

                using (var streamReader = new StreamReader(text))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();

                        if (string.IsNullOrEmpty(episodeName))
                            episodeName = line;
                        else
                        {
                            if (!line.StartsWith("["))
                                quotes.Add((line, blob.Name.Replace(".txt", "") + $" - {episodeName}"));
                        }
                    }
                }
            }

            var rnd = new Random();
            int indexValue = rnd.Next(0, quotes.Count - 1);

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new { quote = quotes[indexValue].Item1, episode = quotes[indexValue].Item2 });

            return response;
        }

        [Function(nameof(QuoteSearch))]
        public async Task<HttpResponseData> QuoteSearch([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var search = executionContext.BindingContext.BindingData["search"].ToString();

            var searchCleaned = new string(search.Where(c => !char.IsPunctuation(c)).ToArray());

            var quotes = new List<(string, string)>();
            var matches = new List<(string, string)>();
            foreach (BlobItem blob in _blobContainerClient.GetBlobs())
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blob.Name);

                var content = await blobClient.DownloadAsync();
                var text = content.Value.Content;
                string episodeName = null;

                using (var streamReader = new StreamReader(text))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();

                        if (string.IsNullOrEmpty(episodeName))
                            episodeName = line;
                        else
                        {
                            if (!line.StartsWith("["))
                                quotes.Add((line, blob.Name.Replace(".txt", "") + $" - {episodeName}"));
                        }
                    }
                }
            }

            foreach (var quote in quotes)
            {
                var quoteCleaned = new string(quote.Item1.Where(c => !char.IsPunctuation(c)).ToArray());

                if (Regex.IsMatch(quoteCleaned.ToLower(), @$"\b{searchCleaned.ToLower()}\b"))
                {
                    matches.Add(quote);
                }
            }


            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(matches.Select(a => new { quote = a.Item1, episode = a.Item2 }));


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
                quotes = quotes.Skip(1).ToList();
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
