using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using IIM.Ingestion.Models;
using IIM.Shared.Models;

namespace IIM.Ingestion.Indicators;

/// <summary>
/// Extracts Indicators of Compromise (IoCs) from text with contextual analysis,
/// confidence scoring, and false positive filtering.
/// 
/// Features:
/// - Source-generated regex for performance (.NET 7+)
/// - Pluggable indicator extractors
/// - Precomputed text boundaries for efficient context extraction
/// - Configurable keyword lists and false positive sets
/// - Extraction limits and timeout support
/// </summary>
public sealed partial class IndicatorExtractor
{
	private readonly IndicatorExtractorOptions _options;
	private readonly List<IIndicatorPattern> _patterns;
	private readonly TimeSpan _timeout;

	public IndicatorExtractor() : this(new IndicatorExtractorOptions()) { }

	public IndicatorExtractor(IndicatorExtractorOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_timeout = options.ExtractionTimeout;
		_patterns = BuildPatternRegistry(options);
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// SOURCE-GENERATED REGEX (.NET 7+)
	// ═══════════════════════════════════════════════════════════════════════════

	#region IP Addresses

	// Ensures the IP is not immediately preceded or followed by a '.' or '/' (prevents partial path matches)
	[GeneratedRegex(@"(?<![\d\./])\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b(?![\d\./])")]
	private static partial Regex Ipv4Regex();

	[GeneratedRegex(@"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)/(?:3[0-2]|[12]?\d)\b")]
	private static partial Regex Ipv4CidrRegex();

	// Improved IPv6 pattern - handles common valid forms while rejecting obvious invalids
	[GeneratedRegex(@"\b(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}|(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}|(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}|(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:(?::[0-9a-fA-F]{1,4}){1,6}|:(?::[0-9a-fA-F]{1,4}){1,7}|::(?:[fF]{4}:)?(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?))\b")]
	private static partial Regex Ipv6Regex();

	[GeneratedRegex(@"\b(?:[0-9a-fA-F]{1,4}:){2,7}[0-9a-fA-F]{1,4}/\d{1,3}\b")]
	private static partial Regex Ipv6CidrRegex();

	private static readonly HashSet<string> ReservedIpPrefixes = new()
{
	"0.",          // Software/Internal noise
    "127.",        // Loopback
    "169.254.",    // APIPA (Link-local)
    "224.", "239.", // Multicast
    "255.255.255.255" // Broadcast
};

	private static readonly HashSet<string> SoftwareVersionKeywords = new(StringComparer.OrdinalIgnoreCase)
{
	"version", "v.", "build", "release", "v1.", "v2.", "v4."
};

	#endregion

	#region Network Identifiers

	[GeneratedRegex(@"\bAS[Nn]?\d{1,10}\b")]
	private static partial Regex AsnRegex();

	[GeneratedRegex(@"\b(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\b")]
	private static partial Regex MacAddressRegex();

	#endregion

	#region URLs and Domains

	[GeneratedRegex(@"(?:https?|hxxps?|ftp|ftps):\/\/[^\s<>()\""'\[\]]+", RegexOptions.IgnoreCase)]
	private static partial Regex UrlRegex();

	[GeneratedRegex(@"(?<![\\/])\b(?!(?:https?|ftp)://)[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)+\b", RegexOptions.IgnoreCase)]
	private static partial Regex DomainRegex();

	private static readonly FrozenSet<string> ValidTlds = new[]
{
	"com", "net", "org", "edu", "gov", "io", "co", "uk", "de", "ru", "info", "top", "xyz"
}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> ForbiddenExtensions = new[]
	{
	"php", "asp", "aspx", "html", "js", "css", "jpg", "png", "exe", "dll", "bin", "sh", "py"
}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	#endregion

	#region Communication

	[GeneratedRegex(@"\b[A-Z0-9._%+-]+(?:@|\[@\])[A-Z0-9.-]+(?:\.|\[\.\])[A-Z]{2,}\b", RegexOptions.IgnoreCase)]
	private static partial Regex EmailRegex();

	// Context-aware phone pattern - requires country code or context
	[GeneratedRegex(@"(?:(?:phone|tel|call|contact|fax|mobile|cell)\s*[:\s]?\s*)?\+?\d{1,3}[\s.-]?\(?\d{2,4}\)?[\s.-]?\d{3,4}[\s.-]?\d{4}\b", RegexOptions.IgnoreCase)]
	private static partial Regex PhoneRegex();

	#endregion

	#region Cryptocurrency

	// Updated Bitcoin Regex: Added a check to ensure "MD5" or "Hash" doesn't precede it
	[GeneratedRegex(@"(?i)(?<!(?:md5|hash|file)[:\s]*)\b(?:bc1|[13])[a-zA-HJ-NP-Z0-9]{25,39}\b")]
	private static partial Regex BitcoinRegex();

	[GeneratedRegex(@"\b0x[a-fA-F0-9]{40}\b")]
	private static partial Regex EthereumRegex();

	[GeneratedRegex(@"\b4[0-9AB][1-9A-HJ-NP-Za-km-z]{93}\b")]
	private static partial Regex MoneroRegex();

	#endregion

	#region File Hashes

	[GeneratedRegex(@"\b[a-fA-F0-9]{32}\b")]
	private static partial Regex Md5Regex();

	[GeneratedRegex(@"\b[a-fA-F0-9]{40}\b")]
	private static partial Regex Sha1Regex();

	[GeneratedRegex(@"\b[a-fA-F0-9]{64}\b")]
	private static partial Regex Sha256Regex();

	[GeneratedRegex(@"\b[a-fA-F0-9]{128}\b")]
	private static partial Regex Sha512Regex();

	#endregion

	#region Usernames

	[GeneratedRegex(@"(?:user(?:name)?|author|from|by|creator|owner|account|handle)\s*[:\s=]+\s*([a-zA-Z][a-zA-Z0-9._-]{2,32})", RegexOptions.IgnoreCase)]
	private static partial Regex ContextualUsernameRegex();

	[GeneratedRegex(@"(?:^|[\s(])@([a-zA-Z][a-zA-Z0-9_]{1,38})\b")]
	private static partial Regex SocialHandleRegex();

	// Captures the Label (Group 1) and the Username/ID (Group 2)
	// Update this in your Regex region
	[GeneratedRegex(@"(?i)(ESP\s*User\s*ID|User\s*ID|Display\s*Name|UUID|(?<!file)Name)\s*[:=-]\s*([^\s,;]+)", RegexOptions.Compiled)]
	private static partial Regex HardenedUsernameRegex();

	public FrozenSet<string> UsernameStopWords { get; set; } = FrozenSet.ToFrozenSet(new[]
{
	"the", "system", "agent", "child", "members", "identifier", "company", "unknown", "null", "undefined"
}, StringComparer.OrdinalIgnoreCase);

	#endregion

	#region File Paths and Registry

	[GeneratedRegex(@"[A-Za-z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*")]
	private static partial Regex WindowsPathRegex();

	[GeneratedRegex(@"(?:^|[\s""'])(/(?:[^/\0\s""']+/)*[^/\0\s""']+)")]
	private static partial Regex UnixPathRegex();

	[GeneratedRegex(@"\b(?:HKEY_[A-Z_]+|HK[A-Z]{2})\\[^\s]+")]
	private static partial Regex RegistryKeyRegex();

	#endregion

	#region Filenames
	[GeneratedRegex(@"(?i)\b[a-z0-9_\-\.]{5,}\.(?:mp4|mov|avi|wmv|csv|exe|dll|zip|7z|pdf|docx|txt)\b")]
	private static partial Regex FilenameRegex();

	#endregion

	#region CVE and MITRE

	[GeneratedRegex(@"\bCVE-\d{4}-\d{4,}\b", RegexOptions.IgnoreCase)]
	private static partial Regex CveRegex();

	[GeneratedRegex(@"\b[TS]\d{4}(?:\.\d{3})?\b")]
	private static partial Regex MitreAttackRegex();

	#endregion

	// ═══════════════════════════════════════════════════════════════════════════
	// PLUGGABLE PATTERN REGISTRY
	// ═══════════════════════════════════════════════════════════════════════════

	private List<IIndicatorPattern> BuildPatternRegistry(IndicatorExtractorOptions options)
	{
		var patterns = new List<IIndicatorPattern>();

		// Core network indicators
		patterns.Add(new RegexPattern(IndicatorType.Cidr, Ipv4CidrRegex(), "IPv4", 0.9f));
		patterns.Add(new RegexPattern(IndicatorType.Cidr, Ipv6CidrRegex(), "IPv6", 0.9f));
		patterns.Add(new RegexPattern(IndicatorType.IpAddress, Ipv4Regex(), "IPv4", 0.8f,
			confidenceAdjuster: (v, ctx, opts) => CalculateIpConfidence(v, ctx, opts)));
		patterns.Add(new RegexPattern(IndicatorType.IpAddress, Ipv6Regex(), "IPv6", 0.8f,
			confidenceAdjuster: (v, ctx, opts) => CalculateIpConfidence(v, ctx, opts)));
		patterns.Add(new RegexPattern(IndicatorType.Asn, AsnRegex(), null, 0.85f));
		patterns.Add(new RegexPattern(IndicatorType.MacAddress, MacAddressRegex(), null, 0.8f));

		// URLs and domains
		patterns.Add(new RegexPattern(IndicatorType.Url, UrlRegex(), null, 0.9f,
			confidenceAdjuster: (v, ctx, _) => CalculateUrlConfidence(v, ctx)));

		if (options.ExtractStandaloneDomains)
		{
			patterns.Add(new RegexPattern(IndicatorType.Domain, DomainRegex(), "Standalone", 0.6f,
				filter: v => !options.FalsePositiveDomains.Contains(v),
				confidenceAdjuster: (v, ctx, opts) => CalculateDomainConfidence(v, ctx)));
		}

		// Communication
		patterns.Add(new RegexPattern(IndicatorType.EmailAddress, EmailRegex(), null, 0.85f));

		if (options.ExtractPhoneNumbers)
		{
			patterns.Add(new RegexPattern(IndicatorType.PhoneNumber, PhoneRegex(), null, 0.5f,
				confidenceAdjuster: (v, ctx, opts) => CalculatePhoneConfidence(v, ctx, opts)));
		}

		// Cryptocurrency
		patterns.Add(new RegexPattern(IndicatorType.CryptoAddress, BitcoinRegex(), "Bitcoin", 0.95f));
		patterns.Add(new RegexPattern(IndicatorType.CryptoAddress, EthereumRegex(), "Ethereum", 0.85f,
			filter: v => !IsLikelyNonCryptoHex(v)));
		patterns.Add(new RegexPattern(IndicatorType.CryptoAddress, MoneroRegex(), "Monero", 0.95f));

		// Hashes - ordered by length (longest first)
		patterns.Add(new RegexPattern(IndicatorType.FileHash, Sha512Regex(), "SHA512", 0.9f,
			filter: v => IsLikelyHash(v, options)));
		patterns.Add(new RegexPattern(IndicatorType.FileHash, Sha256Regex(), "SHA256", 0.85f,
			filter: v => IsLikelyHash(v, options),
			confidenceAdjuster: (v, ctx, opts) => CalculateHashConfidence(v, ctx, "SHA256", opts)));
		patterns.Add(new RegexPattern(IndicatorType.FileHash, Sha1Regex(), "SHA1", 0.7f,
			filter: v => IsLikelyHash(v, options) && !IsEthereumAddress(v),
			confidenceAdjuster: (v, ctx, opts) => CalculateHashConfidence(v, ctx, "SHA1", opts)));
		patterns.Add(new RegexPattern(
			IndicatorType.FileHash,
			Md5Regex(),
			"MD5",
			0.7f, // Higher baseline
			filter: v => IsLikelyHash(v, options) && !IsLikelyGuid(v),
			confidenceAdjuster: (v, ctx, opts) =>
			{
				var score = CalculateHashConfidence(v, ctx, "MD5", opts);

				// If the word "MD5" is right next to it, boost to near certainty
				if (ctx.SurroundingLower.Contains("md5"))
					score = Math.Max(score, 0.95f);

				return score;
			}));

		// File system
		patterns.Add(new RegexPattern(IndicatorType.FilePath, WindowsPathRegex(), "Windows", 0.75f));
		patterns.Add(new RegexPattern(IndicatorType.RegistryKey, RegistryKeyRegex(), null, 0.9f));

		// Add this inside BuildPatternRegistry
		patterns.Add(new RegexPattern(
			IndicatorType.FileName,
			FilenameRegex(),
			null,
			0.85f,
			confidenceAdjuster: (v, ctx, opts) =>
			{
				// Boost if preceded by "Filename:" or "File:"
				if (ctx.SurroundingLower.Contains("filename") || ctx.SurroundingLower.Contains("file:"))
					return 0.95f;
				return 0.85f;
			}));
		// Security identifiers
		patterns.Add(new RegexPattern(IndicatorType.Cve, CveRegex(), null, 0.99f));
		patterns.Add(new RegexPattern(IndicatorType.MitreAttack, MitreAttackRegex(), null, 0.5f,
			confidenceAdjuster: (v, ctx, opts) => CalculateMitreConfidence(v, ctx, opts)));

		// Add custom patterns from options
		patterns.AddRange(options.CustomPatterns);

		return patterns;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// PUBLIC API
	// ═══════════════════════════════════════════════════════════════════════════

	public ExtractionResult Extract(string text)
	{
		var stopwatch = Stopwatch.StartNew();

		if (string.IsNullOrWhiteSpace(text))
		{
			return ExtractionResult.Empty();
		}

		// Enforce text length limit
		if (text.Length > _options.MaxTextLength)
		{
			text = text[.._options.MaxTextLength];
		}

		var statistics = new ExtractionStatistics
		{
			OriginalTextLength = text.Length
		};

		// Normalize defanged indicators
		string normalizedText;
		bool wasDefanged;
		if (_options.HandleDefangedIndicators)
		{
			normalizedText = Refang(text);
			wasDefanged = normalizedText != text;
		}
		else
		{
			normalizedText = text;
			wasDefanged = false;
		}

		// Precompute text boundaries for efficient context extraction
		var boundaries = new TextBoundaries(text);

		var occurrences = new List<IndicatorOccurrence>();
		var matchCountsByType = new Dictionary<IndicatorType, int>();

		foreach (var pattern in _patterns)
		{
			// Check timeout
			if (stopwatch.Elapsed > _timeout)
			{
				statistics.TimedOut = true;
				break;
			}

			// Check per-type limit
			var currentCount = matchCountsByType.GetValueOrDefault(pattern.Type, 0);
			if (currentCount >= _options.MaxMatchesPerType)
				continue;

			var matches = pattern.Extract(normalizedText, text, boundaries, _options);

			foreach (var match in matches)
			{
				if (currentCount >= _options.MaxMatchesPerType)
					break;

				// Store both raw and normalized values
				if (wasDefanged && match.Value != GetOriginalValue(text, match.Offset, match.Length))
				{
					match.RawValue = GetOriginalValue(text, match.Offset, match.Length);
				}

				occurrences.Add(match);
				currentCount++;
				statistics.TotalOccurrencesBeforeFiltering++;
			}

			matchCountsByType[pattern.Type] = currentCount;
		}

		// Extract usernames (special handling for capture groups)
		ExtractUsernames(normalizedText, text, boundaries, occurrences);

		// Extract Unix paths (special handling)
		ExtractUnixPaths(normalizedText, text, boundaries, occurrences);

		ExtractEspFields(normalizedText, boundaries, occurrences);

		// Extract domains from URLs
		if (_options.ExtractDomainsFromUrls)
			ExtractDomainsFromUrlOccurrences(occurrences);

		ResolveCollisions(occurrences);
		// Filter by confidence
		var filteredOccurrences = occurrences
			.Where(o => o.Confidence >= _options.MinimumConfidence)
			.ToList();

		// Build aggregated indicators
		var indicators = BuildIndicators(filteredOccurrences);

		// Calculate statistics
		stopwatch.Stop();
		statistics.ExtractionDuration = stopwatch.Elapsed;
		statistics.TotalOccurrences = filteredOccurrences.Count;
		statistics.UniqueIndicators = indicators.Count;
		statistics.OccurrencesByType = filteredOccurrences
			.GroupBy(o => o.Type)
			.ToDictionary(g => g.Key, g => g.Count());
		statistics.ConfidenceDistribution = filteredOccurrences
			.GroupBy(o => o.Type)
			.ToDictionary(
				g => g.Key,
				g => new ConfidenceStats
				{
					Min = g.Min(x => x.Confidence),
					Max = g.Max(x => x.Confidence),
					Average = g.Average(x => x.Confidence)
				});

		return new ExtractionResult
		{
			Indicators = indicators,
			Occurrences = filteredOccurrences,
			Statistics = statistics
		};
	}

	// Inside BuildIndicators or a post-processing step
	private void ResolveCollisions(List<IndicatorOccurrence> occurrences)
	{
		foreach (var occ in occurrences.Where(o => o.Type == IndicatorType.CryptoAddress))
		{
			// If this same string was also flagged as an MD5
			var hasMd5Match = occurrences.Any(o =>
				o.Value == occ.Value &&
				o.Type == IndicatorType.FileHash);

			// Or if the context explicitly says "MD5"
			if (hasMd5Match || occ.Context.SurroundingLower.Contains("md5"))
			{
				occ.Confidence = 0.1f; // Effectively kill the Bitcoin match
			}
		}
	}

	private static string GetOriginalValue(string text, int offset, int length)
	{
		if (offset < 0 || offset + length > text.Length)
			return string.Empty;
		return text.Substring(offset, length);
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// SPECIAL EXTRACTION METHODS
	// ═══════════════════════════════════════════════════════════════════════════

	private void ExtractUsernames(string text, string original, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		// 1. Hardened Contextual extraction (Prefix-based: ESP User ID, UUID, etc.)
		foreach (Match m in HardenedUsernameRegex().Matches(text))
		{
			string label = m.Groups[1].Value;
			string username = m.Groups[2].Value.Trim().TrimEnd('.', ',');

			// Hardening Check: Skip noise, short strings, or common stop-words
			if (username.Length < 3 || UsernameStopWords.Contains(username))
				continue;

			var ctx = boundaries.BuildContext(m.Index);
			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.Username,
				Subtype = "Contextual",
				Value = username, // Fixed: Required member set
				Offset = m.Index,
				Length = m.Length,
				Context = ctx,    // Fixed: Required member set
				Confidence = CalculateHardenedConfidence(username, label, ctx, _options)
			});
		}

		// 2. Social handles (@username)
		foreach (Match m in SocialHandleRegex().Matches(text))
		{
			var rawHandle = m.Groups[1].Value;

			// Hardening: Skip if the handle itself is a stop-word (e.g., "@system")
			if (UsernameStopWords.Contains(rawHandle)) continue;

			var handle = "@" + rawHandle;
			var ctx = boundaries.BuildContext(m.Index);

			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.Username,
				Subtype = "SocialHandle",
				Value = handle,   // Fixed: Required member set
				Offset = m.Index,
				Length = m.Length,
				Context = ctx,    // Fixed: Required member set
				Confidence = 0.85f
			});
		}
	}



	private void ExtractUnixPaths(string text, string original, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		foreach (Match m in UnixPathRegex().Matches(text))
		{
			var path = m.Groups[1].Value;
			if (path.Length > 3 && path.Count(c => c == '/') >= 2)
			{
				sink.Add(new IndicatorOccurrence
				{
					Id = Guid.NewGuid(),
					Type = IndicatorType.FilePath,
					Subtype = "Unix",
					Value = path,
					Offset = m.Groups[1].Index,
					Length = m.Groups[1].Length,
					Context = boundaries.BuildContext(m.Groups[1].Index),
					Confidence = 0.75f
				});
			}
		}
	}

	private void ExtractDomainsFromUrlOccurrences(List<IndicatorOccurrence> occurrences)
	{
		var urlOccurrences = occurrences
			.Where(o => o.Type == IndicatorType.Url)
			.ToList();

		foreach (var urlOcc in urlOccurrences)
		{
			if (Uri.TryCreate(urlOcc.Value, UriKind.Absolute, out var uri))
			{
				var host = uri.Host;

				if (IPAddress.TryParse(host, out _))
					continue;

				if (_options.FalsePositiveDomains.Contains(host))
					continue;

				occurrences.Add(new IndicatorOccurrence
				{
					Id = Guid.NewGuid(),
					Type = IndicatorType.Domain,
					Subtype = "FromUrl",
					Value = host,
					Offset = urlOcc.Offset,
					Length = urlOcc.Length,
					Context = urlOcc.Context,
					Confidence = urlOcc.Confidence * 0.95f,
					DerivedFromId = urlOcc.Id,
					Metadata = new Dictionary<string, string>
					{
						["SourceUrl"] = urlOcc.Value
					}
				});
			}
		}
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// DEFANGING
	// ═══════════════════════════════════════════════════════════════════════════

	private static string Refang(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		// Use span-based replacement for better performance on large texts
		return text
			.Replace("hxxp://", "http://", StringComparison.OrdinalIgnoreCase)
			.Replace("hxxps://", "https://", StringComparison.OrdinalIgnoreCase)
			.Replace("[.]", ".")
			.Replace("[:]", ":")
			.Replace("[@]", "@")
			.Replace("[at]", "@", StringComparison.OrdinalIgnoreCase)
			.Replace("[dot]", ".", StringComparison.OrdinalIgnoreCase)
			.Replace("(.)", ".")
			.Replace("{.}", ".");
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// CONFIDENCE CALCULATORS
	// ═══════════════════════════════════════════════════════════════════════════

	private static float CalculateIpConfidence(string value, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		// 1. Initial baseline
		float confidence = 0.8f;

		// 2. Reject Reserved/Noise Ranges
		if (ReservedIpPrefixes.Any(p => value.StartsWith(p)))
		{
			return 0.1f; // Effectively discard
		}

		// 3. Handle Private/Internal IPs (RFC1918)
		bool isPrivate = opts.PrivateIpPrefixes.Any(p => value.StartsWith(p));
		if (isPrivate)
		{
			// If it's private but we see "Source" or "Remote", keep it. 
			// Otherwise, drop confidence significantly.
			confidence = ContainsAnyKeyword(ctx.SurroundingLower, opts.NetworkKeywords) ? 0.4f : 0.2f;
		}

		// 4. Filter Software Versions (e.g., "Product Version: 1.2.3.4")
		if (ContainsAnyKeyword(ctx.PrecedingWords.Select(w => w.ToLower()).ToList(), SoftwareVersionKeywords))
		{
			return 0.2f;
		}

		// 5. Context Boost (e.g., "Source IP:", "Attacker Host:")
		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.NetworkKeywords))
		{
			confidence = Math.Min(1.0f, confidence + 0.15f);
		}

		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.ThreatKeywords))
		{
			confidence = Math.Min(1.0f, confidence + 0.1f);
		}

