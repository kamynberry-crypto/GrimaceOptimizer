using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace GrimaceOptimizer;

internal sealed class MainForm : Form
{
    private readonly Color Bg = Color.FromArgb(12, 10, 18);
    private readonly Color Panel = Color.FromArgb(25, 20, 36);
    private readonly Color Purple = Color.FromArgb(145, 78, 255);
    private readonly Color Muted = Color.FromArgb(173, 164, 194);
    private readonly FlowLayoutPanel logPanel = new();
    private readonly Label cpu = new(), gpu = new(), ram = new(), storage = new(), region = new();
    private readonly Button optimize = new(), launch = new(), retest = new(), updateButton = new();
    private readonly CheckBox powerPlan = new(), gameMode = new(), captureOff = new(), gpuScheduling = new(), gamingPriority = new(), fortniteProfile = new(), tempCleanup = new(), highPriorityLaunch = new();
    private readonly Label status = new(), version = new();

    public MainForm()
    {
        Text = "Grimace Optimizer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 650);
        Size = new Size(1050, 720);
        BackColor = Bg;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BuildUi();
        Shown += async (_, _) =>
        {
            await InitialScanAsync();
            await CheckForUpdatesAsync(true);
        };
    }

    private void BuildUi()
    {
        var title = new Label { Text = "GRIMACE", Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Purple, AutoSize = true, Location = new Point(35, 25) };
        var subtitle = new Label { Text = "OPTIMIZER  •  Fortnite performance profile", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Muted, AutoSize = true, Location = new Point(39, 73) };
        Controls.Add(title); Controls.Add(subtitle);

        var hwCard = Card("HARDWARE DETECTION", 35, 115, 470, 310);
        cpu.SetBounds(25, 58, 420, 45); gpu.SetBounds(25, 113, 420, 45); ram.SetBounds(25, 168, 420, 45); storage.SetBounds(25, 223, 420, 45);
        foreach (var l in new[] { cpu, gpu, ram, storage }) { l.ForeColor = Color.White; l.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold); hwCard.Controls.Add(l); }
        cpu.Text = "CPU  Detecting…"; gpu.Text = "GPU  Detecting…"; ram.Text = "RAM  Detecting…"; storage.Text = "STORAGE  Detecting…";

        var netCard = Card("LOW-PING REGION", 525, 115, 475, 180);
        region.Text = "Testing regional latency…"; region.ForeColor = Color.White; region.Font = new Font("Segoe UI", 14, FontStyle.Bold); region.SetBounds(25, 60, 420, 35); netCard.Controls.Add(region);
        var netInfo = new Label { Text = "Fortnite is set to Auto so Epic can choose the live server with the best ping. The estimate below uses regional network probes.", ForeColor = Muted, AutoSize = false, Size = new Size(415, 50), Location = new Point(25, 100) };
        netCard.Controls.Add(netInfo);
        retest.Text = "Retest"; StyleButton(retest, false); retest.SetBounds(360, 25, 85, 34); retest.Click += async (_, _) => await TestRegionAsync(); netCard.Controls.Add(retest);

        var actionCard = Card("ONE-CLICK PERFORMANCE", 525, 315, 475, 250);
        var desc = new Label { Text = "Choose exactly what Grimace should optimize. Your selections are applied only when you press Optimize.", ForeColor = Muted, AutoSize = false, Size = new Size(420, 32), Location = new Point(25, 50) };
        actionCard.Controls.Add(desc);

        ConfigureOption(powerPlan, "High Performance power", 25, 82, true);
        ConfigureOption(gameMode, "Windows Game Mode", 235, 82, true);
        ConfigureOption(captureOff, "Disable background capture", 25, 110, true);
        ConfigureOption(gpuScheduling, "Hardware GPU scheduling", 235, 110, true);
        ConfigureOption(gamingPriority, "Gaming priority tuning", 25, 138, true);
        ConfigureOption(fortniteProfile, "Fortnite FPS profile", 235, 138, true);
        ConfigureOption(tempCleanup, "Clean old temp files", 25, 166, true);
        ConfigureOption(highPriorityLaunch, "High-priority Fortnite launch", 235, 166, true);
        foreach (var option in new[] { powerPlan, gameMode, captureOff, gpuScheduling, gamingPriority, fortniteProfile, tempCleanup, highPriorityLaunch })
            actionCard.Controls.Add(option);

