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
                results.Add(x.GetSearchResultAsync(name).Result);
            }
            catch
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {x.GetType().Name} Failed");
            }
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {x.GetType().Name} Finished");
        });

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Albums ?? Enumerable.Empty<Entity>())))
            .WithTitle("Albums")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.EPs ?? Enumerable.Empty<Entity>())))
            .WithTitle("EPs")
            .ExportAndWriteLine();

        ConsoleTableBuilder
            .From(GroupResults(results.SelectMany(x => x.Singles ?? Enumerable.Empty<Entity>())))
            .WithTitle("Singles")
            .ExportAndWriteLine();
    }

    private List<ResultEntity> GroupResults(IEnumerable<Entity> enumerable)
    {
        var groupedByName = enumerable.GroupBy(x =>
        {
            var result = x.Title.Replace(" ", "")
                                .Replace(",", "")
                                .Replace(":", "")
                                .Replace(".", "")
                                .Replace("!", "")
                                .Replace("?", "")
                                .ToLowerInvariant();

            var index = result.IndexOfAny(new[] { '[', '(' });

            if (index > 0)
            {
                return result.Substring(0, index).Trim('.', '!', '?', ' ');
            }
            else
            {
                return result;
            }
        });

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

    private List<DataService> GetAllDataServices()
    {
        var dataServiceType = typeof(DataService).GetTypeInfo();
        var types = GetType().GetTypeInfo().Assembly.DefinedTypes;
        var dataServices = types.Where(x => x.IsClass && dataServiceType.IsAssignableFrom(x) && !x.IsAbstract).ToList();
        return dataServices.Select(x => (DataService)Activator.CreateInstance(x)!).ToList();
    }
}