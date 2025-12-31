using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using IIM.Ingestion.Extensions;
using IIM.Ingestion.Models;
using IIM.Shared.Dtos;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IIM.Ingestion.Services;

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
	// IMPROVED: Added negative lookbehind for date-like patterns (MM-DD- or DD-MM-)
	[GeneratedRegex(@"(?<![\d\./-]|\d{2}-\d{2}-)\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b(?![\d\./-])")]
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


	// Matches international format with optional country code
	[GeneratedRegex(@"(?i)(?:(?:mobile\s*)?phone|tel|call|contact|fax|mobile|cell)\s*[:\s]?\s*(\+?\d{1,4}[\s.-]?\(?\d{1,4}\)?[\s.-]?\d{1,4}[\s.-]?\d{1,4}[\s.-]?\d{1,9})\b")]
	private static partial Regex PhoneRegex();

	// NEW: Direct phone number pattern for numbers with country code (high confidence)
	[GeneratedRegex(@"\+1\d{10}\b")]
	private static partial Regex UsPhoneDirectRegex();

	#endregion

	#region Cryptocurrency

	// Bitcoin address pattern (P2PKH, P2SH, Bech32)
	[GeneratedRegex(@"\b(?:bc1|[13])[a-zA-HJ-NP-Z0-9]{25,39}\b")]
	private static partial Regex BitcoinAddressRegex();

	// Ethereum address pattern (0x + 40 hex)
	[GeneratedRegex(@"\b0x[a-fA-F0-9]{40}\b")]
	private static partial Regex EthereumAddressRegex();

	// Monero address pattern
	[GeneratedRegex(@"\b4[0-9AB][1-9A-HJ-NP-Za-km-z]{93}\b")]
	private static partial Regex MoneroAddressRegex();

	// Ethereum Transaction ID (0x + 64 hex chars)
	[GeneratedRegex(@"\b0x[a-fA-F0-9]{64}\b")]
	private static partial Regex EthereumTxIdRegex();

	// Bitcoin Transaction ID (64 hex chars - same as SHA256, differentiated by context)
	// Note: This is handled via SHA256 regex with context-based type determination

	#endregion

	#region Crypto Context Keywords

	private static readonly FrozenSet<string> BitcoinContextKeywords = FrozenSet.ToFrozenSet(new[]
	{
		"btc", "bitcoin", "satoshi", "sats", "wallet", "address", "sending", "receiving",
		"bc1", "segwit", "bech32", "p2pkh", "p2sh", "utxo", "blockchain"
	}, StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> EthereumContextKeywords = FrozenSet.ToFrozenSet(new[]
	{
		"eth", "ether", "ethereum", "gwei", "wei", "wallet", "address", "erc20", "erc721",
		"contract", "defi", "metamask", "etherscan"
	}, StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> TransactionContextKeywords = FrozenSet.ToFrozenSet(new[]
	{
		"tx", "txid", "txhash", "transaction", "transfer", "sent", "received", "payment",
		"block", "confirmation", "mempool", "fee", "input", "output"
	}, StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> GenericCryptoKeywords = FrozenSet.ToFrozenSet(new[]
	{
		"crypto", "cryptocurrency", "coin", "token", "exchange", "deposit", "withdrawal"
	}, StringComparer.OrdinalIgnoreCase);

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

	// IMPROVED: Handle multi-word values and more label variations
	// Captures Label (Group 1) and Value (Group 2) - allows spaces in value for names
	[GeneratedRegex(@"(?i)(ESP\s*User\s*ID|User\s*ID|Display\s*Name|UUID|Screen/?User\s*Name|(?<!file\s*)(?:Full\s*)?Name)\s*[:=-]\s*(.+?)(?=\r?\n|$)", RegexOptions.Compiled)]
	private static partial Regex HardenedUsernameRegex();

	// NEW: Specifically for "Name: First Last" pattern in structured reports
	[GeneratedRegex(@"(?i)^(?:Suspect\s*)?Name\s*:\s*([A-Z][a-z]+(?:\s+[A-Z][a-z]+)+)\s*$", RegexOptions.Multiline)]
	private static partial Regex FullNameRegex();

	public FrozenSet<string> UsernameStopWords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"the", "system", "agent", "child", "members", "identifier", "company", "unknown", "null", "undefined",
		"verified", "not", "verified)", "(verified", "(verified)", "utc", "at"
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

	#region Dates (for filtering false positives)

	// NEW: Pattern to identify date strings that might be confused with other indicators
	[GeneratedRegex(@"\b(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])-(?:19|20)\d{2}\b")]
	private static partial Regex DateMmDdYyyyRegex();

	[GeneratedRegex(@"\b(?:19|20)\d{2}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])\b")]
	private static partial Regex DateYyyyMmDdRegex();

	// NEW: Date of Birth specific pattern
	[GeneratedRegex(@"(?i)(?:date\s*of\s*birth|dob|birth\s*date|birthday)\s*[:\s]\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})")]
	private static partial Regex DateOfBirthRegex();

	#endregion

	#region Darknet and Anonymity

	// Onion v2 (16 chars) and v3 (56 chars) addresses
	[GeneratedRegex(@"\b[a-z2-7]{16}(?:[a-z2-7]{40})?\.onion\b", RegexOptions.IgnoreCase)]
	private static partial Regex OnionAddressRegex();

	// IPFS CID v0 (Qm + 44 base58) and v1 (bafy... base32)
	[GeneratedRegex(@"\b(?:Qm[1-9A-HJ-NP-Za-km-z]{44}|b[a-z2-7]{58})\b")]
	private static partial Regex IpfsCidRegex();

	#endregion

	#region Cryptographic Identifiers

	// PGP/GPG Key ID (short 8 hex or long 16 hex, often prefixed with 0x)
	[GeneratedRegex(@"(?i)(?:pgp|gpg|key\s*id)[:\s]*(?:0x)?([a-fA-F0-9]{8}(?:[a-fA-F0-9]{8})?)\b")]
	private static partial Regex PgpKeyIdRegex();

	// PGP Fingerprint (40 hex chars, often with spaces)
	[GeneratedRegex(@"\b(?:[a-fA-F0-9]{4}\s+){9}[a-fA-F0-9]{4}\b")]
	private static partial Regex PgpFingerprintRegex();

	#endregion

	#region Mobile Device Identifiers

	// IMEI (15 digits, sometimes with hyphens)
	[GeneratedRegex(@"(?i)(?:imei)[:\s]*(\d{2}[-\s]?\d{6}[-\s]?\d{6}[-\s]?\d)\b")]
	private static partial Regex ImeiRegex();

	#endregion

	#region ESP Report Specific

	// NEW: ESP User ID that may have value on next line
	[GeneratedRegex(@"(?i)ESP\s*User\s*ID\s*:\s*(?:(\S+)|[\r\n]+(\S+))")]
	private static partial Regex EspUserIdRegex();

	// NEW: Profile URL pattern
	[GeneratedRegex(@"(?i)Profile\s*URL\s*[:\s]\s*(https?://[^\s]+)")]
	private static partial Regex ProfileUrlRegex();

	// NEW: Approximate/Estimated Age
	[GeneratedRegex(@"(?i)(?:Approximate|Estimated)?\s*Age\s*[:\s]\s*(\d{1,3})")]
	private static partial Regex AgeRegex();

	#endregion

	#region Events and Timestamps

	// IMPROVED: Matches timestamps with optional UTC/Timezone and handles log/table formatting

	[GeneratedRegex(@"\b(?:\d{4}[-/]\d{1,2}[-/]\d{1,2}|\d{1,2}[-/]\d{1,2}[-/]\d{2,4})[\sT_]\d{1,2}:\d{2}(?::\d{2})?(?:\s?[APMapm]{2})?(?:\s+[A-Z]{3,4})?\b")]
	private static partial Regex GeneralTimestampRegex();


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
			filter: v => !IsLikelyDate(v),
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
			// High-confidence direct US phone numbers with country code
			patterns.Add(new RegexPattern(IndicatorType.PhoneNumber, UsPhoneDirectRegex(), "US", 0.9f,
				confidenceAdjuster: (v, ctx, opts) => CalculatePhoneConfidence(v, ctx, opts)));

			// Context-based phone extraction
			patterns.Add(new RegexPattern(IndicatorType.PhoneNumber, PhoneRegex(), null, 0.5f,
				confidenceAdjuster: (v, ctx, opts) => CalculatePhoneConfidence(v, ctx, opts)));
		}

		// Cryptocurrency - Order matters: more specific patterns first

		// Ethereum Transaction ID (0x + 64 hex) - check BEFORE ETH address
		patterns.Add(new RegexPattern(IndicatorType.CryptoTransaction, EthereumTxIdRegex(), "Ethereum", 0.6f,
			confidenceAdjuster: (v, ctx, opts) => CalculateEthTxConfidence(v, ctx)));

		// Ethereum Address (0x + 40 hex)
		patterns.Add(new RegexPattern(IndicatorType.CryptoAddress, EthereumAddressRegex(), "Ethereum", 0.5f,
			filter: v => !IsLikelyNonCryptoHex(v),
			confidenceAdjuster: (v, ctx, opts) => CalculateEthAddressConfidence(v, ctx)));

		// Bitcoin Address (bc1, 1, or 3 prefix)
		patterns.Add(new RegexPattern(IndicatorType.CryptoAddress, BitcoinAddressRegex(), "Bitcoin", 0.6f,
			confidenceAdjuster: (v, ctx, opts) => CalculateBtcAddressConfidence(v, ctx)));

		// Monero Address
		patterns.Add(new RegexPattern(IndicatorType.CryptoAddress, MoneroAddressRegex(), "Monero", 0.95f));

		// Darknet / Anonymity
		patterns.Add(new RegexPattern(IndicatorType.OnionAddress, OnionAddressRegex(), null, 0.95f));
		patterns.Add(new RegexPattern(IndicatorType.IpfsCid, IpfsCidRegex(), null, 0.9f));

		// Hashes - ordered by length (longest first)
		// Note: SHA256 (64 hex) can also be BTC transaction - handled by confidence adjuster
		patterns.Add(new RegexPattern(IndicatorType.FileHash, Sha512Regex(), "SHA512", 0.9f,
			filter: v => IsLikelyHash(v, options)));
		patterns.Add(new RegexPattern(IndicatorType.FileHash, Sha256Regex(), "SHA256", 0.7f,
			filter: v => IsLikelyHash(v, options),
			confidenceAdjuster: (v, ctx, opts) => CalculateSha256Confidence(v, ctx, opts)));
		patterns.Add(new RegexPattern(IndicatorType.FileHash, Sha1Regex(), "SHA1", 0.7f,
			filter: v => IsLikelyHash(v, options) && !IsEthereumAddress(v),
			confidenceAdjuster: (v, ctx, opts) => CalculateHashConfidence(v, ctx, "SHA1", opts)));
		patterns.Add(new RegexPattern(
			IndicatorType.FileHash,
			Md5Regex(),
			"MD5",
			0.7f,
			filter: v => IsLikelyHash(v, options) && !IsLikelyGuid(v),
			confidenceAdjuster: (v, ctx, opts) =>
			{
				var score = CalculateHashConfidence(v, ctx, "MD5", opts);
				if (ctx.SurroundingLower.Contains("md5"))
					score = Math.Max(score, 0.95f);
				return score;
			}));

		// File system
		patterns.Add(new RegexPattern(IndicatorType.FilePath, WindowsPathRegex(), "Windows", 0.75f));
		patterns.Add(new RegexPattern(IndicatorType.RegistryKey, RegistryKeyRegex(), null, 0.9f));

		patterns.Add(new RegexPattern(
			IndicatorType.FileName,
			FilenameRegex(),
			null,
			0.85f,
			confidenceAdjuster: (v, ctx, opts) =>
			{
				if (ctx.SurroundingLower.Contains("filename") || ctx.SurroundingLower.Contains("file:"))
					return 0.95f;
				return 0.85f;
			}));

		// Security identifiers
		patterns.Add(new RegexPattern(IndicatorType.Cve, CveRegex(), null, 0.99f));
		patterns.Add(new RegexPattern(IndicatorType.MitreAttack, MitreAttackRegex(), null, 0.5f,
			confidenceAdjuster: (v, ctx, opts) => CalculateMitreConfidence(v, ctx, opts)));

		// Cryptographic identifiers
		patterns.Add(new CaptureGroupPattern(IndicatorType.PgpKeyId, PgpKeyIdRegex(), 1, null, 0.9f));
		patterns.Add(new RegexPattern(IndicatorType.PgpFingerprint, PgpFingerprintRegex(), null, 0.85f));

		// Mobile device identifiers
		if (options.ExtractDeviceIdentifiers)
		{
			patterns.Add(new CaptureGroupPattern(IndicatorType.Imei, ImeiRegex(), 1, null, 0.9f));
		}

		patterns.Add(new RegexPattern(IndicatorType.Timestamp,GeneralTimestampRegex(),"DateTime", 1.0f));

		// Add custom patterns from options
		patterns.AddRange(options.CustomPatterns);

		return patterns;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// PUBLIC API
	// ═══════════════════════════════════════════════════════════════════════════

	public ExtractionResult Extract(string text, DocumentShapeResult shape = null)
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

		// Pre-extract date positions to filter false positives
		var datePositions = ExtractDatePositions(normalizedText);

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

				if (pattern.Type != IndicatorType.Timestamp && OverlapsWithDate(match.Offset, match.Length, datePositions))
					continue;


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

		// Extract full names from structured reports
		ExtractFullNames(normalizedText, boundaries, occurrences);

		// Extract Unix paths (special handling)
		ExtractUnixPaths(normalizedText, text, boundaries, occurrences);

		// Extract ESP-specific fields
		ExtractEspFields(normalizedText, boundaries, occurrences);

		// Extract phone numbers with "Mobile Phone:" prefix
		if (_options.ExtractPhoneNumbers)
		{
			ExtractMobilePhones(normalizedText, boundaries, occurrences);
		}

		// Extract Date of Birth as PII
		ExtractDateOfBirth(normalizedText, boundaries, occurrences);

		// Extract Bitcoin transactions (SHA256-like patterns with BTC context)
		ExtractBitcoinTransactions(normalizedText, boundaries, occurrences);

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

		var identityGroups = GroupOccurrencesSemantically(filteredOccurrences, text, shape);

		var proposedEvents = BuildProposedEvents(filteredOccurrences, text, shape);


		var result = new ExtractionResult
		{
			Indicators = indicators,

			Occurrences = occurrences, // The only place with "Heavy" context

			IdentityGroups = identityGroups.Select(g => new EntityGroupDto
			{
				GroupId = g.GroupId,
				Category = g.Category.ToString(),
				Label = g.Label,
				GroupConfidence = g.GroupConfidence,
				Members = g.Members.Select(m => new IndicatorSummary(m.Type.ToString(), m.Value)).ToList()
			}).ToList(),

			ProposedEvents = ProposedEventsMapper.MapToDto(proposedEvents)
		};

		
		return result;
	

	}

	// ═══════════════════════════════════════════════════════════════════════════
	// DATE FILTERING HELPERS
	// ═══════════════════════════════════════════════════════════════════════════

	private List<(int Start, int End)> ExtractDatePositions(string text)
	{
		var positions = new List<(int Start, int End)>();

		foreach (Match m in DateMmDdYyyyRegex().Matches(text))
		{
			positions.Add((m.Index, m.Index + m.Length));
		}

		foreach (Match m in DateYyyyMmDdRegex().Matches(text))
		{
			positions.Add((m.Index, m.Index + m.Length));
		}

		// Also capture timestamps like "01-18-2025 14:53:27"
		var timestampRegex = new Regex(@"\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2}");
		foreach (Match m in timestampRegex.Matches(text))
		{
			positions.Add((m.Index, m.Index + m.Length));
		}

		return positions;
	}

	private static bool OverlapsWithDate(int start, int length, List<(int Start, int End)> datePositions)
	{
		var end = start + length;
		return datePositions.Any(d => start < d.End && end > d.Start);
	}

	private static bool IsLikelyDate(string value)
	{
		// Check if the value looks like a date (e.g., "07-17-1990" parsed as IP-like)
		var parts = value.Split('.');
		if (parts.Length == 4)
		{
			// If any part looks like a year (1900-2099), it's probably a date
			foreach (var part in parts)
			{
				if (int.TryParse(part, out var num) && num >= 1900 && num <= 2099)
					return true;
			}
		}
		return false;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// COLLISION RESOLUTION
	// ═══════════════════════════════════════════════════════════════════════════

	private void ResolveCollisions(List<IndicatorOccurrence> occurrences)
	{
		// Group by value to find conflicts
		var byValue = occurrences.GroupBy(o => o.Value).Where(g => g.Count() > 1);

		foreach (var group in byValue)
		{
			var items = group.ToList();

			// Handle Crypto Address vs Hash conflicts
			var cryptoMatch = items.FirstOrDefault(o => o.Type == IndicatorType.CryptoAddress);
			var hashMatch = items.FirstOrDefault(o => o.Type == IndicatorType.FileHash);

			if (cryptoMatch != null && hashMatch != null)
			{
				// If hash context is stronger, demote crypto
				if (hashMatch.Confidence > cryptoMatch.Confidence)
				{
					cryptoMatch.Confidence = 0.1f;
				}
				// If crypto context is stronger, demote hash
				else if (cryptoMatch.Confidence > hashMatch.Confidence)
				{
					hashMatch.Confidence = 0.1f;
				}
			}

			// Handle SHA256 Hash vs BTC Transaction conflicts
			var sha256Match = items.FirstOrDefault(o => o.Type == IndicatorType.FileHash && o.Subtype == "SHA256");
			var btcTxMatch = items.FirstOrDefault(o => o.Type == IndicatorType.CryptoTransaction && o.Subtype == "Bitcoin");

			if (sha256Match != null && btcTxMatch != null)
			{
				if (btcTxMatch.Confidence > sha256Match.Confidence)
				{
					sha256Match.Confidence = 0.1f;
				}
				else
				{
					btcTxMatch.Confidence = 0.1f;
				}
			}

			// Handle ETH Transaction (0x + 64 hex) vs SHA256
			var ethTxMatch = items.FirstOrDefault(o => o.Type == IndicatorType.CryptoTransaction && o.Subtype == "Ethereum");
			if (ethTxMatch != null && sha256Match != null)
			{
				if (ethTxMatch.Confidence > sha256Match.Confidence)
				{
					sha256Match.Confidence = 0.1f;
				}
				else
				{
					ethTxMatch.Confidence = 0.1f;
				}
			}
		}
	}

	/// <summary>
	/// Extract Bitcoin transactions from SHA256-like patterns with BTC context
	/// </summary>
	private void ExtractBitcoinTransactions(string text, TextBoundaries boundaries, List<IndicatorOccurrence> occurrences)
	{
		// Find all 64-char hex strings that might be BTC transactions
		foreach (Match m in Sha256Regex().Matches(text))
		{
			var value = m.Value;
			var ctx = boundaries.BuildContext(m.Index);

			// Check for BTC transaction context
			bool hasBtcContext = ContainsAnyKeyword(ctx.SurroundingLower, BitcoinContextKeywords);
			bool hasTxContext = ContainsAnyKeyword(ctx.SurroundingLower, TransactionContextKeywords);

			if (hasBtcContext && hasTxContext)
			{
				float confidence = 0.5f;
				confidence += hasBtcContext ? 0.3f : 0f;
				confidence += hasTxContext ? 0.2f : 0f;

				// Penalty if hash keywords present
				if (ctx.SurroundingLower.Contains("sha256") || ctx.SurroundingLower.Contains("hash:"))
				{
					confidence -= 0.3f;
				}

				if (confidence >= 0.6f)
				{
					occurrences.Add(new IndicatorOccurrence
					{
						Id = Guid.NewGuid(),
						Type = IndicatorType.CryptoTransaction,
						Subtype = "Bitcoin",
						Value = value,
						Offset = m.Index,
						Length = m.Length,
						Context = ctx,
						Confidence = Math.Clamp(confidence, 0.1f, 1.0f)
					});
				}
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
			string rawValue = m.Groups[2].Value.Trim();

			// Clean up trailing punctuation and verification notes
			string username = CleanUsernameValue(rawValue);

			// Hardening Check: Skip noise, short strings, or common stop-words
			if (username.Length < 2 || UsernameStopWords.Contains(username))
				continue;

			// Skip if it looks like a date or timestamp
			if (Regex.IsMatch(username, @"^\d{2}[-/]\d{2}[-/]\d{2,4}"))
				continue;

			var ctx = boundaries.BuildContext(m.Index);
			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.Username,
				Subtype = DetermineUsernameSubtype(label),
				Value = username,
				Offset = m.Index,
				Length = m.Length,
				Context = ctx,
				Confidence = CalculateHardenedConfidence(username, label, ctx, _options)
			});
		}

		// 2. Social handles (@username)
		foreach (Match m in SocialHandleRegex().Matches(text))
		{
			var rawHandle = m.Groups[1].Value;

			if (UsernameStopWords.Contains(rawHandle)) continue;

			var handle = "@" + rawHandle;
			var ctx = boundaries.BuildContext(m.Index);

			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.Username,
				Subtype = "SocialHandle",
				Value = handle,
				Offset = m.Index,
				Length = m.Length,
				Context = ctx,
				Confidence = 0.85f
			});
		}
	}

	/// <summary>
	/// Extract full names from "Name: First Last" patterns in structured reports
	/// </summary>
	private void ExtractFullNames(string text, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		foreach (Match m in FullNameRegex().Matches(text))
		{
			var fullName = m.Groups[1].Value.Trim();

			// Validate it looks like a real name (2-4 words, each capitalized)
			var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (nameParts.Length < 2 || nameParts.Length > 4)
				continue;

			// Check if already extracted to avoid duplicates
			if (sink.Any(s => s.Type == IndicatorType.Username &&
							  s.Value.Equals(fullName, StringComparison.OrdinalIgnoreCase)))
				continue;

			var ctx = boundaries.BuildContext(m.Index);
			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.Username,
				Subtype = "FullName",
				Value = fullName,
				Offset = m.Groups[1].Index,
				Length = m.Groups[1].Length,
				Context = ctx,
				Confidence = 0.95f,
				Metadata = new Dictionary<string, string>
				{
					["NameType"] = "PersonName",
					["FirstName"] = nameParts[0],
					["LastName"] = nameParts[^1]
				}
			});
		}
	}

	/// <summary>
	/// Clean up username values by removing trailing punctuation and verification notes
	/// </summary>
	private static string CleanUsernameValue(string value)
	{
		// Remove common suffixes like "(Verified)", "(Not Verified)", timestamps
		var cleaned = Regex.Replace(value, @"\s*\((?:Not\s+)?Verified.*?\)\s*$", "", RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @"\s*\d{2}-\d{2}-\d{4}.*$", ""); // Remove trailing dates
		cleaned = cleaned.TrimEnd('.', ',', ';', ':', ' ');
		return cleaned.Trim();
	}

	/// <summary>
	/// Determine the appropriate subtype based on the label
	/// </summary>
	private static string DetermineUsernameSubtype(string label)
	{
		var lower = label.ToLower();
		if (lower.Contains("esp user id")) return "EspUserId";
		if (lower.Contains("user id")) return "UserId";
		if (lower.Contains("uuid")) return "UUID";
		if (lower.Contains("display name")) return "DisplayName";
		if (lower.Contains("screen") || lower.Contains("user name")) return "ScreenName";
		if (lower.Contains("name")) return "Name";
		return "Contextual";
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

	/// <summary>
	/// Extract ESP-specific fields like Filename, MD5, ESP User ID
	/// </summary>
	private void ExtractEspFields(string text, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		// Filename and MD5 extraction
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
					Id = Guid.NewGuid(),
					Type = IndicatorType.FileName,
					Value = value,
					Confidence = 0.98f,
					Context = ctx,
					Offset = m.Groups[2].Index,
					Length = value.Length
				});
			}
		}

		// ESP User ID (may have value on next line)
		foreach (Match m in EspUserIdRegex().Matches(text))
		{
			var value = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
			if (string.IsNullOrWhiteSpace(value)) continue;

			value = value.Trim();
			var ctx = boundaries.BuildContext(m.Index);

			// Avoid duplicates
			if (sink.Any(s => s.Type == IndicatorType.Username &&
							  s.Subtype == "EspUserId" &&
							  s.Value == value))
				continue;

			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.Username,
				Subtype = "EspUserId",
				Value = value,
				Confidence = 0.98f,
				Context = ctx,
				Offset = m.Index,
				Length = m.Length
			});
		}
	}

	/// <summary>
	/// Extract phone numbers with "Mobile Phone:" prefix specifically
	/// </summary>
	private void ExtractMobilePhones(string text, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		var mobilePhoneRegex = new Regex(@"(?i)Mobile\s*Phone\s*:\s*(\+?\d[\d\s\-\(\)]{8,20})");

		foreach (Match m in mobilePhoneRegex.Matches(text))
		{
			var phoneNumber = m.Groups[1].Value.Trim();

			// Normalize: remove spaces and dashes for comparison
			var normalized = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

			// Skip if too short or too long
			if (normalized.Length < 10 || normalized.Length > 15)
				continue;

			// Check for duplicates
			if (sink.Any(s => s.Type == IndicatorType.PhoneNumber &&
							  Regex.Replace(s.Value, @"[\s\-\(\)]", "") == normalized))
				continue;

			var ctx = boundaries.BuildContext(m.Index);

			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.PhoneNumber,
				Subtype = "Mobile",
				Value = phoneNumber,
				Confidence = 0.95f, // High confidence due to explicit label
				Context = ctx,
				Offset = m.Groups[1].Index,
				Length = m.Groups[1].Length,
				Metadata = new Dictionary<string, string>
				{
					["Normalized"] = normalized,
					["HasCountryCode"] = normalized.StartsWith("+") ? "true" : "false"
				}
			});
		}
	}

	/// <summary>
	/// Extract Date of Birth as a PII indicator
	/// </summary>
	private void ExtractDateOfBirth(string text, TextBoundaries boundaries, List<IndicatorOccurrence> sink)
	{
		foreach (Match m in DateOfBirthRegex().Matches(text))
		{
			var dob = m.Groups[1].Value.Trim();
			var ctx = boundaries.BuildContext(m.Index);

			sink.Add(new IndicatorOccurrence
			{
				Id = Guid.NewGuid(),
				Type = IndicatorType.DateOfBirth,
				Subtype = "DOB",
				Value = dob,
				Confidence = 0.95f,
				Context = ctx,
				Offset = m.Groups[1].Index,
				Length = m.Groups[1].Length
			});
		}
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// DEFANGING
	// ═══════════════════════════════════════════════════════════════════════════

	private static string Refang(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

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
		float confidence = 0.8f;

		if (ReservedIpPrefixes.Any(p => value.StartsWith(p)))
		{
			return 0.1f;
		}

		bool isPrivate = opts.PrivateIpPrefixes.Any(p => value.StartsWith(p));
		if (isPrivate)
		{
			confidence = ContainsAnyKeyword(ctx.SurroundingLower, opts.NetworkKeywords) ? 0.4f : 0.2f;
		}

		if (ContainsAnyKeyword(ctx.PrecedingWords.Select(w => w.ToLower()).ToList(), SoftwareVersionKeywords))
		{
			return 0.2f;
		}

		// Penalty if near "Date of Birth" or "DOB"
		if (ctx.SurroundingLower.Contains("birth") || ctx.SurroundingLower.Contains("dob") ||
			ctx.SurroundingLower.Contains("age"))
		{
			return 0.1f;
		}

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
		if (!value.Contains('.') || value.EndsWith(".") || value.StartsWith("."))
			return 0.1f;

		var parts = value.Split('.');
		var suffix = parts.Last();

		if (!ValidTlds.Contains(suffix))
		{
			if (suffix.Length > 6) return 0.1f;
			return 0.3f;
		}

		if (ForbiddenExtensions.Contains(suffix))
		{
			return 0.1f;
		}

		float confidence = 0.8f;

		if (ctx.SurroundingLower.Contains("url") || ctx.SurroundingLower.Contains("domain") || ctx.SurroundingLower.Contains("http"))
		{
			confidence = Math.Min(1.0f, confidence + 0.15f);
		}

		return confidence;
	}

	private static float CalculatePhoneConfidence(string value, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		var confidence = 0.5f;

		if (value.StartsWith("+"))
			confidence += 0.3f;

		if (ctx.SurroundingLower.Contains("mobile") || ctx.SurroundingLower.Contains("phone") ||
			ctx.SurroundingLower.Contains("cell") || ctx.SurroundingLower.Contains("tel"))
			confidence += 0.4f;

		// Strong penalty if near date-related context
		if (ctx.SurroundingLower.Contains("birth") || ctx.SurroundingLower.Contains("dob") ||
			ctx.SurroundingLower.Contains("date"))
			confidence -= 0.5f;

		return Math.Clamp(confidence, 0.1f, 1.0f);
	}

	private static float CalculateHashConfidence(string value, IndicatorContext ctx, string hashType, IndicatorExtractorOptions opts)
	{
		var confidence = hashType switch
		{
			"SHA512" => 0.9f,
			"SHA256" => 0.85f,
			"SHA1" => 0.7f,
			"MD5" => 0.65f,
			_ => 0.5f
		};

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

	/// <summary>
	/// Special handling for SHA256 - could be hash OR BTC transaction
	/// </summary>
	private static float CalculateSha256Confidence(string value, IndicatorContext ctx, IndicatorExtractorOptions opts)
	{
		// Check for BTC transaction context first
		bool hasBtcContext = ContainsAnyKeyword(ctx.SurroundingLower, BitcoinContextKeywords);
		bool hasTxContext = ContainsAnyKeyword(ctx.SurroundingLower, TransactionContextKeywords);

		// If it looks like a BTC transaction, reduce hash confidence
		if (hasBtcContext && hasTxContext)
		{
			return 0.3f; // Low confidence as hash - likely a BTC tx
		}

		if (hasTxContext && !ContainsAnyKeyword(ctx.SurroundingLower, opts.HashKeywords))
		{
			return 0.4f; // Probably a transaction, not a hash
		}

		// Otherwise, calculate normal hash confidence
		return CalculateHashConfidence(value, ctx, "SHA256", opts);
	}

	/// <summary>
	/// Calculate confidence for Bitcoin addresses vs other base58 strings
	/// </summary>
	private static float CalculateBtcAddressConfidence(string value, IndicatorContext ctx)
	{
		float confidence = 0.5f; // Start lower - many false positives possible

		// Strong boost for explicit BTC context
		if (ContainsAnyKeyword(ctx.SurroundingLower, BitcoinContextKeywords))
		{
			confidence += 0.4f;
		}

		// Boost for generic crypto context
		if (ContainsAnyKeyword(ctx.SurroundingLower, GenericCryptoKeywords))
		{
			confidence += 0.2f;
		}

		// Penalty if hash context is present (likely not a BTC address)
		if (ctx.SurroundingLower.Contains("md5") || ctx.SurroundingLower.Contains("sha") ||
			ctx.SurroundingLower.Contains("hash") || ctx.SurroundingLower.Contains("checksum"))
		{
			confidence -= 0.4f;
		}

		// bc1 addresses (Bech32) are more definitively Bitcoin
		if (value.StartsWith("bc1", StringComparison.OrdinalIgnoreCase))
		{
			confidence += 0.2f;
		}

		return Math.Clamp(confidence, 0.1f, 1.0f);
	}

	/// <summary>
	/// Calculate confidence for Ethereum addresses (0x + 40 hex)
	/// </summary>
	private static float CalculateEthAddressConfidence(string value, IndicatorContext ctx)
	{
		float confidence = 0.5f;

		// Strong boost for ETH context
		if (ContainsAnyKeyword(ctx.SurroundingLower, EthereumContextKeywords))
		{
			confidence += 0.4f;
		}

		// Boost for generic crypto context
		if (ContainsAnyKeyword(ctx.SurroundingLower, GenericCryptoKeywords))
		{
			confidence += 0.2f;
		}

		// Penalty if it looks like a memory address or debug output
		if (ctx.SurroundingLower.Contains("0x00000") || ctx.SurroundingLower.Contains("memory") ||
			ctx.SurroundingLower.Contains("pointer") || ctx.SurroundingLower.Contains("address:"))
		{
			// "address:" alone could be crypto, but with other indicators it's likely not
			if (!ContainsAnyKeyword(ctx.SurroundingLower, EthereumContextKeywords))
			{
				confidence -= 0.3f;
			}
		}

		return Math.Clamp(confidence, 0.1f, 1.0f);
	}

	/// <summary>
	/// Calculate confidence for Ethereum transaction IDs (0x + 64 hex)
	/// </summary>
	private static float CalculateEthTxConfidence(string value, IndicatorContext ctx)
	{
		float confidence = 0.4f; // Start low - could be SHA256 hash

		// Strong boost for ETH context
		if (ContainsAnyKeyword(ctx.SurroundingLower, EthereumContextKeywords))
		{
			confidence += 0.4f;
		}

		// Strong boost for transaction context
		if (ContainsAnyKeyword(ctx.SurroundingLower, TransactionContextKeywords))
		{
			confidence += 0.3f;
		}

		// Penalty if hash context
		if (ctx.SurroundingLower.Contains("sha256") || ctx.SurroundingLower.Contains("hash:") ||
			ctx.SurroundingLower.Contains("file"))
		{
			confidence -= 0.4f;
		}

		return Math.Clamp(confidence, 0.1f, 1.0f);
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

		if (label.Contains("esp user id") || label.Contains("uuid")) return 0.98f;
		if (label.Contains("user id")) return 0.95f;
		if (label.Contains("display name")) return 0.90f;
		if (label.Contains("screen") || label.Contains("user name")) return 0.92f;
		if (label.Contains("name") && value.Contains(' ')) return 0.90f; // Full name

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

		// Check if it has GUID version indicator (char at position 12 is typically 1-5)
		// and variant indicator (char at position 16 is typically 8, 9, a, or b)
		var versionChar = value[12];
		var variantChar = char.ToLower(value[16]);

		// Standard GUIDs have version 1-5 at position 12
		bool hasVersionMarker = versionChar >= '1' && versionChar <= '5';

		// Standard GUIDs have variant marker (8, 9, a, b) at position 16
		bool hasVariantMarker = variantChar == '8' || variantChar == '9' ||
								variantChar == 'a' || variantChar == 'b';

		// Only consider it a GUID if it has BOTH markers (real GUIDs do)
		return hasVersionMarker && hasVariantMarker;
	}

	private static bool IsEthereumAddress(string value)
	{
		return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && value.Length == 42;
	}

	private static bool IsLikelyNonCryptoHex(string value)
	{
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
	// Event AGGREGATION
	// ═══════════════════════════════════════════════════════════════════════════
	private List<ProposedEvent> BuildProposedEvents(
	List<IndicatorOccurrence> occurrences,
	string text,
	DocumentShapeResult shape)
	{
		var events = new List<ProposedEvent>();
		var timestamps = occurrences.Where(o => o.Type == IndicatorType.Timestamp).OrderBy(o => o.Offset).ToList();
		var others = occurrences.Where(o => o.Type != IndicatorType.Timestamp).ToList();

		if (timestamps.Count == 0 || others.Count == 0)
			return events;

		// STRATEGY: If it's a log, group by Line. If it's narrative, group by Absolute Distance.
		bool isLogLike = shape != null && (shape.Shapes.HasFlag(DocumentShape.LogLike) || shape.HasTimestamps);

		var groupings = others.GroupBy(o =>
		{
			if (isLogLike)
			{
				// Try to find a timestamp on the EXACT same line first
				var sameLine = timestamps.FirstOrDefault(ts =>
					GetLineOffset(text, ts.Offset) == GetLineOffset(text, o.Offset));

				if (sameLine != null) return sameLine;
			}

			// Fallback/Default: Nearest timestamp by character distance
			return timestamps
				.OrderBy(ts => Math.Abs(o.Offset - ts.Offset))
				.First();
		});

		foreach (var group in groupings)
		{
			var ts = group.Key;

			// 2. Structural Analysis: Are we inside a table?
			// Look at the context around the timestamp for table markers
			bool isTableContext = ts.Context.SurroundingLower.Contains("|") ||
								  ts.Context.SurroundingLower.Contains("---");

			// 3. Dynamic Filtering based on structure
			var validNearby = group.Where(o =>
			{
				var distance = Math.Abs(o.Offset - ts.Offset);

				// Maximum distance allowed
				if (distance > 500) return false;

				int start = Math.Min(o.Offset, ts.Offset);
				string gap = text.Substring(start, distance);
				int newlineCount = gap.Count(c => c == '\n');

				if (isTableContext)
				{
					// Strict Row-Boundaries: Indicators MUST be on the same line in a table
					return newlineCount == 0;
				}
				else
				{
					// Conversational/List logic: Allow 1 newline for adjacent lines
					return newlineCount <= 1;
				}
			}).ToList();

			if (!validNearby.Any()) continue;

			// 4. Assemble with your existing dictionary logic
			events.Add(new ProposedEvent
			{
				Id = Guid.NewGuid(),
				EventType = DetermineTimestampSubtype(ts.Context) ?? ts.Subtype ?? "Event",
				Timestamp = ts,
				Who = validNearby.Where(o => IsWhoIndicator(o.Type)).ToList(),
				What = validNearby.Where(o => IsWhatIndicator(o.Type)).ToList(),
				Where = validNearby.Where(o => IsWhereIndicator(o.Type)).ToList(),
				Context = ts.Context,
				Confidence = ts.Confidence
			});
		}

		return events;
	}

	// Efficiently find a unique identifier for the line without heavy string splitting
private int GetLineOffset(string text, int offset) => text.AsSpan(0, offset).Count('\n');

	private static readonly Dictionary<string, string> EventKeywords = new(StringComparer.OrdinalIgnoreCase)
{
	{ "upload", "Upload" },
	{ "uploaded", "Upload" },
	{ "download", "Download" },
	{ "downloaded", "Download" },
	{ "login", "Login" },
	{ "logged in", "Login" },
	{ "log in", "Login" },
	{ "signin", "Login" },
	{ "signed in", "Login" },
	{ "sign in", "Login" },
	{ "logout", "Logout" },
	{ "logged out", "Logout" },
	{ "log out", "Logout" },
	{ "signout", "Logout" },
	{ "signed out", "Logout" },
	{ "sign out", "Logout" },
	{ "sent", "Sent" },
	{ "send", "Sent" },
	{ "received", "Received" },
	{ "receive", "Received" },
	{ "access", "Access" },
	{ "accessed", "Access" },
	{ "created", "Created" },
	{ "create", "Created" },
	{ "modified", "Modified" },
	{ "modify", "Modified" },
	{ "updated", "Modified" },
	{ "deleted", "Deleted" },
	{ "delete", "Deleted" },
	{ "posted", "Posted" },
	{ "post", "Posted" },
	{ "registered", "Registered" },
	{ "register", "Registered" },
	{ "transaction", "Transaction" },
	{ "transfer", "Transaction" },
	{ "payment", "Transaction" }
};

	private static string DetermineTimestampSubtype(IndicatorContext ctx)
	{
		var lower = ctx.SurroundingLower;

		// Find all matching keywords and their positions
		var matches = new List<(int position, string eventType)>();

		foreach (var kvp in EventKeywords)
		{
			var idx = lower.IndexOf(kvp.Key, StringComparison.Ordinal);
			if (idx >= 0)
			{
				matches.Add((idx, kvp.Value));
			}
		}

		if (matches.Count == 0)
			return "Event";

		// Return the first one found (closest to start of context)
		// Could also pick closest to timestamp offset if we passed that in
		return matches.OrderBy(m => m.position).First().eventType;
	}


	private static bool IsWhoIndicator(IndicatorType type) =>
		type is IndicatorType.Username or
			   IndicatorType.EmailAddress or
			   IndicatorType.PhoneNumber;

	private static bool IsWhatIndicator(IndicatorType type) =>
		type is IndicatorType.FileHash or
			   IndicatorType.FileName or
			   IndicatorType.FilePath or
			   IndicatorType.Url or
			   IndicatorType.CryptoTransaction;

	private static bool IsWhereIndicator(IndicatorType type) =>
		type is IndicatorType.Domain or
			   IndicatorType.IpAddress or
			   IndicatorType.OnionAddress;

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
			.Where(o => !IsReservedIpv6(o.Value))
			.GroupBy(o => GetIpv6Prefix(o.Value))
			.Where(g => g.Key != null);

		foreach (var group in ipv6Groups)
		{
			var prefix = group.Key!;
			var distinctAddresses = group.Select(g => g.Value).Distinct().ToList();

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

		if (IPAddress.IsLoopback(address)) return true;

		var bytes = address.GetAddressBytes();

		if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80) return true;
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

		Array.Clear(bytes, 8, 8);

		var prefix = new IPAddress(bytes).ToString();
		return prefix + "/64";
	}

	private List<EntityGroup> GroupOccurrencesSemantically(List<IndicatorOccurrence> occurrences,string text, DocumentShapeResult shapeResult) // Pass the shape here
	{
		if (occurrences.Count == 0) return new List<EntityGroup>();

		var groups = new Dictionary<EntityCategory, EntityGroup>();

		foreach (var occ in occurrences)
		{
			var category = DetermineOccurrenceCategory(occ);

			if (!groups.TryGetValue(category, out var group))
			{
				group = new EntityGroup
				{
					GroupId = Guid.NewGuid(),
					Category = category,
					Label = GetLabelForCategory(category),
					Members = new List<IndicatorOccurrence>()
				};
				groups[category] = group;
			}

			var isDuplicate = group.Members.Any(m =>
				m.Type == occ.Type &&
				m.Value.Equals(occ.Value, StringComparison.OrdinalIgnoreCase));

			if (!isDuplicate)
			{
				group.Members.Add(occ);
			}
		}

		// Try to identify related identities within PII group
		if (groups.TryGetValue(EntityCategory.Identity, out var identityGroup))
		{
			var subGroups = ClusterIdentities(identityGroup.Members, text, shapeResult);

			if (subGroups.Count > 1)
			{
				groups.Remove(EntityCategory.Identity);
				foreach (var (index, subGroup) in subGroups.Select((g, i) => (i, g)))
				{
					var key = (EntityCategory)(100 + index);
					groups[key] = new EntityGroup
					{
						GroupId = Guid.NewGuid(),
						Category = EntityCategory.Identity,
						Label = $"Identity",
						Members = subGroup
					};
				}
			}
		}

		// Cluster FileArtifacts by proximity (filename + hash + path that appear together)
		if (groups.TryGetValue(EntityCategory.FileArtifact, out var fileGroup))
		{
			var fileSubGroups = ClusterFileArtifacts(fileGroup.Members, text, shapeResult);
			if (fileSubGroups.Count > 0)
			{
				groups.Remove(EntityCategory.FileArtifact);
				foreach (var (index, subGroup) in fileSubGroups.Select((g, i) => (i, g)))
				{
					var key = (EntityCategory)(200 + index); // Synthetic key for file groups
					var fileName = subGroup.FirstOrDefault(m => m.Type == IndicatorType.FileName)?.Value;
					var label = "File Artifact";

					groups[key] = new EntityGroup
					{
						GroupId = Guid.NewGuid(),
						Category = EntityCategory.FileArtifact,
						Label = label,
						Members = subGroup
					};
				}
			}
		}

		return groups.Values
			.Where(g => g.Members.Count > 0)
			.OrderBy(g => g.Category)
			.ToList();
	}

	/// <summary>
	/// Cluster file-related indicators (FileName, FileHash, FilePath) that appear in proximity
	/// </summary>
	private List<List<IndicatorOccurrence>> ClusterFileArtifacts(
		List<IndicatorOccurrence> fileIndicators,
		string text,
		DocumentShapeResult shape)
	{
		if (fileIndicators.Count == 0) return new List<List<IndicatorOccurrence>>();

		var sorted = fileIndicators.OrderBy(o => o.Offset).ToList();
		var clusters = new List<List<IndicatorOccurrence>>();
		var currentCluster = new List<IndicatorOccurrence> { sorted[0] };

		bool isLogLike = shape.Shapes.HasFlag(DocumentShape.LogLike);
		bool isSectioned = shape.Shapes.HasFlag(DocumentShape.Sectioned);

		for (int i = 1; i < sorted.Count; i++)
		{
			var previous = sorted[i - 1];
			var current = sorted[i];

			// 1. Structural Boundary Check
			bool boundaryCrossed = false;
			if (isLogLike)
			{
				// In logs/tables, different lines = different file artifacts
				boundaryCrossed = GetLineNumber(text, current.Offset) != GetLineNumber(text, previous.Offset);
			}
			else if (isSectioned && shape.Sections?.Count > 0)
			{
				// Ensure artifacts don't merge across section headers
				var prevSection = shape.Sections.LastOrDefault(s => previous.Offset >= s.StartOffset);
				var currSection = shape.Sections.LastOrDefault(s => current.Offset >= s.StartOffset);
				boundaryCrossed = prevSection?.Id != currSection?.Id;
			}

			// 2. Proximity/Context Logic
			int gap = current.Offset - (previous.Offset + previous.Length);
			bool closeEnough = gap < 300;
			bool sameContext = SharesContext(previous, current, text, shape);

			// Logic: Always split on hard boundaries. 
			// Otherwise, use proximity/context for narrative text.
			if (boundaryCrossed || (!closeEnough && !sameContext))
			{
				if (IsValidFileCluster(currentCluster))
				{
					clusters.Add(currentCluster);
				}
				else if (currentCluster.Count > 0)
				{
					clusters.Add(currentCluster);
				}
				currentCluster = new List<IndicatorOccurrence> { current };
			}
			else
			{
				currentCluster.Add(current);
			}
		}

		// Final cluster handling
		if (IsValidFileCluster(currentCluster) || currentCluster.Count > 0)
		{
			clusters.Add(currentCluster);
		}

		return clusters;
	}
	/// <summary>
	/// A valid file cluster should ideally have at least a filename OR a hash
	/// </summary>
	private bool IsValidFileCluster(List<IndicatorOccurrence> cluster)
	{
		if (cluster.Count == 0) return false;

		// Single items are valid
		if (cluster.Count == 1) return true;

		// Multiple items: prefer clusters with diverse types
		var hasFileName = cluster.Any(m => m.Type == IndicatorType.FileName);
		var hasHash = cluster.Any(m => m.Type == IndicatorType.FileHash);
		var hasPath = cluster.Any(m => m.Type == IndicatorType.FilePath);

		// A cluster with filename+hash or path+hash is very meaningful
		return (hasFileName && hasHash) || (hasPath && hasHash) || cluster.Count >= 2;
	}

	private EntityCategory DetermineOccurrenceCategory(IndicatorOccurrence occ)
	{
		return occ.Type switch
		{
			// Identity / PII
			IndicatorType.EmailAddress => EntityCategory.Identity,
			IndicatorType.PhoneNumber => EntityCategory.Identity,
			IndicatorType.DateOfBirth => EntityCategory.Identity,
			IndicatorType.Imei => EntityCategory.Identity,

			// Accounts - but FullName subtypes go to Identity
			IndicatorType.Username when occ.Subtype == "FullName" => EntityCategory.Identity,
			IndicatorType.Username => EntityCategory.Account,
			IndicatorType.CryptoAddress => EntityCategory.Account,
			IndicatorType.CryptoTransaction => EntityCategory.Account,
			IndicatorType.PgpKeyId => EntityCategory.Account,
			IndicatorType.PgpFingerprint => EntityCategory.Account,

			// Organization & Infrastructure
			IndicatorType.Domain => EntityCategory.Org,
			IndicatorType.Url => EntityCategory.Org,
			IndicatorType.IpAddress => EntityCategory.Org,
			IndicatorType.Asn => EntityCategory.Org,
			IndicatorType.MacAddress => EntityCategory.Org,
			IndicatorType.OnionAddress => EntityCategory.Org,

			// File Artifacts (separate category for grouping)
			IndicatorType.FileHash => EntityCategory.FileArtifact,
			IndicatorType.FileName => EntityCategory.FileArtifact,
			IndicatorType.FilePath => EntityCategory.FileArtifact,

			// Threat Indicators
			IndicatorType.RegistryKey => EntityCategory.Threat,
			IndicatorType.Cve => EntityCategory.Threat,
			IndicatorType.MitreAttack => EntityCategory.Threat,
			IndicatorType.Cidr => EntityCategory.Threat,
			IndicatorType.IpfsCid => EntityCategory.Threat,

			_ => EntityCategory.Threat
		};
	}
	private List<List<IndicatorOccurrence>> ClusterIdentities(
		List<IndicatorOccurrence> identityIndicators,
		string text,
		DocumentShapeResult shape)
	{
		if (identityIndicators.Count == 0)
			return new List<List<IndicatorOccurrence>>();

		// If only a few indicators exist, we don't need complex structural clustering
		if (identityIndicators.Count <= 3)
			return new List<List<IndicatorOccurrence>> { identityIndicators };

		// ──────────────────────────────────────────────────────────────────────────
		// 1. First pass: Build raw clusters based on structural boundaries
		// ──────────────────────────────────────────────────────────────────────────
		var sorted = identityIndicators.OrderBy(o => o.Offset).ToList();
		var rawClusters = new List<List<IndicatorOccurrence>>();
		var currentCluster = new List<IndicatorOccurrence> { sorted[0] };

		bool isLogLike = shape.Shapes.HasFlag(DocumentShape.LogLike);
		bool isSectioned = shape.Shapes.HasFlag(DocumentShape.Sectioned);

		for (int i = 1; i < sorted.Count; i++)
		{
			var previous = sorted[i - 1];
			var current = sorted[i];

			// Determine if we crossed a structural "Hard Boundary"
			bool boundaryCrossed = false;

			if (isLogLike)
			{
				// Boundary = New Line
				boundaryCrossed = GetLineNumber(text, current.Offset) != GetLineNumber(text, previous.Offset);
			}
			else if (isSectioned && shape.Sections != null && shape.Sections.Count > 0)
			{
				// Boundary = New Section Header
				var prevSection = shape.Sections.LastOrDefault(s => previous.Offset >= s.StartOffset);
				var currSection = shape.Sections.LastOrDefault(s => current.Offset >= s.StartOffset);
				boundaryCrossed = prevSection?.Id != currSection?.Id;
			}

			int gap = current.Offset - (previous.Offset + previous.Length);

			// Split if hard boundary crossed OR if narrative gap exceeds 500 chars
			if (boundaryCrossed || (gap >= 500 && !SharesContext(previous, current, text, shape)))
			{
				rawClusters.Add(currentCluster);
				currentCluster = new List<IndicatorOccurrence> { current };
			}
			else
			{
				currentCluster.Add(current);
			}
		}
		rawClusters.Add(currentCluster);

		// ──────────────────────────────────────────────────────────────────────────
		// 2. Second pass: Deduplicate and assign unique identities to best clusters
		// ──────────────────────────────────────────────────────────────────────────

		// Group by unique (Type, Value) to ensure we don't have redundant members
		var uniqueIndicators = identityIndicators
			.GroupBy(o => (o.Type, Value: o.Value.ToLowerInvariant()))
			.Select(g => g.First())
			.ToList();

		var finalClusters = rawClusters.Select(_ => new List<IndicatorOccurrence>()).ToList();
		var assigned = new HashSet<(IndicatorType, string)>();

		foreach (var indicator in uniqueIndicators)
		{
			var key = (indicator.Type, indicator.Value.ToLowerInvariant());
			if (assigned.Contains(key)) continue;

			// Use your existing FindBestCluster logic
			int bestClusterIndex = FindBestCluster(indicator, rawClusters, text, shape);
			finalClusters[bestClusterIndex].Add(indicator);
			assigned.Add(key);
		}

		// 3. Cleanup: Remove empty clusters and return
		return finalClusters.Where(c => c.Count > 0).ToList();
	}

	// Helper needed for the isLogLike check
	private int GetLineNumber(string text, int offset) => text.AsSpan(0, offset).Count('\n');


	private int FindBestCluster(IndicatorOccurrence indicator, List<List<IndicatorOccurrence>> clusters, string text, DocumentShapeResult shape)
	{
		int bestIndex = 0;
		int bestScore = -100; // Lower starting point to allow for penalties

		for (int i = 0; i < clusters.Count; i++)
		{
			var cluster = clusters[i];
			if (cluster.Count == 0) continue;

			// --- NEW: STRUCTURAL VALIDATION ---
			// Pick a representative from the cluster to check boundaries
			var representative = cluster[0];

			if (shape.Shapes.HasFlag(DocumentShape.LogLike))
			{
				// In logs, if they aren't on the same line, the score should be zero/penalty
				if (GetLineNumber(text, indicator.Offset) != GetLineNumber(text, representative.Offset))
					continue;
			}
			else if (shape.Shapes.HasFlag(DocumentShape.Sectioned) && shape.Sections?.Count > 0)
			{
				// If they are in different sections, they shouldn't be grouped
				var indicatorSection = shape.Sections.LastOrDefault(s => indicator.Offset >= s.StartOffset);
				var clusterSection = shape.Sections.LastOrDefault(s => representative.Offset >= s.StartOffset);

				if (indicatorSection?.Id != clusterSection?.Id)
					continue;
			}

			int score = 0;

			// Check if this indicator appears in this cluster
			if (cluster.Any(c => c.Type == indicator.Type && c.Value.Equals(indicator.Value, StringComparison.OrdinalIgnoreCase)))
			{
				score += 100;
			}

			// Logic for complementary types (Email + Phone, etc.)
			var clusterTypes = cluster.Select(c => c.Type).Distinct().ToHashSet();

			// [Your existing scoring logic remains here...]
			if (indicator.Type == IndicatorType.PhoneNumber)
			{
				if (clusterTypes.Contains(IndicatorType.EmailAddress)) score += 20;
				// ... etc
			}

			// Proximity Bonus: Closer clusters are better
			int minDistance = cluster.Min(c => Math.Abs(c.Offset - indicator.Offset));
			if (minDistance < 500) score += (500 - minDistance) / 10;

			if (score > bestScore)
			{
				bestScore = score;
				bestIndex = i;
			}
		}

		return bestIndex;
	}

	private bool SharesContext(IndicatorOccurrence a, IndicatorOccurrence b, string text, DocumentShapeResult shape)
	{
		// 1. If it's a log, the only context that matters is the Line
		if (shape.Shapes.HasFlag(DocumentShape.LogLike))
		{
			return GetLineNumber(text, a.Offset) == GetLineNumber(text, b.Offset);
		}

		// 2. Standard Narrative Context
		bool physicalContext = a.Context.Sentence == b.Context.Sentence || a.Context.Block == b.Context.Block;

		// 3. Section Context (Never share context across section headers)
		if (shape.Shapes.HasFlag(DocumentShape.Sectioned) && shape.Sections?.Count > 0)
		{
			var sectionA = shape.Sections.LastOrDefault(s => a.Offset >= s.StartOffset);
			var sectionB = shape.Sections.LastOrDefault(s => b.Offset >= s.StartOffset);

			// If they are in different sections, they definitely DON'T share context
			if (sectionA?.Id != sectionB?.Id) return false;
		}

		return physicalContext;
	}

	private EntityCategory DetermineCategory(List<IndicatorOccurrence> members)
	{
		var types = members.Select(m => m.Type).ToHashSet();
		var subtypes = members.Select(m => m.Subtype?.ToLower()).ToHashSet();

		if (subtypes.Contains("csam") || subtypes.Contains("photodna"))
			return EntityCategory.Identity;

		if (types.Contains(IndicatorType.Domain) || types.Contains(IndicatorType.IpAddress) || types.Contains(IndicatorType.Asn))
			return EntityCategory.Org;

		if (types.Contains(IndicatorType.PhoneNumber) || types.Contains(IndicatorType.EmailAddress) ||
			types.Contains(IndicatorType.DateOfBirth))
			return EntityCategory.Identity;

		if (types.Contains(IndicatorType.CryptoAddress) || types.Contains(IndicatorType.Username))
			return EntityCategory.Account;

		return EntityCategory.Threat;
	}

	private string GetLabelForCategory(EntityCategory category)
	{
		return category switch
		{
			EntityCategory.Identity => "PII",
			EntityCategory.Org => "Organization & Infrastructure",
			EntityCategory.Account => "Accounts & Financial Access",
			EntityCategory.FileArtifact => "File Artifacts",
			EntityCategory.Threat => "Threat Indicators",
			_ => "Uncategorized"
		};
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

public interface IIndicatorPattern
{
	IndicatorType Type { get; }
	string? Subtype { get; }
	IEnumerable<IndicatorOccurrence> Extract(string text, string originalText, TextBoundaries boundaries, IndicatorExtractorOptions options);
}

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

/// <summary>
/// Pattern that extracts a specific capture group from the regex match.
/// Useful for patterns where the label/context is part of the match but we only want the value.
/// </summary>
public sealed class CaptureGroupPattern : IIndicatorPattern
{
	private readonly Regex _regex;
	private readonly int _captureGroupIndex;
	private readonly float _baseConfidence;
	private readonly Func<string, bool>? _filter;
	private readonly Func<string, IndicatorContext, IndicatorExtractorOptions, float>? _confidenceAdjuster;

	public IndicatorType Type { get; }
	public string? Subtype { get; }

	public CaptureGroupPattern(
		IndicatorType type,
		Regex regex,
		int captureGroupIndex,
		string? subtype = null,
		float baseConfidence = 0.7f,
		Func<string, bool>? filter = null,
		Func<string, IndicatorContext, IndicatorExtractorOptions, float>? confidenceAdjuster = null)
	{
		Type = type;
		_regex = regex;
		_captureGroupIndex = captureGroupIndex;
		Subtype = subtype;
		_baseConfidence = baseConfidence;
		_filter = filter;
		_confidenceAdjuster = confidenceAdjuster;
	}

	public IEnumerable<IndicatorOccurrence> Extract(string text, string originalText, TextBoundaries boundaries, IndicatorExtractorOptions options)
	{
		foreach (Match m in _regex.Matches(text))
		{
			if (!m.Groups[_captureGroupIndex].Success)
				continue;

			var value = m.Groups[_captureGroupIndex].Value.Trim();

			if (string.IsNullOrEmpty(value))
				continue;

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
				Offset = m.Groups[_captureGroupIndex].Index,
				Length = m.Groups[_captureGroupIndex].Length,
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
	public bool ExtractPhoneNumbers { get; set; } = true; // Changed default to true for ESP reports
	public bool ExtractDeviceIdentifiers { get; set; } = true; // IMEI, etc.
	public bool GroupIpv6ByPrefix { get; set; } = true;
	public bool ReduceConfidenceForPrivateIps { get; set; } = true;

	// ─────────────────────────────────────────────
	// Limits and Timeouts
	// ─────────────────────────────────────────────

	public int MaxTextLength { get; set; } = 10_000_000;
	public int MaxMatchesPerType { get; set; } = 10_000;
	public TimeSpan ExtractionTimeout { get; set; } = TimeSpan.FromSeconds(30);

	// ─────────────────────────────────────────────
	// Configurable Keyword Sets
	// ─────────────────────────────────────────────

	public FrozenSet<string> NetworkKeywords { get; set; } = FrozenSet.ToFrozenSet(new[]
	{
		"ip", "address", "host", "server", "source", "destination",
		"c2", "command", "control", "beacon", "callback", "proxy", "login"
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
		"operator", "criminal", "hacker", "apt", "nation-state", "suspect"
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
		"d41d8cd98f00b204e9800998ecf8427e",
		"da39a3ee5e6b4b0d3255bfef95601890afd80709",
		"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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