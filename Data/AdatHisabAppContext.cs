using Microsoft.EntityFrameworkCore;

namespace AdatHisabdubai.Data;

public partial class AdatHisabAppContext : DbContext
{
    public AdatHisabAppContext()
    {
    }

    public AdatHisabAppContext(DbContextOptions<AdatHisabAppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Adatdetail> Adatdetails { get; set; }

    public virtual DbSet<Clientmaster> Clientmasters { get; set; }

    public virtual DbSet<CurrencyType> CurrencyTypes { get; set; }

    public virtual DbSet<Currencymst> Currencymsts { get; set; }

    public virtual DbSet<Exchangerate> Exchangerates { get; set; }

    public virtual DbSet<Extrainvoice> Extrainvoices { get; set; }

    public virtual DbSet<Invoicedetail> Invoicedetails { get; set; }

    public virtual DbSet<Jventry> Jventries { get; set; }

    public virtual DbSet<Openingbalance> Openingbalances { get; set; }

    public virtual DbSet<Partymaster> Partymasters { get; set; }

    public virtual DbSet<Rolemst> Rolemsts { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<Yearmaster> Yearmasters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Server=DESKTOP-TNJ6CUF;Database=AdatHisabApp;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Adatdetail>(entity =>
        {
            entity.ToTable("Adatdetail");

            entity.Property(e => e.Amount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Udate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(50);
        });

        modelBuilder.Entity<Clientmaster>(entity =>
        {
            entity.ToTable("Clientmaster");

            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Phoneno).HasMaxLength(50);
            entity.Property(e => e.Udate).HasColumnType("datetime");
            entity.Property(e => e.UserName).HasMaxLength(50);
        });

        modelBuilder.Entity<CurrencyType>(entity =>
        {
            entity.ToTable("CurrencyType");

            entity.Property(e => e.CurrencyType1)
                .HasMaxLength(50)
                .HasColumnName("CurrencyType");
        });

        modelBuilder.Entity<Currencymst>(entity =>
        {
            entity.ToTable("Currencymst");

            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Udate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Exchangerate>(entity =>
        {
            entity.ToTable("Exchangerate");

            entity.Property(e => e.Rate).HasColumnType("numeric(18, 6)");
        });

        modelBuilder.Entity<Extrainvoice>(entity =>
        {
            entity.ToTable("Extrainvoice");

            entity.Property(e => e.AdatAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.AdatPercent).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Amount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Carat).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.FinalAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Udate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(50);
        });

        modelBuilder.Entity<Invoicedetail>(entity =>
        {
            entity.ToTable("Invoicedetail");

            entity.Property(e => e.Adat).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.AdatAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.AdatPercent).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Amount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Carat).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.ConsignExpenseAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.ConsignExpensePercent).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.FinalAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.InvoiceNo).HasMaxLength(50);
            entity.Property(e => e.OpeningType).HasMaxLength(50);
            entity.Property(e => e.Rate).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.ShiparExpenseAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.ShiparExpensePercent).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Udate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(50);
        });

        modelBuilder.Entity<Jventry>(entity =>
        {
            entity.ToTable("Jventry");

            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Jvdate).HasColumnType("datetime");
            entity.Property(e => e.Udate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Openingbalance>(entity =>
        {
            entity.ToTable("Openingbalance");

            entity.Property(e => e.Amount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Udate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Partymaster>(entity =>
        {
            entity.ToTable("Partymaster");

            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.PartyType).HasMaxLength(50);
            entity.Property(e => e.PhoneNo).HasMaxLength(50);
            entity.Property(e => e.Udate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Rolemst>(entity =>
        {
            entity.ToTable("Rolemst");

            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Udate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transaction");

            entity.Property(e => e.AdatAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.AdatPercent).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Amount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.AmountWithoutAdat).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.ConvertAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.ExRate).HasColumnType("numeric(18, 6)");
            entity.Property(e => e.PaymentType).HasMaxLength(50);
            entity.Property(e => e.Udate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Yearmaster>(entity =>
        {
            entity.ToTable("Yearmaster");

            entity.Property(e => e.Cdate).HasColumnType("datetime");
            entity.Property(e => e.Udate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
