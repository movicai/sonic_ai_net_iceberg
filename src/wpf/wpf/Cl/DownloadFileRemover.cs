using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wpf.Properties;

namespace wpf.Cl
{
    public class DownloadFileRemover
    {
        private static DownloadFileRemover _instance;
        public static DownloadFileRemover Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DownloadFileRemover();
                }
                return _instance;
            }
        }

        private static List<string> _downfilequeue = new List<string>();
        public static List<string> DownFileQueue { get => _downfilequeue; }
        System.Timers.Timer _timer = new System.Timers.Timer();
        string _interval;// = Settings.Default["WAV_FILE_REMOVE_INTERVAL"].ToString();
        string _lasttime;// = Settings.Default["WAV_FILE_REMOVE_LASTTIME"].ToString();
        string _filefolder;

        public DownloadFileRemover()
        {
            _interval = ConfigurationManager.AppSettings["WAV_FILE_REMOVE_INTERVAL"].ToString();
            _lasttime = ConfigurationManager.AppSettings["WAV_FILE_REMOVE_LASTTIME"].ToString();
            _filefolder = ConfigurationManager.AppSettings["WAV_FILE_SAVE_FOLDER"].ToString();
        }

        public void Start()
        {
            _timer.Interval = Convert.ToInt32(_interval) * 1000;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }


        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime lastminute = DateTime.Now.AddSeconds(-Convert.ToInt32(_lasttime));
            //var lastitem = _downfilequeue.Last();
            DirectoryInfo dirinfo = new DirectoryInfo(System.IO.Path.Combine(System.Environment.CurrentDirectory, _filefolder));
            var lasttimefiles = dirinfo.GetFiles().Where(c => c.LastAccessTime < lastminute);


            MainWindow.Logger.Info($"DownloadFileRemover Process Start");
            MainWindow.Logger.Info($"Total File Count : {lasttimefiles.Count()}");
            int remove_count = 0;
            foreach (var item in lasttimefiles.ToList())
            {
                try
                {
                    await Task.Run(() => { File.Delete(System.IO.Path.Combine(System.Environment.CurrentDirectory, _filefolder, item.Name)); });
                    remove_count++;
                }
                //catch(IOException ioex)
                //{
                //    MainWindow.Logger.Error(ioex);
                //}
                catch (Exception ex)
                {
                    MainWindow.Logger.Error(ex);
                }
            }
            MainWindow.Logger.Info($"Total Remove File Count: {remove_count}");
            MainWindow.Logger.Info($"DownloadFileRemover Process End.");
        }
    }
}
