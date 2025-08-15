using System;
using System.Collections.Generic;
using System.IO;


namespace IIM.Core.Security
{
    /// <summary>
    /// Configuration for evidence storage, security, and chain of custody requirements.
    /// This is the single source of truth for evidence configuration across the platform.
    /// </summary>
    public class EvidenceConfiguration
    {
        // Storage Settings
        public string StorePath { get; set; } = GetDefaultStorePath();
        public string TempPath { get; set; } = GetDefaultTempPath();
        public string QuarantinePath { get; set; } = GetDefaultQuarantinePath();

        // Security Settings
        public bool EnableEncryption { get; set; } = true;  // Default to secure
        public bool RequireDualControl { get; set; } = false;
        public bool EnableIntegrityChecking { get; set; } = true;
        public bool RequireDigitalSignatures { get; set; } = false;
        public string HashAlgorithm { get; set; } = "SHA256";

        // Size and Type Restrictions
        public int MaxFileSizeMb { get; set; } = 10240; // 10GB default
        public long MaxTotalStorageGb { get; set; } = 500; // 500GB default
        public List<string> AllowedFileTypes { get; set; } = GetDefaultAllowedTypes();
        public List<string> BlockedFileTypes { get; set; } = GetDefaultBlockedTypes();

        // Chain of Custody Settings
        public bool EnableChainOfCustody { get; set; } = true;
        public bool RequireWitnessForAccess { get; set; } = false;
        public int MinimumAuthorizationLevel { get; set; } = 1;
        public bool LogAllAccess { get; set; } = true;

        // Advanced Security Settings
        public Dictionary<string, object> SecuritySettings { get; set; } = new()
        {
            ["EnableVirusScan"] = true,
            ["EnableSandboxing"] = true,
            ["RequireMFA"] = false,
            ["SessionTimeoutMinutes"] = 30,
            ["MaxConcurrentAccess"] = 5,
            ["EnableWatermarking"] = false,
            ["PreserveMetadata"] = true,
            ["EnableForensicMode"] = true
        };

        // Audit Settings
        public bool EnableAuditLog { get; set; } = true;
        public bool EnableVideoRecording { get; set; } = false;
        public int AuditRetentionDays { get; set; } = 2555; // 7 years

        // Performance Settings
        public int ChunkSizeMb { get; set; } = 64;
        public int MaxConcurrentUploads { get; set; } = 3;
        public bool EnableCompression { get; set; } = true;
        public bool EnableDeduplication { get; set; } = true;

        // Helper Methods
        private static string GetDefaultStorePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIM",
                "Evidence",
                "Store");
        }

        private static string GetDefaultTempPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIM",
                "Evidence",
                "Temp");
        }

        private static string GetDefaultQuarantinePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIM",
                "Evidence",
                "Quarantine");
        }

        private static List<string> GetDefaultAllowedTypes()
        {
            return new List<string>
            {
                // Documents
                ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
                ".txt", ".rtf", ".odt", ".ods", ".odp", ".csv", ".xml", ".json",
                
                // Images
                ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif",
                ".raw", ".svg", ".webp", ".ico", ".heic", ".heif",
                
                // Video
                ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
                ".m4v", ".mpg", ".mpeg", ".3gp", ".h264", ".h265",
                
                // Audio
                ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a",
                ".opus", ".amr", ".ac3", ".aiff",
                
                // Archives
                ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
                ".iso", ".dmg", ".pkg",
                
                // Forensic
                ".dd", ".e01", ".ex01", ".aff", ".aff4", ".raw", ".img",
                ".vmdk", ".vhd", ".vhdx", ".ova", ".ovf",
                
                // Database
                ".db", ".sqlite", ".mdb", ".accdb", ".dbf",
                
                // Email
                ".eml", ".msg", ".pst", ".ost", ".mbox",
                
                // Log files
                ".log", ".evtx", ".etl", ".pcap", ".pcapng",
                
                // Mobile
                ".apk", ".ipa", ".xap", ".appx", ".backup"
            };
        }

        private static List<string> GetDefaultBlockedTypes()
        {
            return new List<string>
            {
                ".exe", ".com", ".bat", ".cmd", ".scr", ".msi",
                ".vbs", ".vbe", ".js", ".jse", ".ws", ".wsf",
                ".ps1", ".ps2", ".psc1", ".psc2", ".lnk", ".inf",
                ".reg", ".dll", ".sys", ".drv", ".cpl", ".action"
            };
        }

        /// <summary>
        /// Validates if a file type is allowed based on configuration
        /// </summary>
        public bool IsFileTypeAllowed(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                return false;

            // Check blocked list first (takes precedence)
            if (BlockedFileTypes.Contains(extension))
                return false;

            // If we have an allow list, file must be in it
            if (AllowedFileTypes.Any())
                return AllowedFileTypes.Contains(extension);

            // If no allow list, allow by default (unless blocked)
            return true;
        }

        /// <summary>
        /// Gets the appropriate storage path for evidence based on its classification
        /// </summary>
        public string GetStoragePathForClassification(string classification)
        {
            return classification?.ToUpperInvariant() switch
            {
                "TOP SECRET" => Path.Combine(StorePath, "TopSecret"),
                "SECRET" => Path.Combine(StorePath, "Secret"),
                "CONFIDENTIAL" => Path.Combine(StorePath, "Confidential"),
                "RESTRICTED" => Path.Combine(StorePath, "Restricted"),
                _ => Path.Combine(StorePath, "Unclassified")
            };
        }

        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(StorePath))
                errors.Add("StorePath cannot be empty");

            if (MaxFileSizeMb <= 0)
                errors.Add("MaxFileSizeMb must be greater than 0");

            if (MaxTotalStorageGb <= 0)
                errors.Add("MaxTotalStorageGb must be greater than 0");

            if (ChunkSizeMb <= 0 || ChunkSizeMb > MaxFileSizeMb)
                errors.Add("ChunkSizeMb must be between 1 and MaxFileSizeMb");

            if (EnableEncryption && string.IsNullOrWhiteSpace(HashAlgorithm))
                errors.Add("HashAlgorithm is required when encryption is enabled");

            if (RequireDualControl && !EnableChainOfCustody)
                errors.Add("Chain of custody must be enabled for dual control");

            return (errors.Count == 0, errors);
        }
    }
}