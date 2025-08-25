using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Contract for managing ONNX Runtime inference sessions and execution.
    /// </summary>
    public interface IOnnxRuntimeManager : IDisposable
    {
        /// <summary>
        /// Creates or retrieves a cached ONNX inference session for the specified model and execution provider.
        /// </summary>
        Task<InferenceSession> CreateSessionAsync(string modelPath, ExecutionProvider provider);

        /// <summary>
        /// Runs inference on the given session using provided named inputs.
        /// </summary>
        Task<IDisposableReadOnlyCollection<DisposableNamedOnnxValue>> RunAsync(
            InferenceSession session,
            IEnumerable<NamedOnnxValue> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets input metadata for the given session.
        /// </summary>
        IReadOnlyDictionary<string, NodeMetadata> GetInputMetadata(InferenceSession session);

        /// <summary>
        /// Gets output metadata for the given session.
        /// </summary>
        IReadOnlyDictionary<string, NodeMetadata> GetOutputMetadata(InferenceSession session);

        /// <summary>
        /// Utility to create a NamedOnnxValue from a raw data tensor.
        /// </summary>
        NamedOnnxValue CreateTensor<T>(string name, T[] data, int[] dimensions) where T : unmanaged;

        /// <summary>
        /// Preprocesses raw input data into model-ready NamedOnnxValues according to model type.
        /// </summary>
        Task<List<NamedOnnxValue>> PreprocessInputAsync(
            InferenceSession session,
            object rawInput,
            ModelType modelType);

        /// <summary>
        /// Converts model output to user-facing structure according to model type.
        /// </summary>
        Task<object> PostprocessOutputAsync(
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs,
            ModelType modelType);
    }


}
