namespace IIM.Core.Services.Gpu
{
	public interface IGpuProbeService
	{
		bool HasCuda { get; }
		bool HasDirectML { get; }
		bool HasMetal { get; }
	}
}
