using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace TCP_Heatbeat
{
    /// <summary>
    /// 매 5초 마다 heatbeat 확인합니다.
    /// </summary>
    public partial class ServerWindow : Window
    {
        #region const region
        private const int INTERVAL = 5;
        private const string IP = "127.0.0.1";
        private const int PORT = 999;
        #endregion const region

        TcpListener listener;
        Task acceptClient;
        ObservableCollection<CustomTcpClient> clients = new ObservableCollection<CustomTcpClient>();
        public ServerWindow()
        {
            InitializeComponent();
            XAML_List.ItemsSource = clients;
            listener = new TcpListener(IPAddress.Any,PORT);
            listener.Start();
            acceptClient = new Task(AcceptClient);
            acceptClient.Start();

        }
        /// <summary>
        /// 접속요청이 오는 클라이언트를 수락합니다.
        /// </summary>
        private void AcceptClient()
        {
            
            while (true)
            {
                var clientTemp = listener.AcceptTcpClient();
                var client = new CustomTcpClient(clientTemp, INTERVAL);
                Dispatcher.Invoke(() =>
                {
                    clients.Add(client);
                    client.OnDisconnected += DisconnectionClient;
                });
                
            }
        }

        /// <summary>
        /// 응답이 없는 클라이언트를 연결 해제합니다.
        /// </summary>
        /// <param name="disconnectedClient">연결이 해제된 Client</param>
        private void DisconnectionClient(CustomTcpClient disconnectedClient)
        {
            if (clients.Contains(disconnectedClient))
            {
                Dispatcher.Invoke(() =>
                {
                    clients.Remove(disconnectedClient);
                    disconnectedClient.Dispose();
                });

            }
        }

    }
}
