using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeepApi.Classes
{
    public class SearchParameters
    {
        public string SearchTerm { get; set; }
        public int? SeriesNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public string Person { get; set; }
    }
}