        optimize.Text = "⚡  OPTIMIZE SELECTED"; StyleButton(optimize, true); optimize.SetBounds(25, 201, 200, 38); optimize.Click += async (_, _) => await OptimizeAsync(); actionCard.Controls.Add(optimize);
        launch.Text = "▶  LAUNCH FORTNITE"; StyleButton(launch, false); launch.SetBounds(240, 201, 205, 38); launch.Click += (_, _) => LaunchFortnite(); actionCard.Controls.Add(launch);

        var logCard = Card("ACTIVITY", 35, 445, 470, 220);
        logPanel.FlowDirection = FlowDirection.TopDown; logPanel.WrapContents = false; logPanel.AutoScroll = true; logPanel.BackColor = Panel; logPanel.SetBounds(20, 48, 430, 145); logCard.Controls.Add(logPanel);

        status.Text = "Ready"; status.ForeColor = Muted; status.AutoSize = true; status.Location = new Point(550, 590); Controls.Add(status);

        version.Text = UpdateService.VersionText; version.ForeColor = Color.FromArgb(125, 115, 145); version.AutoSize = true; version.Location = new Point(35, 590); Controls.Add(version);
        updateButton.Text = "Check for Updates"; StyleButton(updateButton, false); updateButton.SetBounds(35, 615, 165, 34); updateButton.Click += async (_, _) => await CheckForUpdatesAsync(false); Controls.Add(updateButton);
        var foot = new Label { Text = "No backup/restore module • No Python • Native Windows x64 build", ForeColor = Color.FromArgb(115, 106, 134), AutoSize = true, Location = new Point(550, 625) }; Controls.Add(foot);
    }

    private void ConfigureOption(CheckBox box, string text, int x, int y, bool enabledByDefault)
    {
        box.Text = text;
        box.Checked = enabledByDefault;
        box.AutoSize = false;
        box.Size = new Size(205, 25);
        box.Location = new Point(x, y);
        box.ForeColor = Color.White;
        box.BackColor = Panel;
        box.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
    }

    private Panel Card(string heading, int x, int y, int w, int h)
    {
        var p = new RoundedPanel { BackColor = Panel, Radius = 18, Location = new Point(x, y), Size = new Size(w, h) };
        var l = new Label { Text = heading, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Purple, AutoSize = true, Location = new Point(25, 23) };
        p.Controls.Add(l); Controls.Add(p); return p;
    }

    private void StyleButton(Button b, bool primary)
    {
        b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = primary ? 0 : 1; b.FlatAppearance.BorderColor = Purple;
        b.BackColor = primary ? Purple : Color.FromArgb(35, 27, 49); b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI", 10, FontStyle.Bold); b.Cursor = Cursors.Hand;
    }


    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (!UpdateService.IsConfigured)
        {
            if (!silent)
                MessageBox.Show(
                    "GitHub updates are not configured yet. Open UpdateService.cs and replace YOUR_GITHUB_USERNAME with your GitHub username.",
                    "Grimace Optimizer Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        updateButton.Enabled = false;
        status.Text = "Checking for updates…";
        try
        {
            var update = await UpdateService.CheckAsync();
            if (update is null)
            {
                status.Text = "Up to date";
                if (!silent)
                    MessageBox.Show($"You're running the latest version ({UpdateService.VersionText}).", "Grimace Optimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var answer = MessageBox.Show(
                $"Grimace Optimizer {update.TagName} is available.\n\nYou have {UpdateService.VersionText}.\n\nDownload and install it now?",
                "Grimace Optimizer Update", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (answer != DialogResult.Yes)
            {
                status.Text = $"Update available: {update.TagName}";
                return;
            }

            var progress = new Progress<string>(message =>
            {
                status.Text = message;
                Log(message);
            });
            await UpdateService.DownloadAndStageAsync(update, progress);
            status.Text = "Update downloaded — restarting…";
            Log($"Installing {update.TagName}. Grimace will restart automatically.");
            await Task.Delay(600);
            Application.Exit();
        }
        catch (Exception ex)
        {
            status.Text = "Update check failed";
            Log("Update error: " + ex.Message);
            if (!silent)
                MessageBox.Show("Could not check for updates.\n\n" + ex.Message, "Grimace Optimizer Updates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            updateButton.Enabled = true;
        }
    }

    private async Task InitialScanAsync()
    {
        Log("Detecting hardware automatically…");
        var hwTask = HardwareInfo.DetectAsync();
        var regionTask = TestRegionAsync();
        var hw = await hwTask;
        cpu.Text = $"CPU   {hw.Cpu}"; gpu.Text = $"GPU   {hw.Gpu}"; ram.Text = $"RAM   {hw.Ram}"; storage.Text = $"STORAGE   {hw.Storage}";
        Log("Hardware detection complete.");
        await regionTask;
    }

    private async Task TestRegionAsync()
    {
        retest.Enabled = false; region.Text = "Testing regional latency…";
        var result = await RegionProbe.FindBestAsync();
        if (result is null) { region.Text = "Epic Auto • region test unavailable"; Log("Region test unavailable; Fortnite Auto remains enabled."); }
        else { region.Text = $"Epic Auto  •  estimate: {result.Name} ~{result.Milliseconds} ms"; Log($"Lowest measured regional probe: {result.Name} (~{result.Milliseconds} ms)."); }
        retest.Enabled = true;
    }

    private async Task OptimizeAsync()
    {
        optimize.Enabled = false; launch.Enabled = false; status.Text = "Optimizing…"; Log("Starting Grimace performance profile…");
        var progress = new Progress<string>(Log);
        try
        {
            var options = new OptimizationOptions(
                powerPlan.Checked, gameMode.Checked, captureOff.Checked, gpuScheduling.Checked,
                gamingPriority.Checked, fortniteProfile.Checked, tempCleanup.Checked);
            var changes = await Optimizer.ApplyAsync(options, progress);
            Optimizer.SetFortniteAutoRegion(progress);
            status.Text = $"Optimization complete • {changes.Count} actions applied";
            Log("Done. Restart Windows if GPU scheduling was changed.");
        }
        catch (Exception ex)
        {
            status.Text = "Optimization finished with an error"; Log("Error: " + ex.Message);
        }
        finally { optimize.Enabled = true; launch.Enabled = true; }
    }

    private void LaunchFortnite()
    {
        try
        {
            Optimizer.SetFortniteAutoRegion(new Progress<string>(Log));
            Optimizer.LaunchFortnite(highPriorityLaunch.Checked);
            status.Text = "Fortnite launch requested through Epic Games Launcher";
            Log("Launching Fortnite; process priority will switch to High after the game starts.");
        }
        catch (Exception ex) { Log("Launch failed: " + ex.Message); }
    }

    private void Log(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(message)); return; }
        var l = new Label { Text = "• " + message, ForeColor = Muted, AutoSize = false, Width = 395, Height = 34, Padding = new Padding(0, 4, 0, 0) };
        logPanel.Controls.Add(l); logPanel.ScrollControlIntoView(l);
    }

    private sealed class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 16;
        protected override void OnResize(EventArgs e) { base.OnResize(e); ApplyRegion(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); ApplyRegion(); }
        private void ApplyRegion()
        {
            using var path = new GraphicsPath(); int r = Radius * 2;
            path.AddArc(0, 0, r, r, 180, 90); path.AddArc(Width-r-1, 0, r, r, 270, 90); path.AddArc(Width-r-1, Height-r-1, r, r, 0, 90); path.AddArc(0, Height-r-1, r, r, 90, 90); path.CloseFigure(); Region = new Region(path);
        }
    }
}
