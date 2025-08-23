// ============================================
// File: tests/IIM.Core.Tests/Services/InvestigationServiceTests.cs
// Complete test implementation with all required mocks
// ============================================

using FluentAssertions;
using IIM.Application.Interfaces;
using IIM.Application.Services;
using IIM.Components.Pages;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Core.Services;

using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Xunit;

namespace IIM.Core.Tests.Services
{
    public class InvestigationServiceTests
    {
        private readonly InvestigationService _sut;
        private readonly Mock<ILogger<InvestigationService>> _loggerMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<IModelOrchestrator> _modelOrchestratorMock;
        private readonly Mock<IModelConfigurationTemplateService> _templateServiceMock;
        private readonly Mock<IExportService> _exportServiceMock;

        private readonly Mock<IVisualizationService> _visualizationServiceMock;

        public InvestigationServiceTests()
        {
            _loggerMock = new Mock<ILogger<InvestigationService>>();
            _sessionServiceMock = new Mock<ISessionService>();
            _modelOrchestratorMock = new Mock<IModelOrchestrator>();
            _templateServiceMock = new Mock<IModelConfigurationTemplateService>();
            _exportServiceMock = new Mock<IExportService>();
   
            _visualizationServiceMock = new Mock<IVisualizationService>();

            _sut = new InvestigationService(
                _loggerMock.Object,
                _sessionServiceMock.Object,
                _modelOrchestratorMock.Object,
                _templateServiceMock.Object,
                _exportServiceMock.Object,
    
                _visualizationServiceMock.Object
            );
        }

        #region Session Management Tests

        [Fact]
        public async Task CreateSessionAsync_Should_CreateNewSession()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Test Investigation",
                InvestigationType = "GeneralInquiry"
            };

            var expectedSession = new InvestigationSession
            {
                Id = Guid.NewGuid().ToString(),
                CaseId = request.CaseId,
                Title = request.Title,
                Type = InvestigationType.GeneralInquiry,
                Status = InvestigationStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Messages = new List<InvestigationMessage>(),
                EnabledTools = new List<string>(),
                Models = new Dictionary<string, ModelConfiguration>()
            };

