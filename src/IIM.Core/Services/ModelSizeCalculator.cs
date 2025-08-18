using IIM.Shared.Enums;
using System.Text.RegularExpressions;

namespace IIM.Core.Services;

public class ModelSizeCalculator
{
    private static readonly Dictionary<ModelSize, long> SizeMap = new()
    {
        { ModelSize.Tiny, 100L * 1024 * 1024 },
        { ModelSize.Small, 500L * 1024 * 1024 },
        { ModelSize.Base, 1L * 1024 * 1024 * 1024 },
        { ModelSize.Medium, 2L * 1024 * 1024 * 1024 },
        { ModelSize.Large, 5L * 1024 * 1024 * 1024 },
        { ModelSize.XLarge, 10L * 1024 * 1024 * 1024 },
        { ModelSize.XXLarge, 20L * 1024 * 1024 * 1024 }
    };

    private static readonly Dictionary<ModelQuantization, float> QuantizationMultipliers = new()
    {
        { ModelQuantization.Q4_0, 0.35f },
        { ModelQuantization.Q4_K_M, 0.4f },
        { ModelQuantization.Q5_K_M, 0.5f },
        { ModelQuantization.Q8_0, 0.8f },
        { ModelQuantization.F16, 1.0f },
        { ModelQuantization.F32, 2.0f }
    };

    public ModelSize InferModelSize(string modelId)
    {
        var lower = modelId.ToLowerInvariant();

        // Check explicit size indicators
        if (lower.Contains("tiny")) return ModelSize.Tiny;
        if (lower.Contains("small")) return ModelSize.Small;
        if (lower.Contains("base")) return ModelSize.Base;
        if (lower.Contains("medium")) return ModelSize.Medium;
        if (lower.Contains("large") && !lower.Contains("xlarge")) return ModelSize.Large;
        if (lower.Contains("xxl") || lower.Contains("xxlarge")) return ModelSize.XXLarge;
        if (lower.Contains("xl") || lower.Contains("xlarge")) return ModelSize.XLarge;

        // Check parameter count
        var match = Regex.Match(lower, @"(\d+)b");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var billions))
        {
            return billions switch
            {
                <= 1 => ModelSize.Tiny,
                <= 3 => ModelSize.Small,
                <= 7 => ModelSize.Medium,
                <= 13 => ModelSize.Large,
                <= 30 => ModelSize.XLarge,
                _ => ModelSize.XXLarge
            };
        }

        return ModelSize.Medium; // Default
    }

    public ModelQuantization InferQuantization(string modelId)
    {
        var lower = modelId.ToLowerInvariant();

        if (lower.Contains("q4_0")) return ModelQuantization.Q4_0;
        if (lower.Contains("q4_k_m")) return ModelQuantization.Q4_K_M;
        if (lower.Contains("q5_k_m")) return ModelQuantization.Q5_K_M;
        if (lower.Contains("q8_0")) return ModelQuantization.Q8_0;
        if (lower.Contains("f16")) return ModelQuantization.F16;
        if (lower.Contains("f32")) return ModelQuantization.F32;

        return ModelQuantization.Q4_K_M; // Default for efficiency
    }

    public long CalculateMemoryUsage(ModelSize size, ModelQuantization quantization)
    {
        var baseSize = SizeMap[size];
        var multiplier = QuantizationMultipliers[quantization];
        return (long)(baseSize * multiplier);
    }

    public long EstimateModelMemory(string modelId)
    {
        var size = InferModelSize(modelId);
        var quantization = InferQuantization(modelId);
        return CalculateMemoryUsage(size, quantization);
    }
}
