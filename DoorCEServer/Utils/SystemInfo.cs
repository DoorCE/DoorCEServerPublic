using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime;
using System.Runtime.Versioning;
using DoorCEModel.Utils.Extensions;

namespace DoorCEServer.Utils;

public static class SystemInfo
    {
        public static string MachineName => Environment.MachineName;
        public static string NodeId => GetNodeId();

        public static string RuntimeVersion => (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException()).GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName;
        public static string OsNameAndVersion => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public static bool IsServerGC => GCSettings.IsServerGC;
        public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode => GCSettings.LargeObjectHeapCompactionMode;
        public static GCLatencyMode LatencyMode => GCSettings.LatencyMode;
        public static string ContentRootPath => Directory.GetCurrentDirectory();
        public static string? ExecutingAssemblyName => Assembly.GetEntryAssembly()?.GetName().Name;
        private static bool Windows => Environment.OSVersion.Platform == PlatformID.Win32NT;
        private static bool Unix => Environment.OSVersion.Platform == PlatformID.Unix;
        private static bool MacOSX => Environment.OSVersion.Platform == PlatformID.MacOSX;

        private static string? InternalNodeId { get; set; }
        private static DateTimeOffset StartDateTime { get; } = DateTimeOffset.UtcNow;
        public static string UpTime => GetUpTime();

        /// <summary>
        /// Finds the MAC address of the first NIC 
        /// </summary>
        /// <returns>The MAC address</returns>
        private static string GetMacAddress()
        {
            const int minMacAddressLength = 12;

            var macAddress = new string('0', minMacAddressLength);
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            if (nics.Length > 1)
            {
                foreach (NetworkInterface adapter in nics)
                {
                    PhysicalAddress address = adapter.GetPhysicalAddress();
                    if (adapter.OperationalStatus == OperationalStatus.Up &&
                        adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        macAddress = address.ToString();
                        break;
                    }
                }
            }

            return macAddress;
        }

        private static string GetUpTime()
        {
            TimeSpan difference = DateTimeOffset.UtcNow - StartDateTime;
            var days = (int)difference.TotalDays;
            var s = days > 2?"days":"day";
            var dif = difference.Subtract(new TimeSpan(days, 0, 0, 0));
            return $"up {days} {s}, {dif.Hours:D2}:{dif.Minutes:D2}";
        }

        public static string GetNodeId(int id = 0)
        {
            if (string.IsNullOrEmpty(InternalNodeId)) 
            {
                var machineName = id==0?Environment.MachineName:$"{Environment.MachineName}-{id:X6}";
                var machineNameHashCode = machineName.GetDeterministicHashCode();
                var highMachineNameHashCode = (int)(machineNameHashCode >> 32);
                var lowMachineNameHashCode = (int)(machineNameHashCode & ((1L << 32) - 1));
                InternalNodeId =  $"{(int)Environment.OSVersion.Platform:X2}-{GetMacAddress()}-{highMachineNameHashCode:X8}-{lowMachineNameHashCode:X8}";
            }
            return InternalNodeId;
        }
        
        public static string GetIpAddress()
        {
            var ipAddress = "0.0.0.0";

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var address = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                
                if (address != null)
                {
                    ipAddress = address.ToString();
                }
            }
            return ipAddress;
        }
        
        public static string GetHostName()
        {
            return Dns.GetHostName();
        }
       
    }