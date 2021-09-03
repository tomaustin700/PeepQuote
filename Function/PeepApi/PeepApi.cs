using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PeepApi.Classes;

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



        [OpenApiOperation(operationId: "search", Summary = "Allows searching through all of the Peep Show dialog",  Visibility = OpenApiVisibilityType.Undefined)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SearchParameters), Required = true, Description = "Search parameters for querying Peep Show")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SearchResult), Summary = "Search results returned from the search")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "No body specified")]

        [Function(nameof(Search))]
        public async Task<HttpResponseData> Search([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get")] HttpRequestData req,
            FunctionContext executionContext)
        {

            SearchParameters searchParameters = null;
            try
            {
                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                searchParameters = await JsonSerializer.DeserializeAsync<SearchParameters>(req.Body, serializeOptions);
            }
            catch (JsonException)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.WriteString("No Body Specified");
                return badResponse;
            }

            string searchCleaned = null;
            if (!string.IsNullOrEmpty(searchParameters.SearchTerm))
                searchCleaned = new string(searchParameters.SearchTerm.Where(c => !char.IsPunctuation(c)).ToArray());

            int matchCount = 0;

            var quotes = new List<(string quote, string episode, string person)>();
            foreach (BlobItem blob in _blobContainerClient.GetBlobs())
            {

                if (searchParameters.SeriesNumber.HasValue)
                {
                    var blobNameCleaned = blob.Name.ToLower().Replace("0", "").Replace("s", "");
                    if (!blobNameCleaned.StartsWith(searchParameters.SeriesNumber.ToString()))
                        continue;
                }

                if (searchParameters.EpisodeNumber.HasValue)
                {
                    var name = blob.Name.Replace(".txt", "");
                    var blobEpisodeNumber = name.Substring(name.Length - 1);
                    if (searchParameters.EpisodeNumber.Value.ToString() != blobEpisodeNumber)
                        continue;
                }

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
                            {
                                var name = blob.Name.Replace(".txt", "");

                                if (!string.IsNullOrEmpty(searchParameters.Person) && !line.ToLower().StartsWith(searchParameters.Person.ToLower()))
                                    continue;
                                else
                                {
                                    var quote = line.Split(":")[1];
                                    var person = line.Split(":")[0];

                                    if (!string.IsNullOrEmpty(searchCleaned))
                                    {
                                        var quoteCleaned = new string(quote.Where(c => !char.IsPunctuation(c)).ToArray());
                                        var regexTerm = @$"\b{searchCleaned.ToLower()}\b";

                                        var mCount = Regex.Matches(quoteCleaned.ToLower(), regexTerm).Count;

                                        if (mCount > 0)
                                        {
                                            matchCount += mCount;
                                            quotes.Add((quote, name + $" - {episodeName}", person));

                                        }
                                    }
                                    else
                                    {
                                        quotes.Add((quote, name + $" - {episodeName}", person));
                                        matchCount += 1;

                                    }

                                }

                            }
                        }
                    }
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new SearchResult() { Count = matchCount, Results = quotes.Select(a => new QuoteData() { Quote = a.quote, Person = a.person, Episode = a.episode }) });


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
