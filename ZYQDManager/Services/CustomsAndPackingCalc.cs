using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

/// <summary>
/// 装箱单行级自动计算（对齐老系统 qty_unitchange1 / updateSummaryData）。
/// </summary>
public static class CustomsAndPackingCalc
{
    public const double KgPerPallet = 15;
    public const string MergeRolePrimary = "拼托主";
    public const string MergeRoleMember = "拼托";

    public static bool IsMergePrimary(string? var8)
        => string.Equals(var8, MergeRolePrimary, StringComparison.Ordinal);

    public static bool IsMergeMember(string? var8)
        => string.Equals(var8, MergeRoleMember, StringComparison.Ordinal);

    /// <summary>拼托主且无整托、无尾托时，由本行承担拼成的那 1 托。</summary>
    public static bool NeedsSharedPalletSlot(CustomsAndPackingLineDto line)
        => IsMergePrimary(line.var8)
           && line.Pallet_qty <= 0
           && string.IsNullOrWhiteSpace(line.var2);

    /// <summary>
    /// 按数量/打包/打托/尺寸重算箱数、尾箱、整托、不足托、托重、净毛重合计、体积。
    /// </summary>
    public static void RecalcLine(CustomsAndPackingLineDto line)
    {
        var qty = line.qty;
        var packing = ParsePositive(line.Packing_method, 1);
        var palletizing = ParsePositive(line.palletizing_method, 1);
        var hasTailPallet = !string.IsNullOrWhiteSpace(line.var2);
        var mergeMember = IsMergeMember(line.var8);

        if (packing > 0)
        {
            line.box_qty = Math.Ceiling(qty / packing);
            var rem = qty % packing;
            line.var1 = rem > 0 ? FormatNum(rem) : "0";
            line.total_Net_weight = Round2(qty / packing * line.Net_weight);
        }

        if (palletizing > 0)
        {
            line.Pallet_qty = Math.Floor(line.box_qty / palletizing);
            var remBox = line.box_qty % palletizing;
            line.var3 = remBox > 0 ? FormatNum(remBox) : "0";
        }

        var palletWeight = line.Pallet_qty * KgPerPallet;
        if (!mergeMember)
        {
            if (hasTailPallet)
                palletWeight += KgPerPallet;
            else if (NeedsSharedPalletSlot(line))
                palletWeight += KgPerPallet;
        }
        line.Pallet_weight = palletWeight;

        if (packing > 0)
            line.total_gross_weight = Round2(qty / packing * line.gross_weight + palletWeight);

        line.volume = RoundCeil2(CalcVolume(
            line.Pallet_qty,
            mergeMember ? null : line.Pallet_Size,
            mergeMember ? null : line.var2,
            line.Carton_Size,
            line.emit,
            NeedsSharedPalletSlot(line)));
    }

    public static void RecalcLine(ZyqdCustomsAndPackingModel row)
    {
        var dto = CustomsAndPackingService.ToLineDto(row, 0);
        RecalcLine(dto);
        ApplyDtoToModel(dto, row);
    }

    public static void RecalcAllLines(IList<CustomsAndPackingLineDto> lines)
    {
        foreach (var line in lines)
            RecalcLine(line);
    }

    public static void ApplySummary(ZyqdCustomsAndPackingModel header, IReadOnlyList<CustomsAndPackingLineDto> lines)
    {
        header.all_qty = lines.Sum(x => x.qty);
        header.all_box_qty = lines.Sum(x => x.box_qty);
        // 整托 + 尾托 + 拼托主承担的那 1 托（挂靠行不计）
        header.all_Pallet_qty = lines.Sum(DisplayPalletQty);
        header.all_total_Net_weight = Round2(lines.Sum(x => x.total_Net_weight));
        header.all_total_gross_weight = Round2(lines.Sum(x => x.total_gross_weight));
        header.all_Net_weight = header.all_total_Net_weight;
        header.all_gross_weight = header.all_total_gross_weight;
        header.all_volume = Round2(lines.Sum(x => x.volume));
        header.all_emit = FormatNum(lines.Sum(ParseEmit));
        header.var5 = FormatNum(lines.Sum(x => ParseNum(x.var3)));
        header.all_Pallet_weight = Round2(lines.Sum(x => x.Pallet_weight));
    }

