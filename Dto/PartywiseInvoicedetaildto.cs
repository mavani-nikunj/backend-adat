namespace AdatHisabdubai.Dto
{
    public class PartywiseInvoicedetaildto
    {
        public int Id { get; set; }
        public DateTime? Invoicedate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? PeymentAmount { get; set; }
        public decimal? ReceiveAmount { get; set; }
        public decimal? BalanceAmount { get; set; }


    }
}
