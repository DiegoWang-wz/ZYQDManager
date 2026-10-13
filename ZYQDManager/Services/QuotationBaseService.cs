using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.DataModels.Quotation;

namespace ZYQDManager.Services;

public class QuotationBaseService
{
    private readonly SqlServerDbContext _db;

    public QuotationBaseService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<(List<QuotationBaseListItemDto> Rows, int Total)> QueryPageAsync(
        QuotationBaseQueryDto query, int skip, int take, CancellationToken ct = default)
    {
        var type = QuotationBaseTypes.Normalize(query.ItemType);
        if (take <= 0) take = 20;
        if (skip < 0) skip = 0;

        return type switch
        {
            QuotationBaseTypes.Remote => await PageAsync(
                Filter(_db.QuotationBaseRemotes.AsNoTracking(), query),
                query, skip, take, MapRemote, ct),
            QuotationBaseTypes.Accessory => await PageAsync(
                Filter(_db.QuotationBaseAccessories.AsNoTracking(), query),
                query, skip, take, MapAccessory, ct),
            QuotationBaseTypes.CB => await PageAsync(
                Filter(_db.QuotationBaseCbs.AsNoTracking(), query),
                query, skip, take, MapCb, ct),
            QuotationBaseTypes.PR => await PageAsync(
                Filter(_db.QuotationBasePrs.AsNoTracking(), query),
                query, skip, take, MapPr, ct),
            _ => await PageAsync(
                Filter(_db.QuotationBaseMotors.AsNoTracking(), query),
                query, skip, take, MapMotor, ct),
        };
    }

    /// <summary>导出用：按筛选/勾选拉全量（不分页）。</summary>
    public async Task<List<QuotationBaseListItemDto>> QueryAllAsync(
        QuotationBaseQueryDto query,
        IReadOnlyCollection<int>? selectedIds = null,
        CancellationToken ct = default)
    {
        var type = QuotationBaseTypes.Normalize(query.ItemType);
        var ids = selectedIds?.Where(x => x > 0).Distinct().ToList();

        return type switch
        {
            QuotationBaseTypes.Remote => await AllAsync(
                ApplyIds(Filter(_db.QuotationBaseRemotes.AsNoTracking(), query), ids),
                query, MapRemote, ct),
            QuotationBaseTypes.Accessory => await AllAsync(
                ApplyIds(Filter(_db.QuotationBaseAccessories.AsNoTracking(), query), ids),
                query, MapAccessory, ct),
            QuotationBaseTypes.CB => await AllAsync(
                ApplyIds(Filter(_db.QuotationBaseCbs.AsNoTracking(), query), ids),
                query, MapCb, ct),
            QuotationBaseTypes.PR => await AllAsync(
                ApplyIds(Filter(_db.QuotationBasePrs.AsNoTracking(), query), ids),
                query, MapPr, ct),
            _ => await AllAsync(
                ApplyIds(Filter(_db.QuotationBaseMotors.AsNoTracking(), query), ids),
                query, MapMotor, ct),
        };
    }

    private static IQueryable<T> ApplyIds<T>(IQueryable<T> q, List<int>? ids)
        where T : QuotationBaseEntity
        => ids is { Count: > 0 } ? q.Where(x => ids.Contains(x.Id)) : q;

    private static async Task<List<QuotationBaseListItemDto>> AllAsync<T>(
        IQueryable<T> q,
        QuotationBaseQueryDto query,
        Func<T, QuotationBaseListItemDto> map,
        CancellationToken ct)
        where T : QuotationBaseEntity
    {
        q = Sort(q, query);
        var entities = await q.ToListAsync(ct);
        return entities.Select(map).ToList();
    }

