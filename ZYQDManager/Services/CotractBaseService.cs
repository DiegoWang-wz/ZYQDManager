using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public class CotractBaseService
{
    private const string LegacyHttpClient = "LegacyFiles";
    public const string TemplateFileName = "遮阳基础数据导入模板.xlsx";

    private const int ColOuterBoxLogo = 1;
    private const int ColTuoMark = 2;
    private const int ColTuoMarkUrl = 3;
    private const int ColCountry = 4;
    private const int ColClientNo = 5;
    private const int ColClientName = 6;
    private const int ColMaterialNo = 7;
    private const int ColMaterialName = 8;
    private const int ColMaterialType = 9;
    private const int ColMaterialModel = 10;
    private const int ColSpecs = 11;
    private const int ColPowered = 12;
    private const int ColCust = 13;
    private const int ColNotes = 14;
    private const int ColQtyUnit = 15;
    private const int ColCurrency = 16;
    private const int ColPrice = 17;
    private const int ColPriceUnit = 18;
    private const int ColBoxLabel = 19;
    private const int ColBoxLabel2 = 20;
    private const int ColBoxLabelUrl = 21;
    private const int ColBoxLabelUrl2 = 22;
    private const int ColPoNo = 23;
    private const int ColNameplate = 24;
    private const int ColNameplate2 = 25;
    private const int ColSapNo = 26;
    private const int ColPacking = 27;
    private const int ColPackingUnit = 28;
    private const int ColCarton = 29;
    private const int ColPallet = 30;
    private const int ColPalletizing = 31;
    private const int ColPalletizingUnit = 32;
    private const int ColNetWeight = 33;
    private const int ColGrossWeight = 34;
    private const int ColManager = 35;

    private static readonly string[] TemplateHeaders =
    [
        "客户外箱标志", "托唛", "托唛(图)", "客户目的国", "客户编号", "客户名称",
        "物料号", "物料名", "物料类型", "物料model", "物料specs", "物料Powered", "物料Cust",
        "备注Notes", "数量单位", "货币单位", "单价", "单价单位",
        "箱唛", "箱唛2", "箱唛图例", "箱唛图例2", "客户po号",
        "铭牌图片", "铭牌图片2", "sap单号", "打包方式", "打包方式单位",
        "纸箱尺寸", "托盘尺寸", "打托方式", "打托方式单位", "净重", "毛重", "客户负责人"
    ];

    private const string LegacyImgUploadPath = "/T_ZYQD_Cotract_Base/Add_Cotract_Base_img";

    private readonly SqlServerDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IWebHostEnvironment _env;
    private readonly string _imageBase;
    private readonly FileOperationService _fileOp;

    public CotractBaseService(
        SqlServerDbContext db,
        IHttpClientFactory httpFactory,
        IWebHostEnvironment env,
        IOptions<LegacyFilesOptions> legacyFiles,
        FileOperationService fileOp)
    {
        _db = db;
        _httpFactory = httpFactory;
        _env = env;
        _imageBase = legacyFiles.Value.ResolvedBaseUrl();
        _fileOp = fileOp;
    }

    public async Task<List<ZyqdCotractBaseModel>> QueryAsync(CotractBaseQueryDto query, CancellationToken ct = default)
        => await Sorted(BuildFiltered(query)).ToListAsync(ct);

    public async Task<(List<ZyqdCotractBaseModel> Rows, int Total)> QueryPageAsync(
        CotractBaseQueryDto query, int skip, int take, CancellationToken ct = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 20;

        var q = BuildFiltered(query);
        var total = await q.CountAsync(ct);
        var rows = await Sorted(q, query.SortLabel, query.SortDescending)
            .Skip(skip)
            .Take(take)
            .Select(x => new ZyqdCotractBaseModel
            {
                Id = x.Id,
                Customer_outer_box_logo = x.Customer_outer_box_logo,
                Tuo_Mark = x.Tuo_Mark,
                Tuo_Mark_url = x.Tuo_Mark_url,
                Customer_destination_country = x.Customer_destination_country,
                ClientNo = x.ClientNo,
                ClientName = x.ClientName,
                materialNo = x.materialNo,
                materialName = x.materialName,
                materialType = x.materialType,
                materialModel = x.materialModel,
                materiaSpecs = x.materiaSpecs,
                materiaPowered = x.materiaPowered,
                materiaCust = x.materiaCust,
                Notes = x.Notes,
                Quantity_unit = x.Quantity_unit,
                currency_unit = x.currency_unit,
                price = x.price,
                price_unit = x.price_unit,
                Box_label = x.Box_label,
                Box_label_url = x.Box_label_url,
                Box_label2 = x.Box_label2,
                Box_label_url2 = x.Box_label_url2,
                Client_poNo = x.Client_poNo,
                nameplate_url = x.nameplate_url,
                nameplate_url2 = x.nameplate_url2,
                SAP_order_NO = x.SAP_order_NO,
                Packing_method = x.Packing_method,
                Packing_method_unit = x.Packing_method_unit,
                Carton_Size = x.Carton_Size,
                Pallet_Size = x.Pallet_Size,
                palletizing_method = x.palletizing_method,
                palletizing_method_unit = x.palletizing_method_unit,
                Net_weight = x.Net_weight,
                gross_weight = x.gross_weight,
                Customer_Manager = x.Customer_Manager,
                CreatePerson = x.CreatePerson,
                CreateTime = x.CreateTime
            })
            .ToListAsync(ct);
        return (rows, total);
    }

    public Task<int> CountAllAsync(CancellationToken ct = default)
        => _db.CotractBases.AsNoTracking().CountAsync(ct);

    public Task<ZyqdCotractBaseModel?> GetAsync(int id, CancellationToken ct = default)
        => _db.CotractBases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<ZyqdCotractBaseModel>> ListByClientNoAsync(string? clientNo, CancellationToken ct = default)
    {
        clientNo = CotractPiTypes.NormalizeClientNo(clientNo);
        if (clientNo.Length == 0)
            return [];

        return await _db.CotractBases.AsNoTracking()
            .Where(x => x.ClientNo == clientNo && (x.deleteSigh == null || x.deleteSigh != "X"))
            .Where(x => (x.materialNo != null && x.materialNo != "")
                        || (x.materialModel != null && x.materialModel != "")
                        || (x.materiaCust != null && x.materiaCust != ""))
            .OrderBy(x => x.materialNo)
            .Select(x => new ZyqdCotractBaseModel
            {
                Id = x.Id,
                ClientNo = x.ClientNo,
                ClientName = x.ClientName,
                materialNo = x.materialNo,
                materialName = x.materialName,
                materialType = x.materialType,
                materialModel = x.materialModel,
                materiaSpecs = x.materiaSpecs,
                materiaPowered = x.materiaPowered,
                materiaCust = x.materiaCust,
                Notes = x.Notes,
                Quantity_unit = x.Quantity_unit,
                currency_unit = x.currency_unit,
                price = x.price,
                price_unit = x.price_unit
            })
            .ToListAsync(ct);
    }

    public bool CanAccess(ZyqdCotractBaseModel row, CotractBaseQueryDto query)
    {
        if (query.ViewAllCustomers) return true;

        var empNo = (query.ViewerEmpNo ?? "").Trim();
        if (empNo.Length > 0
            && !string.IsNullOrWhiteSpace(row.Customer_Manager)
            && row.Customer_Manager.Contains(empNo, StringComparison.OrdinalIgnoreCase))
            return true;

        var clientNo = (row.ClientNo ?? "").Trim();
        if (clientNo.Length == 0) return false;

        return query.AssignedCustomerCodes.Any(x =>
            string.Equals((x ?? "").Trim(), clientNo, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(bool Ok, string? Message, int Id)> SaveAsync(ZyqdCotractBaseModel model, string? empNo, bool isCreate, CancellationToken ct = default)
    {
        Normalize(model);
        var err = Validate(model);
        if (err is not null)
            return (false, err, model.Id);

        var dup = await ExistsSameClientMaterialAsync(model, ct);
        if (dup)
            return (false, $"客户编号 {model.ClientNo}、物料编号 {model.materialNo} 已存在，不能重复创建", model.Id);

        var actor = (empNo ?? "").Trim();
        var now = DateTime.Now;
        if (isCreate)
        {
            if (actor.Length == 0)
                return (false, "无法获取当前登录工号，请重新登录后再保存", model.Id);

            model.Id = 0;
            model.CreateTime = now;
            model.CreatePerson = actor;
            model.UpdateTime = now;
            model.UpdatePerson = actor;
            model.deleteSigh ??= "";
            _db.CotractBases.Add(model);
        }
        else
        {
            var dbRow = await _db.CotractBases.FirstOrDefaultAsync(x => x.Id == model.Id, ct);
            if (dbRow is null)
                return (false, "记录不存在", model.Id);

            CopyFields(model, dbRow);
            dbRow.UpdateTime = now;
            if (actor.Length > 0)
                dbRow.UpdatePerson = actor;
        }

        await _db.SaveChangesAsync(ct);
        return (true, null, model.Id);
    }

    public async Task<(bool Ok, string? Message)> DeleteAsync(int id, CancellationToken ct = default)
    {
        var row = await _db.CotractBases.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return (false, "记录不存在");
        _db.CotractBases.Remove(row);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<byte[]> ExportAsync(
        CotractBaseQueryDto query,
        string imageBase,
        IReadOnlyCollection<int>? selectedIds = null,
        CancellationToken ct = default)
    {
        var q = BuildFiltered(query);
        if (selectedIds is { Count: > 0 })
        {
            var ids = selectedIds.Distinct().ToList();
            q = q.Where(x => ids.Contains(x.Id));
        }

        var rows = await Sorted(q, query.SortLabel, query.SortDescending).ToListAsync(ct);
        if (rows.Count == 0)
            return [];

        var images = await DownloadImagesAsync(rows, imageBase, ct);
        var templateBytes = await LoadShareTemplateBytesAsync(ct);

        using var input = new MemoryStream(templateBytes);
        using var wb = new XLWorkbook(input);
        var ws = wb.Worksheets.First();
        const int dataStartRow = 2;
        if (rows.Count > 1)
            ws.Row(dataStartRow).InsertRowsBelow(rows.Count - 1);

        var pending = new List<PendingPicture>();
        for (var i = 0; i < rows.Count; i++)
            FillTemplateRow(ws, dataStartRow + i, rows[i], imageBase, images, pending);
        PlacePendingPictures(ws, pending);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public Task<byte[]> GetTemplateBytesAsync(CancellationToken ct = default)
        => LoadShareTemplateBytesAsync(ct);

    public async Task<CotractBaseImportResult> ImportAsync(
        Stream excelStream,
        string? empNo,
        CotractBaseQueryDto scope,
        CancellationToken ct = default)
    {
        if (excelStream.CanSeek)
            excelStream.Position = 0;

        string? savePath = null;
        try
        {
            savePath = await SaveImportToShareAsync(excelStream, ct);
            await _fileOp.DecodeFileAsync(savePath, ct);
            var bytes = await File.ReadAllBytesAsync(savePath, ct);
            await _fileOp.EncodeFileAsync(savePath, ct);
            using var ms = new MemoryStream(bytes);
            return await ImportWorkbookAsync(ms, empNo, scope, ct);
        }
        catch (Exception ex)
        {
            return FailImport("导入失败：" + FlattenException(ex));
        }
    }

    private async Task<CotractBaseImportResult> ImportWorkbookAsync(
        Stream excelStream,
        string? empNo,
        CotractBaseQueryDto scope,
        CancellationToken ct)
    {
        var actor = (empNo ?? "").Trim();
        if (actor.Length == 0)
            return FailImport("无法获取当前登录工号，请重新登录后再导入");

        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(excelStream);
        }
        catch (Exception ex)
        {
            return FailImport("无法打开 Excel，请使用 xlsx 模板：" + ex.Message);
        }

        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null)
                return FailImport("工作簿中没有工作表");

            var pictures = IndexPictures(ws);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            foreach (var key in pictures.Keys)
                lastRow = Math.Max(lastRow, key.Row);

            if (lastRow < 2)
                return FailImport("没有可导入的数据");

            var byKey = new Dictionary<string, ZyqdCotractBaseModel>(StringComparer.OrdinalIgnoreCase);
            var firstRowByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var excelRow = 2; excelRow <= lastRow; excelRow++)
            {
                if (IsImportRowEmpty(ws, excelRow, pictures))
                    continue;

                var parsed = await ParseImportRowAsync(ws, excelRow, pictures, ct);
                if (!parsed.Ok)
                    return FailImport(parsed.Error ?? $"第{excelRow}行解析失败");

                var model = parsed.Row!;
                Normalize(model);

                if (string.IsNullOrWhiteSpace(model.ClientNo) || string.IsNullOrWhiteSpace(model.materialNo))
                    return FailImport($"第{excelRow}行，客户编号,物料编号不可为空请检查");

                if (!CanAccess(model, scope))
                    return FailImport($"第{excelRow}行，无权导入客户 {model.ClientNo}");

                var err = Validate(model);
                if (err is not null)
                    return FailImport($"第{excelRow}行，{err}");

                var key = ImportKey(model.ClientNo, model.materialNo);
                if (firstRowByKey.TryGetValue(key, out var prevRow))
                    return FailImport($"第{excelRow}行与第{prevRow}行客户编号、物料编号重复（{model.ClientNo} / {model.materialNo}）");

                firstRowByKey[key] = excelRow;
                byKey[key] = model;
            }

            var rows = byKey.Values.ToList();
            if (rows.Count == 0)
                return FailImport("没有可导入的数据");

            var clientNos = rows.Select(x => x.ClientNo!.ToLower()).Distinct().ToList();
            var materialNos = rows.Select(x => x.materialNo!.ToLower()).Distinct().ToList();
            var candidates = await _db.CotractBases
                .AsNoTracking()
                .Where(x => x.ClientNo != null && x.materialNo != null
                            && clientNos.Contains(x.ClientNo.ToLower())
                            && materialNos.Contains(x.materialNo.ToLower()))
                .ToListAsync(ct);

            var existed = candidates.Where(x =>
                rows.Any(n =>
                    string.Equals(n.ClientNo, x.ClientNo, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(n.materialNo, x.materialNo, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (existed.Count > 0)
            {
                var samples = existed
                    .Select(x => $"{x.ClientNo}/{x.materialNo}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToList();
                var more = existed.Select(x => $"{x.ClientNo}/{x.materialNo}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() > samples.Count
                    ? "…"
                    : "";
                return FailImport("客户编号、物料编号已存在，导入已拦截：" + string.Join("、", samples) + more);
            }

            var now = DateTime.Now;
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var row in rows)
                {
                    row.Id = 0;
                    row.CreateTime = now;
                    row.CreatePerson = actor;
                    row.UpdateTime = now;
                    row.UpdatePerson = actor;
                    row.deleteSigh ??= "";
                    _db.CotractBases.Add(row);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            return new CotractBaseImportResult
            {
                Ok = true,
                Imported = rows.Count,
                Replaced = 0,
                Message = $"成功导入 {rows.Count} 条"
            };
        }
    }

    private static CotractBaseImportResult FailImport(string message)
        => new() { Ok = false, Message = message };

    private static string FlattenException(Exception ex)
    {
        var parts = new List<string>();
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (string.IsNullOrWhiteSpace(e.Message))
                continue;
            if (parts.Count == 0 || parts[^1] != e.Message)
                parts.Add(e.Message);
        }
        return parts.Count == 0 ? ex.ToString() : string.Join(" → ", parts);
    }

    private static string ImportKey(string? clientNo, string? materialNo)
        => $"{(clientNo ?? "").Trim()}\u001f{(materialNo ?? "").Trim()}";

    private string EnsureTemplate()
    {
        var path = ResolveTemplatePath(createDir: true);
        if (File.Exists(path))
            return path;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        for (var i = 0; i < TemplateHeaders.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = TemplateHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.WrapText = true;
        }

        ws.SheetView.FreezeRows(1);
        ws.Row(1).Height = 22;
        ws.Columns(1, TemplateHeaders.Length).Width = 14;
        ws.Cell(2, 1).Value = "";
        wb.SaveAs(path);
        return path;
    }

    private string ResolveTemplatePath(bool createDir = false)
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var dir = Path.Combine(webRoot, "Templates");
        if (createDir)
            Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, TemplateFileName);
        if (!createDir && !File.Exists(path))
            throw new FileNotFoundException("未找到导出模板，请确认文件在 wwwroot/Templates/" + TemplateFileName);

        return path;
    }

    private async Task<byte[]> LoadShareTemplateBytesAsync(CancellationToken ct)
    {
        var sharePath = _fileOp.Options.TemplateFullPath();
        if (File.Exists(sharePath))
        {
            await _fileOp.DecodeFileAsync(sharePath, ct);
            var bytes = await File.ReadAllBytesAsync(sharePath, ct);
            await _fileOp.EncodeFileAsync(sharePath, ct);
            if (bytes.Length > 0)
                return bytes;
        }

        return await File.ReadAllBytesAsync(EnsureTemplate(), ct);
    }

    private async Task<string> SaveImportToShareAsync(Stream excelStream, CancellationToken ct)
    {
        var fileName = Guid.NewGuid().ToString("N") + ".xlsx";
        var savePath = _fileOp.Options.UploadFullPath(fileName);
        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            await excelStream.CopyToAsync(fs, ct);

        return savePath;
    }

    /// <summary>
    /// 列号与老系统 NPOI 导出一致（模板第 1 行为表头，数据从第 2 行开始）。
    /// </summary>
    private static void FillTemplateRow(
        IXLWorksheet ws,
        int row,
        ZyqdCotractBaseModel r,
        string imageBase,
        Dictionary<string, byte[]> images,
        List<PendingPicture> pending)
    {
        PutImageOrText(ws, row, ColOuterBoxLogo, r.Customer_outer_box_logo, imageBase, images, pending);
        ws.Cell(row, ColTuoMark).Value = r.Tuo_Mark;
        PutImageOrText(ws, row, ColTuoMarkUrl, r.Tuo_Mark_url, imageBase, images, pending);
        ws.Cell(row, ColCountry).Value = r.Customer_destination_country;
        ws.Cell(row, ColClientNo).Value = r.ClientNo;
        ws.Cell(row, ColClientName).Value = r.ClientName;
        ws.Cell(row, ColMaterialNo).Value = r.materialNo;
        ws.Cell(row, ColMaterialName).Value = r.materialName;
        ws.Cell(row, ColMaterialType).Value = r.materialType;
        ws.Cell(row, ColMaterialModel).Value = r.materialModel;
        ws.Cell(row, ColSpecs).Value = r.materiaSpecs;
        ws.Cell(row, ColPowered).Value = r.materiaPowered;
        ws.Cell(row, ColCust).Value = r.materiaCust;
        ws.Cell(row, ColNotes).Value = r.Notes;
        ws.Cell(row, ColQtyUnit).Value = r.Quantity_unit;
        ws.Cell(row, ColCurrency).Value = r.currency_unit;
        ws.Cell(row, ColPrice).Value = r.price;
        ws.Cell(row, ColPriceUnit).Value = r.price_unit;
        ws.Cell(row, ColBoxLabel).Value = r.Box_label;
        ws.Cell(row, ColBoxLabel2).Value = r.Box_label2;
        PutImageOrText(ws, row, ColBoxLabelUrl, r.Box_label_url, imageBase, images, pending);
        PutImageOrText(ws, row, ColBoxLabelUrl2, r.Box_label_url2, imageBase, images, pending);
        ws.Cell(row, ColPoNo).Value = r.Client_poNo;
        PutImageOrText(ws, row, ColNameplate, r.nameplate_url, imageBase, images, pending);
        PutImageOrText(ws, row, ColNameplate2, r.nameplate_url2, imageBase, images, pending);
        ws.Cell(row, ColSapNo).Value = r.SAP_order_NO;
        ws.Cell(row, ColPacking).Value = r.Packing_method;
        ws.Cell(row, ColPackingUnit).Value = r.Packing_method_unit;
        ws.Cell(row, ColCarton).Value = r.Carton_Size;
        ws.Cell(row, ColPallet).Value = r.Pallet_Size;
        ws.Cell(row, ColPalletizing).Value = r.palletizing_method;
        ws.Cell(row, ColPalletizingUnit).Value = r.palletizing_method_unit;
        ws.Cell(row, ColNetWeight).Value = r.Net_weight;
        ws.Cell(row, ColGrossWeight).Value = r.gross_weight;
        ws.Cell(row, ColManager).Value = r.Customer_Manager;
    }

    private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(
        List<ZyqdCotractBaseModel> rows, string imageBase, CancellationToken ct)
    {
        var urls = rows
            .SelectMany(r => new[]
            {
                r.Customer_outer_box_logo, r.Tuo_Mark_url, r.Box_label_url,
                r.Box_label_url2, r.nameplate_url, r.nameplate_url2
            })
            .Select(p => LegacyFileUrl.Resolve(p, imageBase))
            .Where(u => u.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        if (urls.Count == 0)
            return result;

        var http = _httpFactory.CreateClient(LegacyHttpClient);
        using var gate = new SemaphoreSlim(6);
        var bag = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        await Task.WhenAll(urls.Select(async url =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var bytes = await TryDownloadImageAsync(http, url, ct);
                if (bytes is { Length: > 0 })
                    bag[url] = bytes;
            }
            finally
            {
                gate.Release();
            }
        }));

        foreach (var kv in bag)
            result[kv.Key] = kv.Value;
        return result;
    }

    private static async Task<byte[]?> TryDownloadImageAsync(HttpClient http, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
                return null;
            if (resp.Content.Headers.ContentLength is > 8 * 1024 * 1024)
                return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            return LooksLikeImage(bytes) ? bytes : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikeImage(byte[] bytes)
    {
        if (bytes.Length < 12) return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8) return true;
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return true;
        if (bytes[0] == 0x42 && bytes[1] == 0x4D) return true;
        return false;
    }

    private sealed class PendingPicture
    {
        public required IXLPicture Pic { get; init; }
        public int Row { get; init; }
        public int Col { get; init; }
        public double ImgW { get; set; }
        public double ImgH { get; set; }
    }

    private static void PutImageOrText(
        IXLWorksheet ws, int row, int col, string? path, string imageBase,
        Dictionary<string, byte[]> images, List<PendingPicture> pending)
    {
        var raw = (path ?? "").Trim();
        if (LegacyFileUrl.IsBlankOrPlaceholder(raw))
        {
            if (raw.Length > 0)
                ws.Cell(row, col).Value = raw;
            return;
        }

        var url = LegacyFileUrl.Resolve(raw, imageBase);
        if (url.Length > 0 && images.TryGetValue(url, out var bytes))
        {
            QueuePicture(ws, row, col, bytes, $"imgR{row}C{col}", pending);
            return;
        }

        ws.Cell(row, col).Value = raw;
    }

    private static void QueuePicture(
        IXLWorksheet ws, int row, int col, byte[] bytes, string name, List<PendingPicture> pending)
    {
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            var format = DetectPictureFormat(bytes);
            var pic = format == XLPictureFormat.Unknown
                ? ws.AddPicture(ms, name)
                : ws.AddPicture(ms, format, name);
            pending.Add(new PendingPicture
            {
                Pic = pic,
                Row = row,
                Col = col,
                ImgW = Math.Max(1, pic.OriginalWidth),
                ImgH = Math.Max(1, pic.OriginalHeight)
            });
        }
        catch
        {
            // 单张图失败不影响整份导出
        }
    }

    private static void PlacePendingPictures(IXLWorksheet ws, List<PendingPicture> pending)
    {
        const double pad = 5;
        const double pxPerColChar = 7.0;
        const double maxColChars = 255;
        const double maxRowPts = 409;

        foreach (var p in pending)
        {
            var needCol = (p.ImgW + 2 * pad) / pxPerColChar;
            var needRow = (p.ImgH + 2 * pad) * 72.0 / 96.0;
            if (needCol > maxColChars || needRow > maxRowPts)
            {
                var scale = Math.Min(
                    maxColChars * pxPerColChar / (p.ImgW + 2 * pad),
                    maxRowPts * 96.0 / 72.0 / (p.ImgH + 2 * pad));
                p.ImgW *= scale;
                p.ImgH *= scale;
                needCol = (p.ImgW + 2 * pad) / pxPerColChar;
                needRow = (p.ImgH + 2 * pad) * 72.0 / 96.0;
            }

            if (ws.Column(p.Col).Width < needCol)
                ws.Column(p.Col).Width = needCol;
            if (ws.Row(p.Row).Height < needRow)
                ws.Row(p.Row).Height = needRow;
        }

        foreach (var p in pending)
        {
            var cellW = ws.Column(p.Col).Width * pxPerColChar;
            var cellH = ws.Row(p.Row).Height * 96.0 / 72.0;
            var finalW = (int)Math.Max(1, Math.Round(p.ImgW));
            var finalH = (int)Math.Max(1, Math.Round(p.ImgH));
            var ox = (int)Math.Max(0, Math.Round((cellW - finalW) / 2.0));
            var oy = (int)Math.Max(0, Math.Round((cellH - finalH) / 2.0));
            var cell = ws.Cell(p.Row, p.Col);
            p.Pic.MoveTo(cell, ox, oy, cell, ox + finalW, oy + finalH);
        }
    }

    private static XLPictureFormat DetectPictureFormat(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return XLPictureFormat.Jpeg;
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return XLPictureFormat.Png;
        if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
            return XLPictureFormat.Gif;
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
            return XLPictureFormat.Bmp;
        return XLPictureFormat.Unknown;
    }

    private sealed class ParsedImportRow
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public ZyqdCotractBaseModel? Row { get; init; }

        public static ParsedImportRow Success(ZyqdCotractBaseModel row) => new() { Ok = true, Row = row };
        public static ParsedImportRow Fail(string error) => new() { Ok = false, Error = error };
    }

    private static Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> IndexPictures(IXLWorksheet ws)
    {
        var map = new Dictionary<(int, int), (byte[], XLPictureFormat)>();
        foreach (var pic in ws.Pictures)
        {
            try
            {
                var row = pic.TopLeftCell.Address.RowNumber;
                var col = pic.TopLeftCell.Address.ColumnNumber;
                var src = pic.ImageStream;
                using var ms = new MemoryStream();
                if (src.CanSeek)
                    src.Position = 0;
                src.CopyTo(ms);
                var bytes = ms.ToArray();
                if (bytes.Length > 0)
                    map[(row, col)] = (bytes, pic.Format);
            }
            catch
            {
                // 单张图读失败不影响其它行
            }
        }

        return map;
    }

    private static bool IsImportRowEmpty(
        IXLWorksheet ws,
        int row,
        Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> pictures)
    {
        if (pictures.Keys.Any(k => k.Row == row))
            return false;

        var lastCol = Math.Max(ColManager, ws.LastColumnUsed()?.ColumnNumber() ?? ColManager);
        for (var col = 1; col <= lastCol; col++)
        {
            if (GetCellText(ws.Cell(row, col)).Length > 0)
                return false;
        }

        return true;
    }

    private async Task<ParsedImportRow> ParseImportRowAsync(
        IXLWorksheet ws,
        int excelRow,
        Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> pictures,
        CancellationToken ct)
    {
        var model = new ZyqdCotractBaseModel();
        try
        {
            model.Tuo_Mark = GetCellText(ws.Cell(excelRow, ColTuoMark));
            model.Customer_destination_country = GetCellText(ws.Cell(excelRow, ColCountry));
            model.ClientNo = GetCellText(ws.Cell(excelRow, ColClientNo));
            model.ClientName = GetCellText(ws.Cell(excelRow, ColClientName));
            model.materialNo = GetCellText(ws.Cell(excelRow, ColMaterialNo));
            model.materialName = GetCellText(ws.Cell(excelRow, ColMaterialName));
            model.materialType = GetCellText(ws.Cell(excelRow, ColMaterialType));
            model.materialModel = GetCellText(ws.Cell(excelRow, ColMaterialModel));
            model.materiaSpecs = GetCellText(ws.Cell(excelRow, ColSpecs));
            model.materiaPowered = GetCellText(ws.Cell(excelRow, ColPowered));
            model.materiaCust = GetCellText(ws.Cell(excelRow, ColCust));
            model.Notes = GetCellText(ws.Cell(excelRow, ColNotes));
            model.Quantity_unit = GetCellText(ws.Cell(excelRow, ColQtyUnit));
            model.currency_unit = GetCellText(ws.Cell(excelRow, ColCurrency));
            model.price_unit = GetCellText(ws.Cell(excelRow, ColPriceUnit));
            model.Box_label = GetCellText(ws.Cell(excelRow, ColBoxLabel));
            model.Box_label2 = GetCellText(ws.Cell(excelRow, ColBoxLabel2));
            model.Client_poNo = GetCellText(ws.Cell(excelRow, ColPoNo));
            model.SAP_order_NO = GetCellText(ws.Cell(excelRow, ColSapNo));
            model.Packing_method = GetCellText(ws.Cell(excelRow, ColPacking));
            model.Packing_method_unit = GetCellText(ws.Cell(excelRow, ColPackingUnit));
            model.Carton_Size = GetCellText(ws.Cell(excelRow, ColCarton));
            model.Pallet_Size = GetCellText(ws.Cell(excelRow, ColPallet));
            model.palletizing_method = GetCellText(ws.Cell(excelRow, ColPalletizing));
            model.palletizing_method_unit = GetCellText(ws.Cell(excelRow, ColPalletizingUnit));
            model.Customer_Manager = GetCellText(ws.Cell(excelRow, ColManager));

            if (!TryGetDouble(ws.Cell(excelRow, ColPrice), out var price, out var priceErr) && priceErr is not null)
                return ParsedImportRow.Fail($"第{excelRow}行，单价不是有效数字");
            model.price = price;

            if (!TryGetDouble(ws.Cell(excelRow, ColNetWeight), out var net, out var netErr) && netErr is not null)
                return ParsedImportRow.Fail($"第{excelRow}行，净重不是有效数字");
            model.Net_weight = net;

            if (!TryGetDouble(ws.Cell(excelRow, ColGrossWeight), out var gross, out var grossErr) && grossErr is not null)
                return ParsedImportRow.Fail($"第{excelRow}行，毛重不是有效数字");
            model.gross_weight = gross;

            model.Customer_outer_box_logo = await ResolveImageOrTextAsync(
                ws, excelRow, ColOuterBoxLogo, pictures, allowPlainText: true, ct);
            model.Tuo_Mark_url = await ResolveImageOrTextAsync(
                ws, excelRow, ColTuoMarkUrl, pictures, allowPlainText: true, ct);
            model.Box_label_url = await ResolveImageOrTextAsync(
                ws, excelRow, ColBoxLabelUrl, pictures, allowPlainText: true, ct);
            model.Box_label_url2 = await ResolveImageOrTextAsync(
                ws, excelRow, ColBoxLabelUrl2, pictures, allowPlainText: true, ct);
            model.nameplate_url = await ResolveImageOrTextAsync(
                ws, excelRow, ColNameplate, pictures, allowPlainText: true, ct);
            model.nameplate_url2 = await ResolveImageOrTextAsync(
                ws, excelRow, ColNameplate2, pictures, allowPlainText: true, ct);

            return ParsedImportRow.Success(model);
        }
        catch (Exception ex)
        {
            return ParsedImportRow.Fail($"第{excelRow}行解析失败：{ex.Message}");
        }
    }

    private async Task<string?> ResolveImageOrTextAsync(
        IXLWorksheet ws,
        int row,
        int col,
        Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> pictures,
        bool allowPlainText,
        CancellationToken ct)
    {
        if (pictures.TryGetValue((row, col), out var pic))
        {
            var saved = await SaveLegacyImageAsync(pic.Bytes, pic.Format, ct);
            if (!string.IsNullOrEmpty(saved))
                return saved;
        }

        if (!allowPlainText)
            return null;

        var text = GetCellText(ws.Cell(row, col));
        return string.IsNullOrEmpty(text) ? null : text;
    }

    public async Task<string> UploadImageAsync(Stream stream, string? fileName, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        var format = ext switch
        {
            ".jpg" or ".jpeg" => XLPictureFormat.Jpeg,
            ".gif" => XLPictureFormat.Gif,
            ".bmp" => XLPictureFormat.Bmp,
            _ => XLPictureFormat.Png
        };
        var path = await SaveLegacyImageAsync(bytes, format, ct);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("不是有效的图片文件");
        return path;
    }

    private async Task<string?> SaveLegacyImageAsync(byte[] bytes, XLPictureFormat format, CancellationToken ct)
    {
        if (bytes.Length == 0 || !LooksLikeImage(bytes))
            return null;

        var ext = format switch
        {
            XLPictureFormat.Jpeg => ".jpg",
            XLPictureFormat.Png => ".png",
            XLPictureFormat.Gif => ".gif",
            XLPictureFormat.Bmp => ".bmp",
            _ => DetectImageExtension(bytes)
        };

        var fileName = Guid.NewGuid().ToString("N") + ext;
        var uploadUrl = _imageBase + LegacyImgUploadPath + "?ID=&file_text=file1";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeFromExt(ext));
        content.Add(fileContent, "file1", fileName);

        var http = _httpFactory.CreateClient(LegacyHttpClient);
        using var resp = await http.PostAsync(uploadUrl, content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"老站图片上传失败 HTTP {(int)resp.StatusCode}：{TrimBody(body)}");

        var path = ParseLegacyUploadPath(body);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("老站图片上传未返回路径：" + TrimBody(body));

        return path.Trim();
    }

    private static string MediaTypeFromExt(string ext) => ext.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        _ => "image/png"
    };

    private static string? ParseLegacyUploadPath(string body)
    {
        var json = (body ?? "").Trim();
        if (json.Length == 0) return null;
        if (json.StartsWith('"') && json.EndsWith('"'))
        {
            try { json = JsonSerializer.Deserialize<string>(json) ?? json; }
            catch { /* 不是二次编码 */ }
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.Equals("filePath", StringComparison.OrdinalIgnoreCase))
                    return prop.Value.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string TrimBody(string body)
    {
        var t = (body ?? "").Trim().Replace('\r', ' ').Replace('\n', ' ');
        return t.Length <= 200 ? t : t[..200];
    }

    private static string DetectImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return ".jpg";
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";
        if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return ".gif";
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) return ".bmp";
        return ".png";
    }

    private static string GetCellText(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        try
        {
            return (cell.GetFormattedString() ?? "").Trim();
        }
        catch
        {
            return cell.GetValue<string>()?.Trim() ?? "";
        }
    }

    private static bool TryGetDouble(IXLCell cell, out double value, out string? error)
    {
        value = 0;
        error = null;
        if (cell.IsEmpty())
            return false;

        if (cell.DataType == XLDataType.Number)
        {
            value = cell.GetDouble();
            return true;
        }

        var s = GetCellText(cell);
        if (s.Length == 0)
            return false;

        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
            || double.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("zh-CN"), out value))
            return true;

        error = "invalid";
        return false;
    }

    public static string? Validate(ZyqdCotractBaseModel item)
    {
        if (string.IsNullOrWhiteSpace(item.Customer_outer_box_logo))
            return "外箱标志为空且不为 N/A";

        if (string.IsNullOrWhiteSpace(item.Tuo_Mark) && string.IsNullOrWhiteSpace(item.Tuo_Mark_url))
            return "托唛文字或图片其一必填";
        if (string.IsNullOrWhiteSpace(item.Customer_destination_country))
            return "客户目的国必填";
        if (string.IsNullOrWhiteSpace(item.ClientNo))
            return "客户编号必填";
        if (string.IsNullOrWhiteSpace(item.ClientName))
            return "客户名称必填";
        if (string.IsNullOrWhiteSpace(item.materialNo))
            return "物料编号必填";
        if (string.IsNullOrWhiteSpace(item.materialName))
            return "物料名称必填";
        if (string.IsNullOrWhiteSpace(item.materiaSpecs))
            return "物料 specs 必填";
        if (string.IsNullOrWhiteSpace(item.materiaPowered))
            return "物料 Powered 必填";
        if (string.IsNullOrWhiteSpace(item.materiaCust))
            return "物料 Cust 必填";
        if (string.IsNullOrWhiteSpace(item.Quantity_unit))
            return "数量单位必填";
        if (string.IsNullOrWhiteSpace(item.currency_unit))
            return "货币单位必填";
        if (item.price == 0)
            return "单价必填";
        if (string.IsNullOrWhiteSpace(item.price_unit))
            return "单价单位必填";
        if (string.IsNullOrWhiteSpace(item.Box_label) && string.IsNullOrWhiteSpace(item.Box_label_url))
            return "箱唛文字或图片其一必填";
        if (string.IsNullOrWhiteSpace(item.nameplate_url) && string.IsNullOrWhiteSpace(item.nameplate_url2))
            return "铭牌图片其一必填";
        if (string.IsNullOrWhiteSpace(item.Packing_method))
            return "打包方式必填";
        if (string.IsNullOrWhiteSpace(item.Packing_method_unit))
            return "打包方式单位必填";
        if (string.IsNullOrWhiteSpace(item.Carton_Size))
            return "纸箱尺寸必填";
        if (string.IsNullOrWhiteSpace(item.Pallet_Size))
            return "托盘尺寸必填";
        if (string.IsNullOrWhiteSpace(item.palletizing_method))
            return "打托方式必填";
        if (string.IsNullOrWhiteSpace(item.palletizing_method_unit))
            return "打托方式单位必填";
        if (item.Net_weight == 0)
            return "净重必填";
        if (item.gross_weight == 0)
            return "毛重必填";
        if (string.IsNullOrWhiteSpace(item.Customer_Manager))
            return "客户负责人必填";
        return null;
    }

    private IQueryable<ZyqdCotractBaseModel> BuildFiltered(CotractBaseQueryDto query)
    {
        var q = ApplyScope(_db.CotractBases.AsNoTracking(), query);

        if (string.Equals(query.FilterColumn, "CreateTime", StringComparison.Ordinal))
            return FilterCreateTimeRange(q, query.DateStart, query.DateEnd);

        var keyword = query.Keyword?.Trim();
        if (string.IsNullOrEmpty(keyword))
            return q;

        return query.FilterColumn switch
        {
            "ClientName" => q.Where(x => x.ClientName != null && x.ClientName.Contains(keyword)),
            "materialNo" => q.Where(x => x.materialNo != null && x.materialNo.Contains(keyword)),
            "materialName" => q.Where(x => x.materialName != null && x.materialName.Contains(keyword)),
            "Tuo_Mark" => q.Where(x => x.Tuo_Mark != null && x.Tuo_Mark.Contains(keyword)),
            "Customer_destination_country" => q.Where(x => x.Customer_destination_country != null && x.Customer_destination_country.Contains(keyword)),
            "materialType" => q.Where(x => x.materialType != null && x.materialType.Contains(keyword)),
            "materialModel" => q.Where(x => x.materialModel != null && x.materialModel.Contains(keyword)),
            "materiaSpecs" => q.Where(x => x.materiaSpecs != null && x.materiaSpecs.Contains(keyword)),
            "materiaPowered" => q.Where(x => x.materiaPowered != null && x.materiaPowered.Contains(keyword)),
            "materiaCust" => q.Where(x => x.materiaCust != null && x.materiaCust.Contains(keyword)),
            "Notes" => q.Where(x => x.Notes != null && x.Notes.Contains(keyword)),
            "Quantity_unit" => q.Where(x => x.Quantity_unit != null && x.Quantity_unit.Contains(keyword)),
            "currency_unit" => q.Where(x => x.currency_unit != null && x.currency_unit.Contains(keyword)),
            "price" => FilterPrice(q, keyword),
            "price_unit" => q.Where(x => x.price_unit != null && x.price_unit.Contains(keyword)),
            "Box_label" => q.Where(x => x.Box_label != null && x.Box_label.Contains(keyword)),
            "Box_label2" => q.Where(x => x.Box_label2 != null && x.Box_label2.Contains(keyword)),
            "Client_poNo" => q.Where(x => x.Client_poNo != null && x.Client_poNo.Contains(keyword)),
            "SAP_order_NO" => q.Where(x => x.SAP_order_NO != null && x.SAP_order_NO.Contains(keyword)),
            "Packing_method" => q.Where(x => x.Packing_method != null && x.Packing_method.Contains(keyword)),
            "Packing_method_unit" => q.Where(x => x.Packing_method_unit != null && x.Packing_method_unit.Contains(keyword)),
            "Carton_Size" => q.Where(x => x.Carton_Size != null && x.Carton_Size.Contains(keyword)),
            "Pallet_Size" => q.Where(x => x.Pallet_Size != null && x.Pallet_Size.Contains(keyword)),
            "palletizing_method" => q.Where(x => x.palletizing_method != null && x.palletizing_method.Contains(keyword)),
            "palletizing_method_unit" => q.Where(x => x.palletizing_method_unit != null && x.palletizing_method_unit.Contains(keyword)),
            "Net_weight" => FilterNetWeight(q, keyword),
            "gross_weight" => FilterGrossWeight(q, keyword),
            "Customer_Manager" => q.Where(x => x.Customer_Manager != null && x.Customer_Manager.Contains(keyword)),
            "CreatePerson" => q.Where(x => x.CreatePerson != null && x.CreatePerson.Contains(keyword)),
            _ => q.Where(x => x.ClientNo != null && x.ClientNo.Contains(keyword))
        };
    }

    private static bool TryParseDouble(string keyword, out double value)
        => double.TryParse(keyword, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
           || double.TryParse(keyword, NumberStyles.Any, CultureInfo.CurrentCulture, out value);

    private static IQueryable<ZyqdCotractBaseModel> FilterPrice(IQueryable<ZyqdCotractBaseModel> q, string keyword)
        => TryParseDouble(keyword, out var n) ? q.Where(x => x.price == n) : q.Where(_ => false);

    private static IQueryable<ZyqdCotractBaseModel> FilterNetWeight(IQueryable<ZyqdCotractBaseModel> q, string keyword)
        => TryParseDouble(keyword, out var n) ? q.Where(x => x.Net_weight == n) : q.Where(_ => false);

    private static IQueryable<ZyqdCotractBaseModel> FilterGrossWeight(IQueryable<ZyqdCotractBaseModel> q, string keyword)
        => TryParseDouble(keyword, out var n) ? q.Where(x => x.gross_weight == n) : q.Where(_ => false);

    private static IQueryable<ZyqdCotractBaseModel> FilterCreateTimeRange(
        IQueryable<ZyqdCotractBaseModel> q, DateTime? start, DateTime? end)
    {
        if (start is DateTime s)
        {
            var from = s.Date;
            q = q.Where(x => x.CreateTime != null && x.CreateTime >= from);
        }

        if (end is DateTime e)
        {
            var to = e.Date.AddDays(1);
            q = q.Where(x => x.CreateTime != null && x.CreateTime < to);
        }

        return q;
    }

    private static IQueryable<ZyqdCotractBaseModel> Sorted(
        IQueryable<ZyqdCotractBaseModel> q, string? sortLabel = null, bool desc = true)
    {
        IOrderedQueryable<ZyqdCotractBaseModel> ordered = sortLabel switch
        {
            "Tuo_Mark" => desc ? q.OrderByDescending(x => x.Tuo_Mark) : q.OrderBy(x => x.Tuo_Mark),
            "Customer_destination_country" => desc ? q.OrderByDescending(x => x.Customer_destination_country) : q.OrderBy(x => x.Customer_destination_country),
            "ClientNo" => desc ? q.OrderByDescending(x => x.ClientNo) : q.OrderBy(x => x.ClientNo),
            "ClientName" => desc ? q.OrderByDescending(x => x.ClientName) : q.OrderBy(x => x.ClientName),
            "materialNo" => desc ? q.OrderByDescending(x => x.materialNo) : q.OrderBy(x => x.materialNo),
            "materialName" => desc ? q.OrderByDescending(x => x.materialName) : q.OrderBy(x => x.materialName),
            "materialType" => desc ? q.OrderByDescending(x => x.materialType) : q.OrderBy(x => x.materialType),
            "materialModel" => desc ? q.OrderByDescending(x => x.materialModel) : q.OrderBy(x => x.materialModel),
            "materiaSpecs" => desc ? q.OrderByDescending(x => x.materiaSpecs) : q.OrderBy(x => x.materiaSpecs),
            "materiaPowered" => desc ? q.OrderByDescending(x => x.materiaPowered) : q.OrderBy(x => x.materiaPowered),
            "materiaCust" => desc ? q.OrderByDescending(x => x.materiaCust) : q.OrderBy(x => x.materiaCust),
            "Notes" => desc ? q.OrderByDescending(x => x.Notes) : q.OrderBy(x => x.Notes),
            "Quantity_unit" => desc ? q.OrderByDescending(x => x.Quantity_unit) : q.OrderBy(x => x.Quantity_unit),
            "currency_unit" => desc ? q.OrderByDescending(x => x.currency_unit) : q.OrderBy(x => x.currency_unit),
            "price" => desc ? q.OrderByDescending(x => x.price) : q.OrderBy(x => x.price),
            "price_unit" => desc ? q.OrderByDescending(x => x.price_unit) : q.OrderBy(x => x.price_unit),
            "Box_label" => desc ? q.OrderByDescending(x => x.Box_label) : q.OrderBy(x => x.Box_label),
            "Box_label2" => desc ? q.OrderByDescending(x => x.Box_label2) : q.OrderBy(x => x.Box_label2),
            "Client_poNo" => desc ? q.OrderByDescending(x => x.Client_poNo) : q.OrderBy(x => x.Client_poNo),
            "SAP_order_NO" => desc ? q.OrderByDescending(x => x.SAP_order_NO) : q.OrderBy(x => x.SAP_order_NO),
            "Packing_method" => desc ? q.OrderByDescending(x => x.Packing_method) : q.OrderBy(x => x.Packing_method),
            "Packing_method_unit" => desc ? q.OrderByDescending(x => x.Packing_method_unit) : q.OrderBy(x => x.Packing_method_unit),
            "Carton_Size" => desc ? q.OrderByDescending(x => x.Carton_Size) : q.OrderBy(x => x.Carton_Size),
            "Pallet_Size" => desc ? q.OrderByDescending(x => x.Pallet_Size) : q.OrderBy(x => x.Pallet_Size),
            "palletizing_method" => desc ? q.OrderByDescending(x => x.palletizing_method) : q.OrderBy(x => x.palletizing_method),
            "palletizing_method_unit" => desc ? q.OrderByDescending(x => x.palletizing_method_unit) : q.OrderBy(x => x.palletizing_method_unit),
            "Net_weight" => desc ? q.OrderByDescending(x => x.Net_weight) : q.OrderBy(x => x.Net_weight),
            "gross_weight" => desc ? q.OrderByDescending(x => x.gross_weight) : q.OrderBy(x => x.gross_weight),
            "Customer_Manager" => desc ? q.OrderByDescending(x => x.Customer_Manager) : q.OrderBy(x => x.Customer_Manager),
            "CreatePerson" => desc ? q.OrderByDescending(x => x.CreatePerson) : q.OrderBy(x => x.CreatePerson),
            "CreateTime" => desc ? q.OrderByDescending(x => x.CreateTime) : q.OrderBy(x => x.CreateTime),
            _ => q.OrderByDescending(x => x.CreateTime)
        };
        return ordered.ThenByDescending(x => x.Id);
    }

    private static IQueryable<ZyqdCotractBaseModel> ApplyScope(IQueryable<ZyqdCotractBaseModel> q, CotractBaseQueryDto query)
    {
        if (query.ViewAllCustomers)
            return q;

        var empNo = (query.ViewerEmpNo ?? "").Trim();
        var codes = query.AssignedCustomerCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return q.Where(x =>
            (x.ClientNo != null && codes.Contains(x.ClientNo))
            || (empNo.Length > 0 && x.Customer_Manager != null && x.Customer_Manager.Contains(empNo)));
    }

    private static void Normalize(ZyqdCotractBaseModel m)
    {
        m.ClientNo = m.ClientNo?.Trim();
        m.ClientName = m.ClientName?.Trim();
        m.materialNo = m.materialNo?.Trim();
        m.materialName = m.materialName?.Trim();
        m.Customer_Manager = m.Customer_Manager?.Trim();
    }

    private async Task<bool> ExistsSameClientMaterialAsync(ZyqdCotractBaseModel model, CancellationToken ct)
    {
        var clientNo = (model.ClientNo ?? "").Trim();
        var materialNo = (model.materialNo ?? "").Trim();
        if (clientNo.Length == 0 || materialNo.Length == 0)
            return false;

        var q = _db.CotractBases.AsNoTracking().Where(x =>
            x.ClientNo != null && x.materialNo != null
            && x.ClientNo.ToLower() == clientNo.ToLower()
            && x.materialNo.ToLower() == materialNo.ToLower());

        if (model.Id > 0)
            q = q.Where(x => x.Id != model.Id);

        return await q.AnyAsync(ct);
    }

    private static void CopyFields(ZyqdCotractBaseModel from, ZyqdCotractBaseModel to)
    {
        to.Customer_outer_box_logo = from.Customer_outer_box_logo;
        to.Tuo_Mark = from.Tuo_Mark;
        to.Tuo_Mark_url = from.Tuo_Mark_url;
        to.Customer_destination_country = from.Customer_destination_country;
        to.ClientNo = from.ClientNo;
        to.ClientName = from.ClientName;
        to.materialNo = from.materialNo;
        to.materialName = from.materialName;
        to.materialType = from.materialType;
        to.materialModel = from.materialModel;
        to.materiaSpecs = from.materiaSpecs;
        to.materiaPowered = from.materiaPowered;
        to.materiaColor = from.materiaColor;
        to.materiaCust = from.materiaCust;
        to.materiaCustLogo = from.materiaCustLogo;
        to.RadFreq = from.RadFreq;
        to.inBox = from.inBox;
        to.MstCtn = from.MstCtn;
        to.AI = from.AI;
        to.UNIT = from.UNIT;
        to.price = from.price;
        to.Box_label = from.Box_label;
        to.Box_label_url = from.Box_label_url;
        to.Box_label2 = from.Box_label2;
        to.Box_label_url2 = from.Box_label_url2;
        to.Client_poNo = from.Client_poNo;
        to.Client_Model = from.Client_Model;
        to.nameplate_url = from.nameplate_url;
        to.nameplate_url2 = from.nameplate_url2;
        to.SAP_order_NO = from.SAP_order_NO;
        to.SAP_Material_NO = from.SAP_Material_NO;
        to.SAP_Material_Name = from.SAP_Material_Name;
        to.Packing_method = from.Packing_method;
        to.Packing_method_unit = from.Packing_method_unit;
        to.Carton_Size = from.Carton_Size;
        to.Pallet_Size = from.Pallet_Size;
        to.Net_weight = from.Net_weight;
        to.gross_weight = from.gross_weight;
        to.Customer_Manager = from.Customer_Manager;
        to.palletizing_method = from.palletizing_method;
        to.palletizing_method_unit = from.palletizing_method_unit;
        to.currency_unit = from.currency_unit;
        to.Quantity_unit = from.Quantity_unit;
        to.price_unit = from.price_unit;
        to.Notes = from.Notes;
        to.var1 = from.var1;
        to.var2 = from.var2;
        to.var3 = from.var3;
        to.var4 = from.var4;
        to.var5 = from.var5;
        to.var6 = from.var6;
        to.var7 = from.var7;
        to.var8 = from.var8;
        to.var9 = from.var9;
        to.var10 = from.var10;
        to.Remark = from.Remark;
    }
}
