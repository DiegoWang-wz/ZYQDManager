using MudBlazor;

namespace ZYQDManager.Utilities;

/// <summary>
/// 编辑页工具栏按钮约定：保存 Primary；导出 Excel / 预览 PDF / 导出 PDF 同为 Dark，与保存区分。
/// </summary>
public static class EditToolbarStyle
{
    public static readonly Variant Variant = Variant.Filled;

    public static readonly Color SaveColor = Color.Primary;
    public static readonly Color ExportColor = Color.Dark;

    public static readonly string SaveIcon = Icons.Material.Filled.Save;
    public static readonly string ExcelIcon = Icons.Material.Filled.TableView;
    public static readonly string PdfIcon = Icons.Material.Filled.PictureAsPdf;
    public static readonly string PreviewIcon = Icons.Material.Filled.Visibility;
}
