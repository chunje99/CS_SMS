using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CS_SMS_LIB
{
    public class CEA3600 : IDisposable
    {
        string m_ip;
        private int m_port;
        private Socket m_socket;
        public string m_name { get; } = "";
        private bool m_isCon;
        public Action<string, string> act0 = null;
        public Queue<string> m_msgQueue = new Queue<string>();
        public CEA3600(string ip, int port, string name)
        {
            m_ip = ip;
            m_port = port;
            m_name = name;
            m_isCon = false;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                m_socket.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public int Con()
        {
            Debug.WriteLine("IP:{0} PORT:{1}", m_ip, m_port);
            try
            {
                IPAddress ip = IPAddress.Parse(m_ip);
                IPEndPoint bcScanner = new IPEndPoint(ip, m_port);
                m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
                m_socket.Connect(bcScanner);
                m_isCon = true; ;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return -1;
            }
            return 0;
        }
        public int Close()
        {
            m_socket.Close();
            m_isCon = false;
            return 0;
        }
        public int Start()
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("Scaner START {0}", m_name);
            Task.Run(() =>
            {
                // we got the client attempting to connect
                byte[] header = new byte[3];
                int bytesRec;
                try
                {
                    while (m_isCon)
                    {
                        bytesRec = m_socket.Receive(header);
                        if (header[0] != 0)
                        {
                            Debug.WriteLine("{0} : 프로토콜 에러", m_name);
                            break;
                        }
                        if (bytesRec < 3)
                        {
                            Debug.WriteLine("{0} : size < 3", m_name);
                            break;
                        }
                        int barcodeSize = header[1] - 3;
                        int barcodeType = header[2];
                        Debug.WriteLine("{0} : Length {1} Type {2}",
                            m_name, barcodeSize.ToString(), barcodeType.ToString());
                        byte[] body = new byte[barcodeSize];
                        bytesRec = m_socket.Receive(body);
                        string data = Encoding.ASCII.GetString(body, 0, bytesRec);
                        //Debug.WriteLine(bytes[0]);
                        Debug.WriteLine("{0} : {1}", m_name, data);
                        if(act0 != null)
                            act0(m_ip, data);
                        m_msgQueue.Enqueue(data);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            });

            return 0;
        }
    }
    public class UDPer : IDisposable
    {
        private readonly int PORT = 11234;
        private readonly int DEVICE_PORT = 12362;
        private UdpClient udpClient = null;
        public Dictionary<string, KeyValuePair<string, int>> m_deviceTable { get; } = null;
        public List<CEA3600> m_scaner { get;} = new List<CEA3600>();

        public UDPer()
        {
            Debug.WriteLine("UDPer");
            udpClient = new UdpClient();
            Debug.WriteLine("udpClient");
            m_deviceTable = new Dictionary<string, KeyValuePair<string, int>>();
            Debug.WriteLine("m_deviceTalbe");
            Bind();
        }
        public void Print()
        {
            Debug.WriteLine("Print");
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                udpClient.Close();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public int Bind()
        {
            Debug.WriteLine("Bind()");
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
            return 0;
        }
        public int Start()
        {
            Debug.WriteLine("Start()");
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var from = new IPEndPoint(0, 0);
                        var recvBuffer = udpClient.Receive(ref from);
                        string MAC = Encoding.UTF8.GetString(recvBuffer);
                        Debug.WriteLine(MAC);
                        if (!m_deviceTable.ContainsKey(MAC))
                            m_deviceTable.Add(MAC, new KeyValuePair<string, int>(from.Address.ToString(), 54321));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            });
            return 0;
        }
        public int Scan()
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("SCAN");
            m_deviceTable.Clear();
            byte[] bytes = Encoding.ASCII.GetBytes("MVP\x0d");
            udpClient.Send(bytes, bytes.Length, "255.255.255.255", DEVICE_PORT);
            return 0;
        }
        public int Tables()
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("Tables");
            foreach (KeyValuePair<string, KeyValuePair<string, int>> kv in m_deviceTable)
            {
                Debug.WriteLine("MAC : {0}, IP : {1}, PORT : {2}", kv.Key, kv.Value.Key, kv.Value.ToString());
            }
            return 0;
        }
        public int StartScaner()
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("StartScaner");
            StopScanner();
            foreach (KeyValuePair<string, KeyValuePair<string, int>> kv in m_deviceTable)
            {
                m_scaner.Add(new CEA3600(kv.Value.Key, kv.Value.Value, kv.Key));
            }
            foreach (CEA3600 scaner in m_scaner) //Con and start
            {
                scaner.Con();
                scaner.Start();
            }
            return 0;
        }
        public int StartScaner(string host, int port, string name)
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("StartScaner {0} {1}", host, port.ToString());
            CEA3600 cea = new CEA3600(host, port, name);
            m_scaner.Add(cea);
            cea.Con();
            cea.Start();
            return 0;
        }
        public int StopScanner()
        {
            foreach (CEA3600 scaner in m_scaner) //close();
            {
                scaner.Close();
            }
            m_scaner.Clear();
            return 0;
        }
    }
}
