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
using Serilog;

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

        private async void UpdateUI(TextBlock tbox, string data)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                tbox.Text = data;
            });
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "프린터 테스트";
        }

        private void Sample_Print_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var num = Convert.ToInt32(button.Tag.ToString());
            global.m_printer[num].PrintSample("Sample");
        }

        private void Chute_Print_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var num = Convert.ToInt32(button.Tag.ToString());
            ///QRCode 12개
            global.m_printer[num].PrintSample("QRCode2");
        }

        private void Chute2_Print_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var num = Convert.ToInt32(button.Tag.ToString());
            ///QRCode 12개
            global.m_printer[num].PrintSample("QRCodeIndic");
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var baseName = "Print" + button.Tag.ToString() + "_";
            var num = Convert.ToInt32(button.Tag.ToString());
            var port = ((TextBox)FindName(baseName + "Port")).Text;
            var host = ((TextBox)FindName(baseName + "Host")).Text;
            var Status = (TextBlock)FindName(baseName + "Status");
            global.m_printer[num].m_port = port;
            global.m_printer[num].m_host = host;
            global.m_printer[num].act0 = (int error, string data) =>
            {
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Status, data);
                });
            };
            global.m_printer[num].PrintConnect();
        }
    }
}
