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
        ObservableCollection<PListData> mdsList = new ObservableCollection<PListData>();



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
                        PIDData pidData = await global.api.GetPID();
                        if (pidData.status == "OK")
                        {
                            ///Distribution
                            global.md.mdsData.pid = pidData.pid;
                            OnEvent_PID(pidData.pid);
                        }
                        else
                            Alert(pidData.msg);
                        //m_monitorMutex.ReleaseMutex();
                    }
                    //Thread.Sleep(50);
                }
            });
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Log.Information("Monitoring OnLoad");
            global.m_mainTopTB.Text = "작업";
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
            int locPrint = 0;
            if (chute_num == 1 || chute_num == 3 || chute_num == 5)
                locPrint = 0;
            else if (chute_num == 2 || chute_num == 4 || chute_num == 6)
                locPrint = 1;
            else if (chute_num == 7 || chute_num == 9 || chute_num == 11)
                locPrint = 2;
            else if (chute_num == 8 || chute_num == 10 || chute_num == 12)
                locPrint = 3;

            PrintList p = await global.api.Print(chute_num, "", "");
            global.m_printer[locPrint].PrintData(p);
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateUI(Monitoring_printer, "Printing " + (locPrint + 1).ToString());
            });
            Log.Information("Printing chute_num {0} locPrint {1} end", chute_num, locPrint+1);
        }
        private void OnEvent_PRINT(int module, int direct, int value)
        {
            Task.Run(() =>
            {
                Log.Information("OnEvent_PRINT {0} {1} {2} START", module, direct, value);
                if (value == 0)
                    return;

                int chute_num1 = module * 4 + direct * 2 + (1 + direct) % 2;   ///left
                int chute_num2 = module * 4 + direct * 2 + (1 + direct) % 2 + 2; ///right
                if (global.md.mdsData.moduleInfos[module].printInfos[direct].leftChute == 1) ///left
                {
                    Printing(chute_num1);
                }
                else if (global.md.mdsData.moduleInfos[module].printInfos[direct].rightChute == 1) ///right
                {
                    Printing(chute_num2);
                }
                else if (global.md.mdsData.moduleInfos[module].printInfos[direct].leftChute == 0 &&   ///left & right
                         global.md.mdsData.moduleInfos[module].printInfos[direct].rightChute == 0)
                {
                    Printing(chute_num1);
                    Printing(chute_num2);
                }
                Log.Information("OnEvent_PRINT {0} {1} {2} END", module, direct, value);
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


        private void SetSUBScanner()
        {
            foreach (var scanner in global.udp.m_scaner)
            {
                scanner.act0 = (string name, int chute_num, string barcode ) =>
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
                };
            }
            return;
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
                                }
                                mdsList.Add(d);
                            }
                            //Log.Information("=======Make PID======");
                            //global.md.MakePID();
                            Log.Information("=======ADD QUEUE======");
                            m_queueData.Enqueue(m_lastData);
                        }
                    }
                }
                //Monitoring_bundle.Flyout.Hide();
                Thread.Sleep(600);
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
                            }
                            global.api.Leave(m_lastData.seq, -1, m_lastData.send_cnt, m_lastData.chute_num);
                        }
                    }
                }
                //remainList.Clear();
                Thread.Sleep(500);
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
                m_lastData = tData;
                bool isFirst = true;
                ///check focus
                Log.Information("bundleFocusIdx: " + m_bundleFocusIdx.ToString());
                ///check dup send seq
                bool isBeforeUpdate = false;
                foreach (var data in m_lastData.list)
                {
                    if (m_lastBundleSeq > 0 && m_lastBundleSeq == data.seq)
                    {
                        Log.Information("Duple Bundle list");
                        isBeforeUpdate = true;
                    }
                }
                if (isBeforeUpdate) ///데이터 다시 가지고 오기
                {
                    Thread.Sleep(500);
                    Log.Information("=======RE Get Chute======");
                    var ttData = await global.api.GetChute(m_lastCode, "bundle");
                    if (ttData.status != "OK")
                    {
                        Alert(ttData.msg);
                        return;
                    }
                    m_lastData = ttData;
                }
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
                m_lastData = tData;
                bool isFirst = true;
                ///for focus
                if( m_lastData.list.Count() > m_remainFocusIdx)
                {
                    if( m_lastData.list[m_remainFocusIdx].highlight != "gray" )
                        isFirst = false;
                }
                int idx = 0;
                foreach (var data in m_lastData.list)
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
                    mdsList.Add(data);
                }
            }
            else
                Alert(tData.msg);
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
                m_lastData = tData;
                //Log.Information("=======Make PID======");
                //global.md.MakePID();
                m_cancelData = m_lastData;
                cancelList.Clear();
                cancelList.Add(m_lastData);

                m_lastData.send_cnt = 1;
                Log.Information(m_lastData.chute_num.ToString());
                Log.Information("======={0}======", m_lastData.sku_nm);
                Log.Information("=======ADD QUEUE======");
                m_queueData.Enqueue(m_lastData);

                mdsList.Clear();
                foreach (var data in m_lastData.list)
                {
                    //if (data.highlight == "ON")
                    //    data.color = "Aqua";
                    mdsList.Add(data);
                }
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
        }

        private void RemainChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var textbox = sender as TextBox;
            if (textbox == null) return;
            if( textbox.Name == m_remainFocusIdx.ToString() )
            {
                Log.Information("RemainFocusChange");
                textbox.Focus(FocusState.Programmatic);
            }
        }
    }
}
