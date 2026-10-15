using MudBlazor;

namespace ZYQDManager.Utilities;

/// <summary>门户首页入口定义（按当前用户权限过滤后展示）。</summary>
public sealed class PortalEntry
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Href { get; init; }
    public required string Icon { get; init; }
    public string Color { get; init; } = "#1565c0";
}

public static class PortalEntries
{
    public static IReadOnlyList<PortalEntry> FilterForUser(
        bool canSeeBusinessModules,
        bool canViewInvoiceKp,
        bool canManageUsers)
    {
        var list = new List<PortalEntry>();

        if (canSeeBusinessModules)
        {
            list.Add(new PortalEntry
            {
                Key = "project",
                Title = "项目管理",
                Href = "/project",
                Icon = Icons.Material.Filled.Assignment,
                Color = "#0277bd"
            });
            list.Add(new PortalEntry
            {
                Key = "contract-pi",
                Title = "客户PI",
                Href = "/contract?type=" + Uri.EscapeDataString("客户PI"),
                Icon = Icons.Material.Filled.Description,
                Color = "#1976d2"
            });
            list.Add(new PortalEntry
            {
                Key = "contract-ci",
                Title = "客户CI",
                Href = "/contract?type=" + Uri.EscapeDataString("客户CI"),
                Icon = Icons.Material.Filled.ReceiptLong,
                Color = "#7b1fa2"
            });
            list.Add(new PortalEntry
            {
                Key = "contract-bg",
                Title = "报关PI",
                Href = "/contract?type=" + Uri.EscapeDataString("报关PI"),
                Icon = Icons.Material.Filled.LocalShipping,
                Color = "#ed6c02"
            });
            list.Add(new PortalEntry
            {
                Key = "contract-pk",
                Title = "装箱单",
                Href = "/contract-pk",
                Icon = Icons.Material.Filled.Inventory2,
                Color = "#2e7d32"
            });
            list.Add(new PortalEntry
            {
                Key = "contract-base",
                Title = "合同基础数据",
                Href = "/contract-base",
                Icon = Icons.Material.Filled.Storage,
                Color = "#00838f"
            });
            list.Add(new PortalEntry
            {
                Key = "quotation",
                Title = "报价列表",
                Href = "/quotation",
                Icon = Icons.Material.Filled.RequestQuote,
                Color = "#c62828"
            });
            list.Add(new PortalEntry
            {
                Key = "quotation-base",
                Title = "报价基础数据",
                Href = "/quotation-base",
                Icon = Icons.Material.Filled.TableChart,
                Color = "#ad1457"
            });
            if (canViewInvoiceKp)
            {
                list.Add(new PortalEntry
                {
                    Key = "invoice-kp",
                    Title = "开票管理",
                    Href = "/invoice-kp",
                    Icon = Icons.Material.Filled.Receipt,
                    Color = "#6a1b9a"
                });
            }
        }

        list.Add(new PortalEntry
        {
            Key = "sir",
            Title = "检测报告管理",
            Href = "/sir",
            Icon = Icons.Material.Filled.FactCheck,
            Color = "#00695c"
        });

        list.Add(new PortalEntry
        {
            Key = "profile",
            Title = "个人信息",
            Href = "/Profile",
            Icon = Icons.Material.Filled.Person,
            Color = "#455a64"
        });

        if (canManageUsers)
        {
            list.Add(new PortalEntry
            {
                Key = "personnel",
                Title = "人员管理",
                Href = "/personnel",
                Icon = Icons.Material.Filled.ManageAccounts,
                Color = "#37474f"
            });
        }

        return list;
    }
}
