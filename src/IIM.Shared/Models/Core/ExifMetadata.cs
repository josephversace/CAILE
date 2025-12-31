using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{

	public sealed class NormalizedExifMetadata
	{
		public ExifGpsMetadata? Gps { get; init; }
		public ExifCameraMetadata? Camera { get; init; }
		public ExifSoftwareMetadata? Software { get; init; }
		public ExifDateMetadata? Dates { get; init; }
	}

	public sealed class ExifGpsMetadata
	{
		public double Latitude { get; init; }
		public double Longitude { get; init; }
		public double? AltitudeMeters { get; init; }
		public DateTimeOffset? TimestampUtc { get; init; }
	}

	public sealed class ExifCameraMetadata
	{
		public string? Make { get; init; }
		public string? Model { get; init; }
		public string? SerialNumber { get; init; }
		public string? Lens { get; init; }
	}

	public sealed class ExifSoftwareMetadata
	{
		public string? CreatorTool { get; init; }
		public string? Software { get; init; }
		public string? OperatingSystem { get; init; }
	}

	public sealed class ExifDateMetadata
	{
		public DateTimeOffset? DateTimeOriginal { get; init; }
		public DateTimeOffset? CreateDate { get; init; }
		public DateTimeOffset? ModifyDate { get; init; }
	}

}
