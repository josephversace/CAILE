# Ingestion.Services

This folder contains services used by the **ingestion pipeline**, including
text extraction, metadata enrichment, and offline external tooling.

This README exists to capture **local intent and guardrails** for future
changes. It is intentionally narrow in scope.

---

## External Tools (ExifTool)

ExifTool is integrated as an **offline Tool**, not a service.

Key characteristics:

- Installed by the **installer**, not downloaded at runtime
- OS-specific binaries are packaged with the installer
- Runtime layout is normalized to:

~/.iim/tools/exiftool/
├─ exiftool(.exe)
├─ exiftool_files/ (Windows)
└─ VERSION

- The ingestion pipeline does **not** know:
  - where ExifTool lives
  - how it is executed
  - what version is installed

All execution, validation, and normalization logic lives in
`IExifToolService`.

---

## Configuration Ownership

- Tool paths are written at install time into `appsettings.json`
- The API consumes tool paths via `CaileConfig`
- Tool paths are resolved once at service construction
- Tool paths are **not passed per ingestion call**

If a value does not vary per ingestion request, it does not belong in the
pipeline method signature.

---

## Failure Behavior

- ExifTool execution is **best-effort**
- If metadata extraction fails or a file type is unsupported:
  - ingestion continues
  - no exception is thrown
- Tool availability is validated at API startup, not lazily

This prevents silent ingestion failures and keeps enrichment non-blocking.

---

## What Does NOT Belong Here

Do **not** add:

- Network-enabled tools
- Headless browsers
- Web scraping or crawling
- Stateful or long-running processes

These belong in a separate MCP/service layer, not in ingestion tooling.

---

## Deferred Improvements

The following are intentionally deferred:

- Tool health diagnostics endpoints
- Tool hash / integrity verification
- Additional offline tools (e.g. FFmpeg, poppler)
- Agent-driven tool invocation

New tools should follow the same pattern as ExifTool.

---

## Design Goal

The ingestion pipeline must remain:

- deterministic
- offline-safe
- auditable
- predictable

If a change here feels “clever”, stop and reconsider.

---