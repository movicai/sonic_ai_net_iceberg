namespace mus.viewer.network
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Runtime.CompilerServices;

    public delegate void FindDeviceEventHandler(object sender, FindDeviceEventArgs e);

    public class FindDevice
    {
        private static readonly HttpClient client = new HttpClient();
        //static RestSharp.RestClient RestClient= new RestSharp.RestClient(client);
        public event FindDeviceEventHandler FindDeviceEvent;

        private string IpFrom;
        private string IpTo;
        private string Subnet;
        private string HbUri;
        public FindDevice(string[] param)
        {
            IpFrom = param[0];
            IpTo = param[1];
            Subnet = param[2];
            HbUri = param[3];
        }
        public FindDevice(string subnet, string hburi = null)
        {
            this.Subnet = subnet;
            if (hburi != null)
            {
                this.HbUri = hburi;
            }
            else
            {
                this.HbUri = "/headcheck";
            }
        }

        private void OnFindDeviceEvent(IPAddress ip)
        {
            if (FindDeviceEvent != null)
            {
                FindDeviceEvent(null, new FindDeviceEventArgs(ip, this.Subnet));
            }
        }
        public async Task StartFind(/*string[] args*/)
        {
            
            // Get the local IP address and subnet mask
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
                await CallRestApi(ip);
            }
        }

        IPAddress GetSubnetMask(IPAddress address)
        {
            // Replace with the actual subnet mask
            return IPAddress.Parse(this.Subnet);
        }

        async Task CallRestApi(IPAddress ip)
        {
            try
            {
                // Replace with the actual REST API endpoint

                //var response = await client.GetAsync($"http://{ip.ToString()}{this.HbUri}");

                //response.EnsureSuccessStatusCode();
                //string responseBody = await response.Content.ReadAsStringAsync();

                // TODO: Handle the response
                //Console.WriteLine(responseBody);
                OnFindDeviceEvent(ip);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}
