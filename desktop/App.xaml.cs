using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace SimConnectTestWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Started(object sender, EventArgs e)
        {
            FsxData.planeTable = new System.Collections.Hashtable();
            WebServer srv = new WebServer();
            srv.start();
        }        
    }
}
