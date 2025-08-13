#!/bin/bash

# IIM Platform Scaffolding Script
# Creates the complete project structure with stub files

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Base directories
PROJECT_ROOT="${1:-.}"
SRC_DIR="$PROJECT_ROOT/src"

echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}       IIM Platform - Project Scaffolding Script${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}\n"

# Function to create a file with boilerplate
create_file() {
    local filepath=$1
    local content=$2
    
    mkdir -p "$(dirname "$filepath")"
    
    if [ ! -f "$filepath" ]; then
        echo "$content" > "$filepath"
        echo -e "${GREEN}✓${NC} Created: $filepath"
    else
        echo -e "${YELLOW}⚠${NC} Exists: $filepath"
    fi
}

# Function to create C# class file
create_cs_class() {
    local filepath=$1
    local namespace=$2
    local classname=$3
    local type=${4:-"class"} # class, interface, record, enum
    
    local content="using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace $namespace;

public $type $classname
{
    // TODO: Implement $classname
}"
    
    create_file "$filepath" "$content"
}

# Function to create Razor component
create_razor_component() {
    local filepath=$1
    local name=$2
    
    local content="@* $name Component *@

<div class=\"$name\">
    <h3>$name</h3>
    @* TODO: Implement component *@
</div>

@code {
    [Parameter] public string Id { get; set; } = string.Empty;
    
    protected override async Task OnInitializedAsync()
    {
        // TODO: Initialize component
        await base.OnInitializedAsync();
    }
}"
    
    create_file "$filepath" "$content"
}

