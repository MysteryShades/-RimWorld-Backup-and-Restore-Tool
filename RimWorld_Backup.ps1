#requires -version 4.0

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

[System.Windows.Forms.Application]::EnableVisualStyles()

# ===== Helper Functions =====
function Fnt($s) { 
    try { 
        $p = $s -split ','; $sz = [float]$p[1]; 
        $st = if ($p[2] -eq "Bold") { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }; 
        return New-Object Drawing.Font("Tahoma", $sz, $st) 
    } catch { return New-Object Drawing.Font("Tahoma",9) } 
}

# 强类型数组返回，确保即使只有1个文件或0个文件，遍历也不会出错
function Get-DeepFiles($rootPath) {
    $result = New-Object System.Collections.Generic.List[string]
    $stack = New-Object System.Collections.Stack
    $stack.Push($rootPath)
    
    while ($stack.Count -gt 0) {
        $currentDir = $stack.Pop()
        try {
            $files = [System.IO.Directory]::GetFiles($currentDir, '*', [System.IO.SearchOption]::TopDirectoryOnly)
            foreach ($f in $files) { $result.Add($f) }
            
            $dirs = [System.IO.Directory]::GetDirectories($currentDir, '*', [System.IO.SearchOption]::TopDirectoryOnly)
            foreach ($d in $dirs) { $stack.Push($d) }
        } catch {
            # 忽略无权限错误，继续遍历其他目录
        }
    }
    return $result.ToArray()
}

$script:lang = "zh"
$script:rootDir = if ($PSCommandPath) { Split-Path $PSCommandPath -Parent } else { $PWD.Path }
$script:configFile = Join-Path $script:rootDir "cfg.xml"

function L($zh, $en) { if ($script:lang -eq "en") { return $en } return $zh }
function T($p) { return ($p -and (Test-Path "$p\steamapps\common\RimWorld")) }

function Find-SteamAuto {
    foreach ($p in @("E:\SteamLibrary","D:\SteamLibrary","C:\Program Files (x86)\Steam","C:\Program Files\Steam")) { if (T $p) { return $p } }
    try { $r = Get-ItemProperty "Registry::HKEY_CURRENT_USER\Software\Valve\Steam" -ErrorAction Stop; if ($r.SteamPath -and (T $r.SteamPath)) { return $r.SteamPath } } catch {}
    return ""
}

function Get-FileSize($p) { 
    if (-not (Test-Path $p)) { return "0 B" }
    $l = (Get-Item $p).Length
    if ($l -gt 1GB) { return "{0:N1} GB" -f ($l/1GB) }
    if ($l -gt 1MB) { return "{0:N1} MB" -f ($l/1MB) }
    if ($l -gt 1KB) { return "{0:N1} KB" -f ($l/1KB) }
    return "$l B" 
}

function Get-BackupZips {
    $root = Join-Path $script:rootDir "RimWorld_Backup"
    if (-not (Test-Path $root)) { return @() }
    $zips = Get-ChildItem $root -Filter "*.zip" -ErrorAction SilentlyContinue | Sort-Object Name -Descending
    if (-not $zips) { return @() }
    $result = @()
    foreach ($z in $zips) {
        $size = Get-FileSize($z.FullName)
        $result += New-Object PSObject -Property @{ Path = $z.FullName; Name = $z.BaseName; Size = $size; DisplayName = "$($z.BaseName) - $size" }
    }
    return $result
}

