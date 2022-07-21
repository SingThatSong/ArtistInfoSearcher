using SpotifyAPI.Web;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace ArtistInfoSearcher.DataServices;

public class SpotifyService : DataService
{
    public override ServiceType ServiceType => ServiceType.Spotify;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var config = SpotifyClientConfig.CreateDefault();

        var request = new ClientCredentialsRequest("9cae58c8e7734dc3a03fa8dd9b49d606", "cdea925ad76b4ef8a90cceeae14350cf");
        var response = await new OAuthClient(config).RequestToken(request);

        var spotify = new SpotifyClient(config.WithToken(response.AccessToken));

        var artists = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Artist, artistName));
        var id = artists.Artists?.Items?.FirstOrDefault(x => x.Name == artistName);

        if (id == null) return new SearchResult();

        var firstPage = await spotify.Artists.GetAlbums(id.Id, new ArtistsAlbumsRequest() { Limit = 50 });

        var albumsRecieved = new List<SimpleAlbum>(firstPage.Total!.Value);
        albumsRecieved.AddRange(firstPage.Items!);

        while (albumsRecieved.Count < firstPage.Total)
        {
            var page = await spotify.Artists.GetAlbums(id.Id, new ArtistsAlbumsRequest() { Limit = 50, Offset = albumsRecieved.Count });
            albumsRecieved.AddRange(page.Items!);
        }

        var albums = albumsRecieved.Where(x => x.AlbumGroup == "album" && x.AlbumType == "album").ToList();
        var singles = albumsRecieved.Where(x => x.AlbumGroup == "single" && x.AlbumType == "single").ToList();
        var compilations = albumsRecieved.Where(x => x.AlbumGroup == "compilation" && x.AlbumType == "compilation").ToList();
        var appearances = albumsRecieved.Where(x => x.AlbumGroup == "appears_on" && !x.Artists.Any(x => x.Name == "Various Artists")).ToList();
        var other = albumsRecieved.Except(albums)
                                  .Except(singles)
                                  .Except(compilations)
                                  .Except(appearances)
                                  .Where(x => !x.Artists.Any(x => x.Name == "Various Artists"))
                                  .ToList();

        return new SearchResult()
        {
            Albums = albums.Select(x => new Album(x.Name, int.Parse(x.ReleaseDate!.Substring(0, 4)))).ToList(),
            Singles = singles.Select(x => new Album(x.Name, int.Parse(x.ReleaseDate!.Substring(0, 4)))).ToList(),
            Compilations = compilations.Select(x => new Album(x.Name, int.Parse(x.ReleaseDate!.Substring(0, 4)))).ToList(),
            Appearances = appearances.Select(x => new Album(x.Name, int.Parse(x.ReleaseDate!.Substring(0, 4)))).ToList(),
            Others = other.Select(x => new Album(x.Name, int.Parse(x.ReleaseDate!.Substring(0, 4)))).ToList(),
        };
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

    private static async Task<List<Album>?> GetAllReleasesAsync(long artistID, HttpClient httpClient)
    {
        try
        {
            var answer = await httpClient.GetStringAsync($"https://itunes.apple.com/lookup?id={artistID}&entity=album&limit=200");
            var doc = JsonDocument.Parse(answer);

            var result = new List<Album>();
            foreach (var album in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                if (album.TryGetProperty("collectionName", out var collectionProperty))
                {
                    var title = collectionProperty.GetString();
                    if (title != null)
                    {
                        result.Add(new Album(title, album.GetProperty("releaseDate").GetDateTime().Year));
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