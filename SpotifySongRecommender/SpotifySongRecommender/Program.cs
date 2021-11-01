using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifySongRecommender
{
    class Program
    {
        /*
         * First, read the code and understand what it does.
         * Second, change the search words in GeneratePlaylistFromSeedSong to get song recommendations 
         * based on a song you like. You can look them up online and listen to see if they match your tastes.
         * Third, change the seedSongFeatures and their numbers to tailor the search to your tast.
         * Explore the fields of the TrackAudioFeatures and filter by your own combinations.
         * Example: 
         * A higher tempo describes a fast paced song, while a lower tempo is a slower song.
         * Have fun!
         */

        private static SpotifyClient SpotifyClient;

        static void Main(string[] args)
        {
            // your spotify app's identification information
            // in the industry, secrets are not stored unencrypted in plain sight, but this is not the scope of this 
            // exercise so for convenience we'll just use them as unecnrypted strings
            string spotifyAppClientID = "[PLACE_YOUR_SPOTIFYAPP_CLIENTID_HERE]";
            string spotifyAppClientSecret = "[PLACE_YOUR_SPOTIFYAPP_CLIENTSECRET_HERE]";

            // we are using this spotify api written for .NET
            // https://github.com/JohnnyCrazy/SpotifyAPI-NET
            // it's a wrapper around the Spotify API so it exposes a client which will make our code a lot 
            // easier to read and more oragnized
            SpotifyClientConfig config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(spotifyAppClientID, spotifyAppClientSecret));
            SpotifyClient = new SpotifyClient(config);

            Console.WriteLine("Spotify API Song Recommender\n");

            GeneratePlaylistFromSeedSong("river flows in you yiruma");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void GeneratePlaylistFromSeedSong(string songNameAndArtistSearchString)
        {
            // in pretty much any data base, its entries (be it songs, books, groceries etc.)
            // are identified by a unique code. So we need to get that code for the song we are searching by.
            string seedSongID = GetSongIDByNameAndArtist(songNameAndArtistSearchString);

            // once we have our song's ID, we can look at its track audio features.
            // these are pieces of information the describe a song such as tempo, musical key, etc.
            // we will be using these descriptions to find more songs that are very similar to this one
            // the degree of similarity will be determined by how strict you set the numbers 
            // when querying for more songs
            TrackAudioFeatures seedSongFeatures = GetSongFeaturesByID(seedSongID);

            // here we are building the range of values to determine the similarity of the new
            // songs with the one we provided. the smaller the numbers, the more similar songs you will find
            // the larger the numbers, the more room for variation you leave in the query results
            int maxLoudness = -10;
            float minTempo = seedSongFeatures.Tempo - 70;
            float maxTempo = seedSongFeatures.Tempo + 10;
            float maxEnergy = (float)(seedSongFeatures.Energy + 0.1);
            float minAcousticness = (float)(seedSongFeatures.Acousticness - 0.1);
            float maxAcousticness = (float)(seedSongFeatures.Acousticness + 0.1);
            float minValence = (float)(seedSongFeatures.Valence - 0.1);
            float maxValence = (float)(seedSongFeatures.Valence + 0.1);

            // now we make the query...
            List<SimpleTrack> recommendedSongs = Get10RecommendedSongs(
                seedSongID,
                seedSongFeatures,
                maxLoudness,
                minTempo,
                maxTempo,
                maxEnergy);

            // ...and display the results in the console
            Console.WriteLine(
                string.Join(
                    "\n", 
                    recommendedSongs.Select(st => $"{st.Id}, {st.Name} {st.Artists.First().Name}")));
        }

        private static string GetSongIDByNameAndArtist(string songNameAndArtist)
        {
            string trackID = "N/A";
            SearchRequest sr = new SearchRequest(SearchRequest.Types.Track, songNameAndArtist);
            SearchResponse response = SpotifyClient.Search.Item(sr).Result;
            var firstSong = response.Tracks.Items.FirstOrDefault();
            if (firstSong != default(FullTrack))
            {
                trackID = firstSong.Id;
            }
            Task.WaitAll();
            Console.WriteLine($"{songNameAndArtist} has track ID {trackID}\n");
            return trackID;
        }

        private static TrackAudioFeatures GetSongFeaturesByID(string trackID)
        {
            TrackAudioFeatures features = SpotifyClient.Tracks.GetAudioFeatures(trackID).Result;
            Task.WaitAll();
            Console.WriteLine($"Features for song ID {trackID}\n" +
                $"Acousticness = {features.Acousticness}\n" +
                $"Danceability = {features.Danceability}\n" +
                $"Energy = {features.Energy}\n" +
                $"Instrumentalness = {features.Instrumentalness}\n" +
                $"Key = {features.Key}\n" +
                $"Liveness = {features.Liveness}\n" +
                $"Loudness = {features.Loudness}\n" +
                $"Mode = {features.Mode}\n" +
                $"Speechiness = {features.Speechiness}\n" +
                $"Tempo = {features.Tempo}\n" +
                $"TimeSignature = {features.TimeSignature}\n" +
                $"Valence = {features.Valence}\n");
            return features;
        }

        private static List<SimpleTrack> Get10RecommendedSongs(
            string seedSongID,
            TrackAudioFeatures seedSongFeatures,
            float maxLoudness,
            float minTempo,
            float maxTempo,
            float maxEnergy)
        {
            RecommendationsRequest recreq = new RecommendationsRequest();
            recreq.SeedTracks.Add(seedSongID);
            recreq.Target.Add("acousticness", seedSongFeatures.Acousticness.ToString());
            recreq.Target.Add("key", seedSongFeatures.Key.ToString());
            recreq.Target.Add("loudness", seedSongFeatures.Loudness.ToString());
            recreq.Target.Add("mode", seedSongFeatures.Mode.ToString());
            recreq.Target.Add("tempo", seedSongFeatures.Tempo.ToString());
            recreq.Target.Add("time_signature", seedSongFeatures.TimeSignature.ToString());
            recreq.Target.Add("valence", seedSongFeatures.Valence.ToString());
            recreq.Min.Add("tempo", minTempo.ToString());
            recreq.Max.Add("tempo", maxTempo.ToString());
            recreq.Max.Add("loudness", maxLoudness.ToString());
            recreq.Max.Add("energy", maxEnergy.ToString());
            recreq.Limit = 10;
            RecommendationsResponse recresp = SpotifyClient.Browse.GetRecommendations(recreq).Result;
            Task.WaitAll();

            List<SimpleTrack> results = recresp.Tracks;
            Console.WriteLine($"Found {results.Count} recommended songs");

            return results;
        }
    }
}
