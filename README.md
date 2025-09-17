# CAILE

CAILE Platform - Comprehensive Architectural Review & Onboarding Report

Executive Summary

CAILE (Classified Analytics & Intelligent Logistics Engine) is a secure data router engineered to move classified and other sensitive data between mission enclaves while preserving provenance and control. Built to serve multi-domain operations—from law enforcement casework to defense intelligence fusion centers—it applies AI-driven insights to accelerate investigations without compromising segregation mandates.

Key Capabilities:

- Tier-aware data classification pipelines that tag, label, and prioritize assets as they enter the platform.
- Policy-driven routing services that enforce handling rules, releasability constraints, and cross-domain approval workflows.
- Segregated processing zones with strict boundary controls for evidence, intelligence, and public-safety data.
- Cross-domain analytics that combine structured and unstructured sources with AI-assisted summarization, anomaly detection, and decision support.

1\. Project Overview

Business Purpose

A secure, auditable data movement and analytics platform enabling law enforcement, intelligence, and public-safety teams to:

- Orchestrate intake pipelines that classify evidence, case files, and intelligence by tier the moment they enter the system.
- Enforce routing policies that keep sensitive records within the appropriate enclaves while expediting releasable data to partner domains.
- Maintain segregation controls that isolate investigative workspaces, protect chain-of-custody, and assure compliance audits.
- Deliver AI-driven cross-domain insights that surface correlations, risks, and investigative leads faster than manual review.

Primary Users

- Law enforcement investigators
- Digital forensics specialists
- Intelligence analysts
- Interagency fusion center staff

Core Use Cases

- Evidence ingestion with automated classification, tagging, and provenance capture.
- Policy-aware data routing between on-prem, cloud, and coalition environments.
- Segregated enclave analytics with AI summarization, translation, and prioritization.
- Audit trail, compliance reporting, and cross-domain dissemination tracking.
- Multi-modal investigations spanning text, audio, imagery, and structured datasets.

Operational Goals

- Privacy: All processing happens locally with tier-specific controls, no uncontrolled cloud dependencies.
- Compliance: Continuous monitoring of routing decisions, segregation boundaries, and audit trail completeness.
- Performance: Optimized for Framework Desktop with 128GB RAM and scalable enclave workloads.
- Flexibility: Support for multiple AI model formats, policy engines, and data-handling standards.

2\. Solution \& Repository Structure

Solutions Overview

IIM/

├── src/

│   ├── IIM.Desktop/          # Windows Forms + Blazor Hybrid host

│   ├── IIM.Components/       # Blazor UI components library  

│   ├── IIM.Api/             # Web API backend (Minimal APIs)

│   ├── IIM.Core/            # Core domain logic and interfaces

│   ├── IIM.Application/     # Application services and orchestration

│   ├── IIM.Infrastructure/  # External services implementation

│   ├── IIM.Shared/          # Shared models and contracts

│   └── IIM.Plugin.SDK/      # Plugin development SDK

├── tests/

│   ├── IIM.Core.Tests/      # Unit tests

│   ├── IIM.Api.Tests/       # API integration tests

│   └── IIM.Integration.Tests/ # End-to-end tests

└── docs/                     # Documentation and training materials

K

ey Projects Purpose

Project



Purpose

Key Dependencies



IIM.DesktopWindows desktop host application.NET MAUI Blazor, WebView2

IIM.ComponentsReusable Blazor componentsBlazorise, Bootstrap 5

IIM.ApiREST API backendASP.NET Core Minimal APIs

IIM.CoreDomain models, interfaces, mediator, Semantic Kernel, ONNX Runtime

IIM.ApplicationBusiness logic, AI orchestration, LlamaSharp, Mediator commands and handlers

IIM.Infrastructure WSL2, Docker, storage, AI runtimeDirectML, MinIO, SQLite

IIM.Shared Cross-cutting concernsEnums, Modelss, interfaces



3\. High-Level Architecture

Architecture Diagram

mermaidgraph TB

&nbsp;   subgraph "Client Layer"

&nbsp;       UI\[Blazor Hybrid Desktop<br/>Windows Forms + WebView2]

&nbsp;       PWA\[Progressive Web App<br/>Optional]

&nbsp;   end

&nbsp;   

&nbsp;   subgraph "API Layer" 

&nbsp;       API\[ASP.NET Core API<br/>Minimal APIs]

