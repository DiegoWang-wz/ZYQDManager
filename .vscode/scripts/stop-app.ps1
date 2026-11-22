# Stop every ZYQDManager dev instance (Ctrl+F8).
#
# Why it is not a simple "taskkill /IM ZYQDManager.exe":
#   1) `dotnet watch` restarts a killed child immediately, so the whole parent chain
#      (dotnet watch -> dotnet / ZYQDManager) must be stopped, not only the child;
#   2) the app may listen on a port other than 5240/5241, so we also match any
#      dotnet.exe whose command line contains ZYQDManager (covers `dotnet run`);
#   3) a name-based sweep catches leftovers from other logon sessions;
#   4) processes stuck in a disconnected/dead session can only be killed with
#      administrator rights, so we retry once elevated (one UAC prompt);
#   5) we poll after killing so the build no longer fails on a locked exe/dll.
#
# Keep this file ASCII-only: Windows PowerShell 5.1 reads BOM-less .ps1 as ANSI,
# and non-ASCII comments break parsing.

param([switch]$Elevated)

$ErrorActionPreference = 'SilentlyContinue'
$ports = @(5240, 5241)
$targets = New-Object 'System.Collections.Generic.HashSet[int]'

function Get-AppProcesses {
    $list = @()
    $list += Get-Process -Name ZYQDManager -ErrorAction SilentlyContinue
    $list += Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
        Where-Object { $_.CommandLine -and $_.CommandLine -match 'ZYQDManager' } |
        ForEach-Object { Get-Process -Id ([int]$_.ProcessId) -ErrorAction SilentlyContinue }
    return $list | Where-Object { $_ }
}

function Stop-AppProcesses {
    $found = @(Get-AppProcesses)
    foreach ($p in $found) { [void]$targets.Add($p.Id) }

    # Port owners plus their dotnet / ZYQDManager parent chain
    Get-NetTCPConnection -State Listen |
        Where-Object { $ports -contains $_.LocalPort } |
        ForEach-Object {
            $current = [int]$_.OwningProcess
            while ($current -gt 0 -and -not $targets.Contains($current)) {
                $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$current"
                if (-not $proc) { break }
                if ($proc.Name -ne 'dotnet.exe' -and $proc.Name -ne 'ZYQDManager.exe') { break }
                [void]$targets.Add($current)
                $current = [int]$proc.ParentProcessId
            }
        }

    # Any dotnet.exe / ZYQDManager.exe whose command line mentions ZYQDManager
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='ZYQDManager.exe'" |
        Where-Object { $_.CommandLine -and $_.CommandLine -match 'ZYQDManager' } |
        ForEach-Object { [void]$targets.Add([int]$_.ProcessId) }

    # Name-based fallback (includes instances started from other sessions)
    Get-Process -Name ZYQDManager -ErrorAction SilentlyContinue |
        ForEach-Object { [void]$targets.Add($_.Id) }

    foreach ($processId in $targets) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        # Tree kill as a second attempt; also releases child dotnet processes
        & taskkill.exe /F /T /PID $processId 2>&1 | Out-Null
    }

    # Poll and retry: watch may respawn a child within a few hundred milliseconds
    for ($i = 0; $i -lt 20; $i++) {
        $left = @(Get-AppProcesses)
        if ($left.Count -eq 0) { break }
        foreach ($p in $left) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
            & taskkill.exe /F /T /PID $p.Id 2>&1 | Out-Null
        }
        Start-Sleep -Milliseconds 300
    }
}

Stop-AppProcesses
$remaining = @(Get-AppProcesses)

# Still alive? Most likely processes parked in a disconnected session: retry elevated.
if ($remaining.Count -gt 0 -and -not $Elevated) {
    Write-Host 'Some instances need administrator rights (disconnected session). Retrying elevated...'
    $childArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Elevated')
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $childArgs -Wait -ErrorAction SilentlyContinue
    $remaining = @(Get-AppProcesses)
}

if ($remaining.Count -eq 0) {
    if ($targets.Count -eq 0) {
        Write-Host 'No running ZYQDManager instance found.'
    } else {
        Write-Host ("Stopped process ids: " + (($targets | Sort-Object) -join ', '))
    }
} else {
    $ids = ($remaining | ForEach-Object { $_.Id } | Sort-Object -Unique) -join ', '
    Write-Host ("Still running: " + $ids)
    Write-Host 'Cancel the UAC prompt is the usual cause; otherwise start the IDE as administrator.'
    exit 1
}
