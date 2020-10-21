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
    public sealed partial class Indicator : Page
    {
        public Indicator()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "표시기";
            if(global.mqc.isActive)
            {
                IND_GW_Status.Text = "Active";
            }
            else
            {
                IND_GW_Status.Text = "Inactive";
            }
        }

        private void IND1_ON_Click(object sender, RoutedEventArgs e)
        {
            if(global.mqc.isConnect)
            {
                int id1 = Convert.ToInt32(IND1_ID.Text);
                int id2 = Convert.ToInt32(IND2_ID.Text);
                global.mqc.ind_on_req("F8C6FC", id1, id2);
                IND_GW_Status.Text = "IND ON";
            }
            else
            {
                IND_GW_Status.Text = "disconnected";
            }
        }

        private void IND1_OFF_Click(object sender, RoutedEventArgs e)
        {
            if(global.mqc.isConnect)
            {
                List<string> ind_off = new List<string> { "F8C6FC" };
                global.mqc.ind_off_req(ind_off);
                IND_GW_Status.Text = "IND OFF";
            }
            else
            {
                IND_GW_Status.Text = "disconnected";
            }
        }
    }
}
