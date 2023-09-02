using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wpf.Models;

namespace wpf.Rest
{
    public class ClassifierResultApiCaller
    {
        private string _requestUrl;
        public string RequestUrl { get => _requestUrl; }
        NLog.Logger _logger;

        public ClassifierResultApiCaller()
        {
            _requestUrl = ConfigurationManager.AppSettings["PREDICT_SERVER_IP"].ToString();
        }

        public async Task<object> StartAsync(ClassifierModel device)
        {
            var client = new RestClient($"http://{_requestUrl}/pstart");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Content-Type", "application/json");
            //request.AddJsonBody(device);
            request.AddJsonBody(
               new
               {
                   deviceId = device.deviceId,
                   //ipAddress = device.ipAddress
                   dataCnt = device.dataCnt,
               });
            //string localPath = System.IO.Path.Combine(System.Environment.CurrentDirectory,"Content");
            //bool result = false;
            try
            {
                var response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<List<ClassifierModel>>(response.Content);
                    //try
                    //{
                    //    MainWindow.Logger.Info($"CollectingDevice Registered - {device.deviceId} : {device.ipAddress}");
                    //}
                    //catch (Exception e)
                    //{
                    //    throw new Exception($"Error while saving file: {e.Message}");
                    //}
                    //result = true;
                    return result;
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
            return null;
        }
        public async Task<object> StopAsync(ClassifierModel device)
        {
            var client = new RestClient($"http://{_requestUrl}/api/");
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

    }
    public class ClassifierModel : INotifyPropertyChanged
    {
        private string _deviceId;
        private string _ipAddress;
        private int _dataCnt = 0;
        private string _name;
        private string _machine_state;

        public event PropertyChangedEventHandler PropertyChanged;

        public ClassifierModel()
        {

        }
        //private DateTime _startDate;
        //private DateTime _endDate;
        public ClassifierModel(DeviceModel model)
        {
            _deviceId = model.name;
            _ipAddress = model.ipaddress;
        }
        public ClassifierModel(string deviceid, int datacnt)
        {
            _deviceId = deviceid;
            //_ipAddress = ipaddress;
            _dataCnt = datacnt;
        }
        public string deviceId { get => _deviceId; }
        public string ipAddress { get => _ipAddress; }
        public int dataCnt { get => _dataCnt; }
        //public DateTime startDate { get => _startDate; }
        //public DateTime endDate { get => _endDate;  }
        public string name { get => _name; set => _name = value; }
        public string machine_state { get => _machine_state; set => _machine_state = value; }
        public string   time
        {
            get
            {
                string year = _name.Substring(0, 4);
                string month = _name.Substring(4, 2);
                string day = _name.Substring(6, 2);
                string hour = _name.Substring(8, 2);
                string minute = _name.Substring(10, 2);
                string second = _name.Substring(12, 2);
                return $"{year}-{month}-{day} {hour}:{minute}:{second}";
            }
        }

    }

}
