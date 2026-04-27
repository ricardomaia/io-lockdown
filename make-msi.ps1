# Script para gerar MSI com WiX v5
Write-Host "Compilando projeto..." -ForegroundColor Cyan
./build.ps1

Write-Host "`nGerando MSI com WiX..." -ForegroundColor Cyan

$WIX_EXE = "$env:USERPROFILE\.dotnet\tools\wix.exe"

if (!(Test-Path $WIX_EXE)) {
    Write-Host "Erro: WiX não encontrado em $WIX_EXE." -ForegroundColor Red
    exit
}

# Adiciona as extensões necessárias (Aceitando a EULA wix7 conforme exigido)
& $WIX_EXE extension add WixToolset.UI.wixext/5.0.2 --global -acceptEula wix7
& $WIX_EXE extension add WixToolset.Util.wixext/5.0.2 --global -acceptEula wix7

# Compila e gera o MSI com ambas as extensões
& $WIX_EXE build Package.wxs -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext -o "IOLockdown_Installer.msi" -acceptEula wix7

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSucesso! Arquivo 'IOLockdown_Installer.msi' gerado." -ForegroundColor Green
} else {
    Write-Host "`nErro ao gerar MSI." -ForegroundColor Red
}
