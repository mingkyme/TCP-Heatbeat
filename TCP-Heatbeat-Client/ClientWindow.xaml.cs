using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TCP_Heatbeat_Client
{
    /// <summary>
    /// TCP 통신 맨 처음 붙는 값입니다.
    /// </summary>
    public enum PreMessage
    {
        Heatbeat, // 생존 여부를 확인합니다.
        Message, // 메세지를 보냅니다.
        Close // 정상적으로 종료됨을 알려줍니다.
    };
    public partial class ClientWindow : Window, INotifyPropertyChanged
    {
        #region const region
        private const string IP = "127.0.0.1";
        private const int PORT = 999;

        private const string CONNECTING_MSG = "Connecting to ";
        private const string CONNECTED_MSG = "Connected to ";
        #endregion const region

        private string indicator;
        public string Indicator
        {
            get
            {
                return indicator;
            }
            set
            {
                indicator = value;
                NotifyPropertyChanged(nameof(Indicator));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private TcpClient client;
        private NetworkStream stream;
        private Task connectTask;
        private Task receiveTask;
        private Task sendTask;

        public ClientWindow()
        {
            DataContext = this; // for Binding
            InitializeComponent();
            Indicator = string.Concat(CONNECTING_MSG, IP, ":", PORT);
            connectTask = new Task(Connection);
            connectTask.Start();
            
        }
        private void Connection()
        {
            try
            {
                Indicator = string.Concat(CONNECTING_MSG, IP, ":", PORT);
                if (receiveTask != null)
                {
                    receiveTask.Dispose();
                }
                if(sendTask != null)
                {
                    sendTask.Dispose();
                }

                client = new TcpClient();
                client.Connect(IPAddress.Parse(IP), PORT);
                stream = client.GetStream();
                Indicator = string.Concat(CONNECTED_MSG, IP, ":", PORT);

                receiveTask = new Task(Receive);
                sendTask = new Task(Send);
                receiveTask.Start();
                sendTask.Start();
            }
            catch
            {
                
            }

        }
        private void Receive()
        {
            try
            {
                while (true)
                {
                    PreMessage preMessage = (PreMessage)stream.ReadByte();
                    switch (preMessage)
                    {
                        case PreMessage.Heatbeat:
                            break;

                        case PreMessage.Message:
                            int len = 0;
                            len += stream.ReadByte() * 256 * 256;
                            len += stream.ReadByte() * 256;
                            len += stream.ReadByte() % 256;
                            byte[] receivedBytes = new byte[len];
                            stream.Read(receivedBytes, 0, len);
                            break;

                        case PreMessage.Close:
                            connectTask.Start();
                            break;
                    }
                }
            }
            catch
            {
                connectTask.Start();
            }
        }
        List<byte[]> sendBytes = new List<byte[]>();
        private void Send()
        {
            try
            {
                while(true)
                {
                    if(sendBytes.Count > 0)
                    {
                        stream.WriteByte((byte)PreMessage.Message);
                    }
                }
            }
            catch
            {
                connectTask.Start();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                stream.WriteByte((byte)PreMessage.Close);
            }
            catch
            {

            }
        }
    }
}
