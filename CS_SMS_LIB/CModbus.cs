using EasyModbus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CS_SMS_LIB
{
    public enum MDS_EVENT
    {
	    PID       = 0,
	    ALARM     = 1,
	    FULL      = 2,
	    CONFIRM   = 3,
	    CONFIRM_STACK = 4,
	    CONFIRM_PID   = 5,
	    PRINT     = 6,
	    PLUS      = 7,
	    MINUS     = 8,
    }
    public class CModbus
    {
        static private int readLen = 902;
        public int[] registers { get; } = new int[readLen * 2];
        public MDSData mdsData {get;} = new MDSData();
        private Queue<KeyValuePair<int, int>> m_changeQueue = new Queue<KeyValuePair<int, int>>();
        private ModbusClient m_modbusClient = null;
        private EasyModbus.ModbusServer modbusServer = null;
        public int m_port { get; set; } = 502;
        public string m_host { get; set; } = "192.168.0.1";
        private bool m_active  = true;
        public int m_chuteID { get; set; } = 0;
        private int conCnt = 0;
        public Action<int, int> act0 = null;
        /// <summary>
        /// eventid, id0, id1, id2
        /// </summary>
        public Action<MDS_EVENT, int, int, int> onEvent = null;
        public bool m_dist { get; set; } = true;
        public string m_error { get; set; } = "";

        public CModbus()
        {
            m_host = "192.168.0.1";
            m_port = 502;
            m_active = true;
        }
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
                Debug.WriteLine(e.ToString());
                m_error = e.ToString();
                Thread.Sleep(1000);
                //if (m_active)
                //    return Connection();
                //else
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
                Debug.WriteLine(e.ToString());
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
                            //Debug.WriteLine("Read Register {0} , {1}", offset.ToString(), limit.ToString());
                            var r = m_modbusClient.ReadHoldingRegisters(offset, limit);    //Read 10 Holding Registers from Server, starting with Address 1
                            for (int i = 0; i < r.Length; i++, offset++)
                                if (r[i] != registers[offset])
                                {
                                    registers[offset] = r[i];
                                    m_changeQueue.Enqueue(new KeyValuePair<int, int>(offset, r[i]));
                                    //Debug.WriteLine("Value of HoldingRegister " + i + " " + r[i].ToString());
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        Connection();
                    }
                    await Task.Delay(100);
                }
            });
        }
        async private void ReadMDS()
        {
            await Task.Run(async () =>
            {
                while (m_active)
                {
                    try
                    {
                        //READ < 80
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(0, 80);    //Read 10 Holding Registers from Server, starting with Address 1
                            if( r.Length != 80 )
                            {
                                Debug.WriteLine("Read Error < 79 array" );
                                break;
                            }
                            mdsData.moduleCnt = r[1];
                            mdsData.heartBest = r[2];
                            ///status
                            for (int i = 0; i < 12; i++)
                                mdsData.moduleInfos[i].status = r[i+3];
                            mdsData.settingData.moduleSpeed = r[15];
                            for (int i = 0; i < 3; i++)
                                mdsData.settingData.pointSpeed[i] = r[i + 16];
                            mdsData.currentModuleSpeed = r[19];
                            for( int i = 0; i < 12; i++)
                            {
                                mdsData.moduleInfos[i].alarm = r[i * 5 + 20];
                                for (int j = 0; j < 4; j++)
                                {
                                    mdsData.moduleInfos[i].chuteInfos[j].full = r[i * 5 + j + 21];
                                }

                            }
                        }
                        //READ < confirm word data 176
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(80, 96);    //Read 10 Holding Registers from Server, starting with Address 1
                            if( r.Length != 96 )
                            {
                                Debug.WriteLine("Read Error < 176 array" );
                                break;
                            }
                            for(int i = 0 ; i < 12 ; i++ ) //module
                            {
                                for (int j = 0; j < 4; j++) //chute
                                {
                                    mdsData.moduleInfos[i].chuteInfos[j].confirmData = r[i*8 + j*2 ];
                                    mdsData.moduleInfos[i].chuteInfos[j].stackCount = r[i*8 +  j*2 + 1];
                                }
                            }
                        }
                        //READ < pid + confirm dword data 176
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(500, 98);    //Read 10 Holding Registers from Server, starting with Address 1
                            if( r.Length != 98 )
                            {
                                Debug.WriteLine("Read Error < confirm dword" );
                                break;
                            }

                            int pid = r[0] + r[1] * 65536;
                            if(pid != mdsData.pid)
                            {
                                mdsData.pid = pid;
                                Debug.WriteLine("Reset 32010");
                                m_modbusClient.WriteMultipleRegisters(32010, new int[] { 0 });

                                if (onEvent != null)
                                    onEvent(MDS_EVENT.PID, pid, 0, 0);
                            }

                            for (int i = 0 ; i < 12 ; i++ ) //module
                            {
                                for (int j = 0; j < 4; j++) //chute
                                {
                                    int num = r[(i * 4 + j)*2 + 2] + r[(i * 4 + j)*2 + 3] * 65536;
                                    mdsData.moduleInfos[i].chuteInfos[j].pidNum = num;
                                }
                            }
                        }
                        //READ print input
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(176, 120);    //Read 10 Holding Registers from Server, starting with Address 1
                            if( r.Length != 120 )
                            {
                                Debug.WriteLine("Read Error < 176 array" );
                                break;
                            }
                            int leftChute, rightChute, printButton, plusButton, minusButton;
                            for(int i = 0 ; i < 12 ; i++ ) //module
                            {
                                for (int j = 0; j < 2; j++) //chute
                                {
                                    leftChute = r[i * 10 + j*5];
                                    rightChute = r[i * 10 + j*5 + 1];
                                    printButton = r[i * 10 + j*5 + 2];
                                    plusButton = r[i * 10 + j*5 + 3];
                                    minusButton = r[i * 10 + j*5 + 4];

                                    if (leftChute != mdsData.moduleInfos[i].printInfos[j].leftChute)
                                    {
                                        mdsData.moduleInfos[i].printInfos[j].leftChute = leftChute;
                                    }

                                    if (rightChute != mdsData.moduleInfos[i].printInfos[j].rightChute)
                                        mdsData.moduleInfos[i].printInfos[j].rightChute = rightChute;

                                    if (printButton != mdsData.moduleInfos[i].printInfos[j].printButton)
                                    {

                                        mdsData.moduleInfos[i].printInfos[j].printButton = printButton;
                                        if (onEvent != null)
                                            onEvent(MDS_EVENT.PRINT, i, j, printButton);
                                    }

                                    if (plusButton != mdsData.moduleInfos[i].printInfos[j].plusButton)
                                    {
                                        mdsData.moduleInfos[i].printInfos[j].plusButton = plusButton;
                                        if (onEvent != null)
                                            onEvent(MDS_EVENT.PLUS, i, j, plusButton);
                                    }

                                    if (minusButton != mdsData.moduleInfos[i].printInfos[j].minusButton)
                                    {
                                        mdsData.moduleInfos[i].printInfos[j].minusButton = minusButton;
                                        if (onEvent != null)
                                            onEvent(MDS_EVENT.MINUS, i, j, minusButton);
                                    }
                                }
                            }
                        }
                        ///read tracking data word
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(296, 40);    //Read 10 Holding Registers from Server, starting with Address 1
                            if( r.Length != 40)
                            {
                                Debug.WriteLine("Read Error < 176 array" );
                                break;
                            }
                            for(int i = 0 ; i < 40 ; i++ ) //module
                                mdsData.positions[i].chuteNum = r[i];
                        }
                        //READ < tracking data dword ??
                        {
                            var r = m_modbusClient.ReadHoldingRegisters(598, 80);    //Read 10 Holding Registers from Server, starting with Address 1
                            if( r.Length != 80 )
                            {
                                Debug.WriteLine("Read Error < confirm dword" );
                                break;
                            }

                            for(int i = 0 ; i < 40 ; i++ ) //module
                            {
                                int num = r[i*2] + r[i*2 + 1] * 65536;
                                mdsData.positions[i].pid = num;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        Connection();
                    }
                    await Task.Delay(200);
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
                        Debug.WriteLine("Value of HoldingRegister " + kv.Key + " " + kv.Value.ToString());

                        //Get PID
                        if (kv.Key == 900 || kv.Key == 901)
                        {
                            if (SetPID() == 1)
                            {
                                Distribution();
                            }
                        }
                        if (act0 != null)
                            act0(kv.Key, kv.Value);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            });
        }
        public int StartClient()
        {
            Connection();
            //ReadArray();
            ReadMDS();
            //StartEvent();

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
            Debug.WriteLine(startingAddress);
            Debug.WriteLine(quantity);
            for (int i = 0; i < quantity; i++)
                Debug.WriteLine(modbusServer.holdingRegisters[startingAddress + i]);
        }
        public int MakePID(int chuteID)
        {
            if(!m_dist)
            {
                Debug.WriteLine("========== Error ==========");
                Debug.WriteLine("pid 받기 전에 다시 들어왔음");
            }
            Debug.WriteLine("MakePID chuteID :" + chuteID);
            m_chuteID = chuteID;
            m_modbusClient.WriteMultipleRegisters(32010, new int[] { 1 });
            m_dist = false;
            return 0;
        }
        private int SetPID()
        {
            Debug.WriteLine("SetPID");
            int[] r = m_modbusClient.ReadHoldingRegisters(900, 2);    //Read 10 Holding Registers from Server, starting with Address 1
            int pid = r[1] * 65536 + r[0];
            Debug.WriteLine("Value of HoldingRegister: " + pid);
            if (mdsData.pid != pid)
            {
                mdsData.pid = pid;
                Debug.WriteLine("Reset 32010");
                m_modbusClient.WriteMultipleRegisters(32010, new int[] { 0 });
                return 1;  /// update
            }
            return 0;
        }
        public int Distribution()
        {
            //todo Get chuteID
            m_modbusClient.WriteMultipleRegisters(32000, new int[] { mdsData.pid % 65536, mdsData.pid / 65536, m_chuteID, 0 });
            Debug.WriteLine("Set chute " + m_chuteID);
            m_dist = true;
            return 0;
        }
        public int GetDistribution()
        {
            int[] r = m_modbusClient.ReadHoldingRegisters(32000, 4);    //Read 10 Holding Registers from Server, starting with Address 1
            int pid = r[1] * 65536 + r[0];
            int chute = r[3] * 65536 + r[2];
            Debug.WriteLine("pid : " + pid + " , cute : " + chute);
            return 0;
        }
        public int PrintAll()
        {
            Debug.WriteLine("===================");
            Debug.Write("Value of HoldingRegister ");
            for (int i = 0; i < readLen; i++)
            {
                if (i % 10 == 0)
                    Debug.WriteLine("");
                Debug.Write(registers[i] + ", ");

            }
            Debug.WriteLine("");
            Debug.WriteLine("===================");
            return 0;
        }
        public void SetActive(bool active)
        {
            m_active = active;
        }
    }
}
