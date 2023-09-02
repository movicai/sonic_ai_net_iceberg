using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lc.spec_chart
{
    public delegate void SpectrumOpenFileEventHanlder(object sender, SpectrumOpenFIleEventArgs e);
    public class SpectrumOpenFIleEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public int QCount { get; set; }
        public SpectrumOpenFIleEventArgs(string filepath, int qCount=0)
        {
            this.FilePath = filepath;
            this.QCount = qCount;
        }
    }
}