# Function to create service interface and implementation
create_service() {
    local dir=$1
    local name=$2
    local namespace=$3
    
    # Interface
    create_cs_class "$dir/I${name}.cs" "$namespace" "I${name}" "interface"
    
    # Implementation
    local impl_content="using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace $namespace;

public class $name : I${name}
{
    private readonly ILogger<$name> _logger;
    
    public ${name}(ILogger<$name> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}"
    
    create_file "$dir/${name}.cs" "$impl_content"
}

echo -e "${BLUE}Creating Core Library Structure...${NC}\n"

# ==============================================
# IIM.Core - Business Logic & Domain Models
# ==============================================

CORE_DIR="$SRC_DIR/IIM.Core"

# Models
echo -e "${YELLOW}Creating Core Models...${NC}"
create_cs_class "$CORE_DIR/Models/Case.cs" "IIM.Core.Models" "Case"
create_cs_class "$CORE_DIR/Models/Investigation.cs" "IIM.Core.Models" "Investigation"
create_cs_class "$CORE_DIR/Models/Evidence.cs" "IIM.Core.Models" "Evidence"
create_cs_class "$CORE_DIR/Models/InvestigationSession.cs" "IIM.Core.Models" "InvestigationSession"
create_cs_class "$CORE_DIR/Models/InvestigationQuery.cs" "IIM.Core.Models" "InvestigationQuery"
create_cs_class "$CORE_DIR/Models/InvestigationResponse.cs" "IIM.Core.Models" "InvestigationResponse"
create_cs_class "$CORE_DIR/Models/ModelConfiguration.cs" "IIM.Core.Models" "ModelConfiguration"
create_cs_class "$CORE_DIR/Models/ToolResult.cs" "IIM.Core.Models" "ToolResult"
create_cs_class "$CORE_DIR/Models/Timeline.cs" "IIM.Core.Models" "Timeline"
create_cs_class "$CORE_DIR/Models/Entity.cs" "IIM.Core.Models" "Entity"
create_cs_class "$CORE_DIR/Models/Relationship.cs" "IIM.Core.Models" "Relationship"

# Services
echo -e "${YELLOW}Creating Core Services...${NC}"
create_service "$CORE_DIR/Services" "CaseManager" "IIM.Core.Services"
create_service "$CORE_DIR/Services" "InvestigationService" "IIM.Core.Services"
create_service "$CORE_DIR/Services" "EvidenceManager" "IIM.Core.Services"
create_service "$CORE_DIR/Services" "UnifiedPlatformService" "IIM.Core.Services"

# AI/ML Services
echo -e "${YELLOW}Creating AI/ML Services...${NC}"
create_service "$CORE_DIR/AI" "ModelOrchestrator" "IIM.Core.AI"
create_service "$CORE_DIR/AI" "MultiModalPipeline" "IIM.Core.AI"
create_service "$CORE_DIR/AI" "FineTuningService" "IIM.Core.AI"
create_service "$CORE_DIR/AI" "InferencePipeline" "IIM.Core.AI"

# RAG Services
echo -e "${YELLOW}Creating RAG Services...${NC}"
create_service "$CORE_DIR/RAG" "CaseAwareRAGService" "IIM.Core.RAG"
create_service "$CORE_DIR/RAG" "EmbeddingService" "IIM.Core.RAG"
create_service "$CORE_DIR/RAG" "QdrantService" "IIM.Core.RAG"
create_service "$CORE_DIR/RAG" "RerankerService" "IIM.Core.RAG"

# Tools
echo -e "${YELLOW}Creating Investigation Tools...${NC}"
create_cs_class "$CORE_DIR/Tools/ITool.cs" "IIM.Core.Tools" "ITool" "interface"
create_cs_class "$CORE_DIR/Tools/OSINTTool.cs" "IIM.Core.Tools" "OSINTTool"
create_cs_class "$CORE_DIR/Tools/TimelineBuilderTool.cs" "IIM.Core.Tools" "TimelineBuilderTool"
create_cs_class "$CORE_DIR/Tools/NetworkAnalysisTool.cs" "IIM.Core.Tools" "NetworkAnalysisTool"
create_cs_class "$CORE_DIR/Tools/PatternRecognitionTool.cs" "IIM.Core.Tools" "PatternRecognitionTool"
create_cs_class "$CORE_DIR/Tools/ForensicsTool.cs" "IIM.Core.Tools" "ForensicsTool"
create_cs_class "$CORE_DIR/Tools/ReportGeneratorTool.cs" "IIM.Core.Tools" "ReportGeneratorTool"
create_service "$CORE_DIR/Tools" "InvestigationToolsSuite" "IIM.Core.Tools"

# Model Providers
echo -e "${YELLOW}Creating Model Providers...${NC}"
create_cs_class "$CORE_DIR/Providers/IModelProvider.cs" "IIM.Core.Providers" "IModelProvider" "interface"
create_cs_class "$CORE_DIR/Providers/OllamaProvider.cs" "IIM.Core.Providers" "OllamaProvider"
create_cs_class "$CORE_DIR/Providers/ONNXProvider.cs" "IIM.Core.Providers" "ONNXProvider"
create_cs_class "$CORE_DIR/Providers/WhisperProvider.cs" "IIM.Core.Providers" "WhisperProvider"
create_cs_class "$CORE_DIR/Providers/CLIPProvider.cs" "IIM.Core.Providers" "CLIPProvider"

echo -e "\n${BLUE}Creating Blazor App Structure...${NC}\n"

# ==============================================
# IIM.App.Hybrid - Blazor Hybrid UI
# ==============================================

APP_DIR="$SRC_DIR/IIM.App.Hybrid"

# Main Pages
echo -e "${YELLOW}Creating Main Pages...${NC}"
create_razor_component "$APP_DIR/Pages/Investigation.razor" "Investigation"
create_razor_component "$APP_DIR/Pages/Cases.razor" "Cases"
create_razor_component "$APP_DIR/Pages/Models.razor" "Models"
create_razor_component "$APP_DIR/Pages/Tools.razor" "Tools"
create_razor_component "$APP_DIR/Pages/FineTuning.razor" "FineTuning"
create_razor_component "$APP_DIR/Pages/Reports.razor" "Reports"
create_razor_component "$APP_DIR/Pages/Settings.razor" "Settings"

# Shared Components
echo -e "${YELLOW}Creating Shared Components...${NC}"
COMPONENTS_DIR="$APP_DIR/Components"

# Layout Components
create_razor_component "$COMPONENTS_DIR/Layout/AppHeader.razor" "AppHeader"
create_razor_component "$COMPONENTS_DIR/Layout/AppSidebar.razor" "AppSidebar"
create_razor_component "$COMPONENTS_DIR/Layout/StatusBar.razor" "StatusBar"
create_razor_component "$COMPONENTS_DIR/Layout/ToolRibbon.razor" "ToolRibbon"

# Investigation Components
create_razor_component "$COMPONENTS_DIR/Investigation/InvestigationWorkspace.razor" "InvestigationWorkspace"
create_razor_component "$COMPONENTS_DIR/Investigation/InvestigationMessage.razor" "InvestigationMessage"
create_razor_component "$COMPONENTS_DIR/Investigation/MultiModalInput.razor" "MultiModalInput"
create_razor_component "$COMPONENTS_DIR/Investigation/SessionList.razor" "SessionList"
create_razor_component "$COMPONENTS_DIR/Investigation/SessionItem.razor" "SessionItem"

# Chat Components
create_razor_component "$COMPONENTS_DIR/Chat/ChatContainer.razor" "ChatContainer"
create_razor_component "$COMPONENTS_DIR/Chat/MessageBubble.razor" "MessageBubble"
create_razor_component "$COMPONENTS_DIR/Chat/ToolResult.razor" "ToolResult"
create_razor_component "$COMPONENTS_DIR/Chat/EvidenceCard.razor" "EvidenceCard"
create_razor_component "$COMPONENTS_DIR/Chat/CitationViewer.razor" "CitationViewer"

# Model Components
create_razor_component "$COMPONENTS_DIR/Models/ModelCard.razor" "ModelCard"
create_razor_component "$COMPONENTS_DIR/Models/ModelLibrary.razor" "ModelLibrary"
create_razor_component "$COMPONENTS_DIR/Models/ModelDownloader.razor" "ModelDownloader"
create_razor_component "$COMPONENTS_DIR/Models/ModelStats.razor" "ModelStats"
create_razor_component "$COMPONENTS_DIR/Models/ModelConfiguration.razor" "ModelConfiguration"

# Tool Components
create_razor_component "$COMPONENTS_DIR/Tools/ToolCard.razor" "ToolCard"
create_razor_component "$COMPONENTS_DIR/Tools/ToolExecutor.razor" "ToolExecutor"
create_razor_component "$COMPONENTS_DIR/Tools/ToolResults.razor" "ToolResults"
create_razor_component "$COMPONENTS_DIR/Tools/OSINTInterface.razor" "OSINTInterface"
create_razor_component "$COMPONENTS_DIR/Tools/TimelineBuilder.razor" "TimelineBuilder"

# Evidence Components
create_razor_component "$COMPONENTS_DIR/Evidence/EvidenceExplorer.razor" "EvidenceExplorer"
create_razor_component "$COMPONENTS_DIR/Evidence/EvidenceUploader.razor" "EvidenceUploader"
create_razor_component "$COMPONENTS_DIR/Evidence/EvidenceViewer.razor" "EvidenceViewer"
create_razor_component "$COMPONENTS_DIR/Evidence/ChainOfCustody.razor" "ChainOfCustody"

# Case Components
create_razor_component "$COMPONENTS_DIR/Cases/CaseSelector.razor" "CaseSelector"
create_razor_component "$COMPONENTS_DIR/Cases/CaseCard.razor" "CaseCard"
create_razor_component "$COMPONENTS_DIR/Cases/CaseCreator.razor" "CaseCreator"
create_razor_component "$COMPONENTS_DIR/Cases/CaseOverview.razor" "CaseOverview"

# UI Services
echo -e "${YELLOW}Creating UI Services...${NC}"
SERVICES_DIR="$APP_DIR/Services"
create_service "$SERVICES_DIR" "InvestigationStateService" "IIM.App.Hybrid.Services"
create_service "$SERVICES_DIR" "ModelManagementService" "IIM.App.Hybrid.Services"
create_service "$SERVICES_DIR" "NotificationService" "IIM.App.Hybrid.Services"
create_service "$SERVICES_DIR" "ThemeService" "IIM.App.Hybrid.Services"
create_service "$SERVICES_DIR" "HubConnectionService" "IIM.App.Hybrid.Services"

# State Management
echo -e "${YELLOW}Creating State Management...${NC}"
STATE_DIR="$APP_DIR/State"
create_cs_class "$STATE_DIR/AppState.cs" "IIM.App.Hybrid.State" "AppState"
create_cs_class "$STATE_DIR/InvestigationState.cs" "IIM.App.Hybrid.State" "InvestigationState"
create_cs_class "$STATE_DIR/CaseState.cs" "IIM.App.Hybrid.State" "CaseState"
create_cs_class "$STATE_DIR/ModelState.cs" "IIM.App.Hybrid.State" "ModelState"

echo -e "\n${BLUE}Creating API Structure...${NC}\n"

# ==============================================
# IIM.Api - Web API
# ==============================================

API_DIR="$SRC_DIR/IIM.Api"

# Controllers
echo -e "${YELLOW}Creating API Controllers...${NC}"
CONTROLLERS_DIR="$API_DIR/Controllers"
create_cs_class "$CONTROLLERS_DIR/InvestigationController.cs" "IIM.Api.Controllers" "InvestigationController"
create_cs_class "$CONTROLLERS_DIR/CaseController.cs" "IIM.Api.Controllers" "CaseController"
create_cs_class "$CONTROLLERS_DIR/EvidenceController.cs" "IIM.Api.Controllers" "EvidenceController"
create_cs_class "$CONTROLLERS_DIR/ModelController.cs" "IIM.Api.Controllers" "ModelController"
create_cs_class "$CONTROLLERS_DIR/ToolController.cs" "IIM.Api.Controllers" "ToolController"
create_cs_class "$CONTROLLERS_DIR/RAGController.cs" "IIM.Api.Controllers" "RAGController"

# SignalR Hubs
echo -e "${YELLOW}Creating SignalR Hubs...${NC}"
HUBS_DIR="$API_DIR/Hubs"
create_cs_class "$HUBS_DIR/InvestigationHub.cs" "IIM.Api.Hubs" "InvestigationHub"
create_cs_class "$HUBS_DIR/CollaborationHub.cs" "IIM.Api.Hubs" "CollaborationHub"
create_cs_class "$HUBS_DIR/ModelHub.cs" "IIM.Api.Hubs" "ModelHub"

# DTOs
echo -e "${YELLOW}Creating DTOs...${NC}"
DTO_DIR="$API_DIR/DTOs"
create_cs_class "$DTO_DIR/CreateSessionRequest.cs" "IIM.Api.DTOs" "CreateSessionRequest" "record"
create_cs_class "$DTO_DIR/InvestigationQueryDto.cs" "IIM.Api.DTOs" "InvestigationQueryDto" "record"
create_cs_class "$DTO_DIR/ModelLoadRequest.cs" "IIM.Api.DTOs" "ModelLoadRequest" "record"
create_cs_class "$DTO_DIR/ToolExecutionRequest.cs" "IIM.Api.DTOs" "ToolExecutionRequest" "record"
create_cs_class "$DTO_DIR/RAGSearchRequest.cs" "IIM.Api.DTOs" "RAGSearchRequest" "record"

echo -e "\n${BLUE}Creating Configuration Files...${NC}\n"

# Configuration files
create_file "$PROJECT_ROOT/appsettings.json" '{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=iim.db"
  },
  "Qdrant": {
    "BaseUrl": "http://localhost:6333"
  },
  "Models": {
    "RepositoryUrl": "https://models.iim-training.gov",
    "LocalPath": "./models"
  },
  "WSL": {
    "DistroName": "IIM-Ubuntu",
    "ServicesPath": "/opt/iim/services"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}'

