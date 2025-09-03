using IIM.S3.Interfaces;
using IIM.S3.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IIM.S3.Services
{
    // File: Implementations.cs
    // Purpose: concrete implementations with // FIX: notes for hardened behavior.


    public sealed class UtcClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

    internal static class KeySanitizer
    {
        public static string SafeKey(string key)
        {
            key = key.Replace('\\', '/');
            if (key.StartsWith('/') || key.Contains("..")) throw new InvalidOperationException("Invalid key path.");
            var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return string.Join('/', parts);
        }
    }

    // Add once in your project:
    [JsonSerializable(typeof(List<CompletePart>))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]

// Use it:
var parts = await JsonSerializer.DeserializeAsync(
    req.Body, ApiJsonContext.Default.ListCompletePart) ?? new();


    // ---------- Presigned URLs (CamSig) ----------
    // FIX: honest, verifiable HMAC scheme instead of pretending AWS SigV4 without full canonicalization.
    public sealed class CamSigPresignedUrlService : IPresignedUrlService
    {
        private readonly string _secretKey; // base64
        private readonly string _baseUrl;
        private readonly IClock _clock;
        private readonly ILogger<CamSigPresignedUrlService> _log;

        public CamSigPresignedUrlService(IConfiguration cfg, IClock clock, ILogger<CamSigPresignedUrlService> log)
        {
            _secretKey = cfg["S3:SecretKey"] ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _baseUrl = cfg["S3:BaseUrl"] ?? "http://localhost:5000";
            _clock = clock; _log = log;
        }

        public Task<string> GenerateAsync(PresignedUrlRequest req)
        {
            var exp = _clock.UtcNow.AddSeconds(req.ExpirySeconds).ToUnixTimeSeconds();
            var path = $"/{Uri.EscapeDataString(req.Bucket)}/{Uri.EscapeDataString(req.Key)}";
            var toSign = $"{req.Operation}\n{path}\n{exp}\n{req.UserId}";
            using var hmac = new HMACSHA256(Convert.FromBase64String(_secretKey));
            var sig = WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign)));

            var qs = QueryString.Empty
                .Add("X-CamSig-Alg", "HMAC-SHA256")
                .Add("X-CamSig-Exp", exp.ToString())
                .Add("X-CamSig-User", req.UserId)
                .Add("X-CamSig", sig);

            var url = $"{_baseUrl}{path}{qs}";
            _log.LogInformation("Presigned {op} {path} exp={exp}", req.Operation, path, exp);
            return Task.FromResult(url);
        }

        public Task<bool> ValidateAsync(string absoluteUrl, string operation)
        {
            try
            {
                var uri = new Uri(absoluteUrl);
                var q = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                if (!q.TryGetValue("X-CamSig-Exp", out var expStr) || !long.TryParse(expStr, out var exp)) return Task.FromResult(false);
                if (_clock.UtcNow.ToUnixTimeSeconds() > exp) return Task.FromResult(false);

                var user = q["X-CamSig-User"].ToString();
                var sig = q["X-CamSig"].ToString();

                var toSign = $"{operation}\n{uri.AbsolutePath}\n{exp}\n{user}";
                using var h = new HMACSHA256(Convert.FromBase64String(_secretKey));
                var expected = WebEncoders.Base64UrlEncode(h.ComputeHash(Encoding.UTF8.GetBytes(toSign)));
                var ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sig), Encoding.UTF8.GetBytes(expected));
                return Task.FromResult(ok);
            }
            catch { return Task.FromResult(false); }
        }
    }

    // ---------- Hashing (single-pass to disk) ----------
    // FIX: Handles non-seekable bodies. fs.Flush(true) + best-effort directory fsync for crash safety.
    public sealed class HashingService : IHashingService
    {
        public async Task<HashBundle> HashAndWriteAsync(Stream src, string tempPath, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough);
            using var md5 = MD5.Create();
            using var sha256 = SHA256.Create();
            using var sha512 = SHA512.Create();
            var buf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            long total = 0;
            try
            {
                int read;
                while ((read = await src.ReadAsync(buf.AsMemory(0, buf.Length), ct)) > 0)
                {
                    md5.TransformBlock(buf, 0, read, null, 0);
                    sha256.TransformBlock(buf, 0, read, null, 0);
                    sha512.TransformBlock(buf, 0, read, null, 0);
                    await fs.WriteAsync(buf.AsMemory(0, read), ct);
                    total += read;
                }
                md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                sha512.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                fs.Flush(true);
                TryFsyncDirectory(Path.GetDirectoryName(tempPath)!);
                return new HashBundle(Convert.ToHexString(md5.Hash!).ToLowerInvariant(),
                                      Convert.ToHexString(sha256.Hash!).ToLowerInvariant(),
                                      Convert.ToHexString(sha512.Hash!).ToLowerInvariant(),
                                      total);
            }
            finally { ArrayPool<byte>.Shared.Return(buf); }
        }

        public async Task<(string md5Hex, long size)> HashMd5AndWriteAsync(Stream src, string tempPath, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough);
            using var md5 = MD5.Create();
            var buf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            long total = 0;
            try
            {
                int read;
                while ((read = await src.ReadAsync(buf.AsMemory(0, buf.Length), ct)) > 0)
                {
                    md5.TransformBlock(buf, 0, read, null, 0);
                    await fs.WriteAsync(buf.AsMemory(0, read), ct);
                    total += read;
                }
                md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                fs.Flush(true);
                TryFsyncDirectory(Path.GetDirectoryName(tempPath)!);
                return (Convert.ToHexString(md5.Hash!).ToLowerInvariant(), total);
            }
            finally { ArrayPool<byte>.Shared.Return(buf); }
        }

        private static void TryFsyncDirectory(string dir)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var d = File.Open(dir, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    d.Flush(true);
                }
            }
            catch { /* best-effort */ }
        }
    }

    // ---------- SMB service (hardened) ----------
    // FIX: no guest accounts, disable wide links, path jailing for symlinks, no Everyone grants.
    public sealed class SmbShareService : ISmbShareService
    {
        private readonly StoragePaths _paths;
        private readonly ILogger<SmbShareService> _log;
        private readonly bool _isWindows;

        public SmbShareService(StoragePaths paths, ILogger<SmbShareService> log)
        { _paths = paths; _log = log; _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows); }

        public void InitializeShares()
        {
            var smbRoot = Path.Combine(_paths.BasePath, "shares");
            Directory.CreateDirectory(Path.Combine(smbRoot, "primary"));
            Directory.CreateDirectory(Path.Combine(smbRoot, "quarantine"));

            if (_isWindows)
            {
                TryRun("cmd", $"/c net share primary=\"{Path.Combine(smbRoot, "primary")}\" /GRANT:ForensicsRO,READ");
                TryRun("cmd", $"/c net share quarantine=\"{Path.Combine(smbRoot, "quarantine")}\" /GRANT:ForensicsRO,READ");
            }
            else
            {
                var cfg = @$"
[global]
   workgroup = WORKGROUP
   server string = ForensicS3 SMB
   security = user
   map to guest = Never
   follow symlinks = yes
   wide links = no
[primary]
   path = {Path.Combine(smbRoot, "primary")}
   read only = yes
   guest ok = no
   valid users = @forensics_ro
[quarantine]
   path = {Path.Combine(smbRoot, "quarantine")}
   read only = yes
   guest ok = no
   valid users = @forensics_ro
";
                try
                {
                    File.WriteAllText("/etc/samba/smb.conf", cfg);
                    TryRun("sh", "-lc 'systemctl restart smbd'");
                }
                catch (Exception ex) { _log.LogWarning(ex, "Skipping Samba configuration (permissions?)"); }
            }
        }

        public Task CreateShareAsync(string bucket, string? shareName) => Task.CompletedTask;

        public async Task CreateSymlinkAsync(string bucket, string key, string physicalPath)
        {
            key = KeySanitizer.SafeKey(key);
            var smbRoot = Path.Combine(_paths.BasePath, "shares");
            var shareDir = Path.Combine(smbRoot, bucket);
            Directory.CreateDirectory(shareDir);

            // FIX: path jail
            if (!IsUnder(physicalPath, _paths.BasePath) && !IsUnder(physicalPath, _paths.CasPath))
                throw new InvalidOperationException("Symlink target outside allowed roots.");

            var linkPath = Path.Combine(shareDir, key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
            if (File.Exists(linkPath)) File.Delete(linkPath);

            if (_isWindows) await TryRunAsync("cmd", $"/c mklink \"{linkPath}\" \"{physicalPath}\"");
            else await TryRunAsync("ln", $"-sf \"{physicalPath}\" \"{linkPath}\"");
        }

        public Task RemoveSymlinkAsync(string bucket, string key)
        {
            key = KeySanitizer.SafeKey(key);
            var linkPath = Path.Combine(_paths.BasePath, "shares", bucket, key.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(linkPath)) File.Delete(linkPath);
            return Task.CompletedTask;
        }

        private static bool IsUnder(string path, string root)
        {
            var full = Path.GetFullPath(path);
            var r = Path.GetFullPath(root);
            return full.StartsWith(r, StringComparison.OrdinalIgnoreCase);
        }

        private static void TryRun(string file, string args)
        {
            try { Process.Start(new ProcessStartInfo { FileName = file, Arguments = args, UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(); }
            catch { }
        }
        private static async Task TryRunAsync(string file, string args)
        {
            try { await Process.Start(new ProcessStartInfo { FileName = file, Arguments = args, UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(); }
            catch { }
        }
    }

    // ---------- Policy engine ----------
    public sealed class PolicyEngine : IPolicyEngine
    {
        public Task<bool> CanGeneratePresignedUrlAsync(string user, string bucket, string key, string operation) => Task.FromResult(true);
        public Task<bool> CanBypassGovernanceAsync(string user, string bucket, string key) => Task.FromResult(false); // wire to RBAC
        public Task<RoutingDecision> DetermineRoutingAsync(string bucket, string key, IDictionary<string, string> metadata)
        {
            if (metadata.TryGetValue("x-iim-quarantine", out var v) && v == "1")
                return Task.FromResult(new RoutingDecision(true, bucket, "Flagged by uploader"));
            return Task.FromResult(new RoutingDecision(false, bucket, ""));
        }
    }

    // ---------- CAS (dedup) ----------
    public sealed class ContentAddressableStorage : IDeduplicationService
    {
        private readonly StoragePaths _paths;
        public ContentAddressableStorage(StoragePaths paths) { _paths = paths; }

        public Task<string?> GetPathByHashAsync(string sha256Hex)
        {
            var p = CasPath(sha256Hex);
            return Task.FromResult(File.Exists(p) ? p : null);
        }

        public Task<string> PutCasAsync(string tempPath, string sha256Hex)
        {
            var dest = CasPath(sha256Hex);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (!File.Exists(dest)) File.Move(tempPath, dest, overwrite: false);
            else File.Delete(tempPath);
            TryFsync(Path.GetDirectoryName(dest)!);
            return Task.FromResult(dest);
        }

        private string CasPath(string sha256Hex) => Path.Combine(_paths.CasPath, sha256Hex[..2], sha256Hex[2..4], sha256Hex);
        private static void TryFsync(string dir) { try { using var d = File.Open(dir, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); d.Flush(true); } catch { } }
    }

    // ---------- Storage backend (filesystem) ----------
    public sealed class FileSystemBackend : IStorageBackend
    {
        private readonly StoragePaths _paths;
        private readonly IHashingService _hash;

        public FileSystemBackend(StoragePaths paths, IHashingService hash)
        { _paths = paths; _hash = hash; }

        public Task<string> CreateBucketDirectoryAsync(string bucket)
        {
            var dir = Path.Combine(_paths.BasePath, "buckets", bucket);
            Directory.CreateDirectory(dir);
            return Task.FromResult(dir);
        }

        public async Task<(string partPath, string md5Hex, long size)> StorePartAsync(string uploadId, int partNumber, Stream src, CancellationToken ct = default)
        {
            var dir = Path.Combine(_paths.TempPath, "mp", uploadId);
            Directory.CreateDirectory(dir);
            var partPath = Path.Combine(dir, $"{partNumber:00000000}.part");
            var (md5Hex, size) = await _hash.HashMd5AndWriteAsync(src, partPath, ct);
            return (partPath, md5Hex, size);
        }

        public async Task<string> CombinePartsAsync(string uploadId, IEnumerable<UploadPart> parts, string tempOutPath, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempOutPath)!);
            using var outFs = new FileStream(tempOutPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough);
            var buf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            try
            {
                foreach (var p in parts.OrderBy(p => p.PartNumber))
                {
                    using var inFs = new FileStream(p.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    int read;
                    while ((read = await inFs.ReadAsync(buf.AsMemory(0, buf.Length), ct)) > 0)
                        await outFs.WriteAsync(buf.AsMemory(0, read), ct);
                }
                outFs.Flush(true);
                return tempOutPath;
            }
            finally { ArrayPool<byte>.Shared.Return(buf); }
        }

        public Task CleanupPartsAsync(string uploadId)
        {
            var dir = Path.Combine(_paths.TempPath, "mp", uploadId);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            return Task.CompletedTask;
        }

        public Task<string> MoveToFinalLocationAsync(string bucket, string key, string tempPath)
        {
            key = KeySanitizer.SafeKey(key);
            var finalPath = Path.Combine(_paths.BasePath, "buckets", bucket, key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.Move(tempPath, finalPath, overwrite: true);
            TryFsync(Path.GetDirectoryName(finalPath)!);
            return Task.FromResult(finalPath);
        }

        public Task<Stream> OpenReadAsync(string physicalPath)
            => Task.FromResult<Stream>(new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.RandomAccess));

        private static void TryFsync(string dir) { try { using var d = File.Open(dir, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); d.Flush(true); } catch { } }
    }

    // ---------- SQLite metadata (single node) ----------
    public sealed class SqliteMetadataStore : IMetadataStore
    {
        private readonly string _dbPath;
        private SqliteConnection? _conn;

        public SqliteMetadataStore(StoragePaths paths) => _dbPath = paths.DbPath;

        public void Init()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            _conn = new SqliteConnection($"Data Source={_dbPath};Pooling=True");
            _conn.Open();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS buckets(
  name TEXT PRIMARY KEY,
  enable_dedup INTEGER NOT NULL DEFAULT 1,
  object_lock_enabled INTEGER NOT NULL DEFAULT 0,
  default_retention_days INTEGER NOT NULL DEFAULT 90,
  storage_class TEXT NOT NULL DEFAULT 'STANDARD',
  smb_share TEXT NULL
);
CREATE TABLE IF NOT EXISTS objects(
  bucket TEXT NOT NULL,
  key TEXT NOT NULL,
  latest_version_id TEXT,
  PRIMARY KEY(bucket,key)
);
CREATE TABLE IF NOT EXISTS versions(
  version_id TEXT PRIMARY KEY,
  bucket TEXT NOT NULL,
  key TEXT NOT NULL,
  physical_path TEXT NOT NULL,
  size INTEGER NOT NULL,
  content_type TEXT NOT NULL,
  md5_hex TEXT NOT NULL,
  sha256_hex TEXT NOT NULL,
  sha512_hex TEXT NOT NULL,
  is_dedup INTEGER NOT NULL,
  storage_class TEXT NOT NULL,
  created_utc TEXT NOT NULL,
  deleted_utc TEXT NULL,
  lock_mode TEXT NOT NULL,
  lock_retain_until_utc TEXT NULL,
  lock_legal_hold INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS custody(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  bucket TEXT NOT NULL,
  key TEXT NOT NULL,
  version_id TEXT NOT NULL,
  action TEXT NOT NULL,
  user TEXT NOT NULL,
  ts_utc TEXT NOT NULL,
  details TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS mp_uploads(
  upload_id TEXT PRIMARY KEY,
  bucket TEXT NOT NULL,
  key TEXT NOT NULL,
  initiated_utc TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS mp_parts(
  upload_id TEXT NOT NULL,
  part_num INTEGER NOT NULL,
  size INTEGER NOT NULL,
  md5_hex TEXT NOT NULL,
  path TEXT NOT NULL,
  PRIMARY KEY(upload_id, part_num)
);
";
            cmd.ExecuteNonQuery();
        }

        private SqliteConnection Conn => _conn ?? throw new InvalidOperationException("Metadata store not initialized");

        public async Task CreateBucketAsync(string bucket, BucketConfiguration cfg)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO buckets(name,enable_dedup,object_lock_enabled,default_retention_days,storage_class,smb_share)
VALUES($n,$d,$o,$r,$s,$m)";
            cmd.Parameters.AddWithValue("$n", bucket);
            cmd.Parameters.AddWithValue("$d", cfg.EnableDeduplication ? 1 : 0);
            cmd.Parameters.AddWithValue("$o", cfg.ObjectLockEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$r", cfg.DefaultRetentionDays);
            cmd.Parameters.AddWithValue("$s", cfg.StorageClass);
            cmd.Parameters.AddWithValue("$m", string.IsNullOrWhiteSpace(cfg.SmbShareName) ? DBNull.Value : cfg.SmbShareName);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<BucketConfiguration> GetBucketConfigAsync(string bucket)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = "SELECT enable_dedup,object_lock_enabled,default_retention_days,storage_class,smb_share FROM buckets WHERE name=$n";
            cmd.Parameters.AddWithValue("$n", bucket);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new BucketConfiguration
                {
                    EnableDeduplication = r.GetInt32(0) != 0,
                    ObjectLockEnabled = r.GetInt32(1) != 0,
                    DefaultRetentionDays = r.GetInt32(2),
                    StorageClass = r.GetString(3),
                    SmbShareName = r.IsDBNull(4) ? "" : r.GetString(4)
                };
            }
            return new BucketConfiguration(); // defaults if not found
        }

        public async Task<List<string>> ListBucketsAsync()
        {
            var list = new List<string>();
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM buckets ORDER BY name";
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0));
            return list;
        }

        public async Task<ObjectMetadata?> GetLatestAsync(string bucket, string key)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = @"
SELECT v.version_id,v.physical_path,v.size,v.content_type,v.md5_hex,v.sha256_hex,v.sha512_hex,
       v.is_dedup,v.storage_class,v.created_utc,v.deleted_utc,v.lock_mode,v.lock_retain_until_utc,v.lock_legal_hold
FROM objects o JOIN versions v ON v.version_id=o.latest_version_id
WHERE o.bucket=$b AND o.key=$k";
            cmd.Parameters.AddWithValue("$b", bucket);
            cmd.Parameters.AddWithValue("$k", key);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return new ObjectMetadata
            {
                Bucket = bucket,
                Key = key,
                VersionId = r.GetString(0),
                PhysicalPath = r.GetString(1),
                Size = r.GetInt64(2),
                ContentType = r.GetString(3),
                MD5 = r.GetString(4),
                SHA256 = r.GetString(5),
                SHA512 = r.GetString(6),
                IsDeduplicated = r.GetInt32(7) != 0,
                StorageClass = r.GetString(8),
                Created = DateTimeOffset.Parse(r.GetString(9)),
                Deleted = r.IsDBNull(10) ? null : DateTimeOffset.Parse(r.GetString(10)),
                ObjectLock = new ObjectLockInfo
                {
                    Mode = r.GetString(11),
                    RetainUntil = r.IsDBNull(12) ? null : DateTimeOffset.Parse(r.GetString(12)),
                    LegalHold = r.GetInt32(13) != 0
                }
            };
        }

        public async Task UpsertLatestAsync(ObjectMetadata meta)
        {
            await using var tx = await Conn.BeginTransactionAsync();

            await using (var v = Conn.CreateCommand())
            {
                v.Transaction = tx;
                v.CommandText = @"
INSERT INTO versions(version_id,bucket,key,physical_path,size,content_type,md5_hex,sha256_hex,sha512_hex,is_dedup,storage_class,created_utc,deleted_utc,lock_mode,lock_retain_until_utc,lock_legal_hold)
VALUES($vid,$b,$k,$p,$sz,$ct,$m5,$s2,$s5,$dd,$sc,$cu,NULL,$lm,$lr,$lh)";
                v.Parameters.AddWithValue("$vid", meta.VersionId);
                v.Parameters.AddWithValue("$b", meta.Bucket);
                v.Parameters.AddWithValue("$k", meta.Key);
                v.Parameters.AddWithValue("$p", meta.PhysicalPath);
                v.Parameters.AddWithValue("$sz", meta.Size);
                v.Parameters.AddWithValue("$ct", meta.ContentType);
                v.Parameters.AddWithValue("$m5", meta.MD5);
                v.Parameters.AddWithValue("$s2", meta.SHA256);
                v.Parameters.AddWithValue("$s5", meta.SHA512);
                v.Parameters.AddWithValue("$dd", meta.IsDeduplicated ? 1 : 0);
                v.Parameters.AddWithValue("$sc", meta.StorageClass);
                v.Parameters.AddWithValue("$cu", meta.Created.UtcDateTime.ToString("O"));
                v.Parameters.AddWithValue("$lm", meta.ObjectLock?.Mode ?? "NONE");
                v.Parameters.AddWithValue("$lr", meta.ObjectLock?.RetainUntil?.UtcDateTime.ToString("O"));
                v.Parameters.AddWithValue("$lh", meta.ObjectLock?.LegalHold == true ? 1 : 0);
                await v.ExecuteNonQueryAsync();
            }

            await using (var o = Conn.CreateCommand())
            {
                o.Transaction = tx;
                o.CommandText = @"
INSERT INTO objects(bucket,key,latest_version_id) VALUES($b,$k,$v)
ON CONFLICT(bucket,key) DO UPDATE SET latest_version_id=$v";
                o.Parameters.AddWithValue("$b", meta.Bucket);
                o.Parameters.AddWithValue("$k", meta.Key);
                o.Parameters.AddWithValue("$v", meta.VersionId);
                await o.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task SoftDeleteLatestAsync(string bucket, string key, DateTimeOffset when)
        {
            await using var tx = await Conn.BeginTransactionAsync();
            string? vid;
            await using (var q = Conn.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "SELECT latest_version_id FROM objects WHERE bucket=$b AND key=$k";
                q.Parameters.AddWithValue("$b", bucket);
                q.Parameters.AddWithValue("$k", key);
                vid = (string?)await q.ExecuteScalarAsync();
            }
            if (vid == null) { await tx.RollbackAsync(); return; }

            await using (var u = Conn.CreateCommand())
            {
                u.Transaction = tx;
                u.CommandText = "UPDATE versions SET deleted_utc=$d WHERE version_id=$v";
                u.Parameters.AddWithValue("$d", when.UtcDateTime.ToString("O"));
                u.Parameters.AddWithValue("$v", vid);
                await u.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }

        public async Task AppendCustodyEntryAsync(string bucket, string key, string versionId, CustodyEntry entry)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO custody(bucket,key,version_id,action,user,ts_utc,details)
VALUES($b,$k,$v,$a,$u,$t,$d)";
            cmd.Parameters.AddWithValue("$b", bucket);
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", versionId);
            cmd.Parameters.AddWithValue("$a", entry.Action);
            cmd.Parameters.AddWithValue("$u", entry.User);
            cmd.Parameters.AddWithValue("$t", entry.Timestamp.UtcDateTime.ToString("O"));
            cmd.Parameters.AddWithValue("$d", entry.Details);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CreateMultipartAsync(MultipartUpload up)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO mp_uploads(upload_id,bucket,key,initiated_utc) VALUES($i,$b,$k,$t)";
            cmd.Parameters.AddWithValue("$i", up.UploadId);
            cmd.Parameters.AddWithValue("$b", up.Bucket);
            cmd.Parameters.AddWithValue("$k", up.Key);
            cmd.Parameters.AddWithValue("$t", up.Initiated.UtcDateTime.ToString("O"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<MultipartUpload?> GetMultipartAsync(string uploadId)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = "SELECT bucket,key,initiated_utc FROM mp_uploads WHERE upload_id=$i";
            cmd.Parameters.AddWithValue("$i", uploadId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return new MultipartUpload
            {
                UploadId = uploadId,
                Bucket = r.GetString(0),
                Key = r.GetString(1),
                Initiated = DateTimeOffset.Parse(r.GetString(2))
            };
        }

        public async Task UpsertPartAsync(UploadPart part)
        {
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO mp_parts(upload_id,part_num,size,md5_hex,path)
VALUES($i,$n,$s,$m,$p)";
            cmd.Parameters.AddWithValue("$i", part.UploadId);
            cmd.Parameters.AddWithValue("$n", part.PartNumber);
            cmd.Parameters.AddWithValue("$s", part.Size);
            cmd.Parameters.AddWithValue("$m", part.Md5Hex);
            cmd.Parameters.AddWithValue("$p", part.Path);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<UploadPart>> ListPartsAsync(string uploadId)
        {
            var list = new List<UploadPart>();
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = "SELECT part_num,size,md5_hex,path FROM mp_parts WHERE upload_id=$i ORDER BY part_num";
            cmd.Parameters.AddWithValue("$i", uploadId);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new UploadPart
                {
                    UploadId = uploadId,
                    PartNumber = r.GetInt32(0),
                    Size = r.GetInt64(1),
                    Md5Hex = r.GetString(2),
                    Path = r.GetString(3)
                });
            }
            return list;
        }

        public async Task DeleteMultipartAsync(string uploadId)
        {
            await using var tx = await Conn.BeginTransactionAsync();
            await using (var a = Conn.CreateCommand())
            {
                a.Transaction = tx;
                a.CommandText = "DELETE FROM mp_parts WHERE upload_id=$i";
                a.Parameters.AddWithValue("$i", uploadId);
                await a.ExecuteNonQueryAsync();
            }
            await using (var b = Conn.CreateCommand())
            {
                b.Transaction = tx;
                b.CommandText = "DELETE FROM mp_uploads WHERE upload_id=$i";
                b.Parameters.AddWithValue("$i", uploadId);
                await b.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }

        public async Task<IEnumerable<ObjectMetadata>> ListObjectsAsync(string bucket, string prefix)
        {
            var list = new List<ObjectMetadata>();
            await using var cmd = Conn.CreateCommand();
            cmd.CommandText = @"
SELECT v.version_id,v.key,v.size,v.storage_class,v.created_utc,v.md5_hex
FROM objects o JOIN versions v ON v.version_id=o.latest_version_id
WHERE o.bucket=$b AND v.deleted_utc IS NULL AND v.key LIKE $p || '%'
ORDER BY v.key";
            cmd.Parameters.AddWithValue("$b", bucket);
            cmd.Parameters.AddWithValue("$p", prefix ?? "");
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new ObjectMetadata
                {
                    Bucket = bucket,
                    Key = r.GetString(1),
                    Size = r.GetInt64(2),
                    StorageClass = r.GetString(3),
                    Created = DateTimeOffset.Parse(r.GetString(4)),
                    MD5 = r.GetString(5),
                    VersionId = r.GetString(0)
                });
            }
            return list;
        }
    }

    // ---------- Event bus (stub) ----------
    public sealed class LoggingEventBus : IEventBus
    {
        private readonly ILogger<LoggingEventBus> _log;
        public LoggingEventBus(ILogger<LoggingEventBus> log) => _log = log;

        public Task PublishAsync(string topic, object payload)
        {
            _log.LogInformation("Event[{topic}] {payload}", topic, System.Text.Json.JsonSerializer.Serialize(payload));
            return Task.CompletedTask;
        }
    }

    // ---------- S3 service (core orchestration) ----------
    public sealed class S3Service : IS3Service
    {
        private readonly IStorageBackend _storage;
        private readonly IMetadataStore _meta;
        private readonly IDeduplicationService _dedup;
        private readonly IPresignedUrlService _presign;
        private readonly ISmbShareService _smb;
        private readonly IPolicyEngine _policy;
        private readonly IHashingService _hash;
        private readonly IEventBus _bus;
        private readonly IClock _clock;

        public S3Service(IStorageBackend storage, IMetadataStore meta, IDeduplicationService dedup,
                         IPresignedUrlService presign, ISmbShareService smb, IPolicyEngine policy,
                         IHashingService hash, IEventBus bus, IClock clock)
        {
            _storage = storage; _meta = meta; _dedup = dedup; _presign = presign;
            _smb = smb; _policy = policy; _hash = hash; _bus = bus; _clock = clock;
        }

        public async Task CreateBucketAsync(string bucket, BucketConfiguration config)
        {
            await _meta.CreateBucketAsync(bucket, config);
            await _storage.CreateBucketDirectoryAsync(bucket);
            await _smb.CreateShareAsync(bucket, string.IsNullOrWhiteSpace(config.SmbShareName) ? bucket : config.SmbShareName);
        }

        public Task<string> GeneratePresignedUrlAsync(string bucket, string key, string operation, int expirySeconds)
            => _presign.GenerateAsync(new PresignedUrlRequest { Bucket = bucket, Key = key, Operation = operation, ExpirySeconds = expirySeconds, UserId = "system" });

        public async Task<ObjectMetadata> PutObjectAsync(string bucket, string key, HttpRequest request)
        {
            key = KeySanitizer.SafeKey(key);

            var userMeta = request.Headers
                .Where(h => h.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var route = await _policy.DetermineRoutingAsync(bucket, key, userMeta);
            if (route.RequiresQuarantine)
            {
                userMeta["x-original-bucket"] = route.OriginalBucket;
                userMeta["x-quarantine-reason"] = route.Reason;
                bucket = "quarantine";
            }

            var tmp = Path.Combine(Path.GetTempPath(), $"put-{Guid.NewGuid():N}.tmp");
            var hb = await _hash.HashAndWriteAsync(request.Body, tmp);

            var cfg = await _meta.GetBucketConfigAsync(bucket);

            // WORM overwrite guard
            var existing = await _meta.GetLatestAsync(bucket, key);
            if (existing?.ObjectLock != null)
            {
                var bypass = request.Headers.TryGetValue("X-Camille-Bypass-Governance", out var b) && b == "true";
                var allow = bypass && await _policy.CanBypassGovernanceAsync("system", bucket, key);
                if (existing.ObjectLock.IsLocked(_clock.UtcNow, allow) && !allow)
                    throw new InvalidOperationException("Object under retention/legal hold");
            }

            string path;
            bool dedup = false;
            if (cfg.EnableDeduplication && !bucket.Equals("quarantine", StringComparison.OrdinalIgnoreCase))
            {
                var casHit = await _dedup.GetPathByHashAsync(hb.Sha256Hex);
                if (casHit != null) { path = casHit; File.Delete(tmp); dedup = true; }
                else path = await _dedup.PutCasAsync(tmp, hb.Sha256Hex);
            }
            else
            {
                path = await _storage.MoveToFinalLocationAsync(bucket, key, tmp);
            }

            var meta = new ObjectMetadata
            {
                Bucket = bucket,
                Key = key,
                PhysicalPath = path,
                Size = hb.Bytes,
                ContentType = request.ContentType ?? "application/octet-stream",
                MD5 = hb.Md5Hex,
                SHA256 = hb.Sha256Hex,
                SHA512 = hb.Sha512Hex,
                IsDeduplicated = dedup,
                StorageClass = cfg.StorageClass,
                Created = _clock.UtcNow,
                ObjectLock = cfg.ObjectLockEnabled
                    ? new ObjectLockInfo { Mode = "GOVERNANCE", RetainUntil = _clock.UtcNow.AddDays(cfg.DefaultRetentionDays) }
                    : new ObjectLockInfo { Mode = "NONE" }
            };

            await _meta.UpsertLatestAsync(meta);
            await _meta.AppendCustodyEntryAsync(bucket, key, meta.VersionId, new CustodyEntry { Action = "Created", User = "system", Timestamp = _clock.UtcNow, Details = "PUT" });
            await _smb.CreateSymlinkAsync(bucket, key, path);
            await _bus.PublishAsync("ingest.received", new { bucket, key, size = hb.Bytes, sha256 = hb.Sha256Hex, dedup });

            return meta;
        }

        public async Task GetObjectAsync(string bucket, string key, HttpRequest req, HttpResponse res)
        {
            key = KeySanitizer.SafeKey(key);
            var meta = await _meta.GetLatestAsync(bucket, key) ?? throw new FileNotFoundException();

            res.Headers.ETag = $"\"{meta.MD5}\"";
            res.Headers["x-amz-version-id"] = meta.VersionId;
            res.Headers["Accept-Ranges"] = "bytes";
            res.Headers["Last-Modified"] = meta.Created.UtcDateTime.ToString("R");
            res.ContentType = meta.ContentType;

            if (req.Headers.TryGetValue("Range", out var rng) && TryParseRange(rng!, meta.Size, out var from, out var to))
            {
                var length = (to - from) + 1;
                res.StatusCode = StatusCodes.Status206PartialContent;
                res.Headers["Content-Range"] = $"bytes {from}-{to}/{meta.Size}";
                res.ContentLength = length;

                using var fs = await _storage.OpenReadAsync(meta.PhysicalPath);
                fs.Seek(from, SeekOrigin.Begin);
                await CopyExactlyAsync(fs, res.Body, length);
            }
            else
            {
                res.StatusCode = StatusCodes.Status200OK;
                res.ContentLength = meta.Size;
                using var fs = await _storage.OpenReadAsync(meta.PhysicalPath);
                await fs.CopyToAsync(res.Body);
            }

            await _meta.AppendCustodyEntryAsync(bucket, key, meta.VersionId, new CustodyEntry { Action = "Accessed", User = "system", Timestamp = _clock.UtcNow, Details = "GET" });
        }

        public async Task HeadObjectAsync(string bucket, string key, HttpResponse res)
        {
            key = KeySanitizer.SafeKey(key);
            var meta = await _meta.GetLatestAsync(bucket, key) ?? throw new FileNotFoundException();
            res.Headers.ETag = $"\"{meta.MD5}\"";
            res.Headers["x-amz-version-id"] = meta.VersionId;
            res.Headers["Accept-Ranges"] = "bytes";
            res.Headers["Last-Modified"] = meta.Created.UtcDateTime.ToString("R");
            res.ContentType = meta.ContentType;
            res.ContentLength = meta.Size;
            res.StatusCode = StatusCodes.Status200OK;
        }

        public async Task<bool> DeleteObjectAsync(string bucket, string key, HttpRequest req)
        {
            key = KeySanitizer.SafeKey(key);
            var meta = await _meta.GetLatestAsync(bucket, key);
            if (meta == null) return false;

            var bypass = req.Headers.TryGetValue("X-Camille-Bypass-Governance", out var b) && b == "true";
            var allow = bypass && await _policy.CanBypassGovernanceAsync("system", bucket, key);
            if (meta.ObjectLock != null && meta.ObjectLock.IsLocked(DateTimeOffset.UtcNow, allow) && !allow)
                throw new InvalidOperationException("Object under retention/legal hold");

            await _meta.SoftDeleteLatestAsync(bucket, key, DateTimeOffset.UtcNow);
            await _meta.AppendCustodyEntryAsync(bucket, key, meta.VersionId, new CustodyEntry { Action = "SoftDeleted", User = "system", Timestamp = DateTimeOffset.UtcNow, Details = "DELETE" });
            await _smb.RemoveSymlinkAsync(bucket, key);
            return true;
        }

        public async Task<string> InitiateMultipartUploadAsync(string bucket, string key)
        {
            key = KeySanitizer.SafeKey(key);
            var up = new MultipartUpload { UploadId = Guid.NewGuid().ToString("N"), Bucket = bucket, Key = key, Initiated = DateTimeOffset.UtcNow };
            await _meta.CreateMultipartAsync(up);
            return up.UploadId;
        }

        public async Task<string> UploadPartAsync(string uploadId, int partNumber, HttpRequest req)
        {
            var up = await _meta.GetMultipartAsync(uploadId) ?? throw new InvalidOperationException("Upload not found");
            var (path, md5Hex, size) = await _storage.StorePartAsync(uploadId, partNumber, req.Body);
            await _meta.UpsertPartAsync(new UploadPart { UploadId = uploadId, PartNumber = partNumber, Md5Hex = md5Hex, Size = size, Path = path });
            return $"\"{md5Hex}\""; // S3-style quoted part ETag
        }

        public async Task<(ObjectMetadata meta, string multipartEtag)> CompleteMultipartUploadAsync(string uploadId, IEnumerable<(int PartNumber, string ETag)> parts)
        {
            var up = await _meta.GetMultipartAsync(uploadId) ?? throw new InvalidOperationException("Upload not found");
            var stored = await _meta.ListPartsAsync(uploadId);
            var map = stored.ToDictionary(p => p.PartNumber);

            foreach (var p in parts)
            {
                if (!map.TryGetValue(p.PartNumber, out var s)) throw new InvalidOperationException($"Missing part {p.PartNumber}");
                if (!string.Equals($"\"{s.Md5Hex}\"", p.ETag, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Part ETag mismatch {p.PartNumber}");
            }

            var tempOut = Path.Combine(Path.GetTempPath(), $"mp-{uploadId}-{Guid.NewGuid():N}.tmp");
            await _storage.CombinePartsAsync(uploadId, parts.OrderBy(p => p.PartNumber).Select(p => map[p.PartNumber]), tempOut);

            using var combined = File.OpenRead(tempOut);
            var rehash = await _hash.HashAndWriteAsync(combined, tempOut + ".rehash.tmp");
            File.Delete(tempOut);

            var cfg = await _meta.GetBucketConfigAsync(up.Bucket);

            // WORM on overwrite
            var existing = await _meta.GetLatestAsync(up.Bucket, up.Key);
            if (existing?.ObjectLock != null && existing.ObjectLock.IsLocked(DateTimeOffset.UtcNow, false))
                throw new InvalidOperationException("Object under retention/legal hold");

            string path;
            bool dedup = false;
            var hit = cfg.EnableDeduplication ? await _dedup.GetPathByHashAsync(rehash.Sha256Hex) : null;
            if (hit != null) { path = hit; File.Delete(tempOut + ".rehash.tmp"); dedup = true; }
            else if (cfg.EnableDeduplication) path = await _dedup.PutCasAsync(tempOut + ".rehash.tmp", rehash.Sha256Hex);
            else path = await _storage.MoveToFinalLocationAsync(up.Bucket, up.Key, tempOut + ".rehash.tmp");

            var meta = new ObjectMetadata
            {
                Bucket = up.Bucket,
                Key = up.Key,
                PhysicalPath = path,
                Size = rehash.Bytes,
                ContentType = "application/octet-stream",
                MD5 = rehash.Md5Hex,
                SHA256 = rehash.Sha256Hex,
                SHA512 = rehash.Sha512Hex,
                IsDeduplicated = dedup,
                StorageClass = cfg.StorageClass,
                Created = DateTimeOffset.UtcNow,
                ObjectLock = cfg.ObjectLockEnabled
                    ? new ObjectLockInfo { Mode = "GOVERNANCE", RetainUntil = DateTimeOffset.UtcNow.AddDays(cfg.DefaultRetentionDays) }
                    : new ObjectLockInfo { Mode = "NONE" }
            };

            await _meta.UpsertLatestAsync(meta);
            await _meta.AppendCustodyEntryAsync(up.Bucket, up.Key, meta.VersionId, new CustodyEntry { Action = "Created", User = "system", Timestamp = DateTimeOffset.UtcNow, Details = "CompleteMultipartUpload" });
            await _meta.DeleteMultipartAsync(uploadId);
            await _storage.CleanupPartsAsync(uploadId);
            await _smb.CreateSymlinkAsync(up.Bucket, up.Key, path);

            // FIX: multipart ETag parity with S3
            var mpEtag = ComputeMultipartEtag(stored);
            return (meta, mpEtag);
        }

        public async Task<ListingResult> ListObjectsAsync(string bucket, string prefix, string? delimiter)
        {
            var objs = (await _meta.ListObjectsAsync(bucket, prefix)).ToList();
            if (!string.IsNullOrEmpty(delimiter))
            {
                var result = new ListingResult();
                var set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var o in objs)
                {
                    var rest = o.Key[(prefix?.Length ?? 0)..];
                    var i = rest.IndexOf(delimiter, StringComparison.Ordinal);
                    if (i > 0) set.Add(prefix + rest[..(i + delimiter.Length)]);
                    else result.Objects.Add(new ListedObject { Key = o.Key, Size = o.Size, LastModified = o.Created, ETag = $"\"{o.MD5}\"", StorageClass = o.StorageClass });
                }
                result.CommonPrefixes = set.OrderBy(s => s).ToList();
                return result;
            }
            return new ListingResult
            {
                Objects = objs.Select(o => new ListedObject
                {
                    Key = o.Key,
                    Size = o.Size,
                    LastModified = o.Created,
                    ETag = $"\"{o.MD5}\"",
                    StorageClass = o.StorageClass
                }).ToList()
            };
        }

        private static string ComputeMultipartEtag(List<UploadPart> parts)
        {
            using var md5 = MD5.Create();
            foreach (var p in parts.OrderBy(p => p.PartNumber))
            {
                var bytes = Convert.FromHexString(p.Md5Hex);
                md5.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return $"{Convert.ToHexString(md5.Hash!).ToLowerInvariant()}-{parts.Count}";
        }

        private static bool TryParseRange(string header, long total, out long from, out long to)
        {
            from = 0; to = total - 1;
            if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("bytes=")) return false;
            var parts = header["bytes=".Length..].Split('-', 2);
            if (!long.TryParse(parts[0], out from)) return false;
            to = (parts.Length > 1 && long.TryParse(parts[1], out var t)) ? t : (total - 1);
            if (from < 0 || to < from || to >= total) return false;
            return true;
        }
        private static async Task CopyExactlyAsync(Stream src, Stream dst, long bytes)
        {
            var buf = ArrayPool<byte>.Shared.Rent(64 * 1024);
            try
            {
                long left = bytes;
                while (left > 0)
                {
                    var toRead = (int)Math.Min(buf.Length, left);
                    var read = await src.ReadAsync(buf.AsMemory(0, toRead));
                    if (read <= 0) break;
                    await dst.WriteAsync(buf.AsMemory(0, read));
                    left -= read;
                }
            }
            finally { ArrayPool<byte>.Shared.Return(buf); }
        }
    }

    // ---------- Background services ----------
    public sealed class SmbShareSyncService : BackgroundService
    {
        private readonly ISmbShareService _smb;
        private readonly IMetadataStore _meta;
        private readonly ILogger<SmbShareSyncService> _log;

        public SmbShareSyncService(ISmbShareService smb, IMetadataStore meta, ILogger<SmbShareSyncService> log)
        { _smb = smb; _meta = meta; _log = log; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var buckets = await _meta.ListBucketsAsync();
                    foreach (var b in buckets)
                    {
                        var objs = await _meta.ListObjectsAsync(b, "");
                        foreach (var o in objs.Where(o => o.Deleted == null))
                        {
                            try { await _smb.CreateSymlinkAsync(o.Bucket, o.Key, o.PhysicalPath); }
                            catch (Exception ex) { _log.LogWarning(ex, "Symlink refresh failed for {b}/{k}", o.Bucket, o.Key); }
                        }
                    }
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "SMB sync loop error");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
        }
    }

    public sealed class ObjectLockEnforcer : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Main enforcement happens inline on writes/deletes. This worker can be used for audits.
            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

}
