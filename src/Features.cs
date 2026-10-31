using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;

public sealed class DuoSettings
{
    public string FirstName = "Main";
    public string SecondName = "Alt";
    public string Browser = "Edge";
    public string Repository = "";

    public static bool ValidRepository(string value)
    {
        return value == "" || (value != null && Regex.IsMatch(value, @"^[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_][A-Za-z0-9_.-]{0,99}$"));
    }

    public void Validate()
    {
        if (String.IsNullOrWhiteSpace(FirstName) || FirstName.Length > 24 ||
            String.IsNullOrWhiteSpace(SecondName) || SecondName.Length > 24)
            throw new InvalidDataException("Profile names must contain 1 to 24 characters.");
        if (Browser != "Edge" && Browser != "Chrome" && Browser != "Firefox")
            throw new InvalidDataException("Choose Edge, Chrome, or Firefox.");
        if (!ValidRepository(Repository))
            throw new InvalidDataException("Enter a GitHub repository as owner/repository, or leave it blank.");
    }

    public static DuoSettings Load(string path)
    {
        if (!File.Exists(path)) return new DuoSettings();
        using (var reader = System.Xml.XmlReader.Create(path, new System.Xml.XmlReaderSettings {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null }))
        {
            DuoSettings value = (DuoSettings)new XmlSerializer(typeof(DuoSettings)).Deserialize(reader);
            value.Validate();
            return value;
        }
    }

    public void Save(string path)
    {
        Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var writer = File.CreateText(temp)) new XmlSerializer(typeof(DuoSettings)).Serialize(writer, this);
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

internal sealed partial class DuoForm
{
    private const string AppVersion = "0.3.0";
    private DuoSettings settings = new DuoSettings();
    private Label profileTitle1, profileTitle2;
    private NotifyIcon tray;
    private ContextMenuStrip trayMenu;
    private readonly Process[] firefoxWindows = new Process[2];
    private readonly Button updates = new DuoButton();
    private string lastIssue = "None recorded.";
    private static string SettingsPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxDuo", "settings.xml"); } }

