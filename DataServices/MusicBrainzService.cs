using ArtistInfoSearcher;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Xml.Linq;

namespace ArtistInfoSearcher;

public class MusicBrainzService : DataService
{
    private static Query Query { get; } = new("Banjo", "0.0.1");
    public override ServiceType ServiceType => ServiceType.Musicbrainz;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var musicBrainzID = await GetMusicBrainzIDAsync(artistName);
        Console.WriteLine($"{artistName} {musicBrainzID}");

        var result = new SearchResult();
        result.Albums  = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.Album);
        result.EPs     = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.EP);
        result.Singles = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.Single);
        return result;
    }

    public async Task<Guid> GetMusicBrainzIDAsync(string artistName)
    {
        ISearchResults<ISearchResult<IArtist>> result = await Query.FindArtistsAsync(artistName, limit: 1);
        ISearchResult<IArtist>? sureResult = result.Results.FirstOrDefault(x => x.Score == 100);
        return sureResult?.Item.Id ?? Guid.Empty;
    }

    private async Task<List<Entity>?> GetEnitiesByTypeAsync(Guid musicBrainzArtistID, ReleaseType type)
    {
        if (musicBrainzArtistID == Guid.Empty)
        {
            return null;
        }

        var answer = await Query.BrowseArtistReleaseGroupsAsync(musicBrainzArtistID, limit: 10000, type: type);

        return answer.Results
                     .Where(x => x.SecondaryTypes == null || !x.SecondaryTypes.Any())
                     .Select(x => new Entity(x.Title!, x.FirstReleaseDate!.NearestDate.Year))
                     .OrderByDescending(x => x.Year)
                     .ToList();
    }
}