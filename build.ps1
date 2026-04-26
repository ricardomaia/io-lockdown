# Script de Compilação I/O Lockdown
Write-Host "Iniciando compilação do I/O Lockdown..." -ForegroundColor Cyan

# Limpa pastas antigas
if (Test-Path "./publish") { Remove-Item -Recurse -Force "./publish" }

# Compila como executável único (Self-Contained) para Windows 64-bit
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSucesso! O executável foi gerado na pasta './publish'." -ForegroundColor Green
    Write-Host "Arquivo: io-lockdown.exe" -ForegroundColor Yellow
} else {
    Write-Host "`nErro durante a compilação." -ForegroundColor Red
}
