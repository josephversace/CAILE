using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
// IIM.Core/Storage/IMinIOStorageService.cs
namespace IIM.Shared.Interfaces
{
    public interface IS3StorageService
    {
        Task<bool> CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default);
        Task<string> PutObjectAsync(string bucketName, string objectName, Stream data,
      
        Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
        Task<Stream> GetObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
        Task<bool> DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
        Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    }
}

