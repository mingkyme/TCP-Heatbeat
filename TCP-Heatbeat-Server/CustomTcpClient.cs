using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Heatbeat
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

    /// <summary>
    /// TcpClient with heartbeat
    /// </summary>
    class CustomTcpClient : TcpClient
    {
        private IPAddress ip;
        public IPAddress IP
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
            }
        }
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        private int interval;
        private TcpClient client;
        private Task receiveTask;
        private Task timerTask;
        private NetworkStream stream;
        private int timer = 0;

        private object mutex = new object();
        public CustomTcpClient(TcpClient client, int interval)
        {
            IP = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

            this.client = client;
            this.interval = interval;
            stream = client.GetStream();

            timerTask = new Task(TimerStart);
            timerTask.Start();

            receiveTask = new Task(Receive);
            receiveTask.Start();

        }

        public delegate void DisconnectedHandler(CustomTcpClient clientSocket);
        public event DisconnectedHandler OnDisconnected;
        private void CustomSendBufferSize(byte[] bytes)
        {
            stream.WriteByte( (byte)PreMessage.Message); // 보내기 시작함을 나타냄
            stream.WriteByte( (byte)(bytes.Length / 256 / 256) );
            stream.WriteByte( (byte)(bytes.Length / 256) );
            stream.WriteByte( (byte)(bytes.Length % 256) );
        }
        private int GetBufferSize()
        {
            int len = 0;
            len = stream.ReadByte() * 256 * 256;
            len += stream.ReadByte() * 256;
            len += stream.ReadByte() % 256;
            return len;
        }
        private void Send(byte[] bytes)
        {
            try
            {
                CustomSendBufferSize(bytes);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                Disconnection();
            }

        }
        private void Receive()
        {
            while (true)
            {
                switch((PreMessage)stream.ReadByte())
                {
                    case PreMessage.Heatbeat:
                        break;

                    case PreMessage.Message:
                        int len = GetBufferSize();
                        byte[] receivedBytes = new byte[len];
                        string receivedText = Encoding.UTF8.GetString(receivedBytes);
                        Console.WriteLine(receivedText);
                        lock (mutex)
                        {
                            timer = 0;
                        }
                        break;

                    case PreMessage.Close:
                        Disconnection();
                        break;
                }
                
            }
        }
        private void TimerStart()
        {
            while(true)
            {
                Thread.Sleep(1000);
                lock (mutex)
                {
                    if (timer >= 4)
                    {
                        Heatbeat();
                        timer = 0;
                    }
                    timer += 1;
                    Console.WriteLine(timer);
                }
            }
        }
        private void Heatbeat()
        {
            try
            {
                stream.WriteByte(0); // null 값을 보내 생존을 확인
            }
            catch
            {
                Disconnection();
            }
        }
        private void Disconnection()
        {
            OnDisconnected?.Invoke(this);
            timerTask.Dispose();
            receiveTask.Dispose();
            client.Close();
            stream.Close();
        }
    }
}
