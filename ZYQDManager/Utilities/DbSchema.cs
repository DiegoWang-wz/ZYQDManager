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
    }
}
