using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ArtistInfoSearcher.DataServices;

public class WikidataService : DataService
{
    public override ServiceType ServiceType => ServiceType.Wikidata;
    private HttpClient httpClient = new(new SocketsHttpHandler());

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        artistName = Uri.EscapeDataString(artistName);
        var artistIDjson = $"https://query.wikidata.org/sparql?query=SELECT%20DISTINCT%20%3Fitem%20%3FitemLabel%20WHERE%20%7B%0A%20%20%3Fitem%20wdt%3AP31%20wd%3AQ215380.%0A%20%20%20%20%3Fitem%20%3Flabel%20%22{artistName}%22%40en%20.%0A%20%20%20%20SERVICE%20wikibase%3Alabel%20%7B%20bd%3AserviceParam%20wikibase%3Alanguage%20%22en%22.%20%7D%0A%7D";

        httpClient.DefaultRequestHeaders.Add("Accept", "application/sparql-results+json");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0");

        var answer = await httpClient.GetByteArrayAsync(artistIDjson);
        var doc = JsonDocument.Parse(answer);
        var bindings = doc.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray().FirstOrDefault();
        if (bindings.ValueKind == JsonValueKind.Null || bindings.ValueKind == JsonValueKind.Undefined) return new SearchResult();

        var entityurl = bindings.GetProperty("item").GetProperty("value").GetString();
        if (entityurl == null) return new SearchResult();

        var artistID = entityurl.Substring(entityurl.LastIndexOf('/') + 1);

        var albums = await GetAlbums(artistID);
        var eps = await GetEPs(artistID);
        var singles = await GetSingles(artistID);
        var lives = await GetLives(artistID);
        var compilations = await GetCompilations(artistID);

        var result = new SearchResult();

        result.Albums = ParseReleases(albums);
        result.EPs = ParseReleases(eps);
        result.Singles = ParseReleases(singles);
        result.Lives = ParseReleases(lives);
        result.Compilations = ParseReleases(compilations);

