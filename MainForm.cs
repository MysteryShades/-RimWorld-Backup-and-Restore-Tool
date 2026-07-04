#nullable disable
using System.Drawing.Drawing2D;

namespace RimWorldBackup;

// ===================== Theme =====================
internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(0x2C, 0x25, 0x20);
    public static readonly Color CardBg = Color.FromArgb(0x3E, 0x35, 0x2C);
    public static readonly Color Border = Color.FromArgb(0x5C, 0x4F, 0x42);
    public static readonly Color HeaderBg = Color.FromArgb(0x1E, 0x19, 0x14);
    public static readonly Color Accent = Color.FromArgb(0xD4, 0x83, 0x2A);
    public static readonly Color AccentHover = Color.FromArgb(0xE6, 0x94, 0x3A);
    public static readonly Color AccentPress = Color.FromArgb(0xB8, 0x6E, 0x20);
    public static readonly Color Warning = Color.FromArgb(0xC7, 0x5B, 0x3A);
    public static readonly Color WarningHover = Color.FromArgb(0xD6, 0x6A, 0x48);
    public static readonly Color WarningPress = Color.FromArgb(0xA8, 0x4E, 0x30);
    public static readonly Color Danger = Color.FromArgb(0xB0, 0x40, 0x40);
    public static readonly Color DangerHover = Color.FromArgb(0xC8, 0x50, 0x50);
    public static readonly Color DangerPress = Color.FromArgb(0x90, 0x30, 0x30);
    public static readonly Color Success = Color.FromArgb(0x5A, 0x8F, 0x5A);
    public static readonly Color TextMain = Color.FromArgb(0xF0, 0xE6, 0xD6);
    public static readonly Color TextMuted = Color.FromArgb(0xC8, 0xBC, 0xA8);
    public static readonly Color ControlBg = Color.FromArgb(0x4A, 0x42, 0x38);
    public static readonly Color Disabled = Color.FromArgb(0x80, 0x78, 0x70);
}