    public async Task<(bool Ok, string? Message)> SoftDeleteAsync(
        string itemType, int id, string? person, CancellationToken ct = default)
    {
        var type = QuotationBaseTypes.Normalize(itemType);
        var now = DateTime.Now;
        person ??= "";

        bool touched = type switch
        {
            QuotationBaseTypes.Remote => await SoftDeleteEntityAsync(_db.QuotationBaseRemotes, id, person, now, ct),
            QuotationBaseTypes.Accessory => await SoftDeleteEntityAsync(_db.QuotationBaseAccessories, id, person, now, ct),
            QuotationBaseTypes.CB => await SoftDeleteEntityAsync(_db.QuotationBaseCbs, id, person, now, ct),
            QuotationBaseTypes.PR => await SoftDeleteEntityAsync(_db.QuotationBasePrs, id, person, now, ct),
            _ => await SoftDeleteEntityAsync(_db.QuotationBaseMotors, id, person, now, ct),
        };

        if (!touched)
            return (false, "记录不存在或已删除");

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Message)> SoftDeleteManyAsync(
        string itemType, IEnumerable<int> ids, string? person, CancellationToken ct = default)
    {
        var idList = ids.Distinct().Where(x => x > 0).ToList();
        if (idList.Count == 0)
            return (false, "未选择记录");

        var type = QuotationBaseTypes.Normalize(itemType);
        var now = DateTime.Now;
        person ??= "";
        var okCount = 0;

        foreach (var id in idList)
        {
            var ok = type switch
            {
                QuotationBaseTypes.Remote => await SoftDeleteEntityAsync(_db.QuotationBaseRemotes, id, person, now, ct),
                QuotationBaseTypes.Accessory => await SoftDeleteEntityAsync(_db.QuotationBaseAccessories, id, person, now, ct),
                QuotationBaseTypes.CB => await SoftDeleteEntityAsync(_db.QuotationBaseCbs, id, person, now, ct),
                QuotationBaseTypes.PR => await SoftDeleteEntityAsync(_db.QuotationBasePrs, id, person, now, ct),
                _ => await SoftDeleteEntityAsync(_db.QuotationBaseMotors, id, person, now, ct),
            };
            if (ok) okCount++;
        }

        if (okCount == 0)
            return (false, "没有可删除的记录");

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// 批量把相同字段值写到勾选行（对齐老系统 BatchUpdate：fields 为 field→value）。
    /// </summary>
    public async Task<(bool Ok, string? Message)> BatchUpdateAsync(
        string itemType,
        IEnumerable<int> ids,
        IReadOnlyDictionary<string, string?> fields,
        string? empNo,
        CancellationToken ct = default)
    {
        var idList = ids.Distinct().Where(x => x > 0).ToList();
        if (idList.Count == 0)
            return (false, "未选择记录");

        var allowed = AllowedBatchFields(itemType);
        var patch = fields
            .Where(kv => allowed.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value ?? "", StringComparer.OrdinalIgnoreCase);
        if (patch.Count == 0)
            return (false, "没有需要保存的字段");

        var type = QuotationBaseTypes.Normalize(itemType);
        var now = DateTime.Now;
        empNo = (empNo ?? "").Trim();

        var updated = type switch
        {
            QuotationBaseTypes.Remote => await BatchPatchAsync(
                _db.QuotationBaseRemotes, idList, patch, empNo, now, ApplyRemoteBatch, ct),
            QuotationBaseTypes.Accessory => await BatchPatchAsync(
                _db.QuotationBaseAccessories, idList, patch, empNo, now, ApplyAccessoryBatch, ct),
            QuotationBaseTypes.CB => await BatchPatchAsync(
                _db.QuotationBaseCbs, idList, patch, empNo, now, ApplyCbBatch, ct),
            QuotationBaseTypes.PR => await BatchPatchAsync(
                _db.QuotationBasePrs, idList, patch, empNo, now, ApplyPrBatch, ct),
            _ => await BatchPatchAsync(
                _db.QuotationBaseMotors, idList, patch, empNo, now, ApplyMotorBatch, ct),
        };

        if (updated == 0)
            return (false, "没有可更新的记录");

        await _db.SaveChangesAsync(ct);
        return (true, $"已更新 {updated} 条记录");
    }

    public static HashSet<string> AllowedBatchFields(string? itemType)
    {
        var type = QuotationBaseTypes.Normalize(itemType);
        return type switch
        {
            QuotationBaseTypes.Remote => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Remote_controller_type", "Battery_Type", "Radio_Frequency",
                "Working_Temperature", "NoOfChannels", "Remark",
            },
            QuotationBaseTypes.Accessory => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "adaptor", "Remark",
            },
            QuotationBaseTypes.CB => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Rated_Voltage", "Size", "Rated_Power", "No_Of_Actuator",
                "Output_Control", "Sensor", "Protection_Degree", "Remark",
            },
            QuotationBaseTypes.PR => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Max_Loading", "Max_Stroke", "Protection_Degree2", "Noise", "Remark",
            },
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Rated_torque", "Rated_speed", "Power_supply", "Rated_Power",
                "Rated_Current", "Noise_dBA", "Motor_Length", "Tube_diameter",
                "Battery_Capacity", "Exclude_text", "Remark",
            },
        };
    }

    private static async Task<int> BatchPatchAsync<T>(
        DbSet<T> set,
        List<int> idList,
        Dictionary<string, string> patch,
        string empNo,
        DateTime now,
        Action<T, Dictionary<string, string>> apply,
        CancellationToken ct)
        where T : QuotationBaseEntity
    {
        var items = await set
            .Where(x => idList.Contains(x.Id) && (x.deleteSigh == null || x.deleteSigh == ""))
            .ToListAsync(ct);
        foreach (var item in items)
        {
            apply(item, patch);
            item.UpdateTime = now;
            item.UpdatePerson = empNo;
        }

        return items.Count;
    }

    private static void ApplyMotorBatch(ZyqdQuotationBaseMotorModel row, Dictionary<string, string> f)
    {
        if (f.TryGetValue("Rated_torque", out var v)) row.Rated_torque = v;
        if (f.TryGetValue("Rated_speed", out v)) row.Rated_speed = v;
        if (f.TryGetValue("Power_supply", out v)) row.Power_supply = v;
        if (f.TryGetValue("Rated_Power", out v)) row.Rated_Power = v;
        if (f.TryGetValue("Rated_Current", out v)) row.Rated_Current = v;
        if (f.TryGetValue("Noise_dBA", out v)) row.Noise_dBA = v;
        if (f.TryGetValue("Motor_Length", out v)) row.Motor_Length = v;
        if (f.TryGetValue("Tube_diameter", out v)) row.Tube_diameter = v;
        if (f.TryGetValue("Battery_Capacity", out v)) row.Battery_Capacity = v;
        if (f.TryGetValue("Exclude_text", out v)) row.Exclude_text = v;
        if (f.TryGetValue("Remark", out v)) row.Remark = v;
    }

    private static void ApplyRemoteBatch(ZyqdQuotationBaseRemoteModel row, Dictionary<string, string> f)
    {
        if (f.TryGetValue("Remote_controller_type", out var v)) row.Remote_controller_type = v;
        if (f.TryGetValue("Battery_Type", out v)) row.Battery_Type = v;
        if (f.TryGetValue("Radio_Frequency", out v)) row.Radio_Frequency = v;
        if (f.TryGetValue("Working_Temperature", out v)) row.Working_Temperature = v;
        if (f.TryGetValue("NoOfChannels", out v)) row.NoOfChannels = v;
        if (f.TryGetValue("Remark", out v)) row.Remark = v;
    }

    private static void ApplyAccessoryBatch(ZyqdQuotationBaseAccessoryModel row, Dictionary<string, string> f)
    {
        if (f.TryGetValue("adaptor", out var v)) row.adaptor = v;
        if (f.TryGetValue("Remark", out v)) row.Remark = v;
    }

    private static void ApplyCbBatch(ZyqdQuotationBaseCbModel row, Dictionary<string, string> f)
    {
        if (f.TryGetValue("Rated_Voltage", out var v)) row.Rated_Voltage = v;
        if (f.TryGetValue("Size", out v)) row.Size = v;
        if (f.TryGetValue("Rated_Power", out v)) row.Rated_Power = v;
        if (f.TryGetValue("No_Of_Actuator", out v)) row.No_Of_Actuator = v;
        if (f.TryGetValue("Output_Control", out v)) row.Output_Control = v;
        if (f.TryGetValue("Sensor", out v)) row.Sensor = v;
        if (f.TryGetValue("Protection_Degree", out v)) row.Protection_Degree = v;
        if (f.TryGetValue("Remark", out v)) row.Remark = v;
    }

    private static void ApplyPrBatch(ZyqdQuotationBasePrModel row, Dictionary<string, string> f)
    {
        if (f.TryGetValue("Max_Loading", out var v)) row.Max_Loading = v;
        if (f.TryGetValue("Max_Stroke", out v)) row.Max_Stroke = v;
        if (f.TryGetValue("Protection_Degree2", out v)) row.Protection_Degree2 = v;
        if (f.TryGetValue("Noise", out v)) row.Noise = v;
        if (f.TryGetValue("Remark", out v)) row.Remark = v;
    }

    private static async Task<bool> SoftDeleteEntityAsync<T>(
        DbSet<T> set, int id, string person, DateTime now, CancellationToken ct)
        where T : QuotationBaseEntity
    {
        var row = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return false;
        if (!string.IsNullOrWhiteSpace(row.deleteSigh))
            return false;

        row.deleteSigh = "X";
        row.deleteTime = now;
        row.deletePerson = person;
        row.UpdateTime = now;
        row.UpdatePerson = person;
        return true;
    }

    private static async Task<(List<QuotationBaseListItemDto> Rows, int Total)> PageAsync<T>(
        IQueryable<T> q,
        QuotationBaseQueryDto query,
        int skip,
        int take,
        Func<T, QuotationBaseListItemDto> map,
        CancellationToken ct)
        where T : QuotationBaseEntity
    {
        q = Sort(q, query);
        var total = await q.CountAsync(ct);
        var entities = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (entities.Select(map).ToList(), total);
    }

    private static IQueryable<T> Filter<T>(IQueryable<T> q, QuotationBaseQueryDto query)
        where T : QuotationBaseEntity
    {
        q = q.Where(x => x.deleteSigh == null || x.deleteSigh == "");

        var kw = (query.Keyword ?? "").Trim();
        if (kw.Length == 0)
            return q;

        var col = (query.FilterColumn ?? "Part_No").Trim();
        // 规格字段按具体类型在下方分支；公共字段统一处理
        if (col is "CreatePerson" or "UpdatePerson" or "Remark" or "Part_No")
        {
            return col switch
            {
                "CreatePerson" => q.Where(x => x.CreatePerson != null && x.CreatePerson.Contains(kw)),
                "UpdatePerson" => q.Where(x => x.UpdatePerson != null && x.UpdatePerson.Contains(kw)),
                "Remark" => q.Where(x => x.Remark != null && x.Remark.Contains(kw)),
                _ => q.Where(x => x.Part_No != null && x.Part_No.Contains(kw)),
            };
        }

        if (q is IQueryable<ZyqdQuotationBaseMotorModel> mq)
            return (IQueryable<T>)(object)FilterMotor(mq, col, kw);
        if (q is IQueryable<ZyqdQuotationBaseRemoteModel> rq)
            return (IQueryable<T>)(object)FilterRemote(rq, col, kw);
        if (q is IQueryable<ZyqdQuotationBaseAccessoryModel> aq)
            return (IQueryable<T>)(object)FilterAccessory(aq, col, kw);
        if (q is IQueryable<ZyqdQuotationBaseCbModel> cq)
            return (IQueryable<T>)(object)FilterCb(cq, col, kw);
        if (q is IQueryable<ZyqdQuotationBasePrModel> pq)
            return (IQueryable<T>)(object)FilterPr(pq, col, kw);

        return q.Where(x => x.Part_No != null && x.Part_No.Contains(kw));
    }

    private static IQueryable<ZyqdQuotationBaseMotorModel> FilterMotor(
        IQueryable<ZyqdQuotationBaseMotorModel> q, string col, string kw) => col switch
    {
        "Rated_torque" => q.Where(x => x.Rated_torque != null && x.Rated_torque.Contains(kw)),
        "Rated_speed" => q.Where(x => x.Rated_speed != null && x.Rated_speed.Contains(kw)),
        "Power_supply" => q.Where(x => x.Power_supply != null && x.Power_supply.Contains(kw)),
        "Rated_Power" => q.Where(x => x.Rated_Power != null && x.Rated_Power.Contains(kw)),
        "Rated_Current" => q.Where(x => x.Rated_Current != null && x.Rated_Current.Contains(kw)),
        _ => q.Where(x => x.Part_No != null && x.Part_No.Contains(kw)),
    };

    private static IQueryable<ZyqdQuotationBaseRemoteModel> FilterRemote(
        IQueryable<ZyqdQuotationBaseRemoteModel> q, string col, string kw) => col switch
    {
        "Remote_controller_type" => q.Where(x => x.Remote_controller_type != null && x.Remote_controller_type.Contains(kw)),
        "Battery_Type" => q.Where(x => x.Battery_Type != null && x.Battery_Type.Contains(kw)),
        "Radio_Frequency" => q.Where(x => x.Radio_Frequency != null && x.Radio_Frequency.Contains(kw)),
        _ => q.Where(x => x.Part_No != null && x.Part_No.Contains(kw)),
    };

    private static IQueryable<ZyqdQuotationBaseAccessoryModel> FilterAccessory(
        IQueryable<ZyqdQuotationBaseAccessoryModel> q, string col, string kw) => col switch
    {
        "adaptor" => q.Where(x => x.adaptor != null && x.adaptor.Contains(kw)),
        _ => q.Where(x => x.Part_No != null && x.Part_No.Contains(kw)),
    };

    private static IQueryable<ZyqdQuotationBaseCbModel> FilterCb(
        IQueryable<ZyqdQuotationBaseCbModel> q, string col, string kw) => col switch
    {
        "Rated_Voltage" => q.Where(x => x.Rated_Voltage != null && x.Rated_Voltage.Contains(kw)),
        "Size" => q.Where(x => x.Size != null && x.Size.Contains(kw)),
        "Rated_Power" => q.Where(x => x.Rated_Power != null && x.Rated_Power.Contains(kw)),
        _ => q.Where(x => x.Part_No != null && x.Part_No.Contains(kw)),
    };

    private static IQueryable<ZyqdQuotationBasePrModel> FilterPr(
        IQueryable<ZyqdQuotationBasePrModel> q, string col, string kw) => col switch
    {
        "Max_Loading" => q.Where(x => x.Max_Loading != null && x.Max_Loading.Contains(kw)),
        "Max_Stroke" => q.Where(x => x.Max_Stroke != null && x.Max_Stroke.Contains(kw)),
        "Noise" => q.Where(x => x.Noise != null && x.Noise.Contains(kw)),
        _ => q.Where(x => x.Part_No != null && x.Part_No.Contains(kw)),
    };

    private static IQueryable<T> Sort<T>(IQueryable<T> q, QuotationBaseQueryDto query)
        where T : QuotationBaseEntity
    {
        var label = (query.SortLabel ?? "Id").Trim();
        var desc = query.SortDescending;

        // 公共排序
        if (label is "Id" or "Part_No" or "CreateTime" or "UpdateTime" or "CreatePerson" or "UpdatePerson" or "Remark")
        {
            return label switch
            {
                "Part_No" => desc ? q.OrderByDescending(x => x.Part_No) : q.OrderBy(x => x.Part_No),
                "CreateTime" => desc ? q.OrderByDescending(x => x.CreateTime) : q.OrderBy(x => x.CreateTime),
                "UpdateTime" => desc ? q.OrderByDescending(x => x.UpdateTime) : q.OrderBy(x => x.UpdateTime),
                "CreatePerson" => desc ? q.OrderByDescending(x => x.CreatePerson) : q.OrderBy(x => x.CreatePerson),
                "UpdatePerson" => desc ? q.OrderByDescending(x => x.UpdatePerson) : q.OrderBy(x => x.UpdatePerson),
                "Remark" => desc ? q.OrderByDescending(x => x.Remark) : q.OrderBy(x => x.Remark),
                _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
            };
        }

        if (q is IQueryable<ZyqdQuotationBaseMotorModel> mq)
            return (IQueryable<T>)(object)SortMotor(mq, label, desc);
        if (q is IQueryable<ZyqdQuotationBaseRemoteModel> rq)
            return (IQueryable<T>)(object)SortRemote(rq, label, desc);
        if (q is IQueryable<ZyqdQuotationBaseAccessoryModel> aq)
            return (IQueryable<T>)(object)SortAccessory(aq, label, desc);
        if (q is IQueryable<ZyqdQuotationBaseCbModel> cq)
            return (IQueryable<T>)(object)SortCb(cq, label, desc);
        if (q is IQueryable<ZyqdQuotationBasePrModel> pq)
            return (IQueryable<T>)(object)SortPr(pq, label, desc);

        return desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id);
    }

    private static IQueryable<ZyqdQuotationBaseMotorModel> SortMotor(
        IQueryable<ZyqdQuotationBaseMotorModel> q, string label, bool desc) => label switch
    {
        "Rated_torque" => desc ? q.OrderByDescending(x => x.Rated_torque) : q.OrderBy(x => x.Rated_torque),
        "Rated_speed" => desc ? q.OrderByDescending(x => x.Rated_speed) : q.OrderBy(x => x.Rated_speed),
        "Power_supply" => desc ? q.OrderByDescending(x => x.Power_supply) : q.OrderBy(x => x.Power_supply),
        "Rated_Power" => desc ? q.OrderByDescending(x => x.Rated_Power) : q.OrderBy(x => x.Rated_Power),
        "Rated_Current" => desc ? q.OrderByDescending(x => x.Rated_Current) : q.OrderBy(x => x.Rated_Current),
        "Noise_dBA" => desc ? q.OrderByDescending(x => x.Noise_dBA) : q.OrderBy(x => x.Noise_dBA),
        "Motor_Length" => desc ? q.OrderByDescending(x => x.Motor_Length) : q.OrderBy(x => x.Motor_Length),
        "Tube_diameter" => desc ? q.OrderByDescending(x => x.Tube_diameter) : q.OrderBy(x => x.Tube_diameter),
        "Battery_Capacity" => desc ? q.OrderByDescending(x => x.Battery_Capacity) : q.OrderBy(x => x.Battery_Capacity),
        "Exclude_text" => desc ? q.OrderByDescending(x => x.Exclude_text) : q.OrderBy(x => x.Exclude_text),
        _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
    };

    private static IQueryable<ZyqdQuotationBaseRemoteModel> SortRemote(
        IQueryable<ZyqdQuotationBaseRemoteModel> q, string label, bool desc) => label switch
    {
        "Remote_controller_type" => desc ? q.OrderByDescending(x => x.Remote_controller_type) : q.OrderBy(x => x.Remote_controller_type),
        "Battery_Type" => desc ? q.OrderByDescending(x => x.Battery_Type) : q.OrderBy(x => x.Battery_Type),
        "Radio_Frequency" => desc ? q.OrderByDescending(x => x.Radio_Frequency) : q.OrderBy(x => x.Radio_Frequency),
        "Working_Temperature" => desc ? q.OrderByDescending(x => x.Working_Temperature) : q.OrderBy(x => x.Working_Temperature),
        "NoOfChannels" => desc ? q.OrderByDescending(x => x.NoOfChannels) : q.OrderBy(x => x.NoOfChannels),
        _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
    };

    private static IQueryable<ZyqdQuotationBaseAccessoryModel> SortAccessory(
        IQueryable<ZyqdQuotationBaseAccessoryModel> q, string label, bool desc) => label switch
    {
        "adaptor" => desc ? q.OrderByDescending(x => x.adaptor) : q.OrderBy(x => x.adaptor),
        _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
    };

    private static IQueryable<ZyqdQuotationBaseCbModel> SortCb(
        IQueryable<ZyqdQuotationBaseCbModel> q, string label, bool desc) => label switch
    {
        "Rated_Voltage" => desc ? q.OrderByDescending(x => x.Rated_Voltage) : q.OrderBy(x => x.Rated_Voltage),
        "Size" => desc ? q.OrderByDescending(x => x.Size) : q.OrderBy(x => x.Size),
        "Rated_Power" => desc ? q.OrderByDescending(x => x.Rated_Power) : q.OrderBy(x => x.Rated_Power),
        "No_Of_Actuator" => desc ? q.OrderByDescending(x => x.No_Of_Actuator) : q.OrderBy(x => x.No_Of_Actuator),
        "Output_Control" => desc ? q.OrderByDescending(x => x.Output_Control) : q.OrderBy(x => x.Output_Control),
        "Sensor" => desc ? q.OrderByDescending(x => x.Sensor) : q.OrderBy(x => x.Sensor),
        "Protection_Degree" => desc ? q.OrderByDescending(x => x.Protection_Degree) : q.OrderBy(x => x.Protection_Degree),
        _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
    };

    private static IQueryable<ZyqdQuotationBasePrModel> SortPr(
        IQueryable<ZyqdQuotationBasePrModel> q, string label, bool desc) => label switch
    {
        "Max_Loading" => desc ? q.OrderByDescending(x => x.Max_Loading) : q.OrderBy(x => x.Max_Loading),
        "Max_Stroke" => desc ? q.OrderByDescending(x => x.Max_Stroke) : q.OrderBy(x => x.Max_Stroke),
        "Protection_Degree2" => desc ? q.OrderByDescending(x => x.Protection_Degree2) : q.OrderBy(x => x.Protection_Degree2),
        "Noise" => desc ? q.OrderByDescending(x => x.Noise) : q.OrderBy(x => x.Noise),
        _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
    };

    private static QuotationBaseListItemDto MapMotor(ZyqdQuotationBaseMotorModel x) => new()
    {
        Id = x.Id,
        ItemType = QuotationBaseTypes.Motor,
        Part_No = x.Part_No,
        Quotation_logo = x.Quotation_logo,
        CreateTime = x.CreateTime,
        UpdateTime = x.UpdateTime,
        CreatePerson = x.CreatePerson,
        UpdatePerson = x.UpdatePerson,
        Remark = x.Remark,
        Rated_torque = x.Rated_torque,
        Rated_speed = x.Rated_speed,
        Power_supply = x.Power_supply,
        Rated_Power = x.Rated_Power,
        Rated_Current = x.Rated_Current,
        Noise_dBA = x.Noise_dBA,
        Motor_Length = x.Motor_Length,
        Tube_diameter = x.Tube_diameter,
        Battery_Capacity = x.Battery_Capacity,
        Exclude_text = x.Exclude_text,
    };

    private static QuotationBaseListItemDto MapRemote(ZyqdQuotationBaseRemoteModel x) => new()
    {
        Id = x.Id,
        ItemType = QuotationBaseTypes.Remote,
        Part_No = x.Part_No,
        Quotation_logo = x.Quotation_logo,
        CreateTime = x.CreateTime,
        UpdateTime = x.UpdateTime,
        CreatePerson = x.CreatePerson,
        UpdatePerson = x.UpdatePerson,
        Remark = x.Remark,
        Remote_controller_type = x.Remote_controller_type,
        Battery_Type = x.Battery_Type,
        Radio_Frequency = x.Radio_Frequency,
        Working_Temperature = x.Working_Temperature,
        NoOfChannels = x.NoOfChannels,
    };

    private static QuotationBaseListItemDto MapAccessory(ZyqdQuotationBaseAccessoryModel x) => new()
    {
        Id = x.Id,
        ItemType = QuotationBaseTypes.Accessory,
        Part_No = x.Part_No,
        Quotation_logo = x.Quotation_logo,
        CreateTime = x.CreateTime,
        UpdateTime = x.UpdateTime,
        CreatePerson = x.CreatePerson,
        UpdatePerson = x.UpdatePerson,
        Remark = x.Remark,
        adaptor = x.adaptor,
    };

    private static QuotationBaseListItemDto MapCb(ZyqdQuotationBaseCbModel x) => new()
    {
        Id = x.Id,
        ItemType = QuotationBaseTypes.CB,
        Part_No = x.Part_No,
        Quotation_logo = x.Quotation_logo,
        CreateTime = x.CreateTime,
        UpdateTime = x.UpdateTime,
        CreatePerson = x.CreatePerson,
        UpdatePerson = x.UpdatePerson,
        Remark = x.Remark,
        Rated_Voltage = x.Rated_Voltage,
        Size = x.Size,
        Rated_Power = x.Rated_Power,
        No_Of_Actuator = x.No_Of_Actuator,
        Output_Control = x.Output_Control,
        Sensor = x.Sensor,
        Protection_Degree = x.Protection_Degree,
    };

    private static QuotationBaseListItemDto MapPr(ZyqdQuotationBasePrModel x) => new()
    {
        Id = x.Id,
        ItemType = QuotationBaseTypes.PR,
        Part_No = x.Part_No,
        Quotation_logo = x.Quotation_logo,
        CreateTime = x.CreateTime,
        UpdateTime = x.UpdateTime,
        CreatePerson = x.CreatePerson,
        UpdatePerson = x.UpdatePerson,
        Remark = x.Remark,
        Max_Loading = x.Max_Loading,
        Max_Stroke = x.Max_Stroke,
        Protection_Degree2 = x.Protection_Degree2,
        Noise = x.Noise,
    };

    public async Task<QuotationBaseListItemDto?> GetAsync(
        string itemType, int id, CancellationToken ct = default)
    {
        if (id <= 0) return null;
        var type = QuotationBaseTypes.Normalize(itemType);

        return type switch
        {
            QuotationBaseTypes.Remote => await MapOneAsync(
                _db.QuotationBaseRemotes.AsNoTracking(), id, MapRemote, ct),
            QuotationBaseTypes.Accessory => await MapOneAsync(
                _db.QuotationBaseAccessories.AsNoTracking(), id, MapAccessory, ct),
            QuotationBaseTypes.CB => await MapOneAsync(
                _db.QuotationBaseCbs.AsNoTracking(), id, MapCb, ct),
            QuotationBaseTypes.PR => await MapOneAsync(
                _db.QuotationBasePrs.AsNoTracking(), id, MapPr, ct),
            _ => await MapOneAsync(
                _db.QuotationBaseMotors.AsNoTracking(), id, MapMotor, ct),
        };
    }

    private static async Task<QuotationBaseListItemDto?> MapOneAsync<T>(
        IQueryable<T> q, int id, Func<T, QuotationBaseListItemDto> map, CancellationToken ct)
        where T : QuotationBaseEntity
    {
        var row = await q.FirstOrDefaultAsync(
            x => x.Id == id && (x.deleteSigh == null || x.deleteSigh == ""), ct);
        return row is null ? null : map(row);
    }

    public async Task<(bool Ok, string? Message, int Id)> SaveAsync(
        string itemType,
        QuotationBaseListItemDto model,
        string? empNo,
        bool isCreate,
        CancellationToken ct = default)
    {
        var type = QuotationBaseTypes.Normalize(itemType);
        var partNo = (model.Part_No ?? "").Trim();
        if (partNo.Length == 0)
            return (false, "型号不能为空", 0);

        model.Part_No = partNo;
        empNo = (empNo ?? "").Trim();
        var now = DateTime.Now;

        return type switch
        {
            QuotationBaseTypes.Remote => await SaveRemoteAsync(model, empNo, isCreate, now, ct),
            QuotationBaseTypes.Accessory => await SaveAccessoryAsync(model, empNo, isCreate, now, ct),
            QuotationBaseTypes.CB => await SaveCbAsync(model, empNo, isCreate, now, ct),
            QuotationBaseTypes.PR => await SavePrAsync(model, empNo, isCreate, now, ct),
            _ => await SaveMotorAsync(model, empNo, isCreate, now, ct),
        };
    }

    private async Task<(bool Ok, string? Message, int Id)> SaveMotorAsync(
        QuotationBaseListItemDto model, string empNo, bool isCreate, DateTime now, CancellationToken ct)
    {
        if (await PartNoTakenAsync(_db.QuotationBaseMotors, model.Part_No!, isCreate ? 0 : model.Id, ct))
            return (false, "同类型下型号已存在", 0);

        ZyqdQuotationBaseMotorModel row;
        if (isCreate)
        {
            row = new ZyqdQuotationBaseMotorModel
            {
                CreateTime = now,
                CreatePerson = empNo,
                deleteSigh = "",
            };
            _db.QuotationBaseMotors.Add(row);
        }
        else
        {
            row = await _db.QuotationBaseMotors.FirstOrDefaultAsync(x => x.Id == model.Id, ct)
                  ?? throw new InvalidOperationException("记录不存在");
            if (!string.IsNullOrWhiteSpace(row.deleteSigh))
                return (false, "记录已删除", 0);
        }

        row.Part_No = model.Part_No;
        row.Quotation_logo = model.Quotation_logo;
        row.Remark = model.Remark;
        row.Rated_torque = model.Rated_torque;
        row.Rated_speed = model.Rated_speed;
        row.Power_supply = model.Power_supply;
        row.Rated_Power = model.Rated_Power;
        row.Rated_Current = model.Rated_Current;
        row.Noise_dBA = model.Noise_dBA;
        row.Motor_Length = model.Motor_Length;
        row.Tube_diameter = model.Tube_diameter;
        row.Battery_Capacity = model.Battery_Capacity;
        row.Exclude_text = model.Exclude_text;
        row.UpdateTime = now;
        row.UpdatePerson = empNo;

        await _db.SaveChangesAsync(ct);
        return (true, null, row.Id);
    }

    private async Task<(bool Ok, string? Message, int Id)> SaveRemoteAsync(
        QuotationBaseListItemDto model, string empNo, bool isCreate, DateTime now, CancellationToken ct)
    {
        if (await PartNoTakenAsync(_db.QuotationBaseRemotes, model.Part_No!, isCreate ? 0 : model.Id, ct))
            return (false, "同类型下型号已存在", 0);

        ZyqdQuotationBaseRemoteModel row;
        if (isCreate)
        {
            row = new ZyqdQuotationBaseRemoteModel
            {
                CreateTime = now,
                CreatePerson = empNo,
                deleteSigh = "",
            };
            _db.QuotationBaseRemotes.Add(row);
        }
        else
        {
            row = await _db.QuotationBaseRemotes.FirstOrDefaultAsync(x => x.Id == model.Id, ct)
                  ?? throw new InvalidOperationException("记录不存在");
            if (!string.IsNullOrWhiteSpace(row.deleteSigh))
                return (false, "记录已删除", 0);
        }

        row.Part_No = model.Part_No;
        row.Quotation_logo = model.Quotation_logo;
        row.Remark = model.Remark;
        row.Remote_controller_type = model.Remote_controller_type;
        row.Battery_Type = model.Battery_Type;
        row.Radio_Frequency = model.Radio_Frequency;
        row.Working_Temperature = model.Working_Temperature;
        row.NoOfChannels = model.NoOfChannels;
        row.UpdateTime = now;
        row.UpdatePerson = empNo;

        await _db.SaveChangesAsync(ct);
        return (true, null, row.Id);
    }

    private async Task<(bool Ok, string? Message, int Id)> SaveAccessoryAsync(
        QuotationBaseListItemDto model, string empNo, bool isCreate, DateTime now, CancellationToken ct)
    {
        if (await PartNoTakenAsync(_db.QuotationBaseAccessories, model.Part_No!, isCreate ? 0 : model.Id, ct))
            return (false, "同类型下型号已存在", 0);

        ZyqdQuotationBaseAccessoryModel row;
        if (isCreate)
        {
            row = new ZyqdQuotationBaseAccessoryModel
            {
                CreateTime = now,
                CreatePerson = empNo,
                deleteSigh = "",
            };
            _db.QuotationBaseAccessories.Add(row);
        }
        else
        {
            row = await _db.QuotationBaseAccessories.FirstOrDefaultAsync(x => x.Id == model.Id, ct)
                  ?? throw new InvalidOperationException("记录不存在");
            if (!string.IsNullOrWhiteSpace(row.deleteSigh))
                return (false, "记录已删除", 0);
        }

        row.Part_No = model.Part_No;
        row.Quotation_logo = model.Quotation_logo;
        row.Remark = model.Remark;
        row.adaptor = model.adaptor;
        row.UpdateTime = now;
        row.UpdatePerson = empNo;

        await _db.SaveChangesAsync(ct);
        return (true, null, row.Id);
    }

    private async Task<(bool Ok, string? Message, int Id)> SaveCbAsync(
        QuotationBaseListItemDto model, string empNo, bool isCreate, DateTime now, CancellationToken ct)
    {
        if (await PartNoTakenAsync(_db.QuotationBaseCbs, model.Part_No!, isCreate ? 0 : model.Id, ct))
            return (false, "同类型下型号已存在", 0);

        ZyqdQuotationBaseCbModel row;
        if (isCreate)
        {
            row = new ZyqdQuotationBaseCbModel
            {
                CreateTime = now,
                CreatePerson = empNo,
                deleteSigh = "",
            };
            _db.QuotationBaseCbs.Add(row);
        }
        else
        {
            row = await _db.QuotationBaseCbs.FirstOrDefaultAsync(x => x.Id == model.Id, ct)
                  ?? throw new InvalidOperationException("记录不存在");
            if (!string.IsNullOrWhiteSpace(row.deleteSigh))
                return (false, "记录已删除", 0);
        }

        row.Part_No = model.Part_No;
        row.Quotation_logo = model.Quotation_logo;
        row.Remark = model.Remark;
        row.Rated_Voltage = model.Rated_Voltage;
        row.Size = model.Size;
        row.Rated_Power = model.Rated_Power;
        row.No_Of_Actuator = model.No_Of_Actuator;
        row.Output_Control = model.Output_Control;
        row.Sensor = model.Sensor;
        row.Protection_Degree = model.Protection_Degree;
        row.UpdateTime = now;
        row.UpdatePerson = empNo;

        await _db.SaveChangesAsync(ct);
        return (true, null, row.Id);
    }

    private async Task<(bool Ok, string? Message, int Id)> SavePrAsync(
        QuotationBaseListItemDto model, string empNo, bool isCreate, DateTime now, CancellationToken ct)
    {
        if (await PartNoTakenAsync(_db.QuotationBasePrs, model.Part_No!, isCreate ? 0 : model.Id, ct))
            return (false, "同类型下型号已存在", 0);

        ZyqdQuotationBasePrModel row;
        if (isCreate)
        {
            row = new ZyqdQuotationBasePrModel
            {
                CreateTime = now,
                CreatePerson = empNo,
                deleteSigh = "",
            };
            _db.QuotationBasePrs.Add(row);
        }
        else
        {
            row = await _db.QuotationBasePrs.FirstOrDefaultAsync(x => x.Id == model.Id, ct)
                  ?? throw new InvalidOperationException("记录不存在");
            if (!string.IsNullOrWhiteSpace(row.deleteSigh))
                return (false, "记录已删除", 0);
        }

        row.Part_No = model.Part_No;
        row.Quotation_logo = model.Quotation_logo;
        row.Remark = model.Remark;
        row.Max_Loading = model.Max_Loading;
        row.Max_Stroke = model.Max_Stroke;
        row.Protection_Degree2 = model.Protection_Degree2;
        row.Noise = model.Noise;
        row.UpdateTime = now;
        row.UpdatePerson = empNo;

        await _db.SaveChangesAsync(ct);
        return (true, null, row.Id);
    }

    public async Task<List<QuotationPartOptionDto>> ListPartsAsync(
        string itemType, CancellationToken ct = default)
    {
        var type = QuotationBaseTypes.Normalize(itemType);
        return type switch
        {
            QuotationBaseTypes.Remote => await _db.QuotationBaseRemotes.AsNoTracking()
                .Where(x => x.deleteSigh == null || x.deleteSigh == "")
                .OrderBy(x => x.Part_No)
                .Select(x => new QuotationPartOptionDto { Id = x.Id, Part_No = x.Part_No ?? "", Label = x.Part_No })
                .ToListAsync(ct),
            QuotationBaseTypes.Accessory => await _db.QuotationBaseAccessories.AsNoTracking()
                .Where(x => x.deleteSigh == null || x.deleteSigh == "")
                .OrderBy(x => x.Part_No)
                .Select(x => new QuotationPartOptionDto { Id = x.Id, Part_No = x.Part_No ?? "", Label = x.Part_No })
                .ToListAsync(ct),
            QuotationBaseTypes.CB => await _db.QuotationBaseCbs.AsNoTracking()
                .Where(x => x.deleteSigh == null || x.deleteSigh == "")
                .OrderBy(x => x.Part_No)
                .Select(x => new QuotationPartOptionDto { Id = x.Id, Part_No = x.Part_No ?? "", Label = x.Part_No })
                .ToListAsync(ct),
            QuotationBaseTypes.PR => await _db.QuotationBasePrs.AsNoTracking()
                .Where(x => x.deleteSigh == null || x.deleteSigh == "")
                .OrderBy(x => x.Part_No)
                .Select(x => new QuotationPartOptionDto { Id = x.Id, Part_No = x.Part_No ?? "", Label = x.Part_No })
                .ToListAsync(ct),
            _ => await _db.QuotationBaseMotors.AsNoTracking()
                .Where(x => x.deleteSigh == null || x.deleteSigh == "")
                .OrderBy(x => x.Part_No)
                .Select(x => new QuotationPartOptionDto { Id = x.Id, Part_No = x.Part_No ?? "", Label = x.Part_No })
                .ToListAsync(ct),
        };
    }

    private static Task<bool> PartNoTakenAsync<T>(
        DbSet<T> set, string partNo, int excludeId, CancellationToken ct)
        where T : QuotationBaseEntity
        => set.AsNoTracking().AnyAsync(
            x => x.Part_No == partNo
                 && x.Id != excludeId
                 && (x.deleteSigh == null || x.deleteSigh == ""),
            ct);
}
