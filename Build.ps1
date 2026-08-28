param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "build\GachaLinkFetcher.exe")
)

$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) { throw "未找到 .NET Framework C# 编译器：$compiler" }
$iconPath = Join-Path $PSScriptRoot "GachaLinkFetcher.ico"
$manifestPath = Join-Path $PSScriptRoot "app.manifest"
$sourceFiles = Get-ChildItem -Path $PSScriptRoot -Recurse -File -Filter *.cs | ForEach-Object { $_.FullName }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
$arguments = @("/nologo", "/target:winexe", "/platform:x64", "/optimize+", "/out:$OutputPath", "/win32icon:$iconPath", "/win32manifest:$manifestPath", "/r:System.Windows.Forms.dll", "/r:System.Drawing.dll", "/r:System.Web.Extensions.dll") + $sourceFiles
& $compiler $arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Built: $OutputPath"
