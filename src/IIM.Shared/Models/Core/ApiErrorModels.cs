using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    public class ApiError
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public List<ValidationError>? Errors { get; set; }
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
