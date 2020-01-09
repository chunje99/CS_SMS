using EasyModbus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Serilog;

namespace CS_SMS_LIB
{
    public enum MDS_EVENT
    {
        PID = 0,
        ALARM = 1,
        FULL = 2,
        CONFIRM = 3,
        CONFIRM_STACK = 4,
        CONFIRM_PID = 5,
        CHUTECHOICE = 6,
        PRINT = 8,
        PLUS = 9,
        MINUS = 10,
    }


    public class CModbus
    {
        static private int readLen = 902;
        public int[] registers { get; } = new int[readLen * 2];
        public MDSData mdsData {get;} = new MDSData();
        private Queue<KeyValuePair<int, int>> m_changeQueue = new Queue<KeyValuePair<int, int>>();
        public ModbusClient m_modbusClient { get; set; } = null;
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
        public bool m_isCon { get; set; } = false;
        //static Mutex m_mutex = new Mutex(false, "modbus_mutex");
        static Mutex m_mutex = new Mutex();

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
            Log.Information("MDS Connection " + conCnt.ToString() );
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
                m_isCon = true;
                conCnt++;
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
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
                m_isCon = false;
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
            }
            return 0;

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
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(0, 80);    //Read 10 Holding Registers from Server, starting with Address 1
                            m_mutex.ReleaseMutex();
                            if( r.Length != 80 )
                            {
                                Log.Information("Read Error < 79 array" );
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

                            int alarm, full;
                            for( int i = 0; i < 12; i++)
                            {
                                alarm = r[i * 5 + 20];
                                if(mdsData.moduleInfos[i].alarm != alarm)
                                {
                                    mdsData.moduleInfos[i].alarm = r[i * 5 + 20] = alarm;
                                    if (onEvent != null)
                                        onEvent(MDS_EVENT.ALARM, i, 0, alarm);
                                }
                                for (int j = 0; j < 4; j++)
                                {
                                    full = r[i * 5 + j + 21];
                                    if(mdsData.moduleInfos[i].chuteInfos[j].full != full)
                                    {
                                        mdsData.moduleInfos[i].chuteInfos[j].full = full;
                                        if (onEvent != null)
                                            onEvent(MDS_EVENT.FULL, i, j, full);
                                    }
                                }

                            }
                        }
                        
