using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

public class ClientPaymentService
{
    private readonly SqlServerDbContext _db;

    public ClientPaymentService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<List<ZyqdBalancePayTypeModel>> ListPayTypesAsync(CancellationToken ct = default)
    {
        return await _db.BalancePayTypes.AsNoTracking()
            .Where(x => x.deleteSigh != "X")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<List<SelectOptionItem>> ListPaymentOptionsAsync(CancellationToken ct = default)
    {
        var types = await ListPayTypesAsync(ct);
        return types
            .Select(t => new SelectOptionItem
            {
                Value = (t.Description ?? "").Trim(),
                Label = FormatPayType(t.Id, t.Description)
            })
            .Where(x => x.Value.Length > 0)
            .ToList();
    }

    public async Task<ZyqdClientPaymentModel?> GetActiveByClientNoAsync(
        string? clientNo, CancellationToken ct = default)
    {
        var no = CotractPiTypes.NormalizeClientNo(clientNo);
        if (no.Length == 0)
            return null;
        return await _db.ClientPayments.AsNoTracking()
            .Include(x => x.BalancePayType)
            .Where(x => x.ClientNo == no && x.deleteSigh != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(bool Ok, string? Message)> SavePayTypeAsync(
        int id, string? description, string empNo, CancellationToken ct = default)
    {
        var desc = (description ?? "").Trim();
        if (desc.Length == 0)
            return (false, "请填写付款方式描述");

        var now = DateTime.Now;
        var person = (empNo ?? "").Trim();
        if (id <= 0)
        {
            _db.BalancePayTypes.Add(new ZyqdBalancePayTypeModel
            {
                Description = desc,
                UpdatePerson = person,
                UpdateTime = now,
                deleteSigh = "N",
            });
        }
        else
        {
            var row = await _db.BalancePayTypes.FirstOrDefaultAsync(x => x.Id == id && x.deleteSigh != "X", ct);
            if (row is null)
                return (false, "付款方式不存在");
            row.Description = desc;
            row.UpdatePerson = person;
            row.UpdateTime = now;
        }

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Message)> DeletePayTypeAsync(
        int id, string? empNo = null, CancellationToken ct = default)
    {
        if (id <= 0)
            return (false, "记录无效");

        var used = await _db.ClientPayments.AsNoTracking()
            .AnyAsync(x => x.BalancePayTypeId == id && x.deleteSigh != "X", ct);
        if (used)
            return (false, "已有客户使用该付款方式，不能删除");

        var row = await _db.BalancePayTypes.FirstOrDefaultAsync(x => x.Id == id && x.deleteSigh != "X", ct);
        if (row is null)
            return (false, "付款方式不存在或已删除");

        row.deleteSigh = "X";
        row.deleteTime = DateTime.Now;
        row.deletePerson = (empNo ?? "").Trim();
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<List<ZyqdClientPaymentModel>> ListClientPaymentsAsync(
        string? keyword, IReadOnlyCollection<string>? allowedClientNos = null, CancellationToken ct = default)
    {
        var kw = (keyword ?? "").Trim();
        var q = _db.ClientPayments.AsNoTracking()
            .Include(x => x.BalancePayType)
            .Where(x => x.deleteSigh != "X");
        if (allowedClientNos is not null)
        {
            var allowed = allowedClientNos
                .Select(CotractPiTypes.NormalizeClientNo)
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (allowed.Count == 0)
                return [];
            q = q.Where(x => x.ClientNo != null && allowed.Contains(x.ClientNo));
        }
        if (kw.Length > 0)
        {
            q = q.Where(x =>
                (x.ClientNo ?? "").Contains(kw)
                || (x.BalancePayType != null && (x.BalancePayType.Description ?? "").Contains(kw)));
        }

        return await q
            .OrderBy(x => x.ClientNo)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<(bool Ok, string? Message)> SaveClientPaymentAsync(
        int id, string? clientNo, decimal deposit, int payTypeId, string empNo,
        IReadOnlyCollection<string>? allowedClientNos = null, CancellationToken ct = default)
    {
        var no = CotractPiTypes.NormalizeClientNo(clientNo);
        if (no.Length == 0)
            return (false, "请填写客户编号");
        if (allowedClientNos is not null && !ContainsClient(allowedClientNos, no))
            return (false, "只能维护已分配给自己的客户");
        if (deposit < 0 || deposit > 100)
            return (false, "定金请填写 0–100 的百分比");
        if (payTypeId <= 0)
            return (false, "请选择付款方式");

        var payExists = await _db.BalancePayTypes.AsNoTracking()
            .AnyAsync(x => x.Id == payTypeId && x.deleteSigh != "X", ct);
        if (!payExists)
            return (false, "付款方式不存在");

        var now = DateTime.Now;
        var person = (empNo ?? "").Trim();

        if (id <= 0)
        {
            var dup = await _db.ClientPayments
                .Where(x => x.ClientNo == no && x.deleteSigh != "X")
                .AnyAsync(ct);
            if (dup)
                return (false, "该客户编号已有付款方式，请直接编辑");

            var recycled = await _db.ClientPayments
                .FirstOrDefaultAsync(x => x.ClientNo == no && x.deleteSigh == "X", ct);
            if (recycled is not null)
            {
                recycled.Deposit = FormatDeposit(deposit);
                recycled.BalancePayTypeId = payTypeId;
                recycled.UpdatePerson = person;
                recycled.UpdateTime = now;
                recycled.deleteSigh = "N";
            }
            else
            {
                _db.ClientPayments.Add(new ZyqdClientPaymentModel
                {
                    ClientNo = no,
                    Deposit = FormatDeposit(deposit),
                    BalancePayTypeId = payTypeId,
                    UpdatePerson = person,
                    UpdateTime = now,
                    deleteSigh = "N"
                });
            }
        }
        else
        {
            var row = await _db.ClientPayments.FirstOrDefaultAsync(x => x.Id == id && x.deleteSigh != "X", ct);
            if (row is null)
                return (false, "记录不存在");
            row.Deposit = FormatDeposit(deposit);
            row.BalancePayTypeId = payTypeId;
            row.UpdatePerson = person;
            row.UpdateTime = now;
        }

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Message)> DeleteClientPaymentAsync(
        int id, string empNo, IReadOnlyCollection<string>? allowedClientNos = null, CancellationToken ct = default)
    {
        var row = await _db.ClientPayments.FirstOrDefaultAsync(x => x.Id == id && x.deleteSigh != "X", ct);
        if (row is null)
            return (false, "记录不存在");
        if (allowedClientNos is not null && !ContainsClient(allowedClientNos, row.ClientNo))
            return (false, "只能维护已分配给自己的客户");

        row.deleteSigh = "X";
        row.UpdatePerson = (empNo ?? "").Trim();
        row.UpdateTime = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static bool ContainsClient(IReadOnlyCollection<string> allowed, string? clientNo)
    {
        var no = CotractPiTypes.NormalizeClientNo(clientNo);
        if (no.Length == 0) return false;
        return allowed.Any(x =>
            string.Equals(CotractPiTypes.NormalizeClientNo(x), no, StringComparison.OrdinalIgnoreCase));
    }

    public static byte[] BuildClientPaymentExcel(IReadOnlyList<ZyqdClientPaymentModel> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("客户付款方式");
        WriteHeader(ws, "序号", "客户编号", "定金", "付款方式编号", "付款方式", "维护人", "维护时间");

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 2;
            SetNumber(ws.Cell(row, 1), i + 1);
            SetNumber(ws.Cell(row, 2), r.ClientNo);
            SetNumber(ws.Cell(row, 3), ParseDeposit(r.Deposit));
            SetNumber(ws.Cell(row, 4), r.BalancePayTypeId);
            SetText(ws.Cell(row, 5), r.BalancePayType?.Description);
            SetText(ws.Cell(row, 6), r.UpdatePerson);
            SetDate(ws.Cell(row, 7), r.UpdateTime);
        }

        ws.Columns().AdjustToContents();
        return SaveWorkbook(wb);
    }

    public static byte[] BuildPayTypeExcel(IReadOnlyList<ZyqdBalancePayTypeModel> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("付款方式列表");
        WriteHeader(ws, "序号", "编号", "描述", "修改人", "修改时间");

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 2;
            SetNumber(ws.Cell(row, 1), i + 1);
            SetNumber(ws.Cell(row, 2), r.Id);
            SetText(ws.Cell(row, 3), r.Description);
            SetText(ws.Cell(row, 4), r.UpdatePerson);
            SetDate(ws.Cell(row, 5), r.UpdateTime);
        }

        ws.Columns().AdjustToContents();
        return SaveWorkbook(wb);
    }

    private static void WriteHeader(IXLWorksheet ws, params string[] titles)
    {
        for (var i = 0; i < titles.Length; i++)
            ws.Cell(1, i + 1).Value = titles[i];
        StyleHeader(ws.Range(1, 1, 1, titles.Length));
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        header.Style.Font.FontColor = XLColor.White;
    }

    private static byte[] SaveWorkbook(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetNumber(IXLCell cell, int value)
    {
        cell.Value = value;
        ApplyBuiltInNumberFormat(cell, true);
    }

    private static void SetNumber(IXLCell cell, decimal value)
    {
        cell.Value = value;
        ApplyBuiltInNumberFormat(cell, value == decimal.Truncate(value));
    }

    private static void SetNumber(IXLCell cell, string? value)
    {
        var s = (value ?? "").Trim();
        if (decimal.TryParse(s, out var n))
        {
            SetNumber(cell, n);
            return;
        }

        cell.Value = Blank.Value;
        ApplyBuiltInNumberFormat(cell, true);
    }

    // Excel 内置：1 = 0（数值），2 = 0.00（数值）。写 Format 字符串会被当成“自定义”，整数还会显示成 30.
    private static void ApplyBuiltInNumberFormat(IXLCell cell, bool integer)
        => cell.Style.NumberFormat.NumberFormatId = integer ? 1 : 2;

    private static void SetDate(IXLCell cell, DateTime? value)
    {
        if (value is null)
        {
            cell.Value = Blank.Value;
            return;
        }

        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = "yyyy-mm-dd hh:mm:ss";
    }

    private static void SetText(IXLCell cell, string? value)
        => cell.Value = value ?? "";


    public static string FormatPayType(int id, string? description)
    {
        var desc = (description ?? "").Trim();
        return desc.Length == 0 ? id.ToString() : $"{id}.{desc}";
    }

    public static bool TryParseDeposit(string? value, out decimal deposit)
    {
        var s = (value ?? "").Trim().TrimEnd('%').Trim();
        return decimal.TryParse(s, out deposit);
    }

    public static decimal ParseDeposit(string? value)
        => TryParseDeposit(value, out var d) ? d : 0;

    public static string FormatDeposit(decimal deposit)
        => deposit.ToString("0.##");
}
