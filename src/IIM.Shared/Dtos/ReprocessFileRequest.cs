using System.Collections.Generic;

namespace IIM.Shared.Dtos
{
	/// <summary>
	/// Request to reprocess a file through the ingestion pipeline with optional step selection.
	/// </summary>
	public class ReprocessFileRequest
	{
		/// <summary>
		/// Only run these specific pipeline steps. If empty/null, runs all steps.
		/// </summary>
		public List<string>? OnlySteps { get; set; }

		/// <summary>
		/// Skip these pipeline steps. Ignored if OnlySteps is specified.
		/// </summary>
		public List<string>? SkipSteps { get; set; }

		/// <summary>
		/// Force reprocessing even if artifacts already exist.
		/// </summary>
		public bool Force { get; set; }

		/// <summary>
		/// Step-specific configuration overrides in format "StepId.SettingKey" = "value"
		/// </summary>
		public Dictionary<string, string>? Overrides { get; set; }
	}

	/// <summary>
	/// Response from a reprocess request.
	/// </summary>
	public class ReprocessFileResponse
	{
		/// <summary>
		/// The job ID tracking this reprocessing task.
		/// </summary>
		public string? JobId { get; set; }

		/// <summary>
		/// Whether the job was queued successfully.
		/// </summary>
		public bool Queued { get; set; }

		/// <summary>
		/// Message providing additional context.
		/// </summary>
		public string? Message { get; set; }

		/// <summary>
		/// Steps that will be executed.
		/// </summary>
		public List<string>? Steps { get; set; }
	}
}