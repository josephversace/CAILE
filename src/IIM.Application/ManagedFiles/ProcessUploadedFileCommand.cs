// IIM.Application/Files/ProcessUploadedFileCommand.cs
using Hangfire;
using IIM.Application.AI.DataEnrichment;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Files
{
    public class ProcessUploadedFileCommand : IRequest<ProcessUploadedFileResult>
    {
        public string BucketName { get; init; }
        public string ObjectKey { get; init; }
        public long FileSize { get; init; }
        public DateTime EventTime { get; init; }
    }

    public class ProcessUploadedFileResult
    {
        public string JobId { get; init; }
        public string Status { get; init; }
    }

    public class ProcessUploadedFileCommandHandler : IRequestHandler<ProcessUploadedFileCommand, ProcessUploadedFileResult>
    {
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<ProcessUploadedFileCommandHandler> _logger;

        public ProcessUploadedFileCommandHandler(
            IBackgroundJobClient backgroundJobs,
            ILogger<ProcessUploadedFileCommandHandler> logger)
        {
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public Task<ProcessUploadedFileResult> Handle(ProcessUploadedFileCommand command, CancellationToken ct)
        {
            // Queue to DataEnrichmentOrchestrator's new method
            var jobId = _backgroundJobs.Enqueue<DataEnrichmentOrchestrator>(
                orchestrator => orchestrator.ProcessFileFromStorageAsync(
                    command.BucketName,
                    command.ObjectKey,
                    command.FileSize
                ));

            _logger.LogInformation(
                "Queued processing job {JobId} for {Bucket}/{Key}",
                jobId, command.BucketName, command.ObjectKey);

            return Task.FromResult(new ProcessUploadedFileResult
            {
                JobId = jobId,
                Status = "Queued"
            });
        }
    }
}