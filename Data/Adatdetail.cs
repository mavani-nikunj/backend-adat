using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Adatdetail
{
    public int Id { get; set; }

    public int? PartyId { get; set; }

    public decimal? Amount { get; set; }

    public int? InvoiceId { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public string? Remark { get; set; }

    public int? ClientId { get; set; }

    public int? YearId { get; set; }

    public string? UserId { get; set; }

    public int? AdatPartyId { get; set; }

    public DateOnly? Invoicedate { get; set; }
}
