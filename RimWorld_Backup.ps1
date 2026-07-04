#requires -version 4.0

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

function Fnt($s) { try { $p = $s -split ','; $sz = [float]$p[1]; $st = if ($p[2] -eq "Bold") { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }; return New-Object Drawing.Font("Tahoma", $sz, $st) } catch { return New-Object Drawing.Font("Tahoma",9) } }

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

function Get-DirSize($p) { if (-not (Test-Path $p)) { return "0 B" }; $l = (Get-ChildItem $p -Recurse -File -EA 0 | Measure-Object -Property Length -Sum).Sum; if (-not $l) { return "0 B" }; if ($l -gt 1GB) { return "{0:N1} GB" -f ($l/1GB) }; if ($l -gt 1MB) { return "{0:N1} MB" -f ($l/1MB) }; if ($l -gt 1KB) { return "{0:N1} KB" -f ($l/1KB) }; return "$l B" }

function Get-BackupZips {
    $root = Join-Path $script:rootDir "RimWorld_Backup"
    if (-not (Test-Path $root)) { return @() }
    $zips = Get-ChildItem $root -Filter "*.zip" -ErrorAction SilentlyContinue | Sort-Object Name -Descending
    if (-not $zips) { return @() }
    $result = @()
    foreach ($z in $zips) {
        $size = Get-FileSize($z.FullName)
        $result += New-Object PSObject -Property @{ Path = $z.FullName; Name = $z.BaseName; Size = $size; DisplayName = "$($z.BaseName)  -  $size" }
    }
    return $result
}

function Get-FileSize($p) { if (-not (Test-Path $p)) { return "0 B" }; $l = (Get-Item $p).Length; if ($l -gt 1GB) { return "{0:N1} GB" -f ($l/1GB) }; if ($l -gt 1MB) { return "{0:N1} MB" -f ($l/1MB) }; if ($l -gt 1KB) { return "{0:N1} KB" -f ($l/1KB) }; return "$l B" }

function nlb($t,$x,$y,$s,$c) { $r = New-Object Windows.Forms.Label; $r.Text = $t; $r.Location = New-Object Drawing.Point($x,$y); $r.AutoSize = $true; if ($s) { try { $fs = [float]($s -split ',' | Select-Object -First 1); $fb = ($s -like "*Bold*"); $sty = if ($fb) { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }; $r.Font = New-Object Drawing.Font("Tahoma", $fs, $sty) } catch {} }; if ($c) { $r.ForeColor = $c }; return $r }
function ncb($t,$x,$y) { $r = New-Object Windows.Forms.CheckBox; $r.Text = $t; $r.Size = New-Object Drawing.Size(310,22); $r.Location = New-Object Drawing.Point($x,$y); $r.Checked = $true; return $r }
function ntb($x,$y,$w) { $r = New-Object Windows.Forms.TextBox; $r.Size = New-Object Drawing.Size($w,25); $r.Location = New-Object Drawing.Point($x,$y); return $r }
function nbb($t,$x,$y,$w,$h) { $r = New-Object Windows.Forms.Button; $r.Text = $t; if ($h) { $r.Size = New-Object Drawing.Size($w,$h) } else { $r.Size = New-Object Drawing.Size($w,25) }; $r.Location = New-Object Drawing.Point($x,$y); return $r }
function ngb($t,$x,$y,$w,$h) { $r = New-Object Windows.Forms.GroupBox; $r.Text = " $t "; $r.Size = New-Object Drawing.Size($w,$h); $r.Location = New-Object Drawing.Point($x,$y); return $r }

