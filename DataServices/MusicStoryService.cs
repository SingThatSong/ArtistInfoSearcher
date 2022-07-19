using CommandDotNet.Tokens;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Account;

namespace ArtistInfoSearcher.DataServices;

public class MusicStoryService : DataService
{
    private const string Consumer_key = "73c2354d0be078e9b1f7cc78b0b3a6c7806d6cf4";
    private const string Consumer_secret = "6854aac3d1beb557d620fb34865dc30750356869";
    private const string Access_token = "5267220a0bb426517e045c6d75492e88a1412490";
    private const string Token_secret = "8d6161342fa73d2c867d60a51c673a7eed8ee167";
    private readonly HttpClient httpClient = new(new SocketsHttpHandler());

    public override ServiceType ServiceType => ServiceType.MusicStory;

    public override async Task<SearchResult> GetSearchResultAsyncInternal(string artistName)
    {
        var id = await GetArtistID(artistName);

        if (string.IsNullOrEmpty(id)) return new SearchResult();

        var albumsFirstPage = await DoRequest($"http://api.music-story.com/artist/{id}/albums");

        List<ParseEntity> entities = ParsePage(albumsFirstPage);

        var pageCount = GetPageCount(albumsFirstPage);

        if (pageCount > 1)
        {
            var page = 2;

            while (page <= pageCount)
            {
                var albumsPage = await DoRequest($"http://api.music-story.com/artist/{id}/albums?page={page}");
                var parsedPage = ParsePage(albumsPage);
                entities.AddRange(parsedPage);
                page++;
            }
        }

        return new SearchResult()
        {
            Albums = entities.Where(x => x.Format == "Album").Select(x => new Entity(x.Title!, int.Parse(x.Date!.Substring(0, 4)))).ToList(),
            Singles = entities.Where(x => x.Format == "Single").Select(x => new Entity(x.Title!, int.Parse(x.Date!.Substring(0, 4)))).ToList(),
            EPs = entities.Where(x => x.Format == "EP").Select(x => new Entity(x.Title!, int.Parse(x.Date!.Substring(0, 4)))).ToList(),
        };
    }

    private int GetPageCount(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var el = doc.SelectSingleNode("descendant::pageCount")!.InnerText;

        return int.Parse(el);
    }

    private List<ParseEntity> ParsePage(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var el = doc.SelectNodes("descendant::item")!.Cast<XmlNode>().ToList();

        return el.Select(x => new ParseEntity()
        {
            Title = x.SelectSingleNode("descendant::title")?.InnerText,
            Format = x.SelectSingleNode("descendant::format")?.InnerText,
            Date = x.SelectSingleNode("descendant::release_date")?.InnerText,
        }).ToList();
    }

    private async Task<string?> GetArtistID(string artistName)
    {
        var request = $"http://api.music-story.com/artist/search?name={artistName}";
        var requestResult = await DoRequest(request);
        var doc = new XmlDocument();
        doc.LoadXml(requestResult);
        var nodes = doc.SelectNodes("descendant::item");

        if (nodes != null)
        {
            foreach (var node in nodes.OfType<XmlNode>())
            {
                if (node["name"]!.InnerText == artistName)
                {
                    return node["id"]!.InnerText;
                }
            }
        }

        return null;
    }

    private async Task<string> DoRequest(string request)
    {
        request += request.Contains('?') ? $"&oauth_token={Access_token}" : $"?oauth_token={Access_token}";
        var signedRequest = SignRequest(request);
        return await httpClient.GetStringAsync(signedRequest);
    }

    private string SignRequest(string request)
    {
        var HTTP_METHOD = "GET";

        var requestParts = request.Split('?');

        var REQUEST = requestParts[0];
        var PARAMS = from param in requestParts[1].Split('&')
                     let keyvaluepair = param.Split('=')
                     orderby keyvaluepair[0]
                     select new
                     {
                         Parameter = EscapeUriDataStringRfc3986(keyvaluepair[0]),
                         Value = EscapeUriDataStringRfc3986(keyvaluepair[1])
                     };

        string encodedParams = "";
        foreach (var param in PARAMS)
        {
            if (encodedParams != "")
            {
                encodedParams += "&";
                encodedParams += param.Parameter + "=" + param.Value;
            }
            else encodedParams = param.Parameter + "=" + param.Value;
        }

        var chain = string.Join("&",
            EscapeUriDataStringRfc3986(HTTP_METHOD),
            EscapeUriDataStringRfc3986(REQUEST),
            EscapeUriDataStringRfc3986(encodedParams));

        return request + "&oauth_signature=" + EscapeUriDataStringRfc3986(GetSignature(chain));
    }

    /// <summary>
    /// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
    /// </summary>
    private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };

    /// <summary>
    /// Escapes a string according to the URI data string rules given in RFC 3986.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value.</returns>
    /// <remarks>
    /// The <see cref="Uri.EscapeDataString"/> method is <i>supposed</i> to take on
    /// RFC 3986 behavior if certain elements are present in a .config file.  Even if this
    /// actually worked (which in my experiments it <i>doesn't</i>), we can't rely on every
    /// host actually having this configuration element present.
    /// </remarks>
    private string EscapeUriDataStringRfc3986(string value)
    {
        // Start with RFC 2396 escaping by calling the .NET method to do the work.
        // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
        // If it does, the escaping we do that follows it will be a no-op since the
        // characters we search for to replace can't possibly exist in the string.
        StringBuilder escaped = new StringBuilder(Uri.EscapeDataString(value));

        // Upgrade the escaping to RFC 3986, if necessary.
        for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++)
        {
            escaped.Replace(UriRfc3986CharsToEscape[i], Uri.HexEscape(UriRfc3986CharsToEscape[i][0]));
        }

        // Return the fully-RFC3986-escaped string.
        return escaped.ToString();
    }

    private string GetSignature(string input)
    {
        var key = Encoding.ASCII.GetBytes(Consumer_secret + "&" + Token_secret);

        HMACSHA1 myhmacsha1 = new HMACSHA1(key);
        byte[] byteArray = Encoding.ASCII.GetBytes(input);
        return Convert.ToBase64String(myhmacsha1.ComputeHash(byteArray));
    }
}

internal class ParseEntity
{
    public string? Title { get; set; }
    public string? Date { get; set; }
    public string? Format { get; set; }
}