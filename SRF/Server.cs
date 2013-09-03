using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Xml.Linq;
using SRF.IO;
using SRF.Messages;
using SRF.Resources;

namespace SRF
{

    public class Server : INotifyPropertyChanged
    {

        private IPEndPoint endPoint;
        private Socket socket;
        private const int BUFFER_SIZE = 65536;

        private string action = Resource.WaitingAction;
        private int currentFileCount;
        private int filesCount;

        private string fileName = string.Empty;

        private long currentSize;
        private long currentFullSize;
        private long size;
        private long fullSize;

        private long speed;
        private long old;

        private double currentProgress;
        private double totalProgress;

        private BufferedStream bs;
        private FileStream fs;
        private XMLFileSystem xmlfs;
        private BackgroundWorker bw;

        private Timer timer;

        private bool isBusy = false;

        public event EventHandler<ServerErrorEventArgs> Error;
        public event PropertyChangedEventHandler PropertyChanged;

        public Server()
        {
            timer = new Timer(1000);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnError(Exception e)
        {
            if (Error != null)
                Error(this, new ServerErrorEventArgs(e));
        }

        public void Send(IPEndPoint endPoint, XMLFileSystem xmlfs)
        {
            this.endPoint = endPoint;

            bw = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            bw.DoWork += send_DoWork;
            bw.RunWorkerAsync(xmlfs);
        }

        private void send_DoWork(object o, DoWorkEventArgs args)
        {
            IsBusy = true;
            Socket handler = null;
            SRFMessage message = null;
            xmlfs = null;

            byte[] buf = new byte[BUFFER_SIZE];
            int count = 0;

            try
            {
                xmlfs = (XMLFileSystem)args.Argument;

                FilesCount = xmlfs.FilesCount;
                FullSize = xmlfs.FullSize;

                Action = Resource.BeginSending;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(endPoint);
                socket.Listen(0);

                Action = Resource.WaitConnection;
                handler = socket.Accept();

                message = new SRFMessage() { Command = Commands.Hello };
                handler.Send(message.Message);

                handler.Receive(message.Message);
                if (message.Command != Commands.OK)
                    throw new Exception(Resource.ERROR01);

                while (message.Command != Commands.End)
                {
                    handler.Receive(message.Message);

                    switch (message.Command)
                    {
                        case Commands.FileSystem:
                            string fsPath = "fs.xml.out.gz";
                            FileName = "File System";
                            CurrentSize = 0;
                            CurrentFullSize = message.CurrentFullSize = new FileInfo(fsPath).Length;
                            handler.Send(message.Message);

                            fs = new FileStream(fsPath, FileMode.Open, FileAccess.Read);
                            bs = new BufferedStream(fs);

                            timer.Start();

                            while ((count = bs.Read(buf, 0, buf.Length)) > 0)
                            {
                                handler.Send(buf, 0, count, 0);

                                CurrentSize += count;

                                TotalProgress = CurrentProgress = Math.Round(currentSize * 100d / currentFullSize, 2);
                            }

                            timer.Stop();
                            old = 0;
                            Speed = 0;

                            bs.Close();
                            fs.Close();

                            handler.Receive(message.Message);
                            if (message.Command != Commands.OK)
                                throw new Exception(Resource.ERROR02);

                            break;
                        case Commands.File:
                            XElement el = xmlfs.SearchFile(message.ID);
                            if (el == null)
                            {
                                message = new SRFMessage() { Command = Commands.Error };
                                handler.Send(message.Message);
                                throw new Exception(Resource.ERROR03);
                            }
                            message = new SRFMessage() { Command = Commands.OK };
                            handler.Send(message.Message);

                            count = 0;
                            currentSize = 0;
                            FileName = el.Attribute("name").Value;
                            CurrentFullSize = long.Parse(el.Attribute("size").Value);
                            CurrentProgress = 0;

                            CurrentFileCount++;

                            fs = new FileStream(el.Attribute("cut").Value + el.Attribute("path").Value, FileMode.Open, FileAccess.Read);
                            bs = new BufferedStream(fs);

                            timer.Start();

                            while ((count = bs.Read(buf, 0, buf.Length)) > 0)
                            {
                                handler.Send(buf, 0, count, 0);

                                CurrentSize += count;
                                Size += count;

                                CurrentProgress = Math.Round(currentSize * 100d / currentFullSize, 2);
                                TotalProgress = Math.Round(size * 100d / fullSize, 2);
                            }

                            timer.Stop();
                            old = 0;
                            Speed = 0;

                            bs.Close();
                            fs.Close();

                            handler.Receive(message.Message);
                            if (message.Command != Commands.OK)
                                throw new Exception(Resource.ERROR04);

                            break;
                        case Commands.End:
                            return;
                        default:
                            break;
                    }

                    Action = Resource.EndSending;
                }
            }
            catch (IOException ioe)
            {
                Action = Resource.Error;
                OnError(ioe);
            }
            catch (SocketException se)
            {
                Action = Resource.Error;
                OnError(se);
            }
            finally
            {
                message = null;
                xmlfs = null;

                if (timer.Enabled)
                    timer.Stop();

                if (bs != null)
                {
                    bs.Close();
                    bs = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
                if (handler != null)
                {
                    if (handler.Connected)
                        handler.Disconnect(false);
                    handler.Close();
                    handler = null;
                }
                if (socket != null)
                {
                    if (socket.Connected)
                        socket.Disconnect(false);
                    socket.Close();
                    socket = null;
                }

                Clear();
                IsBusy = false;
            }
        }

        public void Receive(IPEndPoint endPoint, string folder)
        {
            this.endPoint = endPoint;

            bw = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            bw.DoWork += receive_DoWork;
            bw.RunWorkerAsync(folder);
        }

        private void receive_DoWork(object o, DoWorkEventArgs args)
        {
            IsBusy = true;

            var buf = new byte[BUFFER_SIZE];

            try
            {
                var folder = (string)args.Argument;

                Action = Resource.BeginReceiving;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(this.endPoint);

                SRFMessage message = new SRFMessage();
                socket.Receive(message.Message);

                if (message.Command != Commands.Hello)
                    throw new Exception(Resource.ERROR05);

                message = new SRFMessage { Command = Commands.OK };
                socket.Send(message.Message);

                message = new SRFMessage { Command = Commands.FileSystem };
                socket.Send(message.Message);
                socket.Receive(message.Message);
                if (message.Command != Commands.FileSystem)
                    throw new Exception(Resource.ERROR06);

                CurrentFullSize = message.CurrentFullSize;
                CurrentSize = 0;

                fs = new FileStream("fs.xml.gz", FileMode.Create, FileAccess.ReadWrite);
                bs = new BufferedStream(fs);

                timer.Start();

                int count;
                while (currentSize < currentFullSize)
                {
                    count = socket.Receive(buf, 0, buf.Length, 0);

                    bs.Write(buf, 0, count);
                    bs.Flush();

                    CurrentSize += count;

                    TotalProgress = CurrentProgress = Math.Round(currentSize * 100d / currentFullSize, 2);
                }

                timer.Stop();
                old = 0;
                Speed = 0;

                fs.Close();
                bs.Close();

                message = new SRFMessage { Command = Commands.OK };
                socket.Send(message.Message);

                xmlfs = new XMLFileSystem("fs.xml");

                FilesCount = xmlfs.FilesCount;
                FullSize = xmlfs.FullSize;

                xmlfs.PrepareFolders(folder);

                foreach (XElement item in xmlfs.Files.Elements("file"))
                {
                    message = new SRFMessage { Command = Commands.File, ID = UInt32.Parse(item.Attribute("id").Value) };
                    socket.Send(message.Message);
                    socket.Receive(message.Message);
                    if (message.Command != Commands.OK)
                        throw new Exception(Resource.ERROR03);

                    count = 0;
                    FileName = item.Attribute("name").Value;
                    CurrentSize = 0;
                    CurrentFullSize = long.Parse(item.Attribute("size").Value);
                    CurrentProgress = 0;

                    CurrentFileCount++;

                    fs = new FileStream(folder + "\\" + item.Attribute("path").Value, FileMode.CreateNew, FileAccess.Write);
                    bs = new BufferedStream(fs);

                    timer.Start();

                    while (currentSize < currentFullSize)
                    {
                        count = socket.Receive(buf, 0, buf.Length, 0);

                        bs.Write(buf, 0, count);
                        bs.Flush();

                        CurrentSize += count;
                        Size += count;

                        CurrentProgress = Math.Round(currentSize * 100d / currentFullSize, 2);
                        TotalProgress = Math.Round(size * 100d / fullSize, 2);
                    }

                    timer.Stop();
                    old = 0;
                    Speed = 0;

                    bs.Close();
                    fs.Close();

                    message = new SRFMessage() { Command = Commands.OK };
                    socket.Send(message.Message);
                }

                message = new SRFMessage() { Command = Commands.End };
                socket.Send(message.Message);
                Action = Resource.EndReceiving;
            }
            catch (IOException ioe)
            {
                Action = Resource.Error;
                OnError(ioe);
            }
            catch (SocketException se)
            {
                Action = Resource.Error;
                OnError(se);
            }
            finally
            {
                if (timer.Enabled)
                    timer.Stop();

                if (bs != null)
                {
                    bs.Close();
                    bs = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
                if (socket != null)
                {
                    if (socket.Connected)
                        socket.Disconnect(false);
                    socket.Close();
                    socket = null;
                }

                Clear();
                IsBusy = false;
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Speed = currentSize - old;
            old = currentSize;
        }

        private void Clear()
        {
            CurrentFileCount = 0;
            FilesCount = 0;
            FileName = string.Empty;
            CurrentSize = 0;
            CurrentFullSize = 0;
            Size = 0;
            FullSize = 0;
            Speed = 0;
            CurrentProgress = 0;
            TotalProgress = 0;
        }

        public string Action
        {
            get
            {
                return action;
            }
            set
            {
                action = value;
                OnPropertyChanged("Action");
            }
        }

        public int CurrentFileCount
        {
            get
            {
                return currentFileCount;
            }
            set
            {
                currentFileCount = value;
                OnPropertyChanged("CurrentFileCount");
            }
        }

        public int FilesCount
        {
            get
            {
                return filesCount;
            }
            set
            {
                filesCount = value;
                OnPropertyChanged("FilesCount");
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public long CurrentSize
        {
            get
            {
                return currentSize;
            }
            set
            {
                currentSize = value;
                OnPropertyChanged("CurrentSize");
            }
        }

        public long CurrentFullSize
        {
            get
            {
                return currentFullSize;
            }
            set
            {
                currentFullSize = value;
                OnPropertyChanged("CurrentFullSize");
            }
        }

        public long Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                OnPropertyChanged("Size");
            }
        }

        public long FullSize
        {
            get
            {
                return fullSize;
            }
            set
            {
                fullSize = value;
                OnPropertyChanged("FullSize");
            }
        }

        public long Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
                OnPropertyChanged("Speed");
            }
        }

        public double CurrentProgress
        {
            get
            {
                return currentProgress;
            }
            set
            {
                currentProgress = value;
                OnPropertyChanged("CurrentProgress");
            }
        }

        public double TotalProgress
        {
            get
            {
                return totalProgress;
            }
            set
            {
                totalProgress = value;
                OnPropertyChanged("TotalProgress");
            }
        }

        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
            private set
            {
                isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

    }

    public class ServerErrorEventArgs : EventArgs
    {

        public ServerErrorEventArgs() { }

        public ServerErrorEventArgs(Exception e) { Exception = e; }

        public Exception Exception { get; set; }

    }

}