from ytmusicapi import YTMusic
import sys
import json

sys.stdout.reconfigure(encoding='utf-8')

artist = sys.argv[1]

ytmusic = YTMusic()
search_results = ytmusic.search(artist, filter='artists', limit=1)

if len(search_results) > 0:
    artistFound = next (c for c in search_results if c['artist'] == artist)

    artistData = ytmusic.get_artist(artistFound['browseId'])
    
    songs_results = ytmusic.get_playlist(artistData['songs']['browseId'][2:], 1000)
    print(json.dumps(songs_results))
    print()

    if artistData['albums']['browseId'] is not None:
        albums_results = ytmusic.get_artist_albums(artistData['albums']['browseId'],
                                                  artistData['albums']['params'])
        print(json.dumps(albums_results))
        print()
            
        singles_results = ytmusic.get_artist_albums(artistData['singles']['browseId'],
                                                  artistData['singles']['params'])

        print(json.dumps(singles_results))
        print()

        for item in albums_results:
            album_results = ytmusic.get_album(item['browseId'])
            print(json.dumps(album_results))
            print()

        for item in singles_results:
            album_results = ytmusic.get_album(item['browseId'])
            print(json.dumps(album_results))
            print()

    else:
        print(json.dumps(artistData))
        print()

        for item in artistData['albums']['results']:
            album_results = ytmusic.get_album(item['browseId'])
            print(json.dumps(album_results))
            print()

        for item in artistData['singles']['results']:
            album_results = ytmusic.get_album(item['browseId'])
            print(json.dumps(album_results))
            print()