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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Monitoring : Page
    {
        public Monitoring()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            SetMainScanner();
            CheckMsg();
            MakeTable();
            ///Check plc data
            CheckPLCData();
        }

        private void CheckPLCData()
        {
            global.md.act0 = (int address, int value) =>
            {
                Debug.WriteLine("{0}, {1}", value.ToString(), address.ToString());
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if(address == 900 || address == 901)
                        UpdateUI(Monitoring_pid, global.md.m_pid.ToString());
                    else
                        global.ChangeText(value.ToString(), address);

                });
            };
        }

        private void CheckMsg()
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
                                            global.api.GetChute(barcode);
                                            global.md.MakePID(global.api.m_chute);
                                            UpdateUI(Monitoring_scanner1, barcode);
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
                global.api.GetChute(data);
                global.md.MakePID(global.api.m_chute);
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Monitoring_scanner0, data);
                    //UpdateUI(Monitoring_pid, global.md.m_pid.ToString());
                    UpdateUI(Monitoring_chuteid, global.md.m_chuteID.ToString());
                });

            };
            return 0;
        }
        public int MakeTable()
        {
            MakeTableHeartBest();
            MakeTableSettingData();
            MakeTableEventData();
            MakeTableTrackingData();
            MakeTableConfirmData();
            MakeTableInputData();
            return 0;
        }
        public int MakeTableHeartBest()
        {
            TextBlock t0 = new TextBlock();
            t0.Text = "CNT";
            t0.Margin = new Thickness(0, 0, 0, 0);
            HeartBest.Children.Add(t0);
            for (int i = 1; i < 13; i++)
            {
                TextBlock t2 = new TextBlock();
                t2.Text = "Mod" + i;
                t2.Margin = new Thickness(50*i, 0, 0, 0);
                HeartBest.Children.Add(t2);
            }
            int location = 0;
            TextBlock t1 = new TextBlock();
            t1.Text = "0";
            t1.Margin = new Thickness(0, 20, 0, 0);
            HeartBest.Children.Add(t1);
            global.m_plcData.Add(new PlcData(t1, "HeartBest_cnt" , location++));
            for (int i = 1; i < 13; i++)
            {
                TextBlock t2 = new TextBlock();
                t2.Text = "0";
                t2.Margin = new Thickness(50*i, 20, 0, 0);
                HeartBest.Children.Add(t2);
                global.m_plcData.Add(new PlcData(t2, "HeartBest_modul" + i, location++));
            }
            return 0;
        }
        public int MakeTableSettingData()
        {
            int location = 21;
            for (int i = 0; i < 12; i++)
            {
                TextBlock t1 = new TextBlock();
                t1.Text = "Mod" + (i + 1);
                t1.Margin = new Thickness(50*i, 0, 0, 0);
                SettingData.Children.Add(t1);
            }

            for (int i = 0; i < 12; i++)
            {
                TextBlock t1 = new TextBlock();
                t1.Text = "0";
                t1.Margin = new Thickness(50*i, 20, 0, 0);
                SettingData.Children.Add(t1);
                global.m_plcData.Add(new PlcData(t1, "SettingData_modul" + (i+1), location++));
            }
            return 0;
        }
        public int MakeTableEventData()
        {
            int chuteCnt = 1;
            int location = 41;
            for( int i = 1; i < 13; i++)
            {
                Grid g1 = new Grid();
                g1.BorderThickness = new Thickness(1);
                g1.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black);

                TextBlock t1 = new TextBlock();
                t1.Margin = new Thickness(0, 0, 0, 0);
                t1.Text = "Module #" + i.ToString();
                g1.Children.Add(t1);

                TextBlock t2 = new TextBlock();
                t2.Text = "Alarm Code";
                t2.Margin = new Thickness(100, 0, 0, 0);
                g1.Children.Add(t2);

                TextBlock t3 = new TextBlock();
                t3.Text = "Speed";
                t3.Margin = new Thickness(100, 20, 0, 0);
                g1.Children.Add(t3);


                for ( int j = 0; j < 4; j++)
                {
                    TextBlock t4 = new TextBlock();
                    t4.Text = "Chute_" + (chuteCnt++).ToString() + " 만재";
                    t4.Margin = new Thickness(100, j * 20 + 40, 0, 0);
                    g1.Children.Add(t4);

                }
                for ( int j = 0; j < 6; j++)
                { 
                    TextBlock t5 = new TextBlock();
                    t5.Text = "0";
                    t5.Margin = new Thickness(500, j*20, 0, 0);
                    g1.Children.Add(t5);
                    global.m_plcData.Add(new PlcData(t5, "EventData_modul" + i + "_" + j, location++));
                }

                EventData.Children.Add(g1);
            }
            return 0;
        }
        public int MakeTableTrackingData()
        {
            int location = 200;
            for ( int i = 0; i < 51; i++)
            {
                Grid g1 = new Grid();
            g1.BorderThickness = new Thickness(1);
            g1.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black);
                TextBlock t1 = new TextBlock();
                t1.Margin = new Thickness(0, 0, 0, 0);
                t1.Text = i.ToString();
                g1.Children.Add(t1);
                TextBlock t2 = new TextBlock();
                t2.Text = "PID";
                t2.Margin = new Thickness(100, 0, 0, 0);
                g1.Children.Add(t2);
                TextBlock t3 = new TextBlock();
                t3.Text = "ChuteNum";
                t3.Margin = new Thickness(100, 20, 0, 0);
                g1.Children.Add(t3);

                TextBlock t4 = new TextBlock();
                t4.Text = "0";
                t4.Margin = new Thickness(500, 00, 0, 0);
                g1.Children.Add(t4);
                global.m_plcData.Add(new PlcData(t4, "TrackingData_" + i + "_0", location++));
                location++; //dword
                TextBlock t5 = new TextBlock();
                t5.Text = "0";
                t5.Margin = new Thickness(500, 20, 0, 0);
                g1.Children.Add(t5);
                global.m_plcData.Add(new PlcData(t5, "TrackingData_" + i + "_1", location++));

                TrackingData.Children.Add(g1);
            }
            return 0;
        }
        public int MakeTableConfirmData()
        {
            int location = 500;
            for( int i = 1; i < 49; i++)
            {
                Grid g1 = new Grid();
                g1.BorderThickness = new Thickness(1);
                g1.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black);

                TextBlock t1 = new TextBlock();
                t1.Margin = new Thickness(0, 0, 0, 0);
                t1.Text = "Chute #" + i.ToString();
                g1.Children.Add(t1);
                TextBlock t2 = new TextBlock();
                t2.Text = "PID";
                t2.Margin = new Thickness(100, 0, 0, 0);
                g1.Children.Add(t2);
                TextBlock t3 = new TextBlock();
                t3.Text = "Confirm Data";
                t3.Margin = new Thickness(100, 20, 0, 0);
                g1.Children.Add(t3);
                TextBlock t4 = new TextBlock();
                t4.Text = "Stack Count";
                t4.Margin = new Thickness(100, 40, 0, 0);
                g1.Children.Add(t4);
                TextBlock t5 = new TextBlock();
                t5.Text = "Stack Count";
                t5.Margin = new Thickness(100, 60, 0, 0);
                g1.Children.Add(t5);

                for (int j = 0; j < 4; j++)
                {
                    TextBlock t6 = new TextBlock();
                    t6.Margin = new Thickness(500, 20*j, 0, 0);
                    t6.Text = "0";
                    g1.Children.Add(t6);
                    global.m_plcData.Add(new PlcData(t6, "ConfirmData_Chute" + i + "_" + j, location++));
                    if (j == 0) location++;

                }

                ConfirmData.Children.Add(g1);
            }

            return 0;
        }
        public int MakeTableInputData()
        {
            int chuteCnt = 1;
            int location = 800;
            for( int i = 1; i < 13; i++)
            {
                Grid g1 = new Grid();
                g1.BorderThickness = new Thickness(1);
                g1.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black);

                TextBlock t1 = new TextBlock();
                t1.Margin = new Thickness(0, 0, 0, 0);
                t1.Text = "Module #" + i.ToString() + "\n Pakced Data";
                g1.Children.Add(t1);
                for( int j = 0; j < 4; j++)
                {
                    TextBlock t2 = new TextBlock();
                    t2.Text = "Chute_" + (chuteCnt++).ToString() + " Button";
                    t2.Margin = new Thickness(100, j*20, 0, 0);
                    g1.Children.Add(t2);

                    TextBlock t3 = new TextBlock();
                    t3.Text = "0";
                    t3.Margin = new Thickness(500, j*20, 0, 0);
                    g1.Children.Add(t3);
                    global.m_plcData.Add(new PlcData(t3, "InputData_Chute" + (chuteCnt-1) + "_" + j, location++));

                }

                InputData.Children.Add(g1);
            }
            return 0;
        }

        private void Monitoring_chuteid_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}
