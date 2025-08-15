using Microsoft.AspNetCore.SignalR.Client;

namespace IIM.App.Hybrid.Services
{
    // Stub implementations to get the app running

  

    public interface IimClient
    {
        Task<string> GetStatusAsync();
    }

 
    public interface IInferenceService
    {
        Task<string> InferAsync(string prompt);
    }

    public class InferenceService : IInferenceService
    {
        public Task<string> InferAsync(string prompt) => Task.FromResult("Mock response");
    }

    public interface IInvestigationService
    {
        Task<List<Investigation>> GetInvestigationsAsync();
    }

    public class InvestigationService : IInvestigationService
    {
        public Task<List<Investigation>> GetInvestigationsAsync() => Task.FromResult(new List<Investigation>());
    }

    public interface ICaseManager
    {
        string CurrentCase { get; }
    }

    public class CaseManager : ICaseManager
    {
        public string CurrentCase => "CASE-001";
    }

    public interface INotificationService
    {
        Task ShowNotificationAsync(string message);
    }

    public class NotificationService : INotificationService
    {
        public Task ShowNotificationAsync(string message) => Task.CompletedTask;
    }

    public interface IHubConnectionService
    {
        HubConnection? Connection { get; }
        Task StartAsync();
    }

    public class HubConnectionService : IHubConnectionService
    {
        public HubConnection? Connection { get; private set; }
        public Task StartAsync() => Task.CompletedTask;
    }

    public interface IQdrantService
    {
        Task<bool> IsHealthyAsync();
    }

    public class QdrantService : IQdrantService
    {
        public Task<bool> IsHealthyAsync() => Task.FromResult(true);
    }

    public interface IModelManagementService
    {
        Task<List<Model>> GetModelsAsync();
    }

  

    // Basic model classes
    public class Investigation
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class Model
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}

// Add to IIM.Core.Interfaces namespace
namespace IIM.Core.Interfaces
{
    // Empty for now - just to resolve namespace
}