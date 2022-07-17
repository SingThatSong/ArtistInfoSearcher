using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace ArtistInfoSearcher;

public class ItunesService : DataService
{
    public override ServiceType ServiceType => ServiceType.Itunes;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        artistName = artistName.Replace(' ', '+');
        var httpClient = new HttpClient(new SocketsHttpHandler());

        var artistID = await GetArtistID(artistName, httpClient);

        var result = new SearchResult();
        if (artistID == null) return result;

        var allReleases = await GetAllReleasesAsync(artistID.Value, httpClient);

        if (allReleases != null)
        {
            result.Singles = allReleases.Where(x => x.Title.EndsWith(" - Single")).ToList();
            result.EPs = allReleases.Where(x => x.Title.EndsWith(" - EP")).ToList();
            result.Albums = allReleases.Except(result.Singles).Except(result.EPs).Where(x => !x.Title.EndsWith("Version)") && !x.Title.EndsWith("Edition)")).ToList();

            result.Singles.ForEach(x => x.Title = x.Title.Remove(x.Title.IndexOf(" - Single")));
            result.Singles = result.Singles.DistinctBy(x => x.Title)
                                           .ToList();

            result.EPs.ForEach(x => x.Title = x.Title.Remove(x.Title.IndexOf(" - EP")));
            result.EPs = result.Singles.DistinctBy(x => x.Title)
                                       .ToList();

            result.Albums = result.Albums.DistinctBy(x => x.Title).ToList();
        }

        return result;
    }

    private static async Task<long?> GetArtistID(string artistName, HttpClient httpClient)
    {
        try
        {
            var answer = await httpClient.GetStringAsync($"https://itunes.apple.com/search?term={artistName}&entity=allArtist&attribute=allArtistTerm&limit=1");
            var doc = JsonDocument.Parse(answer);
            return doc.RootElement.GetProperty("results")[0].GetProperty("artistId").GetInt64();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<List<Entity>?> GetAllReleasesAsync(long artistID, HttpClient httpClient)
    {
        try
        {
            var answer = await httpClient.GetStringAsync($"https://itunes.apple.com/lookup?id={artistID}&entity=album&limit=200");
            var doc = JsonDocument.Parse(answer);

            var result = new List<Entity>();
            foreach (var album in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                if (album.TryGetProperty("collectionName", out var collectionProperty))
                {
                    var title = collectionProperty.GetString();
                    if (title != null)
                    {
                        result.Add(new Entity(title, album.GetProperty("releaseDate").GetDateTime().Year));
                    }
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }
}