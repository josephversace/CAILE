// src/IIM.Core/Configuration/StorageConfiguration.cs
using System;
using System.IO;

namespace IIM.Core.Configuration
{
    /// <summary>
    /// Central storage configuration for all IIM services
    /// </summary>
    public class StorageConfiguration
    {
        /// <summary>
        /// Gets or sets the base storage path for IIM data
        /// </summary>
        public string BasePath { get; set; } = GetDefaultBasePath();

        /// <summary>
        /// Gets or sets whether to use SQLite for structured data
        /// </summary>
        public bool UseSqlite { get; set; } = false;

        /// <summary>
        /// Gets or sets the SQLite database filename
        /// </summary>
        public string SqliteDbName { get; set; } = "iim.db";

        /// <summary>
        /// Gets the full path to the SQLite database
        /// </summary>
        public string SqlitePath => Path.Combine(BasePath, SqliteDbName);

        /// <summary>
        /// Gets the path for case storage
        /// </summary>
        public string CasesPath => Path.Combine(BasePath, "Cases");

        /// <summary>
        /// Gets the path for investigation sessions
        /// </summary>
        public string SessionsPath => Path.Combine(BasePath, "Sessions");

        /// <summary>
        /// Gets the path for evidence storage
        /// </summary>
        public string EvidencePath => Path.Combine(BasePath, "Evidence");

        /// <summary>
        /// Gets the path for vector storage
        /// </summary>
        public string VectorStorePath => Path.Combine(BasePath, "VectorStore");

        /// <summary>
        /// Gets the path for temporary files
        /// </summary>
        public string TempPath => Path.Combine(BasePath, "Temp");

        /// <summary>
        /// Gets the path for models
        /// </summary>
        public string ModelsPath => Path.Combine(BasePath, "Models");

        /// <summary>
        /// Gets the default base path based on the operating system
        /// </summary>
        private static string GetDefaultBasePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIM"
            );
        }

        /// <summary>
        /// Ensures all required directories exist
        /// </summary>
        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(BasePath);
            Directory.CreateDirectory(CasesPath);
            Directory.CreateDirectory(SessionsPath);
            Directory.CreateDirectory(EvidencePath);
            Directory.CreateDirectory(VectorStorePath);
            Directory.CreateDirectory(TempPath);
            Directory.CreateDirectory(ModelsPath);
        }
    }
}