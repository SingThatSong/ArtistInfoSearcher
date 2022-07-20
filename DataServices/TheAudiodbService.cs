using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace ArtistInfoSearcher.DataServices;

public class TheAudiodbService : DataService
{
    public override ServiceType ServiceType => ServiceType.TheAudiodb;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler());
        var id = await GetArtistID(artistName, httpClient);

        if (id == null) return new SearchResult();

        var answer = await httpClient.GetStringAsync($"https://theaudiodb.com/api/v1/json/2/album.php?i={id}");
        var albumArray = JsonDocument.Parse(answer).RootElement.GetProperty("album");

        if (albumArray.ValueKind == JsonValueKind.Null) return new SearchResult();

        var doc = albumArray.EnumerateArray().ToList();

        var result = new SearchResult();
        result.Albums = doc.Where(x => x.GetProperty("strReleaseFormat").GetString() == "Album")
                           .Select(x => new Entity(x.GetProperty("strAlbum").GetString()!, int.Parse(x.GetProperty("intYearReleased")!.GetString()!)))
                           .ToList();
        result.Singles = doc.Where(x => x.GetProperty("strReleaseFormat").GetString() == "Single")
                           .Select(x => new Entity(x.GetProperty("strAlbum").GetString()!, int.Parse(x.GetProperty("intYearReleased")!.GetString()!)))
                           .ToList();
        result.EPs = doc.Where(x => x.GetProperty("strReleaseFormat").GetString() == "EP")
                        .Select(x => new Entity(x.GetProperty("strAlbum").GetString()!, int.Parse(x.GetProperty("intYearReleased")!.GetString()!)))
                        .ToList();
        result.Lives = doc.Where(x => x.GetProperty("strReleaseFormat").GetString() == "Live")
                        .Select(x => new Entity(x.GetProperty("strAlbum").GetString()!, int.Parse(x.GetProperty("intYearReleased")!.GetString()!)))
                        .ToList();
        result.Compilations = doc.Where(x => x.GetProperty("strReleaseFormat").GetString() == "Compilation")
                                 .Select(x => new Entity(x.GetProperty("strAlbum").GetString()!, int.Parse(x.GetProperty("intYearReleased")!.GetString()!)))
                                 .ToList();

        var otherTypes = new List<string> { "Album", "Single", "EP", "Live", "Compilation" };

        result.Others = doc.Where(x => !otherTypes.Contains(x.GetProperty("strReleaseFormat")!.GetString()!))
                           .Select(x => new Entity(x.GetProperty("strAlbum").GetString()!, int.Parse(x.GetProperty("intYearReleased")!.GetString()!)))
                           .ToList();

        return result;
    }

    private async Task<string?> GetArtistID(string artistName, HttpClient httpClient)
    {
        try
        {
            var answer = await httpClient.GetStringAsync($"https://theaudiodb.com/api/v1/json/2/search.php?s={artistName}");
            var doc = JsonDocument.Parse(answer);
            return doc.RootElement.GetProperty("artists")[0].GetProperty("idArtist").GetString();
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