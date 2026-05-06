# I/O Lockdown Project Instructions

## Critical Safety Mandate
- **NEVER** test this software on the host machine.
- **ALWAYS** use the dedicated Windows 11 Virtual Machine (`io-lockdown-win11-test`) for testing and validation.
- The software manages hardware states (USB, Network) and can lock the host system out of its own peripherals if misconfigured or during a lockdown event.

## Testing Workflow
1. Build the artifacts on the host using `./build.ps1` or `./make-msi.ps1`.
2. Use `./run-in-vm.ps1` to deploy and test inside the VM.
3. Ensure VirtualBox Guest Additions are active in the guest for `guestcontrol` to work.
