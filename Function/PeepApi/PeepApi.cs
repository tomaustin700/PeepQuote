using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Azure.Storage.Blobs;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using System.Net;
using PeepApi.Classes;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace PeepApi
{
    public class PeepApi
    {
        private readonly BlobContainerClient _blobContainerClient;

        public PeepApi(BlobServiceClient blobServiceClient)
        {
            _blobContainerClient = blobServiceClient.GetBlobContainerClient("scripts");
        }

        [OpenApiOperation(operationId: "search", Summary = "Allows searching through all of the Peep Show dialog", Visibility = OpenApiVisibilityType.Undefined)]
        [OpenApiParameter("searchTerm")]
        [OpenApiParameter("seriesNumber")]
        [OpenApiParameter("episodeNumber")]
        [OpenApiParameter("person")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SearchResult), Summary = "Search results returned from the search")]

        [FunctionName(nameof(SearchV2))]
        public async Task<IActionResult> SearchV2([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/Search")] HttpRequest req)
        {

            var searchTerm = req.Query["searchTerm"]; 
            var seriesNumber = req.Query["seriesNumber"]; 
            var episodeNumber = req.Query["episodeNumber"]; 
            var person = req.Query["person"];

            string searchCleaned = null;
            if (!string.IsNullOrEmpty(searchTerm))
                searchCleaned = new string(((string)searchTerm).Where(c => !char.IsPunctuation(c)).ToArray());

            int matchCount = 0;

            BlobClient blobClient = _blobContainerClient.GetBlobClient("content.json");

            var content = await blobClient.DownloadAsync();
            var json = content.Value.Content;

            var data = await JsonSerializer.DeserializeAsync<List<JsonData>>(json);
            var quotes = new List<(string quote, string episode, string person)>();


            if (!string.IsNullOrEmpty(seriesNumber))
            {
                data = data.Where(a => a.SeriesNumber == int.Parse(seriesNumber)).ToList();
            }

            if (!string.IsNullOrEmpty(episodeNumber))
            {
                data = data.Where(a => a.EpisodeNumber == int.Parse(episodeNumber)).ToList();
            }

            if (!string.IsNullOrEmpty(person))
            {
                data = data.Where(a => a.Person.ToLower() == person).ToList();
            }


            foreach (var quote in data)
            {
                if (!string.IsNullOrEmpty(searchCleaned))
                {
                    var quoteCleaned = new string(quote.Quote.Where(c => !char.IsPunctuation(c)).ToArray());
                    var regexTerm = @$"\b{searchCleaned.ToLower()}\b";

                    var mCount = Regex.Matches(quoteCleaned.ToLower(), regexTerm).Count;

                    if (mCount > 0)
                    {
                        matchCount += mCount;
                        quotes.Add((quote.Quote.Trim(), $"s{quote.SeriesNumber}e{quote.EpisodeNumber}" + $" - {quote.EpisodeName}", quote.Person));

                    }
                }
                else
                {
                    quotes.Add((quote.Quote.Trim(), $"s{quote.SeriesNumber}e{quote.EpisodeNumber}" + $" - {quote.EpisodeName}", quote.Person));
                    matchCount += 1;

                }
            }


            return new OkObjectResult(new SearchResult() { Count = matchCount, Results = quotes.Select(a => new QuoteData() { Quote = a.quote, Person = a.person, Episode = a.episode }) });

        }


        [FunctionName(nameof(Search))]
        public async Task<IActionResult> Search([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get")] HttpRequest req)
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
                return new BadRequestObjectResult("No Body Specified");
            }

            string searchCleaned = null;
            if (!string.IsNullOrEmpty(searchParameters.SearchTerm))
                searchCleaned = new string(searchParameters.SearchTerm.Where(c => !char.IsPunctuation(c)).ToArray());

            int matchCount = 0;

            BlobClient blobClient = _blobContainerClient.GetBlobClient("content.json");

            var content = await blobClient.DownloadAsync();
            var json = content.Value.Content;

            var data = await JsonSerializer.DeserializeAsync<List<JsonData>>(json);
            var quotes = new List<(string quote, string episode, string person)>();


            if (searchParameters.SeriesNumber.HasValue)
            {
                data = data.Where(a => a.SeriesNumber == searchParameters.SeriesNumber.Value).ToList();
            }

            if (searchParameters.EpisodeNumber.HasValue)
            {
                data = data.Where(a => a.EpisodeNumber == searchParameters.EpisodeNumber.Value).ToList();
            }

            if (!string.IsNullOrEmpty(searchParameters.Person))
            {
                data = data.Where(a => a.Person.ToLower() == searchParameters.Person.ToLower()).ToList();
            }


            foreach (var quote in data)
            {
                if (!string.IsNullOrEmpty(searchCleaned))
                {
                    var quoteCleaned = new string(quote.Quote.Where(c => !char.IsPunctuation(c)).ToArray());
                    var regexTerm = @$"\b{searchCleaned.ToLower()}\b";

                    var mCount = Regex.Matches(quoteCleaned.ToLower(), regexTerm).Count;

                    if (mCount > 0)
                    {
                        matchCount += mCount;
                        quotes.Add((quote.Quote.Trim(), $"s{quote.SeriesNumber}e{quote.EpisodeNumber}" + $" - {quote.EpisodeName}", quote.Person));

                    }
                }
                else
                {
                    quotes.Add((quote.Quote.Trim(), $"s{quote.SeriesNumber}e{quote.EpisodeNumber}" + $" - {quote.EpisodeName}", quote.Person));
                    matchCount += 1;

                }
            }


            return new OkObjectResult(new SearchResult() { Count = matchCount, Results = quotes.Select(a => new QuoteData() { Quote = a.quote, Person = a.person, Episode = a.episode }) });

        }

       
    }
}
