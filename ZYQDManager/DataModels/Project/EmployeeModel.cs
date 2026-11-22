namespace ZYQDManager.DataModels;

/// <summary>
/// 员工基础信息 T_ZYQD_Employee。
///
/// <para>
/// 数据来源：从 CommonApi.dbo.CentralUsers 同步过来的快照，
/// 职位取 ZYQDManager 系统下的角色码，部门取 UserInformation.ZYQDManager.department。
/// 业务表只存工号，姓名/部门/职位通过 join 这张表获取。
/// </para>
/// </summary>
public class EmployeeModel
{
    public string EmpNo { get; set; } = "";
    public string? EmpName { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastSyncTime { get; set; }
}
