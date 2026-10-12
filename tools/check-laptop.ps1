#requires -Version 5.1
<#
.SYNOPSIS
  River Rush 3D laptop readiness check for Windows 10/11.
.DESCRIPTION
  Read-only: prints processor, RAM, graphics adapter(s), operating system,
  and drive free space. Does not install programs, change settings, or upload data.
  Optional -SaveReport writes the summary to a JSON file in the current folder.
#>
param(
    [switch] $SaveReport
)

$ErrorActionPreference = 'Stop'
try {
    $cpu = @(Get-CimInstance Win32_Processor | Select-Object -First 1)[0]
    $computer = Get-CimInstance Win32_ComputerSystem
    $os = Get-CimInstance Win32_OperatingSystem
    $graphics = @(Get-CimInstance Win32_VideoController |
        Where-Object { $_.Name } |
        ForEach-Object { [ordered]@{
            Name = [string] $_.Name
            DriverVersion = [string] $_.DriverVersion
            # AdapterRAM can overflow/truncate above 4 GB on some Windows drivers.
            AdapterRAMNote = 'Verify dedicated GPU memory in Task Manager > Performance > GPU'
        } })
    $drives = @(Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" |
        ForEach-Object { [ordered]@{
            Drive = [string] $_.DeviceID
            TotalGB = [Math]::Round($_.Size / 1GB, 1)
            FreeGB = [Math]::Round($_.FreeSpace / 1GB, 1)
        } })
    $ram = [Math]::Round($computer.TotalPhysicalMemory / 1GB, 1)
    $names = @($graphics | ForEach-Object { $_.Name })
    $integratedHints = 'Radeon 780M|Radeon 760M|Radeon 680M|Radeon Graphics|Iris|UHD Graphics|Vega'
    $discreteHints = 'GeForce|NVIDIA|Radeon RX|Intel Arc A|Intel Arc B'

    $recommendation = 'Begin with Balanced (uncertain GPU), then benchmark'
    if ($ram -lt 10) {
        $recommendation = 'Balanced / 30 FPS: limited physical RAM'
    }
    elseif ($names | Where-Object { $_ -match $integratedHints }) {
        $recommendation = 'Balanced / 60 FPS: integrated GPU likely'
    }
    elseif ($names | Where-Object { $_ -match $discreteHints }) {
        $recommendation = 'High / 60 FPS starting point; consider Ultra only after GPU/VRAM and FPS checks'
    }

    $report = [ordered]@{
        Project = 'River Rush 3D'
        CollectedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm')
        CPU = [string] $cpu.Name
        RAM_GB = $ram
        OS = [string] $os.Caption
        GPU = $graphics
        LocalDrives = $drives
        RecommendedStartingPreset = $recommendation
        Notes = @(
            '32 GB RAM helps Unity editing but does not guarantee graphics performance.',
            'Install Unity and keep active project files on an SSD when possible.',
            'Real FPS depends on the GPU, cooling, shaders and map complexity.'
        )
    }

    $json = $report | ConvertTo-Json -Depth 6
    Write-Output $json
    if ($SaveReport) {
        $path = Join-Path (Get-Location).Path 'RiverRush_Laptop_Report.json'
        Set-Content -LiteralPath $path -Value $json -Encoding UTF8
        Write-Output "Report saved: $path"
    }
}
catch {
    Write-Error "Read-only hardware check failed: $($_.Exception.Message)"
    exit 1
}
