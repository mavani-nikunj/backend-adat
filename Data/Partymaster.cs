using System;
using System.Collections.Generic;

namespace AdatHisabdubai.Data;

public partial class Partymaster
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? PhoneNo { get; set; }

    public string? Email { get; set; }

    public string? Addresss { get; set; }

    public int? ClientId { get; set; }

    public int? YearId { get; set; }

    public DateTime? Cdate { get; set; }

    public DateTime? Udate { get; set; }

    public string? Remark { get; set; }

    public int? CurrencyId { get; set; }

    public string? PartyType { get; set; }

    public bool? IsGroupParty { get; set; }

    public int? GroupPartyId { get; set; }
}
