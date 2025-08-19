using IIM.Core.Services;
using IIM.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using IIM.Shared.Enums;
using IIM.Application.Services;
using IIM.Core.Tests.Mocks;

namespace IIM.Core.Tests.Services;

public class InvestigationServiceTests
{
    private readonly InvestigationService _sut;
    private readonly Mock<ILogger<InvestigationService>> _loggerMock;
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly Mock<IPdfService> _pdfServiceMock;

    public InvestigationServiceTests()
    {
        _loggerMock = new Mock<ILogger<InvestigationService>>();
        _exportServiceMock = new Mock<IExportService>();
        _pdfServiceMock = new Mock<IPdfService>();

        _sut = new InvestigationService(
            _loggerMock.Object,
            _exportServiceMock.Object,
            _pdfServiceMock.Object,
            null!, // Add other mocks as needed
            null!,
            null!
        );
    }

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
        result.CaseId.Should().Be(request.CaseId);
        result.Title.Should().Be(request.Title);
        result.Status.Should().Be(InvestigationStatus.Active);
    }

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
    }

    [Fact]
    public async Task GetSessionAsync_WithInvalidId_Should_ThrowKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetSessionAsync("invalid-id")
        );
    }
}