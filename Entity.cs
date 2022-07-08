using ArtistInfoSearcher;

public class Entity
{
    public Entity(string title, int year)
    {
        Title = title;
        Year = year;
    }

    public int Year { get; set; }
    public string Title { get; set; }
    public ServiceType ServiceType { get; set; }
}