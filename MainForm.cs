using System.Drawing;
using System.Windows.Forms;

namespace GrimaceOptimizer;

public sealed class MainForm : Form
{
    readonly Color Bg = Color.FromArgb(13, 11, 20);
    readonly Color Panel = Color.FromArgb(25, 20, 37);
    readonly Color Purple = Color.FromArgb(154, 83, 255);
    readonly Color Text = Color.FromArgb(240, 238, 248);
    readonly Color Muted = Color.FromArgb(165, 158, 180);

    Label versionLabel = null!;
    Label hardwareLabel = null!;
    Label regionLabel = null!;
    Label statusLabel = null!;
    ProgressBar progress = null!;
    Button optimizeButton = null!;
    Button launchButton = null!;
    readonly Dictionary<string, CheckBox> checks = new();
    ComboBox fpsBox = null!;

    public MainForm()
    {
        Text = "Grimace Optimizer";
        ClientSize = new Size(1100, 900);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        ForeColor = Text;
        Font = new Font("Segoe UI", 10);

        BuildUi();
        Shown += async (_, _) => await LoadHardwareAsync();
        Shown += async (_, _) => await AutoUpdateAsync();
    }

    void BuildUi()
    {
        var title = new Label { Text = "GRIMACE OPTIMIZER", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Purple, Location = new Point(35, 25), AutoSize = true };
        Controls.Add(title);
        var sub = new Label { Text = "Fortnite performance & gaming optimization", ForeColor = Muted, Location = new Point(38, 68), AutoSize = true };
        Controls.Add(sub);

        var hw = Card(35, 100, 500, 145);
        hardwareLabel = new Label { Text = "Detecting hardware...", ForeColor = Text, Location = new Point(18, 16), Size = new Size(460, 112) };
        hw.Controls.Add(hardwareLabel); Controls.Add(hw);

        var reg = Card(555, 100, 510, 145);
        var rt = new Label { Text = "LOW-PING REGION", ForeColor = Purple, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(18, 16), AutoSize = true };
        reg.Controls.Add(rt);
        regionLabel = new Label { Text = "Checking nearby regions...", ForeColor = Text, Location = new Point(18, 52), Size = new Size(470, 70) };
        reg.Controls.Add(regionLabel); Controls.Add(reg);

        var tabs = new TabControl { Location = new Point(35, 265), Size = new Size(1030, 455), Appearance = TabAppearance.Normal };
        tabs.TabPages.Add(MakeTab("SYSTEM", new[]
        {
            ("High Performance power plan","HighPerformancePower",true),
            ("Disable Windows power throttling","DisablePowerThrottling",false),
            ("Hardware-accelerated GPU scheduling","Hags",true),
            ("Gaming priority tuning","GamingPriority",true),
            ("Windows visual-effects performance mode","VisualPerformance",false),
            ("Disable window animations","DisableAnimations",false)
        }));
        tabs.TabPages.Add(MakeTab("WINDOWS GAMING", new[]
        {
            ("Windows Game Mode","GameMode",true),
            ("Disable background Game DVR capture","DisableGameDvr",true),
            ("Disable Xbox Game Bar startup overlay","DisableXboxOverlay",true)
        }));
        tabs.TabPages.Add(MakeTab("FORTNITE", new[]
        {
            ("Competitive FPS profile","CompetitiveProfile",true),
            ("Request fullscreen mode","Fullscreen",false),
            ("Request NVIDIA Reflex preference","NvidiaReflex",true),
            ("Matchmaking region: Auto","AutoRegion",true)
        }));
        var network = new TabPage("NETWORK") { BackColor = Panel };
        AddCheck(network, "Flush DNS cache", "FlushDns", false, 25, 25);
        AddCheck(network, "Keep Fortnite matchmaking on Auto", "AutoRegion", true, 25, 75);
        tabs.TabPages.Add(network);
        tabs.TabPages.Add(MakeTab("CLEANUP", new[]
        {
            ("Clean old temp files","CleanTemp",true),
            ("Clear DirectX / NVIDIA shader cache","ClearShaderCache",false)
        }));
        var launch = new TabPage("LAUNCH") { BackColor = Panel };
        AddCheck(launch, "Set Fortnite process priority to High", "HighPriority", true, 25, 25);
        var fpsLabel = new Label { Text = "FPS cap", ForeColor = Text, Location = new Point(25, 85), AutoSize = true };
        launch.Controls.Add(fpsLabel);
        fpsBox = new ComboBox { Location = new Point(25, 115), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        fpsBox.Items.AddRange(new object[] { "Unlimited", "60", "120", "144", "165", "180", "240" });
        fpsBox.SelectedItem = "144";
        launch.Controls.Add(fpsBox);
        tabs.TabPages.Add(launch);
        Controls.Add(tabs);

        optimizeButton = new Button { Text = "OPTIMIZE SELECTED", Location = new Point(35, 750), Size = new Size(260, 58), BackColor = Purple, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        optimizeButton.FlatAppearance.BorderSize = 0; optimizeButton.Click += async (_, _) => await OptimizeAsync(); Controls.Add(optimizeButton);
        launchButton = new Button { Text = "LAUNCH FORTNITE", Location = new Point(310, 750), Size = new Size(230, 58), BackColor = Color.FromArgb(42, 35, 58), ForeColor = Text, FlatStyle = FlatStyle.Flat };
        launchButton.FlatAppearance.BorderSize = 0; launchButton.Click += (_, _) => Optimizer.LaunchFortnite(); Controls.Add(launchButton);

        statusLabel = new Label { Text = "Ready", ForeColor = Muted, Location = new Point(560, 758), Size = new Size(300, 25) }; Controls.Add(statusLabel);
        progress = new ProgressBar { Location = new Point(560, 790), Size = new Size(300, 8), Style = ProgressBarStyle.Marquee, Visible = false }; Controls.Add(progress);
        versionLabel = new Label { Text = $"Version {UpdateService.VersionText}", ForeColor = Muted, Location = new Point(880, 760), AutoSize = true }; Controls.Add(versionLabel);
        var update = new Button { Text = "Check for Updates", Location = new Point(880, 792), Size = new Size(165, 32), BackColor = Panel, ForeColor = Text, FlatStyle = FlatStyle.Flat };
        update.FlatAppearance.BorderColor = Purple; update.Click += async (_, _) => await ManualUpdateAsync(); Controls.Add(update);
    }

    TabPage MakeTab(string name, (string Text, string Key, bool Checked)[] items)
    {
        var page = new TabPage(name) { BackColor = Panel };
        int y = 25;
        foreach (var item in items) { AddCheck(page, item.Text, item.Key, item.Checked, 25, y); y += 50; }
        if (name == "FORTNITE")
        {
            var l = new Label { Text = "FPS cap", ForeColor = Text, Location = new Point(520, 25), AutoSize = true };
            page.Controls.Add(l);
            fpsBox = new ComboBox { Location = new Point(520, 55), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            fpsBox.Items.AddRange(new object[] { "Unlimited", "60", "120", "144", "165", "180", "240" }); fpsBox.SelectedItem = "144";
            page.Controls.Add(fpsBox);
        }
        return page;
    }

    void AddCheck(TabPage page, string text, string key, bool value, int x, int y)
    {
        if (checks.ContainsKey(key)) return;
        var c = new CheckBox { Text = text, Checked = value, ForeColor = Text, Location = new Point(x, y), AutoSize = true };
        checks[key] = c; page.Controls.Add(c);
    }

    Panel Card(int x, int y, int w, int h) => new() { Location = new Point(x,y), Size = new Size(w,h), BackColor = Panel, BorderStyle = BorderStyle.FixedSingle };

    async Task LoadHardwareAsync()
    {
        var h = await HardwareInfo.DetectAsync();
        hardwareLabel.Text = $"CPU: {h.Cpu}\nGPU: {h.Gpu}\nRAM: {h.Ram}\nStorage: {h.Storage}";
        var best = await RegionProbe.FindBestAsync();
        regionLabel.Text = $"Estimated best nearby region: {best}\nFortnite remains on Auto so Epic can select the actual server.";
    }

    async Task AutoUpdateAsync()
    {
        var info = await UpdateService.CheckAsync();
        if (info != null)
        {
            var yes = MessageBox.Show($"Version {info.Version} is available. Update now?", "Grimace Optimizer", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (yes == DialogResult.Yes) await StartUpdate(info);
        }
    }

    async Task ManualUpdateAsync()
    {
        statusLabel.Text = "Checking for updates...";
        var info = await UpdateService.CheckAsync();
        if (info == null) { statusLabel.Text = "You are up to date."; return; }
        var yes = MessageBox.Show($"Version {info.Version} is available. Update now?", "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (yes == DialogResult.Yes) await StartUpdate(info);
        else statusLabel.Text = "Update canceled.";
    }

    async Task StartUpdate(UpdateService.UpdateInfo info)
    {
        statusLabel.Text = $"Downloading {info.Version}...";
        progress.Visible = true; optimizeButton.Enabled = false; launchButton.Enabled = false;
        if (await UpdateService.StartUpdateAsync(info))
        {
            statusLabel.Text = "Update downloaded. Restarting...";
            await Task.Delay(300);
            Application.Exit();
        }
        else
        {
            progress.Visible = false; optimizeButton.Enabled = true; launchButton.Enabled = true;
            statusLabel.Text = "Update failed.";
            MessageBox.Show("The update could not be installed. Download the newest EXE from GitHub Releases.", "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    async Task OptimizeAsync()
    {
        optimizeButton.Enabled = false; launchButton.Enabled = false; progress.Visible = true; statusLabel.Text = "Optimizing...";
        try
        {
            int fps = fpsBox?.SelectedItem?.ToString() == "Unlimited" ? 0 : int.TryParse(fpsBox?.SelectedItem?.ToString(), out var n) ? n : 144;
            var o = new OptimizationOptions(
                Get("HighPerformancePower"), Get("DisablePowerThrottling"), Get("Hags"), Get("GamingPriority"),
                Get("VisualPerformance"), Get("DisableAnimations"), Get("GameMode"), Get("DisableGameDvr"), Get("DisableXboxOverlay"),
                Get("CompetitiveProfile"), Get("Fullscreen"), Get("NvidiaReflex"), Get("AutoRegion"), fps,
                Get("FlushDns"), Get("CleanTemp"), Get("ClearShaderCache"), Get("HighPriority"));
            var p = new Progress<string>(s => statusLabel.Text = s);
            await Optimizer.ApplyAsync(o, p);
            statusLabel.Text = "Optimization complete.";
            MessageBox.Show("Grimace optimization completed. Some Windows changes may require a restart.", "Grimace Optimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { statusLabel.Text = "Optimization error."; MessageBox.Show(ex.Message, "Optimizer error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { progress.Visible = false; optimizeButton.Enabled = true; launchButton.Enabled = true; }
    }

    bool Get(string key) => checks.TryGetValue(key, out var c) && c.Checked;
}
