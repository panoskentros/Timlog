using Microsoft.Extensions.Logging;
using Moq;
using Timlog.Application.Commands;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;
using Timlog.Domain.Entities;
using Timlog.Domain.Enums;
using Timlog.Domain.ValueObjects;
using Xunit;

namespace Timlog.Application.Tests;

public class ApproveInvoiceCommandHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
    private readonly Mock<ICredentialRepository> _credentialRepoMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionServiceMock = new();
    private readonly Mock<IAadeClient> _aadeClientMock = new();
    private readonly Mock<ILogger<ApproveInvoiceCommandHandler>> _loggerMock = new();
    private readonly ApproveInvoiceCommandHandler _handler;

    public ApproveInvoiceCommandHandlerTests()
    {
        _handler = new ApproveInvoiceCommandHandler(
            _invoiceRepoMock.Object,
            _credentialRepoMock.Object,
            _encryptionServiceMock.Object,
            _aadeClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_InvoiceNotFound_ReturnsFailureResult()
    {
        _invoiceRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        var command = new ApproveInvoiceCommand(Guid.NewGuid(), 12345);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task HandleAsync_AadeClientFails_DoesNotIssueAndReturnsFailure()
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            1,
            Afm.Create("094259216"),
            Afm.Create("094259216"),
            "Client Name",
            100m,
            24m,
            0m,
            124m,
            InvoiceType.ServicesDomestic);

        var credential = new TenantCredential(Guid.NewGuid(), 12345, "094259216", "user", "key");

        _invoiceRepoMock
            .Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _encryptionServiceMock.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decrypted");

        _aadeClientMock
            .Setup(a => a.SendInvoiceAsync(invoice, credential, "decrypted", "decrypted", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AadeOperationResult>.Fail("AADE rejected invoice"));

        var command = new ApproveInvoiceCommand(invoice.Id, 12345);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.Success);
        Assert.Equal("AADE rejected invoice", result.Message);
        Assert.True(invoice.IsDraft);

        _invoiceRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AadeClientSucceeds_MarksIssuedAndSaves()
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            1,
            Afm.Create("094259216"),
            Afm.Create("094259216"),
            "Client Name",
            100m,
            24m,
            0m,
            124m,
            InvoiceType.ServicesDomestic);

        var credential = new TenantCredential(Guid.NewGuid(), 12345, "094259216", "user", "key");

        _invoiceRepoMock
            .Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _encryptionServiceMock.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decrypted");

        var aadeResponse = new AadeOperationResult("MARK12345", "https://mydata.aade.gr/qr");
        _aadeClientMock
            .Setup(a => a.SendInvoiceAsync(invoice, credential, "decrypted", "decrypted", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AadeOperationResult>.Ok(aadeResponse));

        var command = new ApproveInvoiceCommand(invoice.Id, 12345);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("MARK12345", result.Data.Mark);
        Assert.Equal("https://mydata.aade.gr/qr", result.Data.QrUrl);
        Assert.False(invoice.IsDraft);

        _invoiceRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}