# Docker Compose
create_file "$PROJECT_ROOT/docker-compose.yml" 'version: "3.8"

services:
  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - ./data/qdrant:/qdrant/storage

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: iim
      POSTGRES_USER: iim
      POSTGRES_PASSWORD: iim_secure_password
    ports:
      - "5432:5432"
    volumes:
      - ./data/postgres:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - ./data/redis:/data'

# CSS Styles
echo -e "${YELLOW}Creating CSS files...${NC}"
create_file "$APP_DIR/wwwroot/css/app.css" '/* IIM Platform Styles */

:root {
    --primary: #6366f1;
    --primary-dark: #4f46e5;
    --secondary: #8b5cf6;
    --success: #10b981;
    --danger: #ef4444;
    --warning: #f59e0b;
    --dark: #0f172a;
    --light: #f8fafc;
    --border: #e2e8f0;
}

/* Add component styles here */'

# JavaScript
echo -e "${YELLOW}Creating JavaScript files...${NC}"
create_file "$APP_DIR/wwwroot/js/app.js" '// IIM Platform JavaScript

window.IIM = {
    // Add JavaScript functions here
    initializeApp: function() {
        console.log("IIM Platform initialized");
    },
    
    showNotification: function(message, type) {
        // TODO: Implement notification system
    },
    
    handleFileUpload: function(fileInput) {
        // TODO: Implement file upload handling
    }
};'

