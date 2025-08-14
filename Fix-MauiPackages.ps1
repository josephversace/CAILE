# ============================================================================
# Fix-MauiPackages.ps1
# Resolves package version conflicts in IIM.App.Hybrid
# ============================================================================

Write-Host "Fixing MAUI Package Version Conflicts" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Navigate to the hybrid app project
Set-Location "src\IIM.App.Hybrid"

Write-Host "`nCurrent directory: $(Get-Location)" -ForegroundColor Cyan

# ============================================================================
# Step 1: Fix the package versions
# ============================================================================
Write-Host "`n[1] Updating package references to resolve conflicts..." -ForegroundColor Yellow

# Remove conflicting packages first
Write-Host "Removing conflicting packages..." -ForegroundColor Cyan
dotnet remove package Microsoft.AspNetCore.Components.Forms 2>$null
dotnet remove package CommunityToolkit.Maui 2>$null

# Add back with correct versions
Write-Host "`nAdding packages with aligned versions..." -ForegroundColor Cyan

# Add MAUI Controls explicitly (fixes MA002 warning)
dotnet add package Microsoft.Maui.Controls --version 8.0.100

# Add the latest compatible AspNetCore Components
dotnet add package Microsoft.AspNetCore.Components.Forms --version 8.0.10

# Add CommunityToolkit with correct version
dotnet add package CommunityToolkit.Maui --version 9.0.0

# ============================================================================
# Step 2: Alternative - Update all packages to latest
# ============================================================================
Write-Host "`n[2] Updating all packages to latest compatible versions..." -ForegroundColor Yellow

# Update all packages
Write-Host "This might take a minute..." -ForegroundColor Gray
dotnet add package Microsoft.Maui.Controls
dotnet add package Microsoft.AspNetCore.Components.WebView.Maui
dotnet add package Microsoft.Extensions.Logging.Debug

# ============================================================================
# Step 3: Clear NuGet cache if needed
# ============================================================================
Write-Host "`n[3] Clearing NuGet cache to ensure clean package restore..." -ForegroundColor Yellow
dotnet nuget locals all --clear

# ============================================================================
# Step 4: Restore and build
# ============================================================================
Write-Host "`n[4] Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`n[5] Building project..." -ForegroundColor Yellow
dotnet build

# Check if build succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Build successful!" -ForegroundColor Green
    
    # Return to root
    Set-Location ..\..
    
    Write-Host "`nRunning the application..." -ForegroundColor Cyan
    dotnet run --project src\IIM.App.Hybrid
} else {
    Write-Host "`n❌ Build still has errors" -ForegroundColor Red
    
    # Try a more aggressive fix
    Write-Host "`nTrying alternative fix..." -ForegroundColor Yellow
    
    # Return to root first
    Set-Location ..\..
    
    # Edit the project file directly
    $projFile = "src\IIM.App.Hybrid\IIM.App.Hybrid.csproj"
    $projContent = Get-Content $projFile -Raw
    
    Write-Host "`nProject file location: $projFile" -ForegroundColor Cyan
    
    # Create a fixed version
    Write-Host "`nCreating fixed project file..." -ForegroundColor Yellow
    
    # This is a template for what should work
    $fixedProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <WindowsPackageType>None</WindowsPackageType>
    <ApplicationTitle>IIM Investigation Suite</ApplicationTitle>
    <ApplicationId>com.iim.investigation</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <!-- Skip the MAUI warning -->
    <SkipValidateMauiImplicitPackageReferences>true</SkipValidateMauiImplicitPackageReferences>
    <!-- Treat warnings as warnings, not errors -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningsAsErrors></WarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- MAUI Packages with aligned versions -->
    <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.100" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.100" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.1" />
    <PackageReference Include="CommunityToolkit.Maui" Version="9.0.0" />
    <!-- Fix the Forms version explicitly -->
    <PackageReference Include="Microsoft.AspNetCore.Components.Forms" Version="8.0.10" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\IIM.Core\IIM.Core.csproj" />
    <ProjectReference Include="..\IIM.Shared\IIM.Shared.csproj" />
  </ItemGroup>
</Project>
'@

    Write-Host "`nWould you like to:" -ForegroundColor Yellow
    Write-Host "1. Save a backup and apply the fix" -ForegroundColor White
    Write-Host "2. Just try to build with warnings allowed" -ForegroundColor White
    Write-Host "3. Skip and try manual fixes" -ForegroundColor White
    Write-Host "Enter choice (1/2/3): " -ForegroundColor Cyan -NoNewline
    $choice = Read-Host
    
    switch ($choice) {
        "1" {
            # Backup and fix
            Copy-Item $projFile "$projFile.backup"
            Write-Host "Backup saved to $projFile.backup" -ForegroundColor Gray
            $fixedProject | Out-File $projFile -Encoding UTF8
            Write-Host "Applied fixed project file" -ForegroundColor Green
            
            # Try building again
            dotnet restore src\IIM.App.Hybrid
            dotnet build src\IIM.App.Hybrid
        }
        "2" {
            # Build allowing warnings
            Write-Host "`nBuilding with warnings allowed..." -ForegroundColor Cyan
            dotnet build src\IIM.App.Hybrid /p:TreatWarningsAsErrors=false
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "`n✅ Build successful (with warnings)" -ForegroundColor Green
                dotnet run --project src\IIM.App.Hybrid
            }
        }
        "3" {
            Write-Host "`nManual fix instructions:" -ForegroundColor Yellow
            Write-Host "1. Open src\IIM.App.Hybrid\IIM.App.Hybrid.csproj" -ForegroundColor White
            Write-Host "2. Add: <TreatWarningsAsErrors>false</TreatWarningsAsErrors>" -ForegroundColor White
            Write-Host "3. Update all package versions to match" -ForegroundColor White
            Write-Host "4. Run: dotnet restore" -ForegroundColor White
            Write-Host "5. Run: dotnet build" -ForegroundColor White
        }
    }
}
