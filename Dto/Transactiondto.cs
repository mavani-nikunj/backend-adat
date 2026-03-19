namespace AdatHisabdubai.Dto
{
    public class Transactiondto
    {
        public int? Id { get; set; }
        public int? JvId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Remark { get; set; }

        public List<TransactionLIstdto>? TransactionLIstdtos { get; set; }

    }
}
