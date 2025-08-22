using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Desktop.Services.Http
{
    /// <summary>
    /// HTTP delegating handler that tracks upload progress for large file transfers.
    /// This handler sits in the HTTP pipeline and monitors bytes being sent to the server.
    /// Used primarily for evidence uploads to MinIO to provide real-time progress to the UI.
    /// </summary>
    public class SimpleProgressHandler : DelegatingHandler
    {
        /// <summary>
        /// Event raised periodically during HTTP upload to report progress.
        /// Subscribe to this event to update UI progress bars or status messages.
        /// </summary>
        public event EventHandler<HttpProgressEventArgs>? HttpSendProgress;

        /// <summary>
        /// Buffer size for reading the request stream in chunks.
        /// Smaller = more frequent updates but more overhead.
        /// Larger = fewer updates but less responsive progress bar.
        /// </summary>
        private const int BufferSize = 8192; // 8KB chunks

        /// <summary>
        /// Intercepts HTTP requests to track upload progress.
        /// </summary>
        /// <param name="request">The HTTP request being sent</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The HTTP response from the server</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Only track progress for requests with content (uploads)
            if (request.Content == null)
            {
                // No content to track, just pass through
                return await base.SendAsync(request, cancellationToken);
            }

            // Try to get the content length for progress calculation
            var totalBytes = request.Content.Headers.ContentLength;

            // If we have content but no progress event subscribers, just pass through
            if (HttpSendProgress == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Create a progress-tracking stream wrapper
            var progressContent = new ProgressStreamContent(
                request.Content,
                BufferSize,
                (bytesTransferred) =>
                {
                    // Calculate percentage if we know the total size
                    var percentage = 0;
                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        percentage = (int)((bytesTransferred * 100) / totalBytes.Value);
                    }

                    // Raise the progress event
                    HttpSendProgress?.Invoke(this, new HttpProgressEventArgs
                    {
                        BytesTransferred = bytesTransferred,
                        TotalBytes = totalBytes,
                        ProgressPercentage = percentage
                    });
                });

            // Replace the request content with our progress-tracking wrapper
            request.Content = progressContent;

            // Send the request with progress tracking
            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Internal class that wraps HTTP content to track bytes being sent.
        /// This is what actually monitors the stream as it's being uploaded.
        /// </summary>
        private class ProgressStreamContent : HttpContent
        {
            private readonly HttpContent _innerContent;
            private readonly int _bufferSize;
            private readonly Action<long> _progress;

            /// <summary>
            /// Initializes a new progress-tracking content wrapper.
            /// </summary>
            /// <param name="innerContent">The actual content being uploaded</param>
            /// <param name="bufferSize">Size of buffer for reading chunks</param>
            /// <param name="progress">Callback invoked with bytes transferred</param>
            public ProgressStreamContent(HttpContent innerContent, int bufferSize, Action<long> progress)
            {
                _innerContent = innerContent;
                _bufferSize = bufferSize;
                _progress = progress;

                // Copy headers from inner content
                foreach (var header in innerContent.Headers)
                {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            /// <summary>
            /// Serializes the HTTP content to a stream with progress tracking.
            /// This is called by the HTTP client when sending the request.
            /// </summary>
            /// <param name="stream">The network stream to write to</param>
            /// <param name="context">Transport context (unused)</param>
            /// <returns>Task representing the async operation</returns>
            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                // Get the source stream from the inner content
                using var inputStream = await _innerContent.ReadAsStreamAsync();

                var buffer = new byte[_bufferSize];
                long totalBytesTransferred = 0;
                int bytesRead;

                // Read from source and write to destination in chunks
                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    // Write chunk to output stream
                    await stream.WriteAsync(buffer, 0, bytesRead);

                    // Update progress
                    totalBytesTransferred += bytesRead;
                    _progress?.Invoke(totalBytesTransferred);
                }
            }

            /// <summary>
            /// Attempts to calculate the length of the HTTP content.
            /// </summary>
            /// <param name="length">Receives the content length if known</param>
            /// <returns>True if length is known, false otherwise</returns>
            protected override bool TryComputeLength(out long length)
            {
                length = _innerContent.Headers.ContentLength ?? -1;
                return length >= 0;
            }

            /// <summary>
            /// Disposes of the progress content wrapper and inner content.
            /// </summary>
            /// <param name="disposing">True if disposing managed resources</param>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _innerContent?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// Event arguments for HTTP upload progress events.
    /// Provides information about the current state of an upload operation.
    /// </summary>
    public class HttpProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the number of bytes that have been transferred so far.
        /// Use this to show absolute progress (e.g., "1.5 MB of 10 MB uploaded").
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Gets or sets the total number of bytes to be transferred.
        /// Will be null if the total size is unknown (e.g., chunked transfer encoding).
        /// </summary>
        public long? TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the progress as a percentage (0-100).
        /// Will be 0 if TotalBytes is unknown.
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Gets a human-readable string describing the current progress.
        /// Example: "1.5 MB / 10 MB (15%)"
        /// </summary>
        public string GetProgressString()
        {
            if (!TotalBytes.HasValue || TotalBytes.Value == 0)
            {
                return $"{FormatBytes(BytesTransferred)} uploaded";
            }

            return $"{FormatBytes(BytesTransferred)} / {FormatBytes(TotalBytes.Value)} ({ProgressPercentage}%)";
        }

        /// <summary>
        /// Formats bytes into human-readable string (KB, MB, GB).
        /// </summary>
        /// <param name="bytes">Number of bytes to format</param>
        /// <returns>Formatted string like "1.5 MB"</returns>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}