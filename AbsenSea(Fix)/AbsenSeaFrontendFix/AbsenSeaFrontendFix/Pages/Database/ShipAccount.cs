using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbsenSeaFrontendFix.Pages.Database
{
    [Table("SHIP_ACCOUNT")]
    public class ShipAccount : BaseModel
    {
        [PrimaryKey("SHIP_ID")]
        public long SHIP_ID { get; set; }

        [Column("SHIP_NAME")]
        public string SHIP_NAME { get; set; }

        [Column("user_id")]
        public Guid? user_id { get; set; }
    }
}
