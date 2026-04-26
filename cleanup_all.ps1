# Script de Limpeza Total I/O Lockdown
# EXECUTAR COMO ADMINISTRADOR

Write-Host "Iniciando limpeza profunda do I/O Lockdown..." -ForegroundColor Cyan

# 1. Para o serviço e processos
Write-Host "Encerrando processos e serviços..."
Stop-Service -Name "IOLockdownService" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "io-lockdown" -Force -ErrorAction SilentlyContinue
taskkill /F /IM "io-lockdown.exe" /T 2>$null

# 2. Desinstala via MSI (procurando pelo nome em todos os contextos)
$apps = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -match "I/O Lockdown" }
foreach ($app in $apps) {
    Write-Host "Desinstalando: $($app.Name) (Versão $($app.Version))..." -ForegroundColor Yellow
    $app.Uninstall()
}

# 3. Remove o serviço caso tenha sobrado
sc.exe delete IOLockdownService 2>$null

# 4. Limpa pastas de instalação
$installDir = "C:\Program Files\IOLockdown"
if (Test-Path $installDir) {
    Write-Host "Removendo pasta de instalação..."
    Remove-Item -Recurse -Force $installDir
}

# 5. Limpa atalhos do menu iniciar
$shortcutDir = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\IO Lockdown"
if (Test-Path $shortcutDir) {
    Remove-Item -Recurse -Force $shortcutDir
}

Write-Host "`nSistema limpo! Agora você pode instalar o MSI mais recente (v1.0.1.0)." -ForegroundColor Green
