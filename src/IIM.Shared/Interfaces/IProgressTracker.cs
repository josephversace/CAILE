using IIM.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Tracks progress of inference requests
    /// </summary>
    public interface IProgressTracker
    {
        void UpdateProgress(string requestId, InferenceProgressUpdate update);
        InferenceProgressUpdate? GetProgress(string requestId);
        Dictionary<string, InferenceProgressUpdate> GetAllProgress();
        void RemoveProgress(string requestId);
    }




}