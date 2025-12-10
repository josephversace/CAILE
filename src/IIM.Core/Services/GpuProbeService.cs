using System;
using System.IO;

namespace IIM.Core.Services.Gpu
{
	public class GpuProbeService : IGpuProbeService
	{
		public bool HasCuda =>
			OperatingSystem.IsWindows() &&
			File.Exists("C:\\Windows\\System32\\nvcuda.dll");

		public bool HasDirectML =>
			OperatingSystem.IsWindows() &&
			File.Exists("C:\\Windows\\System32\\DirectML.dll");

		public bool HasMetal =>
			OperatingSystem.IsMacOS();
	}
}
