# Roblox Duo

A small, experimental Windows companion for opening two Roblox accounts side by side. English interface, separate browser profiles, and no installer.

![Roblox Duo interface](docs/interface.png)

## Getting started

Requires Windows 10/11, .NET Framework 4.5 or newer, Edge/Chrome/Firefox, and the desktop Roblox Player installed from roblox.com. Browsers must be installed in standard locations.

1. Close every Roblox window and any other multi-instance utility.
2. Run `RobloxDuo.exe` and select **Enable Duo mode**.
3. Select **Open Main**, sign in on the official Roblox website, and press **Play** in your chosen experience. Allow the browser to open Roblox.
4. Select **Open Alt**, sign in to a **different account**, and launch an experience.
5. Keep Duo running while playing. Minimizing hides it in the Windows system tray. Double-click its icon or choose **Open Roblox Duo** from the tray menu to restore it. Use **Exit** or the window close button to quit.

Buttons open browser profiles; they do not automatically authenticate or join a game. The counter reports RobloxPlayerBeta processes, not successful game sessions.

## Settings and diagnostics

- **Settings:** rename Main and Alt (up to 24 characters), select a browser, and configure a public GitHub repository. Settings persist in `%LOCALAPPDATA%\RobloxDuo\settings.xml`. Renaming a profile does not change its directory or sign-in.
- **Browser profiles:** Edge retains existing v0.2.0 sessions. Chrome and Firefox use separate directories per browser and account; switching browsers requires signing in there. Firefox uses a dedicated profile with `-no-remote`; reuse an already-open Firefox profile window.
- **Diagnostics:** view the mode, process count, selected browser availability, Roblox launch protocol, latest error, and suggested fixes. The report can be selected and copied and does not contain passwords or cookies.
- **Check for updates:** after publication, enter the project's `owner/repository` in Settings. Duo checks the [latest stable GitHub release](https://docs.github.com/en/rest/releases/releases#get-the-latest-release) only when clicked. Newer versions offer to open GitHub for manual download; nothing is downloaded, installed, or executed automatically. Release tags should follow `vMAJOR.MINOR.PATCH`. Missing releases, rate limits, and connection errors are reported. No repository is configured by default because the project has not yet been published.

Firefox launch arguments follow [Mozilla's command-line documentation](https://firefox-source-docs.mozilla.org/browser/CommandLineParameters.html).

## Compatibility

**Experimental:** simultaneous gameplay has not been validated with two signed-in accounts. Roblox updates may prevent this method from working. A successful build or an enabled status does not establish client compatibility. The Microsoft Store client is not tested.

Duo creates and holds the named Windows mutexes `ROBLOX_singletonMutex` and `ROBLOX_singletonEvent` before Roblox starts. This approach is also described in [this community implementation](https://github.com/randheimer/roblox-multi-instance). Duo does not patch the client, inject code, or close handles in other processes.

If the first game closes when the second launches, the current client may be incompatible. If enabling fails, close other Roblox sessions and multi-instance utilities before retrying. If the selected browser is missing, choose an installed browser in Settings or use separate profiles manually after enabling Duo mode.

## Privacy

The application does not read passwords or cookies, and contains no telemetry. Sign-in happens on roblox.com in your chosen browser. Edge stores the two persistent profiles under `%LOCALAPPDATA%\RobloxDuo\Account1` and `Account2`. Chrome and Firefox use `Chrome\Account1`, `Chrome\Account2`, `Firefox\Account1`, and `Firefox\Account2` beneath that same root. Sign out in those browser windows to remove active account sessions. Do not commit or share these profile directories. A manual update check sends a normal HTTPS request to GitHub's public API without an access token.

## Build from source

On Windows, in PowerShell:

```powershell
.\build.ps1
```

Output: `dist/RobloxDuo.exe`. The build uses the C# compiler included with the Windows .NET Framework; no NuGet packages are required.

If local PowerShell policy blocks scripts, review the scripts first, then run `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1`. This applies only to that process and does not change the system execution policy. The same process option can be used with the test command below.

Run the isolated lock and UI checks:

```powershell
powershell -NoProfile -STA -File .\tests.ps1
```

Tests cover mutex collision/cleanup, settings persistence and validation, version parsing, browser arguments and profile separation, tray hide/restore, diagnostics, and UI rendering. They do not launch browsers or Roblox or sign in to an account. Live update checks against this project's future repository and two-account gameplay remain unverified.

## Publishing

The GitHub Actions workflow builds and tests on pushes and pull requests. A version tag such as `v0.3.0` also creates a GitHub Release containing the portable ZIP. Releases are unsigned. Tag only the commit you intend to publish.

## Developer and feedback

Developed by **overdosedclxwn**.

Ideas or feedback? DM **overdosedclxwn** on Discord.

## License

MIT. Independent community project; not affiliated with or endorsed by Roblox Corporation. Roblox is a trademark of its respective owner.