function nlb($t,$x,$y,$s,$c) { $r = New-Object Windows.Forms.Label; $r.Text = $t; $r.Location = New-Object Drawing.Point($x,$y); $r.AutoSize = $true; if ($s) { try { $fs = [float]($s -split ',' | Select-Object -First 1); $fb = ($s -like "*Bold*"); $sty = if ($fb) { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }; $r.Font = New-Object Drawing.Font("Tahoma", $fs, $sty) } catch {} }; if ($c) { $r.ForeColor = $c }; return $r }
function ncb($t,$x,$y) { $r = New-Object Windows.Forms.CheckBox; $r.Text = $t; $r.Size = New-Object Drawing.Size(310,22); $r.Location = New-Object Drawing.Point($x,$y); $r.Checked = $true; return $r }
function ntb($x,$y,$w) { $r = New-Object Windows.Forms.TextBox; $r.Size = New-Object Drawing.Size($w,25); $r.Location = New-Object Drawing.Point($x,$y); return $r }
function nbb($t,$x,$y,$w,$h) { $r = New-Object Windows.Forms.Button; $r.Text = $t; if ($h) { $r.Size = New-Object Drawing.Size($w,$h) } else { $r.Size = New-Object Drawing.Size($w,25) }; $r.Location = New-Object Drawing.Point($x,$y); return $r }
function ngb($t,$x,$y,$w,$h) { $r = New-Object Windows.Forms.GroupBox; $r.Text = " $t "; $r.Size = New-Object Drawing.Size($w,$h); $r.Location = New-Object Drawing.Point($x,$y); return $r }

# ===== Config Save/Load =====
function Save-Cfg($sp, $opts, $lang) {
    $xml = New-Object System.Xml.XmlDocument
    $root = $xml.CreateElement("C")
    $xml.AppendChild($root)
    $sNode = $xml.CreateElement("S"); $sNode.InnerText = $sp; $root.AppendChild($sNode)
    $lNode = $xml.CreateElement("L"); $lNode.InnerText = $lang; $root.AppendChild($lNode)
    $oNode = $xml.CreateElement("O"); $root.AppendChild($oNode)
    if ($opts.Contains('Config')) { $oNode.AppendChild($xml.CreateElement("C")) | Out-Null }
    if ($opts.Contains('Saves')) { $oNode.AppendChild($xml.CreateElement("S")) | Out-Null }
    if ($opts.Contains('Workshop')) { $oNode.AppendChild($xml.CreateElement("W")) | Out-Null }
    if ($opts.Contains('LocalMods')) { $oNode.AppendChild($xml.CreateElement("L")) | Out-Null }
    if ($opts.Contains('GameInfo')) { $oNode.AppendChild($xml.CreateElement("G")) | Out-Null }
    if ($opts.Contains('GameFiles')) { $oNode.AppendChild($xml.CreateElement("G2")) | Out-Null }
    if ($opts.Contains('Log')) { $oNode.AppendChild($xml.CreateElement("P")) | Out-Null }
    $xml.Save($script:configFile)
}

function Load-Cfg {
    if (-not (Test-Path $script:configFile)) { return $null }
    try { 
        $x = [xml](Get-Content $script:configFile -Raw -EA 0); if (-not $x) { return $null }
        $o = New-Object Collections.ArrayList
        if ($x.C.O.C) { $o.Add('Config')|Out-Null }
        if ($x.C.O.S) { $o.Add('Saves')|Out-Null }
        if ($x.C.O.W) { $o.Add('Workshop')|Out-Null }
        if ($x.C.O.L) { $o.Add('LocalMods')|Out-Null }
        if ($x.C.O.G) { $o.Add('GameInfo')|Out-Null }
        if ($x.C.O.G2) { $o.Add('GameFiles')|Out-Null }
        if ($x.C.O.P) { $o.Add('Log')|Out-Null }
        $l = if ($x.C.L) { $x.C.L } else { "zh" }
        return @{ SteamPath = $x.C.S; Language = $l; Options = $o }
    } catch { return $null }
}

# ===== Form Initialization =====
$form = New-Object Windows.Forms.Form
$form.Size = New-Object Drawing.Size(920,760)
$form.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedSingle"
$form.MaximizeBox = $false
$form.Font = Fnt "Tahoma,9"
$form.BackColor = "#F0F0F5"
$title = nlb "RimWorld Backup Tool" 20 15 "16,Bold" "#2D2D3C"; $form.Controls.Add($title)
$sub = nlb "Backup and restore in one click." 22 48 "9" "Gray"; $form.Controls.Add($sub)

$chkLang = New-Object Windows.Forms.ComboBox
$chkLang.Items.AddRange(@("中文", "English"))
$chkLang.SelectedIndex = 0
$chkLang.DropDownStyle = "DropDownList"
$chkLang.Size = New-Object Drawing.Size(80,22)
$chkLang.Location = New-Object Drawing.Point(700, 15)
$form.Controls.Add($chkLang)

