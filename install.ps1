# Script de Instalação I/O Lockdown
# EXECUTAR COMO ADMINISTRADOR

$installDir = "C:\Program Files\IOLockdown"
$exePath = "$installDir\io-lockdown.exe"
$serviceName = "IOLockdownService"

Write-Host "Instalando I/O Lockdown em $installDir..." -ForegroundColor Cyan

# 1. Cria diretório de instalação
if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir
}

# 2. Para o serviço se já existir
if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "Parando serviço existente..."
    Stop-Service $serviceName
    sc.exe delete $serviceName
}

# 3. Copia arquivos da pasta publish
if (Test-Path "./publish") {
    Copy-Item "./publish/*" $installDir -Recurse -Force
} else {
    Write-Host "Erro: Pasta './publish' não encontrada. Execute ./build.ps1 primeiro." -ForegroundColor Red
    exit
}

# 4. Cria o Serviço do Windows (Motor de Proteção)
# O serviço roda como LocalSystem para ter privilégios totais de hardware
sc.exe create $serviceName binPath= "\"$exePath\" --service" start= auto displayname= "I/O Lockdown Security Service"
sc.exe description $serviceName "Protege adaptadores de rede e portas USB contra ataques físicos durante o bloqueio da sessão."
Start-Service $serviceName

# 5. Configura a Interface (Tray Icon) para iniciar com o Windows para o usuário logado
$registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Set-ItemProperty -Path $registryPath -Name "IOLockdownUI" -Value "\"$exePath\""

Write-Host "`nInstalação concluída com sucesso!" -ForegroundColor Green
Write-Host "O serviço está rodando em background." -ForegroundColor White
Write-Host "A interface de bandeja iniciará automaticamente no próximo login." -ForegroundColor White
Write-Host "Para iniciar agora: & '$exePath'" -ForegroundColor Yellow
