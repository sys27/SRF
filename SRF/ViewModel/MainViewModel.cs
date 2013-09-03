using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

using SRF.Command;
using SRF.IO;
using SRF.Resources;
using SRF.View;
using System.ComponentModel;

namespace SRF.ViewModel
{

    public class MainViewModel
    {

        private Server server;
        private XMLFileSystem xmlfs;
        private Window mainView;

        private ICommand sendCommand;
        private ICommand receiveCommand;
        private ICommand exitCommand;

        public MainViewModel(Server server, Window mainView)
        {
            this.server = server;
            this.server.Error += new EventHandler<ServerErrorEventArgs>(server_Error);
            this.mainView = mainView;
        }

        private void server_Error(object sender, ServerErrorEventArgs e)
        {
#if DEBUG
            App.Current.Dispatcher.BeginInvoke(new Action<ServerErrorEventArgs>((args) =>
            {
                MessageBox.Show(mainView, args.Exception.ToString(), Resource.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }), e);
#else
            App.Current.Dispatcher.BeginInvoke(new Action<ServerErrorEventArgs>((args) =>
            {
                MessageBox.Show(mainView, args.Exception.Message, Resource.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }), e);
#endif
        }

        public void ShutdownApplication(object sender, CancelEventArgs args)
        {
            if (server.IsBusy)
            {
                if (MessageBox.Show(mainView, Resource.ExitMessage, Resource.ExitTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    args.Cancel = true;
                }
            }
        }

        private void SendCommand_Execute(object o)
        {
            SendView sendView = new SendView() { Owner = mainView };
            SendViewModel sendViewModel = new SendViewModel(sendView);
            sendView.DataContext = sendViewModel;
            if (sendView.ShowDialog() == true)
            {
                Thread createFSThread = new Thread(() =>
                {
                    xmlfs = new XMLFileSystem();

                    server.Action = Resource.CreateFileSystem;

                    foreach (FSItem fsItem in sendViewModel.Items)
                    {
                        if (fsItem.Type == FSItemType.Folder)
                            xmlfs.AddFolder(fsItem.Path);
                        else
                            xmlfs.AddFile(fsItem.Path);
                    }

                    xmlfs.Save("fs.xml", true);

                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        server.Send(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(sendViewModel.Address), int.Parse(sendViewModel.Port)), xmlfs);
                    }));
                });
                createFSThread.Start();
            }
        }

        private bool SendCommand_CanExecute(object o)
        {
            return !server.IsBusy;
        }

        private void ReceiveCommand_Execute(object o)
        {
            ReceiveView receiveView = new ReceiveView() { Owner = mainView };
            ReceiveViewModel receiveViewModel = new ReceiveViewModel(receiveView);
            receiveView.DataContext = receiveViewModel;
            if (receiveView.ShowDialog() == true)
            {
                server.Receive(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(receiveViewModel.Address), int.Parse(receiveViewModel.Port)), receiveViewModel.Folder);
            }
        }

        private bool ReceiveCommand_CanExecute(object o)
        {
            return !server.IsBusy;
        }

        public Server Server
        {
            get
            {
                return server;
            }
        }

        public ICommand SendCommand
        {
            get
            {
                if (sendCommand == null)
                    sendCommand = new RelayCommand(SendCommand_Execute, SendCommand_CanExecute);
                return sendCommand;
            }
        }

        public ICommand ReceiveCommand
        {
            get
            {
                if (receiveCommand == null)
                    receiveCommand = new RelayCommand(ReceiveCommand_Execute, ReceiveCommand_CanExecute);
                return receiveCommand;
            }
        }

        public ICommand ExitCommand
        {
            get
            {
                if (exitCommand == null)
                    exitCommand = new RelayCommand((o) =>
                    {
                        mainView.Close();
                    });

                return exitCommand;
            }
        }

    }

}