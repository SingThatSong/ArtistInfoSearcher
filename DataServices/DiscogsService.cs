using RestSharpHelper.OAuth1;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using DiscogsClient;
using DiscogsClient.Internal;
using DiscogsClient.Data.Query;

namespace ArtistInfoSearcher.DataServices;

public class DiscogsService : DataService
{
    public override ServiceType ServiceType => ServiceType.Discogs;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        //Create authentication based on Discogs token
        var tokenInformation = new TokenAuthenticationInformation("zTnmUSwmJgwhJEqvYyuiCNJHFcwtvINfWANTaQcI");
        //Create discogs client using the authentication
        var discogsClient = new DiscogsClient.DiscogsClient(tokenInformation);
        var artistSearch = await discogsClient.SearchAsync(new DiscogsSearch() 
        { 
            query = artistName, 
            type = DiscogsEntityType.artist
        });

        var artistID = artistSearch?.results.FirstOrDefault(x => x.title == artistName)?.id;

        if (!artistID.HasValue) return new SearchResult();

        var releases = discogsClient.GetArtistReleaseAsEnumerable(artistID.Value)
                                    .ToList()
                                    .Where(x => x.type == "master" && x.artist != "Various" && x.role != "UnofficialRelease")
                                    .DistinctBy(x => x.title)
                                    .ToList();

        return new SearchResult()
        {
            Appearances = releases.Where(x => x.artist != artistName).Select(x => new Album(x.title, x.year)).ToList(),
            Others = releases.Where(x => x.artist == artistName).Select(x => new Album(x.title, x.year)).ToList(),
        };
    }
}