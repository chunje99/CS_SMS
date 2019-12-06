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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Monitoring : Page
    {
        public string m_lastCode { get; set; } = "";
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
                switch(eType)
                {
                    case MDS_EVENT.PID:
                        OnEvent_PID(id0);
                        break;
                    case MDS_EVENT.PRINT:
                        OnEvent_PRINT(id0, id1, id2);
                        break;
                }
            };
        }

        private void OnEvent_PID(int pid)
        {
            Debug.WriteLine("OnEvent_PID {0}", pid);
            global.md.Distribution();
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateUI(Monitoring_pid, pid.ToString());
                UpdateUI(Monitoring_chuteid, global.md.m_chuteID.ToString());
            });
        }
        private void OnEvent_PRINT(int module, int direct, int value)
        {
            if (value == 0)
                return;

            Debug.WriteLine("OnEvent_PRINT {0} {1} {2}", module, direct, value);
            int locPrint = 0;
            if( direct == 0 )
            {
                if( module == 0 )
                {
                    locPrint = 0;
                    //1
                }
                else if( module == 1 )
                {
                    if(global.md.mdsData.moduleInfos[module].printInfos[direct].leftChute == 1)
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
                else if( module == 2 )
                {
                    locPrint = 2;
                    //3
                }
            } else if( direct == 1 )
            {
                if( module == 0 )
                {
                    locPrint = 1;
                    //2
                }
                else if( module == 1 )
                {
                    if(global.md.mdsData.moduleInfos[module].printInfos[direct].leftChute == 1)
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
                else if( module == 2 )
                {
                    locPrint = 3;
                    //4
                }
            }

            global.PrintSample(
                global.m_printIP[locPrint],
                global.m_printPORT[locPrint],
                null,
                "Printer " + (locPrint + 1).ToString()
                );
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
                        for( int i = 0; i < global.udp.m_scaner.Count(); i++)
                        {
                            int idx = i;
                            if (global.udp.m_scaner[i].m_msgQueue.Count() > 0 )
                            {
                                var barcode = global.udp.m_scaner[i].m_msgQueue.Dequeue();
                                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    switch(idx)
                                    {
                                        case 0:
                                            if(m_lastCode != barcode)
                                            {
                                                m_lastCode = barcode;
                                                global.api.GetChute(m_lastCode);
                                                global.md.MakePID(global.api.m_chute);
                                                UpdateUI(Monitoring_scanner1, m_lastCode);
                                            }
                                            else
                                            {
                                                Debug.WriteLine("===Sampe Code======");
                                            }
                                            break;
                                        case 1:
                                            UpdateUI(Monitoring_scanner2, barcode);
                                            break;
                                        case 2:
                                            UpdateUI(Monitoring_scanner3, barcode);
                                            break;
                                        case 3:
                                            UpdateUI(Monitoring_scanner4, barcode);
                                            break;
                                    }
                                });
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
                if(m_lastCode != data)
                {
                    m_lastCode = data;
                    global.api.GetChute(data);
                    global.md.MakePID(global.api.m_chute);
                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateUI(Monitoring_scanner0, data);
                    });
                }
                else
                {
                    Debug.WriteLine("===Sampe Code======");
                }

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
                while(true)
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
                    Thread.Sleep(5000);
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
            logs += "CurrentModuleSpeed ";
            logs += global.md.mdsData.CurrentModuleSpeed.ToString() + "\n";
            for( int i = 0; i < 3; i++)
            {
                logs += "point" + i.ToString() + " ";
                logs += global.md.mdsData.settingData.pointSpeed[i].ToString() + "\n";
            }
            logs += "ModuleInfo \n";
            for( int i = 0; i < 12; i++)
            {
                logs += "Module" + (i+1).ToString() + "\n";
                logs += "\t"+"status ";
                logs += "\t"+global.md.mdsData.moduleInfos[i].status.ToString() + "\n";
                logs += "\t"+"Speed ";
                logs += "\t"+global.md.mdsData.moduleInfos[i].initSpeed.ToString() + "\n";
                logs += "\t"+"EventSpeed ";
                logs += "\t"+global.md.mdsData.moduleInfos[i].eventSpeed.ToString() + "\n";
                logs += "\t"+"alarm ";
                logs += "\t"+global.md.mdsData.moduleInfos[i].alarm.ToString() + "\n";
                for(int j = 0; j < 4; j++)
                {
                    logs += "\t"+"chute" + (i*4+j+1).ToString() + "\n";
                    logs += "\t"+"\t"+"confirmData ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].chuteInfos[j].confirmData.ToString() + "\n";
                    logs += "\t"+"\t"+"stackCnt ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].chuteInfos[j].stackCount.ToString() + "\n";
                    logs += "\t"+"\t"+"pidNum ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].chuteInfos[j].pidNum.ToString() + "\n";
                    logs += "\t"+"\t"+"full ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].chuteInfos[j].full.ToString() + "\n";
                }
                for(int j = 0; j < 2; j++)
                {
                    logs += "\t"+"PrintInfo_" + (i*2+j).ToString();
                    logs += "\t"+"\t"+"leftChute ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].printInfos[j].leftChute.ToString() + "\n";
                    logs += "\t"+"\t"+"rightChute ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].printInfos[j].rightChute.ToString() + "\n";
                    logs += "\t"+"\t"+"printButton ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].printInfos[j].printButton.ToString() + "\n";
                    logs += "\t"+"\t"+"plusButton ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].printInfos[j].plusButton.ToString() + "\n";
                    logs += "\t"+"\t"+"minusButton ";
                    logs += "\t"+"\t"+global.md.mdsData.moduleInfos[i].printInfos[j].minusButton.ToString() + "\n";
                }
            }

            logs += "TrackingData \n";
            for(int i = 0; i < 40; i++ )
            {
                logs += "position" + (i+1).ToString() + "\n";
                logs += "\t" + "chuteNum ";
                logs += "\t"+global.md.mdsData.positions[i].chuteNum.ToString() + "\n";
                logs += "\t"+"pid ";
                logs += "\t"+global.md.mdsData.positions[i].pid.ToString() + "\n";
            }

            LOGS.Text = logs;

        }
    }
}
