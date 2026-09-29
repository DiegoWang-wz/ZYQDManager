using Microsoft.EntityFrameworkCore;

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
    }
}
