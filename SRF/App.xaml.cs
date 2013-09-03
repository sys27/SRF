using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Windows;
using SRF.View;
using SRF.ViewModel;

namespace SRF
{

    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);

            Server server = new Server();

            MainView mainView = new MainView();
            MainViewModel mainViewModel = new MainViewModel(server, mainView);
            mainView.Closing += mainViewModel.ShutdownApplication;
            mainView.DataContext = mainViewModel;
            mainView.Show();
        }

    }

}
