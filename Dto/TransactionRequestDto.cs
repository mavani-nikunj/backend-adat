namespace AdatHisabdubai.Dto
{
    public class TransactionRequestDto
    {
        public int? Id { get; set; }
        public int? JvId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Remark { get; set; }
        public TransactionItemDto Receive { get; set; }
        public TransactionItemDto Payment { get; set; }
    }
}
