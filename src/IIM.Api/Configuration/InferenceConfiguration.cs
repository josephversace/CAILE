namespace IIM.Api.Configuration
{
    /// <summary>
    /// Configuration for inference pipeline and model orchestration
    /// </summary>
    public class InferenceConfiguration
    {
        public int MaxConcurrentInferences { get; set; } = 3;
        public int DefaultTimeoutSeconds { get; set; } = 60;
        public string ModelCachePath { get; set; } = @"C:\ProgramData\IIM\Models";
        public long MaxMemoryBytes { get; set; } = 120L * 1024 * 1024 * 1024; // 120GB
        public bool EnableGpuAcceleration { get; set; } = true;
        public string DefaultProvider { get; set; } = "DirectML";
    }
}
