using AbsenSeaFrontendFix.Pages.Database;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("CREW_CHECK")]
public class CrewCheck : BaseModel
{
    [PrimaryKey("CHECK_ID")]
    public long CHECK_ID { get; set; }

    [Column("CAPTURE_AT")]
    public DateTime CAPTURE_AT { get; set; }

    [Column("CHECK_DATE")]
    public DateTime? CHECK_DATE { get; set; }

    [Column("PRESENT")]
    public bool? PRESENT { get; set; }

    [Column("HELMET")]
    public bool? HELMET { get; set; }

    [Column("VEST")]
    public bool? VEST { get; set; }

    [Column("CREW_ID")]
    public long? CREW_ID { get; set; }

    // Navigation property for joined data
    [Reference(typeof(CrewMember))]
    public CrewMember CREW_MEMBER { get; set; }
}
