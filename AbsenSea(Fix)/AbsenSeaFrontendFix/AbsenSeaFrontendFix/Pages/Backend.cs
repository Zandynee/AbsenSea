using Supabase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetEnv;

namespace AbsenSeaFrontendFix.Pages
{
    internal class Backend
    {
        public Client SupabaseClient { get; private set; }

         Backend() { } // Private constructor for factory pattern

        public static async Task<Backend> CreateAsync()
        {
            
           
            DotNetEnv.Env.Load(); // Load once, here
            DotNetEnv.Env.TraversePath().Load();
            var backend = new Backend();
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
                throw new InvalidOperationException("SUPABASE_URL or SUPABASE_KEY environment variable is missing.");
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };
            backend.SupabaseClient = new Client(url, key, options);
            await backend.SupabaseClient.InitializeAsync();
            return backend;
        }

        // Add your data access methods here
        public async Task<List<CrewCheck>> GetCrewChecksWithMembers()
        {
            var result = await SupabaseClient
                .From<CrewCheck>()
                .Select("*, CREW_MEMBER(*)")
                .Get();

            return result.Models;
        }

        public async Task<CrewCheck> GetCrewCheckById(long checkId)
        {
            var result = await SupabaseClient
                .From<CrewCheck>()
                .Select("*, CREW_MEMBER(*)")
                .Where(x => x.CHECK_ID == checkId)
                .Single();

            return result;
        }

        public async Task<CrewCheck> InsertCrewCheck(CrewCheck crewCheck)
        {
            var result = await SupabaseClient
                .From<CrewCheck>()
                .Insert(crewCheck);

            return result.Models.FirstOrDefault();
        }
        public async Task<bool> TestConnection()
        {
            try
            {
                var result = await SupabaseClient
                    .From<CrewCheck>()
                    .Select("*")
                    .Limit(1)
                    .Get();

                Console.WriteLine($"Connection successful! Found {result.Models.Count} record(s)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                return false;
            }
        }
        // Add more methods as needed
    }
}