        return result;
    }

    private List<Entity> ParseReleases(JsonDocument doc)
    {
        List<Entity> result = new();
        foreach (var album in doc.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray())
        {
            var date = album.GetProperty("publication_date").GetProperty("value").GetDateTimeOffset();
            var title = album.GetProperty("albumLabel").GetProperty("value").GetString();
            var uri = album.GetProperty("album").GetProperty("value").GetString();

            if (!uri!.Contains(title!))
            {
                result.Add(new Entity(title!, date.Year));
            }
        }

        return result;
    }

    private async Task<JsonDocument> GetAlbums(string artistID)
    {
        var albumsQuery = $"https://query.wikidata.org/sparql?query=SELECT%20%3Falbum%20%3FalbumLabel%20%3Fpublication_date%20WHERE%20%7B%0A%20%20%3Falbum%20wdt%3AP31%20wd%3AQ482994%3B%0A%20%20%20%20wdt%3AP175%20wd%3A{artistID}%3B%0A%20%20%20%20wdt%3AP577%20%3Fpublication_date%20.%0A%20%20MINUS%20%7B%20%3Falbum%20wdt%3AP7937%20wd%3AQ222910.%20%7D%0A%20%20MINUS%20%7B%20%3Falbum%20wdt%3AP7937%20wd%3AQ209939.%20%7D%0A%20%20SERVICE%20wikibase%3Alabel%20%7B%20bd%3AserviceParam%20wikibase%3Alanguage%20%22en%22.%20%7D%0A%7D";

        var answer2 = await httpClient.GetByteArrayAsync(albumsQuery);
        var str = Encoding.UTF8.GetString(answer2);
        return JsonDocument.Parse(answer2);
    }

    private async Task<JsonDocument> GetEPs(string artistID)
    {
        var albumsQuery = $"https://query.wikidata.org/sparql?query=SELECT%20%3Falbum%20%3FalbumLabel%20%3Fpublication_date%20WHERE%20%7B%0A%20%20%3Falbum%20wdt%3AP31%20wd%3AQ169930%3B%0A%20%20%20%20wdt%3AP175%20wd%3A{artistID}%3B%0A%20%20%20%20wdt%3AP577%20%3Fpublication_date%20.%0A%20%20MINUS%20%7B%20%3Falbum%20wdt%3AP7937%20wd%3AQ222910.%20%7D%0A%20%20MINUS%20%7B%20%3Falbum%20wdt%3AP7937%20wd%3AQ209939.%20%7D%0A%20%20SERVICE%20wikibase%3Alabel%20%7B%20bd%3AserviceParam%20wikibase%3Alanguage%20%22en%22.%20%7D%0A%7D";

        var answer2 = await httpClient.GetByteArrayAsync(albumsQuery);
        var str = Encoding.UTF8.GetString(answer2);
        return JsonDocument.Parse(answer2);
    }

    private async Task<JsonDocument> GetSingles(string artistID)
    {
        var albumsQuery = $"https://query.wikidata.org/sparql?query=SELECT%20%3Falbum%20%3FalbumLabel%20%3Fpublication_date%20WHERE%20%7B%0A%20%20%3Falbum%20wdt%3AP31%20wd%3AQ134556%3B%0A%20%20%20%20wdt%3AP175%20wd%3A{artistID}%3B%0A%20%20%20%20wdt%3AP577%20%3Fpublication_date%20.%0A%20%20MINUS%20%7B%20%3Falbum%20wdt%3AP7937%20wd%3AQ222910.%20%7D%0A%20%20MINUS%20%7B%20%3Falbum%20wdt%3AP7937%20wd%3AQ209939.%20%7D%0A%20%20SERVICE%20wikibase%3Alabel%20%7B%20bd%3AserviceParam%20wikibase%3Alanguage%20%22en%22.%20%7D%0A%7D";

        var answer2 = await httpClient.GetByteArrayAsync(albumsQuery);
        var str = Encoding.UTF8.GetString(answer2);
        return JsonDocument.Parse(answer2);
    }

    private async Task<JsonDocument> GetCompilations(string artistID)
    {
        var albumsQuery = $"https://query.wikidata.org/sparql?query=SELECT+%3Falbum+%3FalbumLabel+%3Fpublication_date+WHERE+%7B%0A++%3Falbum+wdt%3AP31+wd%3AQ482994%3B%0A++++wdt%3AP7937+wd%3AQ222910%3B+%0A++++wdt%3AP175+wd%3A{artistID}%3B%0A++++wdt%3AP577+%3Fpublication_date+.%0A++SERVICE+wikibase%3Alabel+%7B+bd%3AserviceParam+wikibase%3Alanguage+%22en%22.+%7D%0A%7D";

        var answer2 = await httpClient.GetByteArrayAsync(albumsQuery);
        var str = Encoding.UTF8.GetString(answer2);
        return JsonDocument.Parse(answer2);
    }

    private async Task<JsonDocument> GetLives(string artistID)
    {
        var albumsQuery = $"https://query.wikidata.org/sparql?query=SELECT+%3Falbum+%3FalbumLabel+%3Fpublication_date+WHERE+%7B%0A++%3Falbum+wdt%3AP31+wd%3AQ482994%3B%0A++++wdt%3AP7937+wd%3AQ209939%3B+%0A++++wdt%3AP175+wd%3A{artistID}%3B%0A++++wdt%3AP577+%3Fpublication_date+.%0A++SERVICE+wikibase%3Alabel+%7B+bd%3AserviceParam+wikibase%3Alanguage+%22en%22.+%7D%0A%7D";

        var answer2 = await httpClient.GetByteArrayAsync(albumsQuery);
        var str = Encoding.UTF8.GetString(answer2);
        return JsonDocument.Parse(answer2);
    }
}