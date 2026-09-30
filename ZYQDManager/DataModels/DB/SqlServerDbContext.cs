using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataModels.Quotation;

namespace ZYQDManager.DataModels;

public class SqlServerDbContext : DbContext
{
    public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : base(options)
    {
    }

    public DbSet<ZyqdUserProfileModel> UserProfiles => Set<ZyqdUserProfileModel>();
    public DbSet<ZyqdUserCustomerAssignModel> UserCustomerAssigns => Set<ZyqdUserCustomerAssignModel>();
    public DbSet<MotorLxModel> MotorLx => Set<MotorLxModel>();
    public DbSet<MotorGgModel> MotorGg => Set<MotorGgModel>();
    public DbSet<MotorCrModel> MotorCr => Set<MotorCrModel>();
    public DbSet<ApprovalFlowModel> ApprovalFlows => Set<ApprovalFlowModel>();
    public DbSet<ZyqdCotractBaseModel> CotractBases => Set<ZyqdCotractBaseModel>();
    public DbSet<ZyqdCotractPiModel> CotractPis => Set<ZyqdCotractPiModel>();
    public DbSet<ZyqdSirMainModel> SirMains => Set<ZyqdSirMainModel>();
    public DbSet<ZyqdSirDetailModel> SirDetails => Set<ZyqdSirDetailModel>();
    public DbSet<ZyqdSirSampleForFilingModel> SirSamples => Set<ZyqdSirSampleForFilingModel>();
    public DbSet<ZyqdQuotationBaseMotorModel> QuotationBaseMotors => Set<ZyqdQuotationBaseMotorModel>();
    public DbSet<ZyqdQuotationBaseRemoteModel> QuotationBaseRemotes => Set<ZyqdQuotationBaseRemoteModel>();
    public DbSet<ZyqdQuotationBaseAccessoryModel> QuotationBaseAccessories => Set<ZyqdQuotationBaseAccessoryModel>();
    public DbSet<ZyqdQuotationBaseCbModel> QuotationBaseCbs => Set<ZyqdQuotationBaseCbModel>();
    public DbSet<ZyqdQuotationBasePrModel> QuotationBasePrs => Set<ZyqdQuotationBasePrModel>();
    public DbSet<ZyqdQuotationMainModel> QuotationMains => Set<ZyqdQuotationMainModel>();
    public DbSet<ZyqdQuotationDetailModel> QuotationDetails => Set<ZyqdQuotationDetailModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ZyqdUserProfileModel>(entity =>
        {
            entity.ToTable("ZYQD_UserProfile");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.EmpNo).IsUnique();
        });

        modelBuilder.Entity<ZyqdUserCustomerAssignModel>(entity =>
        {
            entity.ToTable("ZYQD_UserCustomerAssign");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.EmpNo, e.CustomerCode }).IsUnique();
            entity.HasIndex(e => e.EmpNo);
            entity.HasIndex(e => e.CustomerCode);
        });

        modelBuilder.Entity<MotorLxModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Motor_LX");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.MotorFunction).HasColumnName("Function");
            entity.Property(e => e.SCRequirementUL).HasColumnName("SCRequirement_UL");
            entity.Property(e => e.SCRequirementCE).HasColumnName("SCRequirement_CE");
            entity.Property(e => e.SCRequirementFCC).HasColumnName("SCRequirement_FCC");
            entity.Property(e => e.SCRequirementROHS).HasColumnName("SCRequirement_ROHS");
        });

        modelBuilder.Entity<MotorGgModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Motor_GG");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.Description).HasColumnName("Description");
        });

        modelBuilder.Entity<MotorCrModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Motor_CR");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.Description).HasColumnName("Description");
        });

        modelBuilder.Entity<ApprovalFlowModel>(entity =>
        {
            entity.ToTable("T_ZYQD_ApprovalFlow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Action).HasColumnName("Action");
        });

        modelBuilder.Entity<ZyqdCotractBaseModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Cotract_Base");
            entity.HasKey(e => e.Id);
            // 新库该表 Id 为 IDENTITY，由 SQL Server 生成；图片路径仍写 /Img/...，不写老库
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<ZyqdCotractPiModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Cotract_PI");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DocumentGuid).HasColumnName("Guid");
            entity.Property(e => e.SignDate).HasColumnName("DATE");
        });

        modelBuilder.Entity<ZyqdSirMainModel>(entity =>
        {
            entity.ToTable("T_ZYQD_SIR_main");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<ZyqdSirDetailModel>(entity =>
        {
            entity.ToTable("T_ZYQD_SIR_detail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<ZyqdSirSampleForFilingModel>(entity =>
        {
            entity.ToTable("T_ZYQD_SIR_datail_SampleForFiling");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        ConfigureQuotationBase<ZyqdQuotationBaseMotorModel>(modelBuilder, "T_ZYQD_Quotation_Base_Motor");
        ConfigureQuotationBase<ZyqdQuotationBaseRemoteModel>(modelBuilder, "T_ZYQD_Quotation_Base_Remote");
        ConfigureQuotationBase<ZyqdQuotationBaseAccessoryModel>(modelBuilder, "T_ZYQD_Quotation_Base_Accessory");
        ConfigureQuotationBase<ZyqdQuotationBaseCbModel>(modelBuilder, "T_ZYQD_Quotation_Base_CB");
        ConfigureQuotationBase<ZyqdQuotationBasePrModel>(modelBuilder, "T_ZYQD_Quotation_Base_PR");

        modelBuilder.Entity<ZyqdQuotationMainModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Quotation_main");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DATE).HasColumnName("DATE");
            entity.HasIndex(e => e.guid).IsUnique();
        });

        modelBuilder.Entity<ZyqdQuotationDetailModel>(entity =>
        {
            entity.ToTable("T_ZYQD_Quotation_detail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Qty).HasPrecision(18, 4);
            entity.Property(e => e.Price).HasPrecision(18, 4);
            entity.Property(e => e.Discount).HasPrecision(18, 4);
            entity.Property(e => e.Amount).HasPrecision(18, 4);
            entity.HasIndex(e => e.main_guid);
        });
    }

    private static void ConfigureQuotationBase<T>(ModelBuilder modelBuilder, string tableName)
        where T : QuotationBaseEntity
    {
        modelBuilder.Entity<T>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });
    }
}