		return confidence;
	}

	// Helper for List<string> keyword checks
	private static bool ContainsAnyKeyword(List<string> words, IEnumerable<string> keywords)
	{
		return words.Any(w => keywords.Contains(w, StringComparer.OrdinalIgnoreCase));
	}

	private static float CalculateUrlConfidence(string value, IndicatorContext ctx)
	{
		var confidence = 0.9f;

		if (value.Contains("hxxp", StringComparison.OrdinalIgnoreCase))
			confidence = 0.95f;

		if (value.Length > 200)
			confidence *= 0.95f;

		return confidence;
	}

	private static float CalculateDomainConfidence(string value, IndicatorContext ctx)
	{
		// 1. Basic Structure Check
		if (!value.Contains('.') || value.EndsWith(".") || value.StartsWith("."))
			return 0.1f;

		// 2. Extract the Suffix (TLD)
		var parts = value.Split('.');
		var suffix = parts.Last();

		// 3. Filter: Is it a valid TLD?
		// This kills "janelle.mcmillian" because "mcmillian" is not in our list.
		if (!ValidTlds.Contains(suffix))
		{
			// If it's a very long suffix (> 6 chars), it's likely a name or internal noise
			if (suffix.Length > 6) return 0.1f;

			// Otherwise, penalize heavily but don't kill (handles newer/obscure gTLDs)
			return 0.3f;
		}

		// 4. Filter: Is it a common file extension?
		// This kills "profile.php"
		if (ForbiddenExtensions.Contains(suffix))
		{
			return 0.1f;
		}

		// 5. Baseline for valid-looking domains
		float confidence = 0.8f;

		// 6. Context Boost (e.g., "URL:", "Domain:", "Connect to:")
		if (ctx.SurroundingLower.Contains("url") || ctx.SurroundingLower.Contains("domain") || ctx.SurroundingLower.Contains("http"))
		{
			confidence = Math.Min(1.0f, confidence + 0.15f);
		}

		return confidence;
	}

	// Add this to your special extraction methods
	private void ExtractEspFields(string text, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		// Regex to find "Filename: [value]" or "MD5: [value]"
		var espRegex = new Regex(@"(?i)(Filename|MD5|Original Filename)\s*:\s*([^\s\r\n]+)");

		foreach (Match m in espRegex.Matches(text))
		{
			var label = m.Groups[1].Value.ToLower();
			var value = m.Groups[2].Value.Trim();
			var ctx = boundaries.BuildContext(m.Index);

			if (label.Contains("filename"))
			{
				sink.Add(new IndicatorOccurrence
				{
					Type = IndicatorType.FileName,
					Value = value,
					Confidence = 0.98f, // Very high because of the explicit label
					Context = ctx,
					Offset = m.Groups[2].Index,
					Length = value.Length
				});
			}
		}
	}

	private static float CalculatePhoneConfidence(string value, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		var confidence = 0.5f;

		// Boost if has country code
		if (value.StartsWith("+"))
			confidence += 0.2f;

		// Boost if has context keywords
		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.PhoneContextKeywords))
			confidence += 0.25f;

		return Math.Min(1f, confidence);
	}

	private static float CalculateHashConfidence(string value, IndicatorContext ctx, string hashType, IndicatorExtractorOptions opts)
	{
		var confidence = hashType switch
		{
			"SHA512" => 0.9f,
			"SHA256" => 0.85f,
			"SHA1" => 0.7f,
			"MD5" => 0.65f, // Baseline
			_ => 0.5f
		};

		// HARDENING: Look for explicit labels like "MD5: " or "Hash: "
		if (ctx.SurroundingLower.Contains(hashType.ToLower() + ":") ||
			ctx.SurroundingLower.Contains(hashType.ToLower() + " "))
		{
			confidence += 0.3f;
		}

		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.HashKeywords))
			confidence += 0.15f;

		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.FileKeywords))
			confidence += 0.1f;

		return Math.Min(1.0f, confidence);
	}

	private static float CalculateUsernameConfidence(string value, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.ActorKeywords))
			return 0.9f;

		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.SocialPlatformKeywords))
			return 0.85f;

		return 0.7f;
	}

	private static float CalculateHardenedConfidence(string value, string label, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		label = label.ToLower();

		// Reward high-value structural matches
		if (label.Contains("esp user id") || label.Contains("uuid")) return 0.98f;
		if (label.Contains("user id")) return 0.95f;
		if (label.Contains("display name")) return 0.90f;

		// Fallback to your original keyword logic
		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.ActorKeywords)) return 0.85f;

		return 0.70f;
	}

	private static float CalculateMitreConfidence(string value, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.MitreKeywords))
			return 0.95f;

		if (ContainsAnyKeyword(ctx.SurroundingLower, opts.ThreatKeywords))
			return 0.7f;

		return 0.4f;
	}

	private static bool ContainsAnyKeyword(string text, FrozenSet<string> keywords)
	{
		foreach (var keyword in keywords)
		{
			if (text.Contains(keyword, StringComparison.Ordinal))
				return true;
		}
		return false;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// FALSE POSITIVE FILTERS
	// ═══════════════════════════════════════════════════════════════════════════

	private static bool IsLikelyHash(string value, IndicatorExtractorOptions opts)
	{
		if (opts.HashExclusions.Contains(value))
			return false;

		var distinctChars = value.Distinct().Count();
		if (distinctChars < 8)
			return false;

		if (IsRepeatingPattern(value))
			return false;

		return true;
	}

	private static bool IsLikelyGuid(string value)
	{
		if (value.Length != 32)
			return false;

		var formatted = $"{value[..8]}-{value[8..12]}-{value[12..16]}-{value[16..20]}-{value[20..]}";
		return Guid.TryParse(formatted, out _);
	}

	private static bool IsEthereumAddress(string value)
	{
		return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && value.Length == 42;
	}

	private static bool IsLikelyNonCryptoHex(string value)
	{
		// Check if it might be a memory address or other non-crypto hex
		return value.StartsWith("0x00000", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsRepeatingPattern(string value)
	{
		for (var patternLen = 1; patternLen <= Math.Min(8, value.Length / 2); patternLen++)
		{
			var pattern = value.AsSpan(0, patternLen);
			var isRepeating = true;

			for (var i = patternLen; i < value.Length; i += patternLen)
			{
				var remaining = Math.Min(patternLen, value.Length - i);
				if (!value.AsSpan(i, remaining).SequenceEqual(pattern[..remaining]))
				{
					isRepeating = false;
					break;
				}
			}

			if (isRepeating)
				return true;
		}

		return false;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// INDICATOR AGGREGATION
	// ═══════════════════════════════════════════════════════════════════════════

	private List<Indicator> BuildIndicators(List<IndicatorOccurrence> occurrences)
	{
		var indicators = new Dictionary<string, Indicator>();

		foreach (var occ in occurrences)
		{
			var key = $"{occ.Type}:{occ.Subtype}:{occ.Value}";

			if (!indicators.TryGetValue(key, out var indicator))
			{
				indicator = new Indicator
				{
					Id = Guid.NewGuid(),
					Type = occ.Type,
					Subtype = occ.Subtype,
					Value = occ.Value,
					NormalizedValue = occ.Value,
					RawValue = occ.RawValue,
					Occurrences = new List<Guid>(),
					FirstSeen = occ.Offset,
					Confidence = occ.Confidence
				};

				indicators[key] = indicator;
			}

			indicator.Occurrences.Add(occ.Id);
			indicator.Confidence = Math.Max(indicator.Confidence, occ.Confidence);
			occ.IndicatorId = indicator.Id;
		}

		if (_options.GroupIpv6ByPrefix)
			GroupIpv6Prefixes(indicators, occurrences);

		return indicators.Values
			.OrderByDescending(i => i.Confidence)
			.ThenByDescending(i => i.Occurrences.Count)
			.ToList();
	}

	private static void GroupIpv6Prefixes(Dictionary<string, Indicator> indicators, List<IndicatorOccurrence> occurrences)
	{
		var ipv6Groups = occurrences
			.Where(o => o.Type == IndicatorType.IpAddress && o.Subtype == "IPv6")
			// Filter out noise ranges before grouping
			.Where(o => !IsReservedIpv6(o.Value))
			.GroupBy(o => GetIpv6Prefix(o.Value))
			.Where(g => g.Key != null);

		foreach (var group in ipv6Groups)
		{
			var prefix = group.Key!;

			// HARDENING: Count DISTINCT address values, not total occurrences
			var distinctAddresses = group.Select(g => g.Value).Distinct().ToList();

			// Only create a prefix indicator if there's actual "lateral" spread (more than 1 unique IP)
			if (distinctAddresses.Count > 1)
			{
				indicators[$"ipv6prefix:{prefix}"] = new Indicator
				{
					Id = Guid.NewGuid(),
					Type = IndicatorType.IpAddress,
					Subtype = "IPv6Prefix64",
					Value = prefix,
					NormalizedValue = prefix,
					RelatedValues = distinctAddresses,
					// Boost confidence because multiple IPs in one prefix suggests a scanning pattern
					Confidence = Math.Min(1.0f, group.Max(g => g.Confidence) + 0.05f),
					Metadata = new Dictionary<string, string>
					{
						["UniqueAddressCount"] = distinctAddresses.Count.ToString(),
						["TotalHitCount"] = group.Count().ToString()
					}
				};
			}
		}
	}

	private static bool IsReservedIpv6(string ip)
	{
		if (!IPAddress.TryParse(ip, out var address)) return true;

		// Check for Loopback (::1)
		if (IPAddress.IsLoopback(address)) return true;

		var bytes = address.GetAddressBytes();

		// fe80:: (Link-local)
		if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80) return true;

		// ff00:: (Multicast)
		if (bytes[0] == 0xff) return true;

		return false;
	}

	private static string? GetIpv6Prefix(string ip)
	{
		if (!IPAddress.TryParse(ip, out var address))
			return null;

		var bytes = address.GetAddressBytes();
		if (bytes.Length != 16)
			return null;

		// Zero lower 64 bits
		Array.Clear(bytes, 8, 8);

		var prefix = new IPAddress(bytes).ToString();
		return prefix + "/64";
	}

}

// ═══════════════════════════════════════════════════════════════════════════════
// TEXT BOUNDARIES (Precomputed for Performance)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Precomputes sentence and line boundaries for efficient context extraction.
/// </summary>
public sealed class TextBoundaries
{
	private readonly string _text;
	private readonly string _textLower;
	private readonly List<int> _sentenceStarts;
	private readonly List<int> _lineStarts;

	public TextBoundaries(string text)
	{
		_text = text;
		_textLower = text.ToLowerInvariant();
		_sentenceStarts = new List<int> { 0 };
		_lineStarts = new List<int> { 0 };

		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];

			if (c == '\n')
			{
				if (i + 1 < text.Length)
					_lineStarts.Add(i + 1);
			}

			if (".!?".Contains(c))
			{
				if (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
					_sentenceStarts.Add(i + 1);
			}
		}
	}

	public IndicatorContext BuildContext(int index)
	{
		var sentenceStart = FindBoundaryStart(_sentenceStarts, index);
		var sentenceEnd = FindBoundaryEnd(_sentenceStarts, index, _text.Length);

		var lineStart = FindBoundaryStart(_lineStarts, index);
		var lineEnd = FindBoundaryEnd(_lineStarts, index, _text.Length);

		var windowStart = Math.Max(0, index - 200);
		var windowEnd = Math.Min(_text.Length, index + 200);

		var surrounding = _text[windowStart..windowEnd].Trim();
		var surroundingLower = _textLower[windowStart..windowEnd].Trim();

		return new IndicatorContext
		{
			Sentence = _text[sentenceStart..sentenceEnd].Trim(),
			Block = _text[lineStart..lineEnd].Trim(),
			Surrounding = surrounding,
			SurroundingLower = surroundingLower,
			PrecedingWords = ExtractWords(_text, Math.Max(0, index - 100), index),
			FollowingWords = ExtractWords(_text, index, Math.Min(_text.Length, index + 100))
		};
	}

	private static int FindBoundaryStart(List<int> boundaries, int index)
	{
		var pos = boundaries.BinarySearch(index);
		if (pos < 0) pos = ~pos - 1;
		return pos >= 0 ? boundaries[pos] : 0;
	}

	private static int FindBoundaryEnd(List<int> boundaries, int index, int textLength)
	{
		var pos = boundaries.BinarySearch(index);
		if (pos < 0) pos = ~pos;
		return pos < boundaries.Count ? boundaries[pos] : textLength;
	}

	private static List<string> ExtractWords(string text, int start, int end)
	{
		if (start >= end || start < 0 || end > text.Length)
			return new List<string>();

		return text[start..end]
			.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
			.Take(5)
			.ToList();
	}
}

// ═══════════════════════════════════════════════════════════════════════════════
// PLUGGABLE PATTERN INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interface for pluggable indicator extraction patterns.
/// </summary>
public interface IIndicatorPattern
{
	IndicatorType Type { get; }
	string? Subtype { get; }
	IEnumerable<IndicatorOccurrence> Extract(string text, string originalText, TextBoundaries boundaries, IndicatorExtractorOptions options);
}

/// <summary>
/// Regex-based indicator pattern implementation.
/// </summary>
public sealed class RegexPattern : IIndicatorPattern
{
	private readonly Regex _regex;
	private readonly float _baseConfidence;
	private readonly Func<string, bool>? _filter;
	private readonly Func<string, IndicatorContext, IndicatorExtractorOptions, float>? _confidenceAdjuster;

	public IndicatorType Type { get; }
	public string? Subtype { get; }

	public RegexPattern(
		IndicatorType type,
		Regex regex,
		string? subtype = null,
		float baseConfidence = 0.7f,
		Func<string, bool>? filter = null,
		Func<string, IndicatorContext, IndicatorExtractorOptions, float>? confidenceAdjuster = null)
	{
		Type = type;
		_regex = regex;
		Subtype = subtype;
		_baseConfidence = baseConfidence;
		_filter = filter;
		_confidenceAdjuster = confidenceAdjuster;
	}

	public IEnumerable<IndicatorOccurrence> Extract(string text, string originalText, TextBoundaries boundaries, IndicatorExtractorOptions options)
	{
		foreach (Match m in _regex.Matches(text))
		{
			var value = m.Value;

			if (_filter != null && !_filter(value))
				continue;

			var ctx = boundaries.BuildContext(m.Index);
			var confidence = _confidenceAdjuster?.Invoke(value, ctx, options) ?? _baseConfidence;

			yield return new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = Type,
				Subtype = Subtype,
				Value = value,
				Offset = m.Index,
				Length = m.Length,
				Context = ctx,
				Confidence = confidence
			};
		}
	}
}

