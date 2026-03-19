namespace AdatHisabdubai.Dto
{
    public class Registerdot
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Phoneno { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? Remark { get; set; }

        public DateTime? Cdate { get; set; }

        public DateTime? Udate { get; set; }

        public string? UserName { get; set; }

        public string? UserPassword { get; set; }

        public int? UserRoleId { get; set; }
        public int? ClientId { get; set; }
    }
}
