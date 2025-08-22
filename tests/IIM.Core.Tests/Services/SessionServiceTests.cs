using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IIM.Core.Models;
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

        /// <summary>
        /// Test that CreateSessionAsync creates a new session with correct properties
        /// </summary>
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

            // Act
            var result = await _sut.CreateSessionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.CaseId.Should().Be(request.CaseId);
            result.Title.Should().Be(request.Title);
            result.Type.Should().Be(InvestigationType.GeneralInquiry);
            result.Status.Should().Be(InvestigationStatus.Active);
            result.Messages.Should().BeEmpty();
            result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Test that GetSessionAsync returns the correct session
        /// </summary>
        [Fact]
        public async Task GetSessionAsync_WithValidId_Should_ReturnSession()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Test Investigation",
                InvestigationType = "GeneralInquiry"
            };
            var session = await _sut.CreateSessionAsync(request);

            // Act
            var result = await _sut.GetSessionAsync(session.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(session.Id);
            result.CaseId.Should().Be(session.CaseId);
        }

        /// <summary>
        /// Test that GetSessionAsync throws when session not found
        /// </summary>
        [Fact]
        public async Task GetSessionAsync_WithInvalidId_Should_ThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = "non-existent-id";

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.GetSessionAsync(invalidId)
            );
        }

        /// <summary>
        /// Test that UpdateSessionAsync updates the session correctly
        /// </summary>
        [Fact]
        public async Task UpdateSessionAsync_Should_UpdateSession()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Original Title",
                InvestigationType = "GeneralInquiry"
            };
            var session = await _sut.CreateSessionAsync(request);
            var originalUpdateTime = session.UpdatedAt;

            // Wait a bit to ensure timestamp difference
            await Task.Delay(10);

            // Act
            var result = await _sut.UpdateSessionAsync(session.Id, s =>
            {
                s.Title = "Updated Title";
                s.Status = InvestigationStatus.Completed;
            });

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Status.Should().Be(InvestigationStatus.Completed);
            result.UpdatedAt.Should().BeAfter(originalUpdateTime);
        }

        /// <summary>
        /// Test that CloseSessionAsync marks session as completed
        /// </summary>
        [Fact]
        public async Task CloseSessionAsync_Should_MarkSessionAsCompleted()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Test Investigation",
                InvestigationType = "GeneralInquiry"
            };
            var session = await _sut.CreateSessionAsync(request);

            // Act
            var result = await _sut.CloseSessionAsync(session.Id);
            var closedSession = await _sut.GetSessionAsync(session.Id);

            // Assert
            result.Should().BeTrue();
            closedSession.Status.Should().Be(InvestigationStatus.Completed);
        }

        /// <summary>
        /// Test that CloseSessionAsync returns false for non-existent session
        /// </summary>
        [Fact]
        public async Task CloseSessionAsync_WithInvalidId_Should_ReturnFalse()
        {
            // Arrange
            var invalidId = "non-existent-id";

            // Act
            var result = await _sut.CloseSessionAsync(invalidId);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that GetAllSessionsAsync returns all sessions
        /// </summary>
        [Fact]
        public async Task GetAllSessionsAsync_Should_ReturnAllSessions()
        {
            // Arrange
            var request1 = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Investigation 1",
                InvestigationType = "GeneralInquiry"
            };
            var request2 = new CreateSessionRequest
            {
                CaseId = "case-456",
                Title = "Investigation 2",
                InvestigationType = "DeepAnalysis"
            };

            await _sut.CreateSessionAsync(request1);
            await _sut.CreateSessionAsync(request2);

            // Act
            var result = await _sut.GetAllSessionsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(s => s.Title == "Investigation 1");
            result.Should().Contain(s => s.Title == "Investigation 2");
        }

        /// <summary>
        /// Test that GetSessionsByCaseAsync returns only sessions for specified case
        /// </summary>
        [Fact]
        public async Task GetSessionsByCaseAsync_Should_ReturnFilteredSessions()
        {
            // Arrange
            var caseId = "case-123";

            await _sut.CreateSessionAsync(new CreateSessionRequest
            {
                CaseId = caseId,
                Title = "Investigation 1",
                InvestigationType = "GeneralInquiry"
            });

            await _sut.CreateSessionAsync(new CreateSessionRequest
            {
                CaseId = caseId,
                Title = "Investigation 2",
                InvestigationType = "DeepAnalysis"
            });

            await _sut.CreateSessionAsync(new CreateSessionRequest
            {
                CaseId = "case-456",
                Title = "Different Case",
                InvestigationType = "GeneralInquiry"
            });

            // Act
            var result = await _sut.GetSessionsByCaseAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(s => s.CaseId == caseId);
        }

        /// <summary>
        /// Test that DeleteSessionAsync removes the session
        /// </summary>
        [Fact]
        public async Task DeleteSessionAsync_Should_RemoveSession()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Test Investigation",
                InvestigationType = "GeneralInquiry"
            };
            var session = await _sut.CreateSessionAsync(request);

            // Act
            var result = await _sut.DeleteSessionAsync(session.Id);

            // Assert
            result.Should().BeTrue();
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.GetSessionAsync(session.Id)
            );
        }

        /// <summary>
        /// Test that DeleteSessionAsync returns false for non-existent session
        /// </summary>
        [Fact]
        public async Task DeleteSessionAsync_WithInvalidId_Should_ReturnFalse()
        {
            // Arrange
            var invalidId = "non-existent-id";

            // Act
            var result = await _sut.DeleteSessionAsync(invalidId);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that AddMessageAsync adds message to session
        /// </summary>
        [Fact]
        public async Task AddMessageAsync_Should_AddMessageToSession()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Test Investigation",
                InvestigationType = "GeneralInquiry"
            };
            var session = await _sut.CreateSessionAsync(request);

            var message = new InvestigationMessage
            {
                Role = MessageRole.User,
                Content = "Test message",
                Timestamp = DateTimeOffset.UtcNow
            };

            // Act
            var result = await _sut.AddMessageAsync(session.Id, message);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1);
            result.Messages.First().Content.Should().Be("Test message");
        }

        /// <summary>
        /// Test that AddMessageAsync throws for non-existent session
        /// </summary>
        [Fact]
        public async Task AddMessageAsync_WithInvalidSessionId_Should_ThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = "non-existent-id";
            var message = new InvestigationMessage
            {
                Role = MessageRole.User,
                Content = "Test message"
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.AddMessageAsync(invalidId, message)
            );
        }

        /// <summary>
        /// Test that sessions are returned in correct order (most recent first)
        /// </summary>
        [Fact]
        public async Task GetAllSessionsAsync_Should_ReturnSessionsInCorrectOrder()
        {
            // Arrange
            var request1 = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "First",
                InvestigationType = "GeneralInquiry"
            };
            var session1 = await _sut.CreateSessionAsync(request1);

            await Task.Delay(10); // Ensure time difference

            var request2 = new CreateSessionRequest
            {
                CaseId = "case-123",
                Title = "Second",
                InvestigationType = "GeneralInquiry"
            };
            var session2 = await _sut.CreateSessionAsync(request2);

            // Update first session to make it most recent
            await _sut.UpdateSessionAsync(session1.Id, s => s.Title = "First Updated");

            // Act
            var result = await _sut.GetAllSessionsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().Id.Should().Be(session1.Id, "First session was updated most recently");
            result.Last().Id.Should().Be(session2.Id);
        }
    }
}