﻿using System.Text;

namespace ArtistInfoSearcher;

public class SearchResult
{
    public List<Entity>? Albums { get; set; }
    public List<Entity>? EPs { get; set; }
    public List<Entity>? Singles { get; set; }
    public List<Entity>? Compilations { get; set; }
    public List<Entity>? Lives { get; set; }
    public List<Entity>? Others { get; set; }

    public List<Entity> AllResults
    {
        get
        {
            List<Entity> result = new List<Entity>();
            if (Albums != null) result.AddRange(Albums);
            if (EPs != null) result.AddRange(EPs);
            if (Singles != null) result.AddRange(Singles);
            if (Compilations != null) result.AddRange(Compilations);
            if (Lives != null) result.AddRange(Lives);
            if (Others != null) result.AddRange(Others);

            return result;
        }
    }
}

public class Result
{
    public List<ResultEntity>? Albums { get; set; }
    public List<ResultEntity>? EPs { get; set; }
    public List<ResultEntity>? Singles { get; set; }
}

public class ResultEntity
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