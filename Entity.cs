namespace ArtistInfoSearcher;

public class Album
{
    public Album(string title, int? year = null)
    {
        Title = title;
        Year = year;
    }

    public int? Year { get; set; }
    public string Title { get; set; }
    public ServiceType? ServiceType { get; set; }

    public List<Song> Songs { get; set; } = new List<Song>();
}

public record Song(int Number, string Title, TimeSpan Duration);