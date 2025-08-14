# Run this PowerShell script to add required NuGet packages

Write-Host "Installing required NuGet packages..." -ForegroundColor Green

# For IIM.Core
Set-Location IIM.Core
dotnet add package Microsoft.Extensions.Logging.Abstractions

# For IIM.Desktop  
Set-Location ../IIM.Desktop
dotnet add package Microsoft.AspNetCore.Components.Web

# Return to root
Set-Location ..

Write-Host "Packages installed successfully!" -ForegroundColor Green
