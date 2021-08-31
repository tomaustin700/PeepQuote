using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeepApi.Classes
{
    public class SearchResult
    {
        public int Count { get; set; }
        public IEnumerable<QuoteData> Results { get; set; }
    }
}
