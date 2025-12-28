// Add these to your existing models file or create a new one

namespace IIM.Shared.Models;

/// <summary>
/// Types of indicators that can be extracted from text
/// </summary>
public enum IndicatorType
{
	// Network
	IpAddress,
	Cidr,
	Domain,
	Url,
	Asn,
	MacAddress,

	// Communication
	EmailAddress,
	PhoneNumber,

	// Cryptocurrency
	CryptoAddress,
	CryptoTransaction,  // NEW: For BTC/ETH transaction IDs

	// File-related
	FileHash,
	FileName,
	FilePath,
	RegistryKey,

	// Security
	Cve,
	MitreAttack,

	// Identity
	Username,
	DateOfBirth,        // NEW: For DOB extraction

	// Darknet / Anonymity
	OnionAddress,       // NEW: .onion addresses
	IpfsCid,            // NEW: IPFS content identifiers

	// Cryptographic Identifiers
	PgpKeyId,           // NEW: PGP/GPG key IDs
	PgpFingerprint,     // NEW: PGP fingerprints

	// Device Identifiers
	Imei,               // NEW: Mobile device IMEI

	// Generic
	Custom
}

