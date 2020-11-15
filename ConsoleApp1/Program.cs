using System;
using CS_SMS_LIB;
using System.Diagnostics;
using Serilog;


namespace ConsoleApp1
{
    class Program
    {
        static public void Test1()
        {
            UDPer udp = new UDPer();
            udp.Start();
            Log.Information("==============");
            Log.Information("Find Zebra");
            Log.Information("==============");
            udp.Scan();
            udp.Tables();

            Log.Information("==============");
            Log.Information("PLC");
            Log.Information("==============");
            CModbus md = new CModbus("127.0.0.1", 502);
            md.StartClient();
            //md.startServer();

            bool active = true;
            int chuteid = 1;
            while (active)
            {

                Log.Information("Press any key to continue . . . ");
                Log.Information("w : make pid");
                Log.Information("W : Distribution");
                Log.Information("p : get pid");
                Log.Information("r : read distribution state");
                Log.Information("a : read all");
                Log.Information("s : scaner start");
                Log.Information("x : exit");
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
                        //Log.Information(md.m_pid);
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
                            scaner.act0 = (job, name, chute_num, barcode) =>
                            {
                                Log.Information("==============");
                                Log.Information("MAIN");
                                Log.Information(job);
                                Log.Information(name);
                                Log.Information(chute_num.ToString());
                                Log.Information(barcode);
                                Log.Information("==============");
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
            banner.m_ip = "172.16.0.100";
            banner.m_port = 5123;
            Log.Information( "Connect : " + banner.Connect());
            banner.m_port = 51236;
            Log.Information( "Connect : " + banner.Connect());
            banner.Start();

        }
        static public void Test3()
        {
            CApi api = new CApi();
            //var p = await api.Print(2);
            api.Cancel(15);
            api.AddGoods(3, "12391283");
            api.AddStatus(1, "+");
            api.AddStatus(3, "+");
            
        }
        static void Main(string[] args)
        {
            //Log.Logger = new LoggerConfiguration().WriteTo.Debug().CreateLogger();
            Log.Logger = new LoggerConfiguration().WriteTo.Debug().CreateLogger();
            Log.Information("==============");
            Log.Information("START");
            Log.Information("==============");

            Log.Information("==============");
            Log.Information("Scaner");
            Log.Information("==============");
            Log.Information("Serilog");
            //Program.Test1();
            //Program.Test2();
            Program.Test3();

            bool active = true;
            while (active)
            {

                Log.Information("Press any key to continue . . . ");
                Log.Information("x : exit");
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
