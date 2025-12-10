namespace IIM.Shared.Models;

/// <summary>
/// Unified database configuration for internal presets and external servers.
/// </summary>
public class DatabaseConfig
{
	public string Engine { get; }
	public string Host { get; }
	public string? User { get; }
	public string? Password { get; }
	public string ConnectionString => BuildConnectionString();

	public bool IsExternal =>
		Engine == "external-sqlserver" || Engine == "external-postgres";

	/// <summary>
	/// Primary 4-argument constructor used for BYO DB.
	/// </summary>
	public DatabaseConfig(string engine, string host, string user, string password)
	{
		Engine = engine.ToLowerInvariant();
		Host = host;
		User = user;
		Password = password;
	}

	/// <summary>
	/// Internal presets use this simpler constructor, but internally map to the 4-arg one.
	/// </summary>
	public DatabaseConfig(string engine, string host)
	{
		Engine = engine.ToLowerInvariant();
		Host = host;
		User = null;
		Password = null;
	}
	public DatabaseConfig()
	{
		
	}

	/// <summary>
	/// Builds the final connection string for deployment.
	/// </summary>
	private string BuildConnectionString()
	{
		return Engine switch
		{
			"sqlite" => Host, // e.g., "router.db"
			"sqlexpress" => $"Server={Host};Trusted_Connection=True;",
			"postgres" => $"Host={Host};User ID={User};Password={Password};",
			"external-postgres" => $"Host={Host};User ID={User};Password={Password};",
			"external-sqlserver" => $"Server={Host};User ID={User};Password={Password};",
			_ => Host // fallback safety
		};
	}

	// ------------------------------------------------------------------------
	// FACTORY HELPERS (no params for user simplicity — essential for your flow)
	// ------------------------------------------------------------------------

	public static DatabaseConfig MicroSQLite()
		=> new("sqlite", "router.db");

	public static DatabaseConfig MiniSqlExpress()
		=> new("sqlexpress", @".\SQLEXPRESS");

	public static DatabaseConfig SmallPostgres()
		=> new("postgres", "localhost", "postgres", "postgres");

	public static DatabaseConfig ExternalPostgres(string host, string user, string pass)
		=> new("external-postgres", host, user, pass);

	public static DatabaseConfig ExternalSqlServer(string host, string user, string pass)
		=> new("external-sqlserver", host, user, pass);
}
