using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using CS_SMS_LIB;
using System.ComponentModel;
using Windows.System;
using Serilog;
using Windows.UI.Popups;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;


// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Monitoring : Page
    {
        ObservableCollection<PListData> bundleList = new ObservableCollection<PListData>();
        ObservableCollection<PListData> remainList = new ObservableCollection<PListData>();
        ObservableCollection<Product> cancelList = new ObservableCollection<Product>();
        ObservableCollection<Product> cancelScanList = new ObservableCollection<Product>();
        ObservableCollection<PListData> mdsList = new ObservableCollection<PListData>();
        ObservableCollection<PListData> mdsListHistory = new ObservableCollection<PListData>();



        public string m_lastCode { get; set; } = "";
        public DateTime m_lastTime = DateTime.Now;
        //public CMPS m_lastData = new CMPS();
        public Product m_lastData = new Product();
        public Product m_cancelData = null;
        private ConcurrentQueue<Product> m_queueData = new ConcurrentQueue<Product>();
        private ConcurrentQueue<Product> m_currentData = new ConcurrentQueue<Product>();
        //static Mutex m_monitorMutex = new Mutex(false, "monitoring_mutex");
        static Mutex m_monitorMutex = new Mutex();
        private int m_lastBundleCnt = 0;
        private int m_bundleFocusIdx  = 0;
        private int m_lastRemainCnt = 0;
        private int m_remainFocusIdx = 0;
        private int m_lastBundleSeq = 0;
        public Monitoring()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            // Show the message dialog
            SetMainScanner();
            MakeEvent();
            SetMqttHandler();
            CheckQueue();
        }

        private void CheckQueue()
        {
            Log.Information("Start CheckQueue()");
            Task.Run(async () =>
            {
                Product pData = new Product();
                int taskCnt = 0;
                while (true)
                {
                    if (m_currentData.IsEmpty && m_queueData.TryDequeue(out pData))
                    {
                        //m_monitorMutex.WaitOne();
                        m_currentData.Enqueue(pData);
                        ///MakePID
                        Log.Information("=======Make PID====== {0}", taskCnt++);
                        if (pData.pid < 1)
                        {
                            PIDData pidData = await global.api.GetPID();
                            if (pidData.status == "OK")
                            {
                                ///Distribution
                                //global.md.mdsData.pid = pidData.pid;
                                //OnEvent_PID(pidData.pid);
                                pData.pid = pidData.pid;
                            }
                            else
                                Alert(pidData.msg);
                            //m_monitorMutex.ReleaseMutex();
                        }
                        global.md.mdsData.pid = pData.pid;
                        OnEvent_PID(pData.pid);
                    }
                    //Thread.Sleep(50);
                }
            });
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Log.Information("Monitoring OnLoad");
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "작업 관리";
            SetSUBScanner();
        }

        private async void Alert(string msg)
        {
            Log.Information("Alert:" + msg);
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);

           // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
           // messageDialog.Commands.Add(new UICommand(
           //     "Try again",
           //     new UICommandInvokedHandler(this.CommandInvokedHandler)));
            messageDialog.Commands.Add(new UICommand(
                "Close",
                new UICommandInvokedHandler(this.CommandInvokedHandler)));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            await messageDialog.ShowAsync();
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            // Display message showing the label of the command that was invoked
        }

        private void MakeEvent()
        {
            global.md.onEvent = (MDS_EVENT eType, int id0, int id1, int id2) =>
            {
                Log.Information("EVENT {0} {1} {2} {3}", eType, id0, id1, id2);
                switch (eType)
                {
                    case MDS_EVENT.PID:
                        OnEvent_MasterSensor(id0);
                        //OnEvent_PID(id0);
                        break;
                    case MDS_EVENT.PRINT:
                        OnEvent_PRINT(id0, id1, id2);
                        break;
                    case MDS_EVENT.CONFIRM_PID:
                        ///module, chute_num, pid
                        OnEvent_CONFIRM_PID(id0, id1, id2);
                        break;
                    case MDS_EVENT.CHUTECHOICE:
                        OnEvent_CHUTECHOICE(id0, id1, id2);
                        break;
                    case MDS_EVENT.PLUS:
                        OnEvent_PLUS(id0, id1, id2);
                        break;
                    case MDS_EVENT.MINUS:
                        OnEvent_MINUS(id0, id1, id2);
                        break;
                    case MDS_EVENT.FULL:
                        ///chute_num, 0, onoff
                        OnEvent_FULL(id0, id1, id2);
                        break;
                    case MDS_EVENT.FIRSTSENSOR:
                        OnEvent_FIRSTSENSOR(id0, id1, id2);
                        break;
                }
            };
        }

        private void OnEvent_MasterSensor(int pid)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_MasterSensor {0}", pid);
                if (pid <= 0)
                {
                    return;

                }
                ///call Leave api
                global.api.MasterSensor(pid);
            });
        }

        private void OnEvent_PID(int pid)
        {
            m_monitorMutex.WaitOne();
            Product pData;
            if (!m_currentData.TryDequeue(out pData))
            {
                var g = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Alert("CurrentData Empty 에러");
                });
                m_monitorMutex.ReleaseMutex();
                return;
            }
            Log.Information("OnEvent_PID {0} chute_num {1}", pid, pData.chute_num);
            if (pid <= 0)
            {
                m_monitorMutex.ReleaseMutex();
                return;

            }
            if (pData.chute_num <= 0)
            {
                var ig = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Alert("슈트 0 할당 에러");
                });
                m_monitorMutex.ReleaseMutex();
                return;
            }
            global.md.Distribution(pData.chute_num, pid);
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateUI(Monitoring_pid, pid.ToString());
                UpdateUI(Monitoring_chuteid, global.md.m_chuteID.ToString());
            });
            ///call Leave api
            global.api.Leave(pData.seq, pid, pData.send_cnt, pData.chute_num);
            m_monitorMutex.ReleaseMutex();
        }
        private void OnEvent_CONFIRM_PID(int module, int chute_num, int pid)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_CONFIRM_PID module {0} chute_num{1} pid {2}", module, chute_num, pid);
                if (pid <= 0)
                    return;

                int confirm_data = global.md.mdsData.moduleInfos[module].chuteInfos[chute_num].confirmData;
                int t_pid = global.md.mdsData.moduleInfos[module].chuteInfos[chute_num].pidNum;
                int stack_count = global.md.mdsData.moduleInfos[module].chuteInfos[chute_num].stackCount;
                global.api.Release(t_pid, confirm_data, stack_count, module * 4 + chute_num + 1);
            });
        }
        private void OnEvent_CHUTECHOICE(int chute_num, int id1, int onoff)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_CHUTECHOICE chute_num {0} data {1}", chute_num, onoff);
                global.api.FullManual(chute_num, onoff);
            });
        }
        private void OnEvent_FULL(int module, int chuteid, int onoff)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_FULL module {0} chuteidx {1} data {2}", module, chuteid, onoff);
                global.api.FullAuto(module * 4 + chuteid + 1, onoff);
            });
        }
        private async void Printing(int chute_num)
        {
            Log.Information("Printing chute_num {0} start", chute_num);
            int locPrint = global.m_matchPrintChute[chute_num];
            /*
            if (chute_num == 1 || chute_num == 3 || chute_num == 5)
                locPrint = 0;
            else if (chute_num == 2 || chute_num == 4 || chute_num == 6)
                locPrint = 1;
            else if (chute_num == 7 || chute_num == 9 || chute_num == 11)
                locPrint = 2;
            else if (chute_num == 8 || chute_num == 10 || chute_num == 12)
                locPrint = 3;
            */

            PrintList p = await global.api.Print(chute_num, "", "");
            Log.Information("Printing Location {0}", locPrint);
            global.m_printer[locPrint].PrintData(p);
            //var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            //{
            UpdateUI(Monitoring_printer, "Printing " + (locPrint + 1).ToString());
            //});
            Log.Information("Printing chute_num {0} locPrint {1} end", chute_num, locPrint + 1);
        }
        private async void Printing(int chute_num, string job_dt, string box_num)
        {
            Log.Information("Printing chute_num {0} job_dt {1} box_num {2} start", chute_num, job_dt, box_num);
            int locPrint = global.m_matchPrintChute[chute_num];
            PrintList p = await global.api.Print(chute_num, job_dt, box_num);
            Log.Information("Printing Location {0}", locPrint);
            global.m_printer[locPrint].PrintData(p);
            UpdateUI(Monitoring_printer, "Printing " + (locPrint + 1).ToString());
            Log.Information("Printing chute_num {0} locPrint {1} end", chute_num, locPrint + 1);
        }
        private async void PrintingRack(int rack_num, int chute_num, string job_dt, string box_num)
        {
            Log.Information("PrintingRack chute_num {0} job_dt {1} box_num {2} start", chute_num, job_dt, box_num);
            int locPrint = global.m_matchPrintRack[rack_num];
            PrintList p = await global.api.Print(chute_num, job_dt, box_num);
            Log.Information("Printing Location {0}", locPrint);
            global.m_printer[locPrint].PrintData(p);
            UpdateUI(Monitoring_printer, "Printing " + (locPrint + 1).ToString());
            Log.Information("Printing chute_num {0} locPrint {1} end", chute_num, locPrint + 1);
        }
        private void OnEvent_PRINT(int chute_num, int value, int id2)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_LPRINT {0} {1} {2} START", chute_num, value, id2);
                if (value == 0)
                    return;

                //int chute_num1 = module * 4 + direct * 2 + (1 + direct) % 2;   ///left
                //int chute_num2 = module * 4 + direct * 2 + (1 + direct) % 2 + 2; ///right
                Printing(chute_num);
                Log.Information("OnEvent_PRINT {0} {1} {2} END", chute_num, value, id2);
            });
        }

        private void OnEvent_PLUS(int module, int direct, int value)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_PLUS module {0} direct {1} value {2}", module, direct, value);
                if (value == 0)
                    return;

                int chute_num1 = module * 4 + direct * 2 + (1 + direct) % 2;
                int chute_num2 = module * 4 + direct * 2 + (1 + direct) % 2 + 2;
                global.api.AddStatus(chute_num1, "+");
                global.api.AddStatus(chute_num2, "+");
            });
        }
        private void OnEvent_MINUS(int module, int direct, int value)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_MINUS module {0} direct {1} value {2}", module, direct, value);
                if (value == 0)
                    return;

                int chute_num1 = module * 4 + direct * 2 + (1 + direct) % 2;
                int chute_num2 = module * 4 + direct * 2 + (1 + direct) % 2 + 2;
                global.api.AddStatus(chute_num1, "-");
                global.api.AddStatus(chute_num2, "-");
            });
        }

        private void OnEvent_FIRSTSENSOR(int value, int id1, int id2)
        {
            Task.Run(async () =>
            {
                Log.Information("OnEvent_FIRSTSENSOR value {0}", value);
                if (value == 0)
                    return;

                var tData = await global.api.FirstSensor();
                Log.Information("===First Data===");
                if (tData.status == "OK")
                {
                    if (tData.buffer > 0)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            tData.send_cnt = 1;
                            if(tData.buffer > 1)
                                tData.send_cnt = tData.buffer;
                            m_cancelData = tData;
                            cancelList.Clear();
                            cancelList.Add(tData);
                            Log.Information(tData.chute_num.ToString());
                            Log.Information("======={0}======", tData.sku_nm);
                            Log.Information("=======ADD QUEUE======");
                            m_queueData.Enqueue(tData);
                            mdsList.Clear();
                            foreach (var data in tData.list)
                            {
                                if (data.highlight == "yellow")
                                    data.leave_qty_color = "red";
                                Log.Information(data.highlight);
                                mdsList.Add(data);
                                if (data.highlight == "yellow")
                                {
                                    var historyData = new PListData(data);
                                    historyData.highlight = "white";
                                    mdsListHistory.Insert(0, historyData);
                                    while (mdsListHistory.Count() > 10)
                                    {
                                        try
                                        {
                                            mdsListHistory.RemoveAt(10);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Information("{0} Exception caught.", ex);
                                        }
                                    }
                                }
                            }
                        });
                        m_lastData = tData;
                    }
                }
            });
        }


        private void SetSUBScanner()
        {
            foreach (var scanner in global.udp.m_scaner)
            {
                scanner.act0 = (string job, string name, int chute_num, string barcode ) =>
                {
                    Log.Information(job + " "+ name + " " + chute_num + " " + barcode);
                    if (job == "BOX")
                    {
                        if (chute_num == -1)
                        {
                            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Alert("스캐너를 할당하세요!(" + barcode + ")");
                            });
                        }
                        else if (chute_num == 0)
                        {
                            Scanner_Process(barcode, Monitoring_scanner0, 0);
                        }
                        else if (chute_num >= 1 && chute_num <= 48)
                        {
                            global.api.AddGoods(chute_num, barcode);
                            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                UpdateUI(Monitoring_scanner1, "슈터 " + chute_num.ToString() + " : " + barcode);
                            });
                        }
                        else
                        {
                            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Alert("(" + barcode + ")할당이 잘못 되었습니다.! chute = " + chute_num.ToString());
                            });
                        }
                    }
                    else if(job == "INDICATOR")
                    {
                        GetIndicatorList(chute_num, barcode);
                    }
                };
            }
            return;
        }
        private async void GetIndicatorList(int chute_num, string barcode)
        {
            Log.Information("GetIndicatorList: " + chute_num.ToString() + " ," + barcode);
            var indicatorBody = await global.api.GetIndicatorList(chute_num, barcode);
            if (global.mqc.isConnect)
            {
                CMqttApi.MpsBodyIndOff offReqBody = new CMqttApi.MpsBodyIndOff { ind_off = indicatorBody.ind_off };
                //first ind off
                global.mqc.ind_off_req(offReqBody);

                CMqttApi.MpsBodyIndOn reqBody = new CMqttApi.MpsBodyIndOn { action = indicatorBody.action, action_type = indicatorBody.action_type, biz_type = indicatorBody.biz_type };
                foreach (var a in indicatorBody.ind_on)
                {
                    int box = Convert.ToInt32(a.org_boxin_qty);
                    int ea = Convert.ToInt32(a.org_ea_qty);
                    reqBody.ind_on.Add(new CMqttApi.MpsIndOn { id=a.id, org_box_qty = box, org_ea_qty = ea, biz_id = a.biz_id, view_type = a.view_type, seg_role = a.seg_role});
                }
                //and ind on
                global.mqc.ind_on_req(reqBody);
                //UpdateUI("LED ON");
            }
            else
            {
                //UpdateUI("disconnected");
            }
        }

        private async void UpdateUI(TextBox tbox, string barcode)
        {
            Log.Information("UpdateUI : " + barcode);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                //code to update UI
                tbox.Text = barcode;
            });
        }
        public int SetMainScanner()
        {
            global.banner.act0 = (string barcode) =>
            {
                Scanner_Process(barcode, Monitoring_scanner0, 500);
            };
            return 0;
        }

        private void Monitoring_chuteid_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Bundle_Click(object sender, RoutedEventArgs e)
        {
            bundleList.Clear();
            var options = new FlyoutShowOptions()
            {
                Position = new Point(0,100),
                ShowMode = FlyoutShowMode.Transient
            };
            Monitoring_bundle.Flyout.ShowAt(Monitoring_Main_Grid as UIElement, options);
        }

        private void Bundle_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                if (!global.md.m_isCon)
                {
                    Alert("MDS 접속에러");
                    return;
                }

                Bundle_input.Focus(FocusState.Programmatic);
                Bundle_input.SelectionStart = Bundle_input.Text.Length;
                TextBox box = sender as TextBox;
                Log.Information(box.Text);
                foreach( var data in bundleList)
                {
                    if(data.idx.ToString() == box.Name)
                    {
                        Log.Information("=======send leave api ====== {0} {1}", data.idx, data.seq);
                        m_lastData.Set(data);
                        try
                        {
                            m_lastBundleCnt = Int32.Parse(box.Text);
                        }
                        catch (FormatException ex)
                        {
                            Alert(ex.Message);
                            continue;
                        }
                        m_lastData.send_cnt = m_lastBundleCnt;
                        if( data.remain_qty < m_lastBundleCnt)
                        {
                            Alert("잔류 수량 초과");
                        }
                        else
                        {
                            mdsList.Clear();
                            cancelList.Clear();
                            cancelList.Add(m_lastData);
                            m_bundleFocusIdx = data.idx;
                            m_lastBundleSeq = data.seq;
                            foreach (var d in m_lastData.list)
                            {
                                if (data.seq == d.seq)
                                {
                                    data.highlight = "yellow";
                                    d.highlight = "yellow";
                                    data.leave_qty_color = "red";
                                    d.leave_qty_color = "red";
                                }
                                mdsList.Add(d);
                                if (d.highlight == "yellow" )
                                {
                                    var historyData = new PListData(d);
                                    historyData.highlight = "white";
                                    mdsListHistory.Insert(0, historyData);
                                    while (mdsListHistory.Count() > 10)
                                    {
                                        try
                                        {
                                            mdsListHistory.RemoveAt(10);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Information("{0} Exception caught.", ex);
                                        }
                                    }
                                }
                            }
                            //Log.Information("=======Make PID======");
                            //global.md.MakePID();
                            Log.Information("=======ADD QUEUE======");
                            m_queueData.Enqueue(m_lastData);
                        }
                    }
                }
                Monitoring_bundle.Flyout.Hide();
                //Thread.Sleep(600);
                Thread.Sleep(700);
                Bundle_Processing();
            }
        }

        private void Remain_Click(object sender, RoutedEventArgs e)
        {
            Remain_input.Text = "";
            remainList.Clear();
            var options = new FlyoutShowOptions()
            {
                Position = new Point(0,100),
                ShowMode = FlyoutShowMode.Transient
            };
            Monitoring_remain.Flyout.ShowAt(Monitoring_Main_Grid as UIElement, options);
        }

        private void Remain_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                if (!global.md.m_isCon)
                {
                    Alert("MDS 접속에러");
                    return;
                }
                TextBox box = sender as TextBox;
                Log.Information(box.Text);
                foreach ( var data in remainList)
                {
                    if(data.idx.ToString() == box.Name)
                    {
                        Log.Information("=======send leave api ====== {0} {1}", data.idx, data.seq);
                        m_lastData.Set(data);
                        try
                        {
                            m_lastRemainCnt = Int32.Parse(box.Text);
                        }
                        catch (FormatException ex)
                        {
                            Alert(ex.Message);
                            continue;
                        }
                        m_lastData.send_cnt = m_lastRemainCnt;
                        if( data.remain_qty < m_lastRemainCnt)
                        {
                            Alert("잔류 수량 초과");
                        }
                        else
                        {
                            mdsList.Clear();
                            m_remainFocusIdx = data.idx;
                            foreach (var d in m_lastData.list)
                            {

                                if (data.seq == d.seq)
                                    data.highlight = "yellow";
                                mdsList.Add(d);
                                if (d.highlight == "yellow" )
                                {
                                    var historyData = new PListData(d);
                                    historyData.highlight = "white";
                                    mdsListHistory.Insert(0, historyData);
                                    while (mdsListHistory.Count() > 10)
                                    {
                                        try
                                        {
                                            mdsListHistory.RemoveAt(10);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Information("{0} Exception caught.", ex);
                                        }
                                    }
                                }
                            }
                            global.api.Leave(m_lastData.seq, -1, m_lastData.send_cnt, m_lastData.chute_num);
                        }
                    }
                }
                //remainList.Clear();
                //Thread.Sleep(500);
                Thread.Sleep(200);
                Remain_Processing();
                //Monitoring_remain.Flyout.Hide();
            }
        }

        private async void Bundle_Processing()
        {
            Log.Information("=======Bundle Processing======");
            if (!global.md.m_isCon)
            {
                Alert("MDS 접속에러");
                return;
            }

            bundleList.Clear();
            mdsList.Clear();
            Log.Information("=======Get Chute======");
            var tData = await global.api.GetChute(m_lastCode, "bundle");
            if (tData.status == "OK")
            {
                bool isFirst = true;
                ///check focus
                Log.Information("bundleFocusIdx: " + m_bundleFocusIdx.ToString());
                ///check dup send seq
                bool isBeforeUpdate = false;
                foreach (var data in tData.list)
                {
                    if (m_lastBundleSeq > 0 && m_lastBundleSeq == data.seq)
                    {
                        Log.Information("Duple Bundle list");
                        isBeforeUpdate = true;
                    }
                }
                if (isBeforeUpdate) ///데이터 다시 가지고 오기
                {
                    //Thread.Sleep(500);
                    Thread.Sleep(200);
                    Log.Information("=======RE Get Chute======");
                    var ttData = await global.api.GetChute(m_lastCode, "bundle");
                    if (ttData.status != "OK")
                    {
                        Alert(ttData.msg);
                        return;
                    }
                    m_lastData = ttData;
                }
                else
                    m_lastData = tData;
                if (m_lastData.list.Count() > m_bundleFocusIdx)
                {
                    if (m_lastData.list[m_bundleFocusIdx].highlight != "gray")
                        isFirst = false;
                }
                int idx = 0;
                foreach (var data in m_lastData.list)
                {
                    if (data.highlight != "gray")
                    {
                        data.cnt = m_lastBundleCnt;
                        if (isFirst)
                        {
                            m_bundleFocusIdx = idx;
                            isFirst = false;
                        }
                    }
                    data.idx = idx++;
                    bundleList.Add(data);
                    ///todo same seq
                    //PListData d = new PListData(data);
                    //d.highlight = "yellow";
                    //d.leave_qty_color = "red";
                    mdsList.Add(data);
                }
            }
            else
                Alert(tData.msg);

        }

        private async void Remain_Processing()
        {
            Log.Information("=======Remain Processing======");
            if (!global.md.m_isCon)
            {
                Alert("MDS 접속에러");
                return;
            }
            remainList.Clear();
            mdsList.Clear();
            Log.Information("=======Get Chute======");
            var tData = await global.api.GetChute(m_lastCode, "remain");
            if (tData.status == "OK")
            {
                bool isFirst = true;
                ///for focus
                if( tData.list.Count() > m_remainFocusIdx)
                {
                    if( tData.list[m_remainFocusIdx].highlight != "gray" )
                        isFirst = false;
                }
                int idx = 0;
                foreach (var data in tData.list)
                {

                    if (data.highlight != "gray")
                    {
                        data.cnt = m_lastRemainCnt;
                        if (isFirst)
                        {
                            m_remainFocusIdx = data.idx;
                            isFirst = false;
                        }
                    }
                    data.idx = idx++;
                    remainList.Add(data);
                    ///todo same seq
                    //PListData d = new PListData(data);
                    //d.highlight = "yellow";
                    //d.leave_qty_color = "red";
                    mdsList.Add(data);
                }
                m_lastData = tData;
            }
            else
                Alert(tData.msg);
        }

        private async void CancelScan_Processing()
        {
            Log.Information("=======CancelScan Processing======");
            if (!global.md.m_isCon)
            {
                Alert("MDS 접속에러");
                return;
            }
            //cancelScanList.Clear();
            //mdsList.Clear();
            Log.Information("=======CancelScan api======");
            var tData = await global.api.CancelScan(m_lastCode);
            if (tData.status == "OK")
            {
                cancelScanList.Add(tData);
            }
            //cancelScanList.Add(new Product { chute_num = m_cancelCnt });
            return;
        }

        private async void MDS_Processing()
        {
            Log.Information("=======MDS_Processing======");
            if (!global.md.m_isCon)
            {
                Alert("MDS 접속에러");
                return;
            }

            Log.Information("=======Get Chute======");
            var tData = await global.api.GetChute(m_lastCode, "single");
            if (tData.status == "OK")
            {
                //Log.Information("=======Make PID======");
                //global.md.MakePID();
                m_cancelData = tData;
                cancelList.Clear();
                cancelList.Add(tData);

                tData.send_cnt = 1;
                Log.Information(tData.chute_num.ToString());
                Log.Information("======={0}======", tData.sku_nm);
                Log.Information("=======ADD QUEUE======");
                m_queueData.Enqueue(tData);

                mdsList.Clear();
                foreach (var data in tData.list)
                {
                    //if (data.highlight == "ON")
                    //    data.color = "Aqua";
                    if (data.highlight == "yellow")
                        data.leave_qty_color = "red";
                    Log.Information(data.highlight);
                    mdsList.Add(data);
                    if (data.highlight == "yellow")
                    {
                        var historyData = new PListData(data);
                        historyData.highlight = "white";
                        mdsListHistory.Insert(0, historyData);
                        while (mdsListHistory.Count() > 10)
                        {
                            try
                            {
                                mdsListHistory.RemoveAt(10);
                            }
                            catch (Exception ex)
                            {
                                Log.Information("{0} Exception caught.", ex);
                            }
                        }
                    }
                }
                m_lastData = tData;
            }
            else
                Alert(tData.msg);

            /*
            mdsList.Clear();
            //CMPS p2 = new CMPS(p);
            mdsList.Add(m_lastData);
            */
            Log.Information("=======MDS_Processing end======");
        }

        private void Scanner_Process(string barcode, TextBox textBox, int delay_ms)
        {
            DateTime dt2 = DateTime.Now;
            TimeSpan span = dt2 - m_lastTime;
            int ms = (int)span.TotalMilliseconds;
            Log.Information("SacnTime : " + ms.ToString());
            m_lastTime = dt2;

            if (ms > delay_ms || m_lastCode != barcode)
            {
                Log.Information("===Change Code======");
                m_lastCode = barcode;

                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (!global.md.m_isCon)
                    {
                        Alert("MDS 접속에러");
                        return;
                    }
                    UpdateUI(textBox, m_lastCode);
                    if (Monitoring_bundle.Flyout.IsOpen)
                    {
                        Bundle_Processing();
                    }
                    else if( Monitoring_remain.Flyout.IsOpen)
                    {
                        Remain_Processing();
                    }
                    else if( Monitoring_cancelScan.Flyout.IsOpen)
                    {
                        CancelScan_Processing();
                    }
                    else if (global.md.m_isCon)
                    {
                        MDS_Processing();
                    }
                    else
                    {
                        Log.Information("=======ERROR======");
                        Log.Information("=======ERROR======");
                        Log.Information("=======ERROR======");
                    }
                });
            }
            else
            {
                Log.Information("===Same Code======");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("===Cancel======");
            var options = new FlyoutShowOptions()
            {
                Position = new Point(0,100),
                ShowMode = FlyoutShowMode.Transient
            };
            Monitoring_cancel.Flyout.ShowAt(Monitoring_Main_Grid as UIElement, options);
        }

        private void CancelScan_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("===CancelScan======");
            cancelScanList.Clear();
            var options = new FlyoutShowOptions()
            {
                Position = new Point(0,100),
                ShowMode = FlyoutShowMode.Transient
            };
            Monitoring_cancelScan.Flyout.ShowAt(Monitoring_Main_Grid as UIElement, options);
        }

        private void Cancel_Confirm(object sender, RoutedEventArgs e)
        {
            if (!global.md.m_isCon)
            {
                Alert("MDS 접속에러");
                return;
            }
            Monitoring_cancel.Flyout.Hide();
            Log.Information("===Cancel confirm======");
            global.api.Cancel(global.md.mdsData.pid);
            global.md.CancelPID();
            cancelList.Clear();
        }

        private void Input_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                if (!global.md.m_isCon)
                {
                    Alert("MDS 접속에러");
                    return;
                }
                TextBox box = sender as TextBox;
                Log.Information(box.Text);
                Scanner_Process(box.Text, Monitoring_scanner0, 100);
                box.Text = "";
            }
        }

        private void TmpClick(object sender, RoutedEventArgs e)
        {
            Scanner_Process("8809681702713", Monitoring_scanner0, 100);
        }

        private void BundleChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var textbox = sender as TextBox;
            if (textbox == null) return;
            if (textbox.Name == m_bundleFocusIdx.ToString())
            {
                Log.Information("BundleFocusChange");
                textbox.Focus(FocusState.Programmatic);
            }
            textbox.SelectionStart = textbox.Text.Length;
        }

        private void RemainChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var textbox = sender as TextBox;
            if (textbox == null) return;
            if( textbox.Name == m_remainFocusIdx.ToString() )
            {
                Log.Information("RemainFocusChange");
                textbox.Focus(FocusState.Programmatic);
                textbox.SelectionStart = textbox.Text.Length;
            }
            textbox.SelectionStart = textbox.Text.Length;
        }

        private void Bundle_Close(object sender, RoutedEventArgs e)
        {
            Monitoring_bundle.Flyout.Hide();
        }

        private void Remain_Close(object sender, RoutedEventArgs e)
        {
            Monitoring_remain.Flyout.Hide();
        }

        private void Cancel_Close(object sender, RoutedEventArgs e)
        {
            Monitoring_cancel.Flyout.Hide();
        }

        private void CancelScan_Close(object sender, RoutedEventArgs e)
        {
            Monitoring_cancelScan.Flyout.Hide();
        }

        private void SetMqttHandler()
        {
            Log.Information("SetMqttHandler");
            global.mqc.handleIndOnRes = (CMqttApi.MpsRes<CMqttApi.MpsBodyIndOnRes> res) =>
            {
                Log.Information("handleIndOnRes: " + res.body.id + " biz_flag :" + res.body.biz_flag);
                var json = new JObject();
                json.Add("action", res.body.action);
                json.Add("id", res.body.id);
                json.Add("biz_id", res.body.biz_id);
                json.Add("biz_type", res.body.biz_type);
                json.Add("action_type", res.body.action_type);
                json.Add("biz_flag", res.body.biz_flag);
                json.Add("org_relay", res.body.org_relay);
                json.Add("org_box_qty", res.body.org_box_qty);
                json.Add("org_ea_qty", res.body.org_ea_qty);
                json.Add("res_box_qty", res.body.res_box_qty);
                json.Add("res_ea_qty", res.body.res_ea_qty);
                IndicatorRes(json);
                if (res.body.biz_flag == "full")
                    global.mqc.led_on_req(res.body.id);
            };
            global.mqc.handleWebPrintingRack = (CMqttApi.WebReq<CMqttApi.WebPropertiesPrintRack> res) =>
            {
                int rack_num = res.properties.rack_num;
                int chute_num = res.properties.chute_num;
                string job_dt = res.properties.job_dt;
                string box_num = res.properties.box_num;
                Log.Information("handleWebPrintingRack rack_num {0} chute_num {1} job_dt {2} box_num {3}",
                            rack_num, chute_num, job_dt, box_num);
                PrintingRack(rack_num, chute_num, job_dt, box_num);
            };
            global.mqc.handleWebPrintingChute = (CMqttApi.WebReq<CMqttApi.WebPropertiesPrintChute> res) =>
            {
                int chute_num = res.properties.chute_num;
                string job_dt = res.properties.job_dt;
                string box_num = res.properties.box_num;
                Log.Information("handleWebPrintingChute chute_num {0} job_dt {1} box_num {2}",
                            chute_num, job_dt, box_num);
                Printing(chute_num, job_dt, box_num);
            };
        }
        private async void IndicatorRes(JObject json)
        {
            Log.Information("IndicatorRes");
            var indicatorOK = await global.api.IndicatorRes(json);
            ///indicator off
            CMqttApi.MpsBodyIndOff reqBody = new CMqttApi.MpsBodyIndOff { ind_off = indicatorOK.ind_off };
            global.mqc.ind_off_req(reqBody);
        }
    }
}
