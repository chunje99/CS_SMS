﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleApp1
{
    class ConEA3600
    {
        private string m_ip;
        private int m_port;
        private Socket m_socket;
        private string m_name;
        private bool m_isCon;
        public ConEA3600(string ip, int port, string name)
        {
            m_ip = ip;
            m_port = port;
            m_name = name;
            m_isCon = false;
        }
        public int Con()
        {
            IPAddress ip = IPAddress.Parse(m_ip);
            IPEndPoint bcScanner = new IPEndPoint(ip, m_port);
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
            ProtocolType.Tcp);
            m_socket.Connect(bcScanner);
            m_isCon = true; ;
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
            Console.WriteLine("==============");
            Console.WriteLine("Scaner START {0}", m_name);
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
                            Console.WriteLine("{0} : 프로토콜 에러", m_name);
                            break;
                        }
                        if( bytesRec < 3 )
                        {
                            Console.WriteLine("{0} : size < 3", m_name);
                            break;
                        }
                        int barcodeSize = header[1] - 3;
                        int barcodeType = header[2];
                        Console.WriteLine("{0} : Length {1} Type {2}",
                            m_name, barcodeSize.ToString(), barcodeType.ToString());
                        byte[] body = new byte[barcodeSize];
                        bytesRec = m_socket.Receive(body);
                        string data = Encoding.ASCII.GetString(body, 0, bytesRec);
                        //Console.WriteLine(bytes[0]);
                        Console.WriteLine("{0} : {1}", m_name, data);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            });

            return 0;
        }
    }
    class UDPer
    {
        private readonly int PORT = 11234;
        private readonly int DEVICE_PORT = 12362;
        private UdpClient udpClient = null;
        private Dictionary<string, KeyValuePair<string, int>> m_deviceTable;
        private List<ConEA3600> m_scaner = new List<ConEA3600>();


        public UDPer()
        {
            udpClient = new UdpClient();
            m_deviceTable = new Dictionary<string, KeyValuePair<string, int>>();
            Bind();
        }
        public int Bind()
        {
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
            return 0;
        }
        public int Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var from = new IPEndPoint(0, 0);
                        var recvBuffer = udpClient.Receive(ref from);
                        string MAC = Encoding.UTF8.GetString(recvBuffer);
                        Console.WriteLine(MAC);
                        if (!m_deviceTable.ContainsKey(MAC))
                            m_deviceTable.Add(MAC, new KeyValuePair<string, int>(from.Address.ToString(), 54321));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            });
            return 0;
        }
        public int Scan()
        {
            Console.WriteLine("==============");
            Console.WriteLine("SCAN");
            m_deviceTable.Clear();
            byte[] bytes = Encoding.ASCII.GetBytes("MVP\x0d");
            udpClient.Send(bytes, bytes.Length, "255.255.255.255", DEVICE_PORT);
            return 0;
        }
        public int Tables()
        {
            Console.WriteLine("==============");
            Console.WriteLine("Tables");
            foreach (KeyValuePair<string, KeyValuePair<string, int>> kv in m_deviceTable)
            {
                Console.WriteLine("MAC : {0}, IP : {1}, PORT : {2}", kv.Key, kv.Value.Key, kv.Value.ToString());
            }
            return 0;
        }
        public int StartScaner()
        {
            Console.WriteLine("==============");
            Console.WriteLine("StartScaner");
            foreach (ConEA3600 scaner in m_scaner) //close();
            {
                scaner.Close();
            }
            m_scaner.Clear();
            foreach (KeyValuePair<string, KeyValuePair<string, int>> kv in m_deviceTable)
            {
                m_scaner.Add(new ConEA3600(kv.Value.Key, kv.Value.Value, kv.Key));
            }
            foreach (ConEA3600 scaner in m_scaner) //Con and start
            {
                scaner.Con();
                scaner.Start();
            }
            return 0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==============");
            Console.WriteLine("START");
            Console.WriteLine("==============");

            UDPer udp = new UDPer();
            udp.Start();

            ConsoleKeyInfo cki;
            Console.WriteLine("s : scan, t : into table, x : exit, S : start scaner");
            do
            {
                if (Console.KeyAvailable)
                {
                    cki = Console.ReadKey(true);
                    switch (cki.KeyChar)
                    {
                        case 's':
                            udp.Scan();
                            break;
                        case 't':
                            udp.Tables();
                            break;
                        case 'S':
                            udp.StartScaner();
                            break;
                        case 'x':
                            return;
                    }
                }
                Thread.Sleep(10);
            } while (true);


            //ConEA3600 ea3600 = new ConEA3600("192.168.0.6", 54321);
            //ea3600.Con();
            //ea3600.Read();
        }
    }
}
