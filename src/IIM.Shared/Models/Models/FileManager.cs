using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class WorkspaceFolder
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

    }

    public record FileReference(string Id, string Name, string Path, long Size, string StorageKey);
    public record FolderReference(string Id, string Name, string Path);
    // Base file item model
    public class FileItem
        {
            public string Id { get; set; } // Unique identifier (could be S3 ETag or custom)
            public string Name { get; set; }
            public string VirtualPath { get; set; } // User-friendly path like /Documents/Reports
            public string Extension { get; set; }
            public FileItemType Type { get; set; }
            public long Size { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime ModifiedDate { get; set; }
            public string MimeType { get; set; }
            public string Owner { get; set; }

            // Hidden S3 metadata (not shown to users)
            public string InternalBucket { get; set; }
            public string InternalKey { get; set; }
            public string ETag { get; set; }
            public string StorageClass { get; set; }
        }

        public enum FileItemType
        {
            File,
            Folder,
            VirtualFolder // For organizing S3 flat structure
        }

        // Classification metadata
        public class ClassificationMetadata
        {
            public string FileId { get; set; }
            public DataClassificationLevel Level { get; set; }
            public List<string> Tags { get; set; } = new();
            public string Description { get; set; }
            public string AIGeneratedSummary { get; set; }
            public float? ConfidenceScore { get; set; }
            public ReviewStatus Status { get; set; }
            public DateTime? ClassifiedDate { get; set; }
            public string ClassifiedBy { get; set; }
            public DateTime? ReviewedDate { get; set; }
            public string ReviewedBy { get; set; }
            public List<ComplianceFlag> ComplianceFlags { get; set; } = new();
            public Dictionary<string, object> CustomMetadata { get; set; } = new();
        }

        public enum DataClassificationLevel
        {
            Unclassified,
            Public,
            Internal,
            Confidential,
            Restricted,
            TopSecret
        }

        public enum ReviewStatus
        {
            Pending,
            InReview,
            Approved,
            Rejected,
            RequiresManualReview
        }

        public enum ComplianceFlag
        {
            None,
            PII,
            PHI,
            PCI,
            GDPR,
            HIPAA,
            SOX,
            Financial,
            Legal
        }

    #region FileRequests

 
        public class GetFilesRequest
        {
            public string Path { get; set; }
            public int PageSize { get; set; } = 100;
            public string ContinuationToken { get; set; }
            public FileSortOrder SortOrder { get; set; } = FileSortOrder.NameAsc;
            public FileFilterOptions Filters { get; set; }
        }

        public class FileFilterOptions
        {
            public List<DataClassificationLevel> Classifications { get; set; }
            public List<string> Extensions { get; set; }
            public DateTime? ModifiedAfter { get; set; }
            public DateTime? ModifiedBefore { get; set; }
            public long? MinSize { get; set; }
            public long? MaxSize { get; set; }
            public List<string> Tags { get; set; }
            public ReviewStatus? Status { get; set; }
        }

        public enum FileSortOrder
        {
            NameAsc,
            NameDesc,
            DateAsc,
            DateDesc,
            SizeAsc,
            SizeDesc,
            ClassificationAsc,
            ClassificationDesc
        }

        public class FileUploadRequest
        {
            public string FileName { get; set; }
            public string Path { get; set; }
            public string ContentType { get; set; }
            public long ContentLength { get; set; }
            public DataClassificationLevel? InitialClassification { get; set; }
            public List<string> InitialTags { get; set; }
        }

        public class BulkClassificationRequest
        {
            public List<string> FileIds { get; set; }
            public DataClassificationLevel Classification { get; set; }
            public List<string> Tags { get; set; }
            public string Reason { get; set; }
            public bool UseAI { get; set; } = true;
        }

        public class AIAnalysisRequest
        {
            public string FileId { get; set; }
            public List<FileAnalysisType> RequestedAnalyses { get; set; }
        }

        public enum FileAnalysisType
        {
            Classification,
            Description,
            Tags,
            Entities,
            Sentiment,
            Compliance,
            All
        }



    #endregion

    #region FileListResponses
    public class FileListResponse
    {
        public string CurrentPath { get; set; }
        public List<FileItem> Files { get; set; }
        public List<FileItem> Folders { get; set; }
        public int TotalCount { get; set; }
        public string ContinuationToken { get; set; }
        public bool HasMore { get; set; }
        public FileStatistics Statistics { get; set; }
    }

    public class FileStatistics
    {
        public int TotalFiles { get; set; }
        public int ClassifiedFiles { get; set; }
        public int PendingReview { get; set; }
        public Dictionary<DataClassificationLevel, int> ClassificationBreakdown { get; set; }
        public long TotalSize { get; set; }
    }

    public class AIAnalysisResponse
    {
        public string FileId { get; set; }
        public DataClassificationLevel SuggestedClassification { get; set; }
        public float ConfidenceScore { get; set; }
        public string GeneratedDescription { get; set; }
        public List<string> SuggestedTags { get; set; }
        public List<DetectedEntity> Entities { get; set; }
        public List<ComplianceFlag> DetectedCompliance { get; set; }
        public Dictionary<string, object> AdditionalInsights { get; set; }
    }

    public class DetectedEntity
    {
        public string Type { get; set; } // Person, Organization, Location, etc.
        public string Value { get; set; }
        public float Confidence { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
    }

    public class ChatResponse
    {
        public string MessageId { get; set; }
        public string Response { get; set; }
        public List<string> SuggestedActions { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }

    #endregion


    #region Classification

    public class ClassifiableFile : FileItem
    {
        // Inherits all FileItem properties
        // Add classification-specific properties
        public ClassificationMetadata Classification { get; set; }
        public bool IsClassified => Classification != null;
        public bool RequiresReview => Classification?.Status == ReviewStatus.RequiresManualReview;
    }

    // Missing: FileManagerEntry<T> (core component from FluentUI)
    public class FileManagerEntry<T> where T : class
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public T Data { get; set; }
        public List<FileManagerEntry<T>> Children { get; set; } = new();
        public FileManagerEntry<T> Parent { get; set; }

        // For S3 abstraction
        public string VirtualPath { get; set; }
        public long Size { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    // Missing: BulkClassificationItem
    public class BulkClassificationItem
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public DataClassificationLevel? RequestedLevel { get; set; }
        public List<string> RequestedTags { get; set; }
        public ClassificationStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public enum ClassificationStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    // Missing: Event Args Classes
    public class FileSelectionChangedEventArgs : EventArgs
    {
        public IEnumerable<FileManagerEntry<ClassifiableFile>> SelectedItems { get; set; }
        public IEnumerable<string> SelectedIds { get; set; }
        public int SelectionCount { get; set; }
    }

    public class DirectoryChangedEventArgs : EventArgs
    {
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public FileManagerEntry<ClassifiableFile> Directory { get; set; }
        public int ItemCount { get; set; }
    }

    public class ClassificationUpdatedEventArgs : EventArgs
    {
        public string FileId { get; set; }
        public ClassificationMetadata OldClassification { get; set; }
        public ClassificationMetadata NewClassification { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Missing: ClassificationData (referenced in FileManagerState)
    public class ClassificationData
    {
        public string FileId { get; set; }
        public DataClassificationLevel Level { get; set; }
        public List<string> Tags { get; set; }
        public float? ConfidenceScore { get; set; }
        public DateTime ClassifiedAt { get; set; }
        public bool IsAIGenerated { get; set; }
    }


    #endregion

    #region Responses

    // Missing: FileUploadResponse
    public class FileUploadResponse
    {
        public List<FileItem> UploadedFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; }
        public Dictionary<string, string> FileIdMap { get; set; } // filename -> fileId
    }

    // Missing: BulkClassificationResponse
    public class BulkClassificationResponse
    {
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ClassificationResult> Results { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    // Missing: ClassificationResult
    public class ClassificationResult
    {
        public string FileId { get; set; }
        public bool Success { get; set; }
        public ClassificationMetadata Classification { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    // Missing: ClassificationUpdate
    public class ClassificationUpdate
    {
        public string FileId { get; set; }
        public DataClassificationLevel Level { get; set; }
        public List<string> Tags { get; set; }
        public string Description { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdateReason { get; set; }
    }

    // Missing: Tree structure responses
    public class TreeStructureResponse
    {
        public List<TreeNode> Nodes { get; set; }
        public int TotalFolders { get; set; }
    }

    public class TreeNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentPath { get; set; }
        public bool HasChildren { get; set; }
        public bool IsExpanded { get; set; }
        public List<TreeNode> Children { get; set; }
        public int ItemCount { get; set; }
    }

    // Missing: Search response
    public class SearchResponse
    {
        public string Query { get; set; }
        public string SearchPath { get; set; }
        public List<FileItem> Results { get; set; }
        public int TotalResults { get; set; }
        public TimeSpan SearchTime { get; set; }
        public Dictionary<string, int> FacetCounts { get; set; } // e.g., by classification level
    }

    // Missing: Move/Copy responses
    public class MoveResponse
    {
        public List<string> MovedFileIds { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    // Missing: Delete response
    public class DeleteResponse
    {
        public List<string> DeletedFileIds { get; set; }
        public int DeletedCount { get; set; }
        public List<string> FailedDeletes { get; set; }
        public bool Success { get; set; }
    }


    #endregion 

}
