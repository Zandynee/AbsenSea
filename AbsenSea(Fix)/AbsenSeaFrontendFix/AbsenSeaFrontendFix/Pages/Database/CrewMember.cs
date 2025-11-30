using AbsenSeaFrontendFix.Pages.Database;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("CREW_MEMBER")]
public class CrewMember : BaseModel
{
    [PrimaryKey("CREW_ID")]
    public long CREW_ID { get; set; }

    [Column("CREW_NAME")]
    public string CREW_NAME { get; set; }

    [Column("user_id")]
    public Guid? user_id { get; set; }

    [Column("ship_id")]
    public long? ship_id { get; set; }
}