// ═══════════════════════════════════════════════════════════════════════════════
// OPTIONS
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class IndicatorExtractorOptions
{
	// ─────────────────────────────────────────────
	// Extraction Control
	// ─────────────────────────────────────────────

	public bool HandleDefangedIndicators { get; set; } = true;
	public float MinimumConfidence { get; set; } = 0.7f;
	public bool ExtractDomainsFromUrls { get; set; } = true;
	public bool ExtractStandaloneDomains { get; set; } = true;
	public bool ExtractPhoneNumbers { get; set; } = false; // Opt-in due to false positive rate
	public bool GroupIpv6ByPrefix { get; set; } = true;
	public bool ReduceConfidenceForPrivateIps { get; set; } = true;

	// ─────────────────────────────────────────────
	// Limits and Timeouts
	// ─────────────────────────────────────────────

	public int MaxTextLength { get; set; } = 10_000_000; // 10MB
	public int MaxMatchesPerType { get; set; } = 10_000;
	public TimeSpan ExtractionTimeout { get; set; } = TimeSpan.FromSeconds(30);

	// ─────────────────────────────────────────────
	// Configurable Keyword Sets
	// ─────────────────────────────────────────────

	public FrozenSet<string> NetworkKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"ip", "address", "host", "server", "source", "destination",
		"c2", "command", "control", "beacon", "callback", "proxy"
	});

	public FrozenSet<string> ThreatKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"malware", "threat", "attack", "exploit", "vulnerability",
		"ransomware", "trojan", "backdoor", "c2", "command and control",
		"phishing", "campaign", "apt", "indicator", "ioc", "intrusion"
	});

	public FrozenSet<string> ActorKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"actor", "threat actor", "attacker", "adversary", "group",
		"operator", "criminal", "hacker", "apt", "nation-state"
	});

	public FrozenSet<string> HashKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"hash", "md5", "sha", "sha1", "sha256", "sha512",
		"checksum", "digest", "fingerprint", "ioc", "indicator"
	});

	public FrozenSet<string> FileKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"file", "malware", "sample", "binary", "executable", "payload", "dropper"
	});

	public FrozenSet<string> PhoneContextKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"phone", "tel", "telephone", "call", "contact", "fax", "mobile", "cell", "whatsapp"
	});

	public FrozenSet<string> SocialPlatformKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"twitter", "telegram", "discord", "forum", "reddit", "facebook", "instagram", "tiktok"
	});



	public FrozenSet<string> MitreKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"mitre", "att&ck", "attack", "technique", "tactic", "procedure", "ttp"
	});

	public FrozenSet<string> SuspiciousTlds { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"tk", "ml", "ga", "cf", "gq", "top", "xyz", "work", "click", "loan", "download"
	});

	// ─────────────────────────────────────────────
	// Configurable False Positive Sets
	// ─────────────────────────────────────────────

	public FrozenSet<string> HashExclusions { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"d41d8cd98f00b204e9800998ecf8427e", // MD5 empty
        "da39a3ee5e6b4b0d3255bfef95601890afd80709", // SHA1 empty
        "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", // SHA256 empty
        "00000000000000000000000000000000",
		"ffffffffffffffffffffffffffffffff",
		"0000000000000000000000000000000000000000",
		"ffffffffffffffffffffffffffffffffffffffff"
	}, StringComparer.OrdinalIgnoreCase);

	public FrozenSet<string> PrivateIpPrefixes { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"10.", "192.168.", "172.16.", "172.17.", "172.18.", "172.19.",
		"172.20.", "172.21.", "172.22.", "172.23.", "172.24.", "172.25.",
		"172.26.", "172.27.", "172.28.", "172.29.", "172.30.", "172.31.",
		"127.", "0.", "169.254."
	});

	public FrozenSet<string> FalsePositiveDomains { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"example.com", "example.org", "example.net",
		"localhost", "localhost.localdomain",
		"test.com", "test.local",
		"domain.com", "your-domain.com",
		"placeholder.com", "contoso.com"
	}, StringComparer.OrdinalIgnoreCase);

	// ─────────────────────────────────────────────
	// Custom Patterns
	// ─────────────────────────────────────────────

	public List<IIndicatorPattern> CustomPatterns { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════════
// MODELS
// ═══════════════════════════════════════════════════════════════════════════════
