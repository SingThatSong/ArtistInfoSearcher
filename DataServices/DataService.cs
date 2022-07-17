namespace ArtistInfoSearcher;

public abstract class DataService
{
    public abstract ServiceType ServiceType { get; }
    public abstract Task<SearchResult> GetSearchResultAsyncInternal(string artistName);
    public async Task<SearchResult> GetSearchResultAsync(string artistName)
    {
        var result = await GetSearchResultAsyncInternal(artistName);

        result.Albums?.ForEach(x => x.ServiceType = ServiceType);
        result.EPs?.ForEach(x => x.ServiceType = ServiceType);
        result.Singles?.ForEach(x => x.ServiceType = ServiceType);
        result.AllTracks?.ForEach(x => x.ServiceType = ServiceType);

        return result;
    }
}