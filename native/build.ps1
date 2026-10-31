$ErrorActionPreference = 'Stop'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /optimize+ "/out:$PSScriptRoot\DuoBridge.exe" "$PSScriptRoot\Bridge.cs"
if ($LASTEXITCODE -ne 0) { throw 'Native build failed' }
