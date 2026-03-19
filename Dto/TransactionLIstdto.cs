namespace AdatHisabdubai.Dto
{
    public class TransactionLIstdto
    {
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
    }
}
