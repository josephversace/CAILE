using System;

namespace IIM.Shared.Models
{
    public class EvidenceContext
    {
        public string CaseId { get; set; } = string.Empty;
        public string EvidenceId { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;
        public DateTime CollectedAt { get; set; }
        public string ChainOfCustody { get; set; } = string.Empty;
    }
}
