using System.Windows;
using System.Windows.Input;

using SRF.Command;

namespace SRF.ViewModel
{

    public class ReceiveViewModel : ViewModelBase
    {

        private Window view;

        private string address = string.Empty;
        private string port = "9999";
        private string folder = string.Empty;

        private ICommand chooseFolderCommand;
        private ICommand okCommand;

        public ReceiveViewModel(Window view)
        {
            this.view = view;
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

        public string Folder
        {
            get
            {
                return folder;
            }
            set
            {
                folder = value;
                OnPropertyChanged("Folder");
            }
        }

        public ICommand ChooseFolderCommand
        {
            get
            {
                if (chooseFolderCommand == null)
                    chooseFolderCommand = new RelayCommand((o) =>
                    {
                        System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            Folder = fbd.SelectedPath;
                        }
                    });
                return chooseFolderCommand;
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