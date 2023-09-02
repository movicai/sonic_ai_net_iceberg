namespace mus.viewer.network
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Runtime.CompilerServices;
    using RestSharp;
    using wpf;
    using Newtonsoft.Json;
    using wpf.Rest;
    using System.Collections.Generic;
    using System.Configuration;

    public delegate void FindDeviceEventHandler(object sender, FindDeviceEventArgs e);

    public class FindDevice
    {
        private static readonly HttpClient client = new HttpClient();
        //static RestSharp.RestClient RestClient= new RestSharp.RestClient(client);
        public event FindDeviceEventHandler FindDeviceEvent;
        public event FindDeviceEventHandler FindDeviceCompeteEvent;

        private string _ipFrom;
        private string _ipTo;
        private string _subnet;
        private string _deviceInfoUrl;
        private int _api_call_timeout;
        public FindDevice(string[] param)
        {
            _ipFrom = param[0];
            _ipTo = param[1];
            _subnet = param[2];
            _deviceInfoUrl = param[3];

            _api_call_timeout = Convert.ToInt32(ConfigurationManager.AppSettings["FINDDEVICE_API_CALL_TIMEOUT"]);
        }
        public FindDevice(string subnet, string hburi = null)
        {
            this._subnet = subnet;
            if (hburi != null)
            {
                this._deviceInfoUrl = hburi;
            }
            else
            {
                this._deviceInfoUrl = "/deviceinfo/getdeviceinfo";
            }
        }

        private void OnFindDeviceEvent(IPAddress ip, string deviceId)
        {
            if (FindDeviceEvent != null)
            {
                FindDeviceEvent(null, new FindDeviceEventArgs(ip, _subnet, deviceId));
            }
        }
        public async Task StartFindLocal(/*string[] args*/)
        {

            // Get the local IP address and subnet mask
            //var localIP = Dns.GetHostAddresses(_ipFrom).AddressList.FirstOrDefault();
            var localIP = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            var subnetMask = GetSubnetMask(localIP);

            // Calculate the network address
            var network = IPNetwork.Parse(localIP.ToString(), subnetMask.ToString());

            // Get all IP addresses in the same subnet
            var ipAddresses = network.ListIPAddress().ToList();
            // Call the REST API for each IP address
            client.Timeout = TimeSpan.FromMilliseconds(200);
            foreach (var ip in ipAddresses)
            {
                await CallRestApiRestSharp(ip);
            }
        }

        public async Task StartFind()
        {
            try
            {
                // Validate IP addresses
                if (!IPAddress.TryParse(_ipFrom, out var ipFrom) || !IPAddress.TryParse(_ipTo, out var ipTo))
                {
                    throw new ArgumentException("Invalid IP address specified.", nameof(ipFrom));
                }

                // Check if IP addresses are IPv4
                if (ipFrom.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                    ipTo.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    throw new ArgumentException("Only IPv4 addresses are supported.", nameof(ipFrom));
                }

                // Validate that both IP addresses are in the same subnet
                var subnetMask = GetSubnetMask(ipFrom);
                var network = IPNetwork.Parse(ipFrom.ToString(), subnetMask.ToString());
                if (!network.Contains(ipTo))
                {
                    throw new Exception("IP range is not in the same subnet.");
                }

                // Get all IP addresses in the specified range
                var ipFromBytes = ipFrom.GetAddressBytes();
                var ipToBytes = ipTo.GetAddressBytes();
                Array.Reverse(ipFromBytes);  // Convert to big-endian
                Array.Reverse(ipToBytes);  // Convert to big-endian
                var ipFromInt = BitConverter.ToInt32(ipFromBytes, 0);
                var ipToInt = BitConverter.ToInt32(ipToBytes, 0);
                var ipRangeCount = (int)(ipToInt - ipFromInt + 1);

                var ipAddresses = new List<IPAddress>(ipRangeCount);
                for (int i = 0; i < ipRangeCount; ++i)
                {
                    var ipInt = ipFromInt + i;
                    var ipBytes = BitConverter.GetBytes(ipInt);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(ipBytes); // Convert to big-endian if necessary
                    }
                    var ip = new IPAddress(ipBytes);
                    ipAddresses.Add(ip);
                }


                // Call the REST API for each IP address
                //client.Timeout = TimeSpan.FromMilliseconds(1000);
                foreach (var ip in ipAddresses)
                {
                    await CallRestApiRestSharp(ip);
                }
                if (FindDeviceCompeteEvent != null)
                {
                    FindDeviceCompeteEvent(this, null);
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
        }



        IPAddress GetSubnetMask(IPAddress address)
        {
            // Replace with the actual subnet mask
            return IPAddress.Parse(this._subnet);
        }

        //async Task CallRestApi(IPAddress ip)
        //{
        //    try
        //    {
        //        // Replace with the actual REST API endpoint
        //        var response = await client.GetAsync($"http://{ip.ToString()}:8082/{_deviceInfoUrl}");

        //        response.EnsureSuccessStatusCode();
        //        string responseBody = await response.Content.ReadAsStringAsync();

        //        // TODO: Handle the response
        //        //Console.WriteLine(responseBody);
        //        OnFindDeviceEvent(ip);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        async Task CallRestApiRestSharp(IPAddress ip)
        {
            try
            {
                var client = new RestClient($"http://{ip.ToString()}:8082{_deviceInfoUrl}");
                var request = new RestRequest();
                request.Method = Method.Get;
                request.Timeout = _api_call_timeout;

                var response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<DeviceInfoResultJsonData>(response.Content);

                    OnFindDeviceEvent(ip, result.deviceId);
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
        }
    }

    public class DeviceInfoResultJsonData
    {
        public string id { get; set; }
        public string deviceId { get; set; }
    }

}
