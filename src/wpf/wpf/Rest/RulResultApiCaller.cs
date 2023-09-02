using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wpf.Models;

namespace wpf.Rest
{
    public class RulResultApiCaller
    {
        private string _requestUrl;
        public string RequestUrl { get => _requestUrl; }
        NLog.Logger _logger;

        public RulResultApiCaller()
        {
            _requestUrl = ConfigurationManager.AppSettings["PREDICT_SERVER_IP"].ToString();
        }

        public async Task<object> StartAsync(RulModel device)
        {
            var client = new RestClient($"http://{_requestUrl}/lstart");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Content-Type", "application/json");
            //request.AddJsonBody(device);
            request.AddJsonBody(
               new
               {
                   deviceId = device.deviceId,
                   //ipAddress = device.ipAddress
               });
            //string localPath = System.IO.Path.Combine(System.Environment.CurrentDirectory,"Content");
            //bool result = false;
            try
            {

                var response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<List<RulModel>>(response.Content);

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

    }

    public class RulModel
    {
        private string _deviceId;
        private string _ipAddress;
        private double _predict;
        private double _similarity;

        public RulModel()
        {
            
        }
        //private DateTime _startDate;
        //private DateTime _endDate;
        public RulModel(DeviceModel model)
        {
            _deviceId = model.name;
            _ipAddress = model.ipaddress;
        }
        public RulModel(string deviceid, string ipaddress)
        {
            _deviceId = deviceid;
            _ipAddress = ipaddress;
        }
        public string deviceId { get => _deviceId; }
        public string ipAddress { get => _ipAddress; }
        //public DateTime startDate { get => _startDate; }
        //public DateTime endDate { get => _endDate;  }
        public double predict
        {
            get
            {
                return Math.Round(_predict, 2);
            }
            set => _predict = value;
        }
        public double similarity
        {
            get
            {
                return Math.Round(_similarity, 2);
            }
            set => _similarity = value;
        }
    }

}
