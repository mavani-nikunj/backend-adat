using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Invoicedetail
{
    public int Id { get; set; }

    public int? ShiperId { get; set; }

    public int? ConsignId { get; set; }

    public DateOnly? Invoicedate { get; set; }

    public decimal? Carat { get; set; }

    public decimal? Rate { get; set; }

    public decimal? Amount { get; set; }

    public int? YearId { get; set; }

    public string? Remark { get; set; }

    public int? ClientId { get; set; }

    public int? Terms { get; set; }

    public DateOnly? Duedate { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public decimal? Adat { get; set; }

    public decimal? FinalAmount { get; set; }

    public string? InvoiceNo { get; set; }

    public bool? IsOpening { get; set; }

    public string? OpeningType { get; set; }

    public int? CurrencyId { get; set; }

    public bool? IsExtra { get; set; }

    public decimal? AdatPercent { get; set; }

    public decimal? AdatAmount { get; set; }

    public string? UserId { get; set; }

    public int? ShiperCompanyId { get; set; }

    public int? ConsignCompanyId { get; set; }

    public decimal? ShiparExpensePercent { get; set; }

    public decimal? ShiparExpenseAmount { get; set; }

    public decimal? ConsignExpensePercent { get; set; }

    public decimal? ConsignExpenseAmount { get; set; }

    public int? AdatpartyId { get; set; }
}
