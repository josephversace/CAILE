using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class Setting
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }  // JSON serialized
        public string Category { get; set; }  // "MinIO", "Deployment", etc.
        public string Description { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsUserConfigurable { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
