# PeepQuote API
PeepQuote allows you to query the entire script of Peep Show by phrases/words and filter by person, episode and series.

## Searching
Searching is done using a HTTP Post request at the following url: https://api.peepquote.com/search

Search parameters are sent using the Body of the request

```json
{
    "seriesNumber" : 1,
    "episodeNumber" : 1,
    "person" : "Mark",
    "searchTerm" : "what does it mean"

}

```

All search parameters are optional.

A Swagger page is also available at https://api.peepquote.com/swagger/ui
