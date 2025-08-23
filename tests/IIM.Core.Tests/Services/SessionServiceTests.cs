using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IIM.Core.Services;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IIM.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for SessionService
    /// </summary>
    public class SessionServiceTests
    {
        private readonly SessionService _sut;
        private readonly Mock<ILogger<SessionService>> _loggerMock;

        /// <summary>
        /// Initialize test fixtures
        /// </summary>
        public SessionServiceTests()
        {
            _loggerMock = new Mock<ILogger<SessionService>>();
            _sut = new SessionService(_loggerMock.Object);
        }

        [Fact]
        public async Task CreateSessionAsync_Should_CreateNewSession()
        {
            var request = new CreateSessionRequest("case-123", "Test Investigation", "GeneralInquiry");

            var result = await _sut.CreateSessionAsync(request);

            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.CaseId.Should().Be(request.CaseId);
            result.Title.Should().Be(request.Title);
            result.Type.Should().Be(InvestigationType.GeneralInquiry);
            result.Status.Should().Be(InvestigationStatus.Active);
            result.Messages.Should().BeEmpty();
            result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetSessionAsync_WithValidId_Should_ReturnSession()
        {
            var request = new CreateSessionRequest("case-123", "Test Investigation", "GeneralInquiry");
            var session = await _sut.CreateSessionAsync(request);

            var result = await _sut.GetSessionAsync(session.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(session.Id);
            result.CaseId.Should().Be(session.CaseId);
        }

        [Fact]
        public async Task GetSessionAsync_WithInvalidId_Should_ThrowKeyNotFoundException()
        {
            var invalidId = "non-existent-id";

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.GetSessionAsync(invalidId)
            );
        }

        [Fact]
        public async Task UpdateSessionAsync_Should_UpdateSession()
        {
            var request = new CreateSessionRequest("case-123", "Original Title", "GeneralInquiry");
            var session = await _sut.CreateSessionAsync(request);
            var originalUpdateTime = session.UpdatedAt;

            await Task.Delay(10);

            var result = await _sut.UpdateSessionAsync(session.Id, s =>
            {
                s.Title = "Updated Title";
                s.Status = InvestigationStatus.Completed;
            });

            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Status.Should().Be(InvestigationStatus.Completed);
            result.UpdatedAt.Should().BeAfter(originalUpdateTime);
        }

        [Fact]
        public async Task CloseSessionAsync_Should_MarkSessionAsCompleted()
        {
            var request = new CreateSessionRequest("case-123", "Test Investigation", "GeneralInquiry");
            var session = await _sut.CreateSessionAsync(request);

            var result = await _sut.CloseSessionAsync(session.Id);
            var closedSession = await _sut.GetSessionAsync(session.Id);

            result.Should().BeTrue();
            closedSession.Status.Should().Be(InvestigationStatus.Completed);
        }

        [Fact]
        public async Task CloseSessionAsync_WithInvalidId_Should_ReturnFalse()
        {
            var invalidId = "non-existent-id";

            var result = await _sut.CloseSessionAsync(invalidId);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllSessionsAsync_Should_ReturnAllSessions()
        {
            var request1 = new CreateSessionRequest("case-123", "Investigation 1", "GeneralInquiry");
            var request2 = new CreateSessionRequest("case-456", "Investigation 2", "DeepAnalysis");

            await _sut.CreateSessionAsync(request1);
            await _sut.CreateSessionAsync(request2);

            var result = await _sut.GetAllSessionsAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(s => s.Title == "Investigation 1");
            result.Should().Contain(s => s.Title == "Investigation 2");
        }

        [Fact]
        public async Task GetSessionsByCaseAsync_Should_ReturnFilteredSessions()
        {
            var caseId = "case-123";

            await _sut.CreateSessionAsync(new CreateSessionRequest(caseId, "Investigation 1", "GeneralInquiry"));
            await _sut.CreateSessionAsync(new CreateSessionRequest(caseId, "Investigation 2", "DeepAnalysis"));
            await _sut.CreateSessionAsync(new CreateSessionRequest("case-456", "Different Case", "GeneralInquiry"));

            var result = await _sut.GetSessionsByCaseAsync(caseId);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(s => s.CaseId == caseId);
        }

        [Fact]
        public async Task DeleteSessionAsync_Should_RemoveSession()
        {
            var request = new CreateSessionRequest("case-123", "Test Investigation", "GeneralInquiry");
            var session = await _sut.CreateSessionAsync(request);

            var result = await _sut.DeleteSessionAsync(session.Id);

            result.Should().BeTrue();
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.GetSessionAsync(session.Id)
            );
        }

        [Fact]
        public async Task DeleteSessionAsync_WithInvalidId_Should_ReturnFalse()
        {
            var invalidId = "non-existent-id";

            var result = await _sut.DeleteSessionAsync(invalidId);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddMessageAsync_Should_AddMessageToSession()
        {
            var request = new CreateSessionRequest("case-123", "Test Investigation", "GeneralInquiry");
            var session = await _sut.CreateSessionAsync(request);

            var message = new InvestigationMessage
            {
                Role = MessageRole.User,
                Content = "Test message",
                Timestamp = DateTimeOffset.UtcNow
            };

            var result = await _sut.AddMessageAsync(session.Id, message);

            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1);
            result.Messages.First().Content.Should().Be("Test message");
        }

        [Fact]
        public async Task AddMessageAsync_WithInvalidSessionId_Should_ThrowKeyNotFoundException()
        {
            var invalidId = "non-existent-id";
            var message = new InvestigationMessage
            {
                Role = MessageRole.User,
                Content = "Test message"
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.AddMessageAsync(invalidId, message)
            );
        }

        [Fact]
        public async Task GetAllSessionsAsync_Should_ReturnSessionsInCorrectOrder()
        {
            var request1 = new CreateSessionRequest("case-123", "First", "GeneralInquiry");
            var session1 = await _sut.CreateSessionAsync(request1);

            await Task.Delay(10);

            var request2 = new CreateSessionRequest("case-123", "Second", "GeneralInquiry");
            var session2 = await _sut.CreateSessionAsync(request2);

            await _sut.UpdateSessionAsync(session1.Id, s => s.Title = "First Updated");

            var result = await _sut.GetAllSessionsAsync();

            result.Should().HaveCount(2);
            result.First().Id.Should().Be(session1.Id, "First session was updated most recently");
            result.Last().Id.Should().Be(session2.Id);
        }
    }
}
