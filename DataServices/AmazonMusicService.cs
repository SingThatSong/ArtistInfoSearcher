using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Yandex.Music.Api.Models.Account;
using Yandex.Music.Api.Models.Search;

namespace ArtistInfoSearcher.DataServices;

public class AmazonMusicService : DataService
{
    public override ServiceType ServiceType => ServiceType.Amazon;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler());

        httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
        httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        httpClient.DefaultRequestHeaders.Add("Host", "na.mesk.skill.music.a2z.com");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://music.amazon.com");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://music.amazon.com/");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        httpClient.DefaultRequestHeaders.Add("TE", "trailers");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0");
        httpClient.DefaultRequestHeaders.Add("x-amzn-affiliate-tags", "");
        httpClient.DefaultRequestHeaders.Add("x-amzn-application-version", "1.0.10594.0");
        httpClient.DefaultRequestHeaders.Add("x-amzn-authentication", """{"interface":"ClientAuthenticationInterface.v1_0.ClientTokenElement","accessToken":""}{"interface":"ClientAuthenticationInterface.v1_0.ClientTokenElement","accessToken":""}""");
        httpClient.DefaultRequestHeaders.Add("x-amzn-csrf", """{"interface":"CSRFInterface.v1_0.CSRFHeaderElement","token":"dlVLWGVfiB+Z0ImzRq0PLdpQv9+END2ncL5V3H5SMgk=","timestamp":"1658243070574","rndNonce":"1111726468"}""");
        httpClient.DefaultRequestHeaders.Add("x-amzn-currency-of-preference", "USD");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-family", "WebPlayer");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-height", "1080");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-id", "13399770033938929");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-language", "en_US");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-model", "WEBPLAYER");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-time-zone", "Europe/Moscow");
        httpClient.DefaultRequestHeaders.Add("x-amzn-device-width", "1920");
        httpClient.DefaultRequestHeaders.Add("x-amzn-feature-flags", "hd-supported");
        httpClient.DefaultRequestHeaders.Add("x-amzn-music-domain", "music.amazon.com");
        httpClient.DefaultRequestHeaders.Add("x-amzn-os-version", "1.0");
        httpClient.DefaultRequestHeaders.Add("x-amzn-page-url", "https://music.amazon.com/search/10+years?filter=IsLibrary%257Cfalse&sc=none");
        httpClient.DefaultRequestHeaders.Add("x-amzn-ref-marker", "");
        httpClient.DefaultRequestHeaders.Add("x-amzn-referer", "music.amazon.com");
        httpClient.DefaultRequestHeaders.Add("x-amzn-request-id", "4bc87869-bb93-4784-b836-3ecb1d444e00");
        httpClient.DefaultRequestHeaders.Add("x-amzn-session-id", "139-0225612-9556362");
        httpClient.DefaultRequestHeaders.Add("x-amzn-timestamp", "1658245994030");
        httpClient.DefaultRequestHeaders.Add("x-amzn-user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0");
        httpClient.DefaultRequestHeaders.Add("x-amzn-video-player-token", "");
        httpClient.DefaultRequestHeaders.Add("x-amzn-weblab-id-overrides", "");

        var artistID = await GetArtistID(artistName, httpClient);

        var result = new SearchResult()
        {
            Albums = new List<Album>(),
            EPs = new List<Album>(),
            Singles = new List<Album>(),
            Others = new List<Album>(),
        };

        if (artistID == null) return result;

        var json = $$"""{"id":"uri://artist/{{artistID.Split('/', StringSplitOptions.RemoveEmptyEntries)[1]}}/chronological-albums","userHash":"{\"level\":\"LIBRARY_MEMBER\"}"}""";
        var content = new StringContent(json);
        var answer = await httpClient.PostAsync("https://na.mesk.skill.music.a2z.com/api/showCatalogAlbums", content);

        (json, var parsed) = await ParseFirstPage(answer);
        foreach (var entity in parsed)
        {
            if (entity.Title.EndsWith(" (Single)"))
            {
                result.Singles!.Add(new Album(entity.Title.Substring(0, entity.Title.Length - 9), int.Parse(entity.Year!)));
            }
            else
            {
                result.Others!.Add(new Album(entity.Title, int.Parse(entity.Year!)));
            }
        }

        while (json != null)
        {
            json = $$"""{"id":"uri://artist/{{artistID.Split('/', StringSplitOptions.RemoveEmptyEntries)[1]}}/chronological-albums","next": "{{json.Replace("\"", "\\\"")}}","userHash":"{\"level\":\"LIBRARY_MEMBER\"}"}""";
            content = new StringContent(json);
            answer = await httpClient.PostAsync("https://na.mesk.skill.music.a2z.com/api/showCatalogAlbums", content);
            try
            {
                (json, parsed) = await ParseNextPage(answer);

                foreach (var entity in parsed)
                {
                    if (entity.Title.EndsWith(" (Single)"))
                    {
                        result.Singles!.Add(new Album(entity.Title.Substring(0, entity.Title.Length - 9), int.Parse(entity.Year!)));
                    }
                    else
                    {
                        result.Others!.Add(new Album(entity.Title, int.Parse(entity.Year!)));
                    }
                }
            }
            catch
            {
                break;
            }
        }

        return result;
    }

    private async Task<(string? Next, List<(string Title, string Year)> Items)> ParseFirstPage(HttpResponseMessage answer)
    {
        var answerJson = await UnGzip(answer);
        var doc = JsonDocument.Parse(answerJson);

        var artistItems = doc.RootElement
                             .GetProperty("methods")[0]
                             .GetProperty("template")
                             .GetProperty("widgets")
                             .EnumerateArray()
                             .First(x => x.GetProperty("header").GetString() == "Albums");

        List<(string Title, string Year)> result = new List<(string Title, string Year)>();

        foreach (var item in artistItems.GetProperty("items").EnumerateArray())
        {
            var title = item.GetProperty("primaryText").GetProperty("text").GetString();
            var year = item.GetProperty("tertiaryText").GetString();

            result.Add((title!, year!));
        }

        string? unescaped = null;
        try
        {
            var test = doc.RootElement
                                 .GetProperty("methods")[0]
                                 .GetProperty("template")
                                 .GetProperty("widgets")[0]
                                 .GetProperty("onEndOfWidget")[0]
                                 .GetProperty("url")
                                 .GetString();
            unescaped = Uri.UnescapeDataString(test!);
            unescaped = unescaped.Substring(unescaped.IndexOf("&next=") + 6).Replace("}&userHash={\"level\":\"LIBRARY_MEMBER\"}", "}");
        }
        catch { }

        return (unescaped, result);
    }

    private async Task<(string? Next, List<(string Title, string Year)> Items)> ParseNextPage(HttpResponseMessage answer)
    {
        var answerJson = await UnGzip(answer);
        var doc = JsonDocument.Parse(answerJson);

        var artistItems = doc.RootElement
                             .GetProperty("methods")[0];

        List<(string Title, string Year)> result = new List<(string Title, string Year)>();

        foreach (var item in artistItems.GetProperty("items").EnumerateArray())
        {
            var title = item.GetProperty("primaryText").GetProperty("text").GetString();
            var year = item.GetProperty("tertiaryText").GetString();

            result.Add((title!, year!));
        }

        string? unescaped = null;
        try
        {
            var test = doc.RootElement
                                 .GetProperty("methods")[0]
                                 .GetProperty("onEndOfWidget")[0]
                                 .GetProperty("url")
                                 .GetString();
            unescaped = Uri.UnescapeDataString(test!);
            unescaped = unescaped.Substring(unescaped.IndexOf("&next=") + 6).Replace("}&userHash={\"level\":\"LIBRARY_MEMBER\"}", "}");
        }
        catch { }

        return (unescaped, result);
    }

    private async Task<string?> GetArtistID(string artistName, HttpClient httpClient)
    {
        try
        {
            var json = $$"""{"keyword":"{\"interface\":\"Web.TemplatesInterface.v1_0.Touch.SearchTemplateInterface.SearchKeywordClientInformation\",\"keyword\":\"{{artistName}}\"}","suggestedKeyword":"{{artistName}}","userHash":"{\"level\":\"LIBRARY_MEMBER\"}"}""";

            var content = new StringContent(json);
            await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Options, "https://na.mesk.skill.music.a2z.com/api/showSearch"));
            var answer = await httpClient.PostAsync("https://na.mesk.skill.music.a2z.com/api/showSearch", content);

            var answerJson = await UnGzip(answer);
            var doc = JsonDocument.Parse(answerJson);

            var artistItems = doc.RootElement
                                 .GetProperty("methods")[0]
                                 .GetProperty("template")
                                 .GetProperty("widgets")
                                 .EnumerateArray()
                                 .First(x => x.GetProperty("header").GetString() == "Artists");

            foreach (var artistItem in artistItems.GetProperty("items").EnumerateArray())
            {
                if (artistItem.GetProperty("primaryText").GetProperty("text").GetString() == artistName)
                {
                    return artistItem.GetProperty("primaryLink").GetProperty("deeplink").GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> UnGzip(HttpResponseMessage response)
    {
        var answerStream = await response.Content.ReadAsStreamAsync();
        using var to = new MemoryStream();
        using var decompressor = new GZipStream(answerStream, CompressionMode.Decompress);
        decompressor.CopyTo(to);
        to.Position = 0;
        return Encoding.UTF8.GetString(to.ToArray());
    }
}