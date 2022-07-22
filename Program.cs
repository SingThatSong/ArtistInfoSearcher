using CommandDotNet;
using ConsoleTableExt;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using SearchLib;
using SearchLib.DataServices;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace ArtistInfoSearcher;

public class Program
{
    static int Main(string[] args)
    {
        return new AppRunner<Program>().Run(args);
    }

    public void RequestArtist(string name, bool verbose)
    {
        var results = new ArtistSearcher().RequestArtistAsync(name).Result;

        ConsoleTableBuilder
            .From(GroupResults(results.Albums, verbose: verbose))
            .WithTitle("Albums")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.EPs, verbose: verbose))
            .WithTitle("EPs")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.Singles, verbose: verbose))
            .WithTitle("Singles")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.Lives, verbose: verbose))
            .WithTitle("Lives")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.Compilations, verbose: verbose))
            .WithTitle("Compilations")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.Appearances, verbose: verbose))
            .WithTitle("Appearances")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.Others, verbose: verbose))
            .WithTitle("Others")
            .ExportAndWriteLine();
    }

    private List<ResultEntity> GroupResults(IEnumerable<GroupedEntity> enumerable, bool verbose = false)
    {
        List<ResultEntity> resultEntities = new List<ResultEntity>();
        foreach (var item in enumerable)
        {
            if (verbose)
            {
                if (item.Years.Count > 1 || item.Titles.Count > 1)
                {
                    var yearsSorted = item.Years.OrderByDescending(x => x.Type.Distinct().Count()).ToList();
                    var titlesSorted = item.Titles.OrderByDescending(x => x.Type.Distinct().Count()).ToList();

                    for (int i = 0; i < Math.Max(yearsSorted.Count, item.Titles.Count); i++)
                    {
                        string? year = null;
                        string? title = null;
                        ServiceType? service = null;
                        if (i == 0)
                        {
                            service = item.Services;

                            if (yearsSorted.Count == 1)
                            {
                                year = yearsSorted.First().Year.ToString();
                            }
                            else
                            {
                                year = yearsSorted.Count > i
                                        ? $"{i + 1}. {yearsSorted.ElementAt(i).Year} ({string.Join(", ", yearsSorted.ElementAt(i).Type.Distinct())})"
                                        : null;
                            }

                            if (titlesSorted.Count == 1)
                            {
                                title = titlesSorted.First().Title;
                            }
                            else
                            {
                                title = titlesSorted.Count > i
                                         ? $"{i + 1}. {titlesSorted.ElementAt(i).Title} ({string.Join(", ", titlesSorted.ElementAt(i).Type.Distinct())})"
                                         : null;
                            }
                        }
                        else
                        {
                            year = yearsSorted.Count > i
                                    ? $"{i + 1}. {yearsSorted.ElementAt(i).Year} ({string.Join(", ", yearsSorted.ElementAt(i).Type.Distinct())})"
                                    : null;

                            title = titlesSorted.Count > i
                                     ? $"{i + 1}. {titlesSorted.ElementAt(i).Title} ({string.Join(", ", titlesSorted.ElementAt(i).Type.Distinct())})"
                                     : null;
                        }

                        resultEntities.Add(new ResultEntity(year, title, service));
                    }
                }
                else
                {
                    resultEntities.Add(new ResultEntity(item.Years.ToString(), item.Titles.ToString(), item.Services));
                }
            }
            else
            {
                var yearsSorted = item.Years.OrderByDescending(x => x.Type.Distinct().Count()).ToList();
                var titlesSorted = item.Titles.OrderByDescending(x => x.Type.Distinct().Count()).ToList();

                resultEntities.Add(new ResultEntity(yearsSorted.First().Year.ToString(), titlesSorted.First().Title, null));
            }
        }

        return resultEntities;
    }
}

public record ResultEntity(string? Year, string? Title, ServiceType? Services);