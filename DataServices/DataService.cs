using ArtistInfoSearcher;

public abstract class DataService
{
    public abstract ServiceType ServiceType { get; }
    public abstract SearchResult GetSearchResult(string artistName);
}