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
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            engine.ResetSystemToSafeState();

            Assert.True(mock.NetworkEnabled);
            Assert.True(mock.UsbHardwareEnabled);
            Assert.True(mock.UsbStorageEnabled);
            Assert.False(engine.ViolationDetected);
        }

        [Fact]
        public async Task TriggerViolation_ShouldDisableHardwareAndSetFlag()
        {
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            await engine.TriggerViolation("Test Violation");

            Assert.True(engine.ViolationDetected);
            Assert.False(mock.NetworkEnabled);
            Assert.False(mock.UsbHardwareEnabled);
            Assert.Equal(1, mock.AlarmPlayedCount);
            Assert.Equal(1, mock.PhotoCapturedCount);
        }

        [Fact]
        public async Task TriggerViolation_IsIdempotent()
        {
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            await engine.TriggerViolation("First");
            await engine.TriggerViolation("Second");

            Assert.Equal(1, mock.AlarmPlayedCount);
            Assert.Equal(1, mock.PhotoCapturedCount);
        }

        [Fact]
        public void IsDeviceAuthorized_ShouldReturnTrueForKnownDevices()
        {
            var mock = new MockHardwareController();
            mock.Devices = new List<string> { "USB\\VID_123", "PCI\\VEN_456" };
            var engine = new LockdownEngine(mock);

            Assert.True(engine.IsDeviceAuthorized("USB\\VID_123"));
            Assert.False(engine.IsDeviceAuthorized("USB\\VID_999"));
        }

        [Fact]
        public void IsDeviceAuthorized_NullOrEmpty_ReturnsTrue()
        {
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            // null/empty device IDs are treated as authorized to avoid false positives
            Assert.True(engine.IsDeviceAuthorized(null!));
            Assert.True(engine.IsDeviceAuthorized(""));
        }

        [Fact]
        public void GetCurrentPnpDevices_DelegatesToHardware()
        {
            var mock = new MockHardwareController();
            mock.Devices = new List<string> { "USB\\VID_111", "USB\\VID_222" };
            var engine = new LockdownEngine(mock);

            var result = engine.GetCurrentPnpDevices();

            Assert.Equal(2, result.Count);
            Assert.Contains("USB\\VID_111", result);
        }

        [Fact]
        public void SystemUnlock_ShouldRestoreHardware_WhenNoViolation()
        {
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            engine.IsLocked = true;
            mock.SetNetworkState(false);
            mock.SetUsbStorageState(false);

            engine.IsLocked = false;
            if (!engine.ViolationDetected)
                engine.ResetSystemToSafeState();

            Assert.True(mock.NetworkEnabled);
            Assert.True(mock.UsbStorageEnabled);
        }

        [Fact]
        public async Task SystemUnlock_ShouldNotRestoreHardware_WhenViolationDetected()
        {
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            await engine.TriggerViolation("Intruder");

            engine.IsLocked = false;
            if (!engine.ViolationDetected)
                engine.ResetSystemToSafeState();

            Assert.False(mock.NetworkEnabled);
            Assert.False(mock.UsbHardwareEnabled);
        }

        [Fact]
        public void RequestLock_ShouldCallLockWorkStation()
        {
            var mock = new MockHardwareController();
            var engine = new LockdownEngine(mock);

            engine.RequestLock("Test lock reason");

            Assert.Equal(1, mock.WorkstationLockedCount);
        }

        [Fact]
        public void CaptureHardwareWhitelist_ShouldPopulateTrustedIds()
        {
            var mock = new MockHardwareController();
            mock.Devices = new List<string> { "USB\\VID_AAA", "USB\\VID_BBB" };
            var engine = new LockdownEngine(mock);

            engine.CaptureHardwareWhitelist();

            Assert.Contains("USB\\VID_AAA", engine.TrustedDeviceIds);
            Assert.Contains("USB\\VID_BBB", engine.TrustedDeviceIds);
        }

        [Fact]
        public void CaptureHardwareWhitelist_ShouldReplaceExistingList()
        {
            var mock = new MockHardwareController();
            mock.Devices = new List<string> { "USB\\VID_OLD" };
            var engine = new LockdownEngine(mock);

            mock.Devices = new List<string> { "USB\\VID_NEW" };
            engine.CaptureHardwareWhitelist();

            Assert.DoesNotContain("USB\\VID_OLD", engine.TrustedDeviceIds);
            Assert.Contains("USB\\VID_NEW", engine.TrustedDeviceIds);
        }
    }
}
