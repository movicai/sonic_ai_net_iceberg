using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf.Charts
{
    public class ChartWavFileQueue : Queue<string>, INotifyPropertyChanged
    {
        private string _CurrentWavFile;
        public string CurrentWavFile
        {
            get => _CurrentWavFile; 
            set
            {
                _CurrentWavFile = value;
                OnPropertyChanged(nameof(CurrentWavFile));
            }
        }
        private long _DequeueCount = 0;
        public long DeQueueCount { get=>_DequeueCount; set=>_DequeueCount = value; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ChartWavFileQueue()
        {
        }

        
        new public void Enqueue(string queueName)
        {
            base.Enqueue(queueName);
            this.CurrentWavFile = queueName;
        }

        new public string Dequeue()
        {
            _DequeueCount++;
            return base.Dequeue();
        }
    }
}
