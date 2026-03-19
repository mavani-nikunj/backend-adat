namespace AdatHisabdubai.Dto
{
    public class Outstandinginvoicedto
    {
        public int? InvoiceId { get; set; }
        public string? PartyName { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public decimal? ReceiveAmount { get; set; }
        public decimal? OutstandingAmount { get; set; }
        public int? Days { get; set; }
        public string? SubParty { get; set; }


    }
}
