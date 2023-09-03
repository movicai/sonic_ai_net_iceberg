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
    using System.Windows.Shapes;
    using RestSharp;

    public class FileDownloader : IDisposable
    {
        private Queue<string> _queue = new Queue<string>();
        public Queue<string> DownloadQueue
        {
            get
            {
                if (_queue == null) _queue = new Queue<string>();
                return _queue;
            }
            set => _queue = value;
        }
        private int _request_timeout = Convert.ToInt32(ConfigurationManager.AppSettings["REST_API_CALL_TIMEOUT"].ToString());

        //private readonly string requestUrl;
        //private string localPath;

        public FileDownloader()
        {
            //_queue = new Queue<string>();
        }
        //public FileDownloader(string requestUrl, string localpath) : this()
        //{
        //    this.requestUrl = requestUrl;
        //    localPath = localpath;
        //}

        public void Dispose()
        {
            _queue = null;
        }

        public async Task DownloadAndQueueAsync(string requestUrl, string localPath)
        {
            var client = new RestClient(requestUrl);
            var request = new RestRequest();
            request.Method = Method.Get;
            request.Timeout = _request_timeout;

            //string localPath = System.IO.Path.Combine(System.Environment.CurrentDirectory,"Content");
            try
            {
                var response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        //await File.WriteAllBytesAsync(_localPath, response.RawBytes);
                        await WriteAllBytesAsync(localPath, response.RawBytes);
                        Console.WriteLine($"File downloaded successfully. - {localPath}");

                        // Adding file path to queue after successful download
                        if (_queue != null && !_queue.Contains(localPath) && File.ReadAllBytes(localPath).Length > 0)
                        {
                            _queue.Enqueue(localPath);
                        }
                        MainWindow.Logger.Info($"File added to queue: {localPath}");
                    }
                    catch (Exception e)
                    {
                        MainWindow.Logger.Error($"Error while saving file: {e.Message}");
                    }
                }
                else
                {
                    MainWindow.Logger.Error($"Error while downloading file: {response.ErrorMessage}");
                }
            }
            catch (Exception e)
            {
                MainWindow.Logger.Error($"{e.Message}", e);
            }
        }

        const int _WAV_FORMAT_POSITION_ = 20;
        const short _PCM_FORMAT_ = 1;
        private async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                int _scnt = 0;
                if (_scnt++ == 0)
                {
                    int _wavformat = BitConverter.ToInt16(bytes, _WAV_FORMAT_POSITION_);
                    //if (_wavformat != 1)
                    //{
                        byte[] _pcmformat = BitConverter.GetBytes(_PCM_FORMAT_);
                        Array.Copy(_pcmformat, 0, bytes, _WAV_FORMAT_POSITION_, _pcmformat.Length);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    //}
                }
                else
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
