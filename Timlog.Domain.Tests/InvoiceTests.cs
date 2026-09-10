using Timlog.Domain.Entities;
using Timlog.Domain.Enums;
using Timlog.Domain.ValueObjects;
using Xunit;

namespace Timlog.Domain.Tests;

public class InvoiceTests
{
    [Fact]
    public void Constructor_SetsDraftAndActiveState()
    {
        var invoice = CreateTestInvoice();

        Assert.True(invoice.IsDraft);
        Assert.True(invoice.IsActive);
        Assert.Null(invoice.AadeMark);
        Assert.Null(invoice.QrUrl);
    }

    [Fact]
    public void MarkAsIssued_SetsIssuedDetailsAndMarksNotDraft()
    {
        var invoice = CreateTestInvoice();

        invoice.MarkAsIssued("1234567890", "https://mydata.aade.gr/qr");

        Assert.False(invoice.IsDraft);
        Assert.Equal("1234567890", invoice.AadeMark);
        Assert.Equal("https://mydata.aade.gr/qr", invoice.QrUrl);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var invoice = CreateTestInvoice();

        invoice.Deactivate();

        Assert.False(invoice.IsActive);
    }

    private static Invoice CreateTestInvoice()
    {
        return new Invoice(
            Guid.NewGuid(),
            1,
            Afm.Create("094259216"),
            Afm.Create("094259216"),
            "Client Ltd",
            1000m,
            240m,
            200m,
            1040m,
            InvoiceType.ServicesDomestic);
    }
}