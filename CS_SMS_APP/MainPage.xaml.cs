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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    public class PlcData
    {
        public TextBlock m_control { get; set; } = null;
        public string m_name { get; set; } = "";
        public int m_location { get; set; } = -1;
        public PlcData( TextBlock control, string name, int location)
        {
            m_control = control;
            m_name = name;
            m_location = location;
        }

    }

    /// </summary>
    public class global
    {
        public static UDPer udp = new UDPer();
        public static CModbus md = new CModbus();
        public static CBanner banner = new CBanner();
        public static CApi api = new CApi();

        public static TextBox[] m_scanner = new TextBox[5];
        public static Queue<string> m_msgQueue = new Queue<string>();
        //public static List<PlcData> m_plcData = new List<PlcData>();
        public static List<string> m_printIP = new List<string>(new string[] {"", "", "", ""});
        public static List<string> m_printPORT = new List<string>(new string[] { "", "", "", "" });
        public global()
        {
        }
        /*
        static public int ChangeText( string txt, int location)
        {
            PlcData item = m_plcData.Find(x => x.m_location == location);
            if(item != null)
                if (item.m_location != -1)
                    item.m_control.Text = txt;
            return 0;
        }
        */
        static private string GetStatusMsg(int nStatus)
        {
            string errMsg = "";
            switch ((SLCS_ERROR_CODE)nStatus)
            {
                case SLCS_ERROR_CODE.ERR_CODE_NO_ERROR: errMsg = "No Error"; break;
                case SLCS_ERROR_CODE.ERR_CODE_NO_PAPER: errMsg = "Paper Empty"; break;
                case SLCS_ERROR_CODE.ERR_CODE_COVER_OPEN: errMsg = "Cover Open"; break;
                case SLCS_ERROR_CODE.ERR_CODE_CUTTER_JAM: errMsg = "Cutter jammed"; break;
                case SLCS_ERROR_CODE.ERR_CODE_TPH_OVER_HEAT: errMsg = "TPH overheat"; break;
                case SLCS_ERROR_CODE.ERR_CODE_AUTO_SENSING: errMsg = "Gap detection Error (Auto-sensing failure)"; break;
                case SLCS_ERROR_CODE.ERR_CODE_NO_RIBBON: errMsg = "Ribbon End"; break;
                case SLCS_ERROR_CODE.ERR_CODE_BOARD_OVER_HEAT: errMsg = "Board overheat"; break;
                case SLCS_ERROR_CODE.ERR_CODE_MOTOR_OVER_HEAT: errMsg = "Motor overheat"; break;
                case SLCS_ERROR_CODE.ERR_CODE_WAIT_LABEL_TAKEN: errMsg = "Waiting for the label to be taken"; break;
                case SLCS_ERROR_CODE.ERR_CODE_CONNECT: errMsg = "Port open error"; break;
                case SLCS_ERROR_CODE.ERR_CODE_GETNAME: errMsg = "Unknown (or Not supported) printer name"; break;
                case SLCS_ERROR_CODE.ERR_CODE_OFFLINE: errMsg = "Offline (The printer is in an error status)"; break;
                default: errMsg = "Unknown error"; break;
            }
            return errMsg;
        }

        public const int ISerial = 0;
        public const int IParallel = 1;
        public const int IUsb = 2;
        public const int ILan = 3;
        public const int IBluetooth = 5;

        static private bool ConnectPrinter(string host, string port, TextBlock status)
        {
            string strPort = "";
            int nInterface = ILan;
            int nBaudrate = 115200, nDatabits = 8, nParity = 0, nStopbits = 0;
            int nStatus = (int)SLCS_ERROR_CODE.ERR_CODE_NO_ERROR;

            if (false)
            {
                // USB
                //nInterface = IUsb;
            }
            else if (true)
            {
                // NETWORK
                //nInterface = ILan;
                strPort = host;
                nBaudrate = Convert.ToInt32(port);
            }

            nStatus = BXLLApi.ConnectPrinterEx(nInterface, strPort, nBaudrate, nDatabits, nParity, nStopbits);

            if (nStatus != (int)SLCS_ERROR_CODE.ERR_CODE_NO_ERROR)
            {
                BXLLApi.DisconnectPrinter();
                status.Text = GetStatusMsg(nStatus);
                return false;
            }
            return true;
        }


        static private string ByteToString(byte[] strByte)
        {
            string str = Encoding.Default.GetString(strByte);
            return str;
        }

        static public void PrintConnect(string host, string port, TextBlock status)
        {
            if (!ConnectPrinter(host, port, status))
                return;

            int lResult = 0;
            string strResult = "";

            // Get printer name (^PI0 command)
            string strCommand = "^PI0\r\n"; // 0x5e, 49, 30, 0x0d, 0x0a
            byte[] byCommand = Encoding.Default.GetBytes(strCommand);

            if (BXLLApi.WriteBuff(byCommand, byCommand.Length, ref lResult))
            {
                System.Threading.Thread.Sleep(100);
                byte[] byReadPrtName = new byte[32];
                if (BXLLApi.ReadBuff(byReadPrtName, byReadPrtName.Length, ref lResult) == true)
                {
                    strResult = ByteToString(byReadPrtName);
                    if(status != null)
                        status.Text = strResult;
                }
            }

            BXLLApi.DisconnectPrinter();

        }

        static private void SendPrinterSettingCommand()
        {
            string Width = "101.6";
            string Height = "152.4";
            string MarginX = "0";
            string MarginY = "0";
            string Density = "14";
            // 203 DPI : 1mm is about 7.99 dots
            // 300 DPI : 1mm is about 11.81 dots
            // 600 DPI : 1mm is about 23.62 dots
            int dotsPer1mm = (int)Math.Round((float)BXLLApi.GetPrinterDPI() / 25.4f);
            int nPaper_Width = Convert.ToInt32(double.Parse(Width) * dotsPer1mm);
            int nPaper_Height = Convert.ToInt32(double.Parse(Height) * dotsPer1mm);
            int nMarginX = Convert.ToInt32(double.Parse(MarginX) * dotsPer1mm);
            int nMarginY = Convert.ToInt32(double.Parse(MarginY) * dotsPer1mm);
            int nSpeed = (int)SLCS_PRINT_SPEED.PRINTER_SETTING_SPEED;
            int nDensity = Convert.ToInt32(Density);
            int nOrientation = (int)SLCS_ORIENTATION.TOP2BOTTOM;

            int nSensorType = (int)SLCS_MEDIA_TYPE.GAP;


            //	Clear Buffer of Printer
            BXLLApi.ClearBuffer();


            //	Set Label and Printer
            BXLLApi.SetConfigOfPrinter(nSpeed, nDensity, nOrientation, false, 1, true);

            // Select international character set and code table.To
            BXLLApi.SetCharacterset((int)SLCS_INTERNATIONAL_CHARSET.ICS_KOREA, (int)SLCS_CODEPAGE.FCP_CP1252);

            /* 
                1 Inch : 25.4mm
                1 mm   :  7.99 Dots (XT5-40, SLP-TX400, SLP-DX420, SLP-DX220, SLP-DL410, SLP-T400, SLP-D420, SLP-D220, SRP-770/770II/770III)
                1 mm   :  7.99 Dots (SPP-L310, SPP-L410, SPP-L3000, SPP-L4000) 
                1 mm   :  7.99 Dots (XD3-40d, XD3-40t, XD5-40d, XD5-40t, XD5-40LCT)
                1 mm   : 11.81 Dots (XT5-43, SLP-TX403, SLP-DX423, SLP-DX223, SLP-DL413, SLP-T403, SLP-D423, SLP-D223)
                1 mm   : 11.81 Dots (XD5-43d, XD5-43t, XD5-43LCT)
                1 mm   : 23.62 Dots (XT5-46)
            */

            BXLLApi.SetPaper(nMarginX, nMarginY, nPaper_Width, nPaper_Height, nSensorType, 0, 2 * dotsPer1mm);
        }
        static public void PrintSample(string host, string port, TextBlock status, string printer)
        {
            if (!ConnectPrinter(host, port, status))
                return;

            int multiplier = 1;

            int resolution = BXLLApi.GetPrinterDPI();
            int dotsPer1mm = (int)Math.Round((float)resolution / 25.4f);
            if (resolution >= 600)
                multiplier = 3;

            SendPrinterSettingCommand();

            // Prints string using TrueFont
            BXLLApi.PrintTrueFont(2 * dotsPer1mm, 5 * dotsPer1mm, "Arial", 14, 0, true, true, false, "Test Label", false);

            //	Draw Lines
            BXLLApi.PrintBlock(1 * dotsPer1mm, 10 * dotsPer1mm, 71 * dotsPer1mm, 11 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.LINE_OVER_WRITING, 0);

            //Print string using Vector Font
            BXLLApi.PrintDeviceFont(2 * dotsPer1mm, 12 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "배송지:" + printer);
            BXLLApi.PrintDeviceFont(2 * dotsPer1mm, 17 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "CJ 대한통운");

            BXLLApi.PrintDeviceFont(3 * dotsPer1mm, 24 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "바코드 CODE39");
            BXLLApi.Print1DBarcode(3 * dotsPer1mm, 28 * dotsPer1mm, (int)SLCS_BARCODE.CODE39, 4 * multiplier, 6 * multiplier, 48 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, "1234567890");
            BXLLApi.PrintDeviceFont(3 * dotsPer1mm, 44 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "바코드 CODE128");
            BXLLApi.Print1DBarcode(3 * dotsPer1mm, 48 * dotsPer1mm, (int)SLCS_BARCODE.CODE128, 4 * multiplier, 6 * multiplier, 48 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, "1234567890abcdefg");

            //	Print Command
            BXLLApi.Prints(1, 1);

            // Disconnect printer
            BXLLApi.DisconnectPrinter();

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
            NavView.MenuItems.Add(new NavigationViewItemSeparator());
            NavView.MenuItems.Add(new NavigationViewItem()
            { Content = "My content", Icon = new SymbolIcon(Symbol.Folder), Tag = "content" });

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
                case "printer":
                    ContentFrame.Navigate(typeof(PrinterSetting));
                    break;

            }
        }
    }
}
