using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
	/// <summary>
	/// Provides the live Foundry Local model-management service URL.
	/// 
	/// Responsibilities:
	///   • Return configured override if present.
	///   • If not, auto-detect the Foundry service port using:
	///       `foundry service status`
	///   • Never cache results (optional TTL caching can be added later).
	///   • Always return the root URL (e.g., http://127.0.0.1:55986).
	/// </summary>
	public interface IFoundryEndpointProvider
	{
		/// <summary>
		/// Gets the current Foundry base URL.
		/// This must return only the root:
		///   http://127.0.0.1:PORT
		/// Not /v1, not /openai/status.
		/// </summary>
		string GetBaseUrl();

		/// <summary>
		/// Checks if the Foundry service is online by calling:
		///   GET {baseUrl}/openai/status
		/// </summary>
		Task<bool> IsOnlineAsync(CancellationToken ct = default);

		/// <summary>
		/// Forces a redetection on next call.
		/// (May be no-op when caching disabled.)
		/// </summary>
		void Reset();
	}
}
