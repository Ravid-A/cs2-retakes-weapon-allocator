<#
.SYNOPSIS
    Compiles Panorama layouts and stylesheets and copies them into csgo\overrides as loose files.

.DESCRIPTION
    Workshop Tools is a GUI over resourcecompiler.exe, which ships with the game in game\bin\win64.
    This calls it directly and puts the compiled output where the game will find it.

        .\build-hud.ps1
        .\build-hud.ps1 -Watch                  # recompile and copy on every save
        .\build-hud.ps1 -Addon my_other_addon
        .\build-hud.ps1 -NoDeploy               # compile only, leave overrides alone

    Output goes to game\csgo\overrides\panorama\..., mirroring the game's own layout. That requires
    a DIRECTORY search path in csgo\gameinfo.gi, not a VPK one:

        Game                csgo/overrides
        Game                csgo

    A directory entry is not a sealed archive, so replacing a file inside it may be picked up without
    a full restart - worth trying before you quit the game, though the search path itself still only
    mounts at startup.

.NOTES
    Written on a Mac against the documented flags, so the first run is the real test. Every step
    prints what it is doing and stops on the first failure rather than carrying on.
#>
[CmdletBinding()]
param(
    # Assemble panorama files from every project into the addon before compiling. Each project owns
    # its own layouts so it can be split into its own repo; this puts them back together.
    [switch] $Collect,
    [string] $RepoRoot = (Join-Path $PSScriptRoot '..\..'),
    [string] $Cs2Root  = 'X:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive',
    [string] $Addon    = 'hud_test1',
    [switch] $Watch,
    [switch] $NoDeploy,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$compiler   = Join-Path $Cs2Root 'game\bin\win64\resourcecompiler.exe'
$contentDir = Join-Path $Cs2Root "content\csgo_addons\$Addon\panorama"
$gameDir    = Join-Path $Cs2Root "game\csgo_addons\$Addon\panorama"
$overrides  = Join-Path $Cs2Root 'game\csgo\overrides\panorama'

function Assert-Path([string] $path, [string] $what) {
    if (-not (Test-Path $path)) {
        throw "$what not found: $path`nPass -Cs2Root if your install is elsewhere."
    }
}

Assert-Path $compiler   'resourcecompiler.exe'
Assert-Path $contentDir "Addon content (is -Addon '$Addon' right?)"

function Build {
    if ($Collect) {
        Write-Host "`n[0/2] Collecting panorama files from every project" -ForegroundColor Cyan

        $collector = Join-Path $PSScriptRoot 'collect-panorama.py'
        $addon     = Join-Path $Cs2Root "content\csgo_addons\$Addon"

        & python3 $collector --root $RepoRoot --out $addon

        if ($LASTEXITCODE -ne 0) { throw 'Collect failed - see the conflict above.' }
    }

    $sources = Get-ChildItem -Path $contentDir -Recurse -Include *.xml, *.css -File

    if (-not $sources) { throw "No .xml or .css under $contentDir" }

    Write-Host "`n[1/2] Compiling $($sources.Count) file(s)" -ForegroundColor Cyan

    foreach ($src in $sources) {
        # resourcecompiler maps content\ to game\ by convention, so it only needs the input path.
        $rcArgs = @('-i', $src.FullName)
        if ($Force) { $rcArgs += '-f' }

        & $compiler @rcArgs | Out-Null

        if ($LASTEXITCODE -ne 0) { throw "Compile failed: $($src.Name)" }

        Write-Host "      $($src.Name)" -ForegroundColor DarkGray
    }

    $compiled = Get-ChildItem -Path $gameDir -Recurse -Include *.vxml_c, *.vcss_c -File -ErrorAction SilentlyContinue

    if (-not $compiled) {
        throw "Nothing compiled into $gameDir. The compiler ran but produced no _c files - check its output above."
    }

    if ($NoDeploy) {
        Write-Host "[2/2] Compiled to $gameDir (not deployed)" -ForegroundColor Green
        return
    }

    Write-Host "[2/2] Copying $($compiled.Count) compiled file(s) to overrides" -ForegroundColor Cyan

    foreach ($file in $compiled) {
        # Preserve the panorama\layout\custom_game\... shape; that path is what the client resolves.
        $relative = $file.FullName.Substring($gameDir.Length).TrimStart('\')
        $target   = Join-Path $overrides $relative

        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item $file.FullName $target -Force

        Write-Host "      panorama\$relative" -ForegroundColor DarkGray
    }

    Write-Host "      -> $overrides`n" -ForegroundColor Green
}

Build

if ($Watch) {
    Write-Host "Watching $contentDir - Ctrl+C to stop.`n" -ForegroundColor Yellow

    $watcher = New-Object System.IO.FileSystemWatcher $contentDir, '*.*'
    $watcher.IncludeSubdirectories = $true
    $watcher.EnableRaisingEvents   = $true

    while ($true) {
        # Editors save in bursts (write, rename, truncate), so take one change then drain the rest
        # before building - otherwise a single Ctrl+S triggers three compiles.
        $change = $watcher.WaitForChanged([System.IO.WatcherChangeTypes]::All, 1000)

        if ($change.TimedOut) { continue }
        if ($change.Name -notmatch '\.(xml|css)$') { continue }

        Start-Sleep -Milliseconds 250
        while (-not $watcher.WaitForChanged([System.IO.WatcherChangeTypes]::All, 150).TimedOut) { }

        Write-Host "changed: $($change.Name)" -ForegroundColor Yellow

        try   { Build }
        catch { Write-Host "  $_" -ForegroundColor Red }
    }
}
