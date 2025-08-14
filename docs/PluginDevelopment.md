# IIM Plugin Development Guide

## Overview

IIM supports dynamic plugins that extend investigation capabilities. Plugins can:
- Integrate with external APIs and databases
- Execute forensic tools
- Process evidence with custom algorithms
- Add new investigation workflows

## Quick Start

### 1. Install the IIM CLI

```bash
dotnet tool install -g IIM.CLI
```

### 2. Create a New Plugin

```bash
dotnet new iimplugin -n MyAwesomePlugin
cd MyAwesomePlugin
```

### 3. Implement Your Plugin

```csharp
using IIM.Plugin.SDK;

[PluginMetadata(Category = "forensics", Tags = new[] { "custom" })]
public class MyAwesomePlugin : InvestigationPlugin
{
    public override string Id => "com.mycompany.awesome";
    public override string Name => "My Awesome Plugin";
    
    public override async Task<PluginResult> ExecuteAsync(
        PluginRequest request, 
        CancellationToken ct)
    {
        // Your implementation here
        return PluginResult.CreateSuccess(new { 
            Message = "Hello from my plugin!" 
        });
    }
}
```

### 4. Build and Test

```bash
# Build the plugin
iim plugin build

# Test locally
iim plugin test ./bin/Release/MyAwesomePlugin.dll --intent "my_intent"

# Package for distribution
iim plugin package
```

## Plugin Architecture

### Security Model

All plugins run in a sandboxed environment with:
- Restricted file system access
- Rate-limited network requests
- Whitelisted process execution
- Memory and CPU limits

### Available APIs

Plugins have access to secure versions of:
- File system operations
- HTTP client for API calls
- Process runner for tools
- Evidence store for saving results
- Logging infrastructure

## Intent System

Plugins declare supported intents that map to user queries:

```csharp
[IntentHandler("analyze_email")]
public async Task<PluginResult> AnalyzeEmail(PluginRequest request)
{
    var email = request.Parameters["email"];
    // Analysis logic
}
```

## Distribution

### Official Repository

Submit plugins for review:
```bash
iim plugin publish ./my-plugin.iimplugin --api-key $KEY
```

### Private Distribution

For internal/proprietary plugins:
1. Package: `iim plugin package`
2. Sign: `iim plugin sign --cert agency.pfx`
3. Deploy to internal repository

## Best Practices

1. **Handle Errors Gracefully**
   ```csharp
   try
   {
       // Your logic
   }
   catch (Exception ex)
   {
       Logger.LogError(ex, "Operation failed");
       return PluginResult.CreateError(ex.Message);
   }
   ```

2. **Validate Input**
   ```csharp
   if (!request.Parameters.ContainsKey("required_param"))
   {
       return PluginResult.CreateError("Missing required parameter");
   }
   ```

3. **Use Async/Await**
   ```csharp
   public override async Task<PluginResult> ExecuteAsync(...)
   {
       await LongRunningOperation();
   }
   ```

4. **Store Evidence**
   ```csharp
   await Evidence.StoreAsync("analysis_result", data);
   ```

## Examples

See the `examples/` directory for complete plugin examples:
- Hash Analyzer - File hash analysis
- OSINT Email - Email intelligence gathering
- Memory Analyzer - Volatility integration

## Troubleshooting

### Plugin wont load
- Check `iim plugin validate` output
- Ensure all dependencies are included
- Verify manifest is correct

### Permission denied
- Check requested permissions in manifest
- Some operations require elevated permissions

### Performance issues
- Use async operations
- Batch API requests
- Cache results when appropriate