            _sessionServiceMock
                .Setup(x => x.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _sut.CreateSessionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.CaseId.Should().Be(request.CaseId);
            result.Title.Should().Be(request.Title);
            result.Status.Should().Be(InvestigationStatus.Active);
            result.EnabledTools.Should().NotBeEmpty();
            result.Models.Should().NotBeEmpty();

            _sessionServiceMock.Verify(x => x.CreateSessionAsync(
                It.IsAny<CreateSessionRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSessionAsync_Should_ReturnSession()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var expectedSession = new InvestigationSession
            {
                Id = sessionId,
                CaseId = "case-123",
                Title = "Test Session",
                Status = InvestigationStatus.Active
            };

            _sessionServiceMock
                .Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _sut.GetSessionAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(sessionId);
            result.CaseId.Should().Be("case-123");
        }

        [Fact]
        public async Task DeleteSessionAsync_Should_CloseSession()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();

            _sessionServiceMock
                .Setup(x => x.CloseSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteSessionAsync(sessionId);

            // Assert
            result.Should().BeTrue();
            _sessionServiceMock.Verify(x => x.CloseSessionAsync(
                sessionId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Query Processing Tests

        [Fact]
        public async Task ProcessQueryAsync_Should_ProcessQuery()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var query = new InvestigationQuery
            {
                Text = "Test query",
                EnabledTools = new List<string> { "search" },
                Attachments = new List<Attachment>(),
                Context = new Dictionary<string, object>(),
                Timestamp = DateTimeOffset.UtcNow
            };

            var session = new InvestigationSession
            {
                Id = sessionId,
                CaseId = "case-123",
                Models = new Dictionary<string, ModelConfiguration>
                {
                    ["primary"] = new ModelConfiguration
                    {
                        ModelId = "test-model",
                        Provider = "test",
                        Type = ModelType.LLM,
                        Status = ModelStatus.Loaded
                    }
                }
            };

            _sessionServiceMock
                .Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _sessionServiceMock
                .Setup(x => x.AddMessageAsync(sessionId, It.IsAny<InvestigationMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            var result = await _sut.ProcessQueryAsync(sessionId, query);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Contain(query.Text);
            result.ToolResults.Should().NotBeNull();
            result.ToolResults.Should().HaveCount(1); // One tool was enabled

            _sessionServiceMock.Verify(x => x.AddMessageAsync(
                sessionId, It.IsAny<InvestigationMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SendQueryAsync_Should_DelegateToProcessQueryAsync()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var query = new InvestigationQuery
            {
                Text = "Test query",
                EnabledTools = new List<string>()
            };

            var session = new InvestigationSession
            {
                Id = sessionId,
                Models = new Dictionary<string, ModelConfiguration>()
            };

            _sessionServiceMock
                .Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            var result = await _sut.SendQueryAsync(sessionId, query);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Contain(query.Text);
        }

        #endregion

        #region Tool Execution Tests

        [Fact]
        public async Task ExecuteToolAsync_Should_ExecuteTool()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var toolName = "search";
            var parameters = new Dictionary<string, object>
            {
                ["query"] = "test search"
            };

            // Act
            var result = await _sut.ExecuteToolAsync(sessionId, toolName, parameters);

            // Assert
            result.Should().NotBeNull();
            result.ToolName.Should().Be(toolName);
            result.Status.Should().Be(ToolStatus.Success);
            result.Data.Should().NotBeNull();
            result.ExecutionTime.Should().BePositive();
            result.Metadata.Should().ContainKey("sessionId");
        }

        [Fact]
        public async Task ExecuteToolAsync_WithAnalysisTool_Should_IncludeVisualization()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var toolName = "data_analysis";
            var parameters = new Dictionary<string, object>();

            // Act
            var result = await _sut.ExecuteToolAsync(sessionId, toolName, parameters);

            // Assert
            result.Should().NotBeNull();
            result.Visualizations.Should().NotBeEmpty();
            result.Visualizations.First().Type.Should().Be(VisualizationType.Chart);
        }

        #endregion

        #region Case Management Tests

        //[Fact]
        //public async Task GetRecentCasesAsync_Should_ReturnRecentCases()
        //{
        //    // Act
        //    var result = await _sut.GetRecentCasesAsync(5);

        //    // Assert
        //    result.Should().NotBeNull();
        //    cases.Count.Should().BeLessThanOrEqualTo(maxCases);
        //    result.Should().BeInDescendingOrder(c => c.UpdatedAt);
        //}

        [Fact]
        public async Task GetCaseAsync_Should_ReturnCase()
        {
            // Arrange
            var caseId = "case-001"; // This is created in InitializeSampleData

            // Act
            var result = await _sut.GetCaseAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(caseId);
            result.Name.Should().Be("Sample Investigation");
        }

        [Fact]
        public async Task GetCaseAsync_WithInvalidId_Should_ThrowKeyNotFoundException()
        {
            // Arrange
            var caseId = "invalid-case-id";

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.GetCaseAsync(caseId));
        }

        #endregion

        #region Response Management Tests

        //[Fact]
        //public async Task EnrichResponseForDisplayAsync_Should_AddMetadata()
        //{
        //    // Arrange
        //    var response = new InvestigationResponse
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        Message = "Test response",
        //        ToolResults = new List<ToolResult>
        //        {
        //            new ToolResult { ToolName = "test" }
        //        },
        //        Citations = new List<Citation>(),
        //        RelatedEvidence = new List<Evidence>()
        //    };

        //    var message = new InvestigationMessage
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        Role = MessageRole.Assistant,
        //        Content = "Test message"
        //    };

        //    // Act
        //    var result = await _sut.EnrichResponseForDisplayAsync(response, message);

        //    // Assert
        //    result.Should().NotBeNull();
        //    result.DisplayMetadata.Should().NotBeNull();
        //    result.DisplayMetadata.Should().ContainKey("hasToolResults");
        //    result.DisplayMetadata["hasToolResults"].Should().Be(true);
        //    result.DisplayMetadata.Should().ContainKey("messageId");
        //}

        [Fact]
        public async Task GetResponseAsync_Should_ReturnStoredResponse()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var query = new InvestigationQuery { Text = "Test" };

            var session = new InvestigationSession { Id = sessionId };
            _sessionServiceMock
                .Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // First create a response
            var createdResponse = await _sut.ProcessQueryAsync(sessionId, query);

            // Act
            var result = await _sut.GetResponseAsync(createdResponse.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdResponse.Id);
        }


    

        #endregion
    }
}