echo -e "\n${BLUE}Creating Test Structure...${NC}\n"

# ==============================================
# Tests
# ==============================================

TEST_DIR="$PROJECT_ROOT/tests"

# Unit Tests
echo -e "${YELLOW}Creating Unit Tests...${NC}"
create_cs_class "$TEST_DIR/IIM.Core.Tests/Services/CaseManagerTests.cs" "IIM.Core.Tests.Services" "CaseManagerTests"
create_cs_class "$TEST_DIR/IIM.Core.Tests/Services/InvestigationServiceTests.cs" "IIM.Core.Tests.Services" "InvestigationServiceTests"
create_cs_class "$TEST_DIR/IIM.Core.Tests/AI/ModelOrchestratorTests.cs" "IIM.Core.Tests.AI" "ModelOrchestratorTests"

# Integration Tests
echo -e "${YELLOW}Creating Integration Tests...${NC}"
create_cs_class "$TEST_DIR/IIM.Integration.Tests/ApiTests.cs" "IIM.Integration.Tests" "ApiTests"
create_cs_class "$TEST_DIR/IIM.Integration.Tests/HubTests.cs" "IIM.Integration.Tests" "HubTests"

echo -e "\n${BLUE}Creating Documentation...${NC}\n"

# Documentation
create_file "$PROJECT_ROOT/docs/Architecture.md" '# IIM Platform Architecture

