using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Timlog.Application.Commands;
using Timlog.Application.Interfaces;
using Timlog.Infrastructure.Data;

namespace Timlog.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly TimlogDbContext _context;
    private readonly IAadeClient _aadeClient;
    private readonly CreateInvoiceFromTextCommandHandler _handler;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        TimlogDbContext context, 
        IAadeClient aadeClient, 
        CreateInvoiceFromTextCommandHandler handler,
        ICredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        ILogger<InvoicesController> logger)
    {
        _context = context;
        _aadeClient = aadeClient;
        _handler = handler;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    [HttpPost("issue")]
    public async Task<IActionResult> IssueInvoice([FromBody] IssueInvoiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST api/invoices/issue λήφθηκε για ChatId: {ChatId}", request.ChatId);

        var command = new CreateInvoiceFromTextCommand(request.Text);
        var result = await _handler.HandleAsync(request.ChatId, command, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Αποτυχία έκδοσης τιμολογίου μέσω API για ChatId: {ChatId}. Σφάλμα: {Error}", request.ChatId, result.Message);
            return BadRequest(new { error = result.Message });
        }

        _logger.LogInformation("Επιτυχής δημιουργία προσχεδίου τιμολογίου {InvoiceId} μέσω API για ChatId: {ChatId}", result.Data.InvoiceId, request.ChatId);

        return Ok(new 
        { 
            invoiceId = result.Data.InvoiceId, 
            mark = result.Data.Mark 
        });
    }

    [HttpGet("aade/{mark}")]
    public async Task<IActionResult> GetFromAade(long mark, [FromQuery] long chatId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET api/invoices/aade/{Mark} για ChatId: {ChatId}", mark, chatId);

        var credential = await _credentialRepository.GetByChatIdAsync(chatId, cancellationToken);
        if (credential == null)
        {
            _logger.LogWarning("Δεν βρέθηκαν διαπιστευτήρια για ανάκτηση τιμολογίου από ΑΑΔΕ. ChatId: {ChatId}", chatId);
            return BadRequest(new { error = "Τα διαπιστευτήρια δεν βρέθηκαν." });
        }

        var userId = _encryptionService.Decrypt(credential.EncryptedAadeUserId);
        var subKey = _encryptionService.Decrypt(credential.EncryptedSubscriptionKey);

        var aadeResult = await _aadeClient.GetInvoiceFromAadeAsync(mark, userId, subKey, cancellationToken);
    
        if (!aadeResult.Success)
        {
            _logger.LogWarning("Το τιμολόγιο με Mark {Mark} δεν βρέθηκε στην ΑΑΔΕ. Σφάλμα: {Error}", mark, aadeResult.Message);
            return NotFound(new { error = aadeResult.Message });
        }

        return Content(aadeResult.Data, "application/xml");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET api/invoices/{InvoiceId}", id);

        var invoice = await _context.Invoices.FindAsync(new object[] { id }, cancellationToken);
    
        if (invoice is null)
        {
            _logger.LogWarning("Το τιμολόγιο {InvoiceId} δεν βρέθηκε στη βάση δεδομένων.", id);
            return NotFound();
        }

        return Ok(invoice);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] long chatId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST api/invoices/{InvoiceId}/cancel για ChatId: {ChatId}", id, chatId);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var invoice = await _context.Invoices.FindAsync(new object[] { id }, cancellationToken);
        
            if (invoice is null)
            {
                _logger.LogWarning("Αδυναμία ακύρωσης: Το τιμολόγιο {InvoiceId} δεν βρέθηκε.", id);
                return NotFound();
            }

            if (string.IsNullOrEmpty(invoice.AadeMark))
            {
                _logger.LogWarning("Αδυναμία ακύρωσης: Το τιμολόγιο {InvoiceId} δεν έχει έγκυρο Mark ΑΑΔΕ.", id);
                return BadRequest(new { error = "Το τιμολόγιο δεν διαθέτει έγκυρο Mark από την ΑΑΔΕ για να ακυρωθεί." });
            }

            var credential = await _credentialRepository.GetByChatIdAsync(chatId, cancellationToken);
            if (credential == null)
            {
                _logger.LogWarning("Αδυναμία ακύρωσης: Δεν βρέθηκαν διαπιστευτήρια για ChatId: {ChatId}", chatId);
                return BadRequest(new { error = "Τα διαπιστευτήρια δεν βρέθηκαν." });
            }

            var userId = _encryptionService.Decrypt(credential.EncryptedAadeUserId);
            var subKey = _encryptionService.Decrypt(credential.EncryptedSubscriptionKey);

            _logger.LogInformation("Αποστολή αιτήματος ακύρωσης στην ΑΑΔΕ για Mark {Mark} (InvoiceId: {InvoiceId})", invoice.AadeMark, id);
            var cancelResult = await _aadeClient.CancelInvoiceAsync(invoice.AadeMark, userId, subKey, cancellationToken);

            if (!cancelResult.Success)
            {
                _logger.LogError("Η ακύρωση στην ΑΑΔΕ απέτυχε για τιμολόγιο {InvoiceId} (Mark: {Mark}). Σφάλμα: {Error}", id, invoice.AadeMark, cancelResult.Message);
                return BadRequest(new { error = cancelResult.Message ?? "Η ακύρωση στην ΑΑΔΕ απέτυχε." });
            }

            invoice.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Το τιμολόγιο {InvoiceId} ακυρώθηκε επιτυχώς στην ΑΑΔΕ και απενεργοποιήθηκε στη βάση.", id);

            return Ok(new { message = "Το τιμολόγιο ακυρώθηκε με επιτυχία.", invoiceId = invoice.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την ακύρωση του τιμολογίου {InvoiceId}", id);
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500);
        }
    }
}

public record IssueInvoiceRequest(long ChatId, string Text);