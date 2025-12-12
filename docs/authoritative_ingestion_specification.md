# CAILE Authoritative Ingestion Specification

This document defines the **single authoritative ingestion lifecycle** for CAILE.  
It applies to **all file entry points** (chat attachments, file uploads, future remote clients) and is binding for humans and AI agents.

---

## 1. Purpose

The ingestion pipeline exists to:
- Deduplicate files by **content identity (BLAKE3)**
- Preserve **chain-of-custody and evidence integrity**
- Support **long-running enrichment** (Docling, GraphRAG, embeddings, media)
- Cleanly separate **upload**, **storage**, and **analysis** concerns

There must be **exactly one ingestion flow**.

---

## 2. Core Domain Model (Invariants)

### 2.1 StoredFile (Canonical Content)

- Primary Key: `Blake3Hash`
- Represents **unique file content** across the system
- Created **once per unique hash**
- Owns:
  - Physical storage location (SeaweedFS)
  - Quarantine state
  - Classification metadata
  - GraphRAG indexing state
  - Derived artifacts (`ProcessedFile`)

StoredFile **must never** be duplicated.

---

### 2.2 VirtualFile (Workspace Projection)

- Scoped to a Workspace
- Points to a StoredFile via `StoredFileHash`
- Owns:
  - Filename / path
  - Tags
  - Status
  - Chain-of-custody
  - Reprocessing intent

Multiple VirtualFiles may reference the same StoredFile.

---

### 2.3 ProcessedFile (Derived Outputs)

Represents derived artifacts such as:
- OCR text
- Thumbnails
- Transcodes
- EXIF metadata
- Video keyframes

ProcessedFiles are **outputs**, never inputs.

---

## 3. Storage Layer

### 3.1 SeaweedFS

- Treated as a **dumb object store**
- No domain logic
- All paths are determined upstream

### 3.2 Canonical Storage Path

New files are written to quarantine:

```
quarantine/{index}/{blake3}/{originalFileName}
```

Moves between buckets (e.g. quarantine → classified) are handled by `EfWorkspaceManager.MoveStoredFileAsync`.

---

## 4. Ingestion Lifecycle (Authoritative)

### Phase 0 — Endpoint (Upload Boundary)

Endpoints are **thin and fast**.

Responsibilities:
- Accept file stream (`IFormFile`)
- Compute server-side BLAKE3 while streaming
- Optionally verify client-provided hash
- Invoke mediator command

Endpoints MUST NOT:
- Touch EF / DbContexts
- Decide deduplication
- Write StoredFile records
- Call Docling, GraphRAG, or Qdrant

---

### Phase 1 — Mediator Command (Authority)

Command (example):

```
RegisterUploadedFileCommand : IRequest<Guid>
```

Responsibilities:
1. Lookup StoredFile by BLAKE3
2. Decide deduplication
3. Write to SeaweedFS (if new)
4. Create StoredFile (if new)
5. Create VirtualFile
6. Enqueue ingestion (if required)

This is the **only place** where deduplication is decided.

---

### Phase 2 — Background Scheduling

- Ingestion is always asynchronous
- Long-running tasks MUST NOT block uploads
- Jobs may be queued via worker / background service

---

### Phase 3 — IngestionPipeline (Enrichment Only)

Input:
```
IngestAsync(VirtualFileId)
```

Responsibilities:
1. Load VirtualFile + StoredFile
2. Read content from SeaweedFS
3. Run Docling parsing
4. Run GraphRAG → Neo4j
5. Generate embeddings → Qdrant
6. Persist:
   - ProcessedFiles
   - StoredFile indexing metadata
   - VirtualFile status

IngestionPipeline MUST NOT:
- Hash files
- Write original files
- Create StoredFiles
- Create VirtualFiles

---

## 5. External Systems

### 5.1 Docling

- Structural document parsing
- Produces Markdown, pages, images
- Feeds both GraphRAG and embeddings

---

### 5.2 GraphRAG (Neo4j)

- Extracts entities and relationships
- Graph is **content-centric**
- Keyed by StoredFile and chunk IDs
- Reprocessing updates graph state

---

### 5.3 Embeddings (Qdrant)

- Chunk-level semantic vectors
- Scoped by Workspace / Case
- Keyed by:
  - WorkspaceId
  - VirtualFileId
  - ChunkId

Embeddings may be regenerated without modifying StoredFile.

---

## 6. Reprocessing Semantics

- Reprocessing is requested at the **VirtualFile** level
- Reprocessing may:
  - Regenerate embeddings
  - Re-run GraphRAG
  - Rebuild derived artifacts

Reprocessing MUST NOT:
- Change StoredFile identity
- Rewrite original content

---

## 7. Client-Side Hashing (Optimization)

- Clients MAY compute BLAKE3
- Server MUST recompute and verify
- Hash mismatch → reject or flag

Client hashing is an optimization, not a trust anchor.

---

## 8. Rules for Agents (MANDATORY)

AI agents working in this repository MUST:

- Treat this document as authoritative
- Never introduce alternative ingestion flows
- Never bypass the mediator for file ingestion
- Never duplicate StoredFile creation logic
- Never move ingestion logic into endpoints

Violations are considered architectural defects.

---

## 9. Summary

- StoredFile = content identity
- VirtualFile = workspace projection
- Mediator = authority
- IngestionPipeline = enrichment only
- Neo4j = semantic graph
- Qdrant = semantic search

**There is exactly one ingestion pipeline.**

