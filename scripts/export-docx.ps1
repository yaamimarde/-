$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$md = Join-Path $root "docs\药品管理系统-完整文档.md"
$docx = Join-Path $root "docs\药品管理系统-完整文档.docx"

if (-not (Test-Path -LiteralPath $md)) {
    Write-Error "Missing: $md`nRun: py scripts/merge-docs.py"
}

$pandoc = Get-Command pandoc -ErrorAction SilentlyContinue
if ($pandoc) {
    & pandoc $md -o $docx --toc --toc-depth=3 -f markdown -t docx
} else {
    py -c @"
import pypandoc
from pathlib import Path
md = Path(r'$md')
docx = Path(r'$docx')
pypandoc.convert_file(str(md), 'docx', outputfile=str(docx), extra_args=['--toc', '--toc-depth=3'])
print('Exported via pypandoc:', docx)
"@
}

if (Test-Path -LiteralPath $docx) {
    Write-Host "Exported: $docx"
} else {
    Write-Error "Export failed. Install pandoc or: py -m pip install pypandoc_binary"
}
