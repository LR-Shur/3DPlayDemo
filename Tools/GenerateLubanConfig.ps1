param(
    [switch]$SkipCode
)

$scriptRoot = Split-Path -Parent $PSCommandPath

# Luban 配置生成入口：修改 CSV/XML 后运行本脚本即可重新生成代码和二进制表。
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
$lubanDll = Join-Path $projectRoot 'Tools/Luban/Luban.dll'
$conf = Join-Path $projectRoot 'Assets/Config/Luban/luban.conf'
$codeOutput = Join-Path $projectRoot 'Assets/Scripts/Composition/Config/Generated'
$dataOutput = Join-Path $projectRoot 'Assets/StreamingAssets/Config/LubanBytes'

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
