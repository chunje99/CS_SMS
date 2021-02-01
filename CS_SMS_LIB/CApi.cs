using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Threading;

namespace CS_SMS_LIB
{
    public class CApi
    {
#if DEBUG
        public string Domain { get; set; } = "http://sms-api.wtest.biz";
        //public string Domain { get; set; } = "http://sms-admin.wtest.biz";
#else
        public string Domain { get; set; } = "http://127.0.0.1";
#endif
        public string urlParameters { get; set; } = "";
        public int m_chute = 0;

        public CApi()
        {
            Log.Information("CApi");
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="scan_type"></param> single, bundle, remain
        /// <returns></returns>
        public async Task<Product> GetChute(string barcode, string scan_type)
        {
            //m_chute = m_chute % 12;
            //m_chute++;
            //return;
            //test
            //barcode = "8809681702713";
            Product product = new Product();
            ///test
            //DateTime dt = DateTime.Now;
            //product.chute_num = dt.Millisecond % 12 + 1;
            //return product;
            HttpClient client = new HttpClient();
            string url  = "/v1/product/barcode/" + barcode + "?scan_type=" + scan_type;
            Log.Information(url);
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            try
            {
                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    //var resp = response.Content.ReadAsStringAsync();
                    //Log.Information(await resp);
                    var resp = response.Content.ReadAsStreamAsync();
                    //List<Contributor> contributors = JsonConvert.DeserializeObject<List<Contributor>>(resp);
                    //contributors.ForEach(Console.WriteLine);
                    var serializer = new DataContractJsonSerializer(typeof(Product));
                    var repositories = serializer.ReadObject(await resp) as Product;
                    Log.Information(barcode);
                    Log.Information(repositories.status);
                    Log.Information(repositories.msg);
                    if (repositories.status == "OK")
                    {
                        Log.Information(repositories.chute_num.ToString());
                        m_chute = repositories.chute_num;
                        foreach (var item in repositories.list)
                        {
                            Log.Information(item.cust_nm);
                            Log.Information(item.sku_nm);
                        }
                    }
                    return repositories;
                }
                else
                {
                    Log.Information("{0} {1} {2} {3}", url, (int)response.StatusCode, response.ReasonPhrase, barcode);
                    product.msg = String.Format("{0} {1} {2} {3}", url, (int)response.StatusCode, response.ReasonPhrase, barcode);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                product.msg = String.Format("{0} {1} ", url, e.ToString());
            }
            return product;
        }

        /// <summary>
        /// 바코드 찍어서 슈트에 할당할때
        /// pid == -1 일때 잔류 상품 처리
        /// </summary>
        /// <param name="seq"></param> 
        /// <param name="pid"></param>
        /// <param name="cnt"></param>
        /// <param name="chute_num"></param>
        public void Leave(int seq, int pid, int cnt, int chute_num)
        {
            Log.Information("===Leave===");
            Log.Information("seq {0}  pid {1} cnt {2} chute_num {3}", seq, pid, cnt, chute_num);
            HttpClient client = new HttpClient();
            string url = "/v1/product/leave";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("seq", seq);
            json.Add("pid", pid);
            json.Add("cnt", cnt);
            json.Add("chute_num", chute_num);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void Cancel(int pid)
        {
            Log.Information("===Cancel===");
            Log.Information("pid {0}", pid);
            HttpClient client = new HttpClient();
            string url = "/v1/product/cancel";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("pid", pid);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public async Task<Product> CancelScan(string barcode)
        {
            Product product = new Product();
            HttpClient client = new HttpClient();
            string url = "/v2/product/cancelscan/" + barcode;
            Log.Information(url);
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            try
            {
                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    //var resp = response.Content.ReadAsStringAsync();
                    //Log.Information(await resp);
                    var resp = response.Content.ReadAsStreamAsync();
                    //List<Contributor> contributors = JsonConvert.DeserializeObject<List<Contributor>>(resp);
                    //contributors.ForEach(Console.WriteLine);
                    var serializer = new DataContractJsonSerializer(typeof(Product));
                    var repositories = serializer.ReadObject(await resp) as Product;
                    Log.Information(barcode);
                    Log.Information(repositories.status);
                    Log.Information(repositories.msg);
                    if (repositories.status == "OK")
                    {
                        Log.Information(repositories.chute_num.ToString());
                        m_chute = repositories.chute_num;
                        foreach (var item in repositories.list)
                        {
                            Log.Information(item.cust_nm);
                            Log.Information(item.sku_nm);
                        }
                    }
                    return repositories;
                }
                else
                {
                    Log.Information("{0} {1} {2} {3}", url, (int)response.StatusCode, response.ReasonPhrase, barcode);
                    product.msg = String.Format("{0} {1} {2} {3}", url, (int)response.StatusCode, response.ReasonPhrase, barcode);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                product.msg = String.Format("{0} {1} ", url, e.ToString());
            }
            return product;
        }

        public void Release(int pid, int confirm_data, int stack_count, int chute_num)
        {
            Log.Information("===Release===");
            Log.Information("pid {0} confirm_data {1} stack_count {2} chute_num {3}", pid, confirm_data, stack_count, chute_num);
            HttpClient client = new HttpClient();
            string url = "/v1/product/release";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("pid", pid);
            json.Add("confirm_data", confirm_data);
            json.Add("stack_count", stack_count);
            json.Add("chute_num", chute_num);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void FullManual(int chute_num, int onoff)
        {
            Log.Information("===FullManual===");
            Log.Information("chute_num {0} onoff{1}", chute_num, onoff);
            HttpClient client = new HttpClient();
            string url = "/v1/module/fullmanual";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("chute_num", chute_num);
            if (onoff == 2)
                json.Add("full", "gap");
            else if (onoff == 1)
                json.Add("full", "on");
            else
                json.Add("full", "off");
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void FullAuto(int chute_num, int onoff)
        {
            Log.Information("===FullAuto===");
            Log.Information("chute_num {0} onoff{1}", chute_num, onoff);
            HttpClient client = new HttpClient();
            string url = "/v1/module/fullauto";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("chute_num", chute_num);
            if (onoff == 2)
                json.Add("full", "gap");
            else if (onoff == 1)
                json.Add("full", "on");
            else
                json.Add("full", "off");
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        //plus or minus
        public void AddStatus(int chute_num, string status)
        {
            Log.Information("===AddStatus===");
            Log.Information("chute_num {0} status {1}", chute_num, status);

            HttpClient client = new HttpClient();
            string url = "/v1/module/addstatus";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("chute_num", chute_num);
            json.Add("status", status);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void AddGoods(int chute_num, string barcode)
        {
            Log.Information("===AddGoods===");
            Log.Information("chute_num {0} barcode {1}", chute_num, barcode);

            HttpClient client = new HttpClient();
            string url = "/v1/module/addgoods";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("chute_num", chute_num);
            json.Add("barcode", barcode);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void MasterSensor(int pid )
        {
            Log.Information("===MasteSensor===");
            Log.Information("pid {0}", pid);

            HttpClient client = new HttpClient();
            string url = "/v1/product/mastersensor";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("pid", pid);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public async Task<PrintList> Print(int chute_num, string job_dt, string box_num)
        {
            Log.Information("===Print===");
            Log.Information("chute_num {0} job_dt {1} box_num {2}", chute_num, job_dt, box_num);
            PrintList printList = new PrintList();
            try
            {

                HttpClient client = new HttpClient();
                string url = "/v1/module/print";
                client.BaseAddress = new Uri(Domain + url);

                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // List data response.
                var json = new JObject();
                json.Add("chute_num", chute_num);
                json.Add("job_dt", job_dt);
                json.Add("box_num", box_num);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync(Domain + url, content).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    var resp = response.Content.ReadAsStreamAsync();
                    var serializer = new DataContractJsonSerializer(typeof(PrintList));
                    printList = serializer.ReadObject(await resp) as PrintList;
                    Log.Information(printList.status);
                    Log.Information(printList.msg);
                    if (printList.status == "OK")
                    {
                        Log.Information("Printer {@PrintList}", printList);
                        return printList;
                    }
                }
                else
                {
                    Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
            }
            return printList;
        }

        /// <summary>
        /// Search Box list 
        /// </summary>
        /// <param name="searchKey"></param>  box_num, cust_cd, cust_nm, sku_cd
        /// <param name="searchValue"></param>
        /// <returns></returns>
        public async Task<BoxList> Box(string searchKey, string searchValue, string job_dt)
        {
            Log.Information("===Box===");
            Log.Information("searchKey {0} searchValue {1} job_dt {2}", searchKey, searchValue, job_dt);
            BoxList boxList = new BoxList();
            try
            {
                HttpClient client = new HttpClient();
                string url = "/v1/datalist/box?job_dt=" + job_dt + "&searchKey=" + searchKey + "&searchValue=" + searchValue;
                Log.Information(url);
                client.BaseAddress = new Uri(Domain + url);

                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // List data response.
                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    var resp = response.Content.ReadAsStreamAsync();
                    var serializer = new DataContractJsonSerializer(typeof(BoxList));
                    boxList = serializer.ReadObject(await resp) as BoxList;
                    Log.Information(boxList.status);
                    Log.Information(boxList.msg);
                    if (boxList.status == "OK")
                        return boxList;
                }
                else
                {
                    Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
            }
            return boxList;
        }

        public async Task<PIDData> GetPID()
        {
            PIDData pidData = new PIDData();
            HttpClient client = new HttpClient();
            string url = "/v1/module/pid";
            Log.Information(url);
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            try
            {
                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    //var resp = response.Content.ReadAsStringAsync();
                    //Log.Information(await resp);
                    var resp = response.Content.ReadAsStreamAsync();
                    //List<Contributor> contributors = JsonConvert.DeserializeObject<List<Contributor>>(resp);
                    //contributors.ForEach(Console.WriteLine);
                    var serializer = new DataContractJsonSerializer(typeof(PIDData));
                    var repositories = serializer.ReadObject(await resp) as PIDData;
                    Log.Information(repositories.status);
                    Log.Information(repositories.msg);
                    if (repositories.status == "OK")
                    {
                        Log.Information("GetPID:" + repositories.pid.ToString());
                    }
                    return repositories;
                }
                else
                {
                    Log.Information("{0} {1} {2}", url, (int)response.StatusCode, response.ReasonPhrase);
                    pidData.msg = String.Format("{0} {1} {2}", url, (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                pidData.msg = String.Format("{0} {1} ", url, e.ToString());
            }
            return pidData;
        }

        public async Task<IndicatorList> GetIndicatorList(int chute_num, string barcode)
        {
            HttpClient client = new HttpClient();
            string url = "/v2/dps/scanchutegoods";
            Log.Information(url);
            client.BaseAddress = new Uri(Domain + url);
            IndicatorList indicatorList = new IndicatorList();


            // List data response.
            try
            {
                // List data response.
                var json = new JObject();
                json.Add("barcode", barcode);
                json.Add("chute_num", chute_num);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    var resp = response.Content.ReadAsStreamAsync();
                    var serializer = new DataContractJsonSerializer(typeof(IndicatorList));
                    indicatorList = serializer.ReadObject(await resp) as IndicatorList;
                    Log.Information(indicatorList.status);
                    Log.Information(indicatorList.msg);
                    if (indicatorList.status == "OK")
                    {
                        Log.Information("IndicatorList {@IndocatorList}", indicatorList);
                        return indicatorList;
                    }
                }
                else
                {
                    Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                //pidData.msg = String.Format("{0} {1} ", url, e.ToString());
            }
            return indicatorList;
        }

        public async Task<IndicatorRes> IndicatorRes(JObject json)
        {
            HttpClient client = new HttpClient();
            string url = "/v2/dps/indres";
            Log.Information(url);
            client.BaseAddress = new Uri(Domain + url);
            IndicatorRes indicatorRes = new IndicatorRes();


            // List data response.
            try
            {
                // List data response.
                //var json = new JObject();
                //json.Add("id", id);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    var resp = response.Content.ReadAsStreamAsync();
                    var serializer = new DataContractJsonSerializer(typeof(IndicatorRes));
                    indicatorRes = serializer.ReadObject(await resp) as IndicatorRes;
                    Log.Information(indicatorRes.status);
                    Log.Information(indicatorRes.msg);
                    if (indicatorRes.status == "OK")
                    {
                        Log.Information("IndicatorRes {@IndicatorRes}", indicatorRes);
                        return indicatorRes;
                    }
                }
                else
                {
                    Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                //pidData.msg = String.Format("{0} {1} ", url, e.ToString());
            }
            return indicatorRes;
        }

        public async Task<Product> FirstSensor()
        {
            Log.Information("===FirstSensor===");
            Product product = new Product();

            HttpClient client = new HttpClient();
            string url = "/v2/product/firstsensor";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            try
            {
                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
                if (response.IsSuccessStatusCode)
                {
                    var resp = response.Content.ReadAsStreamAsync();
                    var serializer = new DataContractJsonSerializer(typeof(Product));
                    product = serializer.ReadObject(await resp) as Product;
                    Log.Information(product.status);
                    Log.Information(product.msg);
                    if (product.status == "OK")
                    {
                        Log.Information("Product {@Product}", product);
                        return product;
                    }
                }
                else
                {
                    Log.Information("{0} {1} {2}", url, (int)response.StatusCode, response.ReasonPhrase);
                    product.msg = String.Format("{0} {1} {2}", url, (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
                product.msg = String.Format("{0} {1} ", url, e.ToString());
            }
            return product;
        }

        public void SetTest(string j)
        {
            //return;
            Log.Information("===SetTest===");
            HttpClient client = new HttpClient();
            string url = "/v1/product/arrived";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("barcode", "1231823");
            json.Add("pid", "12");
            json.Add("chute", "6");
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Log.Information("===OK===");
            }
            else
            {
                Log.Information("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }
    }
    public class Product
    {
        public string status { get; set; }
        public int seq { get; set; }
        public int chute_num { get; set; }
        public string sku_cd { get; set; }
        public string sku_nm { get; set; }
        public string cust_cd { get; set; }
        public string cust_nm { get; set; }
        public int total_qty { get; set; }
        public int leave_qty { get; set; }
        public int remain_qty { get; set; }
        public string msg { get; set; }
        public int send_cnt { get; set; }
        public int buffer { get; set; }
        public List<PListData> list { get; set; }

        public Product()
        {
            status = "";
            seq = 0;
            chute_num = 0;
            sku_cd = "";
            sku_nm = "";
            cust_cd = "";
            cust_nm = "";
            total_qty = 0;
            leave_qty = 0;
            remain_qty = 0;
            msg = "";
            send_cnt = 1;
            buffer = 0;
            list = new List<PListData>();
        }
        public void Set(PListData p)
        {
            seq = p.seq;
            chute_num = p.chute_num;
            sku_cd = p.sku_cd;
            sku_nm = p.sku_nm;
            cust_cd = p.cust_cd;
            cust_nm = p.cust_nm;
            total_qty = p.total_qty;
            leave_qty = p.leave_qty;
            remain_qty = p.remain_qty;
        }
    }
    public class PListData
    {
        public int idx { get; set; }
        public int seq { get; set; }
        public string highlight { get; set; }
        public int chute_num { get; set; }
        public string sku_cd { get; set; }
        public string sku_nm { get; set; }
        public string cust_cd { get; set; }
        public string cust_nm { get; set; }
        public int total_qty { get; set; }
        public int leave_qty { get; set; }
        public string leave_qty_color { get; set; }
        public int remain_qty { get; set; }
        public int cnt { get; set; } = 0;
        public PListData()
        {
            idx = 0;
            seq = 0;
            highlight = "";
            chute_num = 0;
            sku_cd = "";
            sku_nm = "";
            cust_cd = "";
            cust_nm = "";
            total_qty = 0;
            leave_qty = 0;
            leave_qty_color = "black";
            remain_qty = 0;
            cnt = 0;
        }
        public PListData(PListData p)
        {
            idx = p.idx;
            seq = p.seq;
            highlight = p.highlight;
            chute_num = p.chute_num;
            sku_cd = p.sku_cd;
            sku_nm = p.sku_nm;
            cust_cd = p.cust_cd;
            cust_nm = p.cust_nm;
            total_qty = p.total_qty;
            leave_qty = p.leave_qty;
            leave_qty_color = p.leave_qty_color;
            remain_qty = p.remain_qty;
            cnt = p.cnt;
        }
    }

    public class PrintData
    {
        public string sku_cd { get; set; } = "";
        public string sku_nm { get; set; } = "";
        public string cnt { get; set; } = "";
        public PrintData()
        {

        }
    }
    public class PrintList
    {
        public string status { get; set; } = "";
        public string msg { get; set; } = "";
        public string cust_nm { get; set; } = "";
        public string cust_cd { get; set; } = "";
        public string barcode { get; set; } = "";
        public string no { get; set; } = "";
        public string delivery_date { get; set; } = "";
        public string loc { get; set; } = "";
        public string no2 { get; set; } = "";
        public List<PrintData> list { get; set; } = new List<PrintData>();
        public PrintList()
        {
        }
    }

    public class BoxList
    {
        public string status { get; set; } = "";
        public string msg { get; set; } = "";
        public List<BoxData> list { get; set; } = new List<BoxData>();
        public BoxList()
        { }
    }
    
    public class BoxData
    {
        public int index { get; set; } = 0;
        public string job_dt { get; set; } = "";
        public string box_num { get; set; } = "";
        public string cust_cd { get; set; } = "";
        public string cust_nm { get; set; } = "";
        public string chute_num { get; set; } = "";
        public string goods_cnt { get; set; } = "";
        public BoxData()
        { }
        public BoxData(BoxData t)
        {
            index = t.index;
            job_dt = t.job_dt;
            box_num = t.box_num;
            cust_cd = t.cust_cd;
            cust_nm = t.cust_nm;
            chute_num = t.chute_num;
            goods_cnt = t.goods_cnt;
        }
    }
    public class PIDData
    {
        public string status { get; set; } = "";
        public string msg { get; set; } = "";
        public int pid { get; set; } = 0;
        public PIDData()
        { }
    }

    public class IndicatorData
    {
        public string id { get; set; } = "";
        public string biz_id { get; set; } = "";
        public string[] seg_role { get; set; } = new string[] { };
        public string org_boxin_qty { get; set; } = "0";
        public string org_ea_qty { get; set; } = "0";
        public string view_type { get; set; } = "0";
        public IndicatorData() { }
    }
    public class IndicatorList
    {
        public string status { get; set; } = "";
        public string msg { get; set; } = "";
        public string action { get; set; } = "";
        public string biz_type { get; set; } = "";
        public string action_type { get; set; } = "";
        public List<IndicatorData> ind_on { get; set; } = new List<IndicatorData>();
        public List<string> ind_off { get; set; } = new List<string>();
        public IndicatorList() { }
    }
    //{"status":"OK","msg":"","action":"IND_OFF_REQ","end_off_flag":"false","force_flag":"true",
    //"ind_off":["F8C6FC","1135F2","11364C"]}
    public class IndicatorRes
    {
        public string status { get; set; } = "";
        public string msg { get; set; } = "";
        public string action { get; set; } = "";
        public string end_off_flag { get; set; } = "";
        public string force_flag { get; set; } = "";
        public List<string> ind_off { get; set; } = new List<string>();
        public IndicatorRes() { }
    }
    public class IndicatorOK
    {
        public string status { get; set; } = "";
        public string msg { get; set; } = "";
        public string action { get; set; } = "";
        public string end_off_flag { get; set; } = "";
        public string force_flag { get; set; } = "";
        public List<string> ind_off { get; set; } = new List<string>();
        public IndicatorOK() { }
    }
}
