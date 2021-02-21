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
using Serilog;
using Windows.UI.ViewManagement;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace CS_SMS_APP
{
    /// </summary>
    public class global
    {
        public static CMqttApi mqc = new CMqttApi();
        public static UDPer udp = new UDPer();
        public static CModbus md = new CModbus();
        public static CBanner banner = new CBanner();
        public static CApi api = new CApi();

        public static TextBox[] m_scanner = new TextBox[5];
        public static Queue<string> m_msgQueue = new Queue<string>();
        public static List<CPrinter> m_printer = new List<CPrinter>(new CPrinter[] { new CPrinter(), new CPrinter(), new CPrinter(), new CPrinter()});
        public static List<int> m_matchPrintChute = new List<int>(new int[64]);
        public static List<int> m_matchPrintRack = new List<int>(new int[32]);
        public static TextBlock m_mainTopTB = new TextBlock();
        public static string m_mainTopPrefix = "DAS_카카오분류-";
        public static Page m_currentPage = new Page();
        public static double m_paneLength = 240;
        static global()
        {
            ///Chute 프린터 배치
            m_matchPrintChute[1] = 0;
            m_matchPrintChute[3] = 0;

            m_matchPrintChute[2] = 1;
            m_matchPrintChute[4] = 1;

            m_matchPrintChute[5] = 2;
            m_matchPrintChute[7] = 2;

            m_matchPrintChute[6] = 3;
            m_matchPrintChute[8] = 3;
            ///Rack 프린터 배치
            m_matchPrintRack[1] = 0;
            m_matchPrintRack[2] = 2;
        }
    }

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "Home";
            global.udp.Start();
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(1600, 960);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(Setting));
            }
            else
            {
                // find NavigationViewItem with Content that equals InvokedItem
                var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                NavView_Navigate(item as NavigationViewItem);
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // you can also add items in code behind
            //NavView.MenuItems.Add(new NavigationViewItemSeparator());
            //NavView.MenuItems.Add(new NavigationViewItem()
            //{ Content = "My content", Icon = new SymbolIcon(Symbol.Folder), Tag = "content" });

            // set the initial SelectedItem 
            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem && item.Tag.ToString() == "home")
                {
                    NavView.SelectedItem = item;
                    break;
                }
            }
            ContentFrame.Navigate(typeof(Home));
        }
        private void NavView_Navigate(NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    ContentFrame.Navigate(typeof(Home));
                    break;
                case "scanner":
                    ContentFrame.Navigate(typeof(MainScanner));
                    break;
                case "scanners":
                    ContentFrame.Navigate(typeof(SearchScanner));
                    break;
                case "plc":
                    ContentFrame.Navigate(typeof(ConnectPLC));
                    break;
                case "monitoring":
                    ContentFrame.Navigate(typeof(Monitoring));
                    break;
                case "searchbox":
                    ContentFrame.Navigate(typeof(SearchBox));
                    break;
                case "printer":
                    ContentFrame.Navigate(typeof(PrinterSetting));
                    break;
                case "webview":
                    ContentFrame.Navigate(typeof(Webview));
                    break;
                case "indicator":
                    ContentFrame.Navigate(typeof(Indicator));
                    break;
                case "logs":
                    ContentFrame.Navigate(typeof(Logs));
                    break;

            }
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Information("PageSize : {0} {1} {2} {3}", 
                   e.NewSize.Width, e.NewSize.Height, NavView.OpenPaneLength, NavView.IsPaneOpen);
            ContentFrame.Width = e.NewSize.Width;
            ContentFrame.Height = e.NewSize.Height - 10;
            MainGrid.Height = e.NewSize.Height - 10;
            if (NavView.IsPaneOpen)
                global.m_paneLength = NavView.OpenPaneLength;
            else
                global.m_paneLength = 0;
            global.m_currentPage.Width = e.NewSize.Width;
            global.m_currentPage.Height = e.NewSize.Height - 10;
        }
    }
}
