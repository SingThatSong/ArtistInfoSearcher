using ArtistInfoSearcher;

public abstract class DataService
{
    public abstract ServiceType ServiceType { get; }

    public virtual void Init() { }

    public List<Entity>? GetAllAlbums(Guid musicBrainzArtistID)
    {
        var values = GetAllAlbumsInternal(musicBrainzArtistID);
        values?.ForEach(x => x.ServiceType = ServiceType);
        return values;
    }

    public List<Entity>? GetAllEPs(Guid musicBrainzArtistID)
    {
        var values = GetAllEPsInternal(musicBrainzArtistID);
        values?.ForEach(x => x.ServiceType = ServiceType);
        return values;
    }

    public List<Entity>? GetAllSingles(Guid musicBrainzArtistID)
    {
        var values = GetAllSinglesInternal(musicBrainzArtistID);
        values?.ForEach(x => x.ServiceType = ServiceType);
        return values;
    }

    protected abstract List<Entity>? GetAllAlbumsInternal(Guid musicBrainzArtistID);
    protected abstract List<Entity>? GetAllEPsInternal(Guid musicBrainzArtistID);
    protected abstract List<Entity>? GetAllSinglesInternal(Guid musicBrainzArtistID);
}