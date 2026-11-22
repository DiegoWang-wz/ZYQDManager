# ZYQDManager 开发约定

遮阳管理系统（Blazor Server / .NET 9 / MudBlazor / EF Core + SQL Server）。

## 环境

| 项 | 值 |
|---|---|
| SDK | 9.0.301（`global.json` 锁定，`rollForward: latestFeature`） |
| 目标框架 | `net9.0` |
| HTTP 启动端口 | `http://localhost:5240` |
| HTTPS 启动端口 | `https://localhost:7241` |
| 主库 | `Sunlight_Management_BU`（连接串见 `appsettings.json`） |
| 中央账号/权限 API | `CentralApi:BaseUrl`（JcCommonApi） |
| 私有依赖 | `ZYQDManager/libs/Sap.Data.Hana.Core.v2.1.dll`（Hana 驱动，不可删） |

## 运行与调试

IDE 内（CodeBuddy / VS Code，配置在 `.vscode/`）：

| 操作 | 效果 |
|---|---|
| `F5` | HTTP 启动 + 断点调试（5240） |
| `Ctrl+F6` | 热重载启动（`dotnet watch`，先自动清理旧实例） |
| `Ctrl+F7` | Release 打包（输出到 `W:\ZYQManger`） |
| `Ctrl+F8` | 结束所有运行中的实例（释放 5240/5241），执行后会在终端里列出被停止的 PID |
| `Ctrl+Shift+B` | 仅编译 |
| `Shift+F5` | 停止 |

命令行：

```powershell
dotnet run --project ZYQDManager --launch-profile http   # 常规启动
dotnet watch run --launch-profile http                   # 热重载
dotnet build ZYQDManager/ZYQDManager.csproj              # 编译
```

`Ctrl+F6` 会先执行 `kill: ZYQDManager`（`.vscode/scripts/stop-app.ps1`）。
该脚本不是按进程名简单杀，而是**沿 5240/5241 端口占用者向上追溯父进程链** —— 因为 `dotnet watch` 会重启被杀的 `dotnet.exe` 子进程，只杀子进程是无效的。

## 电机单据「一屏三单」架构

立项单 / 规格书 / 承认书三张单据常驻同一页面，左右切换只做 CSS 平移，不整页重载。

**入口**：`Components/Pages/Project/MotorDocPager.razor`，路由 `/project/motor/{Kind}/{DocGuid}`（`Kind` = `lx` | `gg` | `cr`）。

**数据流**：

```
MotorDocPager
  ├─ LocateAsync(kind, docGuid)          → 1 条查询：定位 ProjectGuid + VersionNo
  ├─ GetBundleAsync(projectGuid, versionNo) → 6 条查询：三单整行 + 版本行 + 项目行 + 版本号列表
  └─ 把同一个 MotorDocBundle 传给三个 sheet（Bundle 参数）
```

同版本左右切换时 `ContainsDocGuid` 命中，**0 次查询**；只有跨版本或保存后新建才重新取 bundle。

### 三条铁律

1. **不要给 `MotorLxSheet` / `MotorGgSheet` / `MotorCrSheet` 加回 `@page`**。它们已由 `MotorDocPager` 接管路由，重复注册会抛 ambiguous route。
2. **三个 sheet 内部不要再查库**。同一渲染树里的子组件会并发初始化，各自用注入的 scoped `DbContext` 查询会撞 `A second operation was started on this context instance`。数据一律由父组件 `GetBundleAsync` 取好下发；`ApplyBundle(Async)` 里只允许做内存赋值。
3. **带 `[SupplyParameterFromQuery]` 的参数不能被父组件显式赋值**（Blazor 会抛异常）。这些参数在 sheet 里已改成普通 `[Parameter]`，由 `MotorDocPager` 自己读查询串后传入。

### 新增单据页时的改动点

- 父组件：新增 `Bundle.Xxx` 字段 + `GetBundleAsync` 里加一条查询 + 容器里加一个 slide；
- 子组件：加 `Bundle` / `Embedded` 参数，`OnParametersSetAsync` 开头加 Bundle 分支，`PageTitle` 与 `MotorDocArrows` 在 `Embedded` 时不输出。

## 样式与缓存

`wwwroot/css/motor-sheet.css` 是单据专用样式（`.msheet-*` / `.mdpager-*` 前缀），在 `App.razor` 里引用时**带文件写入时间做版本号**：

```razor
<link rel="stylesheet" href="css/motor-sheet.css?v=@MotorSheetCssVersion"/>
```

改动这个文件后普通刷新即可生效，不需要强刷。若移除版本号，浏览器会拿旧缓存，表现为「三张单据纵向堆叠、表格高度塌陷」—— 这是最典型的假故障。

### 平移实现要点

- 容器 `.mdpager` **不能**用 `display:flex`，否则 300% 宽的轨道会被 `flex-shrink` 压回一屏，三页挤在一起；
- `.mdpager-slide` 需要 `overflow:hidden`，单据纸张固定 1180px 宽，窄屏下由内部 `.msheet-wrap` 横向滚动；
- 离屏的两张用 `content-visibility:auto` 跳过布局与绘制；
- `MotorDocArrows` 是 `position:fixed`，必须放在带 `transform` 的轨道**外面**，否则 fixed 会以轨道为包含块。

## 已知待办

- 打包目标是 `W:\ZYQManger`。W 是**网络映射盘**（`\\192.168.3.240\开发共享文件\临时转存文件-堡垒机`），目录名刻意保留 `ZYQManger` 的拼写，不要改。发布前确认资源管理器里 W 盘已连接即可。
  若 IDE 以管理员身份运行、看不到用户级盘符映射，把 `.vscode/tasks.json` 里 `publish` 的 `-o` 临时换成上面的 UNC 路径即可（`launch.json` 第三项的 `program` / `cwd` 同理）。
- **只读态仍是 `<input>` + `.msheet-readonly { pointer-events:none }`**（`motor-sheet.css`）。三单同页后单页约 150 个 input，可把只读态输出 `<span>` 代替 `<input>`，DOM 减半并避免自动填充、手机端误弹键盘。

## 其他约定

- 单据页的 `[Parameter] public string? DocGuid` 判空 + `"new"` 都算新建（`IsNew`）。
- 角色判定唯一来源是 JcCommonApi（`AppRoleService`），不再维护本地角色成员表；当前登录人直接读 localStorage，不产生网络请求。
- 单据数据的读一律 `AsNoTracking()`；保存走 `MotorDocService.SaveGgAsync` / `SaveCrAsync` / `MotorLxService.SaveDraftAsync`。
