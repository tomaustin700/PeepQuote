using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PeepApi.Classes
{
    public class SearchParameters
    {
        [JsonPropertyName("searchTerm")]
        public string SearchTerm { get; set; }
        [JsonPropertyName("seriesNumber")]

        public int? SeriesNumber { get; set; }
        [JsonPropertyName("episodeNumber")]

        public int? EpisodeNumber { get; set; }
        [JsonPropertyName("person")]

        public string Person { get; set; }
    }
}
