using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace IIM.Shared.Models
{
	public class Entity
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Name { get; set; } = string.Empty;
		public EntityCategory Type { get; set; }
		public Dictionary<string, object> Properties { get; set; } = new();
		public List<string> Aliases { get; set; } = new();
		public List<Relationship> Relationships { get; set; } = new();
		public List<string> AssociatedCaseIds { get; set; } = new();
		public double RiskScore { get; set; }
		public DateTimeOffset FirstSeen { get; set; }
		public DateTimeOffset LastSeen { get; set; }
		public Dictionary<string, object> Attributes { get; set; } = new();
	}

	/// <summary>
	/// Categories for semantic grouping of indicators
	/// </summary>
	public enum EntityCategory
	{
		/// <summary>
		/// Personal Identifiable Information: names, emails, phones, DOB
		/// </summary>
		Identity = 0,

		/// <summary>
		/// Organization and Infrastructure: domains, IPs, URLs, ASNs
		/// </summary>
		Org = 1,

		/// <summary>
		/// Accounts and Financial: usernames, crypto addresses, transactions
		/// </summary>
		Account = 2,

		/// <summary>
		/// File Artifacts: filenames, hashes, paths (grouped by proximity)
		/// </summary>
		FileArtifact = 3,

		/// <summary>
		/// Threat Indicators: CVEs, MITRE ATT&CK, registry keys
		/// </summary>
		Threat = 4,

		/// <summary>
		/// Event or Activity: logins, uploads, transactions
		/// </summary>
		Event = 5
	}
}
