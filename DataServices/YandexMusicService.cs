using ArtistInfoSearcher;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Text.Json;
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

        if (searchResult.Result.Artists == null) return new SearchResult();
        
        var httpClient = new HttpClient(new SocketsHttpHandler());

        var answer = await httpClient.GetStringAsync($"https://music.yandex.ru/handlers/artist.jsx?artist={searchResult.Result.Artists.Results[0].Id}&what=albums&overembed=false");
        var doc = JsonDocument.Parse(answer);

        var result = new SearchResult()
        {
            Albums = new List<Entity>(),
            Singles = new List<Entity>(),
            EPs = new List<Entity>()
        };
        foreach (var album in doc.RootElement.GetProperty("albums").EnumerateArray())
        {
            var title = album.GetProperty("title").GetString()!;
            var year = album.GetProperty("year").GetInt32();
            var entity = new Entity(title, year);

            if (album.TryGetProperty("type", out var type))
            {
                if (type.GetString() == "single")
                {
                    result.Singles.Add(entity);
                }
                else
                {
                }
            }
            else
            {
                if (album.TryGetProperty("version", out var version))
                {
                    if (!version.GetString()!.Contains("Live"))
                    {
                        result.Albums.Add(entity);
                    }
                }
                else
                {
                    result.Albums.Add(entity);
                }
            }
        }

        return result;
    }}