$tabControl = New-Object Windows.Forms.TabControl
$tabControl.Size = New-Object Drawing.Size(870,580)
$tabControl.Location = New-Object Drawing.Point(20,75)
$form.Controls.Add($tabControl)

# ===== Tab 1: Backup =====
$tab1 = New-Object Windows.Forms.TabPage; $tab1.Text = "1. Backup"; $tabControl.Controls.Add($tab1)
$grpP = ngb "Steam Path" 10 10 830 65
$lblP = nlb "Steam Library:" 15 28 "9"; $grpP.Controls.Add($lblP)
$txtP = ntb 115 26 480; $grpP.Controls.Add($txtP)
$btnBr = nbb "Browse" 610 25 70; $grpP.Controls.Add($btnBr)
$lblAuto = nlb "" 115 45 "8" "Gray"; $grpP.Controls.Add($lblAuto)
$tab1.Controls.Add($grpP)

$grpO = ngb "Backup Options" 10 85 830 165
$chkC = ncb "Mod Config" 15 25
$chkS = ncb "Game Saves" 15 50
$chkW = ncb "Workshop Mods" 15 75
$chkL = ncb "Local Mods" 15 100
$chkG = ncb "Game Info" 375 25
$chkG2 = ncb "Game Files" 375 50; $chkG2.Checked = $true
$chkP = ncb "Player.log" 375 75
$grpO.Controls.AddRange(@($chkC,$chkS,$chkW,$chkL,$chkG,$chkG2,$chkP))
$lblName = nlb "Backup Name:" 15 120; $grpO.Controls.Add($lblName)
$txtName = ntb 110 118 250; $txtName.Text = ""; $grpO.Controls.Add($txtName)
$lblNameHint = nlb "leave empty for auto-name" 370 122 "8" "Gray"; $grpO.Controls.Add($lblNameHint)
$tab1.Controls.Add($grpO)

$btnBk = nbb "Start Backup" 15 265 160 40
$btnBk.Font = Fnt "Microsoft YaHei UI,11,Bold"
$btnBk.BackColor = "#4190F0"; $btnBk.ForeColor = "White"
$tab1.Controls.Add($btnBk)
$lblSt = nlb "" 15 320; $lblSt.ForeColor = "#009900"; $lblSt.Font = Fnt "Tahoma,9,Bold"; $tab1.Controls.Add($lblSt)
$pb = New-Object Windows.Forms.ProgressBar
$pb.Size = New-Object Drawing.Size(810,22); $pb.Location = New-Object Drawing.Point(15,345); $pb.Minimum = 0; $pb.Maximum = 100; $tab1.Controls.Add($pb)
$lblDt = nlb "" 15 370; $lblDt.ForeColor = "Gray"; $lblDt.Font = Fnt "Tahoma,8"; $tab1.Controls.Add($lblDt)

# ===== Tab 2: Restore =====
$tab2 = New-Object Windows.Forms.TabPage; $tab2.Text = "2. Restore"; $tabControl.Controls.Add($tab2)
$grpR = ngb "Select Backup" 10 10 840 360
$lbZip = New-Object Windows.Forms.ListBox; $lbZip.Size = New-Object Drawing.Size(800,150); $lbZip.Location = New-Object Drawing.Point(15,25); $grpR.Controls.Add($lbZip)
$lblNo = nlb "No backups found." 20 80 "8" "Gray"; $lblNo.Visible = $false; $grpR.Controls.Add($lblNo)
$btnRef = nbb "Refresh" 15 185 90 28; $grpR.Controls.Add($btnRef)
$btnRs = nbb "Start Restore" 120 185 160 40; $btnRs.Font = Fnt "Tahoma,11,Bold"; $btnRs.BackColor = "#DC7832"; $btnRs.ForeColor = "White"; $btnRs.Enabled = $false; $grpR.Controls.Add($btnRs)
$lblRPath = nlb "Backup ZIP:" 15 235; $lblRPath.Font = Fnt "Tahoma,8"; $grpR.Controls.Add($lblRPath)
$txtRPath = ntb 15 255 800; $txtRPath.ReadOnly = $true; $grpR.Controls.Add($txtRPath)
$lblRInfo = nlb "" 15 290; $lblRInfo.Font = Fnt "Tahoma,8"; $lblRInfo.ForeColor = "Gray"; $grpR.Controls.Add($lblRInfo)
$pbRs = New-Object Windows.Forms.ProgressBar
$pbRs.Size = New-Object Drawing.Size(800,20); $pbRs.Location = New-Object Drawing.Point(15,315); $pbRs.Minimum = 0; $pbRs.Maximum = 100; $pbRs.Value = 0; $grpR.Controls.Add($pbRs)
$tab2.Controls.Add($grpR)
$lblRs = nlb "" 15 390; $lblRs.ForeColor = "#006400"; $lblRs.Font = Fnt "Tahoma,10"; $tab2.Controls.Add($lblRs)

