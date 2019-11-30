using EasyModbus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class CModbus
    {
        static private int readLen = 902;
        private int[] registers = new int[readLen * 2];
        Queue<KeyValuePair<int, int>> m_changeQueue = new Queue<KeyValuePair<int, int>>();
        ModbusClient m_modbusClient = null;
        private EasyModbus.ModbusServer modbusServer = null;
        private int m_port = 502;
        private string m_host = "127.0.0.1";
        private bool m_active = true;
        private int chuteID = 0;
        private int conCnt = 0;
        private int m_pid = -1;

        public CModbus(string host, int port)
        {
            m_host = host;
            m_port = 502;
            m_active = true;
        }
        public int Connection()
        {
            try
            {
                if (m_modbusClient != null)
                    m_modbusClient.Disconnect();
                m_modbusClient = new ModbusClient(m_host, m_port);
                //modbusClient.UnitIdentifier = 1; Not necessary since default slaveID = 1;
                //modbusClient.Baudrate = 9600;	// Not necessary since default baudrate = 9600
                //modbusClient.Parity = System.IO.Ports.Parity.None;
                //modbusClient.StopBits = System.IO.Ports.StopBits.Two;
                //modbusClient.ConnectionTimeout = 500;			
                m_modbusClient.Connect();
                conCnt++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Thread.Sleep(1000);
                if (m_active)
                    return Connection();
                else
                    return -1;
            }
            return 0;
        }
        public int Disconnection()
        {
            try
            {
                m_modbusClient.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 0;

        }
        async private void ReadArray()
        {
            await Task.Run(async () =>
            {
                while (m_active)
                {
                    try
                    {
                        int limit = 120; ///120 단위로 읽는다
                        if (limit > readLen)
                            limit = readLen;
                        int offset = 0;
                        while (offset < readLen)
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(offset, limit);    //Read 10 Holding Registers from Server, starting with Address 1
                            for (int i = 0; i < r.Length; i++, offset++)
                                if (r[i] != registers[offset])
                                {
                                    registers[offset] = r[i];
                                    m_changeQueue.Enqueue(new KeyValuePair<int, int>(offset, r[i]));
                                    //Console.WriteLine("Value of HoldingRegister " + i + " " + r[i].ToString());
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        Connection();
                    }
                    await Task.Delay(100);
                }
            });
        }
        async private void StartEvent()
        {
            await Task.Run(async () =>
            {
                while (m_active)
                {
                    try
                    {
                        if (m_changeQueue.Count <= 0)
                        {
                            await Task.Delay(100);
                            continue;
                        }
                        var kv = m_changeQueue.Dequeue();
                        Console.WriteLine("Value of HoldingRegister " + kv.Key + " " + kv.Value.ToString());

                        //Get PID
                        if (kv.Key == 900 || kv.Key == 901)
                        {
                            if (GetPID() == 1)
                            {
                                Distribution();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            });
        }
        public int StartClient()
        {
            Connection();
            ReadArray();
            StartEvent();

            return 0;
        }
        public void StartServer()
        {
            modbusServer = new EasyModbus.ModbusServer();
            modbusServer.Port = 502;
            modbusServer.HoldingRegistersChanged += new EasyModbus.ModbusServer.HoldingRegistersChangedHandler(
                HoldingRegistersChanged
                );
            modbusServer.Listen();
            //modbusServer.holdingRegistersChanged += new EasyModbus.ModbusServer.HoldingRegistersChanged(holdingRegistersChanged);
            Console.ReadKey();

            modbusServer.StopListening();
        }

        public void HoldingRegistersChanged(int startingAddress, int quantity)
        {
            Console.WriteLine(startingAddress);
            Console.WriteLine(quantity);
            for (int i = 0; i < quantity; i++)
                Console.WriteLine(modbusServer.holdingRegisters[startingAddress + i]);
        }
        public int MakePID()
        {
            Console.WriteLine("MakePID");
            m_modbusClient.WriteMultipleRegisters(32010, new int[] { 1 });
            return 0;
        }
        public int GetPID()
        {
            Console.WriteLine("GetPID");
            int[] r = m_modbusClient.ReadHoldingRegisters(900, 2);    //Read 10 Holding Registers from Server, starting with Address 1
            int pid = r[1] * 65536 + r[0];
            Console.WriteLine("Value of HoldingRegister: " + pid);
            if (m_pid != pid)
            {
                m_pid = pid;
                Console.WriteLine("Reset 32010");
                m_modbusClient.WriteMultipleRegisters(32010, new int[] { 0 });
                return 1;  /// update
            }
            return 0;
        }
        public int Distribution()
        {
            //todo Get chuteID
            m_modbusClient.WriteMultipleRegisters(32000, new int[] { m_pid % 65536, m_pid / 65536, chuteID, 0 });
            Console.WriteLine("Set chute " + (chuteID + 1));
            chuteID++;
            chuteID = chuteID % 48;
            return 0;
        }
        public int GetDistribution()
        {
            int[] r = m_modbusClient.ReadHoldingRegisters(32000, 4);    //Read 10 Holding Registers from Server, starting with Address 1
            int pid = r[1] * 65536 + r[0];
            int chute = r[3] * 65536 + r[2];
            Console.WriteLine("pid : " + pid + " , cute : " + chute);
            return 0;
        }
        public int PrintAll()
        {
            Console.WriteLine("===================");
            Console.Write("Value of HoldingRegister ");
            for (int i = 0; i < readLen; i++)
            {
                if (i % 10 == 0)
                    Console.WriteLine("");
                Console.Write(registers[i] + ", ");

            }
            Console.WriteLine("");
            Console.WriteLine("===================");
            return 0;
        }
        public void SetActive(bool active)
        {
            m_active = active;
        }
    }
}
