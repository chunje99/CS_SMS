using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Serilog;
using System.Threading;

namespace CS_SMS_LIB
{
    public class CEA3600 : IDisposable
    {
        public string m_ip { get; set; }
        private int m_port;
        private Socket m_socket;
        public string m_name { get; } = "";
        public bool m_isCon { get; set; }
        public Action<string, int, string> act0 = null;
        public int m_chute_num { get; set; }

        public CEA3600(string ip, int port, string name)
        {
            m_ip = ip;
            m_port = port;
            m_name = name;
            m_isCon = false;
            m_chute_num = -1;
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
            Log.Information("IP:{0} PORT:{1}", m_ip, m_port);
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
                Log.Information(e.ToString());
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
            Log.Information("==============");
            Log.Information("Scaner START {0}", m_name);
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
                        Log.Information("{0} : Length {1}", m_name, bytesRec);
                        if (header[0] != 0)
                        {
                            Log.Information("{0} : 프로토콜 에러", m_name);
                            break;
                        }
                        if (bytesRec < 3)
                        {
                            Log.Information("{0} : size < 3  header len = {1}", m_name, header[1]);
                            break;
                        }
                        int barcodeSize = header[1] - 3;
                        int barcodeType = header[2];
                        Log.Information("{0} : Length {1} Type {2}",
                            m_name, barcodeSize.ToString(), barcodeType.ToString());
                        if( barcodeSize <= 0 )
                            continue;
                        byte[] body = new byte[barcodeSize];
                        bytesRec = m_socket.Receive(body);
                        string data = Encoding.ASCII.GetString(body, 0, bytesRec);
                        //Log.Information(bytes[0]);
                        Log.Information("{0} : {1}", m_name, data);
                        if( barcodeType == 28 ) // qr code - chute 분배
                        {
                            string prefix = data.Substring(0, 6);
                            if( prefix == "CHUTE_")
                            {
                                m_chute_num = Int32.Parse(data.Substring(6, data.Length - 6));
                                Log.Information("스캐너 할당 " + m_name + " : " + m_chute_num.ToString());
                            }
                            else
                            {
                                Log.Information("QR code error :" + data);
                            }
                        }
                        else //if( barcodeType == 3 ) // code128
                        {
                            if (act0 != null)
                                act0(m_name, m_chute_num, data);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Information(e.ToString());
                }
            });

            return 0;
        }
    }
    public class UDPer : IDisposable
    {
        private readonly int PORT = 11234;
        private readonly int DEVICE_PORT = 12362;
        //private UdpClient udpClient = null;
        private List<UdpClient> udpClients = new List<UdpClient>();
        public Dictionary<string, KeyValuePair<string, int>> m_deviceTable { get; } = null;
        public List<CEA3600> m_scaner { get;} = new List<CEA3600>();
        static Mutex m_mutex = new Mutex();

        public UDPer()
        {
            Log.Information("UDPer");
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    try
                    {
                        Log.Information("new IP : " + ip.ToString());
                        var ServerEp = new IPEndPoint(ip, 0);
                        udpClients.Add(new UdpClient(ServerEp));
                    }
                    catch (Exception e)
                    {
                        Log.Information("unable to connect.");
                        Log.Information(e.ToString());
                    }
                }
            }


            //udpClient = new UdpClient();
            Log.Information("udpClient");
            m_deviceTable = new Dictionary<string, KeyValuePair<string, int>>();
            Log.Information("Device {@Dictionary<string,KeyValuePair<string,int>>}", m_deviceTable);
            //Bind();
        }
        public void Print()
        {
            Log.Information("Print");
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                //udpClient.Close();
                foreach (var cli in udpClients)
                {
                    cli.Close();
                }
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
            Log.Information("Bind()");
            //udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
            foreach(var cli in udpClients)
            {
                cli.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
            }
            return 0;
        }
        public int Start()
        {
            Log.Information("Start()");
            /*
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var from = new IPEndPoint(0, 0);
                        var recvBuffer = udpClient.Receive(ref from);
                        string MAC = Encoding.UTF8.GetString(recvBuffer);
                        Log.Information(MAC);
                        if (!m_deviceTable.ContainsKey(MAC))
                            m_deviceTable.Add(MAC, new KeyValuePair<string, int>(from.Address.ToString(), 54321));
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.ToString());
                    }
                }
            });
            */
            foreach(var cli in udpClients)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var from = new IPEndPoint(0, 0);
                            var recvBuffer = cli.Receive(ref from);
                            string MAC = Encoding.UTF8.GetString(recvBuffer);
                            Log.Information(MAC);
                            m_mutex.WaitOne();
                            if (!m_deviceTable.ContainsKey(MAC))
                                m_deviceTable.Add(MAC, new KeyValuePair<string, int>(from.Address.ToString(), 54321));
                            m_mutex.ReleaseMutex();
                        }
                        catch (Exception e)
                        {
                            Log.Information(e.ToString());
                        }
                    }
                });
            }
            return 0;
        }
        public int Scan()
        {
            Log.Information("==============");
            Log.Information("SCAN");
            m_deviceTable.Clear();
            byte[] bytes = Encoding.ASCII.GetBytes("MVP\x0d");
            //udpClient.Send(bytes, bytes.Length, "255.255.255.255", DEVICE_PORT);
            foreach(var cli in udpClients)
            {

                cli.Send(bytes, bytes.Length, "255.255.255.255", DEVICE_PORT);
            }
            
            return 0;
        }
        public int Tables()
        {
            Log.Information("==============");
            Log.Information("Tables");
            foreach (KeyValuePair<string, KeyValuePair<string, int>> kv in m_deviceTable)
            {
                Log.Information("MAC : {0}, IP : {1}, PORT : {2}", kv.Key, kv.Value.Key, kv.Value.ToString());
            }
            return 0;
        }
        public int StartScaner()
        {
            Log.Information("==============");
            Log.Information("StartScaner");
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
            Log.Information("==============");
            Log.Information("StartScaner {0} {1}", host, port.ToString());
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
