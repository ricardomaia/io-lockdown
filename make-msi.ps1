# Script para gerar MSI com WiX v5
Write-Host "Compilando projeto..." -ForegroundColor Cyan
./build.ps1

Write-Host "`nGerando MSI com WiX..." -ForegroundColor Cyan

$WIX_EXE = "$env:USERPROFILE\.dotnet\tools\wix.exe"

if (!(Test-Path $WIX_EXE)) {
    Write-Host "Erro: WiX não encontrado em $WIX_EXE." -ForegroundColor Red
    exit
}

# Remove extensões globais que podem causar conflito de versão
# & $WIX_EXE extension remove WixToolset.UI.wixext --global 2>$null

# Tenta baixar a extensão compatível com v5 para o projeto local
# No WiX v5, as extensões podem ser baixadas via NuGet automaticamente no comando build se configurado,
# mas vamos tentar adicionar manualmente a versão correta.
& $WIX_EXE extension add WixToolset.UI.wixext/5.0.2 --global

# Compila e gera o MSI
& $WIX_EXE build Package.wxs -ext WixToolset.UI.wixext -o "IOLockdown_Installer.msi"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSucesso! Arquivo 'IOLockdown_Installer.msi' gerado." -ForegroundColor Green
} else {
    Write-Host "`nErro ao gerar MSI." -ForegroundColor Red
}
