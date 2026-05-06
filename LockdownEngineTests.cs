using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace io_lockdown.Tests
{
    public class MockHardwareController : IHardwareController
    {
        public bool NetworkEnabled { get; private set; } = true;
        public bool UsbHardwareEnabled { get; private set; } = true;
        public bool UsbStorageEnabled { get; private set; } = true;
        public int AlarmPlayedCount { get; private set; } = 0;
        public int PhotoCapturedCount { get; private set; } = 0;
        public int WorkstationLockedCount { get; private set; } = 0;
        public List<string> Devices { get; set; } = new List<string>();

        public void SetNetworkState(bool enable) => NetworkEnabled = enable;
        public void SetUsbHardwareState(bool enable) => UsbHardwareEnabled = enable;
        public void SetUsbStorageState(bool enable) => UsbStorageEnabled = enable;
        public List<string> GetCurrentPnpDevices() => Devices;
        public void PlayAlarm() => AlarmPlayedCount++;
        public Task CapturePhoto() { PhotoCapturedCount++; return Task.CompletedTask; }
        public bool LockWorkStation() { WorkstationLockedCount++; return true; }
    }

    public class LockdownEngineTests
    {
        [Fact]
        public void ResetSystemToSafeState_ShouldEnableAllHardware()
        {
            // Arrange
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            // Act
            engine.ResetSystemToSafeState();

            // Assert
            Assert.True(mock.NetworkEnabled);
            Assert.True(mock.UsbHardwareEnabled);
            Assert.True(mock.UsbStorageEnabled);
            Assert.False(engine.ViolationDetected);
        }

        [Fact]
        public async Task TriggerViolation_ShouldDisableHardwareAndSetFlag()
        {
            // Arrange
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            // Act
            await engine.TriggerViolation("Test Violation");

            // Assert
            Assert.True(engine.ViolationDetected);
            Assert.False(mock.NetworkEnabled);
            Assert.False(mock.UsbHardwareEnabled);
            Assert.Equal(1, mock.AlarmPlayedCount);
            Assert.Equal(1, mock.PhotoCapturedCount);
        }

        [Fact]
        public void IsDeviceAuthorized_ShouldReturnTrueForKnownDevices()
        {
            // Arrange
            var mock = new MockHardwareController();
            mock.Devices = new List<string> { "USB\\VID_123", "PCI\\VEN_456" };
            var engine = new LockdownEngine(mock); // Constructor calls CaptureHardwareWhitelist

            // Act & Assert
            Assert.True(engine.IsDeviceAuthorized("USB\\VID_123"));
            Assert.False(engine.IsDeviceAuthorized("USB\\VID_999"));
        }

        [Fact]
        public async Task SystemUnlock_ShouldRestoreHardware_WhenNoViolation()
        {
            // Arrange
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);
            
            // Simulate Lock
            engine.IsLocked = true;
            mock.SetNetworkState(false);
            mock.SetUsbStorageState(false);

            // Act - Simulate Unlock
            engine.IsLocked = false;
            if (!engine.ViolationDetected)
            {
                engine.ResetSystemToSafeState();
            }

            // Assert
            Assert.True(mock.NetworkEnabled);
            Assert.True(mock.UsbStorageEnabled);
        }
    }
}
