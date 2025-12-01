using Supabase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetEnv;
using System.Linq;
using AbsenSeaFrontendFix.Pages.Database;

namespace AbsenSeaFrontendFix.Pages
{
    internal class Backend
    {
        public Client SupabaseClient { get; private set; }

        private Backend() { } // Private constructor for factory pattern

        public static async Task<Backend> CreateAsync()
        {
            // Get the base directory of the application
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var envPath = System.IO.Path.Combine(baseDirectory, ".env");
            
            // Load .env file from the executable's directory
            if (System.IO.File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }
            else
            {
                // Fallback: try to load from current directory
                DotNetEnv.Env.TraversePath().Load();
            }

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

        // Get all crew checks with member information (manual join)
        public async Task<List<CrewCheck>> GetCrewChecksWithMembers()
        {
            // Fetch checks and crew separately to avoid FK ambiguity
            var checksResult = await SupabaseClient
                .From<CrewCheck>()
                .Select("*")
                .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var crewResult = await SupabaseClient
                .From<CrewMember>()
                .Select("*")
                .Get();

            // Manually populate crew member references
            var crewLookup = crewResult.Models.ToDictionary(c => c.CREW_ID, c => c);

            foreach (var check in checksResult.Models)
            {
                if (check.CREW_ID.HasValue && crewLookup.ContainsKey(check.CREW_ID.Value))
                {
                    check.CREW_MEMBER = crewLookup[check.CREW_ID.Value];
                }
            }

            return checksResult.Models;
        }

        // Get crew checks for a specific date
        public async Task<List<CrewCheck>> GetCrewChecksByDate(DateTime date)
        {
            var checksResult = await SupabaseClient
                .From<CrewCheck>()
                .Select("*")
                .Where(x => x.CHECK_DATE >= date && x.CHECK_DATE < date.AddDays(1))
                .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            // Get crew members for lookup
            var crewResult = await SupabaseClient
                .From<CrewMember>()
                .Select("*")
                .Get();

            var crewLookup = crewResult.Models.ToDictionary(c => c.CREW_ID, c => c);

            foreach (var check in checksResult.Models)
            {
                if (check.CREW_ID.HasValue && crewLookup.ContainsKey(check.CREW_ID.Value))
                {
                    check.CREW_MEMBER = crewLookup[check.CREW_ID.Value];
                }
            }

            return checksResult.Models;
        }

        // Get crew check by ID
        public async Task<CrewCheck> GetCrewCheckById(long checkId)
        {
            var checkResult = await SupabaseClient
                .From<CrewCheck>()
                .Select("*")
                .Where(x => x.CHECK_ID == checkId)
                .Single();

            // Manually load crew member if exists
            if (checkResult.CREW_ID.HasValue)
            {
                try
                {
                    var crewResult = await SupabaseClient
                        .From<CrewMember>()
                        .Select("*")
                        .Where(c => c.CREW_ID == checkResult.CREW_ID.Value)
                        .Single();

                    checkResult.CREW_MEMBER = crewResult;
                }
                catch
                {
                    // Crew member not found, continue without it
                }
            }

            return checkResult;
        }

        // Insert new crew check
        public async Task<CrewCheck> InsertCrewCheck(CrewCheck crewCheck)
        {
            var result = await SupabaseClient
                .From<CrewCheck>()
                .Insert(crewCheck);

            return result.Models.FirstOrDefault();
        }

        // Get all crew members
        public async Task<List<CrewMember>> GetAllCrewMembers()
        {
            var result = await SupabaseClient
                .From<CrewMember>()
                .Select("*")
                .Get();

            return result.Models;
        }

        // Get crew members by ship
        public async Task<List<CrewMember>> GetCrewMembersByShip(long shipId)
        {
            var result = await SupabaseClient
                .From<CrewMember>()
                .Select("*")
                .Where(x => x.ship_id == shipId)
                .Get();

            return result.Models;
        }

        // Get dashboard statistics for today
        public async Task<DashboardStats> GetTodayStats()
        {
            var today = DateTime.Today;

            // Get total crew count
            var crewResult = await SupabaseClient
                .From<CrewMember>()
                .Select("*")
                .Get();

            // Get recent checks and filter locally
            var checksResult = await SupabaseClient
                .From<CrewCheck>()
                .Select("*")
                .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(100)
                .Get();
            
            var todayChecks = checksResult.Models
                .Where(c => c.CHECK_DATE.HasValue && c.CHECK_DATE.Value.Date == today)
                .ToList();

            var totalCrew = crewResult.Models.Count;
            var checkedIn = todayChecks.Count;
            var compliant = todayChecks.Count(c => c.HELMET == true && c.VEST == true);
            var nonCompliant = todayChecks.Count(c => !(c.HELMET == true && c.VEST == true));
            var withHelmet = todayChecks.Count(c => c.HELMET == true);
            var withVest = todayChecks.Count(c => c.VEST == true);

            return new DashboardStats
            {
                TotalCrew = totalCrew,
                CheckedInToday = checkedIn,
                Compliant = compliant,
                NonCompliant = nonCompliant,
                AttendanceRate = totalCrew > 0 ? (checkedIn * 100.0 / totalCrew) : 0,
                ComplianceRate = checkedIn > 0 ? (compliant * 100.0 / checkedIn) : 0,
                HelmetRate = checkedIn > 0 ? (withHelmet * 100.0 / checkedIn) : 0,
                VestRate = checkedIn > 0 ? (withVest * 100.0 / checkedIn) : 0
            };
        }

        // Test database connection
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

        // Get ship account by ID
        public async Task<ShipAccount> GetShipById(long shipId)
        {
            var result = await SupabaseClient
                .From<ShipAccount>()
                .Select("*")
                .Where(x => x.SHIP_ID == shipId)
                .Single();

            return result;
        }

        // Get crew member by ID
        public async Task<CrewMember> GetCrewMemberById(long crewId)
        {
            var result = await SupabaseClient
                .From<CrewMember>()
                .Select("*")
                .Where(x => x.CREW_ID == crewId)
                .Single();

            return result;
        }
    }

    // Dashboard statistics model
    public class DashboardStats
    {
        public int TotalCrew { get; set; }
        public int CheckedInToday { get; set; }
        public int Compliant { get; set; }
        public int NonCompliant { get; set; }
        public double AttendanceRate { get; set; }
        public double ComplianceRate { get; set; }
        public double HelmetRate { get; set; }
        public double VestRate { get; set; }
    }
}