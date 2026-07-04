# RimWorld Backup Tool

> 一键备份与还原 RimWorld 游戏数据 | One-click backup and restore for RimWorld

---

## Files / 文件清单

```
RimWorldBackupTool.exe     ← Main executable, double-click to run (no console window)
                            主程序，双击运行（无黑框）
RimWorld_Backup.ps1        ← PowerShell source code (for debugging)
                            PowerShell 源码（调试用）
test.bat                   ← Debug launcher (shows console window)
                            调试启动（显示黑框）
启动.vbs                   ← Alternative silent launcher
                            备选无黑框启动
RimWorld_Backup/           ← Backup ZIP directory (auto-created)
                            备份文件存放目录（自动生成）
```

## How to Use / 使用方法

1. Double-click **RimWorldBackupTool.exe** to launch / 双击 **RimWorldBackupTool.exe** 启动
2. Steam library path is auto-detected — verify it's correct / 自动检测 Steam 路径，检查是否正确
3. Check the items you want to back up / 勾选需要备份的内容
4. Click "Start Backup" / 点「开始备份」
5. Switch to "Restore" tab to view existing backups and restore / 在「还原」标签页可查看已有备份并还原

## Backup Contents / 备份内容

| 中文 | English |
|------|---------|
| Mod 排序配置（ModsConfig.xml） | Mod load order config (ModsConfig.xml) |
| 游戏存档（.rws 文件） | Game saves (.rws files) |
| 创意工坊 Mod 列表和元信息 | Workshop mod list and metadata |
| 本地 Mod 元信息 | Local mod metadata |
| 游戏版本信息 | Game version info |
| 游戏本体安装文件（可选） | Game installation files (optional) |
| 游戏日志（Player.log） | Player.log |

