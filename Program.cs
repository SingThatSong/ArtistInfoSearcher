using ArtistInfoSearcher.DataServices;
using CommandDotNet;
using ConsoleTableExt;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
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
        var dataServices = GetAllDataServices();

        var results = new ConcurrentBag<SearchResult>();

        Parallel.ForEach(dataServices, x =>
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {x.GetType().Name} Started");
            try
            {
                var result = x.GetSearchResultAsync(name).Result;
                if (result.AllResults.Count == 0)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {x.GetType().Name} Finished without results");
                }
                else
                {
                    results.Add(result);
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {x.GetType().Name} Finished");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {x.GetType().Name} Failed");
                Console.WriteLine(ex.Message);
            }
        });

        foreach (var result in results)
        {
            foreach (var album in result.Albums.ToList())
            {
                var nameNormalized = Normalize(album.Title);

                if (results.SelectMany(x => x.Lives).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Albums.Remove(album);
                    result.Lives.Add(album);
                    continue;
                }

                if (results.SelectMany(x => x.Compilations).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Albums.Remove(album);
                    result.Compilations.Add(album);
                    continue;
                }
            }
        }

        foreach (var result in results)
        {
            foreach (var ep in result.EPs.ToList())
            {
                var nameNormalized = Normalize(ep.Title);

                if (results.SelectMany(x => x.Lives).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.EPs.Remove(ep);
                    result.Lives.Add(ep);
                    continue;
                }

                if (results.SelectMany(x => x.Compilations).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.EPs.Remove(ep);
                    result.Compilations.Add(ep);
                    continue;
                }
            }
        }

        foreach (var result in results)
        {
            foreach (var other in result.Others.ToList())
            {
                var nameNormalized = Normalize(other.Title);

                if (results.SelectMany(x => x.Albums).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others.Remove(other);
                    result.Albums.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.EPs).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others.Remove(other);
                    result.EPs.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Lives).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others.Remove(other);
                    result.Lives.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Compilations).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others.Remove(other);
                    result.Compilations.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Singles).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others.Remove(other);
                    result.Singles.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Appearances).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others.Remove(other);
                    result.Appearances.Add(other);
                    continue;
                }
            }
        }

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Albums), verbose: verbose))
            .WithTitle("Albums")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.EPs), verbose: verbose))
            .WithTitle("EPs")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Singles), includeBrackets: true, verbose: verbose))
            .WithTitle("Singles")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Lives), verbose: verbose))
            .WithTitle("Lives")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Compilations), verbose: verbose))
            .WithTitle("Compilations")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Appearances), verbose: verbose))
            .WithTitle("Appearances")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Others), verbose: verbose))
            .WithTitle("Others")
            .ExportAndWriteLine();
    }

    private List<ResultEntity> GroupResults(IEnumerable<Album> enumerable, bool includeBrackets = false, bool verbose = false)
    {
        var groupedByName = enumerable.GroupBy(x => Normalize(x.Title, includeBrackets));

        var entities = groupedByName.Select(group =>
        {
            var result = new GroupedEntity();

            foreach (var entry in group)
            {
                var existingYear = result.Years.FirstOrDefault(x => x.Year == entry.Year);

                if (existingYear != default)
                {
                    existingYear.Type.Add(entry.ServiceType!.Value.ToString());
                }
                else
                {
                    result.Years.Add((entry.Year!.Value, new List<string>() { entry.ServiceType!.Value.ToString() }));
                }

                var existingTitle = result.Titles.FirstOrDefault(x => x.Title == entry.Title);

                if (existingTitle != default)
                {
                    existingTitle.Type.Add(entry.ServiceType!.Value.ToString());
                }
                else
                {
                    result.Titles.Add((entry.Title, new List<string>() { entry.ServiceType!.Value.ToString() }));
                }

                result.Services = result.Services.HasValue
                                    ? result.Services | entry.ServiceType
                                    : entry.ServiceType;
            }

            return result;
        })
            .OrderByDescending(x => x.Years.Min(y => y.Year))
            .ToList();

        List<ResultEntity> resultEntities = new List<ResultEntity>();
        foreach (var item in entities)
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
                                year = titlesSorted.First().Title;
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

    private static string Normalize(string x, bool includeBrackets = false)
    {
        var result = x.Replace(" ", "")
                      .Replace(",", "")
                      .Replace(":", "")
                      .Replace(".", "")
                      .Replace("'", "")
                      .Replace("!", "")
                      .Replace("?", "")
                      .Replace('[', '(')
                      .ToLowerInvariant();

        if (!includeBrackets)
        {
            var index = result.IndexOf('(');

            if (index > 0)
            {
                return result.Substring(0, index);
            }
            else
            {
                return result;
            }
        }
        else
        {
            return result;
        }
    }

    private List<DataService> GetAllDataServices()
    {
        var dataServiceType = typeof(DataService).GetTypeInfo();
        var types = GetType().GetTypeInfo().Assembly.DefinedTypes;
        var dataServices = types.Where(x => x.IsClass && dataServiceType.IsAssignableFrom(x) && !x.IsAbstract).ToList();
        return dataServices.Select(x => (DataService)Activator.CreateInstance(x)!).ToList();
    }
}

public record ResultEntity(string? Year, string? Title, ServiceType? Services);