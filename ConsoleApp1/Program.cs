using System;
using CS_SMS_LIB;
using System.Diagnostics;


namespace ConsoleApp1
{
    class Program
    {
        static public void Test1()
        {
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
                        md.MakePID();
                        break;
                    case 'W':  //Distribution
                        md.Distribution(chuteid++);
                        break;
                    case 'p':  //get pid
                        //Debug.WriteLine(md.m_pid);
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
                            scaner.act0 = (name, chute_num, barcode) =>
                            {
                                Debug.WriteLine("==============");
                                Debug.WriteLine("MAIN");
                                Debug.WriteLine(name);
                                Debug.WriteLine(chute_num);
                                Debug.WriteLine(barcode);
                                Debug.WriteLine("==============");
                                md.MakePID();
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
        static void Test2()
        {
            CBanner banner = new CBanner();
            banner.m_ip = "192.168.0.100";
            banner.m_port = 5123;
            Debug.WriteLine( "Connect : " + banner.Connect());
            banner.m_port = 51236;
            Debug.WriteLine( "Connect : " + banner.Connect());
            banner.Start();

        }
        static public void Test3()
        {
            CApi api = new CApi();
            var p = api.GetChute("asdfsf");
            
        }
        static void Main(string[] args)
        {
            Debug.WriteLine("==============");
            Debug.WriteLine("START");
            Debug.WriteLine("==============");

            Debug.WriteLine("==============");
            Debug.WriteLine("Scaner");
            Debug.WriteLine("==============");
            //Program.Test1();
            //Program.Test2();
            Program.Test3();

            bool active = true;
            while (active)
            {

                Debug.WriteLine("Press any key to continue . . . ");
                Debug.WriteLine("x : exit");
                var cki = Console.ReadKey(true);
                switch (cki.KeyChar)
                {
                    case 'x':
                        active = false;
                        break;
                }
            }
        }

    }
}
