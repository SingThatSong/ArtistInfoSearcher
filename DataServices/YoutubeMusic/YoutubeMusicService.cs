using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ArtistInfoSearcher.DataServices.YoutubeMusic;

/// <summary>
/// Using https://github.com/sigma67/ytmusicapi
/// </summary>
public class YoutubeMusicService : DataService
{
    public override ServiceType ServiceType => ServiceType.YoutubeMusic;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        await Task.Delay(0);

        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "C:\\Python311\\python.exe";
        start.Arguments = string.Format("{0} {1}", "C:\\Users\\maxim\\source\\repos\\ArtistInfoSearcher\\DataServices\\YoutubeMusic\\setup.py", $"\"{artistName}\"");
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        using Process? process = Process.Start(start);
        using StreamReader? reader = process?.StandardOutput;
        string? result = reader?.ReadToEnd();

        if (result?.Length == 0) return new SearchResult();

        var results = result!.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var songsDoc = JsonDocument.Parse(results.First());
        var doc = JsonDocument.Parse(results.Skip(1).First());

        var returnResult = new SearchResult()
        {
            Songs = ParseSongs(songsDoc)
        };
        List<AlbumParseData> albumData;

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            albumData = results.Skip(3).Select(ParseAlbumData).ToList();
            var doc2 = JsonDocument.Parse(results.Skip(1).First());

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var type = item.GetProperty("type").GetString();
                var entity = new Album(item.GetProperty("title").GetString()!, int.Parse(item.GetProperty("year").GetString()!));

                var songsData = albumData.FirstOrDefault(x => x.Type == type && x.Title == entity.Title && x.Year == entity.Year.ToString());
                if (songsData != null)
                {
                    entity.Songs = songsData.SongParseDatas.Select(x => new Song(x.Number, x.Title, x.Duration)).ToList();
                }
                else
                {

                }

                switch (type)
                {
                    case "Album": returnResult.Albums.Add(entity); break;
                    case "EP": returnResult.EPs.Add(entity); break;
                    case "Single": returnResult.Singles.Add(entity); break;
                    default: returnResult.Others.Add(entity); break;
                }
            }

            foreach (var item in doc2.RootElement.EnumerateArray())
            {
                var entity = new Album(item.GetProperty("title").GetString()!, int.Parse(item.GetProperty("year").GetString()!));

                var songsData = albumData.FirstOrDefault(x => x.Title == entity.Title && x.Year == entity.Year.ToString());
                if (songsData != null)
                {
                    entity.Songs = songsData.SongParseDatas.Select(x => new Song(x.Number, x.Title, x.Duration)).ToList();
                }

                returnResult.Singles.Add(entity);
            }
        }
        else
        {
            albumData = results.Skip(2).Select(ParseAlbumData).ToList();

            foreach (var item in doc.RootElement.GetProperty("albums").GetProperty("results").EnumerateArray())
            {
                var entity = new Album(item.GetProperty("title").GetString()!, int.Parse(item.GetProperty("year").GetString()!));
                var songsData = albumData.FirstOrDefault(x => x.Title == entity.Title && x.Year == entity.Year.ToString());
                if (songsData != null)
                {
                    entity.Songs = songsData.SongParseDatas.Select(x => new Song(x.Number, x.Title, x.Duration)).ToList();

                    switch (songsData.Type)
                    {
                        case "Album": returnResult.Albums.Add(entity); break;
                        case "EP": returnResult.EPs.Add(entity); break;
                        case "Single": returnResult.Singles.Add(entity); break;
                        default: returnResult.Others.Add(entity); break;
                    }
                }
                else
                {

                }
            }

            foreach (var item in doc.RootElement.GetProperty("singles").GetProperty("results").EnumerateArray())
            {
                var entity = new Album(item.GetProperty("title").GetString()!, int.Parse(item.GetProperty("year").GetString()!));
                var songsData = albumData.FirstOrDefault(x => x.Title == entity.Title && x.Year == entity.Year.ToString());
                if (songsData != null)
                {
                    entity.Songs = songsData.SongParseDatas.Select(x => new Song(x.Number, x.Title, x.Duration)).ToList();
                }
                else
                {

                }

                returnResult.Singles.Add(entity);
            }
        }

        return returnResult;
    }

    private List<Song> ParseSongs(JsonDocument songsDoc)
    {
        return songsDoc.RootElement
                           .GetProperty("tracks")
                           .EnumerateArray()
                           .Select((x, i) => new Song(0,
                                                      x.GetProperty("title").GetString()!,
                                                      TimeSpan.FromSeconds(x.GetProperty("duration_seconds").GetInt32())))
                           .ToList();
    }

    private AlbumParseData ParseAlbumData(string albumJson)
    {
        var doc = JsonDocument.Parse(albumJson);

        return new AlbumParseData(
            doc.RootElement.GetProperty("title").GetString()!,
            doc.RootElement.GetProperty("type").GetString()!,
            doc.RootElement.GetProperty("year").GetString()!,
            doc.RootElement.GetProperty("tracks")
                           .EnumerateArray()
                           .Select((x, i) => new SongParseData(i + 1,
                                                               x.GetProperty("title").GetString()!,
                                                               TimeSpan.FromSeconds(x.GetProperty("duration_seconds").GetInt32())))
                           .ToList());
    }
}

public record AlbumParseData(string Title, string Type, string Year, List<SongParseData> SongParseDatas);
public record SongParseData(int Number, string Title, TimeSpan Duration);
