using ArtistInfoSearcher;
using CommandDotNet.Tokens;
using DiscogsClient;
using DiscogsClient.Data.Query;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using RestSharp;
using RestSharp.Authenticators;
using RestSharpHelper.OAuth1;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public class DiscogsService : DataService
{
    public override ServiceType ServiceType => ServiceType.Musicbrainz;
    private DiscogsClient.DiscogsClient DiscogsClient;

    public DiscogsService()
    {
        var userAgent = "BanjoClient/0.1 +http://banjoclient.org";

        //var client = new RestClient("https://api.discogs.com/oauth/request_token")
        //{
        //    Authenticator = OAuth1Authenticator.ForProtectedResource("xyUUWkwRGGdovNJeujtQxpcGPNQogNVWYqUDgCTJ", "VNFwoLtWvAASjgsyWHnMiNgdXFKcVnpaIqJegkBq", null, null, signatureMethod: RestSharp.Authenticators.OAuth.OAuthSignatureMethod.PlainText),
        //    UserAgent = userAgent
        //};

        //var response = client.Execute(new RestRequest(Method.GET));

        //var parsed = response.Content.Split('&').ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);
        //client.Authenticator = OAuth1Authenticator.ForAccessToken("vlKthlJSuQYeuXragPxr",
        //                                                          "SepakVSOIPquFWpWZbJBUCsjzkHrvDdV",
        //                                                          parsed["oauth_token"],
        //                                                          parsed["oauth_token_secret"],
        //                                                          "IEkKuBmnfz");

        //var auth = new RestRequest("https://api.discogs.com/oauth/access_token", Method.POST);
        //var response2 = client.Execute(auth);

        var oAuthCompleteInformation = new OAuthCompleteInformation(
                                  "vlKthlJSuQYeuXragPxr",
                                  "SepakVSOIPquFWpWZbJBUCsjzkHrvDdV",
                                  "xyUUWkwRGGdovNJeujtQxpcGPNQogNVWYqUDgCTJ",
                                  "VNFwoLtWvAASjgsyWHnMiNgdXFKcVnpaIqJegkBq");

        DiscogsClient = new DiscogsClient.DiscogsClient(oAuthCompleteInformation, userAgent);
    }

    public override void Init()
    {
        
    }

    public int? GetArtistID(string artist)
    {
        string ApiKey = "nnru878twsy4ajfdpmqrswbg";
        string SharedSecret = "SvFVMt8ez9";

        string con = $"{ApiKey}{SharedSecret}{((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}";

        string signature = GetMd5Hash(con);

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["apikey"] = ApiKey;
        query["sig"] = signature.ToLower();
        query["nameid"] = "MN0000114342";
        string? queryString = query.ToString();

        using var wc = new HttpClient();
        var f = wc.GetAsync($"http://api.rovicorp.com/data/v1.1/name/discography?{queryString}").Result;

        return 0;
    }

    static string GetMd5Hash(string input)
    {
        using (MD5 md5Hash = MD5.Create())
        {
            // http://msdn.microsoft.com/ru-ru/library/system.security.cryptography.md5%28v=vs.110%29.aspx
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }

    //private static List<Entity>? GetEnitiesByType(Guid musicBrainzArtistID, ReleaseType type)
    //{
    //    if (musicBrainzArtistID == Guid.Empty)
    //    {
    //        return null;
    //    }

    //    var recieved = Query.BrowseArtistReleaseGroups(musicBrainzArtistID, limit: 10000, type: type).Results.ToList();

    //    return recieved.Where(x => x.SecondaryTypes == null || !x.SecondaryTypes.Any())
    //                   .Select(x => new Entity(x.Title!, x.FirstReleaseDate!.NearestDate.Year))
    //                   .OrderByDescending(x => x.Year).ToList();
    //}

    protected override List<Entity>? GetAllAlbumsInternal(Guid musicBrainzArtistID)
    {
        return null;
        //return GetEnitiesByType(musicBrainzArtistID, ReleaseType.Album);
    }

    protected override List<Entity>? GetAllEPsInternal(Guid musicBrainzArtistID)
    {
        return null;
        //return GetEnitiesByType(musicBrainzArtistID, ReleaseType.EP);
    }

    protected override List<Entity>? GetAllSinglesInternal(Guid musicBrainzArtistID)
    {
        return null;
        //return GetEnitiesByType(musicBrainzArtistID, ReleaseType.Single);
    }
}