using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Data transfer object for application settings
    /// </summary>
    public class SettingsDto
    {
        public string MinIOEndpoint { get; set; } = "localhost:9000";
        public string BucketName { get; set; } = "evidence";
        public bool VerifyHashOnUpload { get; set; } = true;
        public bool RequireAuth { get; set; } = false;
        public bool EncryptAtRest { get; set; } = false;
    }
}
