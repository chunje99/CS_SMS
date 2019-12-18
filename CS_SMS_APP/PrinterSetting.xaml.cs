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
using Windows.UI.Core;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class PrinterSetting : Page
    {
        public PrinterSetting()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private void Print1_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[0].m_port = Print1_Port.Text;
            global.m_printer[0].m_host = Print1_Host.Text;
            global.m_printer[0].act0 = (int error, string data) =>
            {
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Print1_Status, data);
                });
            };
            global.m_printer[0].PrintConnect();
        }
        private void Print2_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[1].m_port = Print2_Port.Text;
            global.m_printer[1].m_host = Print2_Host.Text;
            global.m_printer[1].act0 = (int error, string data) =>
            {
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Print2_Status, data);
                });
            };
            global.m_printer[1].PrintConnect();
        }
        private void Print3_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[2].m_port = Print3_Port.Text;
            global.m_printer[2].m_host = Print3_Host.Text;
            global.m_printer[2].act0 = (int error, string data) =>
            {
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Print3_Status, data);
                });
            };
            global.m_printer[2].PrintConnect();
        }
        private void Print4_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[3].m_port = Print4_Port.Text;
            global.m_printer[3].m_host = Print4_Host.Text;
            global.m_printer[3].act0 = (int error, string data) =>
            {
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Print4_Status, data);
                });
            };
            global.m_printer[3].PrintConnect();
        }


        private void Print1_Print_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[0].PrintSample("QRCode");
        }

        private void Print2_Print_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[1].PrintSample("QRCode2");
        }
        private void Print3_Print_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[2].PrintSample("Printer3");
        }

        private void Print4_Print_Click(object sender, RoutedEventArgs e)
        {
            global.m_printer[3].PrintSample("Printer4");
        }

        private async void UpdateUI(TextBlock tbox, string data)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                tbox.Text = data;
            });
        }
    }
}
