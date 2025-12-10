using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Service responsible for classifying files based on content and governance rules
    /// </summary>
    public interface IFileClassificationService
    {
        Task<ClassificationSuggestion> SuggestClassificationAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
        Task<List<string>> ClassifyContentAsync(string textContent, IEnumerable<ClassificationTag> availableClassifications, CancellationToken cancellationToken = default);
        Task<DataSensitivityLevel> DetermineSensitivityAsync(string textContent, CancellationToken cancellationToken = default);
    }
}
