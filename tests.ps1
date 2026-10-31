$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$assembly = [Reflection.Assembly]::LoadFile((Join-Path $PSScriptRoot 'dist\RobloxDuo.exe'))
$type = $assembly.GetType('DuoForm', $true)
$flags = [Reflection.BindingFlags]'NonPublic,Static'
$create = $type.GetMethod('CreateOwnedMutex', $flags)
$name = 'RobloxDuo-Test-' + [Guid]::NewGuid().ToString('N')
$lock = $null
try {
    $lock = $create.Invoke($null, @($name))
    $rejected = $false
    try { $unexpected = $create.Invoke($null, @($name)) }
    catch { $rejected = $true }
    if (-not $rejected) { throw 'An existing named mutex was incorrectly accepted.' }
} finally {
    if ($lock) { $lock.ReleaseMutex(); $lock.Dispose() }
}
$leftover = $null
if ([Threading.Mutex]::TryOpenExisting($name, [ref]$leftover)) {
    $leftover.Dispose()
    throw 'The test mutex leaked after cleanup.'
}
Write-Host 'PASS: mutex creation, collision rejection, and cleanup'

$settingsType = $assembly.GetType('DuoSettings', $true)
$settings = [Activator]::CreateInstance($settingsType)
$settings.FirstName = 'Main test'
$settings.SecondName = 'Alt test'
$settings.Browser = 'Firefox'
$settings.Repository = 'example/roblox-duo'
$testDirectory = Join-Path $PSScriptRoot ('dist\test-' + [Guid]::NewGuid().ToString('N'))
$testSettings = Join-Path $testDirectory 'settings.xml'
try {
    $settings.Save($testSettings)
    $loaded = $settingsType.GetMethod('Load').Invoke($null, @([string]$testSettings))
    if ($loaded.FirstName -ne 'Main test' -or $loaded.Browser -ne 'Firefox') { throw 'Settings did not round-trip.' }
    $settings.FirstName = 'Renamed'
    $settings.Save($testSettings)
    $loaded = $settingsType.GetMethod('Load').Invoke($null, @([string]$testSettings))
    if ($loaded.FirstName -ne 'Renamed') { throw 'Settings replacement failed.' }
    foreach ($bad in @('https://github.com/example/repo', '../repo', 'owner/repo?bad=true', 'owner/repo/extra')) {
        if ($settingsType.GetMethod('ValidRepository').Invoke($null, @($bad))) { throw 'Invalid repository was accepted.' }
    }
} finally {
    if ([IO.File]::Exists($testSettings)) { [IO.File]::Delete($testSettings) }
    if ([IO.Directory]::Exists($testDirectory)) { [IO.Directory]::Delete($testDirectory) }
}
Write-Host 'PASS: settings round-trip, replacement, and repository validation'

$parseVersion = $type.GetMethod('ReleaseVersion', $flags)
$version = $parseVersion.Invoke($null, @('{"tag_name":"v0.10.0","draft":false,"prerelease":false}'))
if ($version -le [Version]'0.3.0.0') { throw 'Versions were compared incorrectly.' }
foreach ($bad in @('{}', '{"tag_name":"latest"}', '{"tag_name":"v1.0.0","prerelease":true}', '{"tag_name":"v1.0.0","draft":true}')) {
    $rejected = $false
    try { $null = $parseVersion.Invoke($null, @($bad)) } catch { $rejected = $true }
    if (-not $rejected) { throw 'Invalid or non-stable release accepted.' }
}
$paths = @()
foreach ($browser in @('Edge', 'Chrome', 'Firefox')) {
    foreach ($number in @(1, 2)) {
        $paths += $type.GetMethod('ProfilePath', $flags).Invoke($null, @($browser, $number))
    }
    $arguments = $type.GetMethod('BrowserArguments', $flags).Invoke($null, @($browser, 'C:\Test Space\Profile'))
    if ($arguments -notlike '*"C:\Test Space\Profile"*') { throw 'Browser profile path was not quoted.' }
    if ($browser -eq 'Firefox' -and $arguments -notlike '*-no-remote -profile*') { throw 'Firefox profile isolation missing.' }
}
if (($paths | Select-Object -Unique).Count -ne 6) { throw 'Browser profiles overlap.' }
if ($paths[0] -notlike '*RobloxDuo\Account1') { throw 'Legacy Edge profile path changed.' }
Write-Host 'PASS: release validation, numeric versions, browser arguments, and profile isolation'

[Windows.Forms.Application]::EnableVisualStyles()
$form = [Activator]::CreateInstance($type)
try {
    $fields = [Reflection.BindingFlags]'NonPublic,Instance'
    foreach ($name in @('account1', 'account2')) {
        if ($type.GetField($name, $fields).GetValue($form).Enabled) { throw 'Account button enabled before preparation.' }
    }
    function Check-English($control) {
        if ($control.Text -match '[\u0400-\u04FF]') { throw "Untranslated UI text: $($control.Text)" }
        foreach ($child in $control.Controls) { Check-English $child }
    }
    Check-English $form
    $form.StartPosition = [Windows.Forms.FormStartPosition]::Manual
    $form.Location = [Drawing.Point]::new(-32000, -32000)
    $form.ShowInTaskbar = $false
    $form.Show()
    [Windows.Forms.Application]::DoEvents()
    $type.GetMethod('MinimizeToTray', $fields).Invoke($form, @())
    $tray = $type.GetField('tray', $fields).GetValue($form)
    if ($form.Visible -or $form.ShowInTaskbar -or -not $tray.Visible) { throw 'Minimize-to-tray failed.' }
    $type.GetMethod('RestoreFromTray', $fields).Invoke($form, @())
    if (-not $form.Visible -or -not $form.ShowInTaskbar -or $tray.Visible) { throw 'Restore-from-tray failed.' }
    $report = $type.GetMethod('DiagnosticReport', $fields).Invoke($form, @())
    if ($report -notlike '*What to try*' -or $report -notlike '*Selected browser*') { throw 'Diagnostic report incomplete.' }
    Write-Host 'PASS: tray hide/restore and diagnostic report'
    $bitmap = New-Object Drawing.Bitmap $form.Width, $form.Height
    try {
        # Capture only the application's own rendered controls, without desktop pixels.
        $form.DrawToBitmap($bitmap, [Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height))
        $docs = Join-Path $PSScriptRoot 'docs'
        New-Item -ItemType Directory -Force $docs | Out-Null
        $bitmap.Save((Join-Path $docs 'interface.png'), [Drawing.Imaging.ImageFormat]::Png)
    } finally { $bitmap.Dispose() }
    Write-Host 'PASS: initial button state, English UI, and form rendering'
} finally {
    $type.GetField('timer', $fields).GetValue($form).Dispose()
    $form.Dispose()
}
