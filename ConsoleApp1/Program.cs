using System;
using CS_SMS_LIB;
using System.Diagnostics;


namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("START");
            Debug.WriteLine("==============");

            Debug.WriteLine("==============");
            Debug.WriteLine("Scaner");
            Debug.WriteLine("==============");
            UDPer udp = new UDPer();
            udp.Start();
            Debug.WriteLine("==============");
            Debug.WriteLine("Find Zebra");
            Debug.WriteLine("==============");
            udp.Scan();
            udp.Tables();

            Debug.WriteLine("==============");
            Debug.WriteLine("PLC");
            Debug.WriteLine("==============");
            CModbus md = new CModbus("127.0.0.1", 502);
            md.StartClient();
            //md.startServer();

            bool active = true;
            int chuteid = 1;
            while (active)
            {

                Debug.WriteLine("Press any key to continue . . . ");
                Debug.WriteLine("w : make pid");
                Debug.WriteLine("W : Distribution");
                Debug.WriteLine("p : get pid");
                Debug.WriteLine("r : read distribution state");
                Debug.WriteLine("a : read all");
                Debug.WriteLine("s : scaner start");
                Debug.WriteLine("x : exit");
                var cki = Console.ReadKey(true);
                switch (cki.KeyChar)
                {
                    case 'w':  //make pid
                        md.MakePID(chuteid++);
                        break;
                    case 'W':  //Distribution
                        md.Distribution();
                        break;
                    case 'p':  //get pid
                        Debug.WriteLine(md.m_pid);
                        break;
                    case 'r':
                        md.GetDistribution();
                        break;
                    case 'a':
                        md.PrintAll();
                        break;
                    case 's':
                        udp.StartScaner();
                        foreach (CEA3600 scaner in udp.m_scaner)
                        {
                            scaner.act0 = (name, barcode) =>
                            {
                                Debug.WriteLine("==============");
                                Debug.WriteLine("MAIN");
                                Debug.WriteLine(name);
                                Debug.WriteLine(barcode);
                                Debug.WriteLine("==============");
                                md.MakePID(chuteid++);
                            };
                        }
                        break;
                    case 'x':
                        md.SetActive(false);
                        udp.StopScanner();
                        active = false;
                        break;
                }
            }
            md.Disconnection();
        }
    }
}
