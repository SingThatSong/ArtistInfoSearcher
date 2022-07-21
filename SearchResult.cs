using System.Text;

namespace ArtistInfoSearcher;

public class SearchResult
{
    public List<Album> Albums { get; set; } = new List<Album>();
    public List<Album> EPs { get; set; } = new List<Album>();
    public List<Album> Singles { get; set; } = new List<Album>();
    public List<Album> Compilations { get; set; } = new List<Album>();
    public List<Album> Lives { get; set; } = new List<Album>();
    public List<Album> Appearances { get; set; } = new List<Album>();
    public List<Album> Others { get; set; } = new List<Album>();

    public List<Album> AllResults
    {
        get
        {
            List<Album> result = new List<Album>();
            result.AddRange(Albums);
            result.AddRange(EPs);
            result.AddRange(Singles);
            result.AddRange(Compilations);
            result.AddRange(Lives);
            result.AddRange(Others);

            return result;
        }
    }
}

public class Result
{
    public List<GroupedEntity>? Albums { get; set; }
    public List<GroupedEntity>? EPs { get; set; }
    public List<GroupedEntity>? Singles { get; set; }
}

public class GroupedEntity
{
    public Years Years { get; set; } = new Years();
    public Titles Titles { get; set; } = new Titles();
    public ServiceType? Services { get; set; }
}

public class Years : List<(int Year, List<string> Type)>
{
    public override string ToString()
    {
        var list = this;

        if (list.Count == 1) return list[0].Year.ToString();

        var sb = new StringBuilder();

        foreach (var item in list.OrderByDescending(x => x.Type.Count))
        {
            if (sb.Length != 0)
            {
                sb.Append("  ");
            }

            sb.Append($"{item.Year} ({string.Join(", ", item.Type.Distinct())})");
        }

        return sb.ToString();
    }
}

public class Titles : List<(string Title, List<string> Type)>
{
    public override string ToString()
    {
        var list = this;

        if (list.Count == 1) return list[0].Title;

        var sb = new StringBuilder();

        foreach (var item in list.OrderByDescending(x => x.Type.Count))
        {
            if (sb.Length != 0)
            {
                sb.Append("  ");
            }

            sb.Append($"{item.Title} ({string.Join(", ", item.Type.Distinct())})");
        }

        return sb.ToString();
    }
}