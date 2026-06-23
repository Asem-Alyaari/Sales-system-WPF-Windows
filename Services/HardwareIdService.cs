using System.Management;

namespace App2.Services
{
    public static class HardwareIdService
    {
        public static string GetHardwareId()
        {
            try
            {
                var hardwareInfo = new List<string>();

                // Get Motherboard Serial Number
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                    foreach (var obj in searcher.Get())
                    {
                        var serial = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(serial))
                            hardwareInfo.Add(serial);
                    }
                }
                catch { }

                // Get Processor ID
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                    foreach (var obj in searcher.Get())
                    {
                        var processorId = obj["ProcessorId"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(processorId))
                            hardwareInfo.Add(processorId);
                    }
                }
                catch { }

                // Get BIOS Serial Number
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
                    foreach (var obj in searcher.Get())
                    {
                        var serial = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(serial))
                            hardwareInfo.Add(serial);
                    }
                }
                catch { }

                // Get MAC Address of first network adapter
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapter WHERE NetEnabled=true");
                    foreach (var obj in searcher.Get())
                    {
                        var mac = obj["MACAddress"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(mac))
                        {
                            hardwareInfo.Add(mac);
                            break; // Only take first active adapter
                        }
                    }
                }
                catch { }

                // Get Volume Serial of C: drive
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='C:'");
                    foreach (var obj in searcher.Get())
                    {
                        var volumeSerial = obj["VolumeSerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(volumeSerial))
                            hardwareInfo.Add(volumeSerial);
                    }
                }
                catch { }

                // Combine all hardware info and hash it
                if (hardwareInfo.Count == 0)
                {
                    // Fallback: use machine name and username
                    hardwareInfo.Add(Environment.MachineName);
                    hardwareInfo.Add(Environment.UserName);
                }

                var combined = string.Join("|", hardwareInfo);
                return ComputeHash(combined);
            }
            catch
            {
                // Ultimate fallback
                return ComputeHash($"{Environment.MachineName}|{Environment.UserName}");
            }
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).Substring(0, 16); // First 16 chars
        }
    }
}
