using System;
using System.Collections.Generic;
using System.Text;

namespace CS_SMS_LIB
{
    public class CMPS
    {
        public string order_key { get; set; }       // 주문번호
        public string order_line_num { get; set; }  
        public string com_cd { get; set; }          // 고객코드
        public string sku_cd { get; set; }          // 상품코드
        public string sku_nm { get; set; }          // 상품명
        public string sku_barcd { get; set; }       // 상품 바코드
        public string cust_cd { get; set; }         // 거래코드
        public string cust_nm { get; set; }         // 거래처명
        public string loc_cd { get; set; }          // 로케이션번호
        public int pick_qty { get; set; }        // 수량
        public int picked_qty { get; set; }      // 검수수량
        public int box_in_qty { get; set; }      // 박스내 상품수량
        public int seq_no { get; set; }             //차수
        public string rgter { get; set; } 
        public CMPS()
        {
            order_key = "";
            order_line_num = "";
            com_cd = "";
            sku_cd = "";
            sku_nm = "";
            sku_barcd = "";
            cust_cd = "";
            cust_nm = "";
            loc_cd = "";
            pick_qty = 0;
            picked_qty = 0;
            box_in_qty = 0;
            seq_no = 0;
            rgter = "";
        }

    }
}
