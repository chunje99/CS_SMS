using System;
using System.Collections.Generic;
using System.Text;

namespace CS_SMS_LIB
{
    public class CPrinter
    {
        public string m_host { get; set; }
        public string m_port { get; set; }

        public CPrinter()
        {
        }

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

        public Action<int, string> act0 { get; set; } = null;

        private bool ConnectPrinter()
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
                strPort = m_host;
                nBaudrate = Convert.ToInt32(m_port);
            }

            nStatus = BXLLApi.ConnectPrinterEx(nInterface, strPort, nBaudrate, nDatabits, nParity, nStopbits);

            if (nStatus != (int)SLCS_ERROR_CODE.ERR_CODE_NO_ERROR)
            {
                BXLLApi.DisconnectPrinter();
                if(act0 != null)
                    act0(nStatus, GetStatusMsg(nStatus));
                return false;
            }
            return true;
        }


        static private string ByteToString(byte[] strByte)
        {
            string str = Encoding.Default.GetString(strByte);
            return str;
        }

        public void PrintConnect()
        {
            if (!ConnectPrinter())
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
                    if (act0 != null)
                        act0(0, strResult);
                }
            }

            BXLLApi.DisconnectPrinter();

        }

        static private void SendPrinterSettingCommand()
        {
            //string Width = "101.6";
            //string Height = "152.4";
            string Width = "100";
            string Height = "80";
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
            //BXLLApi.SetConfigOfPrinter(nSpeed, nDensity, nOrientation, false, 1, true);
            BXLLApi.SetConfigOfPrinter(nSpeed, nDensity, nOrientation, true, 1, true);

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

        public class cnc
        {
            public string code;
            public string name;
            public string cnt;
            public cnc(string a, string b, string c )
            {
                code = a;
                name = b;
                cnt = c;
            }
        }

        public void PrintData(string printer)
        {
            if (!ConnectPrinter())
                return;

            int multiplier = 1;

            int resolution = BXLLApi.GetPrinterDPI();
            int dotsPer1mm = (int)Math.Round((float)resolution / 25.4f);
            if (resolution >= 600)
                multiplier = 3;

            SendPrinterSettingCommand();

            List<cnc> t = new List<cnc>();
            t.Add(new cnc("FRPBMZGPP0019", "4컬러+샤프 멀티펜_AP_스케치북 4컬로 샤프입니다. 가나다라 마바사 아자카차차+2", "123"));
            t.Add(new cnc("FRPBMZGPP0019", "123456789012345678901234567890123456789012345678901234567890", "123"));
            t.Add(new cnc("FRPBMZGPP0019", "4컬러+샤프 멀티펜_AP_스케치북", "123"));

            for (int j = 0; j < 3 ; j++)
            {

                // Prints string using TrueFont
                //BXLLApi.PrintTrueFont(40 * dotsPer1mm, 5 * dotsPer1mm, "Arial", 20, 0, true, true, false, "여의도 CGV점(F020)", false);
                //BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 5 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "여의도 CGV점 (F020)");
                BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 2 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, 2, 2, (int)SLCS_ROTATION.ROTATE_0, true, "여의도 CGV점 (F020)");
                //BXLLApi.PrintVectorFont(40 * dotsPer1mm, 5* dotsPer1mm, string FontSelection, int FontWidth, int FontHeight, string RightSideCharSpacing, bool bBold, bool ReversePrinting, bool TextStyle, int Rotation, string TextAlignment, int TextDirection, string pData)

                //	Draw Lines
                //BXLLApi.PrintBlock(5 * dotsPer1mm, 11 * dotsPer1mm, 95 * dotsPer1mm, 12 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.LINE_OVER_WRITING, 0);
                BXLLApi.PrintBlock(1 * dotsPer1mm, 1 * dotsPer1mm, 99 * dotsPer1mm, 12 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.BOX, 1);


                //Print string using Vector Font
                //BXLLApi.PrintVectorFont(5*dotsPer1mm, 16*dotsPer1mm, "K", 34, 34, "0", false, false, false, (int)SLCS_ROTATION.ROTATE_0, SLCS_FONT_ALIGNMENT.LEFTALIGN.ToString(), (int)SLCS_FONT_DIRECTION.LEFTTORIGHT, "Sample Label-2");
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "상품코드");
                BXLLApi.PrintDeviceFont(43 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "상품명");
                BXLLApi.PrintDeviceFont(85 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "EA");

                int i = 0;
                foreach (var item in t)
                {
                    BXLLApi.PrintDeviceFont(3 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, item.code);
                    if (item.name.Length <= 25)
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, item.name);
                    else if (item.name.Length > 25 && item.name.Length < 50)
                    {
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(0, 25));
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (26 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(25, item.name.Length - 25));
                    }
                    else
                    {
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(0, 25));
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (26 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(25, 25));
                    }
                    BXLLApi.PrintDeviceFont(85 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, item.cnt);
                    i++;
                }

                /*
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 22 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "69816");
                BXLLApi.PrintDeviceFont(35 * dotsPer1mm, 22 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "슬리퍼");
                BXLLApi.PrintDeviceFont(85 * dotsPer1mm, 22 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "3");

                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 32 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "69816");
                BXLLApi.PrintDeviceFont(35 * dotsPer1mm, 32 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "슬리퍼");
                BXLLApi.PrintDeviceFont(85 * dotsPer1mm, 32 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "3");

                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 42 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "69816");
                BXLLApi.PrintDeviceFont(35 * dotsPer1mm, 42 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "슬리퍼");
                BXLLApi.PrintDeviceFont(85 * dotsPer1mm, 42 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "3");
                */

                BXLLApi.Print1DBarcode(25 * dotsPer1mm, 55 * dotsPer1mm, (int)SLCS_BARCODE.CODE128, 2 * multiplier, 3 * multiplier, 50 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, "MPIBR1003F0200017");
                BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 62 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "MPIBR1003F0200017");

                BXLLApi.PrintBlock(1 * dotsPer1mm, 66 * dotsPer1mm, 99 * dotsPer1mm, 67 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.BOX, 1);

                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "No.");
                BXLLApi.PrintDeviceFont(15 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "13");
                BXLLApi.PrintDeviceFont(60 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "배송");
                BXLLApi.PrintDeviceFont(72 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "2019-11-27");
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "LOC");
                BXLLApi.PrintDeviceFont(15 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "M01-0101");
                BXLLApi.PrintDeviceFont(60 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "차수");
                BXLLApi.PrintDeviceFont(72 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "1");


                /*
                BXLLApi.PrintDeviceFont(2 * dotsPer1mm, 17 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "CJ 대한통운");

                BXLLApi.PrintDeviceFont(3 * dotsPer1mm, 24 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "바코드 CODE39");
                BXLLApi.Print1DBarcode(3 * dotsPer1mm, 28 * dotsPer1mm, (int)SLCS_BARCODE.CODE39, 4 * multiplier, 6 * multiplier, 48 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, "1234567890");
                BXLLApi.PrintDeviceFont(3 * dotsPer1mm, 44 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "바코드 CODE128");
                BXLLApi.Print1DBarcode(3 * dotsPer1mm, 48 * dotsPer1mm, (int)SLCS_BARCODE.CODE128, 4 * multiplier, 6 * multiplier, 48 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, "1234567890abcdefg");
                */

                //	Print Command
                BXLLApi.Prints(1, 1);
            }

            // Disconnect printer
            BXLLApi.DisconnectPrinter();

        }

        public void PrintData(PrintList printList)
        {

            if (!ConnectPrinter())
                return;

            int multiplier = 1;

            int resolution = BXLLApi.GetPrinterDPI();
            int dotsPer1mm = (int)Math.Round((float)resolution / 25.4f);
            if (resolution >= 600)
                multiplier = 3;

            SendPrinterSettingCommand();

            for (int j = 0; j < printList.list.Count ;)
            {

                // Prints string using TrueFont
                BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 2 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, 2, 2, (int)SLCS_ROTATION.ROTATE_0, true,
                    printList.cust_cd + " (" + printList.cust_nm + ")");

                //	Draw Lines
                BXLLApi.PrintBlock(1 * dotsPer1mm, 1 * dotsPer1mm, 99 * dotsPer1mm, 12 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.BOX, 1);


                //Print string using Vector Font
                //BXLLApi.PrintVectorFont(5*dotsPer1mm, 16*dotsPer1mm, "K", 34, 34, "0", false, false, false, (int)SLCS_ROTATION.ROTATE_0, SLCS_FONT_ALIGNMENT.LEFTALIGN.ToString(), (int)SLCS_FONT_DIRECTION.LEFTTORIGHT, "Sample Label-2");
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "상품코드");
                BXLLApi.PrintDeviceFont(43 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "상품명");
                BXLLApi.PrintDeviceFont(85 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "EA");

                for(int i = 0; i < 3 && j < printList.list.Count; i++, j++)
                {
                    string sku_cd = printList.list[j].sku_cd;
                    string sku_nm = printList.list[j].sku_nm;
                    string cnt = printList.list[j].cnt;
                    BXLLApi.PrintDeviceFont(3 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, sku_cd );
                    if (sku_nm.Length <= 25)
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, sku_nm);
                    else if (sku_nm.Length > 25 && sku_nm.Length < 50)
                    {
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            sku_nm.Substring(0, 25));
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (26 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            sku_nm.Substring(25, sku_nm.Length - 25));
                    }
                    else
                    {
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            sku_nm.Substring(0, 25));
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (26 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            sku_nm.Substring(25, 25));
                    }
                    BXLLApi.PrintDeviceFont(85 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, cnt);
                }


                BXLLApi.Print1DBarcode(25 * dotsPer1mm, 55 * dotsPer1mm, (int)SLCS_BARCODE.CODE128, 2 * multiplier, 3 * multiplier, 50 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, 
                    printList.barcode);
                BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 62 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, 
                    printList.barcode);

                BXLLApi.PrintBlock(1 * dotsPer1mm, 66 * dotsPer1mm, 99 * dotsPer1mm, 67 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.BOX, 1);

                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "No.");
                BXLLApi.PrintDeviceFont(15 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, printList.no);
                BXLLApi.PrintDeviceFont(60 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "배송");
                BXLLApi.PrintDeviceFont(72 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, printList.delivery_date);
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "LOC");
                BXLLApi.PrintDeviceFont(15 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, printList.loc);
                BXLLApi.PrintDeviceFont(60 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "차수");
                BXLLApi.PrintDeviceFont(72 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, printList.no2);

                //	Print Command
                BXLLApi.Prints(1, 1);
            }

            // Disconnect printer
            BXLLApi.DisconnectPrinter();

        }

        public void PrintSample(string printer)
        {
            if(printer == "QRCode")
            {
                PrintChute();
                return;
            }

            if (!ConnectPrinter())
                return;

            int multiplier = 1;

            int resolution = BXLLApi.GetPrinterDPI();
            int dotsPer1mm = (int)Math.Round((float)resolution / 25.4f);
            if (resolution >= 600)
                multiplier = 3;

            SendPrinterSettingCommand();

            List<cnc> t = new List<cnc>();
            t.Add(new cnc("FRPBMZGPP0019", "4컬러+샤프 멀티펜_AP_스케치북 4컬로 샤프입니다. 가나다라 마바사 아자카차차+2", "123"));
            t.Add(new cnc("FRPBMZGPP0019", "123456789012345678901234567890123456789012345678901234567890", "123"));
            t.Add(new cnc("FRPBMZGPP0019", "4컬러+샤프 멀티펜_AP_스케치북", "123"));

            for (int j = 0; j < 1 ; j++)
            {

                // Prints string using TrueFont
                //BXLLApi.PrintTrueFont(40 * dotsPer1mm, 5 * dotsPer1mm, "Arial", 20, 0, true, true, false, "여의도 CGV점(F020)", false);
                //BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 5 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "여의도 CGV점 (F020)");
                BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 2 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, 2, 2, (int)SLCS_ROTATION.ROTATE_0, true, "여의도 CGV점 (F020)");
                //BXLLApi.PrintVectorFont(40 * dotsPer1mm, 5* dotsPer1mm, string FontSelection, int FontWidth, int FontHeight, string RightSideCharSpacing, bool bBold, bool ReversePrinting, bool TextStyle, int Rotation, string TextAlignment, int TextDirection, string pData)

                //	Draw Lines
                //BXLLApi.PrintBlock(5 * dotsPer1mm, 11 * dotsPer1mm, 95 * dotsPer1mm, 12 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.LINE_OVER_WRITING, 0);
                BXLLApi.PrintBlock(1 * dotsPer1mm, 1 * dotsPer1mm, 99 * dotsPer1mm, 12 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.BOX, 1);


                //Print string using Vector Font
                //BXLLApi.PrintVectorFont(5*dotsPer1mm, 16*dotsPer1mm, "K", 34, 34, "0", false, false, false, (int)SLCS_ROTATION.ROTATE_0, SLCS_FONT_ALIGNMENT.LEFTALIGN.ToString(), (int)SLCS_FONT_DIRECTION.LEFTTORIGHT, "Sample Label-2");
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "상품코드");
                BXLLApi.PrintDeviceFont(43 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "상품명");
                BXLLApi.PrintDeviceFont(85 * dotsPer1mm, 16 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "EA");

                int i = 0;
                foreach (var item in t)
                {
                    BXLLApi.PrintDeviceFont(3 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, item.code);
                    if (item.name.Length <= 25)
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, item.name);
                    else if (item.name.Length > 25 && item.name.Length < 50)
                    {
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(0, 25));
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (26 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(25, item.name.Length - 25));
                    }
                    else
                    {
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(0, 25));
                        BXLLApi.PrintDeviceFont(25 * dotsPer1mm, (26 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false,
                            item.name.Substring(25, 25));
                    }
                    BXLLApi.PrintDeviceFont(85 * dotsPer1mm, (22 + i * 10) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, item.cnt);
                    i++;
                }


                BXLLApi.Print1DBarcode(25 * dotsPer1mm, 55 * dotsPer1mm, (int)SLCS_BARCODE.CODE128, 2 * multiplier, 3 * multiplier, 50 * multiplier, (int)SLCS_ROTATION.ROTATE_0, (int)SLCS_HRI.HRI_NOT_PRINT, "8809681702713");
                BXLLApi.PrintDeviceFont(40 * dotsPer1mm, 62 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_20X26, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "8809681702713");

                BXLLApi.PrintBlock(1 * dotsPer1mm, 66 * dotsPer1mm, 99 * dotsPer1mm, 67 * dotsPer1mm, (int)SLCS_BLOCK_OPTION.BOX, 1);

                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "No.");
                BXLLApi.PrintDeviceFont(15 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "13");
                BXLLApi.PrintDeviceFont(60 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "배송");
                BXLLApi.PrintDeviceFont(72 * dotsPer1mm, 68 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "2019-11-27");
                BXLLApi.PrintDeviceFont(5 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "LOC");
                BXLLApi.PrintDeviceFont(15 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "M01-0101");
                BXLLApi.PrintDeviceFont(60 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, "차수");
                BXLLApi.PrintDeviceFont(72 * dotsPer1mm, 74 * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, false, "1");

                //	Print Command
                BXLLApi.Prints(1, 1);
            }

            // Disconnect printer
            BXLLApi.DisconnectPrinter();

        }

        public void PrintChute()
        {
            if (!ConnectPrinter())
                return;

            int multiplier = 1;

            int resolution = BXLLApi.GetPrinterDPI();
            int dotsPer1mm = (int)Math.Round((float)resolution / 25.4f);
            if (resolution >= 600)
                multiplier = 3;

            SendPrinterSettingCommand();


            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    int chute_num = j * 4 + i;
                    BXLLApi.PrintQRCode((2 + i * 25) * dotsPer1mm, (2 + j*20) * dotsPer1mm, 1, 4, 4, 0, "CHUTE_" + chute_num.ToString()); ;
                    BXLLApi.PrintDeviceFont((17 + i * 25) * dotsPer1mm, (7 + j*20) * dotsPer1mm, (int)SLCS_DEVICE_FONT.KOR_38X38, multiplier, multiplier, (int)SLCS_ROTATION.ROTATE_0, true, chute_num.ToString());
                }
            }
            //	Print Command
            BXLLApi.Prints(1, 1);

            // Disconnect printer
            BXLLApi.DisconnectPrinter();

        }
    }
}
