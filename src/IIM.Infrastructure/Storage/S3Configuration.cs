using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Infrastructure.Storage
{
    // Rename the class to be more generic
    public class S3StorageConfiguration  // ← Renamed from MinIOConfiguration
    {
        public string Endpoint { get; set; } = "localhost:8333";
        public string AccessKey { get; set; } = "admin";
        public string SecretKey { get; set; } = "admin";
        public bool UseSSL { get; set; } = false;
        public string Region { get; set; } = "us-east-1";
        public bool EnableDeduplication { get; set; } = true;
        public int ChunkSize { get; set; } = 4 * 1024 * 1024; // 4MB

        // Add SeaweedFS-specific options if needed
        public bool EnableKafkaNotifications { get; set; } = true;
        public string KafkaBootstrapServers { get; set; } = "localhost:9092";
    }
}
