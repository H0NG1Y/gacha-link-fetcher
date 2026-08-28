param(
    [string]$Version = "4.0.0",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$buildDirectory = Join-Path $PSScriptRoot "build"
$distDirectory = Join-Path $PSScriptRoot "dist"
$appPath = Join-Path $buildDirectory "GachaLinkFetcher.exe"
$portableName = "GachaLinkFetcher-v$Version.exe"
$portablePath = Join-Path $distDirectory $portableName

$assemblyInfo = Get-Content -LiteralPath (Join-Path $PSScriptRoot "Properties\AssemblyInfo.cs") -Raw
$expectedVersionLine = 'AssemblyInformationalVersion("' + $Version + '")'
if (-not $assemblyInfo.Contains($expectedVersionLine)) {
    throw "AssemblyInformationalVersion 与目标版本 v$Version 不一致。"
}

New-Item -ItemType Directory -Force -Path $buildDirectory, $distDirectory | Out-Null
& (Join-Path $PSScriptRoot "Build.ps1") -OutputPath $appPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath $appPath -Destination $portablePath -Force

$artifactPaths = [System.Collections.Generic.List[string]]::new()
$artifactPaths.Add($portablePath)

if (-not $SkipInstaller) {
    $compilerCandidates = @(
        (Join-Path $env:ProgramFiles "Inno Setup 7\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 7\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
    $innoCompiler = $compilerCandidates | Select-Object -First 1
    if (-not $innoCompiler) { throw "未找到 Inno Setup 7/6 编译器 ISCC.exe。" }

    & $innoCompiler "/DMyAppVersion=$Version" (Join-Path $PSScriptRoot "installer\GachaLinkFetcher.iss")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $setupPath = Join-Path $distDirectory "GachaLinkFetcher-Setup-v$Version.exe"
    if (-not (Test-Path -LiteralPath $setupPath)) { throw "安装程序未生成：$setupPath" }
    $artifactPaths.Add($setupPath)

    $setupHash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
    $setupChecksumPath = "$setupPath.sha256"
    [System.IO.File]::WriteAllText($setupChecksumPath, "$setupHash  $([System.IO.Path]::GetFileName($setupPath))`r`n", [System.Text.Encoding]::ASCII)
    $artifactPaths.Add($setupChecksumPath)
}

$checksumLines = foreach ($artifactPath in $artifactPaths | Where-Object { -not $_.EndsWith(".sha256", [System.StringComparison]::OrdinalIgnoreCase) }) {
    $hash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash
    "$hash  $([System.IO.Path]::GetFileName($artifactPath))"
}
$checksumsPath = Join-Path $distDirectory "checksums.txt"
[System.IO.File]::WriteAllText($checksumsPath, (($checksumLines -join "`r`n") + "`r`n"), [System.Text.Encoding]::ASCII)

Write-Host "Release artifacts:"
Get-ChildItem -LiteralPath $distDirectory -File | Sort-Object Name | Select-Object Name, Length, LastWriteTime
