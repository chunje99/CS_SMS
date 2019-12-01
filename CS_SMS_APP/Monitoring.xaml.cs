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
                                global.md.MakePID(++global.md.m_chuteID);
                                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    switch(idx)
                                    {
                                        case 0:
                                            UpdateUI(Monitoring_scanner0, barcode);
                                            break;
                                        case 1:
                                            UpdateUI(Monitoring_scanner1, barcode);
                                            break;
                                        case 2:
                                            UpdateUI(Monitoring_scanner2, barcode);
                                            break;
                                        case 3:
                                            UpdateUI(Monitoring_scanner3, barcode);
                                            break;
                                        case 4:
                                            UpdateUI(Monitoring_scanner4, barcode);
                                            break;
                                    }
                                });
                            }
                        }
                        UpdateUI(Monitoring_pid, global.md.m_pid.ToString());
                        UpdateUI(Monitoring_chuteid, global.md.m_chuteID.ToString());

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
        public Monitoring()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            CheckMsg();
            for(int i = 0; i < 13; i++)
            {
                ColumnDefinition c1 = new ColumnDefinition();
                c1.Width = new GridLength(100, GridUnitType.Star);
                HeartBest.ColumnDefinitions.Add(c1);
            }
            for(int i = 0; i < 13; i++)
            {
                RowDefinition rowDef = new RowDefinition();
                HeartBest.RowDefinitions.Add(rowDef);
            }
            TextBlock t1 = new TextBlock();
            t1.Text = "asdfasdfasf";
            Grid.SetColumn(t1, 0);
            Grid.SetRow(t1, 0);
            HeartBest.Children.Add(t1);
            TextBlock t2 = new TextBlock();
            t2.Text = "asdfasdfasf";
            Grid.SetColumn(t2, 0);
            Grid.SetRow(t2, 1);
            HeartBest.Children.Add(t2);
            TextBlock t3 = new TextBlock();
            t3.Text = "asdfasdfasf";
            Grid.SetColumn(t3, 0);
            Grid.SetRow(t3, 2);
            HeartBest.Children.Add(t3);

            String[] aa = { "aa1", "aa2", "", "" };
            var lv1 = new ListViewItem();
            lv1.Content = "Test Content";
            lv1.Width = 400;
            lv1.HorizontalAlignment = HorizontalAlignment.Stretch;
            lv1.VerticalAlignment = VerticalAlignment.Stretch;
            HeartList.Items.Add(lv1);

        }
        public class Heart_Best
        {
            public string[] name { get; set; } = new string[13];
        }
        public ObservableCollection<Heart_Best> m_items { get; set; } = new ObservableCollection<Heart_Best>();
    }
}
