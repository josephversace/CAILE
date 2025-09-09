using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IIM.Shared.Interfaces;

public interface IObjectStorageProvider
{
    Task<string> GetPresignedUploadUrlAsync(string bucketName, string objectKey, TimeSpan expiry);
    Task<string> GetPresignedDownloadUrlAsync(string bucketName, string objectKey, TimeSpan expiry);
    Task DeleteObjectAsync(string bucketName, string objectKey);
}
