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
using Serilog;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class ConnectPLC : Page
    {
        public ConnectPLC()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private void PLC_Connect_Click(object sender, RoutedEventArgs e)
        {
            global.md.m_host = PLC_Host.Text;
            global.md.m_port = Int32.Parse(PLC_Port.Text);
            if( global.md.Connection() == -1 )
            {
                PLC_Status.Text = global.md.m_error;
            }
            else
            {
                //global.md.StartClient();
                Log.Information("INIT MDS");
                global.md.CancelPID();
                Log.Information("ReadMDS");
                global.md.ReadMDS();
                PLC_Status.Text = "Connected";
            }
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = "PLC 연결";
            if(global.md.m_isCon)
            {
                PLC_Status.Text = "Connected";
            }
        }
    }
}
