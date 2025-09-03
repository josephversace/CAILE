using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Application.Services
{
    /// <summary>
    /// Service for managing the virtual filesystem view of evidence files.
    /// Provides folder structure navigation while maintaining hash-based storage.
    /// </summary>
    public class VirtualFilesystemService
    {
        private readonly SqliteConnection _db;
        private readonly ILogger<VirtualFilesystemService> _logger;
        private readonly IMinIOStorageService _storage;

        /// <summary>
        /// Constructs a complete folder tree for a case from flat file records.
        /// This method builds the virtual folder structure that users see in the UI.
        /// </summary>
        /// <param name="caseId">The case to build folder structure for</param>
        /// <returns>Root folder containing entire tree structure</returns>
        /// <remarks>
        /// This method:
        /// 1. Queries all files for the case
        /// 2. Parses their virtual paths to build folder hierarchy
        /// 3. Creates VirtualFolder objects for each unique path
        /// 4. Links folders and files into tree structure
        /// 5. Calculates aggregate statistics (file counts, sizes)
        /// </remarks>
        public async Task<VirtualFolder> GetCaseFolderStructureAsync(string caseId)
        {
            // Query all files for this case, ordered by path for efficient tree building
            var files = await _db.QueryAsync<ManagedFile>(@"
            SELECT * FROM EvidenceFiles 
            WHERE CaseId = @CaseId 
            ORDER BY VirtualPath", //Ordering helps with folder creation
    
                new { CaseId = caseId });

            // Create root folder that will contain everything
            var root = new VirtualFolder
            {
                CaseId = caseId,
                Path = "/",
                Name = "Root",
                ParentPath = null  // Root has no parent
            };

            // Dictionary for O(1) folder lookups during tree construction
            // Key: folder path, Value: VirtualFolder object
            var folderMap = new Dictionary<string, VirtualFolder> { ["/"] = root };

            // Process each file to build folder structure
            foreach (var file in files)
            {
                // Split path into components
                // Example: "/Seized Phone/WhatsApp/Images/photo.jpg" 
                // Becomes: ["Seized Phone", "WhatsApp", "Images", "photo.jpg"]
                var pathParts = file.VirtualPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var currentPath = "";
                var parentFolder = root;

                // Build folder hierarchy for this file's path
                // We iterate through all parts except the last (which is the filename)
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    // Build up the current path progressively
                    // First iteration: "/Seized Phone"
                    // Second iteration: "/Seized Phone/WhatsApp"
                    // Third iteration: "/Seized Phone/WhatsApp/Images"
                    currentPath += "/" + pathParts[i];

                    // Create folder if it doesn't exist yet
                    if (!folderMap.ContainsKey(currentPath))
                    {
                        var folder = new VirtualFolder
                        {
                            CaseId = caseId,
                            Path = currentPath,
                            Name = pathParts[i],  // Just the folder name, not full path
                            ParentPath = parentFolder.Path
                        };

                        // Add to lookup dictionary for fast access
                        folderMap[currentPath] = folder;

                        // Link to parent folder's subfolder list
                        parentFolder.SubFolders.Add(folder);

                        _logger.LogDebug(
                            "Created virtual folder {Path} under parent {Parent}",
                            currentPath, parentFolder.Path);
                    }

                    // Move down the tree for next iteration
                    parentFolder = folderMap[currentPath];
                }

                // Add file to its immediate parent folder
                parentFolder.Files.Add(file);
                parentFolder.FileCount++;
                parentFolder.TotalSize += file.FileSize;

                _logger.LogDebug(
                    "Added file {FileName} to folder {Folder}",
                    file.FileName, parentFolder.Path);
            }

            _logger.LogInformation(
                "Built folder structure for case {CaseId}: {FolderCount} folders, {FileCount} files",
                caseId, folderMap.Count, files.Count());

            return root;
        }

        /// <summary>
        /// Uploads a file while preserving its virtual path in the folder structure.
        /// Handles deduplication automatically - if file exists, creates virtual reference only.
        /// </summary>
        /// <param name="caseId">Case to upload file to</param>
        /// <param name="fileStream">File content stream</param>
        /// <param name="virtualPath">Full path where file should appear (e.g., "/Evidence/Photos/img.jpg")</param>
        /// <param name="uploadedBy">User performing the upload</param>
        /// <returns>Created evidence file record</returns>
        /// <remarks>
        /// Deduplication logic:
        /// - Calculates SHA-256 hash of file content
        /// - Checks if file with same hash already exists
        /// - If exists: creates new virtual reference to existing storage
        /// - If new: uploads to MinIO and creates new storage entry
        /// This ensures each unique file is only stored once, saving space
        /// </remarks>
        public async Task<ManagedFile> UploadWithPathAsync(
            string caseId,
            Stream fileStream,
            string virtualPath,
            string uploadedBy)
        {
            _logger.LogInformation(
                "Starting upload for {Path} in case {CaseId}",
                virtualPath, caseId);

            // Calculate SHA-256 hash for deduplication
            // This reads the entire stream, so we reset position after
            var hash = await CalculateHashAsync(fileStream);
            fileStream.Position = 0;  // Reset for actual upload

            // Parse the virtual path into components
            var fileName = Path.GetFileName(virtualPath);
            // GetDirectoryName returns with backslashes on Windows, so normalize to forward slashes
            var parentFolder = Path.GetDirectoryName(virtualPath)?.Replace('\\', '/') ?? "/";
            // Count depth for UI tree indentation (number of forward slashes)
            var depth = virtualPath.Count(c => c == '/');

            _logger.LogDebug(
                "Parsed path - Name: {Name}, Parent: {Parent}, Depth: {Depth}",
                fileName, parentFolder, depth);

            // Check if we already have this exact file (deduplication check)
            var existing = await _db.QueryFirstOrDefaultAsync<ManagedFile>(
                "SELECT * FROM EvidenceFiles WHERE Hash = @Hash LIMIT 1",
                new { Hash = hash });

            // Create evidence record
            var evidence = new ManagedFile
            {
                Id = Guid.NewGuid().ToString(),
                CaseId = caseId,
                OriginalFileName = fileName,
                Hash = hash,
                FileSize = fileStream.Length,
                MimeType = DetectMimeType(fileName),
                VirtualPath = virtualPath,
                ParentFolder = parentFolder,
                Depth = depth,
                Status = EvidenceStatus.Quarantined,  // Always starts in quarantine
                UploadedAt = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            if (existing != null)
            {
                // === DEDUPLICATION CASE ===
                // File already exists in storage, just create a virtual reference
                // This saves storage space by not storing the same file twice

                evidence.StoragePath = existing.StoragePath;

                _logger.LogInformation(
                    "File {Hash} already exists at {StoragePath}, creating virtual reference at {Path}",
                    hash, existing.StoragePath, virtualPath);

                // Log for audit trail
                await LogDeduplicationEventAsync(evidence, existing);
            }
            else
            {
                // === NEW FILE CASE ===
                // File doesn't exist yet, need to actually store it

                evidence.StoragePath = $"quarantine/{caseId}/{hash}";

                _logger.LogInformation(
                    "Uploading new file {Hash} to {StoragePath}",
                    hash, evidence.StoragePath);

                // Upload to MinIO with metadata
                await _storage.PutObjectAsync(
                    "iim-quarantine",  // Bucket name - all new files go to quarantine
                    evidence.StoragePath,
                    fileStream,
                    new Dictionary<string, string>
                    {
                        ["case-id"] = caseId,
                        ["virtual-path"] = virtualPath,
                        ["original-name"] = fileName,
                        ["hash"] = hash,
                        ["uploaded-by"] = uploadedBy,
                        ["uploaded-at"] = DateTime.UtcNow.ToString("O")
                    });
            }

            // Save evidence record to database
            // This creates the virtual file entry that users see
            await _db.ExecuteAsync(@"
            INSERT INTO EvidenceFiles (
                Id, CaseId, FileName, Hash, FileSize, MimeType,
                VirtualPath, ParentFolder, Depth, Status,
                UploadedAt, UploadedBy, StoragePath
            ) VALUES (
                @Id, @CaseId, @FileName, @Hash, @FileSize, @MimeType,
                @VirtualPath, @ParentFolder, @Depth, @Status,
                @UploadedAt, @UploadedBy, @StoragePath
            )", evidence);

            _logger.LogInformation(
                "Successfully created evidence record {Id} for file {Path}",
                evidence.Id, virtualPath);

            return evidence;
        }

        /// <summary>
        /// Performs bulk upload of multiple files while preserving folder structure.
        /// Optimized for forensic tools that extract thousands of files.
        /// </summary>
        /// <param name="caseId">Case to upload files to</param>
        /// <param name="files">Collection of files with their relative paths</param>
        /// <param name="basePath">Base path to prepend to all files (e.g., "/Forensic Extract")</param>
        /// <returns>Number of files successfully uploaded</returns>
        /// <remarks>
        /// Performance optimizations:
        /// - Parallel processing with configurable batch size
        /// - Connection pooling for database operations
        /// - Streaming uploads to handle large files
        /// - Progress reporting via callbacks
        /// Typical use: Uploading entire forensic image extracts
        /// </remarks>
        public async Task<int> BulkUploadWithStructureAsync(
            string caseId,
            IEnumerable<FileUploadInfo> files,
            string basePath = "/")
        {
            var uploaded = 0;  // Thread-safe counter using Interlocked
            var failed = 0;
            var duplicates = 0;

            // List to hold running upload tasks
            var tasks = new List<Task>();

            // Semaphore to limit concurrent uploads (prevent resource exhaustion)
            var semaphore = new SemaphoreSlim(10, 10);  // Max 10 concurrent uploads

            _logger.LogInformation(
                "Starting bulk upload to case {CaseId} with base path {BasePath}",
                caseId, basePath);

            foreach (var file in files)
            {
                // Construct full virtual path for this file
                // Normalize path separators to forward slashes
                var virtualPath = Path.Combine(basePath, file.RelativePath)
                    .Replace('\\', '/');

                // Wait for semaphore slot (limits concurrent uploads)
                await semaphore.WaitAsync();

                // Create upload task
                var uploadTask = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogDebug("Uploading {Path}", virtualPath);

                        var result = await UploadWithPathAsync(
                            caseId,
                            file.Stream,
                            virtualPath,
                            file.UploadedBy);

                        // Thread-safe increment
                        Interlocked.Increment(ref uploaded);

                        // Check if it was a duplicate (for statistics)
                        if (result.StoragePath.Contains("quarantine"))
                            _logger.LogDebug("New file uploaded: {Path}", virtualPath);
                        else
                            Interlocked.Increment(ref duplicates);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failed);
                        _logger.LogError(ex,
                            "Failed to upload {Path}", virtualPath);
                    }
                    finally
                    {
                        // Always release semaphore slot
                        semaphore.Release();
                    }
                });

                tasks.Add(uploadTask);

                // Process in batches to avoid too many pending tasks
                if (tasks.Count >= 50)
                {
                    // Wait for current batch to complete
                    await Task.WhenAll(tasks);
                    tasks.Clear();

                    _logger.LogInformation(
                        "Batch complete. Uploaded: {Uploaded}, Failed: {Failed}, Duplicates: {Duplicates}",
                        uploaded, failed, duplicates);
                }
            }

            // Wait for remaining tasks
            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            _logger.LogInformation(
                "Bulk upload complete. Total: {Uploaded}, Failed: {Failed}, Duplicates: {Duplicates}",
                uploaded, failed, duplicates);

            return uploaded;
        }

        /// <summary>
        /// Retrieves all files in a specific virtual folder (non-recursive).
        /// </summary>
        /// <param name="caseId">Case ID to search in</param>
        /// <param name="folderPath">Virtual folder path (e.g., "/Evidence/Photos")</param>
        /// <returns>List of files directly in the specified folder</returns>
        /// <remarks>
        /// Does not include files in subfolders. For recursive listing, use GetFolderContentsRecursiveAsync
        /// </remarks>
        public async Task<List<EvidenceFile>> GetFolderContentsAsync(string caseId, string folderPath)
        {
            // Normalize folder path (ensure it starts with /)
            if (!folderPath.StartsWith("/"))
                folderPath = "/" + folderPath;

            _logger.LogDebug("Getting contents of folder {Path} in case {Case}",
                folderPath, caseId);

            // Query files where ParentFolder matches exactly
            var files = await _db.QueryAsync<EvidenceFile>(@"
            SELECT * FROM EvidenceFiles 
            WHERE CaseId = @CaseId 
              AND ParentFolder = @FolderPath
            ORDER BY FileName",
                new { CaseId = caseId, FolderPath = folderPath });

            return files.ToList();
        }

        /// <summary>
        /// Moves a file to a different virtual folder.
        /// Only updates the virtual path - doesn't move physical storage.
        /// </summary>
        /// <param name="fileId">ID of file to move</param>
        /// <param name="newFolderPath">Target folder path</param>
        /// <returns>True if successful</returns>
        /// <remarks>
        /// This is a virtual operation - the actual file storage location doesn't change.
        /// Only the virtual path that users see is updated.
        /// </remarks>
        public async Task<bool> MoveFileVirtuallyAsync(string fileId, string newFolderPath)
        {
            // Get the existing file
            var file = await _db.QueryFirstOrDefaultAsync<EvidenceFile>(
                "SELECT * FROM EvidenceFiles WHERE Id = @Id",
                new { Id = fileId });

            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for move operation", fileId);
                return false;
            }

            // Calculate new virtual path
            var fileName = Path.GetFileName(file.VirtualPath);
            var newVirtualPath = Path.Combine(newFolderPath, fileName).Replace('\\', '/');

            _logger.LogInformation(
                "Moving file {FileId} from {OldPath} to {NewPath}",
                fileId, file.VirtualPath, newVirtualPath);

            // Update database (virtual move only)
            var rowsAffected = await _db.ExecuteAsync(@"
            UPDATE EvidenceFiles 
            SET VirtualPath = @NewPath,
                ParentFolder = @NewFolder,
                Depth = @NewDepth
            WHERE Id = @Id",
                new
                {
                    Id = fileId,
                    NewPath = newVirtualPath,
                    NewFolder = newFolderPath,
                    NewDepth = newVirtualPath.Count(c => c == '/')
                });

            // Log the move operation for audit trail
            await LogFileMoveAsync(file, newVirtualPath);

            return rowsAffected > 0;
        }

        /// <summary>
        /// Creates a virtual folder (just a database entry, no physical folder).
        /// </summary>
        /// <param name="caseId">Case to create folder in</param>
        /// <param name="folderPath">Full path of new folder</param>
        /// <param name="createdBy">User creating the folder</param>
        /// <returns>True if created successfully</returns>
        /// <remarks>
        /// Virtual folders exist only as database entries. They're created implicitly
        /// when files are added, but this method allows explicit folder creation
        /// for organizational purposes before files are added.
        /// </remarks>
        public async Task<bool> CreateVirtualFolderAsync(
            string caseId,
            string folderPath,
            string createdBy)
        {
            // Normalize path
            if (!folderPath.StartsWith("/"))
                folderPath = "/" + folderPath;

            // Check if folder already exists
            var existing = await _db.QueryFirstOrDefaultAsync<int>(@"
            SELECT COUNT(*) FROM VirtualFolders 
            WHERE CaseId = @CaseId AND Path = @Path",
                new { CaseId = caseId, Path = folderPath });

            if (existing > 0)
            {
                _logger.LogWarning(
                    "Folder {Path} already exists in case {Case}",
                    folderPath, caseId);
                return false;
            }

            // Parse folder name and parent
            var folderName = Path.GetFileName(folderPath);
            var parentPath = Path.GetDirectoryName(folderPath)?.Replace('\\', '/') ?? "/";

            // Insert folder record
            await _db.ExecuteAsync(@"
            INSERT INTO VirtualFolders (Id, CaseId, Path, Name, ParentPath, CreatedAt, CreatedBy)
            VALUES (@Id, @CaseId, @Path, @Name, @ParentPath, @CreatedAt, @CreatedBy)",
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    CaseId = caseId,
                    Path = folderPath,
                    Name = folderName,
                    ParentPath = parentPath,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                });

            _logger.LogInformation(
                "Created virtual folder {Path} in case {Case}",
                folderPath, caseId);

            return true;
        }

        /// <summary>
        /// Calculates SHA-256 hash of a stream's content.
        /// Used for deduplication - files with same hash are identical.
        /// </summary>
        /// <param name="stream">Stream to hash</param>
        /// <returns>Hex string representation of SHA-256 hash</returns>
        /// <remarks>
        /// SHA-256 produces a 256-bit (32 byte) hash.
        /// Converted to hex string for storage (64 characters).
        /// Probability of collision is negligible (1 in 2^256).
        /// </remarks>
        private async Task<string> CalculateHashAsync(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);

            // Convert to lowercase hex string for consistency
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// Detects MIME type from file extension.
        /// Used for file icon display and processing decisions.
        /// </summary>
        /// <param name="fileName">Name of file including extension</param>
        /// <returns>MIME type string (e.g., "image/jpeg")</returns>
        /// <remarks>
        /// This is a simple extension-based detection.
        /// For more accurate detection, consider using file content analysis
        /// or a library like MimeDetective.
        /// </remarks>
        private string DetectMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            // Common forensic file types
            return extension switch
            {
                // Documents
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".rtf" => "application/rtf",

                // Images
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",

                // Videos
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".mkv" => "video/x-matroska",
                ".webm" => "video/webm",
                ".m4v" => "video/x-m4v",

                // Audio
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/x-m4a",
                ".aac" => "audio/aac",
                ".wma" => "audio/x-ms-wma",
                ".flac" => "audio/flac",

                // Archives
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                ".bz2" => "application/x-bzip2",

                // Forensic formats
                ".e01" => "application/x-encase-image",
                ".dd" => "application/x-raw-disk-image",
                ".vmdk" => "application/x-vmdk",
                ".pst" => "application/vnd.ms-outlook",
                ".ost" => "application/vnd.ms-outlook",
                ".eml" => "message/rfc822",
                ".msg" => "application/vnd.ms-outlook",

                // Databases
                ".db" => "application/x-sqlite3",
                ".sqlite" => "application/x-sqlite3",
                ".mdb" => "application/x-msaccess",

                // Code/Config
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".log" => "text/plain",

                // Default for unknown types
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Logs deduplication event for audit trail.
        /// Records when a duplicate file is detected and linked.
        /// </summary>
        private async Task LogDeduplicationEventAsync(
            EvidenceFile newReference,
            EvidenceFile existingFile)
        {
            await _db.ExecuteAsync(@"
            INSERT INTO AuditLog (Id, EventType, EntityType, EntityId, Details, Timestamp, UserId)
            VALUES (@Id, @EventType, @EntityType, @EntityId, @Details, @Timestamp, @UserId)",
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = "Deduplication",
                    EntityType = "EvidenceFile",
                    EntityId = newReference.Id,
                    Details = $"Duplicate of {existingFile.Id} detected. Hash: {newReference.Hash}",
                    Timestamp = DateTime.UtcNow,
                    UserId = newReference.UploadedBy
                });
        }

        /// <summary>
        /// Logs file move operation for audit trail.
        /// </summary>
        private async Task LogFileMoveAsync(ManagedFile file, string newPath)
        {
            await _db.ExecuteAsync(@"
            INSERT INTO AuditLog (Id, EventType, EntityType, EntityId, Details, Timestamp)
            VALUES (@Id, @EventType, @EntityType, @EntityId, @Details, @Timestamp)",
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = "FileMove",
                    EntityType = "EvidenceFile",
                    EntityId = file.Id,
                    Details = $"Moved from {file.VirtualPath} to {newPath}",
                    Timestamp = DateTime.UtcNow
                });
        }
    }

    /// <summary>
    /// Helper class for bulk file upload operations.
    /// Contains information about a file to be uploaded.
    /// </summary>
    public class FileUploadInfo
    {
        /// <summary>
        /// File content stream.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Relative path from the upload root.
        /// Example: "Documents/Contracts/contract.pdf"
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Original file name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User performing the upload.
        /// </summary>
        public string UploadedBy { get; set; }
    }
}
