using System.ComponentModel.DataAnnotations;

namespace AdatHisabdubai.Dto
{
    public class Invoicedetaildto
    {


        public int? PartyId { get; set; }

        public DateOnly? Invoicedate { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Carat must be greater than 0")]
        public decimal Carat { get; set; }

        public decimal? Rate { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal? Amount { get; set; }

        public int? YearId { get; set; }

        public string? Remark { get; set; }

        public int? ClientId { get; set; }

        public int? Terms { get; set; }

        public DateOnly? Duedate { get; set; }

        public DateTime? Cdate { get; set; }

        public DateTime? Udate { get; set; }

        public decimal? Adat { get; set; }

        public decimal? FinalAmount { get; set; }

        public string? InvoiceNo { get; set; }

        public bool? IsOpening { get; set; }

        public string? OpeningType { get; set; }

        public int? CurrencyId { get; set; }

        public bool? IsExtra { get; set; }

        public decimal? AdatPercent { get; set; }

        public decimal? AdatAmount { get; set; }

        public string? UserId { get; set; }
    }
}