    private void InitializeFeatures()
    {
        try { settings = DuoSettings.Load(SettingsPath); }
        catch { lastIssue = "Saved settings could not be read. Defaults are active. Open Settings and save to repair the file."; }
        ApplySettings();
        Button preferences = new DuoButton(), diagnostics = new DuoButton();
        SetupButton(this, preferences, "Settings", 32, 545, 240, false);
        SetupButton(this, diagnostics, "Diagnostics", 290, 545, 240, false);
        SetupButton(this, updates, "Check for updates", 548, 545, 260, false);
        preferences.Click += delegate { ShowSettings(); };
        diagnostics.Click += delegate { ShowDiagnostics(); };
        updates.Click += async delegate { await CheckUpdates(); };
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open Roblox Duo", null, delegate { RestoreFromTray(); });
        trayMenu.Items.Add("Settings", null, delegate { RestoreFromTray(); ShowSettings(); });
        trayMenu.Items.Add("Diagnostics", null, delegate { RestoreFromTray(); ShowDiagnostics(); });
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, delegate { RestoreFromTray(); Close(); });
        tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "Roblox Duo", ContextMenuStrip = trayMenu, Visible = false };
        tray.DoubleClick += delegate { RestoreFromTray(); };
        Resize += delegate { if (WindowState == FormWindowState.Minimized) MinimizeToTray(); };
    }

    private void MinimizeToTray()
    {
        tray.Visible = true;
        Hide();
        ShowInTaskbar = false;
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        tray.Visible = false;
    }

    private void ApplySettings()
    {
        profileTitle1.Text = settings.FirstName;
        profileTitle2.Text = settings.SecondName;
        account1.Text = "Open " + settings.FirstName + "  >";
        account2.Text = "Open " + settings.SecondName + "  >";
        account1.AccessibleName = "Open profile " + settings.FirstName;
        account2.AccessibleName = "Open profile " + settings.SecondName;
    }

    private void ShowSettings()
    {
        using (Form dialog = new Form { Text = "Settings", ClientSize = new Size(560, 420),
            StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, BackColor = BackColor, ForeColor = ForeColor, Font = Font })
        {
            string[] labels = { "First profile name", "Second profile name", "Browser", "GitHub repository (owner/repository)" };
            for (int i = 0; i < labels.Length; i++) LabelAt(dialog, labels[i], 24, 20 + i * 73, 505, 23, 10, Muted, false);
            TextBox firstName = new TextBox { Text = settings.FirstName, MaxLength = 24 };
            TextBox secondName = new TextBox { Text = settings.SecondName, MaxLength = 24 };
            ComboBox browser = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            browser.Items.AddRange(new object[] { "Edge", "Chrome", "Firefox" });
            browser.SelectedItem = settings.Browser;
            TextBox repository = new TextBox { Text = settings.Repository, MaxLength = 140 };
            Control[] inputs = { firstName, secondName, browser, repository };
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i].SetBounds(24, 46 + i * 73, 510, 28);
                inputs[i].AccessibleName = labels[i];
                dialog.Controls.Add(inputs[i]);
            }
            LabelAt(dialog, "Changing names keeps your sign-ins. Each browser uses separate profiles.", 24, 307, 510, 40, 9, Muted, false);
            Button save = new DuoButton();
            SetupButton(dialog, save, "Save settings", 24, 358, 250, true);
            Button cancel = new DuoButton();
            SetupButton(dialog, cancel, "Cancel", 292, 358, 242, false);
            cancel.DialogResult = DialogResult.Cancel;
            dialog.AcceptButton = save; dialog.CancelButton = cancel;
            save.Click += delegate
            {
                try
                {
                    DuoSettings next = new DuoSettings { FirstName = firstName.Text.Trim(), SecondName = secondName.Text.Trim(), Browser = (string)browser.SelectedItem, Repository = repository.Text.Trim() };
                    next.Save(SettingsPath);
                    settings = next; ApplySettings(); dialog.DialogResult = DialogResult.OK;
                }
                catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Could not save settings"); }
            };
            dialog.ShowDialog(this);
        }
    }

    private static string FindBrowser(string browser)
    {
        string relative = browser == "Firefox" ? @"Mozilla Firefox\firefox.exe" :
            browser == "Chrome" ? @"Google\Chrome\Application\chrome.exe" : @"Microsoft\Edge\Application\msedge.exe";
        foreach (string root in new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
        {
            string candidate = Path.Combine(root, relative);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static string ProfilePath(string browser, int number)
    {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxDuo");
        // Preserve existing Edge sessions from v0.2.0.
        return browser == "Edge" ? Path.Combine(root, "Account" + number) : Path.Combine(root, browser, "Account" + number);
    }

    private static string BrowserArguments(string browser, string profile)
    {
        return browser == "Firefox" ? "-no-remote -profile \"" + profile + "\" https://www.roblox.com/home" :
            "--user-data-dir=\"" + profile + "\" --no-first-run --new-window https://www.roblox.com/home";
    }

    private void ShowFailure(string title, Exception ex)
    {
        string help = ex is UnauthorizedAccessException ? "Windows denied access. Close other Duo tools and Roblox, then restart Duo under the same Windows account. Check folder permissions if saving or opening a profile failed." :
            ex is System.Threading.WaitHandleCannotBeOpenedException ? "A Roblox session object already exists with a conflicting type. Close Roblox and other multi-instance tools, then retry. If it persists, sign out of Windows and back in." : ex.Message;
        lastIssue = title + ": " + help;
        MessageBox.Show(this, help + "\n\nUse Diagnostics for the current status.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string DiagnosticReport()
    {
        string running;
        try { running = PlayerCount().ToString(); } catch { running = "Unavailable"; }
        bool protocol = false;
        try { using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"roblox-player\shell\open\command")) protocol = key != null; } catch { }
        return "Roblox Duo " + AppVersion + "\r\n\r\n" +
            "Duo mode: " + (active ? "Enabled (compatibility is not guaranteed)" : "Disabled") + "\r\n" +
            "Roblox processes: " + running + "\r\n" +
            "Selected browser: " + settings.Browser + "\r\n" +
            "Browser installation: " + (FindBrowser(settings.Browser) != null ? "Found" : "Not found in standard folders") + "\r\n" +
            "Roblox launch protocol: " + (protocol ? "Registered" : "Not found or inaccessible") + "\r\n" +
            "Update repository: " + (settings.Repository == "" ? "Not configured" : settings.Repository) + "\r\n\r\n" +
            "Last issue\r\n" + lastIssue + "\r\n\r\n" +
            "What to try\r\n1. Close all Roblox windows before enabling Duo.\r\n2. Close other multi-instance tools if the session lock is busy.\r\n3. If the browser is missing, install it or select another in Settings.\r\n4. If the launch protocol is missing, reinstall Roblox from roblox.com.\r\n5. Use different Roblox accounts. If the first game still closes, the current Roblox client may be incompatible.\r\n6. For Firefox, reuse an already-open profile window; do not launch it twice.\r\n\r\nNo passwords or cookies are included in this report.";
    }

    private void ShowDiagnostics()
    {
        using (Form dialog = new Form { Text = "Diagnostics", ClientSize = new Size(650, 540), StartPosition = FormStartPosition.CenterParent, Font = Font })
        {
            TextBox report = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill, Text = DiagnosticReport(), BackColor = BackColor, ForeColor = ForeColor };
            dialog.Controls.Add(report);
            dialog.ShowDialog(this);
        }
    }

    internal static Version ReleaseVersion(string json)
    {
        var release = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
        if (release == null || !release.ContainsKey("tag_name")) throw new InvalidDataException("GitHub returned an invalid release response.");
        foreach (string flag in new string[] { "draft", "prerelease" })
            if (release.ContainsKey(flag) && Object.Equals(release[flag], true)) throw new InvalidDataException("No stable release was returned.");
        string tag = release["tag_name"] as string;
        if (tag == null || !Regex.IsMatch(tag, @"^v?\d+\.\d+\.\d+(\.\d+)?$"))
            throw new InvalidDataException("The latest release tag must use a version such as v0.3.0.");
        Version version;
        if (!Version.TryParse(tag.TrimStart('v'), out version)) throw new InvalidDataException("The release version is invalid.");
        return new Version(version.Major, version.Minor, version.Build, Math.Max(0, version.Revision));
    }

    private async System.Threading.Tasks.Task CheckUpdates()
    {
        if (settings.Repository == "")
        {
            MessageBox.Show(this, "No public repository is configured yet. Open Settings and enter owner/repository after the project is published.", "Updates are not configured");
            return;
        }
        string repository = settings.Repository;
        updates.Enabled = false; updates.Text = "Checking...";
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RobloxDuo/" + AppVersion);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                using (HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/" + repository + "/releases/latest"))
                {
                    if (IsDisposed) return;
                    if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidDataException("Repository or public release not found. Check Settings and publish a stable GitHub Release first.");
                    if ((int)response.StatusCode == 403 || (int)response.StatusCode == 429) throw new InvalidDataException("GitHub temporarily limited this request. Try again later.");
                    response.EnsureSuccessStatusCode();
                    Version latest = ReleaseVersion(await response.Content.ReadAsStringAsync());
                    if (IsDisposed) return;
                    if (latest <= new Version(0, 3, 0, 0)) MessageBox.Show(this, "You are up to date (v" + AppVersion + ").", "Updates");
                    else if (MessageBox.Show(this, "Version " + latest.ToString(3) + " is available from " + repository + ".\n\nOpen its GitHub Releases page to download manually?", "Update available", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        Process.Start(new ProcessStartInfo("https://github.com/" + repository + "/releases/latest") { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed) ShowFailure("Update check failed", ex is System.Threading.Tasks.TaskCanceledException ? new Exception("The request timed out. Check your connection and try again.") :
                ex is HttpRequestException ? new Exception("Cannot reach GitHub. Check your internet connection or proxy and try again.") : ex);
        }
        finally { if (!IsDisposed) { updates.Enabled = true; updates.Text = "Check for updates"; } }
    }
}
