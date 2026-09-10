using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;
using Timlog.Domain.Entities;
using Timlog.Infrastructure.Aade.Models;
using InvoiceType = Timlog.Infrastructure.Aade.Models.InvoiceType;

namespace Timlog.Infrastructure.Aade;

public class AadeClient : IAadeClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AadeClient> _logger;

    public AadeClient(HttpClient httpClient, ILogger<AadeClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<AadeOperationResult>> SendInvoiceAsync(
        Invoice invoice, 
        TenantCredential credential, 
        string aadeUserId, 
        string subscriptionKey, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Αποστολή τιμολογίου {InvoiceId} στο myDATA της ΑΑΔΕ (User: {UserId})", invoice.Id, aadeUserId);

        var invoicesDoc = MapToAadeXml(invoice, credential);
        var xmlPayload = SerializeToXml(invoicesDoc);
        using var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

        using var request = new HttpRequestMessage(HttpMethod.Post, "SendInvoices");
        request.Content = content;
        request.Headers.Add("aade-user-id", aadeUserId);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Η κλήση στο SendInvoices της ΑΑΔΕ απέτυχε με status {StatusCode}. Body: {ResponseBody}", response.StatusCode, responseXml);
            return Result<AadeOperationResult>.Fail($"AADE HTTP {(int)response.StatusCode}: {responseXml}");
        }

        var responseDoc = DeserializeFromXml<ResponseDoc>(responseXml);
        var firstResponse = responseDoc?.Responses?.FirstOrDefault();
        
        if (firstResponse != null && firstResponse.StatusCode == "Success")
        {
            _logger.LogInformation("Το τιμολόγιο {InvoiceId} καταχωρήθηκε επιτυχώς στην ΑΑΔΕ με MARK: {Mark}", invoice.Id, firstResponse.InvoiceMark);
            return Result<AadeOperationResult>.Ok(new AadeOperationResult(firstResponse.InvoiceMark, firstResponse.QrUrl));
        }

        var errorMessage = firstResponse?.Errors?.ErrorList?.FirstOrDefault()?.Message ?? "Unknown AADE Error";
        _logger.LogWarning("Η ΑΑΔΕ απέρριψε το τιμολόγιο {InvoiceId}: {ErrorMessage}", invoice.Id, errorMessage);
        return Result<AadeOperationResult>.Fail(errorMessage);
    }

    public async Task<Result<string>> GetInvoiceFromAadeAsync(long mark, string aadeUserId, string subscriptionKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ανάκτηση τιμολογίου με Mark {Mark} από την ΑΑΔΕ", mark);

        var url = $"RequestTransmittedDocs?mark={mark - 1}&maxMark={mark}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("aade-user-id", aadeUserId);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Αποτυχία ανάκτησης τιμολογίου {Mark} από την ΑΑΔΕ. Status: {StatusCode}", mark, response.StatusCode);
            return Result<string>.Fail($"Αποτυχία επικοινωνίας με την ΑΑΔΕ (Κωδικός: {response.StatusCode}).");
        }

        var xmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<string>.Ok(xmlContent);
    }

    public async Task<Result> CancelInvoiceAsync(string mark, string aadeUserId, string subscriptionKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Αίτημα ακύρωσης τιμολογίου με Mark {Mark} στην ΑΑΔΕ", mark);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"CancelInvoice?mark={mark}");
        request.Headers.Add("aade-user-id", aadeUserId);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
    
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Αποτυχία ακύρωσης τιμολογίου {Mark} στην ΑΑΔΕ. Status: {StatusCode}", mark, response.StatusCode);
            return Result.Fail($"Αποτυχία επικοινωνίας με την ΑΑΔΕ (Κωδικός: {response.StatusCode}).");
        }

        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDoc = DeserializeFromXml<ResponseDoc>(responseXml);
        var firstResponse = responseDoc?.Responses?.FirstOrDefault();

        if (firstResponse != null && firstResponse.StatusCode == "Success")
        {
            _logger.LogInformation("Η ακύρωση του τιμολογίου με Mark {Mark} ολοκληρώθηκε στην ΑΑΔΕ", mark);
            return Result.Ok();
        }

        var errorMessage = firstResponse?.Errors?.ErrorList?.FirstOrDefault()?.Message ?? "Άγνωστο σφάλμα κατά την ακύρωση.";
        _logger.LogWarning("Η ΑΑΔΕ απέρριψε την ακύρωση για Mark {Mark}: {ErrorMessage}", mark, errorMessage);
        return Result.Fail(errorMessage);
    }

    private InvoicesDoc MapToAadeXml(Invoice invoice, TenantCredential credential)
    {
        var netAmount = Math.Round(invoice.NetAmount, 2, MidpointRounding.AwayFromZero);
        var vatAmount = Math.Round(invoice.VatAmount, 2, MidpointRounding.AwayFromZero);
        var withholdingAmount = Math.Round(invoice.WithholdingAmount, 2, MidpointRounding.AwayFromZero);
        
        var totalGross = netAmount + vatAmount - withholdingAmount;

        var aadeInvoiceType = invoice.Type switch
        {
            Timlog.Domain.Enums.InvoiceType.ServicesDomestic => InvoiceType.Item21,
            Timlog.Domain.Enums.InvoiceType.ServicesIntraCommunity => InvoiceType.Item22,
            Timlog.Domain.Enums.InvoiceType.ServicesThirdCountry => InvoiceType.Item23,
            _ => throw new InvalidOperationException($"Μη υποστηριζόμενος τύπος παραστατικού: {invoice.Type}")
        };

        int vatCategory = invoice.Type == Timlog.Domain.Enums.InvoiceType.ServicesDomestic ? 1 : 7;

        int? vatExemptionCategory = invoice.Type switch
        {
            Timlog.Domain.Enums.InvoiceType.ServicesIntraCommunity => 4,
            Timlog.Domain.Enums.InvoiceType.ServicesThirdCountry => 14,
            Timlog.Domain.Enums.InvoiceType.ServicesDomestic => null,
            _ => throw new InvalidOperationException($"Άγνωστη κατηγορία απαλλαγής ΦΠΑ για τον τύπο: {invoice.Type}")
        };

        var incomeClassificationType = invoice.Type switch
        {
            Timlog.Domain.Enums.InvoiceType.ServicesDomestic => Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationValueType.E3561001,
            Timlog.Domain.Enums.InvoiceType.ServicesIntraCommunity => Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationValueType.E3561005,
            Timlog.Domain.Enums.InvoiceType.ServicesThirdCountry => Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationValueType.E3561006,
            _ => throw new InvalidOperationException($"Μη υποστηριζόμενος τύπος ταξινόμησης εσόδων: {invoice.Type}")
        };

        var invoiceRow = new InvoiceRowType
        {
            LineNumber = 1,
            NetValue = netAmount,
            VatCategory = vatCategory,
            VatAmount = vatAmount,
            IncomeClassification =
            {
                new Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationType
                {
                    ClassificationType = incomeClassificationType,
                    ClassificationTypeSpecified = true,
                    ClassificationCategory = Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationCategoryType.Category13,
                    Amount = netAmount
                }
            }
        };

        if (vatExemptionCategory.HasValue)
        {
            invoiceRow.VatExemptionCategory = vatExemptionCategory.Value;
            invoiceRow.VatExemptionCategorySpecified = true;
        }

        if (withholdingAmount > 0)
        {
            invoiceRow.WithheldAmount = withholdingAmount;
            invoiceRow.WithheldAmountSpecified = true;
            invoiceRow.WithheldPercentCategory = 3; 
            invoiceRow.WithheldPercentCategorySpecified = true;
        }

        var isIssuerDomestic = invoice.IssuerAfm.Value.Length == 9 && !char.IsLetter(invoice.IssuerAfm.Value[0]) 
            || invoice.IssuerAfm.Value.StartsWith("GR", StringComparison.OrdinalIgnoreCase);

        CountryType issuerCountry;
        if (isIssuerDomestic)
        {
            issuerCountry = CountryType.Gr;
        }
        else if (invoice.IssuerAfm.Value.Length >= 2 && Enum.TryParse<CountryType>(invoice.IssuerAfm.Value[..2], true, out var parsedIssuerCountry))
        {
            issuerCountry = parsedIssuerCountry;
        }
        else
        {
            throw new InvalidOperationException($"Δεν ήταν δυνατή η αναγνώριση της χώρας εκδότη: {invoice.IssuerAfm.Value}");
        }

        if (!isIssuerDomestic && string.IsNullOrWhiteSpace(credential.CompanyName))
        {
            throw new InvalidOperationException("Η επωνυμία εκδότη είναι υποχρεωτική για εκδότες εξωτερικού.");
        }

        var isDomestic = invoice.Type == Timlog.Domain.Enums.InvoiceType.ServicesDomestic;

        if (!isDomestic && string.IsNullOrWhiteSpace(invoice.ClientName))
        {
            throw new InvalidOperationException("Η επωνυμία πελάτη είναι υποχρεωτική για παραστατικά εξωτερικού.");
        }

        CountryType counterpartCountry;
        
        if (isDomestic)
        {
            counterpartCountry = CountryType.Gr;
        }
        else if (invoice.Type == Timlog.Domain.Enums.InvoiceType.ServicesIntraCommunity)
        {
            if (invoice.ClientAfm.Value.Length < 2 || !Enum.TryParse<CountryType>(invoice.ClientAfm.Value[..2], true, out counterpartCountry))
            {
                throw new InvalidOperationException($"Δεν ήταν δυνατή η αναγνώριση της χώρας από το πρόθεμα του ΑΦΜ: {invoice.ClientAfm.Value}");
            }
        }
        else
        {
            counterpartCountry = CountryType.Us;
        }

        var aadeInvoice = new AadeBookInvoiceType
        {
            Issuer = new PartyType
            {
                VatNumber = invoice.IssuerAfm.Value,
                Country = issuerCountry,
                Branch = 0,
                Name = isIssuerDomestic ? null : credential.CompanyName,
                Address = isIssuerDomestic || string.IsNullOrWhiteSpace(credential.PostalCode) ? null : new AddressType 
                { 
                    Street = credential.Street,
                    Number = credential.Number,
                    PostalCode = credential.PostalCode, 
                    City = credential.City 
                }
            },
            Counterpart = new PartyType
            {
                Name = isDomestic ? null : invoice.ClientName,
                VatNumber = invoice.ClientAfm.Value,
                Country = counterpartCountry,
                Branch = 0,
                Address = isDomestic ? null : new AddressType 
                { 
                    PostalCode = "00000", 
                    City = "Unknown" 
                }
            },
            InvoiceHeader = new InvoiceHeaderType
            {
                Series = "A",
                Aa = invoice.InvoiceNumber.ToString(),
                IssueDate = invoice.IssueDate,
                InvoiceType = aadeInvoiceType,
                Currency = CurrencyType.Eur,
                CurrencySpecified = true
            },
            InvoiceDetails =
            {
                invoiceRow
            },
            InvoiceSummary = new InvoiceSummaryType
            {
                TotalNetValue = netAmount,
                TotalVatAmount = vatAmount,
                TotalWithheldAmount = withholdingAmount,
                TotalFeesAmount = 0m,
                TotalStampDutyAmount = 0m,
                TotalOtherTaxesAmount = 0m,
                TotalDeductionsAmount = 0m,
                TotalGrossValue = totalGross,
                IncomeClassification =
                {
                    new Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationType
                    {
                        ClassificationType = incomeClassificationType,
                        ClassificationTypeSpecified = true,
                        ClassificationCategory = Gr.Aade.MyData.IncomeClassificaton.V1._0.IncomeClassificationCategoryType.Category13,
                        Amount = netAmount
                    }
                }
            },
            PaymentMethods =
            {
                new PaymentMethodDetailType
                {
                    Type = 3,
                    Amount = totalGross
                }
            }
        };

        var doc = new InvoicesDoc();
        doc.Invoice.Add(aadeInvoice);
        return doc;
    }

    private string SerializeToXml<T>(T obj)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var stringWriter = new StringWriter();
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", "http://www.aade.gr/myDATA/invoice/v1.0");
        serializer.Serialize(stringWriter, obj, namespaces);
        return stringWriter.ToString();
    }

    private T? DeserializeFromXml<T>(string xml)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var stringReader = new StringReader(xml);
        return (T?)serializer.Deserialize(stringReader);
    }
}