## Overview
The IIM Platform is a comprehensive investigation platform built for law enforcement.

## Components
- **IIM.Core**: Business logic and domain models
- **IIM.App.Hybrid**: Blazor Hybrid desktop application
- **IIM.Api**: REST API and SignalR hubs

## Key Features
- Multi-modal AI integration
- Case management
- Real-time collaboration
- Custom model repository
- Investigation tools suite'

create_file "$PROJECT_ROOT/docs/DevelopmentGuide.md" '# IIM Development Guide

## Prerequisites
- .NET 8 SDK
- Docker
- WSL2 (Windows)
- Node.js (for frontend tooling)

## Getting Started
1. Clone the repository
2. Run `dotnet restore`
3. Start Docker services: `docker-compose up -d`
4. Run migrations: `dotnet ef database update`
5. Start the API: `dotnet run --project src/IIM.Api`
6. Start the app: `dotnet run --project src/IIM.App.Hybrid`'

# Create README
create_file "$PROJECT_ROOT/README.md" '# IIM Platform - Intelligent Investigation Machine

## Overview
Premium AI-powered investigation platform for law enforcement.

## Features
- 🤖 Multi-modal AI (LLM, Vision, Audio, Documents)
- 📁 Case management system
- 🔍 Advanced RAG with citations
- 🛠️ Investigation tools suite
- 🎯 Fine-tuning capabilities
- 👥 Real-time collaboration
- 🔒 Security-first design

## Quick Start
```bash
# Install dependencies
dotnet restore

# Start services
docker-compose up -d

# Run the platform
dotnet run --project src/IIM.Api &
dotnet run --project src/IIM.App.Hybrid
```

