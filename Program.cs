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

    public void RequestArtist(string name)
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

        foreach (var result in results.Where(x => x.Albums != null))
        {
            foreach (var album in result.Albums!.ToList())
            {
                var nameNormalized = Normalize(album.Title);

                if (results.SelectMany(x => x.Lives ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Albums!.Remove(album);
                    result.Lives ??= new List<Entity>();
                    result.Lives.Add(album);
                    continue;
                }

                if (results.SelectMany(x => x.Compilations ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Albums!.Remove(album);
                    result.Compilations ??= new List<Entity>();
                    result.Compilations.Add(album);
                    continue;
                }
            }
        }

        foreach (var result in results.Where(x => x.EPs != null))
        {
            foreach (var ep in result.EPs!.ToList())
            {
                var nameNormalized = Normalize(ep.Title);

                if (results.SelectMany(x => x.Lives ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.EPs!.Remove(ep);
                    result.Lives ??= new List<Entity>();
                    result.Lives.Add(ep);
                    continue;
                }

                if (results.SelectMany(x => x.Compilations ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.EPs!.Remove(ep);
                    result.Compilations ??= new List<Entity>();
                    result.Compilations.Add(ep);
                    continue;
                }
            }
        }

        foreach (var result in results.Where(x => x.Others != null))
        {
            foreach (var other in result.Others!.ToList())
            {
                var nameNormalized = Normalize(other.Title);

                if (results.SelectMany(x => x.Albums ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others!.Remove(other);
                    result.Albums ??= new List<Entity>();
                    result.Albums.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.EPs ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others!.Remove(other);
                    result.EPs ??= new List<Entity>();
                    result.EPs.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Lives ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others!.Remove(other);
                    result.Lives ??= new List<Entity>();
                    result.Lives.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Compilations ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others!.Remove(other);
                    result.Compilations ??= new List<Entity>();
                    result.Compilations.Add(other);
                    continue;
                }

                if (results.SelectMany(x => x.Singles ?? Enumerable.Empty<Entity>()).Any(x => Normalize(x.Title) == nameNormalized))
                {
                    result.Others!.Remove(other);
                    result.Singles ??= new List<Entity>();
                    result.Singles.Add(other);
                    continue;
                }
            }
        }

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Albums ?? Enumerable.Empty<Entity>())))
            .WithTitle("Albums")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.EPs ?? Enumerable.Empty<Entity>())))
            .WithTitle("EPs")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Singles ?? Enumerable.Empty<Entity>()), removeBrackets: false))
            .WithTitle("Singles")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Lives ?? Enumerable.Empty<Entity>())))
            .WithTitle("Lives")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Compilations ?? Enumerable.Empty<Entity>())))
            .WithTitle("Compilations")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Others ?? Enumerable.Empty<Entity>())))
            .WithTitle("Others")
            .ExportAndWriteLine();
    }

    private List<ResultEntity> GroupResults(IEnumerable<Entity> enumerable, bool removeBrackets = true)
    {
        var groupedByName = enumerable.GroupBy(x => Normalize(x.Title, removeBrackets));

        return groupedByName.Select(group =>
        {
            var result = new ResultEntity();

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
    }

    private static string Normalize(string x, bool removeBrackets = true)
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

        if (removeBrackets)
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