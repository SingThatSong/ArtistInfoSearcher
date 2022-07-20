using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Account;

namespace ArtistInfoSearcher.DataServices;

public class YandexMusicService : DataService
{
    public override ServiceType ServiceType => ServiceType.YandexMusic;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var api = new YandexMusicApi();
        var auth = new AuthStorage();
        await api.User.AuthorizeAsync(auth, "grish1n.m4x", "mIuTkw9vXMHyheXvUdqz");
        var searchResult = await api.Search.ArtistAsync(auth, artistName);

        var artistID = searchResult.Result.Artists?.Results.FirstOrDefault(x => x.Name == artistName)?.Id;
        if (artistID == null) return new SearchResult();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "OAuth " + auth.Token);

        var answer = await httpClient.GetStringAsync($"https://music.yandex.ru/handlers/artist.jsx?artist={artistID}&what=albums&overembed=false");
        var doc = JsonDocument.Parse(answer);

        var result = new SearchResult()
        {
            Albums = new List<Entity>(),
            Singles = new List<Entity>(),
            EPs = new List<Entity>(),
            Lives = new List<Entity>(),
            Others = new List<Entity>()
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
                    result.Others.Add(entity);
                }
            }
            else
            {
                if (album.TryGetProperty("version", out var version))
                {
                    if (version.GetString()!.Contains("Live"))
                    {
                        result.Lives.Add(entity);
                    }
                    else
                    {
                        result.Others.Add(entity);
                    }
                }
                else
                {
                    if (entity.Title.EndsWith("EP"))
                    {
                        result.EPs.Add(entity);
                    }
                    else
                    {
                        result.Albums.Add(entity);
                    }
                }
            }
        }

        return result;
    }
}