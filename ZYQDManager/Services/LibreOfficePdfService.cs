using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;

namespace ZYQDManager.Services;

public class LibreOfficePdfService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    /// <summary>复用固定配置目录，避免每次导出都冷初始化 UserInstallation。</summary>
    private static readonly string SharedProfileDir =
        Path.Combine(Path.GetTempPath(), "zyqd-lo-profile");

    private readonly IOptionsMonitor<LibreOfficeOptions> _options;
    private readonly FileOperationService _fileOp;
    private readonly ILogger<LibreOfficePdfService> _log;

    public LibreOfficePdfService(
        IOptionsMonitor<LibreOfficeOptions> options,
        FileOperationService fileOp,
        ILogger<LibreOfficePdfService> log)
    {
        _options = options;
        _fileOp = fileOp;
        _log = log;
    }

    /// <summary>启动时后台预热，缩短首次导出等待。</summary>
    public async Task WarmupAsync(CancellationToken ct = default)
    {
        try
        {
            var (ok, msg) = await TestAsync(ct);
            if (ok)
                _log.LogInformation("LibreOffice 预热完成：{Msg}", msg);
            else
                _log.LogWarning("LibreOffice 预热未成功：{Msg}", msg);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "LibreOffice 预热异常");
        }
    }

    public string? ResolvedPath()
    {
        var configured = (_options.CurrentValue.SofficePath ?? "").Trim().Trim('"');
        // Windows 上 soffice.exe 是 GUI 启动器，重定向 stdout 时经常不退出导致超时；CLI 应用 soffice.com。
        var preferred = PreferConsoleLauncher(configured);
        if (preferred.Length > 0 && File.Exists(preferred))
            return preferred;

        var fallbackCom = @"C:\Program Files\LibreOffice\program\soffice.com";
        if (File.Exists(fallbackCom))
            return fallbackCom;

        var fallbackExe = @"C:\Program Files\LibreOffice\program\soffice.exe";
        if (File.Exists(fallbackExe))
            return fallbackExe;

        return preferred.Length > 0 ? preferred : fallbackCom;
    }

    /// <summary>
    /// 配置若指向 soffice.exe，优先改用同目录 soffice.com（控制台版，适合 Process 调用）。
    /// </summary>
    private static string PreferConsoleLauncher(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        if (!path.EndsWith("soffice.exe", StringComparison.OrdinalIgnoreCase))
            return path;

        var com = Path.Combine(Path.GetDirectoryName(path) ?? "", "soffice.com");
        return File.Exists(com) ? com : path;
    }

    public async Task<(bool Ok, string Message)> TestAsync(CancellationToken ct = default)
    {
        var exe = ResolvedPath();
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return (false, "未找到 soffice：" + (exe ?? "(空路径)"));

        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(15, _options.CurrentValue.TimeoutSeconds));
            var (code, output) = await RunAsync(exe, "--headless --nologo --nofirststartwizard --norestore --version", timeout, ct);
            var line = FirstLine(output);
            if (code == 0 && line.Length > 0)
                return (true, "已连接 " + line);
            if (code == 0)
                return (true, "已连接，soffice 可运行：" + exe);
            return (false, $"调用失败 (exit {code}) {(line.Length > 0 ? line : exe)}");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "LibreOffice 连接测试失败");
            return (false, "调用异常：" + ex.Message);
        }
    }

    public async Task<byte[]> ConvertXlsxToPdfAsync(byte[] xlsx, CancellationToken ct = default)
    {
        var exe = ResolvedPath();
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            throw new InvalidOperationException("未找到 soffice：" + (exe ?? "(空路径)"));
        if (xlsx is null || xlsx.Length == 0)
            throw new InvalidOperationException("没有可转换的 Excel 内容");

        var work = Path.Combine(Path.GetTempPath(), "zyqd-lo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var xlsxPath = Path.Combine(work, "pi.xlsx");
        await File.WriteAllBytesAsync(xlsxPath, xlsx, ct);

        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(60, _options.CurrentValue.TimeoutSeconds));
            var args =
                "--headless --nologo --nofirststartwizard --norestore --nolockcheck " +
                "--convert-to pdf:calc_pdf_Export " +
                $"--outdir \"{work}\" \"{xlsxPath}\"";
            var (code, output) = await RunAsync(exe, args, timeout, ct, work, waitPdfDir: work);
            var pdfPath = Directory.GetFiles(work, "*.pdf").FirstOrDefault();
            if (pdfPath is null || new FileInfo(pdfPath).Length == 0)
                throw new InvalidOperationException($"LibreOffice 未生成 PDF (exit {code}) {FirstLine(output)}");
            return await _fileOp.ReadDecryptedPdfAsync(pdfPath, _log, ct);
        }
        finally
        {
            TryDeleteDir(work);
        }
    }

    private async Task<(int ExitCode, string Output)> RunAsync(
        string exe,
        string extraArgs,
        TimeSpan timeout,
        CancellationToken ct,
        string? workDir = null,
        string? waitPdfDir = null)
    {
        await Gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(SharedProfileDir);
            var profileUri = new Uri(SharedProfileDir + Path.DirectorySeparatorChar).AbsoluteUri.TrimEnd('/');
            // 固定配置目录可能残留 .lock，加 nolockcheck；测试路径原先没有该参数
            var flags = extraArgs.Contains("--nolockcheck", StringComparison.OrdinalIgnoreCase)
                ? extraArgs
                : "--nolockcheck " + extraArgs;
            var args = $"\"-env:UserInstallation={profileUri}\" {flags}";
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = workDir is { Length: > 0 } ? workDir : Path.GetDirectoryName(exe) ?? "",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var sb = new StringBuilder();
            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) sb.AppendLine(e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) sb.AppendLine(e.Data);
            };

            _log.LogInformation("LibreOffice 启动 {Exe} {Args}", exe, flags);
            if (!proc.Start())
                throw new InvalidOperationException("无法启动 soffice：" + exe);

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                if (waitPdfDir is { Length: > 0 })
                    await WaitUntilPdfReadyAsync(waitPdfDir, proc, timeoutCts.Token);
                else
                    await proc.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                TryKill(proc);
                throw new TimeoutException($"LibreOffice 超时（{timeout.TotalSeconds:0} 秒）");
            }

            if (!proc.HasExited)
            {
                try { await proc.WaitForExitAsync(timeoutCts.Token); }
                catch (OperationCanceledException)
                {
                    TryKill(proc);
                }
            }

            return (proc.HasExited ? proc.ExitCode : -1, sb.ToString());
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task WaitUntilPdfReadyAsync(string dir, Process proc, CancellationToken ct)
    {
        long lastSize = -1;
        var stable = 0;
        while (!ct.IsCancellationRequested)
        {
            var pdf = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.pdf").FirstOrDefault() : null;
            if (pdf is not null)
            {
                long len;
                try { len = new FileInfo(pdf).Length; }
                catch { len = 0; }

                if (len > 0 && len == lastSize)
                {
                    stable++;
                    if (stable >= 2)
                        return;
                }
                else
                {
                    stable = 0;
                    lastSize = len;
                }
            }
            else if (proc.HasExited)
            {
                await Task.Delay(300, ct);
                if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.pdf").Length > 0)
                    continue;
                return;
            }

            await Task.Delay(200, ct);
        }
        ct.ThrowIfCancellationRequested();
    }

    private static string FirstLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var t = line.Trim();
            if (t.Length > 0) return t;
        }
        return text.Trim();
    }

    private static void TryKill(Process proc)
    {
        try
        {
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch
        {
            // 配置目录可能被 soffice 短暂占用
        }
    }
}
