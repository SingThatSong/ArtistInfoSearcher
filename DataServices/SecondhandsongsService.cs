using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace ArtistInfoSearcher.DataServices;

public class SecondhandsongsService : DataService
{
    public override ServiceType ServiceType => ServiceType.Secondhandsongs;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        artistName = artistName.Replace(' ', '+');
        var httpClient = new HttpClient(new SocketsHttpHandler());
        string? url = await GetArtistUrl(artistName, httpClient);

        var result = new SearchResult();
        if (url == null) return result;

        var releases = await GetArtistReleases(url, httpClient);
        foreach (var release in releases)
        {
            var type = release.GetProperty("entitySubType").GetString();
            var title = release.GetProperty("title").GetString();
            var year = await GetReleaseYear(release.GetProperty("uri").GetString(), httpClient);

            var entity = new Entity(title!, year);

            switch (type)
            {
                case "album":
                    result.Albums ??= new List<Entity>();
                    result.Albums.Add(entity);
                    break;

                case "EP":
                    result.EPs ??= new List<Entity>();
                    result.EPs.Add(entity);
                    break;

                case "single":
                    result.Singles ??= new List<Entity>();
                    result.Singles.Add(entity);
                    break;

                default:
                    result.Others ??= new List<Entity>();
                    result.Others.Add(entity);
                    break;
            }
        }

        return result;
    }

    private async Task<string?> GetArtistUrl(string artistName, HttpClient httpClient)
    {
        var answer = await httpClient.GetStringAsync($"https://secondhandsongs.com/search/artist?entityType=artist&commonName={artistName}&format=json&pageSize=10");

        var parsed = JsonDocument.Parse(answer).RootElement;

        if (parsed.GetProperty("totalResults").GetInt32() == 0) return null;

        foreach (var result in parsed.GetProperty("resultPage").EnumerateArray())
        {
            if (result.GetProperty("commonName").GetString()!.Replace(' ', '+') == artistName)
            {
                return result.GetProperty("uri").GetString();
            }
        }
        return null;
    }

    private async Task<List<JsonElement>> GetArtistReleases(string url, HttpClient httpClient)
    {
        var releases = await httpClient.GetStringAsync($"{url}/releases?format=json");
        var rels = JsonDocument.Parse(releases);
        return rels.RootElement.EnumerateArray().ToList();
    }

    private async Task<int> GetReleaseYear(string? url, HttpClient httpClient)
    {
        if (url == null) return 0;

        var releases = await httpClient.GetStringAsync($"{url}?format=json");
        System.Diagnostics.Debug.WriteLine(releases);
        var rels = JsonDocument.Parse(releases);
        var year = rels.RootElement.GetProperty("date").GetString()!.Substring(0, 4);
        return int.Parse(year);
    }
}