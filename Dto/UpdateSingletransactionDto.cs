namespace AdatHisabdubai.Dto
{
    public class UpdateSingletransactionDto
    {
        public int? PartyId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal? Amount { get; set; }
        public int? CurrencyId { get; set; }
        public string? PaymentType { get; set; }
        public int? BalanceType { get; set; }
        public decimal? ExRate { get; set; }
        public decimal? ConvertAmount { get; set; }
        public string? Remark { get; set; }
        public decimal? AdatPercent { get; set; }

        public decimal? AdatAmount { get; set; }

        public decimal? AmountWithoutAdat { get; set; }
        public DateOnly? CreatDate { get; set; }

    }
}
