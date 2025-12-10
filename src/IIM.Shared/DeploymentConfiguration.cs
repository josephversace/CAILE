namespace IIM.Shared.Configuration
{
	/// <summary>
	/// Defines how the CAILE system operates in this environment.
	/// </summary>
	public class DeploymentConfiguration
	{
        /// <summary>
        /// High-level mode (Local Router, Server Node, UI Client).
        ///</summary>
        public DeploymentMode Mode { get; set; } = DeploymentMode.LocalRouter;

		/// <summary>
		/// True if running in dev environment (enables hot reload, verbose logs).
		/// </summary>
		public bool IsDevelopment { get; set; } = false;

		/// <summary>
		/// Whether authentication is required.
		/// </summary>
		public bool RequireAuth { get; set; } = true;

		/// <summary>
		/// Base API URL for the router/server.
		/// </summary>
		public string ApiUrl { get; set; } = "http://localhost:5080";

		/// <summary>
		/// Admin contact for system provisioning.
		/// </summary>
		public string AdminEmail { get; set; } = "admin@iim.local";

		/// <summary>
		/// Enables/disables user switching of deployment mode.
		/// </summary>
		public bool CanChangeMode { get; set; } = false;


		// -------------------------------------------------------
		// Derived Properties (Mode-Based Feature Flags)
		// -------------------------------------------------------

		public bool IsLocalRouter => Mode == DeploymentMode.LocalRouter;
		public bool IsServerNode => Mode == DeploymentMode.ServerNode;
		public bool IsClientOnly => Mode == DeploymentMode.ClientUI;

		/// <summary>
		/// Local GPU/NPU ONNX execution allowed.
		/// </summary>
		public bool SupportsLocalModels =>
			Mode == DeploymentMode.LocalRouter;

		/// <summary>
		/// Only Server Node runs multi-user workspace syncing.
		/// </summary>
		public bool SupportsMultiUser =>
			Mode == DeploymentMode.ServerNode;

		/// <summary>
		/// Client UI offloads all AI inference to the server/router.
		/// </summary>
		public bool RequiresRemoteInference =>
			Mode == DeploymentMode.ClientUI;

		/// <summary>
		/// Background jobs allowed (Hangfire, dedup, routing).
		/// </summary>
		public bool SupportsBackgroundServices =>
			Mode != DeploymentMode.ClientUI;

		/// <summary>
		/// Configuration editing (AI models, agents, routing policies).
		/// </summary>
		public bool SupportsAdminPanel =>
			Mode == DeploymentMode.LocalRouter || Mode == DeploymentMode.ServerNode;

		/// <summary>
		/// Docling, routing, hashing are only available on node modes.
		/// </summary>
		public bool SupportsDocumentProcessing =>
			Mode != DeploymentMode.ClientUI;
	}

	public enum DeploymentMode
	{
		/// <summary>
		/// Full CAILE appliance running everything locally (your main use case).
		/// </summary>
		LocalRouter,

		/// <summary>
		/// Multi-user server hosting workspaces, sync, storage, and agents.
		/// </summary>
		ServerNode,

		/// <summary>
		/// Web/hybrid client that connects to Router/Server for inference.
		/// </summary>
		ClientUI
	}
}
