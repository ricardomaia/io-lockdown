# I/O Lockdown

[![Deploy GitHub Pages](https://github.com/ricardomaia/io-lockdown/actions/workflows/pages.yml/badge.svg)](https://github.com/ricardomaia/io-lockdown/actions/workflows/pages.yml)

I/O Lockdown is a Windows endpoint security tool designed to protect workstations from physical peripheral attacks and data exfiltration while the system is locked or unattended.

## Defense Objectives & Strategy

The software implements a **Fail-Close** policy to mitigate attack vectors such as BadUSB (Rubber Ducky), hardware Keyloggers, and Rogue Network Adapters.

### 1. Logical Blocking (Locked Session)
Upon detecting a session lock (`Win+L`), the system executes:
- **Network Kill-Switch:** Disables all network adapters (Ethernet/Wi-Fi) via WMI.
- **USB Storage Block:** Disables the `USBSTOR` service in the registry to prevent mounting unauthorized drives.
- **Real-time Whitelisting:** Captures the current state of trusted hardware entities.

### 2. Anti-Tampering Monitoring (Hardware)
While active, the application monitors the PnP (Plug and Play) bus. If any physical change occurs (connection of a new device or removal of an existing one):
- **Violation Detection:** Instantly identifies changes via `WM_DEVICECHANGE` events.
- **Visual Evidence:** Automatically captures a photo of the intruder using the available webcam.
- **Total Lockdown:** Disables all USB controllers (Hubs and Root Controllers) via PowerShell (`Disable-PnpDevice`).
- **Security Persistence:** The "Violation Detected" state prevents automatic re-activation of interfaces upon unlocking.

### 3. Smart Lock (Bluetooth Proximity)
The system can monitor a paired Bluetooth device (e.g., your smartphone). If the device goes out of range, Windows is automatically locked.

## Operation Modes
- **Audit Interface:** A system tray application for real-time log monitoring.
- **Service Mode:** Can run as a Windows Service (`--service`) for system-level protection without requiring user login.

## Build Instructions

### Prerequisites
- **.NET 9.0 SDK**
- **WiX Toolset v5** (for MSI generation)
- **Administrator Privileges** (for hardware manipulation)

### Compilation Steps
1. **Clone the repository:**
   ```bash
   git clone https://github.com/rsmaia/io-lockdown.git
   cd io-lockdown
   ```
2. **Build the executable:**
   ```powershell
   ./build.ps1
   ```
3. **Generate the MSI installer:**
   ```powershell
   ./make-msi.ps1
   ```

## System Requirements
- **OS:** Windows 10/11
- **Privileges:** Run as Administrator.
- **Framework:** .NET 9.0

## Technologies
- **C# / .NET 9.0:** Core logic and UI.
- **WMI & PnP PowerShell:** Low-level hardware state management.
- **Win32 API:** Real-time hardware events and session control.
- **InTheHand.Net:** Bluetooth integration for Smart Lock.
- **UWP MediaCapture:** Violation evidence photography.

---
*Warning: Use with caution. Accidental disconnection of your trusted keyboard during lockdown will result in USB port shutdown, requiring a physical system restart.*
