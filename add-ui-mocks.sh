#!/bin/bash

# ============================================================================
# Add Mock Services and UI Components to Existing IIM Project
# Purpose: Adds only the new mock services and UI components for local development
# Usage: ./add-ui-mocks.sh
# Prerequisites: Run from IIM project root (where IIM.sln exists)
# ============================================================================

set -e

# Color output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Adding Mock Services and UI Components to IIM Project${NC}"

# ============================================================================
# 1. Create Mock Services in IIM.Core
# ============================================================================
echo -e "${YELLOW}Creating Mock Services...${NC}"

# Create Interfaces directory if it doesn't exist
mkdir -p IIM.Core/Interfaces

# Create IInferenceService interface
cat > IIM.Core/Interfaces/IInferenceService.cs << 'EOF'
using System;
using System.Threading.Tasks;
using IIM.Core.Models;

namespace IIM.Core.Interfaces
{
    /// <summary>
    /// Interface for AI inference operations
    /// Implemented by MockInferenceService (dev) and GpuInferenceService (prod)
    /// </summary>
    public interface IInferenceService
    {
        Task<TranscriptionResult> TranscribeAudioAsync(string audioPath, string language = "en");
        Task<ImageSearchResults> SearchImagesAsync(byte[] imageData, int topK = 5);
        Task<RagResponse> QueryDocumentsAsync(string query, string collection = "default");
        Task<bool> IsGpuAvailable();
        Task<DeviceInfo> GetDeviceInfo();
    }
}
EOF

# Create Models for the mock responses
mkdir -p IIM.Core/Models

cat > IIM.Core/Models/InferenceModels.cs << 'EOF'
using System;
using System.Collections.Generic;

namespace IIM.Core.Models
{
    /// <summary>
    /// Result from audio transcription using Whisper model
    /// </summary>
    public class TranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public float Confidence { get; set; }
        public TimeSpan Duration { get; set; }
        public Word[] Words { get; set; } = Array.Empty<Word>();
        public TimeSpan ProcessingTime { get; set; }
        public string DeviceUsed { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual word with timing information
    /// </summary>
    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public float Start { get; set; }
        public float End { get; set; }
    }

    /// <summary>
    /// Results from CLIP image search
    /// </summary>
    public class ImageSearchResults
    {
        public List<ImageMatch> Matches { get; set; } = new();
        public TimeSpan QueryProcessingTime { get; set; }
        public int TotalImagesSearched { get; set; }
    }

    /// <summary>
    /// Individual image match result
    /// </summary>
    public class ImageMatch
    {
        public string ImagePath { get; set; } = string.Empty;
        public float Score { get; set; }
        public BoundingBox? BoundingBox { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Bounding box for detected objects
    /// </summary>
    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Response from RAG (Retrieval Augmented Generation) query
    /// </summary>
    public class RagResponse
    {
        public string Answer { get; set; } = string.Empty;
        public Source[] Sources { get; set; } = Array.Empty<Source>();
        public float Confidence { get; set; }
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Document source for RAG response
    /// </summary>
    public class Source
    {
        public string Document { get; set; } = string.Empty;
        public int Page { get; set; }
        public float Relevance { get; set; }
    }

    /// <summary>
    /// GPU/Device information
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public long MemoryAvailable { get; set; }
        public long MemoryTotal { get; set; }
        public bool SupportsDirectML { get; set; }
        public bool SupportsROCm { get; set; }
    }
}
EOF

# Create Mock Services directory
mkdir -p IIM.Core/Services/Mocks

# Create MockInferenceService
cat > IIM.Core/Services/Mocks/MockInferenceService.cs << 'EOF'
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
EOF

# ============================================================================
# 2. Create Investigation Page Component
# ============================================================================
echo -e "${YELLOW}Creating Investigation UI Page...${NC}"

mkdir -p IIM.Desktop/Pages

cat > IIM.Desktop/Pages/Investigation.razor << 'EOF'
@page "/investigation"
@using IIM.Core.Interfaces
@using IIM.Core.Models
@using Microsoft.AspNetCore.Components.Forms
@inject IInferenceService InferenceService
@inject IJSRuntime JS

<div class="investigation-container">
    <h3>Investigation Dashboard</h3>
    
    <!-- System Status Card -->
    <div class="row mb-3">
        <div class="col-12">
            <div class="card">
                <div class="card-header">
                    <h5>System Status</h5>
                </div>
                <div class="card-body">
                    @if (deviceInfo != null)
                    {
                        <span class="badge bg-info">@deviceInfo.DeviceType</span>
                        <span class="ms-2">@deviceInfo.DeviceName</span>
                    }
                </div>
            </div>
        </div>
    </div>

    <!-- Audio Transcription Card -->
    <div class="row mb-3">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Audio Transcription</h5>
                </div>
                <div class="card-body">
                    <InputFile OnChange="@LoadAudioFile" accept="audio/*" class="form-control mb-2" />
                    
                    @if (audioFile != null)
                    {
                        <button class="btn btn-primary" @onclick="TranscribeAudio" disabled="@isProcessing">
                            @if (isProcessing)
                            {
                                <span class="spinner-border spinner-border-sm me-2"></span>
                            }
                            Transcribe
                        </button>
                    }
                    
