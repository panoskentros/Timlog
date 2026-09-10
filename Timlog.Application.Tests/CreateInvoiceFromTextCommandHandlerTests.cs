using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Timlog.Application.Commands;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;
using Timlog.Domain.Entities;
using Timlog.Domain.Enums;
using Xunit;

namespace Timlog.Application.Tests;

public class CreateInvoiceFromTextCommandHandlerTests
{
    private readonly Mock<IInvoiceTextParser> _parserMock = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
    private readonly Mock<ICredentialRepository> _credentialRepoMock = new();
    private readonly Mock<IValidator<CreateInvoiceFromTextCommand>> _validatorMock = new();
    private readonly Mock<ILogger<CreateInvoiceFromTextCommandHandler>> _loggerMock = new();
    private readonly CreateInvoiceFromTextCommandHandler _handler;

    public CreateInvoiceFromTextCommandHandlerTests()
    {
        _handler = new CreateInvoiceFromTextCommandHandler(
            _parserMock.Object,
            _invoiceRepoMock.Object,
            _credentialRepoMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsFailureResult()
    {
        var command = new CreateInvoiceFromTextCommand("");
        var validationFailures = new List<ValidationFailure> { new("Text", "Validation Error") };
        
        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        var result = await _handler.HandleAsync(12345, command);

        Assert.False(result.Success);
        Assert.Equal("Validation Error", result.Message);
    }

    [Fact]
    public async Task HandleAsync_MissingCredentials_ReturnsFailureResult()
    {
        var command = new CreateInvoiceFromTextCommand("Dummy text");

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCredential?)null);

        var result = await _handler.HandleAsync(12345, command);

        Assert.False(result.Success);
        Assert.Contains("/setup", result.Message);
    }

    [Fact]
    public async Task HandleAsync_ParserFails_ReturnsFailureResult()
    {
        var command = new CreateInvoiceFromTextCommand("Dummy text");

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var credential = new TenantCredential(Guid.NewGuid(), 12345, "094259216", "user", "key");
        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _parserMock
            .Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ParsedInvoiceDto>.Fail("Parser error"));

        var result = await _handler.HandleAsync(12345, command);

        Assert.False(result.Success);
        Assert.Equal("Parser error", result.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidInput_PersistsInvoiceAndReturnsSuccess()
    {
        var command = new CreateInvoiceFromTextCommand("Dummy text");

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var credential = new TenantCredential(Guid.NewGuid(), 12345, "094259216", "user", "key");
        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var parsedDto = new ParsedInvoiceDto("Client Name", "094259216", 1000m, "Services", (int)InvoiceType.ServicesDomestic, false);
        _parserMock
            .Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ParsedInvoiceDto>.Ok(parsedDto));

        _invoiceRepoMock
            .Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.HandleAsync(12345, command);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.InvoiceNumber);
        Assert.Equal("094259216", result.Data.ClientAfm);

        _invoiceRepoMock.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _invoiceRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}