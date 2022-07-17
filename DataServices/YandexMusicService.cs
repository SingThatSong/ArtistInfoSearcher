using ArtistInfoSearcher;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Xml.Linq;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Account;

namespace ArtistInfoSearcher;

public class YandexMusicService : DataService
{
    public override ServiceType ServiceType => ServiceType.YandexMusic;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var api = new YandexMusicApi();
        var auth = new AuthStorage();
        await api.User.AuthorizeAsync(auth, "", "");
        var searchResult = await api.Search.ArtistAsync(auth, artistName);

        var data = await api.Artist.GetAsync(auth, searchResult.Result.Artists.Results[0].Id);
        
        var result =  new SearchResult()
        {
            Albums = data.Result.Albums.Where(x => x.Type != "single").Select(x => new Entity(x.Title, x.Year)).ToList(),
            Singles = data.Result.Albums.Where(x => x.Type == "single").Select(x => new Entity(x.Title, x.Year)).ToList(),
            EPs = data.Result.Albums.Where(x => x.Type == "ep").Select(x => new Entity(x.Title, x.Year)).ToList(),
        };

        return result;
    }}