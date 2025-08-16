using IIM.Core.Models;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace IIM.Core.Services
{
    // API Client belongs in Core as it's business logic
    public interface IIimClient
    {
        Task<string> GetStatusAsync();
        Task<bool> ConnectAsync(string endpoint);
        Task DisconnectAsync();

        Task<bool> HealthAsync();
        Task<GpuInfo> GetGpuInfoAsync();
        Task<T> GenerateAsync<T>(string modelId, object input);
        Task StopAllAsync();


        /// <summary>
        /// Upload and index browser files for RAG (Retrieval Augmented Generation)
        /// </summary>
        /// <param name="files">List of browser files to index</param>
        /// <param name="sentenceMode">Whether to index by sentences or paragraphs</param>
        /// <returns>True if successful</returns>
        Task<bool> RagUploadAsync(List<IBrowserFile> files, bool sentenceMode = false);

        /// <summary>
        /// Query the RAG system with a question
        /// </summary>
        /// <param name="query">The question to ask</param>
        /// <param name="k">Number of relevant chunks to retrieve</param>
        /// <returns>Tuple with answer and citations</returns>
        Task<(string answer, List<RAGDocument> citations)> RagQueryAsync(string query, int k = 5);
    }

    public class IimClient : IIimClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IimClient> _logger;

        public IimClient(HttpClient httpClient, ILogger<IimClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetStatusAsync()
        {
            var response = await _httpClient.GetStringAsync("/api/status");
            return response;
        }

        public async Task<bool> ConnectAsync(string endpoint)
        {
            _httpClient.BaseAddress = new Uri(endpoint);
            return true;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<bool> HealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<GpuInfo> GetGpuInfoAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<GpuInfo>("/v1/gpu");
            return response ?? new GpuInfo();
        }

        public async Task<T> GenerateAsync<T>(string modelId, object input)
        {
            var response = await _httpClient.PostAsJsonAsync("/v1/generate", new { modelId, input });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result!;
        }

        public async Task StopAllAsync()
        {
            await _httpClient.PostAsync("/v1/models/stop-all", null);
        }

        /// <summary>
        /// Upload and index browser files for RAG
        /// </summary>
        public async Task<bool> RagUploadAsync(List<IBrowserFile> files, bool sentenceMode = false)
        {
            try
            {
                _logger.LogInformation("Uploading {Count} files for RAG indexing", files.Count);

                using var content = new MultipartFormDataContent();

                // Add each file to the multipart form
                foreach (var file in files)
                {
                    // Limit file size to 50MB per file
                    var maxFileSize = 50 * 1024 * 1024;

                    try
                    {
                        using var fileStream = file.OpenReadStream(maxAllowedSize: maxFileSize);
                        var fileContent = new StreamContent(fileStream);

                        // Set content type if available
                        if (!string.IsNullOrEmpty(file.ContentType))
                        {
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                        }

                        // Add to multipart form with field name "files"
                        content.Add(fileContent, "files", file.Name);

                        _logger.LogDebug("Added file {FileName} ({Size} bytes) to upload",
                            file.Name, file.Size);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process file {FileName}", file.Name);
                        throw;
                    }
                }

                // Add sentence mode flag as form field
                content.Add(new StringContent(sentenceMode.ToString().ToLower()), "sentenceMode");

                // Add collection name (default)
                content.Add(new StringContent("default"), "collection");

                // Send the request
                var response = await _httpClient.PostAsync("api/rag/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully indexed {Count} files", files.Count);
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to index files. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading files for RAG");
                return false;
            }
        }

        /// <summary>
        /// Query the RAG system - uses EXISTING RAGSearchResult
        /// </summary>
        public async Task<(string answer, List<RAGDocument> citations)> RagQueryAsync(string query, int k = 5)
        {
            try
            {
                _logger.LogInformation("Querying RAG with k={K}: {Query}", k, query);

                var request = new
                {
                    Query = query,
                    TopK = k,
                    Collection = "default"
                };

                var response = await _httpClient.PostAsJsonAsync("api/rag/query", request);

                if (response.IsSuccessStatusCode)
                {
                    // Use EXISTING RAGSearchResult model
                    var result = await response.Content.ReadFromJsonAsync<RAGSearchResult>();

                    if (result != null)
                    {
                        // Generate answer from the search results
                        var answer = GenerateAnswerFromResults(result);

                        // Return answer and use existing RAGDocument as citations
                        return (answer, result.Documents);
                    }
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("RAG query failed. Status: {Status}, Error: {Error}",
                    response.StatusCode, error);
                return ("Unable to process query at this time.", new List<RAGDocument>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying RAG");
                return ("An error occurred while processing your query.", new List<RAGDocument>());
            }
        }

        /// <summary>
        /// Generate a coherent answer from RAG search results
        /// </summary>
        private string GenerateAnswerFromResults(RAGSearchResult result)
        {
            if (!result.Documents.Any())
            {
                return "No relevant information found for your query.";
            }

            // In production, this would use an LLM to synthesize the answer
            // For now, create a comprehensive answer from the results
            var topDocs = result.Documents
                .OrderByDescending(d => d.Relevance)
                .Take(3)
                .ToList();

            var answer = new StringBuilder();
            answer.AppendLine($"Based on {topDocs.Count} relevant documents:");
            answer.AppendLine();

            foreach (var doc in topDocs)
            {
                // Extract key content
                var summary = doc.Content.Length > 200
                    ? doc.Content.Substring(0, 200) + "..."
                    : doc.Content;

                answer.AppendLine($"• {summary}");

                // Add source info if available
                if (!string.IsNullOrEmpty(doc.SourceId))
                {
                    answer.AppendLine($"  Source: {doc.SourceType ?? "Document"} #{doc.SourceId}");
                }
                answer.AppendLine();
            }

            // Add entities if found
            if (result.Entities?.Any() == true)
            {
                answer.AppendLine($"Key entities identified: {string.Join(", ", result.Entities.Take(5).Select(e => e.Name))}");
                answer.AppendLine();
            }

            // Add suggested follow-ups if available
            if (result.SuggestedFollowUps?.Any() == true)
            {
                answer.AppendLine($"Suggested follow-up queries:");
                foreach (var followUp in result.SuggestedFollowUps.Take(3))
                {
                    answer.AppendLine($"• {followUp}");
                }
            }

            return answer.ToString();
        }
    }
}
