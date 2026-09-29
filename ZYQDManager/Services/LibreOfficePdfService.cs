using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;

namespace ZYQDManager.Services;

public class LibreOfficePdfService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
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

    public string? ResolvedPath()
    {
        var configured = (_options.CurrentValue.SofficePath ?? "").Trim().Trim('"');
        if (configured.Length > 0 && File.Exists(configured))
            return configured;

        var fallback = @"C:\Program Files\LibreOffice\program\soffice.exe";
        return File.Exists(fallback) ? fallback : (configured.Length > 0 ? configured : fallback);
    }

    public async Task<(bool Ok, string Message)> TestAsync(CancellationToken ct = default)
    {
        var exe = ResolvedPath();
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return (false, "未找到 soffice.exe：" + (exe ?? "(空路径)"));

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
            throw new InvalidOperationException("未找到 soffice.exe：" + (exe ?? "(空路径)"));
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
        var profile = Path.Combine(Path.GetTempPath(), "zyqd-lo-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(profile);
        try
        {
            var profileUri = new Uri(profile + Path.DirectorySeparatorChar).AbsoluteUri.TrimEnd('/');
            var args = $"\"-env:UserInstallation={profileUri}\" {extraArgs}";
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

            _log.LogInformation("LibreOffice 启动 {Exe} {Args}", exe, extraArgs);
            if (!proc.Start())
                throw new InvalidOperationException("无法启动 soffice.exe");

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
            TryDeleteDir(profile);
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
                    if (stable >= 3)
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
                await Task.Delay(800, ct);
                if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.pdf").Length > 0)
                    continue;
                return;
            }

            await Task.Delay(400, ct);
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
