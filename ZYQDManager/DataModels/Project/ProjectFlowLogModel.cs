namespace ZYQDManager.DataModels;

/// <summary>
/// 电机项目流程日志 T_ZYQD_ProjectFlowLog。
///
/// 定位：只增不改的完整流程流水，供「流程记录」表和追溯使用。
/// 每走完一步（或每次退回）就落一行，展示时直接按版本 + 序号读出来即可，
/// 不再从 ApprovalFlows 倒推 —— 倒推容易把操作人、相关人认错。
///
/// 与其它表的职责边界：
///   - ProjectVersion 记「现在走到哪、在等谁」，只管当前状态；
///   - ApprovalFlow   记「退回原因、审批备注」这类特殊事项，供页面回显；
///   - 本表           记「整条流程发生过哪些步骤」，只管历史留痕。
///
/// 一条记录对应一个业务节点（12 步之一），不是一次按钮动作：
/// 比如「提交立项单」会同时产生「创建立项单」和「指派研发工程师」两行。
/// 退回重走时继续往后追加，序号按版本内递增，绝不覆盖旧行。
/// </summary>
public class ProjectFlowLogModel
{
    public int Id { get; set; }

    public string ProjectGuid { get; set; } = "";

    /// <summary>版本号（V1、V2…），与 ProjectVersion.VersionNo 对应。</summary>
    public string VersionNo { get; set; } = "";

    /// <summary>版本内顺序号，从 1 开始递增。同版本内退回重走的新行继续往后排。</summary>
    public int Seq { get; set; }

    /// <summary>环节号（1..12），对应 <see cref="Utilities.MotorNode"/> 的 12 步。</summary>
    public int Node { get; set; }

    /// <summary>环节名（写死文案，如「创建立项单」），避免展示时再靠节点号翻译。</summary>
    public string NodeName { get; set; } = "";

    /// <summary>操作人工号（这一步是谁做的）。</summary>
    public string? OperatorCode { get; set; }

    /// <summary>相关人工号（这一步流转给了谁），无相关人时为空。</summary>
    public string? RelatedCode { get; set; }

    /// <summary>状态：成功 / 退回 / 进行中，见 <see cref="Utilities.MotorFlow.LogSuccess"/> 等。</summary>
    public string State { get; set; } = "";

    /// <summary>备注，退回时存原因。</summary>
    public string? Remark { get; set; }

    /// <summary>发生时间。编写类环节取最后一次保存时间。</summary>
    public DateTime ActionTime { get; set; }

    /// <summary>本次改版的修改原因（版本级，冗余存一份方便查询）。</summary>
    public string? ChangeReason { get; set; }

    public DateTime CreateTime { get; set; }
}