// ===================== ModernButton =====================
public enum ModernButtonStyle { Primary, Secondary, Warning, Danger }
public class ModernButton : Button
{
    public ModernButtonStyle Style { get; set; } = ModernButtonStyle.Primary;
    private bool _hover, _pressed;
    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Font = new Font("Segoe UI", 9F); Cursor = Cursors.Hand; ForeColor = Color.White;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
    }
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs m) { _pressed = true; Invalidate(); base.OnMouseDown(m); }
    protected override void OnMouseUp(MouseEventArgs m) { _pressed = false; Invalidate(); base.OnMouseUp(m); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnVisibleChanged(EventArgs e) { if (Visible && IsHandleCreated) { Invalidate(ClientRectangle, true); Update(); } base.OnVisibleChanged(e); }
    protected override void OnResize(EventArgs e) { Invalidate(); base.OnResize(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        Color bg = !Enabled ? Theme.ControlBg : _pressed ? GetPressedColor() : _hover ? GetHoverColor() : GetBaseColor();
        using var path = RoundedRect(rect, 6);
        using (var brush = new SolidBrush(bg)) g.FillPath(brush, path);
        if (Style == ModernButtonStyle.Secondary) { using var pen = new Pen(Theme.Border, 1); g.DrawPath(pen, path); }
        TextRenderer.DrawText(g, Text, Font, rect, !Enabled ? Theme.Disabled : Style == ModernButtonStyle.Secondary ? Theme.TextMain : Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
    private Color GetBaseColor() => Style switch { ModernButtonStyle.Primary => Theme.Accent, ModernButtonStyle.Warning => Theme.Warning, ModernButtonStyle.Danger => Theme.Danger, _ => Theme.ControlBg };
    private Color GetHoverColor() => Style switch { ModernButtonStyle.Primary => Theme.AccentHover, ModernButtonStyle.Warning => Theme.WarningHover, ModernButtonStyle.Danger => Theme.DangerHover, _ => Color.FromArgb(0x5C, 0x4F, 0x42) };
    private Color GetPressedColor() => Style switch { ModernButtonStyle.Primary => Theme.AccentPress, ModernButtonStyle.Warning => Theme.WarningPress, ModernButtonStyle.Danger => Theme.DangerPress, _ => Color.FromArgb(0x3E, 0x35, 0x2C) };
    public static GraphicsPath RoundedRect(Rectangle r, int radius) { var p = new GraphicsPath(); int d = radius * 2; if (d > r.Width) d = r.Width; if (d > r.Height) d = r.Height; radius = d / 2; p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
}

// ===================== ModernTabControl =====================
public class ModernTabControl : TabControl
{
    private readonly Font _boldFont;
    public ModernTabControl() { DrawMode = TabDrawMode.OwnerDrawFixed; ItemSize = new Size(120, 38); SizeMode = TabSizeMode.Fixed; Padding = new Point(24, 8); Font = new Font("Segoe UI", 10F); _boldFont = new Font(Font, FontStyle.Bold); DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; using var bg = new SolidBrush(Theme.Bg); g.FillRectangle(bg, ClientRectangle); using var b = new Pen(Theme.Border, 1); g.DrawLine(b, 0, ItemSize.Height + 1, Width, ItemSize.Height + 1); }
    protected override void OnDrawItem(DrawItemEventArgs e) { var g = e.Graphics; var tabRect = GetTabRect(e.Index); bool active = e.Index == SelectedIndex; using var tbg = new SolidBrush(active ? Theme.CardBg : Theme.Bg); g.FillRectangle(tbg, tabRect); if (active) { using var a = new SolidBrush(Theme.Accent); g.FillRectangle(a, tabRect.X, tabRect.Bottom - 3, tabRect.Width, 3); } TextRenderer.DrawText(g, TabPages[e.Index].Text, active ? _boldFont : Font, tabRect, active ? Theme.TextMain : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

// ===================== Card =====================
public class Card : Panel { private static readonly Font TitleFont = new("Segoe UI", 9.5F, FontStyle.Bold); public Card() { BackColor = Theme.CardBg; DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true); } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; using var p = new Pen(Theme.Border, 1); var r = new Rectangle(0, 0, Width - 1, Height - 1); g.DrawRectangle(p, r); if (!string.IsNullOrEmpty(Text)) { var sz = TextRenderer.MeasureText(g, Text, TitleFont); using var bg = new SolidBrush(Theme.CardBg); g.FillRectangle(bg, 14, 2, sz.Width + 10, sz.Height + 16); TextRenderer.DrawText(g, Text, TitleFont, new Point(18, 14), Theme.Accent); } } }

// ===================== ModernProgress =====================
public class ModernProgress : Control
{
    private int _value, _maximum = 100;
    public int Value { get => _value; set { _value = Math.Max(0, Math.Min(_maximum, value)); Invalidate(); } }
    public int Maximum { get => _maximum; set { _maximum = Math.Max(1, value); Invalidate(); } }
    public ModernProgress() { DoubleBuffered = true; Height = 18; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true); }
    protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; var r = new Rectangle(0, 0, Width - 1, Height - 1); using var path = ModernButton.RoundedRect(r, 5); using (var bg = new SolidBrush(Theme.ControlBg)) g.FillPath(bg, path); if (_value > 0 && _maximum > 0) { int fillW = (int)((float)_value / _maximum * (Width - 1)); if (fillW > 0) { var fillRect = new Rectangle(0, 0, fillW + 1, Height); using var fp = ModernButton.RoundedRect(fillRect, 5); using var clip = new Region(path); g.Clip = clip; using var fill = new SolidBrush(Theme.Accent); g.FillPath(fill, fp); g.ResetClip(); } } using var pen = new Pen(Theme.Border, 1); g.DrawPath(pen, path); }
}

// ===================== ModernCheckBox =====================
public class ModernCheckBox : CheckBox
{
    public ModernCheckBox() { FlatStyle = FlatStyle.Flat; BackColor = Theme.CardBg; ForeColor = Theme.TextMain; AutoSize = false; Cursor = Cursors.Hand; AutoCheck = false; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick, true); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location)) { Checked = !Checked; OnCheckedChanged(EventArgs.Empty); } base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { Checked = !Checked; OnCheckedChanged(EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; using var bg = new SolidBrush(BackColor); g.FillRectangle(bg, ClientRectangle); int boxSize = 16, boxY = (Height - boxSize) / 2; var boxRect = new Rectangle(2, boxY, boxSize, boxSize); if (Checked) { using var fill = new SolidBrush(Theme.Accent); g.FillRectangle(fill, boxRect); } else { using var border = new Pen(Color.FromArgb(0xB0, 0xA0, 0x90), 1); g.DrawRectangle(border, boxRect); } if (Checked) { using var pen = new Pen(Color.Black, 2); g.DrawLine(pen, boxRect.X + 3, boxRect.Y + 8, boxRect.X + 7, boxRect.Y + 12); g.DrawLine(pen, boxRect.X + 7, boxRect.Y + 12, boxRect.X + 13, boxRect.Y + 4); } TextRenderer.DrawText(g, Text, Font, new Rectangle(22, 0, Width - 24, Height), ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left); }
}

// ===================== MainForm =====================
public partial class MainForm : Form
{
    private bool _langEn;
    private readonly BackupManager _bm;
    private readonly string _rootDir;
    private CancellationTokenSource _cts;
    private List<BackupEntry> _cachedZips = new();

    private ModernTabControl _tabControl;
    private ComboBox _chkLang;
    private ModernButton _btnExit;
    private Panel _headerPanel, _footerPanel;
    private Label _title, _subtitle;
    private TextBox _txtPath;
    private Label _lblPathStatus;
    private Card _grpPath, _grpOpt, _grpRestore;
    private Label _lblPathLabel, _lblName, _lblNameHint, _lblRPathLabel;
    private CheckBox _chkConfig, _chkSaves, _chkWorkshop, _chkLocal, _chkGameInfo, _chkGameFiles, _chkPlayerLog;
    private TextBox _txtName;
    private ModernButton _btnBackup, _btnCancel, _btnBrowse;
    private Label _lblStatus;
    private ModernProgress _pb;
    private Panel _backupBottomPanel;
    private ListBox _lbZips;
    private Label _lblNoBackups, _lblRInfo;
    private TextBox _txtRPath;
    private ModernProgress _pbRestore;
    private ModernButton _btnRefresh, _btnRestore;
    private Label _lblRestoreStatus;

    public MainForm()
    {
        _rootDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Directory.GetCurrentDirectory();
        _bm = new BackupManager(_rootDir);
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(940, 720); FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = true; MinimumSize = new Size(820, 720); StartPosition = FormStartPosition.CenterScreen; BackColor = Theme.Bg; Font = new Font("Segoe UI", 9F); Text = "RimWorld Backup Tool"; DoubleBuffered = true;
        BuildHeader(); BuildFooter(); BuildTabControl(); LoadConfig(); RefreshZipList(); RefreshLang();
    }

    private void BuildHeader()
    {
        _headerPanel = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Theme.HeaderBg };
        _headerPanel.Paint += (_, e) => { using var b = new SolidBrush(Theme.Accent); e.Graphics.FillRectangle(b, 0, _headerPanel.Height - 3, _headerPanel.Width, 3); };
        var ht = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.HeaderBg, ColumnCount = 2, RowCount = 2, Padding = new Padding(24, 14, 20, 0) };
        ht.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); ht.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        ht.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); ht.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        _title = new Label { Text = "RimWorld 备份还原工具", AutoSize = true, Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent, Dock = DockStyle.Bottom };
        _subtitle = new Label { Text = "一键备份和还原", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(0xB0, 0xB0, 0xC4), BackColor = Theme.HeaderBg, Dock = DockStyle.Top };
        _chkLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F), BackColor = Theme.CardBg, ForeColor = Theme.TextMain, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
        _chkLang.Items.AddRange(new[] { "中文", "English" }); _chkLang.SelectedIndex = 0;
        _chkLang.SelectedIndexChanged += (_, _) => { _langEn = _chkLang.SelectedIndex == 1; RefreshLang(); };
        ht.Controls.Add(_title, 0, 0); ht.Controls.Add(_subtitle, 0, 1); ht.Controls.Add(_chkLang, 1, 0); ht.SetRowSpan(_chkLang, 2);
        _headerPanel.Controls.Add(ht); Controls.Add(_headerPanel);
    }

    private void BuildFooter()
    {
        _footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Bg };
        var ft = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.Bg, ColumnCount = 2, RowCount = 1, Padding = new Padding(20, 8, 20, 8) };
        ft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); ft.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _btnExit = new ModernButton { Size = new Size(90, 32), Style = ModernButtonStyle.Secondary, Text = "退出" }; _btnExit.Click += (_, _) => Close();
        ft.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg }, 0, 0); ft.Controls.Add(_btnExit, 1, 0);
        _footerPanel.Controls.Add(ft); Controls.Add(_footerPanel);
    }

    private void BuildTabControl() { _tabControl = new ModernTabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) }; BuildBackupTab(); BuildRestoreTab(); Controls.Add(_tabControl); _tabControl.BringToFront(); }

    private void BuildBackupTab()
    {
        var tab = new TabPage { BackColor = Theme.Bg, Padding = new Padding(0), AutoScroll = true };

        var pageTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            BackColor = Theme.Bg,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(15, 10, 15, 10),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        pageTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pageTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pageTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _grpPath = new Card { Dock = DockStyle.Fill, Text = "Steam 路径", Padding = new Padding(16, 36, 16, 16), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var pt = new TableLayoutPanel { Dock = DockStyle.Top, BackColor = Theme.CardBg, ColumnCount = 3, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        pt.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); pt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pt.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        pt.RowStyles.Add(new RowStyle(SizeType.AutoSize)); pt.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        _lblPathLabel = new Label { Text = "Steam 库路径：", AutoSize = false, Width = 150, Font = new Font("Segoe UI", 9F), ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 4, 0, 0) };
        _txtPath = new TextBox { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F), BackColor = Theme.CardBg, ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 2, 8, 2) };
        _btnBrowse = new ModernButton { Size = new Size(80, 28), Style = ModernButtonStyle.Secondary, Text = "浏览", Anchor = AnchorStyles.Top | AnchorStyles.Right }; _btnBrowse.Click += BtnBrowse_Click;
        _lblPathStatus = new Label { Text = "", AutoSize = true, Font = new Font("Segoe UI", 8.25F), ForeColor = Theme.TextMuted, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        pt.Controls.Add(_lblPathLabel, 0, 0); pt.Controls.Add(_txtPath, 1, 0); pt.Controls.Add(_btnBrowse, 2, 0); pt.Controls.Add(_lblPathStatus, 1, 1); pt.SetColumnSpan(_lblPathStatus, 2);
        _grpPath.Controls.Add(pt); pageTable.Controls.Add(_grpPath, 0, 0);

        _grpOpt = new Card { Dock = DockStyle.Fill, Text = "备份内容", Padding = new Padding(16, 36, 16, 16), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var ot = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, ColumnCount = 2, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        ot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); ot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); ot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var ck = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, ColumnCount = 2, RowCount = 4, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        ck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); ck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 4; i++) ck.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _chkConfig = MakeCheck("Mod 配置"); _chkSaves = MakeCheck("游戏存档"); _chkWorkshop = MakeCheck("创意工坊 Mod"); _chkLocal = MakeCheck("本地 Mod");
        _chkGameInfo = MakeCheck("游戏版本信息"); _chkGameFiles = MakeCheck("游戏安装文件"); _chkGameFiles.Checked = true; _chkPlayerLog = MakeCheck("游戏日志");
        ck.Controls.Add(_chkConfig, 0, 0); ck.Controls.Add(_chkSaves, 0, 1); ck.Controls.Add(_chkWorkshop, 0, 2); ck.Controls.Add(_chkLocal, 0, 3);
        ck.Controls.Add(_chkGameInfo, 1, 0); ck.Controls.Add(_chkGameFiles, 1, 1); ck.Controls.Add(_chkPlayerLog, 1, 2);
        ot.Controls.Add(ck, 0, 0); ot.SetColumnSpan(ck, 2);
        var np = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Margin = new Padding(0, 8, 0, 0) };
        var nt = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, ColumnCount = 2, RowCount = 1 };
        nt.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); nt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _lblName = new Label { Text = "备份名称：", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Theme.TextMain, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 4, 0, 0) };
        nt.Controls.Add(_lblName, 0, 0);
        var inputFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        _txtName = new TextBox { Width = 260, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F), BackColor = Theme.CardBg, ForeColor = Theme.TextMain, Margin = new Padding(0, 2, 0, 2) };
        _lblNameHint = new Label { Text = "留空则自动命名", Font = new Font("Segoe UI", 8.25F), ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
        inputFlow.Controls.Add(_txtName); inputFlow.Controls.Add(_lblNameHint); nt.Controls.Add(inputFlow, 1, 0);
        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border, Margin = new Padding(0, 0, 0, 8) };
        np.Controls.Add(sep); np.Controls.Add(nt);
        ot.Controls.Add(np, 0, 1); ot.SetColumnSpan(np, 2);
        _grpOpt.Controls.Add(ot); pageTable.Controls.Add(_grpOpt, 0, 1);

        _backupBottomPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Height = 110 };
        var bt = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.Bg, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 8, 0, 0) };
        bt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); bt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bt.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); bt.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); bt.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        var bf = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.Bg, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 8, 0) };
        _btnBackup = new ModernButton { Size = new Size(170, 42), Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), Style = ModernButtonStyle.Primary, Text = "开始备份", Margin = new Padding(0, 0, 8, 0) }; _btnBackup.Click += BtnBackup_Click;
        _btnCancel = new ModernButton { Size = new Size(100, 32), Enabled = false, Visible = false, Style = ModernButtonStyle.Secondary, Text = "取消", Margin = new Padding(0, 5, 0, 0) }; _btnCancel.Click += (_, _) => _cts?.Cancel();
        bf.Controls.Add(_btnBackup); bf.Controls.Add(_btnCancel);
        _lblStatus = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Theme.Success, Text = "", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        _pb = new ModernProgress { Height = 18, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
        bt.Controls.Add(bf, 0, 0); bt.SetRowSpan(bf, 2); bt.Controls.Add(_lblStatus, 1, 1); bt.Controls.Add(_pb, 0, 2); bt.SetColumnSpan(_pb, 2);
        _backupBottomPanel.Controls.Add(bt); pageTable.Controls.Add(_backupBottomPanel, 0, 2);
        tab.Controls.Add(pageTable); _tabControl.Controls.Add(tab);
    }

    private void BuildRestoreTab()
    {
        var tab = new TabPage { BackColor = Theme.Bg };
        var ta = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(12, 8, 12, 8) };

        _grpRestore = new Card { Dock = DockStyle.Fill, Text = "选择备份" };

        // 列表区域（自适应填充）
        var listPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(12, 36, 12, 8) };
        _lbZips = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F), BackColor = Theme.CardBg, ForeColor = Theme.TextMain, IntegralHeight = false };
        _lbZips.SelectedIndexChanged += LbZips_SelectedIndexChanged;
        _lblNoBackups = new Label { Text = "暂无备份。", Font = new Font("Segoe UI", 8.25F), ForeColor = Theme.TextMuted, Visible = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = Theme.CardBg };
        listPanel.Controls.Add(_lbZips); listPanel.Controls.Add(_lblNoBackups);

        // 按钮区域（固定高度）
        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Theme.CardBg, Padding = new Padding(12, 8, 12, 8) };
        var btnFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _btnRefresh = new ModernButton { Size = new Size(100, 36), Style = ModernButtonStyle.Secondary, Text = "刷新", Margin = new Padding(0, 0, 8, 0) }; _btnRefresh.Click += (_, _) => RefreshZipList();
        _btnRestore = new ModernButton { Size = new Size(170, 42), Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), Style = ModernButtonStyle.Warning, Enabled = false, Text = "开始还原" }; _btnRestore.Click += BtnRestore_Click;
        btnFlow.Controls.Add(_btnRefresh); btnFlow.Controls.Add(_btnRestore); btnPanel.Controls.Add(btnFlow);

        // 分隔线
        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };

        // 详情区域（固定高度）
        var detailPanel = new Panel { Dock = DockStyle.Bottom, Height = 110, BackColor = Theme.CardBg, Padding = new Padding(12, 8, 12, 12) };
        var detailTable = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, ColumnCount = 1, RowCount = 4, Height = 90 };
        detailTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); detailTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        detailTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); detailTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        _lblRPathLabel = new Label { Text = "备份文件：", Font = new Font("Segoe UI", 8.25F), ForeColor = Theme.TextMain, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        _txtRPath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F), BackColor = Theme.CardBg, ForeColor = Theme.TextMain, Margin = new Padding(0, 2, 0, 4) };
        _lblRInfo = new Label { Text = "", Font = new Font("Segoe UI", 8.25F), ForeColor = Theme.TextMain, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _pbRestore = new ModernProgress { Dock = DockStyle.Fill, Height = 16, Margin = new Padding(0, 4, 0, 0) };
        detailTable.Controls.Add(_lblRPathLabel, 0, 0); detailTable.Controls.Add(_txtRPath, 0, 1);
        detailTable.Controls.Add(_lblRInfo, 0, 2); detailTable.Controls.Add(_pbRestore, 0, 3);
        detailPanel.Controls.Add(detailTable);

        _grpRestore.Controls.Add(listPanel);
        _grpRestore.Controls.Add(btnPanel);
        _grpRestore.Controls.Add(sep);
        _grpRestore.Controls.Add(detailPanel);

        ta.Controls.Add(_grpRestore); tab.Controls.Add(ta);

        var bp = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Theme.Bg };
        _lblRestoreStatus = new Label { AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = Theme.Success, Text = "", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 8, 0, 0) };
        bp.Controls.Add(_lblRestoreStatus); tab.Controls.Add(bp);
        _tabControl.Controls.Add(tab);
    }

    private void RefreshLang()
    {
        bool e = _langEn; Text = "RimWorld Backup Tool"; _title.Text = e ? "RimWorld Backup Tool" : "RimWorld 备份还原工具"; _subtitle.Text = e ? "Backup and restore in one click." : "一键备份和还原";
        _tabControl.TabPages[0].Text = e ? "Backup" : "备份"; _tabControl.TabPages[1].Text = e ? "Restore" : "还原";
        _grpPath.Text = e ? "Steam Path" : "Steam 路径"; _lblPathLabel.Text = e ? "Steam Library:" : "Steam 库路径："; _btnBrowse.Text = e ? "Browse" : "浏览";
        _grpOpt.Text = e ? "Backup Options" : "备份内容";
        _chkConfig.Text = e ? "Mod Config" : "Mod 配置"; _chkSaves.Text = e ? "Game Saves" : "游戏存档"; _chkWorkshop.Text = e ? "Workshop Mods" : "创意工坊 Mod"; _chkLocal.Text = e ? "Local Mods" : "本地 Mod";
        _chkGameInfo.Text = e ? "Game Info" : "游戏版本信息"; _chkGameFiles.Text = e ? "Game Files" : "游戏安装文件"; _chkPlayerLog.Text = e ? "Player.log" : "游戏日志";
        _lblName.Text = e ? "Backup Name:" : "备份名称："; _lblNameHint.Text = e ? "leave empty for auto-name" : "留空则自动命名";
        _btnBackup.Text = e ? "Start Backup" : "开始备份"; _btnCancel.Text = e ? "Cancel" : "取消";
        _grpRestore.Text = e ? "Select Backup" : "选择备份"; _lblNoBackups.Text = e ? "No backups found." : "暂无备份。"; _btnRefresh.Text = e ? "Refresh" : "刷新"; _btnRestore.Text = e ? "Start Restore" : "开始还原";
        _lblRPathLabel.Text = e ? "Backup ZIP:" : "备份文件："; _btnExit.Text = e ? "Exit" : "退出";
        _lblPathStatus.Text = _txtPath.Text.Length > 0 && BackupManager.IsValidSteamPath(_txtPath.Text) ? (e ? "Path OK" : "路径有效") : (e ? "Select Steam path" : "请选择 Steam 路径");
    }

    private void BtnBrowse_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog(); dlg.Description = _langEn ? "Select Steam Library" : "选择 Steam 库目录"; dlg.ShowNewFolderButton = false;
        if (Directory.Exists(_txtPath.Text)) dlg.SelectedPath = _txtPath.Text;
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            if (BackupManager.IsValidSteamPath(dlg.SelectedPath)) { _txtPath.Text = dlg.SelectedPath; _lblPathStatus.Text = _langEn ? "Path OK" : "路径有效"; }
            else { MessageBox.Show(_langEn ? "Select folder containing steamapps\\common\\RimWorld." : "未找到 RimWorld，请选择包含 steamapps\\common\\RimWorld 的目录。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }

    private async void BtnBackup_Click(object sender, EventArgs e)
    {
        var steam = _txtPath.Text.Trim();
        if (!BackupManager.IsValidSteamPath(steam)) { MessageBox.Show(_langEn ? "Select Steam path first." : "请选择 Steam 路径。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        var opts = new BackupOptions { Config = _chkConfig.Checked, Saves = _chkSaves.Checked, WorkshopMods = _chkWorkshop.Checked, LocalMods = _chkLocal.Checked, GameInfo = _chkGameInfo.Checked, GameFiles = _chkGameFiles.Checked, PlayerLog = _chkPlayerLog.Checked };
        await RunOperation(ct => { var p = new Progress<ProgressReport>(r => { _lblStatus.Text = r.Message; _pb.Value = r.Percentage; }); return _bm.BackupAsync(steam, opts, _txtName.Text.Trim(), p, ct); }, _btnBackup, _pb, _lblStatus, false);
    }

    private void LbZips_SelectedIndexChanged(object sender, EventArgs e)
    {
        _btnRestore.Enabled = _lbZips.SelectedIndex >= 0;
        if (_lbZips.SelectedItem is string d) { var m = _cachedZips.FirstOrDefault(x => x.DisplayName == d); if (m != null) { _txtRPath.Text = m.Path; _lblRInfo.Text = _langEn ? $"Size: {Helpers.FormatSize(m.SizeBytes)}" : $"文件大小: {Helpers.FormatSize(m.SizeBytes)}"; } }
    }

    private async void BtnRestore_Click(object sender, EventArgs e)
    {
        try
        {
            if (_lbZips.SelectedItem is not string d) { MessageBox.Show(_langEn ? "Select a backup." : "请选择备份。", "OK", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var m = _cachedZips.FirstOrDefault(x => x.DisplayName == d);
            if (m == null) { MessageBox.Show(_langEn ? "No backups found." : "备份列表为空。", "OK", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var steam = _txtPath.Text.Trim();
            if (!BackupManager.IsValidSteamPath(steam)) { MessageBox.Show(_langEn ? "Set Steam path first." : "请设置 Steam 路径。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (MessageBox.Show(_langEn ? $"Restore: {m.Name}\nContinue?" : $"还原: {m.Name}\n确定继续?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await RunOperation(ct => { var p = new Progress<ProgressReport>(r => { _lblRestoreStatus.Text = r.Message; _pbRestore.Value = r.Percentage; }); return _bm.RestoreAsync(m.Path, steam, p, ct); }, _btnRestore, _pbRestore, _lblRestoreStatus, true);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RefreshZipList() { _lbZips.Items.Clear(); _cachedZips = _bm.GetBackupZips(); if (_cachedZips.Count == 0) { _lblNoBackups.Visible = true; _btnRestore.Enabled = false; _txtRPath.Text = ""; _lblRInfo.Text = ""; } else { _lblNoBackups.Visible = false; foreach (var e in _cachedZips) _lbZips.Items.Add(e.DisplayName); _lbZips.SelectedIndex = 0; } }

    private async Task RunOperation(Func<CancellationToken, Task> op, ModernButton btn, ModernProgress pb, Label st, bool isRestore)
    {
        _cts = new CancellationTokenSource(); btn.Enabled = false;
        _btnCancel.BringToFront(); _btnCancel.Visible = true; _btnCancel.Enabled = true; _btnCancel.Invalidate(); _btnCancel.Update();
        pb.Value = 0; st.Text = isRestore ? (_langEn ? "Restoring..." : "还原中...") : ""; st.ForeColor = Theme.Success;
        try { await op(_cts.Token); if (!isRestore) RefreshZipList(); st.Text = isRestore ? (_langEn ? "Done! Restart game to apply." : "还原完成！重启游戏生效。") : (_langEn ? "Backup complete!" : "备份完成！"); st.ForeColor = Theme.Success; MessageBox.Show(isRestore ? (_langEn ? "Restore complete!" : "还原完成！") : (_langEn ? "Backup complete!" : "备份完成！"), "OK", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (OperationCanceledException) { st.Text = _langEn ? "Cancelled." : "已取消。"; st.ForeColor = Theme.Warning; }
        catch (Exception ex) { st.Text = _langEn ? "Error" : "错误"; st.ForeColor = Theme.Danger; MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { btn.Enabled = true; _btnCancel.Visible = false; _btnCancel.Enabled = false; _cts?.Dispose(); _cts = null; }
    }

    private void LoadConfig()
    {
        var auto = BackupManager.FindSteamPath(); var cfg = AppConfig.Load(_rootDir);
        if (!string.IsNullOrEmpty(cfg.SteamPath) && Directory.Exists(cfg.SteamPath)) _txtPath.Text = cfg.SteamPath; else if (auto != null) _txtPath.Text = auto;
        if (cfg.Language == "en") { _chkLang.SelectedIndex = 1; _langEn = true; } else { _chkLang.SelectedIndex = 0; _langEn = false; }
        var o = cfg.Options; _chkConfig.Checked = o.Config; _chkSaves.Checked = o.Saves; _chkWorkshop.Checked = o.WorkshopMods; _chkLocal.Checked = o.LocalMods; _chkGameInfo.Checked = o.GameInfo; _chkGameFiles.Checked = o.GameFiles; _chkPlayerLog.Checked = o.PlayerLog;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        try
        {
            var cfg = new AppConfig { SteamPath = _txtPath.Text, Language = _langEn ? "en" : "zh", Options = new BackupOptions { Config = _chkConfig.Checked, Saves = _chkSaves.Checked, WorkshopMods = _chkWorkshop.Checked, LocalMods = _chkLocal.Checked, GameInfo = _chkGameInfo.Checked, GameFiles = _chkGameFiles.Checked, PlayerLog = _chkPlayerLog.Checked } };
            cfg.Save(_rootDir);
        }
        catch { }
        base.OnFormClosing(e);
    }

    private static ModernCheckBox MakeCheck(string text) => new()
    {
        Text = text,
        Checked = true,
        Font = new Font("Segoe UI", 9F),
        Dock = DockStyle.Fill,
        Margin = new Padding(4, 2, 4, 2),
        Height = 24
    };
}
