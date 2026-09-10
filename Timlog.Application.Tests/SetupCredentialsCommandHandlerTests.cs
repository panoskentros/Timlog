using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Timlog.Application.Commands;
using Timlog.Application.Interfaces;
using Timlog.Domain.Entities;
using Xunit;

namespace Timlog.Application.Tests;

public class SetupCredentialsCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepoMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionServiceMock = new();
    private readonly Mock<IValidator<SetupCredentialsCommand>> _validatorMock = new();
    private readonly Mock<ILogger<SetupCredentialsCommandHandler>> _loggerMock = new();
    private readonly SetupCredentialsCommandHandler _handler;

    public SetupCredentialsCommandHandlerTests()
    {
        _handler = new SetupCredentialsCommandHandler(
            _credentialRepoMock.Object,
            _encryptionServiceMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsFailureWithoutPersisting()
    {
        var command = new SetupCredentialsCommand(
            12345,
            "INVALID",
            "user123",
            "key123",
            "Company",
            "Street",
            "10",
            "12345",
            "Athens");

        var validationFailures = new List<ValidationFailure> { new("IssuerAfm", "Το ΑΦΜ δεν είναι έγκυρο.") };
        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Success);
        Assert.Equal("Το ΑΦΜ δεν είναι έγκυρο.", result.Message);
        
        _credentialRepoMock.Verify(r => r.AddAsync(It.IsAny<TenantCredential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewTenant_EncryptsAndAddsCredential()
    {
        var command = new SetupCredentialsCommand(12345, "094259216", "user123", "key123", "Company", "Street", "10", "12345", "Athens");

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCredential?)null);

        _encryptionServiceMock.Setup(e => e.Encrypt("user123")).Returns("enc_user123");
        _encryptionServiceMock.Setup(e => e.Encrypt("key123")).Returns("enc_key123");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Success);
        _credentialRepoMock.Verify(r => r.AddAsync(It.Is<TenantCredential>(c => 
            c.TelegramChatId == 12345 && 
            c.EncryptedAadeUserId == "enc_user123" &&
            c.EncryptedSubscriptionKey == "enc_key123"), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingTenant_UpdatesExistingEntity()
    {
        var command = new SetupCredentialsCommand(12345, "094259216", "new_user", "new_key", "Updated Co", "Updated St", "11", "54321", "Patras");

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var existing = new TenantCredential(Guid.NewGuid(), 12345, "094259216", "old_enc_u", "old_enc_k");

        _credentialRepoMock
            .Setup(r => r.GetByChatIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _encryptionServiceMock.Setup(e => e.Encrypt("new_user")).Returns("new_enc_u");
        _encryptionServiceMock.Setup(e => e.Encrypt("new_key")).Returns("new_enc_k");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Success);
        Assert.Equal("new_enc_u", existing.EncryptedAadeUserId);
        Assert.Equal("new_enc_k", existing.EncryptedSubscriptionKey);
        Assert.Equal("Updated Co", existing.CompanyName);

        _credentialRepoMock.Verify(r => r.AddAsync(It.IsAny<TenantCredential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}