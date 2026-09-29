using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZYQDManager.DataModels;

[Table("ZYQD_UserProfile")]
public class ZyqdUserProfileModel
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("EmpNo")]
    [MaxLength(20)]
    public string EmpNo { get; set; } = "";

    [Column("Department")]
    [MaxLength(100)]
    public string? Department { get; set; }

    [Column("Email")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [Column("Phone")]
    [MaxLength(50)]
    public string? Phone { get; set; }
}