function Restore-FromZip($zipPath, $rw, $ad) {
    if (-not (Test-Path $zipPath)) { throw "ZIP not found" }
    $tmp = Join-Path $env:TEMP "RimWorldRestore_$(Get-Random)"
    try {
        $pbRs.Value = 10; $form.Refresh()
        Expand-Archive -Path $zipPath -DestinationPath $tmp -Force
        $pbRs.Value = 30; $form.Refresh()
        $cfgSrc = Join-Path $tmp "Config"
        if (Test-Path $cfgSrc) {
            if (Test-Path "$cfgSrc\ModsConfig.xml") { Copy-Item "$cfgSrc\ModsConfig.xml" "$ad\Config\" -Force }
            if (Test-Path "$cfgSrc\Prefs.xml") { Copy-Item "$cfgSrc\Prefs.xml" "$ad\Config\" -Force }
            if (Test-Path "$cfgSrc\KeyPrefs.xml") { Copy-Item "$cfgSrc\KeyPrefs.xml" "$ad\Config\" -Force }
        }
        $pbRs.Value = 50; $form.Refresh()
        $svSrc = Join-Path $tmp "Saves"
        if (Test-Path $svSrc) { foreach ($f in (Get-ChildItem $svSrc -Filter "*.rws" -File)) { Copy-Item $f.FullName "$ad\Saves\" -Force } }
        $pbRs.Value = 70; $form.Refresh()
        $lmSrc = Join-Path $tmp "LocalMods"
        if (Test-Path $lmSrc) { foreach ($d in (Get-ChildItem $lmSrc -Directory)) {
            if (Test-Path (Join-Path $d.FullName "About.xml")) { $md = "$rw\Mods\$($d.Name)\About"; New-Item $md -ItemType Directory -Force -EA 0 | Out-Null; Copy-Item (Join-Path $d.FullName "About.xml") "$md\" -Force }
            if (Test-Path (Join-Path $d.FullName "PreviewImage.png")) { $md = "$rw\Mods\$($d.Name)\About"; New-Item $md -ItemType Directory -Force -EA 0 | Out-Null; Copy-Item (Join-Path $d.FullName "PreviewImage.png") "$md\" -Force }
        }}
        $pbRs.Value = 90; $form.Refresh()
    } finally { Remove-Item $tmp -Recurse -Force -EA 0; $pbRs.Value = 100; $form.Refresh() }
    return "OK"
}

function Save-Cfg($sp, $opts, $lang) {
    $x = '<?xml version="1.0"?><C><S>'+$sp+'</S><L>'+$lang+'</L><O>'
    if ($opts.Contains('Config')) { $x+='<C/>' }; if ($opts.Contains('Saves')) { $x+='<S/>' }; if ($opts.Contains('Workshop')) { $x+='<W/>' }
    if ($opts.Contains('LocalMods')) { $x+='<L/>' }; if ($opts.Contains('GameInfo')) { $x+='<G/>' }; if ($opts.Contains('Log')) { $x+='<P/>' }; if ($opts.Contains('Zip')) { $x+='<Z/>' }
    $x+='</O></C>'
    $x | Out-File $script:configFile -Encoding UTF8
}

function Load-Cfg {
    if (-not (Test-Path $script:configFile)) { return $null }
    try { $x = [xml](Get-Content $script:configFile -Raw -EA 0); if (-not $x) { return $null }
        $o = New-Object Collections.ArrayList
        if ($x.C.O.C) { $o.Add('Config')|Out-Null }; if ($x.C.O.S) { $o.Add('Saves')|Out-Null }; if ($x.C.O.W) { $o.Add('Workshop')|Out-Null }
        if ($x.C.O.L) { $o.Add('LocalMods')|Out-Null }; if ($x.C.O.G) { $o.Add('GameInfo')|Out-Null }; if ($x.C.O.P) { $o.Add('Log')|Out-Null }; if ($x.C.O.Z) { $o.Add('Zip')|Out-Null }
        $l = if ($x.C.L) { $x.C.L } else { "zh" }
        return @{ SteamPath = $x.C.S; Language = $l; Options = $o }
    } catch { return $null }
}

# ===== Form =====
$form = New-Object Windows.Forms.Form
$form.Size = New-Object Drawing.Size(920,760); $form.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi; $form.StartPosition = "CenterScreen"; $form.FormBorderStyle = "FixedSingle"; $form.MaximizeBox = $false; $form.Font = Fnt "Tahoma,9"; $form.BackColor = "#F0F0F5"
$title = nlb "RimWorld Backup Tool" 20 15 "16,Bold" "#2D2D3C"; $form.Controls.Add($title)
$sub = nlb "Backup and restore in one click." 22 48 "9" "Gray"; $form.Controls.Add($sub)

$chkLang = New-Object Windows.Forms.ComboBox
$chkLang.Items.AddRange(@("中文", "English"))
$chkLang.SelectedIndex = 0
$chkLang.DropDownStyle = "DropDownList"
$chkLang.Size = New-Object Drawing.Size(80,22)
$chkLang.Location = New-Object Drawing.Point(700, 15)
$form.Controls.Add($chkLang)

$tabControl = New-Object Windows.Forms.TabControl; $tabControl.Size = New-Object Drawing.Size(870,580); $tabControl.Location = New-Object Drawing.Point(20,75)
$form.Controls.Add($tabControl)

# ===== Tab 1 =====
$tab1 = New-Object Windows.Forms.TabPage; $tab1.Text = "1. Backup"; $tabControl.Controls.Add($tab1)

$grpP = ngb "Steam Path" 10 10 830 65
$lblP = nlb "Steam Library:" 15 28 "9"; $grpP.Controls.Add($lblP)
$txtP = ntb 115 26 480; $grpP.Controls.Add($txtP)
$btnBr = nbb "Browse" 610 25 70; $grpP.Controls.Add($btnBr)
$lblAuto = nlb "" 115 45 "8" "Gray"; $grpP.Controls.Add($lblAuto)
$tab1.Controls.Add($grpP)

$grpO = ngb "Backup Options" 10 85 830 165
$chkC = ncb "Mod Config" 15 25; $chkS = ncb "Game Saves" 15 50; $chkW = ncb "Workshop Mods" 15 75
$chkL = ncb "Local Mods" 15 100
$chkG = ncb "Game Info" 375 25
$chkG2 = ncb "Game Files" 375 50; $chkG2.Checked = $true
$chkP = ncb "Player.log" 375 75
$grpO.Controls.AddRange(@($chkC,$chkS,$chkW,$chkL,$chkG,$chkG2,$chkP))
$lblName = nlb "Backup Name:" 15 120; $grpO.Controls.Add($lblName)
$txtName = ntb 110 118 250; $txtName.Text = ""; $grpO.Controls.Add($txtName)
$lblNameHint = nlb "leave empty for auto-name" 370 122 "8" "Gray"; $grpO.Controls.Add($lblNameHint)
$tab1.Controls.Add($grpO)

$btnBk = nbb "Start Backup" 15 265 160 40; $btnBk.Font = Fnt "Microsoft YaHei UI,11,Bold"; $btnBk.BackColor = "#4190F0"; $btnBk.ForeColor = "White"
$tab1.Controls.Add($btnBk)

$lblSt = nlb "" 15 320; $lblSt.ForeColor = "#009900"; $lblSt.Font = Fnt "Tahoma,9,Bold"; $tab1.Controls.Add($lblSt)
$pb = New-Object Windows.Forms.ProgressBar
$pb.Size = New-Object Drawing.Size(810,22); $pb.Location = New-Object Drawing.Point(15,345); $pb.Minimum = 0; $pb.Maximum = 100; $tab1.Controls.Add($pb)
$lblDt = nlb "" 15 370; $lblDt.ForeColor = "Gray"; $lblDt.Font = Fnt "Tahoma,8"; $tab1.Controls.Add($lblDt)

# ===== Tab 2 =====
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

# ===== Init =====
$auto = Find-SteamAuto
$cfg = Load-Cfg

if ($cfg) { $txtP.Text = $cfg.SteamPath; if ($cfg.Language -eq "en") { $chkLang.SelectedIndex = 1; $script:lang = "en" }
    $o = $cfg.Options; if ($o) { $chkC.Checked = $o.Contains('Config'); $chkS.Checked = $o.Contains('Saves'); $chkW.Checked = $o.Contains('Workshop'); $chkL.Checked = $o.Contains('LocalMods'); $chkG.Checked = $o.Contains('GameInfo'); $chkP.Checked = $o.Contains('Log') }
} elseif ($auto) { $txtP.Text = $auto }

function Refresh-Lang {
    $e = ($script:lang -eq "en")
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
    $chkS.Text = L "游戏存档 (.rws)" "Game Saves"
    $chkW.Text = L "创意工坊 Mod" "Workshop Mods"
    $chkL.Text = L "本地 Mod" "Local Mods"
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
    if ($auto) { $lblAuto.Text = L "已自动检测" "Auto-detected" }
    else { $lblAuto.Text = L "请选择 Steam 路径" "Select Steam path" }
    if ($txtP.Text -and (T $txtP.Text)) { $lblAuto.Text = L "路径有效" "Path OK" }
}

function Refresh-List {
    $list = Get-BackupZips; $lbZip.Items.Clear()
    if ($list.Count -eq 0) { $lblNo.Visible = $true; $btnRs.Enabled = $false; $txtRPath.Text = ""; $lblRInfo.Text = "" }
    else { $lblNo.Visible = $false; foreach ($i in $list) { $lbZip.Items.Add($i.DisplayName) | Out-Null }; $lbZip.SelectedIndex = 0; $btnRs.Enabled = $true }
}

Refresh-Lang; Refresh-List

# ===== Events =====
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

$btnBk.Add_Click({
    $btnBk.Enabled = $false; $steam = $txtP.Text.Trim()
    if (-not (T $steam)) {
        $msg = L "请选择 Steam 路径。" "Select Steam path."
        [Windows.Forms.MessageBox]::Show($msg, "Error", "OK", "Error")
        $btnBk.Enabled = $true; return
    }
    $rw = "$steam\steamapps\common\RimWorld"; $ws = "$steam\steamapps\workshop\content\294100"
    $ad = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios"
    $bn = $txtName.Text.Trim()
    if ($bn -eq "") { $bn = Get-Date -Format "yyyy-MM-dd_HH-mm-ss" }
    $root = Join-Path $script:rootDir "RimWorld_Backup"
    if (-not (Test-Path $root)) { New-Item $root -ItemType Directory | Out-Null }
    $tmp = Join-Path $env:TEMP "RWBackup_$(Get-Random)"
    New-Item $tmp -ItemType Directory -Force | Out-Null
    $step = 0
    $total = @($chkC,$chkS,$chkW,$chkL,$chkG,$chkG2,$chkP|Where-Object{$_.Checked}).Count; if ($total -eq 0) { $total = 1 }
    function up($m,$p) { $lblSt.Text = $m; $pb.Value = [Math]::Min($p,99); $form.Refresh(); Start-Sleep -Milliseconds 20 }
    function ud($m) { $lblDt.Text = $m; $form.Refresh(); Start-Sleep -Milliseconds 10 }

    try {
        if ($chkC.Checked) { $step++; up(L "[1/6] Mod 配置..." "[1/6] Config...") ([math]::Round($step/$total*100))
            $cd = "$ad\Config"; if (Test-Path $cd) { $d = "$tmp\Config"; New-Item $d -ItemType Directory -Force|Out-Null
                Copy-Item "$cd\ModsConfig.xml","$cd\Prefs.xml","$cd\KeyPrefs.xml" "$d\" -Force -EA 0; ud "OK" } }
        if ($chkS.Checked) { $step++; up(L "[2/6] 存档..." "[2/6] Saves...") ([math]::Round($step/$total*100))
            $sd = "$ad\Saves"; if (Test-Path $sd) { $d = "$tmp\Saves"; New-Item $d -ItemType Directory -Force|Out-Null
                foreach ($f in (Get-ChildItem $sd -Filter "*.rws" -File)) { Copy-Item $f.FullName "$d\" -Force }; ud "OK: $(@(Get-ChildItem $sd -Filter '*.rws' -File).Count)" } }
        if ($chkW.Checked) { $step++; up(L "[3/6] 创意工坊 Mod..." "[3/6] Workshop...") ([math]::Round($step/$total*100))
            if (Test-Path $ws) { $d = "$tmp\Workshop"; New-Item $d -ItemType Directory -Force|Out-Null; $ds = Get-ChildItem $ws -Directory; $t = $ds.Count; $i = 0
                foreach ($m in $ds) { $i++; $md = Join-Path $d $m.Name; New-Item $md -ItemType Directory -Force|Out-Null
                    if (Test-Path (Join-Path $m.FullName "About\About.xml")) { Copy-Item (Join-Path $m.FullName "About\About.xml") "$md\" -Force }
                    if ($i % 30 -eq 0 -or $i -eq $t) { ud "$i/$t" } }; ud "OK: $t" } }
        if ($chkL.Checked) { $step++; up(L "[4/6] 本地 Mod..." "[4/6] Local mods...") ([math]::Round($step/$total*100))
            $lm = "$rw\Mods"; if (Test-Path $lm) { $d = "$tmp\LocalMods"; New-Item $d -ItemType Directory -Force|Out-Null
                $ds = Get-ChildItem $lm -Directory|Where-Object{$_.Name -ne "Place mods here.txt"}; $t = $ds.Count; $i = 0
                foreach ($m in $ds) { $i++; $md = Join-Path $d $m.Name; New-Item $md -ItemType Directory -Force|Out-Null
                    if (Test-Path (Join-Path $m.FullName "About\About.xml")) { Copy-Item (Join-Path $m.FullName "About\About.xml") "$md\" -Force }
                    if ($i % 10 -eq 0 -or $i -eq $t) { ud "$i/$t" } }; ud "OK: $t" } }
        if ($chkG.Checked) { $step++; up(L "[5/6] 游戏信息..." "[5/6] Game info...") ([math]::Round($step/$total*100))
            $d = "$tmp\GameInfo"; New-Item $d -ItemType Directory -Force|Out-Null
            if (Test-Path "$rw\Version.txt") { Copy-Item "$rw\Version.txt" "$d\" -Force }
            Copy-Item "$rw\LoadFolders.xml" "$d\" -Force -EA 0
            $vf = "$rw\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
            if (Test-Path $vf) { (Get-Item $vf).VersionInfo.FileVersion | Out-File "$d\Version.txt" -Encoding UTF8 }; ud "OK" }
        if ($chkP.Checked) { $step++; up(L "[6/7] 日志..." "[6/7] Player.log...") ([math]::Round($step/$total*100))
            if (Test-Path "$ad\Player.log") { $d = "$tmp\Logs"; New-Item $d -ItemType Directory -Force|Out-Null; Copy-Item "$ad\Player.log" "$d\" -Force; ud "OK" } }
        if ($chkG2.Checked) { $step++; up(L "[7/7] 游戏本体..." "[7/7] Game files...") ([math]::Round($step/$total*100))
            $rw2 = "$steam\steamapps\common\RimWorld"; if (Test-Path $rw2) { $d = "$tmp\GameFiles"; New-Item $d -ItemType Directory -Force|Out-Null
                Copy-Item "$rw2\Version.txt" "$d" -Force -EA 0
                Copy-Item "$rw2\*.dll" "$d" -Force -EA 0; Copy-Item "$rw2\*.exe" "$d" -Force -EA 0
                Copy-Item "$rw2\*.cfg" "$d" -Force -EA 0; Copy-Item "$rw2\*.txt" "$d" -Force -EA 0
                Copy-Item "$rw2\LoadFolders.xml" "$d" -Force -EA 0
                if (Test-Path "$rw2\Data") { Copy-Item "$rw2\Data" "$d" -Recurse -Force -EA 0 }
                if (Test-Path "$rw2\RimWorldWin64_Data") { Copy-Item "$rw2\RimWorldWin64_Data\Managed\*" "$d\Managed" -Recurse -Force -EA 0 }
                ud "OK" } }

        up(L "压缩中..." "Compressing...") 95
        $zf = "$root\$bn.zip"
        Compress-Archive -Path "$tmp\*" -DestinationPath $zf -CompressionLevel Optimal -Force
        Remove-Item $tmp -Recurse -Force -EA 0

        $pb.Value = 100; $lblSt.Text = L "备份完成!" "Done!"
        $lblDt.Text = "$zf"; Refresh-List
        $msg = L "备份完成!" "Done!"
        [Windows.Forms.MessageBox]::Show($msg, "OK", "OK", "Information")
    } catch { $lblSt.Text = "Error"; $lblDt.Text = "$_"; [Windows.Forms.MessageBox]::Show("$_","Error","OK","Error") }
    finally { $btnBk.Enabled = $true }
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

$btnRs.Add_Click({
    try {
        if ($lbZip.SelectedIndex -lt 0) {
            $msg = L "请选择备份。" "Select a backup."
            [Windows.Forms.MessageBox]::Show($msg, "OK", "OK", "Warning"); return
        }
        $selPath = $lbZip.Items[$lbZip.SelectedIndex]
        # get corresponding backup
        $backups = Get-BackupZips
        $sel = $null
        foreach ($b in $backups) { if ($b.DisplayName -eq $selPath) { $sel = $b; break } }
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
        $btnRs.Enabled = $false; $lblRs.Text = L "还原中..." "Restoring..."
        $form.Refresh()
        Restore-FromZip -zipPath $sel.Path -rw "$steam\steamapps\common\RimWorld" -ad "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios"
        $lblRs.Text = L "还原完成! 重启游戏生效。" "Done! Restart game to apply."
        $msg = L "还原完成!" "Done!"
        [Windows.Forms.MessageBox]::Show($msg, "OK", "OK", "Information")
    } catch { $lblRs.Text = "Error"; [Windows.Forms.MessageBox]::Show("$_","Error","OK","Error") }
    finally { $btnRs.Enabled = $true }
})

$form.ShowDialog() | Out-Null
