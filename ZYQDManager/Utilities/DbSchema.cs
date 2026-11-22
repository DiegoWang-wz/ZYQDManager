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

        // 员工基础信息表：业务表只存工号，姓名/部门/职位统一从这里 join 出来
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.T_ZYQD_Employee', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.T_ZYQD_Employee (
                        EmpNo NVARCHAR(20) NOT NULL,
                        EmpName NVARCHAR(50) NULL,
                        Department NVARCHAR(100) NULL,
                        Position NVARCHAR(50) NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_T_ZYQD_Employee_IsActive DEFAULT 1,
                        LastSyncTime DATETIME2 NOT NULL CONSTRAINT DF_T_ZYQD_Employee_LastSyncTime DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_T_ZYQD_Employee PRIMARY KEY CLUSTERED (EmpNo)
                    );
                    CREATE INDEX IX_T_ZYQD_Employee_Name ON dbo.T_ZYQD_Employee(EmpName);
                    CREATE INDEX IX_T_ZYQD_Employee_Dept ON dbo.T_ZYQD_Employee(Department);
                END
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure T_ZYQD_Employee failed");
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
            await EnsureMotorNodeAsync(db, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure T_ZYQD_ProjectVersion node columns failed");
        }

        try
        {
            await EnsureRemoteWalkTimerItemAsync(db, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure remote walk-timer test item failed");
        }

        try
        {
            await EnsureRejectFromAsync(db, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ensure T_ZYQD_ApprovalFlow PrevStepCode failed");
        }
    }

    /// <summary>
    /// 版本表补流程节点列（幂等，只做结构）。
    ///
    /// 节点是流程的唯一权威，缺列会让所有列表/待办查询直接报错，
    /// 所以启动时就地补齐，不依赖人工执行脚本；新环境跑一遍即可。
    /// 这里只 ALTER / CREATE，不 UPDATE / INSERT 任何业务数据 ——
    /// 早期那几段「按单据回填节点」的 SQL 已移除（会抛 4104，且清库后无意义）。
    /// </summary>
    private static async Task EnsureMotorNodeAsync(SqlServerDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ProjectVersion', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'CurrentNode') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD
                        CurrentNode TINYINT NOT NULL
                            CONSTRAINT DF_T_ZYQD_ProjectVersion_CurrentNode DEFAULT 0;

                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'PendingEmpNo') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD PendingEmpNo NVARCHAR(50) NULL;

                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'PendingRole') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD PendingRole NVARCHAR(50) NULL;

                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'NodeEnterTime') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD NodeEnterTime DATETIME2 NULL;

                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'NodeUpdateEmpNo') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD NodeUpdateEmpNo NVARCHAR(50) NULL;

                -- 版本级研发经理：节点 3 提交审批时指定，节点 6 承认书审批沿用
                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'ManagerEmpNo') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD ManagerEmpNo NVARCHAR(50) NULL;

                -- 修改立项单时的逐字段修改点（JSON），研发工程师编规格书时弹窗展示
                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'ChangeDiff') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD ChangeDiff NVARCHAR(MAX) NULL;

                -- 承认书终审人（销售经理）：业务员提交承认书时选定，代替原先写死的蒋经理工号
                IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'FinalEmpNo') IS NULL
                    ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD FinalEmpNo NVARCHAR(50) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_T_ZYQD_ProjectVersion_Node'
                                 AND object_id = OBJECT_ID(N'dbo.T_ZYQD_ProjectVersion'))
                    CREATE INDEX IX_T_ZYQD_ProjectVersion_Node
                        ON dbo.T_ZYQD_ProjectVersion(CurrentNode, PendingEmpNo);
            END
            """, ct);

        // 说明：这里不再做「按单据推算 CurrentNode」的老数据回填。
        // 之前那版在 UPDATE 的 SET 里引用了只存在于上一条 INSERT 的 lx 别名，
        // 绑定不了会直接抛 4104，把整个启动迁移打断；而表清空后这段推算也没有意义。
        // 缺版本行的情况由 MotorFlowService.EnsureVersionAsync 在流转时兜底创建。


        // ---- 12 节点留痕：Node{n}EmpNo / Node{n}Status / Node{n}DoneTime，共 36 列 ----
        // Status 取值：-1 未开始 / 0 进行中 / 1 已完成
        // 必须用静态 DDL：动态拼接 + sp_executesql 在部分环境下会静默不生效；
        // 另外这段不能和下面引用新列的 UPDATE 合并成一次调用 ——
        // 同一批次里 UPDATE 的编译发生在加列之前，会直接报「列名无效」导致整批不执行。
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ProjectVersion', N'U') IS NULL RETURN;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node1EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node1EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node1Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node1Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node1Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node1DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node1DoneTime DATETIME2 NULL;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node2EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node2EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node2Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node2Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node2Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node2DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node2DoneTime DATETIME2 NULL;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node3EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node3EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node3Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node3Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node3Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node3DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node3DoneTime DATETIME2 NULL;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node4EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node4EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node4Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node4Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node4Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node4DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node4DoneTime DATETIME2 NULL;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node5EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node5EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node5Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node5Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node5Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node5DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node5DoneTime DATETIME2 NULL;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node6EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node6EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node6Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node6Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node6Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node6DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node6DoneTime DATETIME2 NULL;

            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node7EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node7EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node7Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node7Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node7Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node7DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node7DoneTime DATETIME2 NULL;

            -- 节点 8：提交审核（承认书）
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node8EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node8EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node8Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node8Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node8Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node8DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node8DoneTime DATETIME2 NULL;

            -- 节点 9：研发经理审核（承认书）
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node9EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node9EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node9Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node9Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node9Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node9DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node9DoneTime DATETIME2 NULL;

            -- 节点 10：销售经理审核（承认书）
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node10EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node10EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node10Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node10Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node10Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node10DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node10DoneTime DATETIME2 NULL;

            -- 节点 11：回传签名版承认书
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node11EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node11EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node11Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node11Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node11Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node11DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node11DoneTime DATETIME2 NULL;

            -- 节点 12：归档
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node12EmpNo') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node12EmpNo NVARCHAR(50) NULL;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node12Status') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node12Status INT NOT NULL
                    CONSTRAINT DF_T_ZYQD_ProjectVersion_Node12Status DEFAULT -1;
            IF COL_LENGTH(N'dbo.T_ZYQD_ProjectVersion', N'Node12DoneTime') IS NULL
                ALTER TABLE dbo.T_ZYQD_ProjectVersion ADD Node12DoneTime DATETIME2 NULL;
            """, ct);

        // 上一版尝试过的 Fs1~Fs12 固化列已废弃（展示改由 T_ZYQD_ProjectFlowLog 承担），
        // 存在就删掉，避免留下三套并存的冗余数据。
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ProjectVersion', N'U') IS NULL RETURN;

            DECLARE @sql NVARCHAR(MAX) = N'';
            SELECT @sql = @sql + N'ALTER TABLE dbo.T_ZYQD_ProjectVersion DROP COLUMN ' + QUOTENAME(c.name) + N';'
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'dbo.T_ZYQD_ProjectVersion')
              AND (c.name LIKE N'Fs[0-9]%');
            IF @sql <> N'' EXEC sp_executesql @sql;
            """, ct);

        // ---- 流程日志表：只增不改的完整流程流水，供流程记录与追溯 ----
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ProjectFlowLog', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.T_ZYQD_ProjectFlowLog
                (
                    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_T_ZYQD_ProjectFlowLog PRIMARY KEY,
                    ProjectGuid   NVARCHAR(50)  NOT NULL,
                    VersionNo     NVARCHAR(20)  NOT NULL,
                    Seq           INT           NOT NULL,
                    Node          INT           NOT NULL,
                    NodeName      NVARCHAR(50)  NOT NULL,
                    OperatorCode  NVARCHAR(50)  NULL,
                    RelatedCode   NVARCHAR(50)  NULL,
                    State         NVARCHAR(20)  NOT NULL,
                    Remark        NVARCHAR(500) NULL,
                    ActionTime    DATETIME2     NOT NULL,
                    ChangeReason  NVARCHAR(500) NULL,
                    CreateTime    DATETIME2     NOT NULL
                );
            END
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ProjectFlowLog', N'U') IS NULL RETURN;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = N'IX_T_ZYQD_ProjectFlowLog_Version'
                             AND object_id = OBJECT_ID(N'dbo.T_ZYQD_ProjectFlowLog'))
                CREATE INDEX IX_T_ZYQD_ProjectFlowLog_Version
                    ON dbo.T_ZYQD_ProjectFlowLog(ProjectGuid, VersionNo, Seq);
            """, ct);

        // 注意：这里只做结构（建列 / 建表 / 建索引），不做任何业务数据的推算与回填。
        // 之前那版启动时会按单据重算 12 步、把老编号刷成新编号，但那条 UPDATE 在
        // UPDATE 的 FROM 里用 CROSS APPLY 反查目标表，实测报 4104（多部分标识符无法绑定），
        // 会让整个启动迁移中断。现已摘除：历史数据的重算改为手工脚本执行，
        // 表已清空的环境不需要任何数据迁移。
    }

    /// <summary>
    /// 审批流水补「动作前状态」列（幂等）。
    ///
    /// 退回记录的 StepCode 存的是「退回到」的状态，单看它无法回答「从哪一级退回来的」，
    /// 流程记录里就只能写成「编写承认书 被退回」，而实际是研发经理从审批节点退回去的。
    /// 历史记录用同一张单据上一条提交/审批留下的 StepCode 回填一次（那条状态就是退回时的节点）。
    /// </summary>
    private static async Task EnsureRejectFromAsync(SqlServerDbContext db, CancellationToken ct)
    {
        // 加列和回填必须分两批：同批次里 UPDATE 的编译发生在加列之前，会报「列名无效」整批不执行。
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ApprovalFlow', N'U') IS NULL RETURN;

            IF COL_LENGTH(N'dbo.T_ZYQD_ApprovalFlow', N'PrevStepCode') IS NULL
                ALTER TABLE dbo.T_ZYQD_ApprovalFlow ADD PrevStepCode NVARCHAR(50) NULL;
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.T_ZYQD_ApprovalFlow', N'U') IS NULL
               OR COL_LENGTH(N'dbo.T_ZYQD_ApprovalFlow', N'PrevStepCode') IS NULL RETURN;

            UPDATE a
            SET PrevStepCode = prev.StepCode
            FROM dbo.T_ZYQD_ApprovalFlow a
            CROSS APPLY (
                SELECT TOP 1 p.StepCode
                FROM dbo.T_ZYQD_ApprovalFlow p
                WHERE p.ProjectGuid = a.ProjectGuid
                  AND p.DocGuid = a.DocGuid
                  AND p.Action IN (N'Submit', N'Approve')
                  AND (p.ActionTime < a.ActionTime
                       OR (p.ActionTime = a.ActionTime AND p.Id < a.Id))
                ORDER BY p.ActionTime DESC, p.Id DESC) prev
            WHERE a.Action = N'Reject'
              AND a.PrevStepCode IS NULL;
            """, ct);
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
