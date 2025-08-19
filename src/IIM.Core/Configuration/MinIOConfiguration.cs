using System;
using System.Collections.Generic;
using System.Text;

namespace Configuration
{
    public class MinIOConfiguration
    {
        public string Endpoint { get; set; } = "localhost:9000";
        public string AccessKey { get; set; } = "minioadmin";
        public string SecretKey { get; set; } = "minioadmin123";
        public bool UseSSL { get; set; } = false;
        public string Region { get; set; } = "us-east-1";
        public bool EnableDeduplication { get; set; } = true;
        public int ChunkSize { get; set; } = 4 * 1024 * 1024; // 4MB
    }
}
