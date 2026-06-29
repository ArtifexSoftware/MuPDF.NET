#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$setup = Join-Path $scriptDir 'setup_layout_python.py'
if (Get-Command python -ErrorAction SilentlyContinue) {
    python $setup @args
} elseif (Get-Command py -ErrorAction SilentlyContinue) {
    py -3 $setup @args
} else {
    Write-Error 'Python not found. Install Python 3.10+ or set PYTHON to an interpreter.'
}
exit $LASTEXITCODE
