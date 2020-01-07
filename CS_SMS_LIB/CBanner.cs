using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using Serilog;

namespace CS_SMS_LIB
{
    public class CBanner
    {
        public int m_port { get; set; } = 51236;
        public string m_ip { get; set; } = "192.168.0.220";
        public bool m_isCon { get; set; } = false;

        private TcpClient tcpClient = null;

        public Action<string> act0 { get; set; } = null;

        public CBanner()
        {
            Log.Information("CBanner");
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
                m_isCon = true;
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
            try
            {
                tcpClient.Close();
                m_isCon = false;
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                return -1;
            }
            return 0;
        }
        public int Start()
        {
            Log.Information("Start()");
            Task.Run(() =>
            {
                NetworkStream netStream = tcpClient.GetStream();
                while (netStream.CanRead)
                {
                    try
                    {
                        byte[] bytes = new byte[1024];
                        int readLen = netStream.Read(bytes, 0, 1024);
                        //Log.Information("readLen " + readLen);
                        if (readLen <= 3)
                            continue;
                        if (bytes[0] != 2)
                        {
                            Log.Information("<STX> Error");
                            continue;
                        }
                        if (bytes[readLen - 2] != 13)
                        {
                            Log.Information("<CR> Error");
                            continue;

                        }
                        if (bytes[readLen - 1] != 10)
                        {
                            Log.Information("<LF> Error");
                            continue;

                        }
                        bool isPrintable = true;
                        for (int i = 1; i < readLen - 2; i++)
                        {
                            if (bytes[i] < 32 || bytes[i] > 126)
                            {
                                isPrintable = false;
                                break;
                            }
                        }
                        if (!isPrintable)
                        {
                            //Log.Information("Printable Error");
                            continue;
                        }

                        string retStr = Encoding.UTF8.GetString(bytes,1, readLen-3);
                        if(act0 != null)
                            act0(retStr);
                        Log.Information(retStr);
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.ToString());
                    }
                }
            });
            return 0;
        }
    }
}
