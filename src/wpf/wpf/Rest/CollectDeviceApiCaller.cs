using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf.Rest
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.IO;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows.Shapes;
    using RestSharp;
    using SharpDX.DirectSound;
    using wpf.Models;

    public class CollectDeviceApiCaller : IDisposable
    {
        private Queue<CollectDeviceModel> _queue = new Queue<CollectDeviceModel>();
        public Queue<CollectDeviceModel> DeviceQueue
        {
            get
            {
                if (_queue == null) _queue = new Queue<CollectDeviceModel>();
                return _queue;
            }
            set => _queue = value;
        }

        private string _requestUrl;
        public string RequestUrl { get => _requestUrl; }
        NLog.Logger _logger;
        private Timer _timer;

        private static CollectDeviceApiCaller _instance;

        public static CollectDeviceApiCaller Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CollectDeviceApiCaller();
                }
                return _instance;
            }
            //set { _instance = value; }
        }


        public CollectDeviceApiCaller()
        {
            //_queue = new Queue<string>();
            _requestUrl = ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString();
        }
        //public CollectingDevice(string requestUrl, string localpath) : this()
        //{
        //    this.requestUrl = requestUrl;
        //    localPath = localpath;
        //}

        public void Start()
        {
            _timer = new Timer(10000);  // Set one second interval
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;  // Have the timer fire repeated events
            _timer.Enabled = true;  // Start the timer
            _timer.Start();

            //_timer_queue = new Timer(1000);
            //_timer_queue.Elapsed += _timer_queue_Elapsed;
            //_timer_queue.Enabled = true;
            //_timer_queue.Start();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    _timer.Enabled = false;
                    while (_queue.Count > 0)
                    {
                        await AddDeviceAsync(_queue.Dequeue());
                    }

                }
                catch (Exception ex)
                {

                    MainWindow.Logger.Error(ex);
                }
                finally
                {
                    _timer.Enabled = true;
                }
            });
        }

        public void Stop()
        {
            _timer.Stop();
        }


        public void Dispose()
        {
            _queue = null;
        }

        public async Task<bool> AddDeviceAsync(CollectDeviceModel device)
        {
            var client = new RestClient($"http://{_requestUrl}/api/device/create");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Content-Type", "application/json");
            //request.AddJsonBody(device);
            request.AddJsonBody(
               new
               {
                   deviceId = device.deviceId,
                   ipAddress = device.ipAddress
               });
            //string localPath = System.IO.Path.Combine(System.Environment.CurrentDirectory,"Content");
            bool result = false;
            try
            {

                var response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //try
                    //{
                    //    MainWindow.Logger.Info($"CollectingDevice Registered - {device.deviceId} : {device.ipAddress}");
                    //}
                    //catch (Exception e)
                    //{
                    //    throw new Exception($"Error while saving file: {e.Message}");
                    //}
                    result = true;
                }
                else
                {
                    MainWindow.Logger.Error($"Error while collect device add : {response.ErrorMessage} - {device.deviceId}:{device.ipAddress}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
            return result;
        }
        public async Task<bool> SubDeviceAsync(CollectDeviceModel device)
        {
            var client = new RestClient($"http://{_requestUrl}/api/device/delete");
            var request = new RestRequest();
            request.Method = Method.Delete;
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(
                new
                {
                    deviceId = device.deviceId,
                    ipAddress = device.ipAddress
                });
            //request.AddBody("{deviceId:\"{0}\", ipAddress:\"{1}\"}");
            //string localPath = System.IO.Path.Combine(System.Environment.CurrentDirectory,"Content");
            bool result = false;
            try
            {

                var response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //try
                    //{
                    //    MainWindow.Logger.Info($"CollectingDevice Registered - {device.deviceId} : {device.ipAddress}");
                    //}
                    //catch (Exception e)
                    //{
                    //    throw new Exception($"Error while saving file: {e.Message}");
                    //}
                    result = true;
                }
                else
                {
                    MainWindow.Logger.Error($"Error while collect device remove : {response.ErrorMessage} - {device.deviceId}:{device.ipAddress}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
            return result;

        }

        private async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
    }

    public class CollectDeviceModel
    {
        private string _deviceId;
        private string _ipAddress;
        //private DateTime _startDate;
        //private DateTime _endDate;
        public CollectDeviceModel(DeviceModel model)
        {
            _deviceId = model.name;
            _ipAddress = model.ipaddress;
        }
        public CollectDeviceModel(string deviceid, string ipaddress)
        {
            _deviceId = deviceid;
            _ipAddress = ipaddress;
        }
        public string deviceId { get => _deviceId; }
        public string ipAddress { get => _ipAddress; }
        //public DateTime startDate { get => _startDate; }
        //public DateTime endDate { get => _endDate;  }
    }
}
