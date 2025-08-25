using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.IO;

namespace IIM.Infrastructure.Models;
public static class ModelConfigurationExtractor
{
    public static ModelConfiguration ExtractModelConfiguration(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("Model file not found", modelPath);

        var extension = Path.GetExtension(modelPath).ToLowerInvariant();

        return extension switch
        {
            ".onnx" => ExtractFromOnnx(modelPath),
        
            _ => throw new NotSupportedException($"Model format '{extension}' is not supported")
        };
    }

    private static ModelConfiguration ExtractFromOnnx(string modelPath)
    {
        using var session = new InferenceSession(modelPath);
        var meta = session.ModelMetadata;

        var config = new ModelConfiguration
        {
            ModelId = meta.ProducerName ?? Path.GetFileNameWithoutExtension(modelPath),
            Name = meta.GraphName ?? Path.GetFileName(modelPath),
            Provider = "ONNX",
            Type = ModelType.Unknown,
            Status = ModelStatus.Unknown,
            ModelPath = modelPath,
            LoadedPath = modelPath,
            Properties = new Dictionary<string, object>(),
            Parameters = new Dictionary<string, object>(),
            Capabilities = new ModelCapabilities()
        };

        config.Properties["producer_version"] = meta.ProducerName;
        config.Properties["domain"] = meta.Domain;
        config.Properties["model_version"] = meta.Version.ToString();
        config.Properties["description"] = meta.Description;

        foreach (var kv in meta.CustomMetadataMap)
            config.Properties[kv.Key] = kv.Value;

        config.Properties["input_count"] = session.InputMetadata.Count;
        config.Properties["output_count"] = session.OutputMetadata.Count;

        return config;
    }

    //private static ModelConfiguration ExtractFromGguf(string modelPath)
    //{
    //    using var fs = File.OpenRead(modelPath);

    //    // Try LLama.GGUF.GGUFFile. If it doesn't exist, you'll need to update your package.
    //    var gguf = GGUFFile.ReadFromStream(fs);

    //    var config = new ModelConfiguration
    //    {
    //        ModelId = gguf.MetaData.TryGetValue("general.name", out var nameObj) ? nameObj?.ToString() ?? "" : Path.GetFileNameWithoutExtension(modelPath),
    //        Provider = "GGUF",
    //        Type = ModelType.Unknown,
    //        Status = ModelStatus.Unknown,
    //        ModelPath = modelPath,
    //        LoadedPath = modelPath,
    //        Metadata = new Dictionary<string, object>(),
    //        Parameters = new Dictionary<string, object>(),
    //        Capabilities = new ModelCapabilities()
    //    };

    //    foreach (var kv in gguf.MetaData)
    //    {
    //        config.Metadata[kv.Key] = kv.Value?.ToString() ?? "";
    //    }

    //    return config;
    //}
}
