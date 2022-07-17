namespace ArtistInfoSearcher;

[Flags]
public enum ServiceType
{
    Itunes      = 1 << 0,
    Musicbrainz = 1 << 1
}