.PHONY: build run test clean docker-up docker-down

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
	dotnet ef database update --project src/IIM.Api/IIM.Api.csproj
