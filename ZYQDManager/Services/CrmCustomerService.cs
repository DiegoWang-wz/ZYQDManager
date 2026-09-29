using Microsoft.Data.SqlClient;
using ZYQDManager.DataDtos;

namespace ZYQDManager.Services;

public class CrmCustomerService
{
    private readonly string _conn;
    private readonly ILogger<CrmCustomerService> _logger;

    public CrmCustomerService(IConfiguration config, ILogger<CrmCustomerService> logger)
    {
        _conn = config.GetConnectionString("CRM_MSCRM") ?? "";
        _logger = logger;
    }

    public async Task<CrmClientDto?> GetBillToAsync(string? clientNo, CancellationToken ct = default)
    {
        clientNo = CotractPiTypes.NormalizeClientNo(clientNo);
        if (clientNo.Length == 0)
            return null;
        EnsureConn();

        const string sql = """
            SELECT TOP(1)
                AccountNumber AS ClientNo,
                new_address AS Address,
                Name AS ClientName,
                b.new_enname AS Country,
                ISNULL(a.telephone1, a.new_telephone) AS Tel,
                c.FullName,
                a.EMailAddress1,
                new_shortname
            FROM dbo.Account AS a
            LEFT JOIN dbo.new_country AS b ON new_country_idName = new_name
            LEFT JOIN dbo.contact AS c ON c.AccountId = a.AccountId
            WHERE a.AccountNumber = @ClientNo
            """;

        try
        {
            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientNo", clientNo);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;
            return ReadBill(reader, clientNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRM GetBillTo failed, ClientNo={ClientNo}", clientNo);
            throw new InvalidOperationException("读取 CRM 付款方失败：" + ex.Message, ex);
        }
    }

    public async Task<List<CrmClientDto>> GetShipToAsync(string? clientNo, CancellationToken ct = default)
    {
        clientNo = CotractPiTypes.NormalizeClientNo(clientNo);
        var list = new List<CrmClientDto>();
        if (clientNo.Length == 0)
            return list;
        EnsureConn();

        const string sql = """
            SELECT
                b.new_name AS ClientNo,
                b.new_address AS Address,
                b.new_Delivery AS ClientName,
                ISNULL(b.new_province_idName, '') + ISNULL(b.new_cityname, '') AS Country,
                b.new_contactphone AS Tel,
                b.new_contact AS FullName
            FROM dbo.Account AS a
            LEFT JOIN dbo.new_address AS b ON a.AccountId = b.new_account_id
            WHERE a.AccountNumber = @ClientNo
            """;

        try
        {
            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientNo", clientNo);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var row = ReadShip(reader);
                if (string.IsNullOrWhiteSpace(row.ClientNo) && string.IsNullOrWhiteSpace(row.ClientName))
                    continue;
                list.Add(row);
            }
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRM GetShipTo failed, ClientNo={ClientNo}", clientNo);
            throw new InvalidOperationException("读取 CRM 送达方失败：" + ex.Message, ex);
        }
    }

    private void EnsureConn()
    {
        if (string.IsNullOrWhiteSpace(_conn))
            throw new InvalidOperationException("未配置 ConnectionStrings:CRM_MSCRM");
    }

    private static CrmClientDto ReadBill(SqlDataReader reader, string clientNo)
    {
        return new CrmClientDto
        {
            ClientNo = Str(reader, "ClientNo") ?? clientNo,
            Address = Str(reader, "Address"),
            ClientName = Str(reader, "ClientName"),
            Country = Str(reader, "Country"),
            Tel = Str(reader, "Tel"),
            FullName = Str(reader, "FullName"),
            Mail = Str(reader, "EMailAddress1"),
            ShortName = Str(reader, "new_shortname")
        };
    }

    private static CrmClientDto ReadShip(SqlDataReader reader)
    {
        var country = Str(reader, "Country") ?? "";
        if (country.Contains('/'))
        {
            var parts = country.Split('/', StringSplitOptions.RemoveEmptyEntries);
            country = parts.Length > 1 ? parts[1] : parts[0];
        }

        return new CrmClientDto
        {
            ClientNo = Str(reader, "ClientNo"),
            Address = Str(reader, "Address"),
            ClientName = Str(reader, "ClientName"),
            Country = country,
            Tel = Str(reader, "Tel"),
            FullName = Str(reader, "FullName")
        };
    }

    private static string? Str(SqlDataReader reader, string name)
    {
        try
        {
            var i = reader.GetOrdinal(name);
            if (reader.IsDBNull(i)) return null;
            var v = reader.GetValue(i)?.ToString()?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
