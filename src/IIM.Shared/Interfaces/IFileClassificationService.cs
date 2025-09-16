using IIM.Shared.Models;
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
    /// Industry-agnostic file classification service
    /// Classifications are driven by client-specific governance framework
    /// </summary>
    public interface IFileClassificationService
    {
        /// <summary>
        /// Classifies file based on client-defined classification taxonomy
        /// </summary>
        Task<ClassificationResult> ClassifyAsync(string fileName, Stream content, Guid? workspaceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available classification tags for a workspace/client
        /// </summary>
        Task<IEnumerable<ClassificationTag>> GetAvailableClassificationsAsync(Guid? workspaceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates classification rules based on client feedback
        /// </summary>
        Task UpdateClassificationRulesAsync(ClassificationFeedback feedback, CancellationToken cancellationToken = default);
    }

}
