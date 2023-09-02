using Arction.Wpf.SignalProcessing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace wpf
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set Deployment Key for Arction components
            string deploymentKey = "lgCAAMJ3AmnkpNkBJABVcGRhdGVhYmxlVGlsbD0yMDI0LTA2LTIyI1JldmlzaW9uPTADgD8BTAmQvpi3QkUQxl4d8EhxbP2UGY15OUvfh+OtYByaniOfWeuwEGXpaaYpzUHBHX6Y0M0K89sjG1DqCFr1QsjhD71S/CMe7QUZtBUsQlNfHAyg1Wf7rMSK0fg7VtCh4Xe0uGeKqDGU4UhkeFXcy09rQAKA9AbLQTVK4U3ImF9iN4+qFbWbCQa+YPetCNfA/Z6Qhstm5onWfJG6oo/WCpPhyTGBJJ399nYm4Qi9gMZcDMwkMKNpwj7Y8LobI/WoAv2gQO2wcjYrqBc/3qmBnYD2WHOvH67Vu0Lc1O5taA0Ivp7o5wN0FFla5aak1ydOcawqxYsGPWLoj4E3+nw2v0yKtMGMEYkrc8klkKmLv1NyE5APOLnqwOKCviKNzCupIu67MUM7CVsgQEEktJMIrPyw2nUjXcG34akllM0um8mmD8CtLR13EqgKDoqWpo8eW6NsT9ylJjk/pnWtDQR5x3ZbkjpkAODVYW2HVPj9P36kxEIXcUbVY4tyoACCnWIH9gLr";
            
            // Setting Deployment Key for bindable chart
            //Arction.Wpf.ChartingMVVM.LightningChart.SetDeploymentKey(deploymentKey);
            
            // Setting Deployment Key for non-bindable chart
            Arction.Wpf.Charting.LightningChart.SetDeploymentKey(deploymentKey);
            
            // Setting of deployment key to other Arction components
            SignalGenerator.SetDeploymentKey(deploymentKey); 
            AudioInput.SetDeploymentKey(deploymentKey); 
            AudioOutput.SetDeploymentKey(deploymentKey); 
            SpectrumCalculator.SetDeploymentKey(deploymentKey); 
            SignalReader.SetDeploymentKey(deploymentKey); 
            
            base.OnStartup(e);
        }
    }
}
