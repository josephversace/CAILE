
IIM Blazor Hybrid + WSL2 + RAG (Upload, PDF/DOCX Parsing, Sentence Chunking)

Includes:
- IIM.App.Hybrid (MAUI Blazor desktop) with RAG page:
  - Upload .txt, .md, .pdf, .docx
  - Toggle: sentence-based chunking or fixed-size chunking
- IIM.Api daemon with endpoints:
  - /healthz, /v1/gpu, /v1/run, /v1/generate, /v1/stop-all
  - /rag/index (raw text) [query: ?mode=fixed|sentences]
  - /rag/upload (multipart) [query: ?mode=fixed|sentences]
  - /agents/rag/query
- WSL2 scripts:
  - scripts/setup_wsl.sh (installs Python deps + Docker)
  - scripts/start_qdrant.sh (runs Qdrant)
  - scripts/embed_service.py (FastAPI + sentence-transformers)

Quick Start
===========
WSL2 terminal:
  bash scripts/setup_wsl.sh
  bash scripts/start_qdrant.sh
  python3 scripts/embed_service.py

Windows terminal 1 (daemon):
  dotnet run --project src/IIM.Api/IIM.Api.csproj

Windows terminal 2 (desktop app):
  dotnet build -t:Run -f net8.0-windows10.0.19041.0 -p:WindowsPackageType=None src/IIM.App.Hybrid/IIM.App.Hybrid.csproj

In the app:
  - Go to RAG tab
  - Check "Sentence chunking" for better semantic splits
  - Upload .pdf/.docx/.txt/.md -> Index
  - Ask questions; answer shows grounded context and citations

Notes:
- PDF parsing uses PdfPig; DOCX uses OpenXML SDK.
- Replace mock generation with real model backends later.
