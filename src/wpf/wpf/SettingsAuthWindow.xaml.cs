using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using wpf.Models;

namespace wpf
{
    public class SettingAuthResultEventArgs : EventArgs
    {
        public bool Result { get; set; }
        public SettingAuthResultEventArgs(bool result)
        {
            Result = result;
        }
    }

    public delegate void SettingAuthEventHandler(object sender, SettingAuthResultEventArgs e);
    /// <summary>
    /// FindDeviceWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsAuthWindow : Window
    {
        public event SettingAuthEventHandler AuthResultEvent;
        //private void OnManualDeviceSave()
        //{
        //    if (AuthResultEvent != null)
        //    {
        //        var model = new DeviceModel()
        //        {
        //            name = txt_name.Text,
        //            ipaddress = txt_ipaddress.Text,
        //            subnet = txt_subnet.Text,
        //        };

        //        AuthResultEvent(this, new SettingAuthResultEventArgs());
        //    }
        //}
        public SettingsAuthWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                this.txt_password.Focus();
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var pwhash = ComputeSha256Hash(this.txt_password.Password.ToString().Trim());
            var apppw = ConfigurationManager.AppSettings["SETTINGS_AUTH_PASSWORD"].ToString();
            var result = apppw.Equals(pwhash);
            if (AuthResultEvent != null)
            {
                AuthResultEvent(this, new SettingAuthResultEventArgs(result));
            }
        }

        /// <summary>
        /// mus.settingauth.console project - hashcode makers
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        protected string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void txt_password_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Button_Click_1(this, null);
            }
        }
    }

}
