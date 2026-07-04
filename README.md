# RimWorld Backup Tool

> 一键备份与还原 RimWorld 游戏数据 | One-click backup and restore for RimWorld

---

## Overview / 概述

A C# Windows Forms tool to back up and restore RimWorld game data, including mods, saves, config files, and game files. Built with .NET 8 and WinForms, featuring a dark RimWorld-themed UI with responsive layout.

一款 C# Windows Forms 工具，用于备份和还原 RimWorld 游戏数据，包括 Mod、存档、配置文件及游戏本体文件。基于 .NET 8 + WinForms 构建，采用 RimWorld 主题深色界面，支持响应式布局。

---

## Features / 功能

| English | 中文 |
|---------|------|
| Backup workshop mods (full files) | 备份创意工坊 Mod（完整文件） |
| Backup local mods (full files) | 备份本地 Mod（完整文件） |
| Backup game saves (.rws) | 备份游戏存档 (.rws) |
| Backup mod config & game config | 备份 Mod 配置和游戏配置 |
| Backup game info & version | 备份游戏版本信息 |
| Backup game installation files | 备份游戏安装文件 |
| Backup Player.log | 备份游戏日志 |
| Restore from any backup ZIP | 从任意备份 ZIP 还原 |
| ZIP64 support (no 65k file limit) | ZIP64 支持（无 65K 文件限制） |
| Multi-threaded compression (DotNetZip) | 多线程压缩 (DotNetZip) |
| Chinese / English interface | 中英文界面切换 |
| Resizable window with responsive layout | 可调整大小的响应式窗口 |

---

## Requirements / 运行要求

- **OS:** Windows 10 / 11
- **Runtime:** .NET 8 Desktop Runtime (x64)
- **Game:** RimWorld (Steam)
- **Disk:** Sufficient space for backup archives

---

## Quick Start / 快速开始

### Method 1: Run the released EXE / 方法一：运行已发布的 EXE

1. Download `RimWorldBackupTool.exe` from the release
2. Double-click to run (no console window)
3. The Steam library path is auto-detected
4. Select items to back up and click "Start Backup"

1. 从发布页面下载 `RimWorldBackupTool.exe`
2. 双击运行（无命令提示符窗口）
3. Steam 路径自动检测
4. 勾选需要备份的内容，点击「开始备份」

### Method 2: Build from source / 方法二：从源码编译

```bash
cd RimWorld_Backup_CSharp
dotnet build -c Release
# Output: bin/Release/net8.0-windows/RimWorldBackup.exe
```

---

## Project Structure / 项目结构

```
RimWorld_Backup_CSharp/
├── Program.cs              # Entry point / 入口点
├── MainForm.cs             # UI layout + controls / 界面布局与控件
├── BackupManager.cs        # Core backup/restore logic / 核心备份/还原逻辑
├── AppConfig.cs            # JSON config persistence / JSON 配置持久化
├── RimWorldBackup.csproj   # Project file / 项目文件
└── RimWorld_Backup/        # Backup ZIP directory / 备份文件存放目录
```

### Dependencies / 依赖

- .NET 8.0-windows
- DotNetZip 1.16.0

---

## Backup Contents / 备份内容

| English | 中文 | ZIP Path |
|---------|------|----------|
| Mod load order & config | Mod 排序及配置 | `Config/` |
| Game saves (.rws) | 游戏存档 | `Saves/` |
| Workshop mods | 创意工坊 Mod | `Workshop/` |
| Local mods | 本地 Mod | `LocalMods/` |
| Game version info | 版本信息 | `GameInfo/` |
| Game installation files | 游戏本体文件 | `GameFiles/` |
| Player.log | 游戏日志 | `Logs/` |

---

## Tech Notes / 技术说明

- **Compression:** Uses DotNetZip with `ParallelDeflateThreshold=0` and `BestSpeed` level, 128KB buffer. File names are UTF-8 encoded for Chinese compatibility.
- **ZIP64:** Always enabled. No file count or size limitations.
- **Layout:** `TableLayoutPanel` + `FlowLayoutPanel` based responsive layout. `Dock.Top` + `AutoSize` for content-driven sizing, `TabPage.AutoScroll` as fallback.
- **Async:** All file I/O runs on background threads via `Task.Run()`. UI thread stays responsive with `async/await` and `IProgress<T>` callbacks.
- **Safety:** Backup uses a temp directory with atomic ZIP creation; restore does not delete the entire game directory.

---

## License

MIT
