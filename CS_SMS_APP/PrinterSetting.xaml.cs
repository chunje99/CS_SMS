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
using CS_SMS_LIB;
using System.Text;

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
            global.m_printPORT[0] = Print1_Port.Text;
            global.m_printIP[0] = Print1_Host.Text;
            global.PrintConnect(Print1_Host.Text, Print1_Port.Text, Print1_Status);
        }
        private void Print2_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printPORT[1] = Print2_Port.Text;
            global.m_printIP[1] = Print2_Host.Text;
            global.PrintConnect(Print2_Host.Text, Print2_Port.Text, Print2_Status);
        }
        private void Print3_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printPORT[2] = Print3_Port.Text;
            global.m_printIP[2] = Print3_Host.Text;
            global.PrintConnect(Print3_Host.Text, Print3_Port.Text, Print3_Status);
        }
        private void Print4_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.m_printPORT[3] = Print4_Port.Text;
            global.m_printIP[3] = Print4_Host.Text;
            global.PrintConnect(Print4_Host.Text, Print4_Port.Text, Print4_Status);
        }


        private void Print1_Print_Click(object sender, RoutedEventArgs e)
        {
            global.PrintSample(Print1_Host.Text, Print1_Port.Text, Print1_Status, "Printer1");
        }

        private void Print2_Print_Click(object sender, RoutedEventArgs e)
        {
            global.PrintSample(Print2_Host.Text, Print2_Port.Text, Print2_Status, "Printer2");
        }
        private void Print3_Print_Click(object sender, RoutedEventArgs e)
        {
            global.PrintSample(Print3_Host.Text, Print3_Port.Text, Print3_Status, "Printer3");
        }

        private void Print4_Print_Click(object sender, RoutedEventArgs e)
        {
            global.PrintSample(Print4_Host.Text, Print4_Port.Text, Print4_Status, "Printer4");
        }
    }
}
