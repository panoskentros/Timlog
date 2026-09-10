using Timlog.Domain.Enums;
using Timlog.Domain.ValueObjects;

namespace Timlog.Domain.Entities;

public class Invoice
{
    public Guid Id { get; private set; }
    public long InvoiceNumber { get; private set; }
    public Afm IssuerAfm { get; private set; } = null!;
    public Afm ClientAfm { get; private set; } = null!;
    public string? ClientName { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal WithholdingAmount { get; private set; }
    public decimal TotalPayable { get; private set; }
    public InvoiceType Type { get; private set; }
    public DateTime IssueDate { get; private set; }
    public string? AadeMark { get; private set; }
    public string? QrUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDraft { get; private set; } = true;

    private Invoice() { }

    public Invoice(
        Guid id, 
        long invoiceNumber, 
        Afm issuerAfm, 
        Afm clientAfm, 
        string? clientName, 
        decimal netAmount, 
        decimal vatAmount, 
        decimal withholdingAmount, 
        decimal totalPayable, 
        InvoiceType type)
    {
        Id = id;
        InvoiceNumber = invoiceNumber;
        IssuerAfm = issuerAfm;
        ClientAfm = clientAfm;
        ClientName = clientName;
        NetAmount = netAmount;
        VatAmount = vatAmount;
        WithholdingAmount = withholdingAmount;
        TotalPayable = totalPayable;
        Type = type;
        IssueDate = DateTime.UtcNow;
    }

    public void SetQrUrl(string url)
    {
        QrUrl = url;
    }

    public void SetAadeMark(string mark)
    {
        AadeMark = mark;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void MarkAsIssued(string aadeMark, string qrUrl)
    {
        IsDraft = false;
        AadeMark = aadeMark;
        QrUrl = qrUrl;
    }
}