namespace ZYQDManager.DataDtos;

/// <summary>
/// 研发工程师名录项：数据源是 JcCommonApi 的账号库（CommonApi.dbo.CentralUsers），
/// 角色取 SystemPermissions 里 ZYQDManager 块的 Role，部门取 UserInformation 里同块的 department。
/// </summary>
public sealed class RdEngineerOption
{
    public string EmpNo { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>账号中心里维护的部门原文（可能为空）。</summary>
    public string Department { get; set; } = "";

    /// <summary>下拉里显示的文字：姓名（工号）。</summary>
    public string Display => string.IsNullOrWhiteSpace(Name) ? EmpNo : $"{Name}（{EmpNo}）";
}

/// <summary>提交时选中的部门 + 人员（承认书提交会同时选研发经理与终审人）。</summary>
public sealed class MotorSubmitPick
{
    public string Department { get; set; } = "";
    public string EmpNo { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>第二个人：承认书提交时的终审人（销售经理），其它场景为空。</summary>
    public string SecondEmpNo { get; set; } = "";
    public string SecondName { get; set; } = "";

    /// <summary>提交时在对话框里填写的备注，写入流程记录。</summary>
    public string? Comment { get; set; }
}

/// <summary>退回对话框里的一个可选项：Node 是目标流程节点（1~当前节点-1）。</summary>
public sealed record MotorRejectOption(int Node, string Label, string Assignee = "");

/// <summary>退回对话框的返回值：退回到哪个节点 + 退回原因。</summary>
public sealed class MotorRejectPick
{
    public int TargetNode { get; set; }
    public string? Comment { get; set; }
}
