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

        Parallel.ForEach(dataServices, x => results.Add(x.GetSearchResultAsync(name).Result));

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

    private List<Entity> GroupResults(IEnumerable<Entity> enumerable)
    {
        var groupedByName = enumerable.GroupBy(x => x.Title + x.Year);

        return groupedByName.Select(x =>
        {
            var result = new Entity(x.First().Title, x.First().Year);

            x.ToList().ForEach(x => result.ServiceType = result.ServiceType.HasValue 
                                                            ? result.ServiceType | x.ServiceType 
                                                            : x.ServiceType);
            return result;
        })
            .OrderByDescending(x => x.Year)
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