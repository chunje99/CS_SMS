using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Windows.UI.Core;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Home : Page
    {
        public string msg = "";
        public bool loaded = false;
        private int m_errorCnt = 0;
        public Home()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private async void AutoConnect(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                //int t = await MainScanner();
                ////sub scanner
                await SubScanner();
                //connect plc
                await ConnectPLC();
                //search printer
                //await PrinterSetting();
                loaded = true;

                if( m_errorCnt == 0)
                {
                    this.Frame.Navigate(typeof(Monitoring));
                }
            }
        }
        private async void UpdateUI(string msg, string color = "black")
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                //MainMsg.Text = msg;
                Paragraph para = new Paragraph();
                var brush = new SolidColorBrush(Windows.UI.Colors.Black);
                if(color == "red")
                    brush = new SolidColorBrush(Windows.UI.Colors.Red);
                para.Inlines.Add(new Run { Text = msg, FontSize = 15, Foreground = brush });
                MainMsg.Blocks.Add(para);
            });
        }
        private async Task<int> MainScanner()
        {
            await Task.Run(() =>
                {
                    Log.Information("Main ScannerIP {0}, PORT {1} ",
                        global.banner.m_ip, global.banner.m_port);
                    msg = "Main ScannerIP " + global.banner.m_ip + " PORT " + global.banner.m_port;
                    UpdateUI(msg);
                    msg = "Main Scanner Connecting...";
                    UpdateUI(msg);
                    int ret = global.banner.Connect();
                    if (ret == 0)
                    {
                        global.banner.Start();
                        msg = "OK";
                        UpdateUI(msg);
                    }
                    else
                    {
                        msg = "Error";
                        UpdateUI(msg, "red");
                        m_errorCnt++;
                    }
                });
            return 0;
        }
        private async Task<int> SubScanner()
        {
            await Task.Run(() =>
            {
                msg = "Sub Scanner Scan";
                UpdateUI(msg);
                global.udp.Print();
                global.udp.Scan();
                Thread.Sleep(1000);
                var devices = global.udp.m_deviceTable;
                Log.Information(devices.Count().ToString());
                if(devices.Count() != 3 )
                {
                    msg = "Sub Scanner Count Error";
                    UpdateUI(msg, "red");
                    m_errorCnt++;
                }

                //// todo refactory
                string[] names = { "", "", "", "", "", "" };
                int i = 0;
                foreach (var device in devices)
                {
                    //names[i] = device.Key.Replace("MAC=", "").Replace("PORT=54321","");
                    //names[i] = device.Key.Replace("MAC=", "").Replace("PORT=54321","") + "  IP" + device.Value.Key;
                    names[i] = device.Value.Key;
                    msg = device.Value.Key + " FIND ";
                    UpdateUI(msg);
                    i++;
                }
                i = 1;
                foreach (var device in devices)
                {
                    msg = "Sub Scanner Connect " + i.ToString();
                    UpdateUI(msg);
                    global.udp.StartScaner(device.Value.Key, 54321, "Scanner_" + i.ToString());
                    Thread.Sleep(100);
                }

                foreach (var scanner in global.udp.m_scaner)
                {
                    msg = "Sub Scanner " + scanner.m_ip;
                    UpdateUI(msg);
                    if (scanner.m_isCon)
                    {
                        msg = " Connection OK";
                        UpdateUI(msg);
                    }
                    else
                    {
                        msg = " Connection ERROR";
                        UpdateUI(msg, "red");
                        m_errorCnt++;
                    }
                }
            });
            return 0;
        }

        private async Task<int> ConnectPLC()
        {
            await Task.Run(() =>
            {
                msg = "Connect MDS";
                UpdateUI(msg);
#if DEBUG
                global.md.m_host = "127.0.0.1";
#endif
                msg = "MDS HOST: " + global.md.m_host + " PORT: " + global.md.m_port;
                UpdateUI(msg);
                if (global.md.Connection() == -1)
                {
                    msg = "Connection Error";
                    UpdateUI(msg, "red");
                    m_errorCnt++;
                }
                else
                {
                    msg = "Connection OK";
                    UpdateUI(msg);
                    //global.md.StartClient();
                    Log.Information("Init MDS");
                    global.md.CancelPID();
                    Log.Information("ReadMDS");
                    global.md.ReadMDS();
                }
            });
            return 0;
        }

        private async Task<int> PrinterSetting()
        {
            await Task.Run(() =>
            {
                msg = "Printer Setting";
                UpdateUI(msg);

                string host = "172.16.0.";
                int idx = 161;
                string port = "9100";
                for( int i = 0; i < 4; i++)
                {
                    global.m_printer[i].m_port = port;
#if DEBUG
                    global.m_printer[i].m_host = "172.16.0.13";
#else
                    global.m_printer[i].m_host = host + (idx + i).ToString();
#endif
                    global.m_printer[i].act0 = (int error, string data) =>
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Log.Information("Print" + i.ToString() + " error : " + error.ToString() + " data: " + data);
                            if (error == 0)
                            {
                                msg = "Print" + i.ToString() + " OK model: " + data;
                                UpdateUI(msg);
                            }
                            else
                            {
                                msg = "Print" + i.ToString() + " error : " + error.ToString() + " data: " + data;
                                UpdateUI(msg, "red");
                                m_errorCnt++;
                            }
                        });
                    };
                    global.m_printer[i].PrintConnect();
                    Thread.Sleep(500);
                }

            });
            return 0;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "Home";
        }
    }
}
