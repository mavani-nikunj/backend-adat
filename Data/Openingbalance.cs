using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Openingbalance
{
    public int Id { get; set; }

    public int? CurrencyId { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public int? ClientId { get; set; }

    public int? YearId { get; set; }

    public DateOnly? CreatDate { get; set; }
}
