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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace CS_SMS_APP
{
    /// </summary>
    public class global
    {
        public static UDPer udp = new UDPer();
        public static CModbus md = new CModbus();
        public static CBanner banner = new CBanner();
        public static CApi api = new CApi();

        public static TextBox[] m_scanner = new TextBox[5];
        public static Queue<string> m_msgQueue = new Queue<string>();
        public static List<CPrinter> m_printer = new List<CPrinter>(new CPrinter[] { new CPrinter(), new CPrinter(), new CPrinter(), new CPrinter()});
        public global()
        {
        }
    }

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            global.udp.Start();
            this.InitializeComponent();
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
                    MainTopText.Text = "Home";
                    ContentFrame.Navigate(typeof(Home));
                    break;
                case "scanner":
                    MainTopText.Text = "고정 스캐너";
                    ContentFrame.Navigate(typeof(MainScanner));
                    break;
                case "scanners":
                    MainTopText.Text = "바코드 스캐너";
                    ContentFrame.Navigate(typeof(SearchScanner));
                    break;
                case "plc":
                    MainTopText.Text = "PLC 접속";
                    ContentFrame.Navigate(typeof(ConnectPLC));
                    break;
                case "monitoring":
                    MainTopText.Text = "작업";
                    ContentFrame.Navigate(typeof(Monitoring));
                    break;
                case "searchbox":
                    MainTopText.Text = "박스 검색";
                    ContentFrame.Navigate(typeof(SearchBox));
                    break;
                case "printer":
                    MainTopText.Text = "프린트 테스트";
                    ContentFrame.Navigate(typeof(PrinterSetting));
                    break;
                case "webview":
                    MainTopText.Text = "거래처 할당";
                    ContentFrame.Navigate(typeof(Webview));
                    break;
                case "logs":
                    MainTopText.Text = "LOGS";
                    ContentFrame.Navigate(typeof(Logs));
                    break;

            }
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Information("PageSize : {0} {1}", e.NewSize.Width, e.NewSize.Height);
            ContentFrame.Width = e.NewSize.Width;
            ContentFrame.Height = e.NewSize.Height - 10;
            MainGrid.Height = e.NewSize.Height - 10;
        }
    }
}