                        await Task.Delay(50);
                        //READ < confirm word data 176
                        {
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(80, 96);    //Read 10 Holding Registers from Server, starting with Address 1
                            m_mutex.ReleaseMutex();
                            if( r.Length != 96 )
                            {
                                Log.Information("Read Error < 176 array" );
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
                        await Task.Delay(50);
                        //READ < pid + confirm dword data 176
                        {
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(500, 98);    //Read 10 Holding Registers from Server, starting with Address 1
                            m_mutex.ReleaseMutex();
                            if( r.Length != 98 )
                            {
                                Log.Information("Read Error < confirm dword" );
                                break;
                            }

                            int pid = r[0] + r[1] * 65536;
                            if(pid != mdsData.first_pid)
                            {
                                mdsData.first_pid = pid;
                                Log.Information("EVENT PID: " + pid.ToString());
                                //await Task.Delay(100);
                                //m_modbusClient.WriteMultipleRegisters(32010, new int[] { 0 });
                                if (onEvent != null)
                                    onEvent(MDS_EVENT.PID, pid, 0, 0);
                            }

                            for (int i = 0 ; i < 12 ; i++ ) //module
                            {
                                for (int j = 0; j < 4; j++) //chute
                                {
                                    int num = r[(i * 4 + j)*2 + 2] + r[(i * 4 + j)*2 + 3] * 65536;
                                    if( mdsData.moduleInfos[i].chuteInfos[j].pidNum != num )
                                    {
                                        mdsData.moduleInfos[i].chuteInfos[j].pidNum = num;
                                        if (onEvent != null)
                                            onEvent(MDS_EVENT.CONFIRM_PID, i, j, num);
                                    }
                                }
                            }
                        }
                        await Task.Delay(50);
                        //READ print input
                        {
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(176, 120);    //Read 10 Holding Registers from Server, starting with Address 1
                            m_mutex.ReleaseMutex();
                            if( r.Length != 120 )
                            {
                                Log.Information("Read Error < 176 array" );
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
                                        if (onEvent != null)
                                        {
                                            int chute_num = i * 4 + j * 2 + (j+1) % 2;
                                            Log.Information("module {0} size {1} left {2}", i, j, chute_num);
                                            onEvent(MDS_EVENT.CHUTECHOICE, chute_num, 0, leftChute);
                                        }
                                    }

                                    if (rightChute != mdsData.moduleInfos[i].printInfos[j].rightChute)
                                    {
                                        mdsData.moduleInfos[i].printInfos[j].rightChute = rightChute;
                                        if (onEvent != null)
                                        {
                                            int chute_num = i * 4 + j * 2 + 2 + (j+1)%2;
                                            Log.Information("module {0} size {1} right chute {2}", i, j, chute_num);
                                            onEvent(MDS_EVENT.CHUTECHOICE, chute_num, 0, rightChute);
                                        }
                                    }

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
                        await Task.Delay(50);
                        ///read tracking data word
                        {
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(296, 40);    //Read 10 Holding Registers from Server, starting with Address 1
                            m_mutex.ReleaseMutex();
                            if( r.Length != 40)
                            {
                                Log.Information("Read Error < 176 array" );
                                break;
                            }
                            for(int i = 0 ; i < 40 ; i++ ) //module
                                mdsData.positions[i].chuteNum = r[i];
                        }
                        //READ < tracking data dword ??
                        {
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(598, 80);    //Read 10 Holding Registers from Server, starting with Address 1
                            m_mutex.ReleaseMutex();
                            if( r.Length != 80 )
                            {
                                Log.Information("Read Error < confirm dword" );
                                break;
                            }

                            for(int i = 0 ; i < 40 ; i++ ) //module
                            {
                                int num = r[i*2] + r[i*2 + 1] * 65536;
                                mdsData.positions[i].pid = num;
                            }
                        }
                        {
                            m_mutex.WaitOne();
                            var r = m_modbusClient.ReadHoldingRegisters(499, 1);    //Read Remain Cnt
                            m_mutex.ReleaseMutex();
                            if( r.Length != 1 )
                            {
                                Log.Information("Read Error < remain word" );
                                break;
                            }
                            mdsData.remainCnt = r[0];
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.ToString());
                        m_mutex.ReleaseMutex();
                        Connection();
                    }
                    await Task.Delay(100);
                }
            });
        }
        public int StartClient()
        {
            Connection();
            ReadMDS();

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
            Log.Information(startingAddress.ToString());
            Log.Information(quantity.ToString());
            for (int i = 0; i < quantity; i++)
                Log.Information(modbusServer.holdingRegisters[startingAddress + i].ToString());
        }
        public int MakePID()
        {
            if(!m_isCon)
            {
                Log.Information("MakePID: ConError");
                return -1;
            }

            if(!m_dist)
            {
                Log.Information("========== Error ==========");
                Log.Information("pid 받기 전에 다시 들어왔음");
            }
            try
            {
                Thread.Sleep(100);
                m_mutex.WaitOne();
                m_modbusClient.WriteMultipleRegisters(32010, new int[] { 1 });
                m_dist = false;
                m_mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                m_mutex.ReleaseMutex();
                Log.Information(e.ToString());
                Connection();
            }
            return 0;
        }
        public int CancelPID()
        {
            if(!m_isCon)
            {
                Log.Information("Cancel: ConError");
                return -1;
            }
            Log.Information("Cancel:");
            try
            {
                Thread.Sleep(100);
                m_mutex.WaitOne();
                m_modbusClient.WriteMultipleRegisters(32010, new int[] { 2 });
                m_mutex.ReleaseMutex();
                Thread.Sleep(300);
                Log.Information("Cancel: Reset");
                m_mutex.WaitOne();
                m_modbusClient.WriteMultipleRegisters(32010, new int[] { 0 });
                m_mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                m_mutex.ReleaseMutex();
                Log.Information(e.ToString());
                Connection();
            }
            return 0;
        }
        public int Distribution(int chuteID)
        {
            if(!m_isCon)
            {
                Log.Information("Distribution: ConError");
                return -1;
            }

            m_chuteID = chuteID;
            Log.Information("Set chute " + chuteID + " PID  " + mdsData.pid.ToString());
            try
            {
                Thread.Sleep(100);
                m_mutex.WaitOne();
                m_modbusClient.WriteMultipleRegisters(32000, new int[] { mdsData.pid % 65536, mdsData.pid / 65536, chuteID, 0 });
                m_dist = true;
                m_mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                m_mutex.ReleaseMutex();
                Log.Information(e.ToString());
                Connection();
            }
            return 0;
        }
        public int Distribution(int chuteID, int pid)
        {
            if(!m_isCon)
            {
                Log.Information("Distribution: ConError");
                return -1;
            }

            m_chuteID = chuteID;
            Log.Information("Set chute " + chuteID + " PID  " + pid.ToString());
            try
            {
                Thread.Sleep(100);
                m_mutex.WaitOne();
                m_modbusClient.WriteMultipleRegisters(32000, new int[] { pid % 65536, pid / 65536, chuteID, 0 });
                m_dist = true;
                m_mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                m_mutex.ReleaseMutex();
                Log.Information(e.ToString());
                Connection();
            }
            return 0;
        }
        public int GetDistribution()
        {
            if(!m_isCon)
            {
                Log.Information("GetDistribution: ConError");
                return -1;
            }

            int[] r = m_modbusClient.ReadHoldingRegisters(32000, 4);    //Read 10 Holding Registers from Server, starting with Address 1
            int pid = r[1] * 65536 + r[0];
            int chute = r[3] * 65536 + r[2];
            Log.Information("pid : " + pid + " , cute : " + chute);
            return 0;
        }
        public int PrintAll()
        {
            Log.Information("===================");
            Log.Information("Value of HoldingRegister ");
            for (int i = 0; i < readLen; i++)
            {
                if (i % 10 == 0)
                    Log.Information("");
                Log.Information(registers[i] + ", ");

            }
            Log.Information("");
            Log.Information("===================");
            return 0;
        }
        public void SetActive(bool active)
        {
            m_active = active;
        }
    }
}
