using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Pluggable content analysis service that can be configured per client
    /// Supports different AI models and analysis techniques
    /// </summary>
    public interface IContentAnalysisService
    {
        /// <summary>
        /// Analyzes content using configured analysis pipeline
        /// </summary>
        Task<ContentAnalysisResult> AnalyzeAsync(Stream content, string fileName, string mimeType, AnalysisOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available analysis capabilities for current configuration
        /// </summary>
        Task<AnalysisCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates analysis configuration (models, thresholds, etc.)
        /// </summary>
        Task UpdateConfigurationAsync(AnalysisConfiguration configuration, CancellationToken cancellationToken = default);
    }
}
