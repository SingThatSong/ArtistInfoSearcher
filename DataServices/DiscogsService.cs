using RestSharpHelper.OAuth1;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using DiscogsClient;
using DiscogsClient.Internal;

namespace ArtistInfoSearcher.DataServices;

public class DiscogsService : DataService
{
    public override ServiceType ServiceType => ServiceType.Discogs;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        await Task.Delay(1);

        //Create authentication based on Discogs token
        var tokenInformation = new TokenAuthenticationInformation("zTnmUSwmJgwhJEqvYyuiCNJHFcwtvINfWANTaQcI");
        //Create discogs client using the authentication
        var discogsClient = new DiscogsClient.DiscogsClient(tokenInformation);
        var artistSearch = discogsClient.SearchAsEnumerable(new DiscogsClient.Data.Query.DiscogsSearch() { artist = artistName, type = DiscogsClient.Data.Query.DiscogsEntityType.master }).ToList();

        return new SearchResult()
        {
            Others = artistSearch.Where(x => x.year.HasValue)
                                 .Select(x => new Album(x.title.Substring($"{artistName} - ".Length), x.year!.Value))
                                 .ToList()
        };
    }
}