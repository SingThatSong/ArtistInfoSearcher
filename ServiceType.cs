namespace ArtistInfoSearcher;

[Flags]
public enum ServiceType
{
    Itunes         = 1 << 0,
    Musicbrainz    = 1 << 1,
    YandexMusic    = 1 << 2,
    Secondhandsongs = 1 << 3,
    MusicStory     = 1 << 4,
    TheAudiodb     = 1 << 5,
    Wikidata       = 1 << 6,
    Amazon         = 1 << 7,
    Spotify        = 1 << 8,
}