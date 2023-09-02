using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf.Rest
{
    using System;
    using System.IO;
    using System.Text.Json.Nodes;
    using System.Timers;
    using Arction.Wpf.Charting.SeriesXY;
    //using LCWpf;
    using Json.Net;
    using RestSharp;
    using Newtonsoft.Json;
    using System.Configuration;

    public delegate void DetectResultEventHanndler(object sender, DetectResultEventArgs e);

    public class DetectResultEventArgs : EventArgs
    {
        public DetectResultJsonData Data { get; set; }
        public DetectResultEventArgs(DetectResultJsonData data)
        {
            this.Data = data;
        }
    }

    public class DetectResultApiCaller : IDisposable
    {
        NLog.Logger _logger;
        private Timer _timer;
        private Timer _timer_queue;
        private string _apiIp;
        FileDownloader _fileDownloader;
        public string ApiIp { get => _apiIp; set => _apiIp = value; }
        private Queue<string> _chartqueue;
        private string _filename;
        public event DetectResultEventHanndler _detectResultEvent;

        private static DetectResultApiCaller _instance;
        public static DetectResultApiCaller Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DetectResultApiCaller();
                }
                return _instance;
            }
        }
        private int _request_timeout = Convert.ToInt32(ConfigurationManager.AppSettings["REST_API_CALL_TIMEOUT"].ToString());

        public DetectResultApiCaller()
        {
            _fileDownloader = new FileDownloader();
            //_chartqueue = chartqueue;
            _filename = null;
        }

        public DetectResultApiCaller(string apiIp)
        {
            _apiIp = apiIp;
            _fileDownloader = new FileDownloader();
            //_chartqueue = chartqueue;
            _filename = null;
        }

        public void Start()
        {
            //_timer = new Timer(1000);  // Set one second interval
            //_timer.Elapsed += OnTimedEvent;
            //_timer.AutoReset = true;  // Have the timer fire repeated events
            //_timer.Enabled = true;  // Start the timer
            //_timer.Start();

            //_timer_queue = new Timer(1000);
            //_timer_queue.Elapsed += _timer_queue_Elapsed;
            //_timer_queue.Enabled = true;
            //_timer_queue.Start();
        }
        public void Stop()
        {
            _timer.Stop();
            _timer_queue.Stop();
        }

        //private void _timer_queue_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    if (_fileDownloader != null && FileDownloader.DownloadQueue.Count > 0)
        //    {
        //        var filepath = FileDownloader.DownloadQueue.Dequeue();
        //        _chartqueue.Enqueue(filepath);
        //    }
        //}

        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Call();
        }

        public void Dispose()
        {
            if (_fileDownloader != null)
            {
                _fileDownloader.Dispose();
            }
            if (_timer != null)
            {

                _timer.Dispose();
            }
            if (_timer_queue != null)
            {
                _timer_queue.Dispose();

            }

        }
        public async Task<DetectResultJsonData> Call(string ipaddress=null)
        {
            ipaddress = ipaddress == null ? _apiIp : ipaddress;
            var client = new RestClient($"http://{ipaddress}:8082/detectresult/getlastdetectresult");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.Timeout = _request_timeout;
            DetectResultJsonData data = null;
            try
            {

                RestResponse response = await client.ExecuteAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Use response.Content to parse your response data
                    // If JSON, you could use Newtonsoft.Json or System.Text.Json to parse it.
                    Console.WriteLine($"Response: {response.Content}");


                    var result = JsonConvert.DeserializeObject<DetectResultJsonData>(response.Content);
                    if (result != null)
                    {
                        if (_detectResultEvent != null)
                        {
                            _detectResultEvent(this, new DetectResultEventArgs(result));
                        }
                        data = result;
                        //var filename = result.spectrogramFile.Replace(".txt", ".wav");
                        //if (_filename != filename)
                        //{
                        //    var fsurl = $"http://{_apiIp}:8082/files/{filename}";
                        //    string localpath = System.IO.Path.Combine(System.Environment.CurrentDirectory, "Content", filename);
                        //    await _fileDownloader.DownloadAndQueueAsync(fsurl, localpath);
                        //    _filename = filename;
                        //} 

                    }
                    return data;
                }
                else
                {
                    Console.WriteLine($"Error: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error($"{ex.Message}");
            }

            return data;

        }
    }


    public class DetectResultJsonData
    {
        public string id { get; set; }
        public string modelPath { get; set; }
        public string machineSignal { get; set; }
        public string totalErrorRate { get; set; }
        public string noveltyLevel { get; set; }
        public string outlierLevel { get; set; }
        public string machineState { get; set; }
        public string spectrogramFile { get; set; }
        public string time
        {
            get
            {
                string year = spectrogramFile.Substring(0, 4);
                string month = spectrogramFile.Substring(4, 2);
                string day = spectrogramFile.Substring(6, 2);
                string hour = spectrogramFile.Substring(8, 2);
                string minute = spectrogramFile.Substring(10, 2);
                string second = spectrogramFile.Substring(12, 2);
                return $"{year}-{month}-{day} {hour}:{minute}:{second}";
            }
        }
        public string modifieddt { get; set; }

    }
}
