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
        public MonitorData m_monitorData { get; set; }
        public Monitoring()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.m_monitorData = new MonitorData();

            SetMainScanner();
            CheckMsg();
            MakeTable();
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
                global.md.MakePID(++global.md.m_chuteID);
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Monitoring_scanner0, data);
                    UpdateUI(Monitoring_pid, global.md.m_pid.ToString());
                    UpdateUI(Monitoring_chuteid, global.md.m_chuteID.ToString());
                });

            };
            return 0;
        }
        public int MakeTable()
        {
            MakeTableHeartBest();
            MakeTableTrackingData();
            return 0;
        }
        public int MakeTableHeartBest()
        {
            /*
            for (int i = 0; i < 13; i++)
            {
                ColumnDefinition c1 = new ColumnDefinition();
                c1.Width = new GridLength(150, GridUnitType.Auto);
                HeartBest.ColumnDefinitions.Add(c1);
            }
            for (int i = 0; i < 2; i++)
            {
                RowDefinition rowDef = new RowDefinition();
                HeartBest.RowDefinitions.Add(rowDef);
            }

            TextBlock t1 = new TextBlock();
            t1.Text = "Current Cnt";
            Grid.SetColumn(t1, 0);
            Grid.SetRow(t1, 0);
            HeartBest.Children.Add(t1);

            for (int i = 1; i < 13 ; i++)
            {
                t1 = new TextBlock();
                //t1.Text = "{x:Bind m_monitorData.HeartBest[" + i +"]}";
                //"{x:Bind m_monitorData.HeartBest[" + i +"]}";
                Binding b = new Binding();
                b.Source = m_monitorData.HeartBest[i];
                t1.SetBinding(TextBlock.TextProperty, b);
                Grid.SetRow(t1, 0);
                Grid.SetColumn(t1, i);
                HeartBest.Children.Add(t1);
            }
            
            for (int i = 0; i < 13 ; i++)
            {
                t1 = new TextBlock();
                t1.Text = global.md.registers[i].ToString();
                Grid.SetRow(t1, 1);
                Grid.SetColumn(t1, i);
                HeartBest.Children.Add(t1);
            }
            */
            return 0;
        }
        public int MakeTableTrackingData()
        {
            /*
                    <TextBlock Text="1" Margin="0,0,0,0"></TextBlock>
                    <TextBlock Text="PID" Margin="50,0,0,0"></TextBlock>
                    <TextBlock Text="chute num" Margin="50,20,0,0"></TextBlock>
                    <TextBlock Text="PID" Margin="150,0,0,0"></TextBlock>
                    <TextBlock Text="chute num" Margin="150,20,0,0"></TextBlock>
            */
            for ( int i = 0; i < 10; i++)
            {
                Grid g1 = new Grid();
                TextBlock t1 = new TextBlock();
                t1.Text = i.ToString();
                g1.Children.Add(t1);
                TextBlock t2 = new TextBlock();
                t2.Text = "PID";
                t2.Margin = new Thickness(50, 0, 0, 0);
                g1.Children.Add(t2);
                TrackingData.Children.Add(g1);
            }
            return 0;
        }
        public class Heart_Best
        {
            public string[] name { get; set; } = new string[13];
        }
        public ObservableCollection<Heart_Best> m_items { get; set; } = new ObservableCollection<Heart_Best>();

        private void Monitoring_chuteid_TextChanged(object sender, TextChangedEventArgs e)
        {
            Bindings.Update();
        }
    }
    public class MonitorData
    {
        public int[] HeartBest { get; set; }
        public int[] SettingData { get; set; }
        public int[] EventData { get; set; }
        public MonitorData()
        {
            HeartBest = new int[13];
            SettingData = new int[12];
            EventData = new int[72];
        }
    }
}
