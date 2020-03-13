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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainScanner : Page
    {
        public MainScanner()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private void Sacnner_Connect_Click(object sender, RoutedEventArgs e)
        {
            if ( Scanner_Status.Text == "Connected" )
            {
                global.banner.Close();
                Scanner_Status.Text = "Disconnected";
            }
            else
            {
                global.banner.m_ip = Scanner_Host.Text;
                global.banner.m_port = Int32.Parse(Scanner_Port.Text);
                int ret = global.banner.Connect();
                if (ret == 0)
                {
                    global.banner.Start();
                    Scanner_Status.Text = "Connected";
                }
            }
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "고정 스캐너";
            if(global.banner.m_isCon)
            {
                Scanner_Status.Text = "Connected";
            }
        }
    }
}
