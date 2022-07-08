using CommandDotNet;
using ConsoleTableExt;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System.Linq;

public class Program
{
    static int Main(string[] args)
    {
        return new AppRunner<Program>().Run(args);
    }

    public void RequestArtist(string name)
    {
        new DiscogsService().GetArtistID(name);

        var musicBrainzService = new MusicBrainzService();

        var musicBrainzID = musicBrainzService.GetMusicBrainzID(name);
        Console.WriteLine($"{name} {musicBrainzID}");

        var result = new SearchResult();
        result.Albums  = musicBrainzService.GetAllAlbums(musicBrainzID);
        result.EPs     = musicBrainzService.GetAllEPs(musicBrainzID);
        result.Singles = musicBrainzService.GetAllSingles(musicBrainzID);

        ConsoleTableBuilder
            .From(result.Albums)
            .WithTitle("Albums")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(result.EPs)
            .WithTitle("EPs")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(result.Singles)
            .WithTitle("Singles")
            .ExportAndWriteLine();
    }
}