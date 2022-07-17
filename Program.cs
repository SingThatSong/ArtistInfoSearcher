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
        var musicBrainzService = new MusicBrainzService();
        var result = musicBrainzService.GetSearchResult(name);

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