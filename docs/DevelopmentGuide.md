# IIM Development Guide

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
6. Start the app: `dotnet run --project src/IIM.App.Hybrid`
