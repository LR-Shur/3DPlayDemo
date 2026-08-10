param(
    [switch]$SkipCode
)

$scriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($scriptPath)) {
    $scriptPath = $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrWhiteSpace($scriptPath)) {
    $scriptPath = $MyInvocation.MyCommand.Definition
}
$scriptRoot = Split-Path -Parent $scriptPath
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    # 某些 Unity/PowerShell 启动器不会提供脚本路径，文档约定从项目根目录执行时使用当前目录兜底。
    $scriptRoot = Join-Path (Get-Location).Path 'Tools'
}

# Luban 配置生成入口：修改 CSV/XML 后运行本脚本即可重新生成代码和二进制表。
# 优先使用当前目录（文档约定从 Unity 项目根目录执行），这样兼容部分 PowerShell 启动器不传脚本路径的情况。
$projectRoot = [System.IO.Directory]::GetCurrentDirectory()
if ([string]::IsNullOrWhiteSpace($projectRoot)) {
    $projectRoot = 'D:\UnityProjectTrain\3DPlayDemo'
}
if (-not (Test-Path (Join-Path $projectRoot 'Assets'))) {
    $projectRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
}
$lubanDll = Join-Path $projectRoot 'Tools/Luban/Luban.dll'
$conf = Join-Path $projectRoot 'Assets/Config/Luban/luban.conf'
$codeOutput = Join-Path $projectRoot 'Assets/Scripts/Composition/Config/Generated'
$dataOutput = Join-Path $projectRoot 'Assets/StreamingAssets/Config/LubanBytes'
$csvDirectory = Join-Path $projectRoot 'Assets/Config/Luban/Data'

$arguments = @(
    $lubanDll,
    '-t', 'client',
    '-c', 'cs-bin',
    '-d', 'bin',
    '--conf', $conf,
    '-x', "outputDataDir=$dataOutput",
    '-x', 'pathValidator.rootDir=.'
)

if (-not $SkipCode) {
    $arguments += @('-x', "outputCodeDir=$codeOutput")
}

Push-Location $projectRoot
try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Luban 生成失败，退出码：$LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