&nbsp;       Hub\[SignalR Hub<br/>Real-time Updates]

&nbsp;   end

&nbsp;   

&nbsp;   subgraph "Application Layer"

&nbsp;       MED\[Mediator<br/>CQRS Pattern]

&nbsp;       SK\[Semantic Kernel<br/>AI Orchestration]

&nbsp;       IS\[Investigation Service]

&nbsp;   end

&nbsp;   

&nbsp;   subgraph "Infrastructure Layer"

&nbsp;       subgraph "AI Runtimes"

&nbsp;           ONNX\[ONNX Runtime<br/>DirectML/CUDA/CPU]

&nbsp;           LLAMA\[LlamaSharp<br/>GGUF/GGML]

&nbsp;       end

&nbsp;       

&nbsp;       subgraph "Storage"

&nbsp;           SQLITE\[SQLite<br/>Metadata]

&nbsp;           MINIO\[MinIO<br/>Evidence Files]

&nbsp;           QDRANT\[Qdrant<br/>Vector Store]

&nbsp;       end

&nbsp;       

&nbsp;       subgraph "Platform"

&nbsp;           WSL\[WSL2 Manager]

&nbsp;           DOCKER\[Docker Services]

&nbsp;       end

&nbsp;   end

&nbsp;   

&nbsp;   UI --> API

&nbsp;   PWA --> API

&nbsp;   API --> MED

&nbsp;   API --> Hub

&nbsp;   MED --> SK

&nbsp;   MED --> IS

&nbsp;   SK --> ONNX

&nbsp;   SK --> LLAMA

&nbsp;   IS --> SQLITE

&nbsp;   IS --> MINIO

&nbsp;   SK --> QDRANT

&nbsp;   DOCKER --> QDRANT

&nbsp;   WSL --> DOCKER

Deployment Architecture

mermaidgraph LR

&nbsp;   subgraph "Framework Desktop"

&nbsp;       subgraph "Windows Host"

&nbsp;           APP\[IIM Desktop App]

&nbsp;           API\_LOCAL\[IIM API Service]

&nbsp;       end

&nbsp;       

&nbsp;       subgraph "WSL2 Ubuntu"

&nbsp;           subgraph "Docker Containers"

&nbsp;               QD\[Qdrant:6333]

&nbsp;               PG\[PostgreSQL:5432]

&nbsp;               MIO\[MinIO:9000]

&nbsp;               EMB\[Embedding Service:8081]

&nbsp;           end

&nbsp;       end

&nbsp;   end

&nbsp;   

&nbsp;   APP --> API\_LOCAL

&nbsp;   API\_LOCAL --> QD

&nbsp;   API\_LOCAL --> PG

&nbsp;   API\_LOCAL --> MIO

&nbsp;   API\_LOCAL --> EMB



4\. Dataflow \& AI Orchestration

Investigation Workflow

mermaidsequenceDiagram

&nbsp;   participant User

&nbsp;   participant UI as Blazor UI

&nbsp;   participant API

&nbsp;   participant Med as Mediator

&nbsp;   participant SK as Semantic Kernel

&nbsp;   participant Model as AI Model

&nbsp;   participant Store as Storage

&nbsp;   

&nbsp;   User->>UI: Upload evidence

&nbsp;   UI->>API: POST /evidence

&nbsp;   API->>Med: UploadEvidenceCommand

&nbsp;   Med->>Store: Store file + hash

&nbsp;   Med->>SK: Extract \& embed

&nbsp;   SK->>Model: Process content

&nbsp;   Model-->>SK: Embeddings

&nbsp;   SK->>Store: Save to Qdrant

&nbsp;   Store-->>Med: Evidence ID

&nbsp;   Med-->>API: Result

&nbsp;   API-->>UI: Success response

&nbsp;   

&nbsp;   User->>UI: Query evidence

&nbsp;   UI->>API: POST /investigation/query

&nbsp;   API->>Med: ProcessQueryCommand

&nbsp;   Med->>SK: Reasoning pipeline

&nbsp;   SK->>Store: Vector search

&nbsp;   Store-->>SK: Top-K results

&nbsp;   SK->>Model: Generate response

&nbsp;   Model-->>SK: Answer + citations

&nbsp;   SK-->>Med: Investigation result

&nbsp;   Med-->>API: Response

&nbsp;   API-->>UI: Display results

AI Model Loading \& Inference