$btnEx = nbb "Exit" 800 680 80 30; $form.Controls.Add($btnEx)

# ===== Load Config =====
$auto = Find-SteamAuto
$cfg = Load-Cfg
if ($cfg) { 
    $txtP.Text = $cfg.SteamPath
    if ($cfg.Language -eq "en") { $chkLang.SelectedIndex = 1; $script:lang = "en" } 
    $o = $cfg.Options
    if ($o) { 
        $chkC.Checked = $o.Contains('Config')
        $chkS.Checked = $o.Contains('Saves')
        $chkW.Checked = $o.Contains('Workshop')
        $chkL.Checked = $o.Contains('LocalMods')
        $chkG.Checked = $o.Contains('GameInfo')
        $chkG2.Checked = $o.Contains('GameFiles')
        $chkP.Checked = $o.Contains('Log') 
    } 
} elseif ($auto) { $txtP.Text = $auto }

function Refresh-Lang { 
    $form.Text = L "RimWorld 备份还原工具" "RimWorld Backup Tool"
    $title.Text = L "RimWorld 备份还原工具" "RimWorld Backup Tool"
    $sub.Text = L "一键备份和还原" "Backup and restore in one click."
    $tab1.Text = L " 备份 " " Backup "
    $tab2.Text = L " 还原 " " Restore "
    $grpP.Text = L " Steam 路径 " " Steam Path "
    $lblP.Text = L "Steam 库路径：" "Steam Library:"
    $btnBr.Text = L "浏览" "Browse"
    $grpO.Text = L " 备份内容 " " Options "
    $chkC.Text = L "Mod 配置 (ModsConfig.xml)" "Mod Config"
    $chkS.Text = L "游戏存档" "Game Saves"
    $chkW.Text = L "创意工坊 Mod (完整文件)" "Workshop Mods (Full)"
    $chkL.Text = L "本地 Mod (完整文件)" "Local Mods (Full)"
    $chkG.Text = L "游戏版本信息" "Game Info"
    $chkG2.Text = L "游戏安装文件" "Game Files"
    $chkP.Text = L "游戏日志" "Player.log"
    $btnBk.Text = L "开始备份" "Start Backup"
    $lblNo.Text = L "暂无备份。" "No backups found."
    $btnRef.Text = L "刷新" "Refresh"
    $btnRs.Text = L "开始还原" "Start Restore"
    $btnEx.Text = L "退出" "Exit"
    $lblName.Text = L "备份名称：" "Backup Name:"
    $lblNameHint.Text = L "留空则自动命名" "leave empty for auto-name"
    $lblRPath.Text = L "备份文件：" "Backup ZIP:"
    $grpR.Text = L " 选择备份 " " Select Backup "
    if ($auto) { $lblAuto.Text = L "已自动检测" "Auto-detected" } else { $lblAuto.Text = L "请选择 Steam 路径" "Select Steam path" }
    if ($txtP.Text -and (T $txtP.Text)) { $lblAuto.Text = L "路径有效" "Path OK" }
}

function Refresh-List { 
    $list = Get-BackupZips; $lbZip.Items.Clear()
    if ($list.Count -eq 0) { $lblNo.Visible = $true; $btnRs.Enabled = $false; $txtRPath.Text = ""; $lblRInfo.Text = "" } 
    else { 
        $lblNo.Visible = $false
        foreach ($i in $list) { $lbZip.Items.Add($i.DisplayName) | Out-Null }
        $lbZip.SelectedIndex = 0
        $btnRs.Enabled = $true 
    }
}

