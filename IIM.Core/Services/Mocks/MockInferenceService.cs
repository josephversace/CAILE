using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IIM.Core.Interfaces;
using IIM.Core.Models;

namespace IIM.Core.Services.Mocks
{
    /// <summary>
    /// Mock implementation of IInferenceService for UI development
    /// Returns realistic fake data without requiring GPU or actual models
    /// </summary>
    public class MockInferenceService : IInferenceService
    {
        private readonly Random _random = new();
        private readonly ILogger<MockInferenceService> _logger;

        public MockInferenceService(ILogger<MockInferenceService> logger)
        {
            _logger = logger;
            _logger.LogInformation("MockInferenceService initialized - Development Mode");
        }

        /// <summary>
        /// Simulates audio transcription with realistic mock data
        /// </summary>
        public async Task<TranscriptionResult> TranscribeAudioAsync(string audioPath, string language = "en")
        {
            _logger.LogDebug("Mock transcribing audio: {AudioPath}", audioPath);
            
            // Simulate processing delay (300-800ms)
            await Task.Delay(_random.Next(300, 800));

            // Generate mock transcription based on file name for consistency
            var mockText = GenerateMockTranscription(audioPath);

            return new TranscriptionResult
            {
                Text = mockText,
                Language = language,
                Confidence = 0.85f + (float)(_random.NextDouble() * 0.14),
                Duration = TimeSpan.FromMinutes(_random.Next(1, 10)),
                Words = GenerateMockWords(mockText),
                ProcessingTime = TimeSpan.FromMilliseconds(_random.Next(300, 800)),
                DeviceUsed = "CPU (Mock Mode)"
            };
        }

        /// <summary>
        /// Simulates CLIP image search with mock results
        /// </summary>
        public async Task<ImageSearchResults> SearchImagesAsync(byte[] imageData, int topK = 5)
        {
            _logger.LogDebug("Mock image search for top {TopK} results", topK);
            
            // Simulate processing delay
            await Task.Delay(_random.Next(200, 500));

            var matches = new List<ImageMatch>();
            for (int i = 0; i < topK; i++)
            {
                matches.Add(new ImageMatch
                {
                    ImagePath = $"/mock-images/match_{i + 1:D3}.jpg",
                    Score = 0.95f - (i * 0.05f),
                    BoundingBox = _random.Next(100) > 50 ? new BoundingBox
                    {
                        X = _random.Next(100, 400),
                        Y = _random.Next(100, 300),
                        Width = _random.Next(50, 200),
                        Height = _random.Next(50, 200)
                    } : null,
                    Metadata = new Dictionary<string, string>
                    {
                        ["CaseId"] = $"CASE-2024-{_random.Next(1000, 9999)}",
                        ["Timestamp"] = DateTime.Now.AddDays(-_random.Next(1, 30)).ToString("yyyy-MM-dd HH:mm:ss"),
                        ["Location"] = $"Camera-{_random.Next(1, 20)}",
                        ["Confidence"] = $"{(0.95f - (i * 0.05f)):P0}"
                    }
                });
            }

            return new ImageSearchResults
            {
                Matches = matches,
                QueryProcessingTime = TimeSpan.FromMilliseconds(_random.Next(200, 500)),
                TotalImagesSearched = _random.Next(10000, 50000)
            };
        }

        /// <summary>
        /// Simulates RAG document query with mock response
        /// </summary>
        public async Task<RagResponse> QueryDocumentsAsync(string query, string collection = "default")
        {
            _logger.LogDebug("Mock RAG query: {Query} in collection: {Collection}", query, collection);
            
            // Simulate longer processing for RAG
            await Task.Delay(_random.Next(500, 1500));

            return new RagResponse
            {
                Answer = GenerateMockRagAnswer(query),
                Sources = GenerateMockSources(),
                Confidence = 0.75f + (float)(_random.NextDouble() * 0.20),
                TokensUsed = _random.Next(500, 2000),
                ProcessingTime = TimeSpan.FromMilliseconds(_random.Next(500, 1500))
            };
        }

        /// <summary>
        /// Reports GPU availability (always false in mock mode)
        /// </summary>
        public Task<bool> IsGpuAvailable() => Task.FromResult(false);

        /// <summary>
        /// Returns mock device information
        /// </summary>
        public Task<DeviceInfo> GetDeviceInfo() => Task.FromResult(new DeviceInfo
        {
            DeviceType = "CPU",
            DeviceName = "Mock Development Environment",
            MemoryAvailable = 8_000_000_000,  // 8GB
            MemoryTotal = 16_000_000_000,     // 16GB
            SupportsDirectML = false,
            SupportsROCm = false
        });

        // ===== Helper Methods =====

        private string GenerateMockTranscription(string audioPath)
        {
            var templates = new[]
            {
                "The suspect was observed entering the building at approximately 14:30 hours. They proceeded directly to the elevator bank and went to the third floor.",
                "Witness statement: I saw the individual near the north entrance between 2:00 PM and 2:15 PM. They were wearing a dark jacket and appeared to be waiting for someone.",
                "Interview transcript: The subject denied any involvement in the incident. They claim to have been at home during the time in question.",
                "Emergency call recording: Caller reported suspicious activity at the location. Multiple individuals were seen leaving the scene quickly.",
                "Surveillance audio: Voices detected in the corridor at 03:45. Unable to identify speakers due to background noise."
            };

            return templates[Math.Abs(audioPath.GetHashCode()) % templates.Length];
        }

        private Word[] GenerateMockWords(string text)
        {
            var words = text.Split(' ');
            var result = new Word[Math.Min(words.Length, 10)]; // First 10 words
            
            float currentTime = 0;
            for (int i = 0; i < result.Length; i++)
            {
                var duration = (float)(_random.NextDouble() * 0.5 + 0.2);
                result[i] = new Word
                {
                    Text = words[i],
                    Start = currentTime,
                    End = currentTime + duration
                };
                currentTime += duration + 0.1f; // Add small gap
            }

            return result;
        }

        private string GenerateMockRagAnswer(string query)
        {
            return $"Based on the analysis of documents in the collection, regarding '{query}': " +
                   "The evidence suggests multiple data points converging on this conclusion. " +
                   "Primary sources indicate activity during the specified timeframe, " +
                   "with corroborating evidence from witness statements and digital records. " +
                   "Further investigation recommended to verify these findings.";
        }

        private Source[] GenerateMockSources()
        {
            return new[]
            {
                new Source 
                { 
                    Document = "witness_statement_001.pdf", 
                    Page = _random.Next(1, 10), 
                    Relevance = 0.92f 
                },
                new Source 
                { 
                    Document = "surveillance_log_2024.xlsx", 
                    Page = 1, 
                    Relevance = 0.88f 
                },
                new Source 
                { 
                    Document = "case_notes_investigation.docx", 
                    Page = _random.Next(5, 15), 
                    Relevance = 0.85f 
                }
            };
        }
    }
}
