namespace IIM.S3.Models
{
    using System.Text.Json.Serialization;
    using IIM.S3.Models; // <-- adjust to where CompletePart is defined
    using System.Collections.Generic;

    // 👇 This class is picked up by the source generator at build time.
    // You only need ONE context per project, but you can add multiple [JsonSerializable] attributes.
    [JsonSerializable(typeof(List<CompletePart>))]
    [JsonSerializable(typeof(CompletePart))] // optional: for single instances
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class ApiJsonContext : JsonSerializerContext
    {
    }

}
