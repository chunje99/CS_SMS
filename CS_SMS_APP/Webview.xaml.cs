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
    public sealed partial class Webview : Page
    {
        public Webview()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
#if DEBUG
            //Uri siteUri = new Uri("http://sms-admin.wtest.biz/config/chute_allocation.php");
            Uri siteUri = new Uri("https://naver.com");
            MyWebview.Source = siteUri;
#endif
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "슈트(셀) 관리";
            global.m_currentPage = this;
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Information("WebviewSize : {0} {1} {2} {3}", e.NewSize.Width, e.NewSize.Height, ((Frame)Window.Current.Content).ActualWidth, ((Frame)Window.Current.Content).ActualHeight );
            //WebviewGrid.Width = e.NewSize.Width;
            //WebviewGrid.Height = e.NewSize.Height;
            WebviewGrid.Width = ((Frame)Window.Current.Content).ActualWidth - global.m_paneLength;
            WebviewGrid.Height = ((Frame)Window.Current.Content).ActualHeight;
        }
    }
}
