param(
    [string]$VmName = "io-lockdown-win11-test",
    [string]$Username = "iolab",
    [string]$Password = "ChangeMe!2026",
    [string]$ArtifactPath,
    [string]$GuestDir = "C:\Users\Public\IOLockdown",
    [ValidateSet("Auto", "InstallMsi", "RunExe")]
    [string]$Mode = "Auto",
    [string[]]$RunArguments = @(),
    [int]$TimeoutMs = 600000
)

$ErrorActionPreference = "Stop"

$VBoxManage = "C:\Program Files\Oracle\VirtualBox\VBoxManage.exe"
if (!(Test-Path $VBoxManage)) {
    throw "VBoxManage.exe not found at '$VBoxManage'. Install Oracle VirtualBox or update this script."
}

if ([string]::IsNullOrWhiteSpace($ArtifactPath)) {
    $msi = Join-Path $PSScriptRoot "IOLockdown_Installer.msi"
    $exe = Join-Path $PSScriptRoot "publish\io-lockdown.exe"

    if (Test-Path $msi) {
        $ArtifactPath = $msi
    } elseif (Test-Path $exe) {
        $ArtifactPath = $exe
    } else {
        throw "No artifact found. Run './make-msi.ps1' or './build.ps1' first, or pass -ArtifactPath."
    }
}

$ArtifactPath = (Resolve-Path $ArtifactPath).Path
$artifactName = Split-Path $ArtifactPath -Leaf
$guestArtifact = "$GuestDir\$artifactName"
$passwordFile = Join-Path $env:TEMP "io-lockdown-vbox-$VmName.pwd"

try {
    Set-Content -LiteralPath $passwordFile -Value $Password -NoNewline

    Write-Host "Waiting for guest desktop/runlevel..." -ForegroundColor Cyan
    & $VBoxManage guestcontrol $VmName waitrunlevel --timeout $TimeoutMs desktop
    if ($LASTEXITCODE -ne 0) {
        throw "VM '$VmName' is not ready for guest control. Confirm Windows is installed, running, logged in, and Guest Additions are active."
    }

    Write-Host "Stopping existing instances in guest..." -ForegroundColor Cyan
    try {
        & $VBoxManage guestcontrol $VmName run --username $Username --passwordfile $passwordFile --exe "C:\Windows\System32\taskkill.exe" -- /F /IM io-lockdown.exe /T
    } catch {
        # Ignore errors if process not found
    }
Write-Host "Copying '$artifactName' to VM..." -ForegroundColor Cyan
& $VBoxManage guestcontrol $VmName copyto --username $Username --passwordfile $passwordFile $ArtifactPath "C:\Users\Public\"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to copy artifact to guest."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$uniqueGuestDir = "$GuestDir\_$timestamp"

Write-Host "Moving artifact to: $uniqueGuestDir" -ForegroundColor Cyan
& $VBoxManage guestcontrol $VmName mkdir --parents --username $Username --passwordfile $passwordFile $uniqueGuestDir
& $VBoxManage guestcontrol $VmName run --username $Username --passwordfile $passwordFile --exe "C:\Windows\System32\cmd.exe" -- /c "move /y C:\Users\Public\$artifactName $uniqueGuestDir\"

if ($Mode -eq "Auto") {
    if ([IO.Path]::GetExtension($ArtifactPath) -ieq ".msi") {
        $Mode = "InstallMsi"
    } else {
        $Mode = "RunExe"
    }
}

    $guestArtifact = "$uniqueGuestDir\$artifactName"

    if ($Mode -eq "InstallMsi") {
        $guestLog = "$uniqueGuestDir\install.log"
        Write-Host "Installing MSI inside VM..." -ForegroundColor Cyan
        & $VBoxManage guestcontrol $VmName run `
            --username $Username `
            --passwordfile $passwordFile `
            --exe "C:\Windows\System32\msiexec.exe" `
            --timeout $TimeoutMs `
            --wait-stdout `
            --wait-stderr `
            -- `
            /i $guestArtifact /qn /norestart ALLUSERS=1 MSIINSTALLPERUSER="" /L*v $guestLog

        if ($LASTEXITCODE -ne 0) {
            throw "MSI installation failed in guest. Check '$guestLog' inside the VM."
        }

        Write-Host "MSI installed. Log: $guestLog" -ForegroundColor Green
        return
    }

    Write-Host "Starting EXE inside VM..." -ForegroundColor Cyan
    & $VBoxManage guestcontrol $VmName start `
        --username $Username `
        --passwordfile $passwordFile `
        --exe $guestArtifact `
        --cwd $uniqueGuestDir `
        -- `
        @RunArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start '$guestArtifact' in guest. If UAC is shown, open the VM GUI and approve it."
    }

    Write-Host "Program started in VM: $guestArtifact" -ForegroundColor Green
}
finally {
    if (Test-Path $passwordFile) {
        Remove-Item -LiteralPath $passwordFile -Force
    }
}