mermaidflowchart TD

&nbsp;   A\[Model Load Request] --> B{Model Format?}

&nbsp;   B -->|ONNX| C\[ONNX Runtime Manager]

&nbsp;   B -->|GGUF| D\[LlamaSharp Manager]

&nbsp;   

&nbsp;   C --> E{Execution Provider?}

&nbsp;   E -->|GPU Available| F\[DirectML/CUDA]

&nbsp;   E -->|CPU Only| G\[CPU Provider]

&nbsp;   

&nbsp;   D --> H\[Load Weights]

&nbsp;   H --> I\[Create Context]

&nbsp;   

&nbsp;   F --> J\[Create Session]

&nbsp;   G --> J

&nbsp;   I --> K\[Ready for Inference]

&nbsp;   J --> K

&nbsp;   

&nbsp;   K --> L\[Process Request]

&nbsp;   L --> M\[Preprocess Input]

&nbsp;   M --> N\[Run Inference]

&nbsp;   N --> O\[Postprocess Output]

&nbsp;   O --> P\[Return Result]



5\. Core Patterns and Frameworks

Mediator Pattern (Custom CQRS)

csharp// Command/Query separation with pipeline behaviors

public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse> 

&nbsp;   where TRequest : IRequest<TResponse>

{

&nbsp;   Task<TResponse> Handle(TRequest request, CancellationToken ct);

}



// Pipeline for cross-cutting concerns

public interface IPipelineBehavior<TRequest, TResponse>

{

&nbsp;   Task<TResponse> Handle(TRequest request, 

&nbsp;       RequestHandlerDelegate<TResponse> next, CancellationToken ct);

}

Usage: Centralized request handling with audit logging, validation, and error handling through pipeline behaviors.

Builder Pattern for AI Pipelines

csharp// Fluent API for constructing investigation workflows

var pipeline = new InvestigationPipelineBuilder()

&nbsp;   .WithModel("whisper-base")

&nbsp;   .AddTranscription()

&nbsp;   .AddTranslation("en")

&nbsp;   .WithModel("all-minilm")

&nbsp;   .AddEmbedding()

&nbsp;   .AddVectorSearch(topK: 10)

&nbsp;   .WithModel("phi-3")

&nbsp;   .AddReasoning()

&nbsp;   .Build();

Semantic Kernel Integration



Plugins: ForensicAnalysis, DataExtraction, ReportGeneration

Functions: Native and semantic functions for investigation tasks

Memory: Integration with Qdrant for semantic search

Planners: Sequential and stepwise planners for complex workflows



LlamaSharp for GGUF Models



Context management with caching

Streaming inference support

Chat session management

Prompt template handling



6\. Auditing, Security \& Compliance

Security Architecture

mermaidgraph TD

&nbsp;   subgraph "Security Layers"

&nbsp;       A\[Input Validation] --> B\[Authentication]

&nbsp;       B --> C\[Authorization]

&nbsp;       C --> D\[Audit Logging]

&nbsp;       D --> E\[Evidence Hashing]

&nbsp;       E --> F\[Encryption at Rest]

&nbsp;   end

&nbsp;   

&nbsp;   subgraph "Compliance Features"

&nbsp;       G\[Chain of Custody]

&nbsp;       H\[Append-only Logs]

&nbsp;       I\[SHA-256 Hashing]

&nbsp;       J\[Export Manifests]

&nbsp;       K\[Offline Operation]

&nbsp;   end

&nbsp;   

&nbsp;   subgraph "Threat Mitigation"

&nbsp;       L\[Prompt Injection Defense]

&nbsp;       M\[Data Exfiltration Prevention]

&nbsp;       N\[Model Bias Detection]

&nbsp;       O\[Access Control]

&nbsp;   end

Audit Implementation



Every action logged: User, timestamp, operation, result

Immutable storage: Append-only audit logs in SQLite

Evidence integrity: SHA-256 hash on upload, verification on access

Export capability: Full audit trail exportable for legal proceedings



7\. Deployment \& Infrastructure

Local Development Setup

bash# Prerequisites

\- Windows 11 Pro/Enterprise with WSL2

\- .NET 8 SDK

\- Docker Desktop or Docker in WSL2

\- 64GB+ RAM (128GB for production)



\# Setup Steps

1\. Enable WSL2: wsl --install

2\. Clone repository

3\. Run setup script: ./scripts/setup-dev.ps1

