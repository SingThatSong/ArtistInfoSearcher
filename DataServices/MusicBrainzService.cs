using ArtistInfoSearcher;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Xml.Linq;

public class MusicBrainzService : DataService
{
    private static Query Query { get; } = new("Banjo", "0.0.1");
    public override ServiceType ServiceType => ServiceType.Musicbrainz;

    public override SearchResult GetSearchResult(string artistName)
    {
        var musicBrainzID = GetMusicBrainzID(artistName);
        Console.WriteLine($"{artistName} {musicBrainzID}");

        var result = new SearchResult();
        result.Albums = GetEnitiesByType(musicBrainzID, ReleaseType.Album);
        result.EPs = GetEnitiesByType(musicBrainzID, ReleaseType.EP);
        result.Singles = GetEnitiesByType(musicBrainzID, ReleaseType.Single);
        return result;
    }

    public Guid GetMusicBrainzID(string artistName)
    {
        ISearchResults<ISearchResult<IArtist>> result = Query.FindArtists(artistName);
        ISearchResult<IArtist>? sureResult = result.Results.FirstOrDefault(x => x.Score == 100);
        return sureResult?.Item.Id ?? Guid.Empty;
    }

    private static List<Entity>? GetEnitiesByType(Guid musicBrainzArtistID, ReleaseType type)
    {
        if (musicBrainzArtistID == Guid.Empty)
        {
            return null;
        }

        var recieved = Query.BrowseArtistReleaseGroups(musicBrainzArtistID, limit: 10000, type: type).Results.ToList();

        return recieved.Where(x => x.SecondaryTypes == null || !x.SecondaryTypes.Any())
                       .Select(x => new Entity(x.Title!, x.FirstReleaseDate!.NearestDate.Year))
                       .OrderByDescending(x => x.Year).ToList();
    }
}