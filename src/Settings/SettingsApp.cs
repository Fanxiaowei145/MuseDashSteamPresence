using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MuseDashSteamPresence;

[assembly: AssemblyTitle("Muse Dash 状态设置")]
[assembly: AssemblyProduct("Muse Dash Steam Presence")]
[assembly: AssemblyCompany("樊小胃")]
[assembly: AssemblyCopyright("Copyright (c) 2026 樊小胃 and contributors")]
[assembly: AssemblyVersion("0.5.1.0")]

internal static class SettingsApp
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [STAThread] static void Main(string[] args)
    {
        SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            string data = GameLocation.SettingsRoot();
            if (args.Length == 2 && args[0] == "--data") data = Path.GetFullPath(args[1]);
            Application.Run(new SettingsWindow(data));
        }
        catch (Exception e) { MessageBox.Show(e.Message, "Muse Dash 状态设置", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

internal sealed class SettingsWindow : Form
{
    readonly string root, configPath;
    readonly Color ink = Color.FromArgb(38, 32, 62), muted = Color.FromArgb(114, 107, 133), accent = Color.FromArgb(110, 72, 214);
    CheckBox idle, song, author;
    TextBox idleText, separator;
    ComboBox separatorChoice;
    Label idlePreview, songPreview, message, connection;
    Button save;
    Button enablePlugin, disablePlugin;
    bool loading, dirty;
    string loadedFingerprint;
    string compatibilityMessage;
    long nextCompatibilityCheck;
    readonly Timer timer = new Timer { Interval = 2000 };

    public SettingsWindow(string data)
    {
        root = data; configPath = Path.Combine(root, PresenceSettings.FileName);
        Text = "Muse Dash 状态设置"; Name = "MuseDashPresenceSettings";
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(760, Math.Min(930, Screen.PrimaryScreen.WorkingArea.Height - 60)); MinimumSize = new Size(776, 650);
        StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(247, 245, 252);
        Font = new Font("Microsoft YaHei UI", 10F); ForeColor = ink;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Controls.Add(content);
        var page = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(28, 22, 28, 16) };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); content.Controls.Add(page);
        var heading = new Label { Text = "Muse Dash  /  状态设置", AutoSize = true, Font = new Font(Font.FontFamily, 21, FontStyle.Bold), ForeColor = ink, Margin = new Padding(0, 0, 0, 7) };
        page.Controls.Add(heading);
        page.Controls.Add(Note("把正在玩的曲子，显示给 Steam 好友。", 12));
        connection = Note("正在检查插件…", 10); connection.MaximumSize = new Size(680, 0); connection.Margin = new Padding(0, 4, 0, 10); page.Controls.Add(connection);
        var pluginActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        enablePlugin = Button("启用插件", 120, false); enablePlugin.Click += delegate { TogglePlugin(true); }; pluginActions.Controls.Add(enablePlugin);
        disablePlugin = Button("停用插件", 120, false); disablePlugin.Click += delegate { TogglePlugin(false); }; pluginActions.Controls.Add(disablePlugin);
        var loaderLink = Button("获取 MelonLoader", 177, false);
        loaderLink.Click += delegate { try { Process.Start(new ProcessStartInfo("https://github.com/LavaGang/MelonLoader.Installer/releases/latest") { UseShellExecute = true }); } catch (Exception e) { ShowError(e.Message); } };
        pluginActions.Controls.Add(loaderLink); page.Controls.Add(pluginActions);

        var idleCard = Card("01   不打歌时", 168); page.Controls.Add(idleCard);
        idle = Check("显示自定义状态", 18, 47); idleCard.Controls.Add(idle);
        idleText = Input(18, 83, 640); idleText.MaxLength = 512; idleText.AccessibleName = "不打歌时的自定义状态"; idleCard.Controls.Add(idleText);
        var idleHint = Note("关闭或留空时只显示 Muse Dash。菜单、选歌、结算和暂停都属于此状态。", 9);
        idleHint.Location = new Point(18, 127); idleHint.MaximumSize = new Size(642, 0); idleCard.Controls.Add(idleHint);

        var playingCard = Card("02   打歌时", 178); page.Controls.Add(playingCard);
        song = Check("显示歌曲名", 18, 47); author = Check("显示歌曲作者", 214, 47);
        playingCard.Controls.Add(song); playingCard.Controls.Add(author);
        var sepLabel = Note("曲名与作者之间", 10); sepLabel.Location = new Point(18, 91); playingCard.Controls.Add(sepLabel);
        separatorChoice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(159, 86), Size = new Size(190, 32), AccessibleName = "曲名作者分隔方式" };
        separatorChoice.Items.AddRange(new object[] { "无", "空格", "下划线 _", "自定义" }); playingCard.Controls.Add(separatorChoice);
        separator = Input(365, 86, 290); separator.MaxLength = 32; separator.AccessibleName = "自定义分隔符"; playingCard.Controls.Add(separator);
        var playHint = Note("两项都关闭时只显示 Muse Dash；只有一项时不加分隔符。", 9);
        playHint.Location = new Point(18, 134); playingCard.Controls.Add(playHint);

        var preview = Card("显示预览", 133); preview.BackColor = Color.FromArgb(238, 232, 252); page.Controls.Add(preview);
        var idleLabel = Note("不打歌", 9); idleLabel.Location = new Point(18, 49); preview.Controls.Add(idleLabel);
        idlePreview = PreviewLabel(100, 46); preview.Controls.Add(idlePreview);
        var songLabel = Note("打歌中", 9); songLabel.Location = new Point(18, 89); preview.Controls.Add(songLabel);
        songPreview = PreviewLabel(100, 86); preview.Controls.Add(songPreview);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 7, 0, 0) };
        save = Button("保存并应用", 154, true); save.AccessibleName = "保存并应用"; save.Click += delegate { SaveSettings(); }; actions.Controls.Add(save);
        var reset = Button("恢复默认", 120, false); reset.Click += delegate { Fill(new PresenceSettings()); SetDirty(); }; actions.Controls.Add(reset);
        var folder = Button("打开配置文件夹", 162, false); folder.Click += delegate { try { Directory.CreateDirectory(root); Process.Start("explorer.exe", "\"" + root + "\""); } catch (Exception e) { ShowError(e.Message); } }; actions.Controls.Add(folder);
        page.Controls.Add(actions);
        message = Note("", 9); message.Margin = new Padding(0, 10, 0, 4); message.MaximumSize = new Size(680, 0); page.Controls.Add(message);
        var foot = Note("可先保存设置再开游戏；游戏中修改约 1 秒生效。启用／停用需先退出游戏。\n关闭此窗口后设置仍有效；过长内容可能截短。本软件不额外登录 Steam。", 9);
        foot.MaximumSize = new Size(680, 0); page.Controls.Add(foot);

        idle.CheckedChanged += Changed; song.CheckedChanged += Changed; author.CheckedChanged += Changed;
        idleText.TextChanged += Changed; separator.TextChanged += Changed; separatorChoice.SelectedIndexChanged += Changed;
        try { Fill(PresenceSettings.Load(configPath)); message.Text = File.Exists(configPath) ? "已载入保存的设置。" : "正在使用默认设置。"; }
        catch (Exception e) { Fill(new PresenceSettings()); ShowError("配置未能读取，保存前不会覆盖原文件。" + e.Message); }
        FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (dirty)
            {
                var result = MessageBox.Show(this, "设置尚未保存。是否保存后关闭？", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel) e.Cancel = true;
                else if (result == DialogResult.Yes && !SaveSettings()) e.Cancel = true;
            }
        };
        timer.Tick += delegate { UpdateConnection(); }; timer.Start(); UpdateConnection();
    }
    Label Note(string text, float size) { return new Label { Text = text, UseMnemonic = false, AutoSize = true, Font = new Font(Font.FontFamily, size), ForeColor = muted, Margin = new Padding(0, 0, 0, 4) }; }
    Panel Card(string title, int height)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Color.White, Margin = new Padding(0, 0, 0, 13) };
        var label = Note(title, 11); label.Font = new Font(Font.FontFamily, 11, FontStyle.Bold); label.ForeColor = ink; label.Location = new Point(18, 14); panel.Controls.Add(label); return panel;
    }
    CheckBox Check(string text, int x, int y) { return new CheckBox { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = ink, AccessibleName = text }; }
    TextBox Input(int x, int y, int width) { return new TextBox { Location = new Point(x, y), Size = new Size(width, 32), BorderStyle = BorderStyle.FixedSingle }; }
    Label PreviewLabel(int x, int y) { return new Label { UseMnemonic = false, Location = new Point(x, y), Size = new Size(555, 31), AutoEllipsis = true, ForeColor = Color.FromArgb(61, 92, 41), Font = new Font(Font.FontFamily, 11, FontStyle.Bold) }; }
    Button Button(string text, int width, bool primary)
    {
        var b = new Button { Text = text, Size = new Size(width, 43), FlatStyle = FlatStyle.Flat, BackColor = primary ? accent : Color.White, ForeColor = primary ? Color.White : ink, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 10, 0) };
        b.FlatAppearance.BorderSize = primary ? 0 : 1; b.FlatAppearance.BorderColor = Color.FromArgb(221, 215, 235); return b;
    }
    PresenceSettings ReadControls()
    {
        string sep = separatorChoice.SelectedIndex == 0 ? "" : separatorChoice.SelectedIndex == 1 ? " " : separatorChoice.SelectedIndex == 2 ? "_" : separator.Text;
        return new PresenceSettings { ShowIdle = idle.Checked, IdleText = idleText.Text, ShowSong = song.Checked, ShowAuthor = author.Checked, Separator = sep };
    }
    void Fill(PresenceSettings s)
    {
        loading = true;
        idle.Checked = s.ShowIdle; idleText.Text = s.IdleText; song.Checked = s.ShowSong; author.Checked = s.ShowAuthor;
        separatorChoice.SelectedIndex = s.Separator == "" ? 0 : s.Separator == " " ? 1 : s.Separator == "_" ? 2 : 3;
        separator.Text = s.Separator;
        loadedFingerprint = ReadControls().Fingerprint; dirty = false; loading = false; UpdatePreview();
    }
    void Changed(object sender, EventArgs e) { if (!loading) { SetDirty(); UpdatePreview(); } }
    void SetDirty() { dirty = ReadControls().Fingerprint != loadedFingerprint; message.Text = dirty ? "有未保存的更改。预览不会改变 Steam 状态。" : "设置未更改。"; message.ForeColor = muted; }
    void UpdatePreview()
    {
        idleText.Enabled = idle.Checked;
        separatorChoice.Enabled = song.Checked && author.Checked;
        separator.Enabled = separatorChoice.Enabled && separatorChoice.SelectedIndex == 3;
        var s = ReadControls(); idlePreview.Text = SongDisplay.Idle(s); songPreview.Text = SongDisplay.Create("ものふぉびあ", "とりぴよ feat. 凛々咲", s);
    }
    bool SaveSettings()
    {
        try
        {
            var s = ReadControls(); s.Save(configPath); loadedFingerprint = s.Fingerprint; dirty = false;
            message.ForeColor = Color.FromArgb(53, 115, 69); message.Text = "已保存。游戏运行时会自动应用；未启动时会在下次打开游戏生效。"; return true;
        }
        catch (Exception e) { ShowError("保存失败：" + e.Message); return false; }
    }
    void ShowError(string text) { message.ForeColor = Color.Firebrick; message.Text = text; }
    void TogglePlugin(bool enabled)
    {
        try
        {
            PluginInstallation.SetEnabled(root, enabled);
            message.ForeColor = Color.FromArgb(53, 115, 69);
            message.Text = enabled ? "两个插件组件已启用；下次启动游戏生效。" : "两个插件组件已停用；保存的显示设置会保留。";
        }
        catch (Exception e) { ShowError(e.Message); }
        UpdateConnection();
    }
    void UpdateConnection()
    {
        try
        {
            if (DateTime.UtcNow.Ticks >= nextCompatibilityCheck)
            {
                string reason;
                compatibilityMessage = SteamBuild.IsSupported(out reason) ? "" : reason;
                nextCompatibilityCheck = DateTime.UtcNow.AddSeconds(30).Ticks;
            }
            var state = PluginInstallation.Inspect(root);
            bool running = state.GameDirectory != null && PluginInstallation.GameRunning(state.GameDirectory);
            enablePlugin.Enabled = state.CanEnable && !running;
            disablePlugin.Enabled = state.CanDisable && !running;
            connection.ForeColor = !state.Ready || compatibilityMessage.Length > 0 ? Color.FromArgb(155, 93, 22) : muted;
            connection.Text = "环境：" + state.Message + "\n" + (compatibilityMessage.Length > 0 ? "Steam：" + compatibilityMessage :
                state.Ready ? PluginInstallation.RuntimeMessage(root, state.GameDirectory) : "配置仍可保存；请先处理上方环境提示");
            if (File.Exists(Path.Combine(root,"deployment-warning.txt")))
            {
                connection.ForeColor = Color.FromArgb(155,93,22);
                connection.Text = "配套文件更新未完成：请关闭设置窗口，再重开游戏。\n配置仍可保存；详细原因见 deployment-warning.txt。";
            }
        }
        catch { connection.ForeColor = Color.Firebrick; connection.Text = "环境检查未完成；设置仍可保存，请检查文件权限。"; enablePlugin.Enabled = disablePlugin.Enabled = false; }
    }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
}