4\. Start services: docker-compose up -d

5\. Run migrations: dotnet ef database update

6\. Launch app: dotnet run --project src/IIM.Desktop

Docker Services Configuration

yamlservices:

&nbsp; qdrant:

&nbsp;   image: qdrant/qdrant:latest

&nbsp;   ports: \[6333, 6334]

&nbsp;   volumes: ./data/qdrant:/qdrant/storage

&nbsp;   

&nbsp; postgres:

&nbsp;   image: postgres:15-alpine

&nbsp;   ports: \[5432]

&nbsp;   environment:

&nbsp;     POSTGRES\_DB: iim

&nbsp;     

&nbsp; minio:

&nbsp;   image: minio/minio:latest

&nbsp;   ports: \[9000, 9001]

&nbsp;   command: server /data --console-address :9001

&nbsp;   

&nbsp; embedding:

&nbsp;   build: ./services/embedding

&nbsp;   ports: \[8081]

&nbsp;   environment:

&nbsp;     MODEL\_PATH: /models

8\. Data Models \& Storage

Core Entities

mermaiderDiagram

&nbsp;   InvestigationSession ||--o{ InvestigationMessage : contains

&nbsp;   InvestigationSession ||--o{ Evidence : references

&nbsp;   Evidence ||--o{ EvidenceChunk : "chunked into"

&nbsp;   EvidenceChunk ||--|| VectorEmbedding : has

&nbsp;   InvestigationMessage ||--o{ ToolResult : produces

&nbsp;   ModelConfiguration ||--o{ ModelParameterSet : has

&nbsp;   AuditLog ||--o{ InvestigationSession : tracks

&nbsp;   

&nbsp;   InvestigationSession {

&nbsp;       string Id PK

&nbsp;       string CaseId

&nbsp;       string Title

&nbsp;       DateTime CreatedAt

&nbsp;       SessionStatus Status

&nbsp;   }

&nbsp;   

&nbsp;   Evidence {

&nbsp;       string Id PK

&nbsp;       string SessionId FK

&nbsp;       string FileName

&nbsp;       string Hash

&nbsp;       long Size

&nbsp;       string MimeType

&nbsp;   }

&nbsp;   

&nbsp;   ModelConfiguration {

&nbsp;       string ModelId PK

&nbsp;       string Provider

&nbsp;       ModelType Type

&nbsp;       string ModelPath

&nbsp;       bool RequiresGpu

&nbsp;   }

Storage Strategy



SQLite: Metadata, configurations, audit logs

MinIO: Binary evidence files, model files

Qdrant: Vector embeddings for semantic search

Local Filesystem: Temporary processing, cache



9\. Key Workflows

Evidence Analysis Workflow



Upload: File uploaded through Blazor UI

Validation: File type, size, malware scan

Storage: Save to MinIO with deduplication

Hashing: Generate SHA-256 hash

Processing: Extract text/metadata based on type

Chunking: Split into semantic chunks

Embedding: Generate vector embeddings

Indexing: Store in Qdrant

Audit: Log all operations



Multi-Modal Investigation



Audio: Whisper transcription → text extraction

Images: CLIP embedding → visual similarity search

Documents: OCR → text extraction → RAG indexing

Integration: Semantic Kernel orchestrates pipeline

Results: Unified investigation timeline



10\. Quality, Testing \& Observability

Testing Strategy

tests/

├── Unit Tests (xUnit + Moq)

│   ├── Service logic

│   ├── Domain models  

│   └── Utilities

├── Integration Tests

│   ├── API endpoints

│   ├── Database operations

│   └── WSL/Docker services

└── E2E Tests (Playwright)

&nbsp;   ├── User workflows

&nbsp;   └── Investigation scenarios

Code Quality Issues Found



Incomplete Implementations: Multiple TODO markers

Duplicate Services: Evidence management duplicated

Missing Error Handling: Some async operations lack try-catch

Documentation Gaps: Many methods lack XML documentation

Inconsistent Naming: Mix of conventions



11\. SWOT Analysis

Strengths



Modern Architecture: Clean separation, SOLID principles

Comprehensive AI Support: ONNX, GGUF, multiple providers

Security-First Design: Local processing, audit trails

Enterprise Features: Scalable, pluggable, observable

Training Integration: Built for CAI-LE certification course



Weaknesses



Code Duplication: ~15% duplicate code detected

Incomplete Features: Several services partially implemented

Complex Setup: WSL2 + Docker + multiple services

Documentation: Insufficient inline and API documentation

Test Coverage: Estimated <40% coverage



Opportunities



GPU Optimization: Better ROCm/CUDA utilization

Model Zoo: Expand pre-configured models

Cloud Extension: Optional cloud backup/sync

Plugin Marketplace: Community contributions

Advanced Analytics: Investigation pattern recognition



Threats



Technical Debt: Accumulating TODOs and stubs

Dependency Risks: LlamaSharp, DirectML compatibility

Complexity Growth: Feature creep without refactoring

Performance: Memory usage with large models

Bus Factor: Knowledge concentration



12\. Recommendations

Priority 1: Code Cleanup (Week 1-2)



Remove Duplicates: Consolidate evidence services

Complete TODOs: Implement stubbed methods

Fix Naming: Standardize conventions

Add Documentation: XML docs for all public APIs



Priority 2: Architecture Improvements (Week 3-4)



Simplify Mediator: Reduce pipeline complexity

Unify Storage: Single source of truth for configurations

Abstract AI Providers: Common interface for ONNX/GGUF

Error Handling: Comprehensive exception strategy



Priority 3: Testing \& Quality (Week 5-6)



Unit Tests: Achieve 80% coverage

Integration Tests: WSL/Docker automation

Performance Tests: Model loading/inference benchmarks

Security Audit: Penetration testing



Quick Wins



Add XML documentation generation

Implement health check endpoints

Create developer setup script

Add logging correlation IDs

Implement graceful shutdown



Technical Debt Priorities



Complete InferencePipeline implementation

Finish WslServiceOrchestrator health checks

Implement missing repository methods

Add retry/circuit breaker patterns

Complete SignalR notification system



13\. Project Plan

Phase 1: Stabilization (Weeks 1-2)



&nbsp;Code cleanup and deduplication

&nbsp;Complete all TODO implementations

&nbsp;Add comprehensive error handling

&nbsp;Document all public APIs



Phase 2: Testing (Weeks 3-4)



&nbsp;Write missing unit tests

&nbsp;Add integration test suite

&nbsp;Performance benchmarking

&nbsp;Security testing



Phase 3: Optimization (Weeks 5-6)



&nbsp;Refactor duplicate services

&nbsp;Optimize model loading

&nbsp;Improve memory management

&nbsp;Add caching strategies



Phase 4: Features (Weeks 7-8)



&nbsp;Complete plugin system

&nbsp;Add model marketplace

&nbsp;Implement advanced analytics

&nbsp;Cloud sync capability



14\. Onboarding Checklist

For New Developers



Environment Setup



&nbsp;Install prerequisites

&nbsp;Clone repository

&nbsp;Run setup scripts

&nbsp;Verify WSL2/Docker





Code Familiarization



&nbsp;Read architecture docs

&nbsp;Review core patterns

&nbsp;Understand data flow

&nbsp;Study key workflows





First Tasks



&nbsp;Fix one TODO

&nbsp;Add one unit test

&nbsp;Document one service

&nbsp;Review one PR







Key Files to Review First



src/IIM.Core/Mediator/Mediator.cs - Command pattern

src/IIM.Application/AI/SemanticKernelOrchestrator.cs - AI orchestration

src/IIM.Api/Program.cs - Service configuration

src/IIM.Desktop/Program.cs - Desktop host

src/IIM.Infrastructure/Platform/WslManager.cs - WSL integration



Appendices

A. Technology Glossary



ONNX: Open Neural Network Exchange format

GGUF: GPT-Generated Unified Format (LlamaSharp)

DirectML: DirectX Machine Learning

ROCm: AMD GPU compute platform

Semantic Kernel: Microsoft's AI orchestration SDK

Qdrant: Vector similarity search engine

MinIO: S3-compatible object storage



B. Architecture Decision Records



Blazor Hybrid: Native performance with web UI flexibility

WSL2: Linux containers on Windows for AI services

Local-First: Privacy and offline capability requirements

Mediator Pattern: Decoupled, testable command handling

SQLite: Simple, embedded database for metadata



C. Development Standards



Naming: PascalCase for public, camelCase for private

Async: All I/O operations must be async

Logging: Structured logging with correlation IDs

Testing: Minimum 80% coverage target

Documentation: XML docs for all public members