## Documentation
- [Architecture](docs/Architecture.md)
- [Development Guide](docs/DevelopmentGuide.md)
- [API Reference](docs/API.md)

## License
Proprietary - Law Enforcement Use Only'

# Create .gitignore
create_file "$PROJECT_ROOT/.gitignore" '# Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio
.vs/
*.suo
*.user
*.userosscache
*.sln.docstates
.vscode/

# Rider
.idea/
*.sln.iml

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# NuGet Packages
*.nupkg
*.snupkg
packages/
.nuget/

# Database
*.db
*.db-shm
*.db-wal

# Models (large files)
models/
*.gguf
*.onnx
*.bin

# Data directories
data/
/qdrant_storage/
/postgres_data/

# macOS
.DS_Store

# Windows
Thumbs.db
ehthumbs.db

# Node
node_modules/
npm-debug.log
yarn-error.log

# Environment files
.env
.env.local
appsettings.Development.json'

echo -e "\n${BLUE}Creating Solution Structure...${NC}\n"

# Create solution file
create_file "$PROJECT_ROOT/IIM.Platform.sln" 'Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IIM.Core", "src\IIM.Core\IIM.Core.csproj", "{1}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IIM.App.Hybrid", "src\IIM.App.Hybrid\IIM.App.Hybrid.csproj", "{2}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IIM.Api", "src\IIM.Api\IIM.Api.csproj", "{3}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
EndGlobal'

# Create project files
echo -e "${YELLOW}Creating project files...${NC}"

# IIM.Core.csproj
create_file "$CORE_DIR/IIM.Core.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
</Project>'

# IIM.App.Hybrid.csproj
create_file "$APP_DIR/IIM.App.Hybrid.csproj" '<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFrameworks>net8.0-windows10.0.19041.0;net8.0-maccatalyst;net8.0-android</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>IIM.App.Hybrid</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\IIM.Core\IIM.Core.csproj" />
  </ItemGroup>
</Project>'

# IIM.Api.csproj
create_file "$API_DIR/IIM.Api.csproj" '<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\IIM.Core\IIM.Core.csproj" />
  </ItemGroup>
</Project>'

# Create Makefile for convenience
create_file "$PROJECT_ROOT/Makefile" '.PHONY: build run test clean docker-up docker-down

build:
	dotnet build

run-api:
	dotnet run --project src/IIM.Api/IIM.Api.csproj

run-app:
	dotnet run --project src/IIM.App.Hybrid/IIM.App.Hybrid.csproj

run: docker-up
	make run-api &
	make run-app

test:
	dotnet test

clean:
	dotnet clean
	find . -name bin -type d -exec rm -rf {} + 2>/dev/null || true
	find . -name obj -type d -exec rm -rf {} + 2>/dev/null || true

docker-up:
	docker-compose up -d

docker-down:
	docker-compose down

install-tools:
	dotnet tool install --global dotnet-ef
	dotnet tool install --global dotnet-aspnet-codegenerator

migrate:
	dotnet ef database update --project src/IIM.Api/IIM.Api.csproj'

echo -e "\n${GREEN}═══════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}       Scaffolding Complete!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}\n"

echo -e "${BLUE}Project structure created successfully!${NC}\n"
echo -e "${YELLOW}Next steps:${NC}"
echo -e "  1. ${GREEN}cd $PROJECT_ROOT${NC}"
echo -e "  2. ${GREEN}dotnet restore${NC}"
echo -e "  3. ${GREEN}docker-compose up -d${NC} (start services)"
echo -e "  4. ${GREEN}dotnet build${NC}"
echo -e "  5. ${GREEN}make run${NC} (or run API and App separately)\n"

echo -e "${BLUE}Key directories created:${NC}"
echo -e "  • src/IIM.Core/         - Business logic & domain models"
echo -e "  • src/IIM.App.Hybrid/   - Blazor Hybrid UI"
echo -e "  • src/IIM.Api/          - REST API & SignalR hubs"
echo -e "  • tests/                - Unit and integration tests"
echo -e "  • docs/                 - Documentation\n"

echo -e "${GREEN}Happy coding! 🚀${NC}"
