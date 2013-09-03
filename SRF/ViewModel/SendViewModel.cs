using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Input;

using SRF.Command;

namespace SRF.ViewModel
{

    public class SendViewModel : ViewModelBase
    {

        private Window view;

        private string address = string.Empty;
        private string port = "9999";
        private ObservableCollection<FSItem> items = new ObservableCollection<FSItem>();
        private FSItem selectedItem;

        private ICommand addFileCommand;
        private ICommand addFolderCommand;
        private ICommand removeCommand;
        private ICommand okCommand;

        public SendViewModel(Window window)
        {
            this.view = window;

            foreach (IPAddress item in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (!item.IsIPv6LinkLocal)
                    if (!item.IsIPv6Multicast)
                        if (!item.IsIPv6SiteLocal)
                            if (!item.IsIPv6Teredo)
                                Address = item.ToString();
            }
        }

        public string Address
        {
            get
            {
                return address;
            }
            set
            {
                address = value;
                OnPropertyChanged("Address");
            }
        }

        public string Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
                OnPropertyChanged("Port");
            }
        }

        public ObservableCollection<FSItem> Items
        {
            get
            {
                return items;
            }
        }

        public FSItem SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        public ICommand AddFileCommand
        {
            get
            {
                if (addFileCommand == null)
                    addFileCommand = new RelayCommand((o) =>
                    {
                        Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { Multiselect = true };
                        if (ofd.ShowDialog(view) == true)
                        {
                            foreach (string file in ofd.FileNames)
                            {
                                items.Add(new FSItem(file, FSItemType.File));
                            }
                        }
                    });

                return addFileCommand;
            }
        }

        public ICommand AddFolderCommand
        {
            get
            {
                if (addFolderCommand == null)
                    addFolderCommand = new RelayCommand((o) =>
                    {
                        System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            items.Add(new FSItem(fbd.SelectedPath, FSItemType.Folder));
                        }
                    });

                return addFolderCommand;
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                    removeCommand = new RelayCommand((o) =>
                    {
                        items.Remove(selectedItem);
                    }, (o) =>
                    {
                        if (selectedItem != null)
                            return true;
                        else
                            return false;
                    });

                return removeCommand;
            }
        }

        public ICommand OKCommand
        {
            get
            {
                if (okCommand == null)
                    okCommand = new RelayCommand((o) =>
                    {
                        view.DialogResult = true;
                        view.Close();
                    });

                return okCommand;
            }
        }

    }

}