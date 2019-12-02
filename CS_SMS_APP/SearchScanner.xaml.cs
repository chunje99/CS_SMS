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
            Debug.WriteLine("Click_Scan");
            global.udp.Print();
            global.udp.Scan();
            Thread.Sleep(1000);
            var devices = global.udp.m_deviceTable;
            Debug.WriteLine(devices.Count().ToString());
            //// todo refactory
            string[] names = { "", "", "", "", "", "" };
            int i = 0;
            foreach( var device in devices)
            {
                names[i] = device.Key.Replace("MAC=", "").Replace("PORT=54321","");
                i++;
            }
            Scanner_1.Text = names[0];
            Scanner_2.Text = names[1];
            Scanner_3.Text = names[2];
            Scanner_4.Text = names[3];
        }

        private void Click_Connection(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Connect Scanner");
            global.udp.StartScaner();
            foreach (var scaner in global.udp.m_scaner)
            {
                scaner.act0 = (name, barcode) =>
                {
                    Debug.WriteLine("==============");
                    Debug.WriteLine("MAIN");
                    Debug.WriteLine(name);
                    Debug.WriteLine(barcode);
                    global.m_msgQueue.Enqueue(barcode);
                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateUI(barcode);
                    });
                    Debug.WriteLine("==============");
                    //global.md.MakePID();
                };
            }
        }
        private async void UpdateUI(string barcode)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                //code to update UI
            });
        }
    }
}
