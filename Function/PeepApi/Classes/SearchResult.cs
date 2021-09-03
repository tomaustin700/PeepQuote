using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PeepApi.Classes
{
    public class SearchResult
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("results")]
        public IEnumerable<QuoteData> Results { get; set; }
    }
}
