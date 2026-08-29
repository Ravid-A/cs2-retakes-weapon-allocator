@echo off
REM Runs build-hud.ps1 with a per-invocation execution-policy bypass.
REM
REM PowerShell's default policy on many machines is AllSigned, which refuses any unsigned script -
REM including local ones, so Unblock-File does not help. -ExecutionPolicy Bypass applies to this one
REM process only and changes nothing system-wide, which is the right trade for a build script.
REM
REM   build-hud.cmd
REM   build-hud.cmd -Watch
REM   build-hud.cmd -Addon my_addon -Force
REM
REM Double-clicking runs a one-off build; use -Watch from a terminal so you can Ctrl+C out.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-hud.ps1" %*

if errorlevel 1 (
    echo.
    echo Build failed. Scroll up for the first error.
    pause
)
