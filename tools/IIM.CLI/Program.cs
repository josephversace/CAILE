using IIM.Infrastructure.Data;
using IIM.Infrastructure.Storage;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using IIM.Shared.Models.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Example of how you might use the CLI to run a task
        // This is a placeholder and needs to be expanded with real command-line parsing (e.g., using System.CommandLine)
        Console.WriteLine("IIM CLI Tool");
        Console.WriteLine("This is a placeholder for future command-line operations.");
        Console.WriteLine("---------------------------------------------------------");

        // Example: Using the FileManager to ingest a file
        using (var scope = host.Services.CreateScope())
        {
            var fileManager = scope.ServiceProvider.GetRequiredService<IManagedFileManager>();
            var workspaceProvider = scope.ServiceProvider.GetRequiredService<IWorkspaceProvider>();

            Console.WriteLine("Demonstrating file ingestion...");
            // In a real scenario, you would get workspaceId and file paths from command line arguments
            // For now, we will assume a dummy workspace and create a dummy file to ingest.

            // This is a simplified flow. A real CLI would have commands for creating workspaces first.
            var dummyWorkspaceId = Guid.NewGuid();
            var dummyFilePath = "sample-cli.txt";
            await File.WriteAllTextAsync(dummyFilePath, "This is a test file for the CLI.");

            try
            {
                using var stream = File.OpenRead(dummyFilePath);
                var virtualFile = new VirtualFile
                {
                    WorkspaceId = dummyWorkspaceId,
                    FileName = Path.GetFileName(dummyFilePath),
                    Path = "/",
                    FileSize = stream.Length,
                    CreatedBy = "CLI_USER",
                    CollectedBy = "CLI_USER",
                    CollectionDate = DateTimeOffset.UtcNow,
                    CollectedLocation = "Local CLI Execution"
                };

                var result = await fileManager.IngestFileAsync(stream, virtualFile);
                Console.WriteLine($"Successfully ingested file. Virtual File ID: {result.Id}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred during ingestion: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                if (File.Exists(dummyFilePath))
                {
                    File.Delete(dummyFilePath);
                }
            }
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);

                // Register all our services, just like in the API project
                services.AddSingleton<IDeduplicationService, DeduplicationService>();
                services.AddSingleton(sp =>
                    configuration.GetSection("S3Storage").Get<S3StorageConfiguration>() ?? new S3StorageConfiguration());
                services.AddSingleton<IObjectStorageProvider, SeaweedFSStorageProvider>();

                // For a CLI, you might use a different DB context setup or none at all
                // This is a simplified setup to resolve compilation errors.
                // services.AddDbContext<...>(); 
                services.AddScoped<IAuditRepository, EfAuditRepository>();
                services.AddScoped<IWorkspaceProvider, PostgresWorkspaceProvider>();

                services.AddScoped<IManagedFileManager, FileManager>();

            });
}
