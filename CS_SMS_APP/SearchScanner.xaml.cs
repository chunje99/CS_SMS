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
using Windows.UI.Popups;
using System.Threading;
using System.Diagnostics;
using Windows.UI.Core;
using Serilog;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class SearchScanner : Page
    {
        public SearchScanner()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void CommandInvokedHandler(IUICommand command)
        {
            // Display message showing the label of the command that was invoked
        }
        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create the message dialog and set its content
            string data = "Host : ";
            var messageDialog = new MessageDialog(data);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                "Try again",
                new UICommandInvokedHandler(this.CommandInvokedHandler)));
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

        private void Click_Scan(object sender, RoutedEventArgs e)
        {
            Log.Information("Click_Scan");
            global.udp.Print();
            global.udp.Scan();
            Thread.Sleep(1000);
            var devices = global.udp.m_deviceTable;
            Log.Information(devices.Count().ToString());
            //// todo refactory
            string[] names = { "", "", "", "", "", "" };
            int i = 0;
            foreach( var device in devices)
            {
                //names[i] = device.Key.Replace("MAC=", "").Replace("PORT=54321","");
                //names[i] = device.Key.Replace("MAC=", "").Replace("PORT=54321","") + "  IP" + device.Value.Key;
                names[i] = device.Value.Key;
                i++;
            }
            Scanner_1.Text = names[0];
            Scanner_2.Text = names[1];
            Scanner_3.Text = names[2];
            Scanner_4.Text = names[3];
        }

        private void Click_Connection(object sender, RoutedEventArgs e)
        {
            Log.Information("Connect Scanner");
            //global.udp.StartScaner();
            if (Scanner_1.Text != "")
                global.udp.StartScaner(Scanner_1.Text, 54321, "Scanner_1");
            if(Scanner_2.Text != "" )
                global.udp.StartScaner(Scanner_2.Text, 54321, "Scanner_2");
            if(Scanner_3.Text != "" )
                global.udp.StartScaner(Scanner_3.Text, 54321, "Scanner_3");
            if(Scanner_4.Text != "" )
                global.udp.StartScaner(Scanner_4.Text, 54321, "Scanner_4");

            foreach (var scanner in global.udp.m_scaner)
            {
                if (scanner.m_isCon)
                {
                    scanner.act0 = (name, chute_num, barcode) =>
                    {
                        Log.Information("==============");
                        Log.Information("MAIN");
                        Log.Information(name);
                        Log.Information(barcode);
                        //global.m_msgQueue.Enqueue(barcode);
                        //var devices = global.udp.m_deviceTable;
                        var ig = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                UpdateUI(name, chute_num, barcode);
                            });
                        Log.Information("==============");
                    };

                    //global.md.MakePID();
                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateUI(scanner.m_name, 0, "접속");
                    });
                }

            }
        }
        private async void UpdateUI(string name, int chute_num, string barcode)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (name == "Scanner_1")
                    ScannerStat_1.Text = chute_num.ToString() + " : "  + barcode;
                if (name == "Scanner_2")
                    ScannerStat_2.Text = chute_num.ToString() + " : "  + barcode;
                if (name == "Scanner_3")
                    ScannerStat_3.Text = chute_num.ToString() + " : "  + barcode;
                if (name == "Scanner_4")
                    ScannerStat_4.Text = chute_num.ToString() + " : "  + barcode;
                //code to update UI
            });
        }
        private async void UpdateUIIP(string name, string ip)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (name == "Scanner_1")
                    Scanner_1.Text = ip;
                if (name == "Scanner_2")
                    Scanner_2.Text = ip;
                if (name == "Scanner_3")
                    Scanner_3.Text = ip;
                if (name == "Scanner_4")
                    Scanner_4.Text = ip;
                //code to update UI
            });
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = "핸드 스캐너";
            foreach (var scanner in global.udp.m_scaner)
            {
                if (scanner.m_isCon)
                {
                    scanner.act0 = (name, chute_num, barcode) =>
                    {
                        Log.Information("==============");
                        Log.Information("MAIN");
                        Log.Information(name);
                        Log.Information(barcode);
                        //global.m_msgQueue.Enqueue(barcode);
                        //var devices = global.udp.m_deviceTable;
                        var ig = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                UpdateUI(name, chute_num, barcode);
                            });
                        Log.Information("==============");
                    };

                    //global.md.MakePID();
                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateUI(scanner.m_name, 0, "접속");
                        UpdateUIIP(scanner.m_name, scanner.m_ip);
                    });
                }

            }

        }
    }
}
