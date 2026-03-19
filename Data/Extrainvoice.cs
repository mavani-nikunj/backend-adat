using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Extrainvoice
{
    public int Id { get; set; }

    public int? PartyId { get; set; }

    public decimal? Carat { get; set; }

    public decimal? Amount { get; set; }

    public decimal? AdatPercent { get; set; }

    public decimal? AdatAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public int? ClientId { get; set; }

    public int? YearId { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public string? UserId { get; set; }

    public string? Remark { get; set; }
}
