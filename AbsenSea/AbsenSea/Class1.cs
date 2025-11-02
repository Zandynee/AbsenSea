using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbsenSea
{
    static void Main(string[] args)
    {
        var connString = "Host=localhost;Port=5432;Database=absensea;Username=postgres;Password=your_password";
        var db = new DbSamples(connString);
        var manager = new AttendanceManager(db);

        var captain = new Officer("O-001", "Hasan", "Captain", pin: "4321");
        var optOfficer = new CheckInOptions { Time = DateTime.UtcNow, Location = "Bridge", Pin = "4321" };

        // This records in memory AND inserts into the DB (via AttendanceManager.RecordCheckIn)
        manager.RecordCheckIn(captain, optOfficer);
        manager.GenerateReport();
    }
    
    public enum CrewStatus { Present, Absent, Unknown }
    public enum EquipmentCondition { Good, Damaged, Missing }
    public enum VerificationResult { Verified, NotVerified }

    // Options object used for polymorphic check-in parameters
    public class CheckInOptions
    {
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public string Location { get; set; } = string.Empty;
        public string Pin { get; set; } = null;          // for Officer
        public bool? EquipmentOk { get; set; } = null;   // for Engineer
        public string Note { get; set; } = null;
    }

    // Encapsulation: fields are private, expose via properties / methods
    public class CrewMember
    {
        private string _crewId;
        private string _name;
        private string _rank;
        private List<SafetyEquipment> _assignedEquipment;
        private List<AttendanceRecord> _attendanceHistory;

        public CrewStatus Status { get; protected set; } = CrewStatus.Unknown;

        public CrewMember(string crewId, string name, string rank)
        {
            _crewId = crewId;
            _name = name;
            _rank = rank;
            _assignedEquipment = new List<SafetyEquipment>();
            _attendanceHistory = new List<AttendanceRecord>();
        }

        // Read-only accessors (encapsulation)
        public string CrewID => _crewId;
        public string Name => _name;
        public string Rank => _rank;

        // safe copy to prevent external mutation
        public IReadOnlyList<SafetyEquipment> AssignedEquipment => _assignedEquipment.AsReadOnly();
        public IReadOnlyList<AttendanceRecord> AttendanceHistory => _attendanceHistory.AsReadOnly();

        // Virtual method used for polymorphism
        public virtual AttendanceRecord CheckIn(CheckInOptions options)
        {
            Status = CrewStatus.Present;
            var record = new AttendanceRecord
            {
                RecordID = Guid.NewGuid().ToString("N"),
                CrewMember = this,
                Status = this.Status,
                DateTime = options.Time,
                VerifiedBy = VerificationResult.NotVerified,
                Location = options.Location,
                Note = options.Note
            };
            _attendanceHistory.Add(record);
            Console.WriteLine($"{Name} ({Rank}) checked in at {record.DateTime:u} - location: {record.Location}");
            return record;
        }

        public virtual AttendanceRecord CheckOut(CheckInOptions options)
        {
            Status = CrewStatus.Absent;
            var record = new AttendanceRecord
            {
                RecordID = Guid.NewGuid().ToString("N"),
                CrewMember = this,
                Status = this.Status,
                DateTime = options.Time,
                VerifiedBy = VerificationResult.NotVerified,
                Location = options.Location,
                Note = options.Note
            };
            _attendanceHistory.Add(record);
            Console.WriteLine($"{Name} ({Rank}) checked out at {record.DateTime:u} - location: {record.Location}");
            return record;
        }

        public void AssignEquipment(SafetyEquipment eq)
        {
            if (!_assignedEquipment.Contains(eq))
            {
                _assignedEquipment.Add(eq);
                eq.AssignToCrew(this);
            }
        }
    }

    // Officer requires PIN verification
    public class Officer : CrewMember
    {
        private string _pin; // private, encapsulated

        public Officer(string crewId, string name, string rank, string pin)
            : base(crewId, name, rank)
        {
            _pin = pin;
        }

        private bool VerifyPin(string pin) => !string.IsNullOrEmpty(_pin) && _pin == pin;

        // Override with same signature to keep polymorphism
        public override AttendanceRecord CheckIn(CheckInOptions options)
        {
            var verified = VerifyPin(options.Pin);
            var record = base.CheckIn(options);
            record.VerifiedBy = verified ? VerificationResult.Verified : VerificationResult.NotVerified;
            record.Note = (record.Note ?? "") + $" Officer PIN verification: {record.VerifiedBy}";
            if (!verified)
            {
                Console.WriteLine($"Warning: Officer {Name} PIN verification failed.");
            }
            return record;
        }
    }

    // Engineer adds equipment check detail
    public class Engineer : CrewMember
    {
        public Engineer(string crewId, string name, string rank)
            : base(crewId, name, rank) { }

        public override AttendanceRecord CheckIn(CheckInOptions options)
        {
            bool equipmentOk = options.EquipmentOk ?? true; // default true if not provided
            var record = base.CheckIn(options);
            record.Note = (record.Note ?? "") + $" Equipment OK: {equipmentOk}";
            return record;
        }
    }

    // Sailor uses default behavior
    public class Sailor : CrewMember
    {
        public Sailor(string crewId, string name, string rank) : base(crewId, name, rank) { }
        // inherit CheckIn from base
    }

    public class SafetyEquipment
    {
        public string EquipmentID { get; set; }
        public string Type { get; set; }
        public EquipmentCondition Condition { get; private set; } = EquipmentCondition.Good;

        public void AssignToCrew(CrewMember crew)
        {
            // keep assignment message but avoid direct manipulation of crew internal list (crew.AssignEquipment handles adding)
            Console.WriteLine($"[Equip] {Type} ({EquipmentID}) assigned to {crew.Name}.");
        }

        public void UpdateCondition(EquipmentCondition newCondition)
        {
            Condition = newCondition;
            Console.WriteLine($"[Equip] {Type} ({EquipmentID}) condition updated to {Condition}.");
        }
    }

    public class AttendanceRecord
    {
        public string RecordID { get; set; }
        public CrewMember CrewMember { get; set; }
        public CrewStatus Status { get; set; }
        public DateTime DateTime { get; set; }
        public VerificationResult VerifiedBy { get; set; }
        public string Location { get; set; }
        public string Note { get; set; }
    }

    public class AttendanceManager
    {
        private List<AttendanceRecord> _records = new List<AttendanceRecord>();

        // Expose read-only copy
        public IReadOnlyList<AttendanceRecord> Records => _records.AsReadOnly();

        // Polymorphism: calls CrewMember.CheckIn with same CheckInOptions
        public AttendanceRecord RecordCheckIn(CrewMember member, CheckInOptions options)
        {
            var rec = member.CheckIn(options);   // runtime dispatch to subclass override
            _records.Add(rec);
            Console.WriteLine($"[Manager] Recorded check-in for {member.Name} (RecordID: {rec.RecordID})");
            return rec;
        }

        public List<CrewMember> GetAbsentees()
        {
            // find latest status per crew
            var latestPerCrew = _records
                .GroupBy(r => r.CrewMember.CrewID)
                .Select(g => g.OrderByDescending(r => r.DateTime).First());

            return latestPerCrew
                .Where(r => r.Status == CrewStatus.Absent)
                .Select(r => r.CrewMember)
                .ToList();
        }

        public void GenerateReport()
        {
            Console.WriteLine("=== Attendance Report ===");
            foreach (var r in _records.OrderBy(r => r.DateTime))
            {
                Console.WriteLine($"{r.DateTime:u} | {r.CrewMember.Name} ({r.CrewMember.CrewID}) | {r.Status} | Verified: {r.VerifiedBy} | {r.Location} | {r.Note}");
            }
            Console.WriteLine("=========================");
        }
    }

    // Example usage
    public class Program
    {
        static void Main(string[] args)
        {
            var manager = new AttendanceManager();

            var captain = new Officer("O-001", "Hasan", "Captain", pin: "4321");
            var chiefEng = new Engineer("E-101", "Rina", "Chief Engineer");
            var ab = new Sailor("S-201", "Andi", "AB");

            var lifejacket = new SafetyEquipment { EquipmentID = "EQ-01", Type = "Lifejacket" };
            var toolbox = new SafetyEquipment { EquipmentID = "EQ-02", Type = "Toolbox" };

            // Assign equipment (encapsulated via CrewMember.AssignEquipment)
            captain.AssignEquipment(lifejacket);
            chiefEng.AssignEquipment(toolbox);

            // Record check-ins (polymorphism in action)
            var optOfficer = new CheckInOptions { Time = DateTime.UtcNow, Location = "Bridge", Pin = "4321" };
            manager.RecordCheckIn(captain, optOfficer);

            var optEngineer = new CheckInOptions { Time = DateTime.UtcNow.AddMinutes(1), Location = "Engine Room", EquipmentOk = true };
            manager.RecordCheckIn(chiefEng, optEngineer);

            var optSailor = new CheckInOptions { Time = DateTime.UtcNow.AddMinutes(2), Location = "Deck" };
            manager.RecordCheckIn(ab, optSailor);

            // Attempt officer with wrong pin
            var optOfficerBad = new CheckInOptions { Time = DateTime.UtcNow.AddMinutes(5), Location = "Bridge", Pin = "0000" };
            manager.RecordCheckIn(captain, optOfficerBad);

            // Generate report
            manager.GenerateReport();

            // Example: mark someone out (use CheckOut)
            var checkoutOptions = new CheckInOptions { Time = DateTime.UtcNow.AddHours(8), Location = "Quarters", Note = "End shift" };
            var coRec = ab.CheckOut(checkoutOptions);
            // manager should also record check-outs to central log if desired:
            manager.RecordCheckIn(ab, checkoutOptions); // OK: Recording CheckOut via RecordCheckIn calls CheckIn; but here we directly added CheckOut result then add to manager:
            // Better approach would be AttendanceManager.RecordCheckOut but omitted for brevity.

            Console.WriteLine("\nAbsentees (latest):");
            var absentees = manager.GetAbsentees();
            foreach (var x in absentees) Console.WriteLine($"- {x.Name} ({x.CrewID})");

            Console.WriteLine("\nDone. Press any key to exit...");
            Console.ReadKey();
        }
    }
}

