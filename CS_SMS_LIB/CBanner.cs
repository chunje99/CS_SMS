using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CS_SMS_LIB
{
    public class CBanner
    {
        public int m_port { get; set; } = 51236;
        public string m_ip { get; set; } = "192.168.0.100";

        private TcpClient tcpClient = null;

        public Action<string> act0 { get; set; } = null;

        public CBanner()
        {
            Debug.WriteLine("CBanner");
            tcpClient = new TcpClient();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                tcpClient.Close();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public int Connect()
        {
            try
            {
                IPAddress ip = IPAddress.Parse(m_ip);
                IPEndPoint ipEndPoint = new IPEndPoint(ip, m_port);
                tcpClient.Connect(ipEndPoint);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return -1;
            }
            return 0;
        }
        public int Close()
        {
            try
            {
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return -1;
            }
            return 0;
        }
        public int Start()
        {
            Debug.WriteLine("Start()");
            Task.Run(() =>
            {
                NetworkStream netStream = tcpClient.GetStream();
                while (netStream.CanRead)
                {
                    try
                    {
                        byte[] bytes = new byte[1024];
                        int readLen = netStream.Read(bytes, 0, 1024);
                        Debug.WriteLine("readLen " + readLen);
                        if (readLen <= 3)
                            continue;
                        if (bytes[0] == 2)
                            Debug.WriteLine("<STX>");
                        else
                        {
                            Debug.WriteLine("<STX> Error");
                            continue;
                        }
                        if (bytes[readLen-2] == 13)
                            Debug.WriteLine("<CR>");
                        else
                        {
                            Debug.WriteLine("<CR> Error");
                            continue;

                        }
                        if (bytes[readLen-1] == 10)
                            Debug.WriteLine("<LF>");
                        else
                        {
                            Debug.WriteLine("<LF> Error");
                            continue;

                        }

                        string retStr = Encoding.UTF8.GetString(bytes,1, readLen-3);
                        if(act0 != null)
                            act0(retStr);
                        Debug.WriteLine(retStr);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            });
            return 0;
        }
    }
}
