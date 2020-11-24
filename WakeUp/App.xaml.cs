using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WakeUp.ViewModel;

namespace WakeUp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public readonly static string ServerUrl = "http://172.30.1.45:5000";
        public static NetworkManager networkManager = new NetworkManager();
        public readonly static ProgramViewModel programViewModel = new ProgramViewModel();
    }
}
