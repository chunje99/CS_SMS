using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==============");
            Console.WriteLine("START");
            Console.WriteLine("==============");

            Console.WriteLine("==============");
            Console.WriteLine("Scaner");
            Console.WriteLine("==============");
            UDPer udp = new UDPer();
            udp.Start();
            Console.WriteLine("==============");
            Console.WriteLine("Find Zebra");
            Console.WriteLine("==============");
            udp.Scan();
            udp.Tables();

            Console.WriteLine("==============");
            Console.WriteLine("PLC");
            Console.WriteLine("==============");
            CModbus md = new CModbus("127.0.0.1", 502);
            md.StartClient();
            //md.startServer();

            bool active = true;
            while (active)
            {

                Console.WriteLine("Press any key to continue . . . ");
                Console.WriteLine("w : make pid");
                Console.WriteLine("W : Distribution");
                Console.WriteLine("p : get pid");
                Console.WriteLine("r : read distribution state");
                Console.WriteLine("a : read all");
                Console.WriteLine("s : scaner start");
                Console.WriteLine("x : exit");
                var cki = Console.ReadKey(true);
                switch (cki.KeyChar)
                {
                    case 'w':  //make pid
                        md.MakePID();
                        break;
                    case 'W':  //Distribution
                        md.Distribution();
                        break;
                    case 'p':  //get pid
                        md.GetPID();
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
                                Console.WriteLine("==============");
                                Console.WriteLine("MAIN");
                                Console.WriteLine(name);
                                Console.WriteLine(barcode);
                                Console.WriteLine("==============");
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
    }
}
