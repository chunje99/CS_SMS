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
using Windows.UI.Core;
using System.Collections.ObjectModel;
using CS_SMS_LIB;
using Windows.System;
using Windows.UI.Popups;
using System.Threading.Tasks;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class SearchBox : Page
    {
        ObservableCollection<BoxData> boxList = new ObservableCollection<BoxData>();
        ObservableCollection<PrintData> printList = new ObservableCollection<PrintData>();
        public PrintList lastPrintList = null;
        public SearchBox()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            SearchBox_Key.SelectedIndex = 0;
        }


        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            SetSUBScanner();
            BoxList bList = await global.api.Box("box_num", "1", "20191218");
            boxList.Clear();
            BoxData t;
            int i = 0;
            foreach ( BoxData b in bList.list)
            {
                t = new BoxData(b);
                t.index = i++;
                boxList.Add(t);
            }
        }

        private void OnChange(object sender, SelectionChangedEventArgs e)
        {
            SearchBox_Value.Text = "";
        }

        private async void Alert(string msg)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);

           // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
           // messageDialog.Commands.Add(new UICommand(
           //     "Try again",
           //     new UICommandInvokedHandler(this.CommandInvokedHandler)));
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

        private void CommandInvokedHandler(IUICommand command)
        {
            // Display message showing the label of the command that was invoked
        }

        private void SetSUBScanner()
        {
            foreach (var scanner in global.udp.m_scaner)
            {
                scanner.act0 = (string name, int chute_num, string barcode ) =>
                {
                    if (chute_num == -1)
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Alert("스캐너를 할당하세요!(" + barcode + ")");
                        });
                    }
                    else if (chute_num == 0)
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            SearchBox_Value.Text = barcode;
                        });
                    }
                    else
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Alert("(" + barcode + ") 메인에서만 작동합니다.");
                        });
                    }
                };
            }
            return;
        }

        private async void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if( e.Key == VirtualKey.Enter )
            {
                var key = SearchBox_Key.SelectedItem as string;
                var value = SearchBox_Value.Text;
                string searchKey = "";
                /// <param name="searchKey"></param>  box_num, cust_cd, cust_nm
                if (key == "박스번호")
                    searchKey = "box_num";
                else if( key == "박스바코드" )
                    searchKey = "box_barcode";
                else if( key == "거래처별" )
                    searchKey = "box_barcode";
                BoxList bList = await global.api.Box(searchKey, value, "");
                boxList.Clear();
                BoxData t;
                int i = 0;
                foreach (BoxData b in bList.list)
                {
                    t = new BoxData(b);
                    t.index = i++;
                    boxList.Add(t);
                }

                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                   Alert("KEY + " + key + " value = " + SearchBox_Value.Text);
                });
            }
        }

        private async void DetailView(object sender, RoutedEventArgs e)
        {
            Button t = sender as Button;
            Log.Information("Detailview {0}", t.Name);
            Log.Information( BoxListView.Items[Int32.Parse(t.Name)].GetType().ToString() );
            BoxData b = BoxListView.Items[Int32.Parse(t.Name)] as BoxData;
            Log.Information(b.cust_nm);
            printList.Clear();
            lastPrintList = await global.api.Print(Int32.Parse(b.chute_num), b.job_dt, b.box_num);
            foreach( PrintData p in lastPrintList.list)
            {
                printList.Add(p);
            }
            Preview.ShowAt((FrameworkElement)sender);
        }

        private void RePrint(object sender, RoutedEventArgs e)
        {
            Log.Information("재출력 ");
            global.m_printer[0].PrintData(lastPrintList);
        }
    }
}
