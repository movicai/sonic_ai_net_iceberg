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
    using System.Web.UI.WebControls;
    using wpf.Charts;
    using NLog;
    using SharpDX.DirectWrite;
    using Arction.RenderingDefinitions;

    public class CallEventArgs : EventArgs
    {
        public string WavFileName { get; set; }
        public CallEventArgs(string wavFileName)
        {

            WavFileName = wavFileName;

        }
    }
    public delegate void LastWavResultApiCallerEventHandler(object sender, CallEventArgs e);
    public class LastWavResultApiCaller : IDisposable
    {
        private Timer _timer;
        private Timer _timer_queue;
        private string _apiIp;
        FileDownloader _fileDownloader;
        public string ApiIp { get => _apiIp; set => _apiIp = value; }
        private ChartWavFileQueue _chartqueue;
        private string _filename;
        private string _wavfilesavefolder;
        private bool _isEventHandling = false;
        public event LastWavResultApiCallerEventHandler CallerResultEvent;
        private int _request_timeout = Convert.ToInt32(ConfigurationManager.AppSettings["REST_API_CALL_TIMEOUT"].ToString());
        private bool _debugmode = Convert.ToBoolean(ConfigurationManager.AppSettings["DEBUG_MODE"].ToString());
        private int _request_interval = Convert.ToInt32(ConfigurationManager.AppSettings["WAV_FILE_REQ_INTERVAL"].ToString());

        public LastWavResultApiCaller(string apiIp, ChartWavFileQueue chartqueue)
        {
            _apiIp = apiIp;
            _fileDownloader = new FileDownloader();
            _chartqueue = chartqueue;
            _filename = null;
            _wavfilesavefolder = ConfigurationManager.AppSettings["WAV_FILE_SAVE_FOLDER"].ToString();
        }
        public LastWavResultApiCaller(string apiIp)
        {
            _isEventHandling = true;

            _apiIp = apiIp;
            _fileDownloader = new FileDownloader();
            //_chartqueue = chartqueue;
            _filename = null;
            _wavfilesavefolder = ConfigurationManager.AppSettings["WAV_FILE_SAVE_FOLDER"].ToString();

        }

        public void Start()
        {
            _timer = new Timer(_request_interval);  // Set one second interval
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;  // Have the timer fire repeated events
            _timer.Enabled = true;  // Start the timer
            _timer.Start();

            _timer_queue = new Timer(1000);
            _timer_queue.Elapsed += _timer_queue_Elapsed;
            _timer_queue.AutoReset = true;
            _timer_queue.Enabled = true;
            _timer_queue.Start();
        }

        int[] reqtimecount = { -3, -4, -5 };
        private async void OnTimedEvent_iceburg(object sender, ElapsedEventArgs e)
        {
            try
            {
                var curtime = DateTime.Now;
                foreach (var item in reqtimecount)
                {
                    var task = Task.Run(async () =>
                    {
                        return await WavHeaderReq(curtime, item);
                    });
                    task.Wait();
                    if (task.Result == true)
                    {
                        await Task.Delay(1000);
                        if (_debugmode)
                        {
                            MainWindow.Logger.Info($"WavGetRequest Success : {curtime.ToString()} - {item.ToString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
            finally { }
        }

        public void Stop()
        {
            _timer.Stop();
            _timer_queue.Stop();
        }

        private void _timer_queue_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_fileDownloader != null && _fileDownloader.DownloadQueue.Count > 0)
            {
                var filepath = _fileDownloader.DownloadQueue.Dequeue();
                if (_isEventHandling)
                {
                    if (CallerResultEvent != null)
                    {
                        CallerResultEvent(this, new CallEventArgs(filepath));
                    }
                }
                else if (_chartqueue != null)
                {
                    _chartqueue.Enqueue(filepath);
                }
                wpf.Cl.DownloadFileRemover.DownFileQueue.Add(filepath);
            }
        }

        object _last_async_status = null;
        /// <summary>
        /// [Deprecate]
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            var client = new RestClient($"http://{_apiIp}:8080/lastwav.txt");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.Timeout = _request_timeout;
            try
            {
                RestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Use response.Content to parse your response data
                    // If JSON, you could use Newtonsoft.Json or System.Text.Json to parse it.
                    Console.WriteLine($"Response: {response.Content}");


                    var result = response.Content;//JsonConvert.DeserializeObject<string>(response.Content);
                    if (result != null)
                    {
                        var filename = result.Trim();
                        if (_filename != filename)
                        {
                            var fsurl = $"http://{_apiIp}:8080/wav/{filename}";
                            if (!Directory.Exists(_wavfilesavefolder))
                            {
                                Directory.CreateDirectory(_wavfilesavefolder);
                            }
                            string localpath = System.IO.Path.Combine(_wavfilesavefolder, $"{Guid.NewGuid().ToString()}.wav");
                            Task.Run(async () =>
                            {
                                await _fileDownloader.DownloadAndQueueAsync(fsurl, localpath);
                            });

                            _filename = filename;
                        }
                    }

                }
                else
                {
                    Console.WriteLine($"Error: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex.Message);
            }
        }


        protected async Task<bool> WavHeaderReq(DateTime curtime, int subtime = 0, RestSharp.Method method = Method.Head)
        {
            curtime = curtime.AddSeconds(subtime);
            var curtimestr = curtime.ToString("yyyyMMddHHmmss");
            var filename = $"{curtimestr}.wav";
            var uri = $"http://{_apiIp}:8080/wav/{filename}";
            var client = new RestClient(uri);
            var request = new RestRequest();
            request.Method = method;
            request.Timeout = _request_timeout;

            if (_debugmode)
            {
                MainWindow.Logger.Info($"WavHeaderReq : {uri}");
            }

            try
            {
                RestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (method == Method.Head)
                    {
                        return await WavGetReq(curtime, subtime, Method.Get);
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex.Message);
            }
            return false;
        }
        protected async Task<bool> WavGetReq(DateTime curtime, int subtime = 0, RestSharp.Method method = Method.Get)
        {
            curtime = curtime.AddSeconds(subtime);
            var curtimestr = curtime.ToString("yyyyMMddHHmmss");
            var filename = $"{curtimestr}.wav";
            var uri = $"http://{_apiIp}:8080/wav/{filename}";
            var client = new RestClient(uri);
            var request = new RestRequest();
            request.Method = method;
            request.Timeout = _request_timeout;

            if (_debugmode)
            {
                MainWindow.Logger.Info($"WavHeaderReq : {uri}");
            }

            try
            {
                RestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (_filename != filename)
                    {
                        var fsurl = $"http://{_apiIp}:8080/wav/{filename}";
                        if (!Directory.Exists(_wavfilesavefolder))
                        {
                            Directory.CreateDirectory(_wavfilesavefolder);
                        }
                        string localpath = System.IO.Path.Combine(_wavfilesavefolder, $"{Guid.NewGuid().ToString()}.wav");
                        await _fileDownloader.DownloadAndQueueAsync(fsurl, localpath);
                        _filename = filename;
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine($"Error: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex.Message);
            }
            return false;
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
            if (_chartqueue != null && _chartqueue.Count > 0)
            {
                _chartqueue.Clear();
            }

        }
    }

    public class LastWavResultJsonData
    {
        public string id { get; set; }
        public string filename { get; set; }
        public string filesize { get; set; }
        public string createDate { get; set; }
    }
}
