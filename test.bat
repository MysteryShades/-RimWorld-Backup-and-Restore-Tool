@echo off
title RimWorld Backup Tool (Debug)
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0RimWorld_Backup.ps1"
pause