    public static CustomsAndPackingLineDto CloneForSplit(CustomsAndPackingLineDto src, int newTempId, double splitQty)
    {
        var clone = new CustomsAndPackingLineDto
        {
            TempId = newTempId,
            materialNo = src.materialNo,
            materialName = src.materialName,
            materialType = src.materialType,
            materialModel = src.materialModel,
            materiaSpecs = src.materiaSpecs,
            materiaPowered = src.materiaPowered,
            materiaColor = src.materiaColor,
            materiaCust = src.materiaCust,
            UNIT = src.UNIT,
            price = src.price,
            qty = splitQty,
            currency_unit = src.currency_unit,
            Packing_method = src.Packing_method,
            Packing_method_unit = src.Packing_method_unit,
            Carton_Size = src.Carton_Size,
            Pallet_Size = src.Pallet_Size,
            palletizing_method = src.palletizing_method,
            palletizing_method_unit = src.palletizing_method_unit,
            Net_weight = src.Net_weight,
            gross_weight = src.gross_weight,
            var2 = src.var2,
            var4 = src.var4,
            Pallet_code = null,
            var8 = null,
            emit = src.emit,
            packing_unit = src.packing_unit,
            sample_type = src.sample_type,
            SAP_order_NO = src.SAP_order_NO,
            Client_poNo = src.Client_poNo,
            PI_Material_Name = src.PI_Material_Name,
            var7 = "拆分行",
            SplitFromTempId = src.TempId,
            Customer_outer_box_logo = src.Customer_outer_box_logo,
            Tuo_Mark = src.Tuo_Mark,
            Tuo_Mark_url = src.Tuo_Mark_url,
            Box_label = src.Box_label,
            Box_label_url = src.Box_label_url,
            Box_label2 = src.Box_label2,
            Box_label_url2 = src.Box_label_url2,
            nameplate_url = src.nameplate_url,
            nameplate_url2 = src.nameplate_url2,
            Customer_destination_country = src.Customer_destination_country,
        };
        RecalcLine(clone);
        return clone;
    }

    public static double DisplayPalletQty(CustomsAndPackingLineDto line)
    {
        if (IsMergeMember(line.var8))
            return 0;
        var n = line.Pallet_qty + (string.IsNullOrWhiteSpace(line.var2) ? 0 : 1);
        if (n <= 0 && NeedsSharedPalletSlot(line))
            return 1;
        return n;
    }

    private static void ApplyDtoToModel(CustomsAndPackingLineDto dto, ZyqdCustomsAndPackingModel row)
    {
        row.box_qty = dto.box_qty;
        row.var1 = dto.var1;
        row.Pallet_qty = dto.Pallet_qty;
        row.var3 = dto.var3;
        row.Pallet_weight = dto.Pallet_weight;
        row.total_Net_weight = dto.total_Net_weight;
        row.total_gross_weight = dto.total_gross_weight;
        row.volume = dto.volume;
    }

    private static double CalcVolume(
        double palletQty,
        string? palletSize,
        string? tailSize,
        string? cartonSize,
        string? emit,
        bool sharedPalletSlot = false)
    {
        var hasTail = !string.IsNullOrWhiteSpace(tailSize);
        if (palletQty <= 0 && !hasTail && !sharedPalletSlot)
            return 0;

        double volume = 0;
        var fullQty = palletQty > 0 ? palletQty : (sharedPalletSlot ? 1 : 0);
        if (TryParseSize(palletSize, out var pw, out var ph, out var pd) && fullQty > 0)
            volume += fullQty * pw * ph * pd / 1_000_000_000d;

        if (hasTail && TryParseSize(tailSize, out var tw, out var th, out var td))
            volume += tw * th * td / 1_000_000_000d;

        var emitN = ParseEmit(emit);
        if (emitN > 0 && TryParseSize(cartonSize, out var cw, out var ch, out var cd))
            volume += emitN * cw * ch * cd / 1_000_000_000d;

        return volume;
    }

    private static bool TryParseSize(string? size, out double a, out double b, out double c)
    {
        a = b = c = 0;
        var parts = (size ?? "").Split('*', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;
        return double.TryParse(parts[0], out a)
               && double.TryParse(parts[1], out b)
               && double.TryParse(parts[2], out c);
    }

    private static double ParsePositive(string? text, double fallback)
    {
        if (!double.TryParse((text ?? "").Trim(), out var n) || n <= 0)
            return fallback;
        return n;
    }

    private static double ParseNum(string? text)
        => double.TryParse((text ?? "").Trim(), out var n) ? n : 0;

    private static double ParseEmit(CustomsAndPackingLineDto line) => ParseEmit(line.emit);

    private static double ParseEmit(string? text) => ParseNum(text);

    private static double Round2(double n) => Math.Round(n, 2, MidpointRounding.AwayFromZero);

    private static double RoundCeil2(double n) => Math.Ceiling(n * 100) / 100;

    private static string FormatNum(double n)
        => n.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