Refresh-Lang; Refresh-List

# ===== UI Events =====
$chkLang.Add_SelectedIndexChanged({ $script:lang = if ($chkLang.SelectedIndex -eq 1) { "en" } else { "zh" }; Refresh-Lang })

$btnBr.Add_Click({ 
    $d = New-Object Windows.Forms.FolderBrowserDialog; $d.Description = L "选择 Steam 库目录" "Select Steam Library"
    $d.ShowNewFolderButton = $false
    if ($txtP.Text -and (Test-Path $txtP.Text)) { $d.SelectedPath = $txtP.Text }
    if ($d.ShowDialog() -eq "OK") { 
        if (T $d.SelectedPath) { $txtP.Text = $d.SelectedPath; $lblAuto.Text = L "路径有效" "Path OK" } 
        else { $msg = L "未找到 RimWorld，请选择包含 steamapps\common\RimWorld 的目录。" "RimWorld not found."; [Windows.Forms.MessageBox]::Show($msg, "Error", "OK", "Warning") }
    }
})

$btnEx.Add_Click({ $form.Close() })

# ===== Backup Logic =====
$btnBk.Add_Click({
    try {
        $btnBk.Enabled = $false
        $form.Refresh()
        
        $steam = $txtP.Text.Trim()
        if (-not (T $steam)) { 
            $msg = L "请选择 Steam 路径。" "Select Steam path."
            [Windows.Forms.MessageBox]::Show($msg, "Error", "OK", "Error")
            $btnBk.Enabled = $true; return
        }
        
        $bn = $txtName.Text.Trim()
        if ($bn -eq "") { $bn = Get-Date -Format "yyyy-MM-dd_HH-mm-ss" }
        $root = Join-Path $script:rootDir "RimWorld_Backup"
        if (-not (Test-Path $root)) { New-Item $root -ItemType Directory | Out-Null }
        
        $zf = "$root\$bn.zip"
        if (Test-Path $zf) {
            $confirm = L "备份文件 '$bn.zip' 已存在，是否覆盖？" "Backup '$bn.zip' already exists. Overwrite?"
            if ([Windows.Forms.MessageBox]::Show($confirm, "Confirm", "YesNo", "Warning") -ne "Yes") { 
                $btnBk.Enabled = $true; return 
            }
            Remove-Item $zf -Force
        }
        
        $opts = New-Object Collections.ArrayList
        if ($chkC.Checked) { $opts.Add('Config')|Out-Null }
        if ($chkS.Checked) { $opts.Add('Saves')|Out-Null }
        if ($chkW.Checked) { $opts.Add('Workshop')|Out-Null }
        if ($chkL.Checked) { $opts.Add('LocalMods')|Out-Null }
        if ($chkG.Checked) { $opts.Add('GameInfo')|Out-Null }
        if ($chkG2.Checked) { $opts.Add('GameFiles')|Out-Null }
        if ($chkP.Checked) { $opts.Add('Log')|Out-Null }
        Save-Cfg $steam $opts $script:lang
        
        $isEn = ($script:lang -eq "en")
        $rw = "$steam\steamapps\common\RimWorld"
        $ws = "$steam\steamapps\workshop\content\294100"
        $ad = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios"
        
        $pb.Value = 0; $lblDt.Text = ""
        $totalFiles = 0
        
        try {
            $zip = [System.IO.Compression.ZipFile]::Open($zf, [System.IO.Compression.ZipArchiveMode]::Create)
            
            # 1. Config
            if ($chkC.Checked -and (Test-Path "$ad\Config")) {
                $lblSt.Text = if ($isEn) { "Config..." } else { "配置..." }; [System.Windows.Forms.Application]::DoEvents()
                Get-ChildItem "$ad\Config" -Filter "*.xml" -File -EA 0 | ForEach-Object {
                    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, "Config/$($_.Name)", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                    $totalFiles++
                }
            }
            # 2. Saves
            if ($chkS.Checked -and (Test-Path "$ad\Saves")) {
                $lblSt.Text = if ($isEn) { "Saves..." } else { "存档..." }; [System.Windows.Forms.Application]::DoEvents()
                Get-ChildItem "$ad\Saves" -Filter "*.rws" -File -EA 0 | ForEach-Object {
                    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, "Saves/$($_.Name)", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                    $totalFiles++
                }
            }
            # 3. Logs
            if ($chkP.Checked -and (Test-Path "$ad\Player.log")) {
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, "$ad\Player.log", "Logs/Player.log", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                $totalFiles++
            }
            # 4. GameInfo
            if ($chkG.Checked -and (Test-Path $rw)) {
                foreach ($f in @('Version.txt','LoadFolders.xml')) {
                    $fp = Join-Path $rw $f
                    if (Test-Path $fp) { 
                        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $fp, "GameInfo/$f", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                        $totalFiles++
                    }
                }
                $vf = Join-Path $rw "RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
                if (Test-Path $vf) {
                    $verText = (Get-Item $vf).VersionInfo.FileVersion
                    $tmpVer = Join-Path $env:TEMP "rw_ver.txt"
                    $verText | Out-File $tmpVer -Encoding UTF8
                    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $tmpVer, "GameInfo/Version.txt", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                    Remove-Item $tmpVer -Force -EA 0
                    $totalFiles++
                }
            }
            
            # 5. Workshop
            if ($chkW.Checked -and (Test-Path $ws)) {
                $lblSt.Text = if ($isEn) { "Workshop Mods..." } else { "工坊 Mod..." }
                $mods = Get-ChildItem $ws -Directory -EA 0
                $t = $mods.Count; $i = 0
                if ($t -gt 0) {
                    foreach ($mod in $mods) {
                        $i++
                        $pb.Value = [math]::Round(($i/$t)*50)
                        $lblDt.Text = "$i / $t"
                        [System.Windows.Forms.Application]::DoEvents()
                        
                        $modPath = $mod.FullName.TrimEnd('\')
                        $files = Get-DeepFiles $modPath
                        foreach ($f in $files) {
                            $rel = $f.Substring($modPath.Length).TrimStart('\').Replace('\','/')
                            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f, "Workshop/$($mod.Name)/$rel", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                            $totalFiles++
                        }
                    }
                }
            }
            $pb.Value = 50; [System.Windows.Forms.Application]::DoEvents()

            # 6. LocalMods
            if ($chkL.Checked -and (Test-Path "$rw\Mods")) {
                $lblSt.Text = if ($isEn) { "Local Mods..." } else { "本地 Mod..." }
                $mods = Get-ChildItem "$rw\Mods" -Directory -EA 0 | Where-Object { $_.Name -ne 'Place mods here.txt' }
                $t = $mods.Count; $i = 0
                if ($t -gt 0) {
                    foreach ($mod in $mods) {
                        $i++
                        $pb.Value = 50 + [math]::Round(($i/$t)*40)
                        $lblDt.Text = "$i / $t"
                        [System.Windows.Forms.Application]::DoEvents()
                        
                        $modPath = $mod.FullName.TrimEnd('\')
                        $files = Get-DeepFiles $modPath
                        foreach ($f in $files) {
                            $rel = $f.Substring($modPath.Length).TrimStart('\').Replace('\','/')
                            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f, "LocalMods/$($mod.Name)/$rel", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                            $totalFiles++
                        }
                    }
                }
            }
            $pb.Value = 90; [System.Windows.Forms.Application]::DoEvents()

            # 7. GameFiles
            if ($chkG2.Checked -and (Test-Path $rw)) {
                $lblSt.Text = if ($isEn) { "Game Files..." } else { "游戏文件..." }
                [System.Windows.Forms.Application]::DoEvents()
                
                $gameFilesList = New-Object System.Collections.Generic.List[string]
                foreach ($dirName in @('Data','RimWorldWin64_Data')) {
                    $dirPath = Join-Path $rw $dirName
                    if (Test-Path $dirPath -PathType Container) {
                        $deepFiles = Get-DeepFiles $dirPath
                        foreach ($df in $deepFiles) { $gameFilesList.Add($df) }
                    }
                }
                foreach ($fileName in @('Version.txt','LoadFolders.xml')) {
                    $filePath = Join-Path $rw $fileName
                    if (Test-Path $filePath -PathType Leaf) { $gameFilesList.Add($filePath) }
                }
                foreach ($pattern in @('*.dll','*.exe','*.cfg','*.txt')) {
                    Get-ChildItem -Path $rw -Filter $pattern -File -EA 0 | ForEach-Object { $gameFilesList.Add($_.FullName) }
                }
                
                $t = $gameFilesList.Count; $i = 0
                $rwClean = $rw.TrimEnd('\')
                foreach ($fPath in $gameFilesList) {
                    $i++
                    $pb.Value = 90 + [math]::Round(($i/$t)*10)
                    $lblDt.Text = "$i / $t"
                    [System.Windows.Forms.Application]::DoEvents()
                    
                    $rel = $fPath.Substring($rwClean.Length).TrimStart('\').Replace('\','/')
                    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $fPath, "GameFiles/$rel", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                    $totalFiles++
                }
            }
            
            $zip.Dispose()
            
            # === 0 文件拦截 ===
            if ($totalFiles -eq 0) {
                if (Test-Path $zf) { Remove-Item $zf -Force }
                $lblSt.Text = L "未找到文件" "No files found"
                $msg = L "未扫描到任何文件！请检查 Steam 路径是否正确，或勾选了要备份的内容。" "No files found! Check paths and options."
                [Windows.Forms.MessageBox]::Show($msg, "Warning", "OK", "Warning")
            } else {
                $pb.Value = 100; $lblSt.Text = L "备份完成!" "Done!"; $lblDt.Text = $zf
                Refresh-List
                $msg = L "备份完成!`n共备份 $totalFiles 个文件。`n文件路径：`n$zf" "Done!`n$totalFiles files saved.`nSaved to:`n$zf"
                [Windows.Forms.MessageBox]::Show($msg, "OK", "OK", "Information")
            }
        } catch {
            $lblSt.Text = "Error"; $lblDt.Text = $_.Exception.Message
            [Windows.Forms.MessageBox]::Show("备份失败，错误信息：`n$($_.Exception.Message)", "Error", "OK", "Error")
        } finally {
            $btnBk.Enabled = $true
        }
    } catch {
        $btnBk.Enabled = $true
        [Windows.Forms.MessageBox]::Show("点击备份时发生错误: $($_.Exception.Message)", "Error", "OK", "Error")
    }
})

# ===== Restore Logic =====
$btnRs.Add_Click({
    try {
        if ($lbZip.SelectedIndex -lt 0) { 
            $msg = L "请选择备份。" "Select a backup."
            [Windows.Forms.MessageBox]::Show($msg, "OK", "OK", "Warning"); return
        }
        
        $backups = Get-BackupZips
        $sel = $backups | Where-Object { $_.DisplayName -eq $lbZip.Items[$lbZip.SelectedIndex] } | Select-Object -First 1
        if (-not $sel) { 
            $msg = L "备份列表为空" "No backups found"
            [Windows.Forms.MessageBox]::Show($msg, "OK", "OK", "Warning"); return
        }
        
        $steam = $txtP.Text.Trim()
        if (-not (T $steam)) { 
            $msg = L "请设置 Steam 路径。" "Set Steam path first."
            [Windows.Forms.MessageBox]::Show($msg, "Error", "OK", "Error"); return
        }
        
        $confirmMsg = L "还原: $($sel.Name)`n确定继续?" "Restore: $($sel.Name)`nContinue?"
        if ([Windows.Forms.MessageBox]::Show($confirmMsg, "Confirm", "YesNo", "Warning") -ne "Yes") { return }
        
        $btnRs.Enabled = $false
        $lblRs.Text = L "还原中..." "Restoring..."
        $pbRs.Value = 0
        $form.Refresh()
        
        $isEn = ($script:lang -eq "en")
        $rw = "$steam\steamapps\common\RimWorld"
        $ws = "$steam\steamapps\workshop\content\294100"
        $ad = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios"
        
        try {
            $lblRs.Text = if ($isEn) { "Extracting files..." } else { "解压文件中..." }
            [System.Windows.Forms.Application]::DoEvents()
            
            $archive = [System.IO.Compression.ZipFile]::OpenRead($sel.Path)
            $t = $archive.Entries.Count; $i = 0
            $successCount = 0
            $failCount = 0
            
            foreach ($entry in $archive.Entries) {
                $i++
                # 统一替换为反斜杠，确保 StartsWith 匹配稳健
                $entryName = $entry.FullName.Replace('/', '\')
                $destPath = ""
                
                if ($entryName.StartsWith("Config\")) { $destPath = Join-Path "$ad\Config" $entryName.Substring(7) }
                elseif ($entryName.StartsWith("Saves\")) { $destPath = Join-Path "$ad\Saves" $entryName.Substring(6) }
                elseif ($entryName.StartsWith("Workshop\")) { $destPath = Join-Path $ws $entryName.Substring(9) }
                elseif ($entryName.StartsWith("LocalMods\")) { $destPath = Join-Path "$rw\Mods" $entryName.Substring(10) }
                elseif ($entryName.StartsWith("GameInfo\")) { $destPath = Join-Path $rw $entryName.Substring(9) }
                elseif ($entryName.StartsWith("Logs\")) { $destPath = Join-Path $ad $entryName.Substring(5) }
                elseif ($entryName.StartsWith("GameFiles\")) { $destPath = Join-Path $rw $entryName.Substring(10) }
                
                # 如果匹配到了路径，且不是目录条目
                if ($destPath -ne "" -and -not $entryName.EndsWith('\')) {
                    try {
                        $dir = Split-Path $destPath -Parent
                        if ($dir -and -not (Test-Path $dir)) { New-Item $dir -ItemType Directory -Force | Out-Null }
                        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)
                        $successCount++
                    } catch {
                        $failCount++
                    }
                }
                
                if ($i % 20 -eq 0 -or $i -eq $t) { 
                    $pbRs.Value = [math]::Round(($i/$t)*100)
                    $lblRs.Text = if ($isEn) { "Restoring $i/$t..." } else { "还原 $i/$t..." }
                    [System.Windows.Forms.Application]::DoEvents()
                }
            }
            $archive.Dispose()
            
            $pbRs.Value = 100
            $lblRs.Text = L "还原完成! 重启游戏生效。" "Done! Restart game to apply."
            $msgDone = L "还原完成!" "Done!"
            if ($failCount -gt 0) {
                $msgDone = L "还原完成! 成功 $successCount 个，失败 $failCount 个 (可能被占用)。" "Done! $successCount ok, $failCount failed."
            } else {
                $msgDone = L "还原完成! 共还原 $successCount 个文件。" "Done! $successCount files restored."
            }
            [Windows.Forms.MessageBox]::Show($msgDone, "OK", "OK", "Information")
        } catch {
            $errMsg = $_.Exception.Message
            if ($errMsg -match "being used by another process" -or $errMsg -match "正由另一进程使用") {
                $lblRs.Text = L "错误：请先关闭游戏！" "Error: Close game first!"
                [Windows.Forms.MessageBox]::Show((L "文件被占用，请确保 RimWorld 游戏已完全关闭！" "File in use. Ensure RimWorld is closed!"), "Error", "OK", "Warning")
            } else {
                $lblRs.Text = "Error"
                [Windows.Forms.MessageBox]::Show("还原失败，错误信息：`n$errMsg", "Error", "OK", "Error")
            }
        } finally {
            $btnRs.Enabled = $true
        }
    } catch {
        $btnRs.Enabled = $true
        [Windows.Forms.MessageBox]::Show("点击还原时发生错误: $($_.Exception.Message)", "Error", "OK", "Error")
    }
})

$btnRef.Add_Click({ Refresh-List })

$lbZip.Add_SelectedIndexChanged({ 
    $btnRs.Enabled = ($lbZip.SelectedIndex -ge 0)
    $selItem = $lbZip.SelectedItem
    if ($selItem) { 
        $list = Get-BackupZips
        $sel = $list | Where-Object { $_.DisplayName -eq $selItem } | Select-Object -First 1
        if ($sel) { $txtRPath.Text = $sel.Path; $lblRInfo.Text = L "文件大小: $($sel.Size)" "Size: $($sel.Size)" }
    }
})

$form.ShowDialog() | Out-Null
