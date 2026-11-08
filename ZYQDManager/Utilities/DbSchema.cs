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
                        Qty DECIMAL(18,4) NULL,
                        Price DECIMAL(18,4) NULL,
                        Discount DECIMAL(18,4) NULL,
                        Amount DECIMAL(18,4) NULL,
                        Qty2 DECIMAL(18,4) NULL,
                        Price2 DECIMAL(18,4) NULL,
                        Discount2 DECIMAL(18,4) NULL,
                        Amount2 DECIMAL(18,4) NULL,
                        Qty3 DECIMAL(18,4) NULL,
                        Price3 DECIMAL(18,4) NULL,
                        Discount3 DECIMAL(18,4) NULL,
                        Amount3 DECIMAL(18,4) NULL,
                        Qty4 DECIMAL(18,4) NULL,
                        Price4 DECIMAL(18,4) NULL,
                        Discount4 DECIMAL(18,4) NULL,
                        Amount4 DECIMAL(18,4) NULL,
                        Note NVARCHAR(MAX) NULL,
                        SortOrder INT NOT NULL CONSTRAINT DF_T_ZYQD_Quotation_detail_Sort DEFAULT 0,
                        createTime DATETIME2 NULL
                    );
                    CREATE INDEX IX_T_ZYQD_Quotation_detail_main ON dbo.T_ZYQD_Quotation_detail(main_guid);
                END

                IF OBJECT_ID(N'dbo.T_ZYQD_Quotation_detail', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.T_ZYQD_Quotation_detail', N'Qty2') IS NULL
                BEGIN
                    ALTER TABLE dbo.T_ZYQD_Quotation_detail ADD
                        Qty2 DECIMAL(18,4) NULL,
                        Price2 DECIMAL(18,4) NULL,
                        Discount2 DECIMAL(18,4) NULL,
                        Amount2 DECIMAL(18,4) NULL,
                        Qty3 DECIMAL(18,4) NULL,
                        Price3 DECIMAL(18,4) NULL,
                        Discount3 DECIMAL(18,4) NULL,
                        Amount3 DECIMAL(18,4) NULL,
                        Qty4 DECIMAL(18,4) NULL,
                        Price4 DECIMAL(18,4) NULL,
                        Discount4 DECIMAL(18,4) NULL,
                        Amount4 DECIMAL(18,4) NULL;
                END
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure T_ZYQD_Quotation_main/detail failed");
        }

        try
        {
            await EnsureRemoteWalkTimerItemAsync(db, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure remote walk-timer test item failed");
        }
    }

    /// <summary>
    /// 遥控器测试(新) 四类产品：在「定时设置」下插入「定时遥控器走时精度测试」，后续编号顺延。
    /// 已有实测结果按原编号一起后移，避免挂到新项目上。
    /// </summary>
    private static async Task EnsureRemoteWalkTimerItemAsync(SqlServerDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_SIR_item_template', N'U') IS NULL
                RETURN;

            IF EXISTS (
                SELECT 1 FROM dbo.T_ZYQD_SIR_item_template
                WHERE Report_type = N'remote_new'
                  AND Specification = N'定时遥控器走时精度测试'
            )
                RETURN;

            IF NOT EXISTS (
                SELECT 1 FROM dbo.T_ZYQD_SIR_item_template
                WHERE Report_type = N'remote_new'
                  AND product_type = N'充电款单向遥控器'
                  AND Specification_type = N'M.22'
                  AND Specification LIKE N'充电功能%'
            )
                RETURN;

            UPDATE dbo.T_ZYQD_SIR_item_template
            SET Specification_type = N'M.' + CAST(TRY_CAST(SUBSTRING(Specification_type, 3, 20) AS int) + 1 AS nvarchar(20)),
                SortNo = (TRY_CAST(SUBSTRING(Specification_type, 3, 20) AS int) + 1) * 10
            WHERE Report_type = N'remote_new'
              AND product_type IN (N'充电款单向遥控器', N'充电款双向遥控器', N'一次电池款单向遥控器', N'一次电池款双向遥控器')
              AND Specification_type LIKE N'M.%'
              AND CHARINDEX(N'.', SUBSTRING(Specification_type, 3, 20)) = 0
              AND TRY_CAST(SUBSTRING(Specification_type, 3, 20) AS int) >= 22;

            UPDATE d
            SET d.Specification_type = N'M.' + CAST(TRY_CAST(SUBSTRING(d.Specification_type, 3, 20) AS int) + 1 AS nvarchar(20))
            FROM dbo.T_ZYQD_SIR_detail d
            INNER JOIN dbo.T_ZYQD_SIR_main m ON m.guid = d.main_guid
            WHERE m.Report_type = N'remote_new'
              AND m.product_type IN (N'充电款单向遥控器', N'充电款双向遥控器', N'一次电池款单向遥控器', N'一次电池款双向遥控器')
              AND ISNULL(d.deleteSigh, N'') <> N'X'
              AND d.Specification_type LIKE N'M.%'
              AND CHARINDEX(N'.', SUBSTRING(d.Specification_type, 3, 20)) = 0
              AND TRY_CAST(SUBSTRING(d.Specification_type, 3, 20) AS int) >= 22;

            INSERT INTO dbo.T_ZYQD_SIR_item_template
                (Report_type, product_type, Specification_type, SortNo, Specification, Specification_EN, Criteria, Criteria_EN, filling_type, Enabled, createTime)
            SELECT v.Report_type, v.product_type, N'M.22', 220,
                   N'定时遥控器走时精度测试',
                   N'Timed remote control precision test while walking',
                   N'1. 以北京时间为基准，设置遥控器时间；' + NCHAR(10) + N'2.1h 后校对，时间差≤±1s；',
                   N'1. Set the remote control time based on Beijing time;' + NCHAR(10) + N'2.1 hours, the time difference is ≤±1s.',
                   N'0', 1, SYSUTCDATETIME()
            FROM (VALUES
                (N'remote_new', N'充电款单向遥控器'),
                (N'remote_new', N'充电款双向遥控器'),
                (N'remote_new', N'一次电池款单向遥控器'),
                (N'remote_new', N'一次电池款双向遥控器')
            ) v(Report_type, product_type);
            """, ct);
    }
}
