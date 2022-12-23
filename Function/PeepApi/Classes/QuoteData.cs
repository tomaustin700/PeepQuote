using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PeepApi.Classes
{
    public class QuoteData
    {
        [JsonPropertyName("quote")]
        public string Quote { get; set; }
        [JsonPropertyName("episode")]
        public string Episode { get; set; }
        [JsonPropertyName("person")]
        public string Person { get; set; }
        [JsonPropertyName("image")]
        public string Image { get; set; }
    }
}
