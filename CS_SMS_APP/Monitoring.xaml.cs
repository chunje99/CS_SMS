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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Monitoring : Page
    {
        ObservableCollection<CMPS> bundleList = new ObservableCollection<CMPS>();
        ObservableCollection<CMPS> remainList = new ObservableCollection<CMPS>();
        ObservableCollection<CMPS> mdsList = new ObservableCollection<CMPS>();
        ObservableCollection<CMPS> cancelList = new ObservableCollection<CMPS>();

        public string m_lastCode { get; set; } = "";
        public DateTime m_lastTime = DateTime.Now;
        public CMPS m_lastData = new CMPS();
        public Monitoring()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            SetMainScanner();
            SetSUBScanner();
            MakeEvent();
            ShowLogs();
        }

        private void MakeEvent()
        {
            global.md.onEvent = (MDS_EVENT eType, int id0, int id1, int id2) =>
            {
                switch (eType)
                {
                    case MDS_EVENT.PID:
                        OnEvent_PID(id0);
                        break;
                    case MDS_EVENT.PRINT:
                        OnEvent_PRINT(id0, id1, id2);
                        break;
                    case MDS_EVENT.CONFIRM_PID:
                        ///module, chute_num, pid
                        OnEvent_CONFIRM_PID(id0, id1, id2);
                        break;
                    case MDS_EVENT.CHUTECHOICE:
                        ///chute_num, 0, onoff
                        OnEvent_CHUTECHOICE(id0, id1, id2);
                        break;
                    case MDS_EVENT.FULL:
                        ///chute_num, 0, onoff
                        OnEvent_FULL(id0, id1, id2);
                        break;
                }
            };
        }

        private void OnEvent_PID(int pid)
        {
            Debug.WriteLine("OnEvent_PID {0}", pid);
            global.md.Distribution(m_lastData.chute_num);
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateUI(Monitoring_pid, pid.ToString());
                UpdateUI(Monitoring_chuteid, global.md.m_chuteID.ToString());
            });
            ///call Leave api
            global.api.Leave(m_lastData.seq, pid);
        }
        private void OnEvent_CONFIRM_PID(int module, int chute_num, int pid)
        {
            Debug.WriteLine("OnEvent_CONFIRM_PID module {0} chute_num{1} pid {2}", module, chute_num, pid );
            int confirm_data = global.md.mdsData.moduleInfos[module].chuteInfos[chute_num].confirmData;
            int t_pid = global.md.mdsData.moduleInfos[module].chuteInfos[chute_num].pidNum;
            int stack_count = global.md.mdsData.moduleInfos[module].chuteInfos[chute_num].stackCount;
            global.api.Release(t_pid, confirm_data, stack_count, module*4 + chute_num + 1);
        }
        private void OnEvent_CHUTECHOICE(int chute_num, int id1, int onoff)
        {
            Debug.WriteLine("OnEvent_CHUTECHOICE chute_num {0} data {1}", chute_num, onoff);
            global.api.FullManual(chute_num, onoff);
        }
        private void OnEvent_FULL(int module, int chuteid, int onoff)
        {
            Debug.WriteLine("OnEvent_FULL module {0} chuteidx {1} data {2}", module, chuteid, onoff);
            global.api.FullAuto(module*12 + chuteid + 1, onoff);
        }
        private void OnEvent_PRINT(int module, int direct, int value)
        {
            if (value == 0)
                return;

            Debug.WriteLine("OnEvent_PRINT {0} {1} {2}", module, direct, value);
            int locPrint = 0;
            if (direct == 0)
            {
                if (module == 0)
                {
                    locPrint = 0;
                    //1
                }
                else if (module == 1)
                {
                    if (global.md.mdsData.moduleInfos[module].printInfos[direct].leftChute == 1)
                    {
                        locPrint = 0;
                        //1
                    }
                    else
                    {
                        locPrint = 2;
                        //3
                    }
                }
                else if (module == 2)
                {
                    locPrint = 2;
                    //3
                }
            }
            else if (direct == 1)
            {
                if (module == 0)
                {
                    locPrint = 1;
                    //2
                }
                else if (module == 1)
                {
                    if (global.md.mdsData.moduleInfos[module].printInfos[direct].leftChute == 1)
                    {
                        locPrint = 3;
                        //1
                    }
                    else
                    {
                        locPrint = 1;
                        //3
                    }
                }
                else if (module == 2)
                {
                    locPrint = 3;
                    //4
                }
            }
            global.m_printer[locPrint].PrintSample("Printer" + (locPrint + 1).ToString());
            /*
            global.PrintSample(
                global.m_printIP[locPrint],
                global.m_printPORT[locPrint],
                null,
                "Printer " + (locPrint + 1).ToString()
                );
                */
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateUI(Monitoring_printer, "Printing " + (locPrint + 1).ToString());
            });
            //var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //{
            //});
        }

        private void SetSUBScanner()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        ///scanner msg
                        foreach (var scanner in global.udp.m_scaner)
                        {
                            if (scanner.m_msgQueue.Count() > 0)
                            {
                                var barcode = scanner.m_msgQueue.Dequeue();
                                if (scanner.m_name == "Scanner_1")
                                {
                                    Scanner_Process(barcode, Monitoring_scanner1);
                                }
                                else if (scanner.m_name == "Scanner_2")
                                {
                                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
                                        UpdateUI(Monitoring_scanner2, barcode);
                                    });
                                }
                                else if (scanner.m_name == "Scanner_3")
                                {
                                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
                                        UpdateUI(Monitoring_scanner3, barcode);
                                    });
                                }
                                else if (scanner.m_name == "Scanner_4")
                                {
                                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
                                        UpdateUI(Monitoring_scanner4, barcode);
                                    });
                                }
                            }
                        }

                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            });

        }
        private async void UpdateUI(TextBox tbox, string barcode)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                //code to update UI
                tbox.Text = barcode;
            });
        }
        public int SetMainScanner()
        {
            global.banner.act0 = (string data) =>
            {
                Scanner_Process(data, Monitoring_scanner0);
            };
            return 0;
        }

        private void Monitoring_chuteid_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
        private void ShowLogs()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            CheckLog();
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                    Thread.Sleep(300);
                }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CheckLog();
        }
        private void CheckLog()
        {
            string logs = "";
            logs = "PID ";
            logs += global.md.mdsData.pid.ToString() + "\n";
            logs += "moduleCnt ";
            logs += global.md.mdsData.moduleCnt.ToString() + "\n";
            logs += "heartBest";
            logs += global.md.mdsData.heartBest.ToString() + "\n";
            logs += "moduleSpeed ";
            logs += global.md.mdsData.settingData.moduleSpeed.ToString() + "\n";
            logs += "currentModuleSpeed ";
            logs += global.md.mdsData.currentModuleSpeed.ToString() + "\n";
            for (int i = 0; i < 3; i++)
            {
                logs += "point" + i.ToString() + " ";
                logs += global.md.mdsData.settingData.pointSpeed[i].ToString() + "\n";
            }
            logs += "ModuleInfo \n";
            for (int i = 0; i < 12; i++)
            {
                logs += "Module" + (i + 1).ToString() + "\n";
                logs += "\t" + "status ";
                logs += "\t" + global.md.mdsData.moduleInfos[i].status.ToString() + "\n";
                logs += "\t" + "Speed ";
                logs += "\t" + global.md.mdsData.moduleInfos[i].initSpeed.ToString() + "\n";
                logs += "\t" + "EventSpeed ";
                logs += "\t" + global.md.mdsData.moduleInfos[i].eventSpeed.ToString() + "\n";
                logs += "\t" + "alarm ";
                logs += "\t" + global.md.mdsData.moduleInfos[i].alarm.ToString() + "\n";
                for (int j = 0; j < 4; j++)
                {
                    logs += "\t" + "chute" + (i * 4 + j + 1).ToString() + "\n";
                    logs += "\t" + "\t" + "confirmData ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].confirmData.ToString() + "\n";
                    logs += "\t" + "\t" + "stackCnt ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].stackCount.ToString() + "\n";
                    logs += "\t" + "\t" + "pidNum ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].pidNum.ToString() + "\n";
                    logs += "\t" + "\t" + "full ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].full.ToString() + "\n";
                }
                for (int j = 0; j < 2; j++)
                {
                    logs += "\t" + "PrintInfo_" + (i * 2 + j).ToString() + "\n";
                    logs += "\t" + "\t" + "leftChute ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].leftChute.ToString() + "\n";
                    logs += "\t" + "\t" + "rightChute ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].rightChute.ToString() + "\n";
                    logs += "\t" + "\t" + "printButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].printButton.ToString() + "\n";
                    logs += "\t" + "\t" + "plusButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].plusButton.ToString() + "\n";
                    logs += "\t" + "\t" + "minusButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].minusButton.ToString() + "\n";
                }
            }

            logs += "TrackingData \n";
            for (int i = 0; i < 40; i++)
            {
                logs += "position" + (i + 1).ToString() + "\n";
                logs += "\t" + "chuteNum ";
                logs += "\t" + global.md.mdsData.positions[i].chuteNum.ToString() + "\n";
                logs += "\t" + "pid ";
                logs += "\t" + global.md.mdsData.positions[i].pid.ToString() + "\n";
            }

            LOGS.Text = logs;

        }
        private void Bundle_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void Bundle_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                TextBox box = sender as TextBox;
                Debug.WriteLine(box.Text);
                Monitoring_bundle.Flyout.Hide();
                if (global.md.m_isCon)
                {
                    Debug.WriteLine("=======Get Chute======");
                    Product p = await global.api.GetChute(m_lastCode);
                    Debug.WriteLine("======={0}======",  p.sku_nm);
                    Debug.WriteLine("=======Make PID======");
                    global.md.MakePID();
                    /// pid 받고 해야할까?
                    Debug.WriteLine("=======TODO CALL bundle api======");

                }
            }
        }

        private void Remain_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                TextBox box = sender as TextBox;
                Debug.WriteLine(box.Text);
                Monitoring_remain.Flyout.Hide();
                Debug.WriteLine("=======TODO CALL remain api======");
            }
        }

        private async void Bundle_Processing()
        {
            Debug.WriteLine("=======Bundle Processing======");
            bundleList.Clear();
            ///TODO API
            Debug.WriteLine("=======Get Chute======");
            Product p = await global.api.GetChute(m_lastCode);
            CMPS p2 = new CMPS();
            p2.sku_barcd = m_lastCode;
            bundleList.Add(p2);
        }

        private async void Remain_Processing()
        {
            Debug.WriteLine("=======Remain Processing======");
            remainList.Clear();
            ///TODO API
            Debug.WriteLine("=======Get Chute======");
            Product p = await global.api.GetChute(m_lastCode);
            CMPS p2 = new CMPS(p);
            p2.sku_barcd = m_lastCode;
            remainList.Add(p2);
        }

        private async void MDS_Processing()
        {
            Debug.WriteLine("=======MDS_Processing======");

            Debug.WriteLine("=======Get Chute======");
            Product p = await global.api.GetChute(m_lastCode);
            m_lastData.Set(p);
            Debug.WriteLine(p.chute_num);
            Debug.WriteLine("======={0}======", p.sku_nm);
            Debug.WriteLine("=======Make PID======");
            global.md.MakePID();

            mdsList.Clear();
            //CMPS p2 = new CMPS(p);
            mdsList.Add(m_lastData);
            Debug.WriteLine("=======MDS_Processing end======");
        }

        private void Scanner_Process(string barcode, TextBox textBox)
        {
            DateTime dt2 = DateTime.Now;
            TimeSpan span = dt2 - m_lastTime;
            int ms = (int)span.TotalMilliseconds;
            Debug.WriteLine("SacnTime : " + ms.ToString());
            m_lastTime = dt2;

            if (ms > 500 || m_lastCode != barcode)
            {
                Debug.WriteLine("===Change Code======");
                m_lastCode = barcode;

                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
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
                        Debug.WriteLine("=======ERROR======");
                        Debug.WriteLine("=======ERROR======");
                        Debug.WriteLine("=======ERROR======");
                    }
                });
            }
            else
            {
                Debug.WriteLine("===Same Code======");
            }
        }

        private void Remain_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("===Cancel======");
            cancelList.Clear();
            cancelList.Add(m_lastData);
        }

        private void Cancel_Confirm(object sender, RoutedEventArgs e)
        {
            Monitoring_cancel.Flyout.Hide();
            Debug.WriteLine("===Cancel confirm======");
            global.api.Cancel(global.md.mdsData.pid);
            global.md.CancelPID();
        }

        private void Input_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                TextBox box = sender as TextBox;
                Debug.WriteLine(box.Text);
                Scanner_Process(box.Text, Monitoring_scanner0);
                box.Text = "";
            }
        }
    }
}