                    @if (transcription != null)
                    {
                        <div class="mt-3 p-3 bg-light rounded">
                            <small class="text-muted">Confidence: @transcription.Confidence.ToString("P0")</small>
                            <p class="mt-2">@transcription.Text</p>
                        </div>
                    }
                </div>
            </div>
        </div>

        <!-- Image Search Card -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Image Search (CLIP)</h5>
                </div>
                <div class="card-body">
                    <InputFile OnChange="@LoadImageFile" accept="image/*" class="form-control mb-2" />
                    
                    @if (imageFile != null)
                    {
                        <button class="btn btn-primary" @onclick="SearchImages" disabled="@isProcessing">
                            Search Similar
                        </button>
                    }
                    
                    @if (imageResults != null)
                    {
                        <div class="mt-3">
                            <small class="text-muted">Found @imageResults.Matches.Count matches</small>
                            @foreach (var match in imageResults.Matches.Take(3))
                            {
                                <div class="p-2 border-bottom">
                                    <span class="badge bg-success">@match.Score.ToString("P0")</span>
                                    <span class="ms-2">@match.Metadata["CaseId"]</span>
                                </div>
                            }
                        </div>
                    }
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private IBrowserFile? audioFile;
    private IBrowserFile? imageFile;
    private bool isProcessing = false;
    private DeviceInfo? deviceInfo;
    private TranscriptionResult? transcription;
    private ImageSearchResults? imageResults;

    protected override async Task OnInitializedAsync()
    {
        // Get device info on load
        deviceInfo = await InferenceService.GetDeviceInfo();
    }

    private void LoadAudioFile(InputFileChangeEventArgs e)
    {
        audioFile = e.File;
    }

    private void LoadImageFile(InputFileChangeEventArgs e)
    {
        imageFile = e.File;
    }

    private async Task TranscribeAudio()
    {
        if (audioFile == null) return;
        
        isProcessing = true;
        try
        {
            // In real app, would save file first
            transcription = await InferenceService.TranscribeAudioAsync(audioFile.Name);
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async Task SearchImages()
    {
        if (imageFile == null) return;
        
        isProcessing = true;
        try
        {
            // In real app, would convert to byte array
            var buffer = new byte[imageFile.Size];
            await imageFile.OpenReadStream().ReadAsync(buffer);
            imageResults = await InferenceService.SearchImagesAsync(buffer, 5);
        }
        finally
        {
            isProcessing = false;
        }
    }
}
EOF

# ============================================================================
# 3. Update Program.cs to use Mock Services in Development
# ============================================================================
echo -e "${YELLOW}Creating Program configuration snippet...${NC}"

cat > add-to-program.cs << 'EOF'
// ============================================================================
// Add this to your Program.cs or MauiProgram.cs in the service configuration section
// This configures dependency injection to use mocks in development
// ============================================================================

#if DEBUG
    // Use mock services for local UI development
    builder.Services.AddSingleton<IInferenceService, MockInferenceService>();
    builder.Services.AddLogging(configure =>
    {
        configure.AddConsole();
        configure.SetMinimumLevel(LogLevel.Debug);
    });
#else
    // Use real GPU services in production
    builder.Services.AddSingleton<IInferenceService, GpuInferenceService>();
    builder.Services.AddLogging(configure =>
    {
        configure.SetMinimumLevel(LogLevel.Information);
    });
#endif
EOF

# ============================================================================
# 4. Add necessary NuGet packages
# ============================================================================
echo -e "${YELLOW}Creating package installation script...${NC}"

cat > install-packages.ps1 << 'EOF'
# Run this PowerShell script to add required NuGet packages

Write-Host "Installing required NuGet packages..." -ForegroundColor Green

# For IIM.Core
Set-Location IIM.Core
dotnet add package Microsoft.Extensions.Logging.Abstractions

# For IIM.Desktop  
Set-Location ../IIM.Desktop
dotnet add package Microsoft.AspNetCore.Components.Web

# Return to root
Set-Location ..

Write-Host "Packages installed successfully!" -ForegroundColor Green
EOF

# ============================================================================
# Summary
# ============================================================================
echo -e "${GREEN}✅ Mock services and UI components added successfully!${NC}"
echo -e "${GREEN}Next steps:${NC}"
echo "1. Run: ${YELLOW}powershell ./install-packages.ps1${NC} to install NuGet packages"
echo "2. Add the code from ${YELLOW}add-to-program.cs${NC} to your Program.cs"
echo "3. Build and run: ${YELLOW}dotnet build && dotnet run --project IIM.Desktop${NC}"
echo ""
echo "Files created:"
echo "  - IIM.Core/Interfaces/IInferenceService.cs"
echo "  - IIM.Core/Models/InferenceModels.cs"
echo "  - IIM.Core/Services/Mocks/MockInferenceService.cs"
echo "  - IIM.Desktop/Pages/Investigation.razor"
echo "  - add-to-program.cs (snippet to add)"
echo "  - install-packages.ps1 (run to install packages)"
