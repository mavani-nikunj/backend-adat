using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Jventry
{
    public int Id { get; set; }

    public DateTime? Jvdate { get; set; }

    public int? Jvid { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public int? ClientId { get; set; }

    public int? YearId { get; set; }

    public string? Remark { get; set; }
}
