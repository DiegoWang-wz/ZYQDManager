using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZYQDManager.DataModels;

[Table("ZYQD_UserCustomerAssign")]
public class ZyqdUserCustomerAssignModel
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("EmpNo")]
    [MaxLength(20)]
    public string EmpNo { get; set; } = "";

    [Column("CustomerCode")]
    [MaxLength(50)]
    public string CustomerCode { get; set; } = "";

    [Column("CustomerName")]
    [MaxLength(200)]
    public string? CustomerName { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; } = DateTime.Now;
}
