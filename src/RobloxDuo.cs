using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

internal sealed class DuoButton : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Enabled) { base.OnPaint(e); return; }
        e.Graphics.Clear(BackColor);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
            Color.FromArgb(161, 172, 193), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed partial class DuoForm : Form
{
    private Mutex first, second;
    private bool active;
    private readonly Label status = new Label();
    private readonly Label count = new Label();
    private readonly Button enable = new DuoButton();
    private readonly Button account1 = new DuoButton();
    private readonly Button account2 = new DuoButton();
    private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    private static readonly Color Muted = Color.FromArgb(151, 164, 185);
    private static readonly Color Accent = Color.FromArgb(130, 112, 255);

    public DuoForm()
    {
        Text = "Roblox Duo";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(840, 790);
        AutoScroll = true;
        AutoScrollMinSize = new Size(840, 790);
        if (Screen.PrimaryScreen.WorkingArea.Height < 870)
            ClientSize = new Size(860, Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 80));
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(14, 18, 27);
        ForeColor = Color.FromArgb(239, 243, 250);
        DoubleBuffered = true;

        LabelAt(this, "DUO", 32, 25, 90, 30, 15, Color.White, true);
        LabelAt(this, "ROBLOX COMPANION", 117, 31, 260, 24, 9, Muted, false);
        LabelAt(this, "v0.3.0  /  EXPERIMENTAL", 580, 31, 230, 24, 9, Muted, false);
        LabelAt(this, "Two accounts. One place to start.", 30, 85, 780, 52, 27, Color.White, true);
        LabelAt(this, "Keep your accounts separate and your next session a click away.", 33, 143, 770, 30, 11, Muted, false);

        Panel mode = Card(32, 195, 776, 112);
        LabelAt(mode, "01  /  PREPARE YOUR SESSION", 22, 16, 425, 24, 9, Accent, true);
        status.SetBounds(22, 47, 430, 50);
        status.Text = "Close all Roblox windows, then enable Duo mode.";
        status.ForeColor = Color.White;
        mode.Controls.Add(status);
        SetupButton(mode, enable, "Enable Duo mode", 490, 32, 262, true);
        enable.Click += delegate { EnableMode(); };

        Panel left = Card(32, 327, 378, 193);
        Panel right = Card(430, 327, 378, 193);
        AccountCard(left, account1, "01", "First account", "Your first Roblox browser profile.");
        AccountCard(right, account2, "02", "Second account", "Sign in with a different Roblox account.");
        account1.Enabled = account2.Enabled = false;
        account1.Click += delegate { OpenAccount(1); };
        account2.Click += delegate { OpenAccount(2); };

        count.SetBounds(34, 607, 770, 28);
        count.ForeColor = Color.FromArgb(103, 218, 173);
        Controls.Add(count);
        LabelAt(this, "Minimize to tray. Double-click the tray icon to return.", 34, 643, 770, 25, 10, Muted, false);
        LabelAt(this, "Experimental compatibility. Not affiliated with Roblox Corporation.", 34, 671, 770, 25, 9, Muted, false);
        LabelAt(this, "Developed by overdosedclxwn", 34, 719, 770, 25, 10, Accent, true);
        LabelAt(this, "Ideas or feedback? DM overdosedclxwn on Discord.", 34, 748, 770, 25, 10, Muted, false);
        InitializeFeatures();
        timer.Interval = 2000;
        timer.Tick += delegate { UpdateCount(); };
        timer.Start();
        UpdateCount();
    }

    private Panel Card(int x, int y, int width, int height)
    {
        Panel panel = new Panel { BackColor = Color.FromArgb(23, 29, 42) };
        panel.SetBounds(x, y, width, height);
        panel.Paint += delegate(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(43, 52, 71)))
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        Controls.Add(panel);
        return panel;
    }

    private void AccountCard(Panel panel, Button button, string number, string title, string description)
    {
        LabelAt(panel, number, 22, 18, 45, 30, 16, Accent, true);
        Label heading = LabelAt(panel, title, 70, 20, 280, 33, 16, Color.White, true);
        heading.AutoEllipsis = true;
        if (number == "01") profileTitle1 = heading; else profileTitle2 = heading;
        LabelAt(panel, description, 22, 68, 335, 30, 10, Muted, false);
        SetupButton(panel, button, "Open account " + (number == "01" ? "1" : "2") + "  >", 22, 121, 334, false);
    }

    private Label LabelAt(Control parent, string text, int x, int y, int w, int h, float size, Color color, bool bold)
    {
        Label label = new Label { Text = text, ForeColor = color, UseMnemonic = false,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular) };
        label.SetBounds(x, y, w, h);
        parent.Controls.Add(label);
        return label;
    }

    private void SetupButton(Control parent, Button button, string text, int x, int y, int width, bool primary)
    {
        button.Text = text;
        button.AutoEllipsis = true;
        button.UseMnemonic = false;
        button.AccessibleName = text;
        button.SetBounds(x, y, width, 46);
        button.BackColor = primary ? Accent : Color.FromArgb(44, 54, 77);
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(148, 133, 255) : Color.FromArgb(60, 72, 99);
        button.Cursor = Cursors.Hand;
        parent.Controls.Add(button);
    }
    private static int PlayerCount()
    {
        Process[] players = Process.GetProcessesByName("RobloxPlayerBeta");
        int result = players.Length;
        foreach (Process p in players) p.Dispose();
        return result;
    }

    private void UpdateCount()
    {
        try { count.Text = "Roblox processes running: " + PlayerCount(); }
        catch { count.Text = "Process count unavailable."; }
    }

    private void EnableMode()
    {
        try
        {
            if (PlayerCount() > 0)
                throw new InvalidOperationException("Close all Roblox windows first, then try again.");
            first = CreateOwnedMutex("ROBLOX_singletonMutex");
            second = CreateOwnedMutex("ROBLOX_singletonEvent");
            active = true;
            enable.Enabled = false; enable.Text = "Duo mode enabled";
            account1.Enabled = account2.Enabled = true;
            status.Text = "Duo mode enabled. Open each account and press Play on Roblox.";
            status.ForeColor = Color.LightGreen;
        }
        catch (Exception ex)
        {
            ReleaseLocks();
            status.Text = "Could not enable Duo mode. Open Diagnostics for help.";
            status.ForeColor = Color.Salmon;
            ShowFailure("Could not enable Duo mode", ex);
        }
    }

    private static Mutex CreateOwnedMutex(string name)
    {
        bool created;
        Mutex mutex = new Mutex(true, name, out created);
        if (!created)
        {
            mutex.Dispose();
            throw new InvalidOperationException("The session lock is already in use. Close Roblox and other multi-instance tools, then try again.");
        }
        return mutex;
    }

    private void OpenAccount(int number)
    {
        try
        {
            string browser = FindBrowser(settings.Browser);
            if (browser == null)
                throw new InvalidOperationException(settings.Browser + " was not found in standard installation folders. Install it, select another browser in Settings, or open two profiles manually.");
            string profile = ProfilePath(settings.Browser, number);
            if (settings.Browser == "Firefox" && firefoxWindows[number - 1] != null && !firefoxWindows[number - 1].HasExited)
            {
                MessageBox.Show(this, "This Firefox profile is already open. Use its existing window to launch the game.", "Profile already open");
                return;
            }
            Directory.CreateDirectory(profile);
            Process launched = Process.Start(new ProcessStartInfo(browser, BrowserArguments(settings.Browser, profile)) { UseShellExecute = true });
            if (settings.Browser == "Firefox")
            {
                if (firefoxWindows[number - 1] != null) firefoxWindows[number - 1].Dispose();
                firefoxWindows[number - 1] = launched;
            }
            else if (launched != null) launched.Dispose();
        }
        catch (Exception ex)
        {
            ShowFailure("Could not open account", ex);
        }
    }

    private void ReleaseLocks()
    {
        foreach (Mutex mutex in new Mutex[] { second, first })
        {
            if (mutex == null) continue;
            try { mutex.ReleaseMutex(); } catch (ApplicationException) { }
            mutex.Dispose();
        }
        second = first = null;
        active = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.WindowsShutDown) { base.OnFormClosing(e); return; }
        if (active && MessageBox.Show(this, "Disable Duo mode and quit? Keep this window minimized while playing.", "Roblox Duo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            e.Cancel = true;
        base.OnFormClosing(e);
        if (!e.Cancel) { timer.Stop(); ReleaseLocks(); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            if (trayMenu != null) trayMenu.Dispose();
            foreach (Process process in firefoxWindows) if (process != null) process.Dispose();
            ReleaseLocks();
        }
        base.Dispose(disposing);
    }

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new DuoForm());
    }
}
