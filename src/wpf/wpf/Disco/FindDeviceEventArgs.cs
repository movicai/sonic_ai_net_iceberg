using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mus.viewer.network
{
    public class FindDeviceEventArgs : EventArgs
    {
        public System.Net.IPAddress DeviceIPAddress { get; set; }
        public string Subnet { get; set; }
        public string DeviceId { get; set; }
        public FindDeviceEventArgs(System.Net.IPAddress deviceIPAddress, string subnet, string deviceId)
        {
            DeviceIPAddress = deviceIPAddress;
            Subnet = subnet;
            DeviceId = deviceId;
        }
    }
}
