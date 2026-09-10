using System.Xml.Serialization;

namespace Timlog.Infrastructure.Aade.Models;

[XmlRoot(ElementName = "ResponseDoc")]
public class ResponseDoc
{
    [XmlElement(ElementName = "response")]
    public List<AadeResponse> Responses { get; set; } = new();
}

public class AadeResponse
{
    [XmlElement(ElementName = "index")]
    public int Index { get; set; }

    [XmlElement(ElementName = "invoiceUid")]
    public string? InvoiceUid { get; set; }

    [XmlElement(ElementName = "invoiceMark")]
    public string? InvoiceMark { get; set; }

    [XmlElement(ElementName = "qrUrl")]
    public string? QrUrl { get; set; }

    [XmlElement(ElementName = "statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [XmlElement(ElementName = "errors")]
    public AadeErrors? Errors { get; set; }
}

public class AadeErrors
{
    [XmlElement(ElementName = "error")]
    public List<AadeError> ErrorList { get; set; } = new();
}

public class AadeError
{
    [XmlElement(ElementName = "message")]
    public string Message { get; set; } = string.Empty;

    [XmlElement(ElementName = "code")]
    public string Code { get; set; } = string.Empty;
}