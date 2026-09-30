using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataModels;

namespace ZYQDManager.Utilities;

public static class DbSchema
{
    public static async Task EnsureAsync(SqlServerDbContext db, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.ZYQD_UserCustomerAssign', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ZYQD_UserCustomerAssign (
                        ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        EmpNo NVARCHAR(20) NOT NULL,
                        CustomerCode NVARCHAR(50) NOT NULL,
                        CustomerName NVARCHAR(200) NULL,
                        CreateTime DATETIME2 NOT NULL CONSTRAINT DF_ZYQD_UserCustomerAssign_CreateTime DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT UQ_ZYQD_UserCustomerAssign UNIQUE (EmpNo, CustomerCode)
                    );
                    CREATE INDEX IX_ZYQD_UserCustomerAssign_EmpNo ON dbo.ZYQD_UserCustomerAssign(EmpNo);
                    CREATE INDEX IX_ZYQD_UserCustomerAssign_CustomerCode ON dbo.ZYQD_UserCustomerAssign(CustomerCode);
                END
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure ZYQD_UserCustomerAssign failed");
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.T_ZYQD_Quotation_main', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.T_ZYQD_Quotation_main (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        guid NVARCHAR(64) NOT NULL,
                        ClientCode NVARCHAR(50) NULL,
                        ClientName NVARCHAR(200) NULL,
                        Address NVARCHAR(500) NULL,
                        Tel NVARCHAR(100) NULL,
                        Mail NVARCHAR(200) NULL,
                        Attn NVARCHAR(100) NULL,
                        Delivery_Term NVARCHAR(200) NULL,
                        Payment_Term NVARCHAR(200) NULL,
                        Quotation_validity NVARCHAR(200) NULL,
                        Note NVARCHAR(MAX) NULL,
                        signature NVARCHAR(200) NULL,
                        signature_url NVARCHAR(500) NULL,
                        Quota_No NVARCHAR(100) NULL,
                        [DATE] NVARCHAR(50) NULL,
                        personId NVARCHAR(50) NULL,
                        rate NVARCHAR(50) NULL,
                        createTime DATETIME2 NULL,
                        currencyUnit NVARCHAR(20) NULL
                    );
                    CREATE UNIQUE INDEX IX_T_ZYQD_Quotation_main_guid ON dbo.T_ZYQD_Quotation_main(guid);
                END

                IF OBJECT_ID(N'dbo.T_ZYQD_Quotation_detail', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.T_ZYQD_Quotation_detail (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        main_guid NVARCHAR(64) NOT NULL,
                        ItemType NVARCHAR(20) NOT NULL,
                        BaseId INT NULL,
                        Part_No NVARCHAR(100) NULL,
                        Quotation_logo NVARCHAR(500) NULL,
                        Qty DECIMAL(18,4) NULL,
                        Price DECIMAL(18,4) NULL,
                        Discount DECIMAL(18,4) NULL,
                        Amount DECIMAL(18,4) NULL,
                        Note NVARCHAR(MAX) NULL,
                        SortOrder INT NOT NULL CONSTRAINT DF_T_ZYQD_Quotation_detail_Sort DEFAULT 0,
                        createTime DATETIME2 NULL
                    );
                    CREATE INDEX IX_T_ZYQD_Quotation_detail_main ON dbo.T_ZYQD_Quotation_detail(main_guid);
                END
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure T_ZYQD_Quotation_main/detail failed");
        }
    }
}
