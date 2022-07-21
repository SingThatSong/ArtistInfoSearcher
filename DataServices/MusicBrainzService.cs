using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Xml.Linq;

namespace ArtistInfoSearcher.DataServices;

public class MusicBrainzService : DataService
{
    private static Query Query { get; } = new("Banjo", "0.0.1");
    public override ServiceType ServiceType => ServiceType.Musicbrainz;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var musicBrainzID = await GetMusicBrainzIDAsync(artistName);
        Console.WriteLine($"{artistName} {musicBrainzID}");

        var result = new SearchResult();
        result.Albums = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.Album);
        result.EPs = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.EP);
        result.Singles = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.Single);
        result.Others = await GetEnitiesByTypeAsync(musicBrainzID, ReleaseType.Other);
        return result;
    }

    public async Task<Guid> GetMusicBrainzIDAsync(string artistName)
    {
        ISearchResults<ISearchResult<IArtist>> result = await Query.FindArtistsAsync(artistName, limit: 10);
        ISearchResult<IArtist>? sureResult = result.Results.FirstOrDefault(x => x.Item.Name == artistName);
        return sureResult?.Item.Id ?? Guid.Empty;
    }

    private async Task<List<Album>?> GetEnitiesByTypeAsync(Guid musicBrainzArtistID, ReleaseType type)
    {
        if (musicBrainzArtistID == Guid.Empty)
        {
            return null;
        }

        var answer = await Query.BrowseArtistReleaseGroupsAsync(musicBrainzArtistID, limit: 10000, type: type);

        return answer.Results
                     .Where(x => x.SecondaryTypes == null || !x.SecondaryTypes.Any())
                     .Select(x => new Album(x.Title!, x.FirstReleaseDate!.NearestDate.Year))
                     .OrderByDescending(x => x.Year)
                     .ToList();
    }
}