using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Transaction
{
    public int Id { get; set; }

    public int? JvId { get; set; }

    public int? PartyId { get; set; }

    public int? YearId { get; set; }

    public DateOnly? CreatDate { get; set; }

    public decimal? Amount { get; set; }

    public int? CurrencyId { get; set; }

    public string? PaymentType { get; set; }

    public decimal? ExRate { get; set; }

    public decimal? ConvertAmount { get; set; }

    public string? Remark { get; set; }

    public int? ClientId { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public int? BalanceType { get; set; }

    public int? InvoiceId { get; set; }

    public decimal? AdatPercent { get; set; }

    public decimal? AdatAmount { get; set; }

    public decimal? AmountWithoutAdat { get; set; }

    public int? Cashbankac { get; set; }

    public int? RevPartyId { get; set; }

    public int? ConvertCurrencyId { get; set; }
}
