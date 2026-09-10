using System.ComponentModel.DataAnnotations;

namespace Timlog.Domain.Enums;

public enum InvoiceType
{
    [Display(Name = "Εγχώριες υπηρεσίες - B2B")]
    ServicesDomestic = 1,

    [Display(Name = "Ενδοκοινοτικές υπηρεσίες - εντός ΕΕ - B2B")]
    ServicesIntraCommunity = 2,

    [Display(Name = "Υπηρεσίες σε τρίτες χώρες - εκτός ΕΕ - B2B")]
    ServicesThirdCountry = 3
}