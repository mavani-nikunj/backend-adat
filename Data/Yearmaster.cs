using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Yearmaster
{
    public int Id { get; set; }

    public DateOnly? Fromyear { get; set; }

    public DateOnly? Toyear { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public int? ClientId { get; set; }

    public string? UserId { get; set; }

    public bool? Isdelete { get; set; }
}
