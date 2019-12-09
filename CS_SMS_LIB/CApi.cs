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

namespace CS_SMS_LIB
{
    public class CApi
    {
        //public string Domain { get; set; } = "http://sms-api.wtest.biz";
        public string Domain { get; set; } = "http://127.0.0.1";
        public string urlParameters { get; set; } = "";
        public int m_chute = 0;

        public CApi()
        {
            Debug.WriteLine("CApi");
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
        public async Task<Product> GetChute(string barcode)
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
            string url  = "/v1/product/barcode/";
            client.BaseAddress = new Uri(Domain + url + barcode);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                //var resp = response.Content.ReadAsStringAsync();
                //Debug.WriteLine(await resp);
                var resp = response.Content.ReadAsStreamAsync();
                //List<Contributor> contributors = JsonConvert.DeserializeObject<List<Contributor>>(resp);
                //contributors.ForEach(Console.WriteLine);
                try
                {
                    var serializer = new DataContractJsonSerializer(typeof(Product));
                    var repositories = serializer.ReadObject(await resp) as Product;
                    Debug.WriteLine(barcode);
                    Debug.WriteLine(repositories.status);
                    Debug.WriteLine(repositories.chute_num);
                    m_chute = repositories.chute_num;
                    return repositories;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
            else
            {
                Debug.WriteLine("{0} {1} {2}", (int)response.StatusCode, response.ReasonPhrase, barcode);
            }
            return product;
        }

        public void Leave(int seq, int pid)
        {
            Debug.WriteLine("===Release===");
            Debug.WriteLine("seq {0}  pid {1}", seq, pid);
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
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Debug.WriteLine("===OK===");
            }
            else
            {
                Debug.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void Cancel(int pid)
        {
            Debug.WriteLine("===Cancel===");
            Debug.WriteLine("pid {0}", pid);
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
                Debug.WriteLine("===OK===");
            }
            else
            {
                Debug.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void Release(int pid, int confirm_data, int stack_count, int chute_num)
        {
            Debug.WriteLine("===Release===");
            Debug.WriteLine("pid {0} confirm_data {1} stack_count {2} chute_num {3}", pid, confirm_data, stack_count, chute_num);
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
                Debug.WriteLine("===OK===");
            }
            else
            {
                Debug.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void FullManual(int chute_num, int onoff)
        {
            Debug.WriteLine("===FullManual===");
            Debug.WriteLine("chute_num {0} onoff{1}", chute_num, onoff);
            HttpClient client = new HttpClient();
            string url = "/v1/module/fullmanual";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("chute_num", chute_num);
            if (onoff == 1)
                json.Add("full", "on");
            else
                json.Add("full", "off");
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Debug.WriteLine("===OK===");
            }
            else
            {
                Debug.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void FullAuto(int chute_num, int onoff)
        {
            Debug.WriteLine("===FullAuto===");
            Debug.WriteLine("chute_num {0} onoff{1}", chute_num, onoff);
            HttpClient client = new HttpClient();
            string url = "/v1/module/fullauto";
            client.BaseAddress = new Uri(Domain + url);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            var json = new JObject();
            json.Add("chute_num", chute_num);
            if (onoff == 1)
                json.Add("full", "on");
            else
                json.Add("full", "off");
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(Domain+url, content).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                Debug.WriteLine("===OK===");
            }
            else
            {
                Debug.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public void SetTest(string j)
        {
            //return;
            Debug.WriteLine("===SetTest==="); 
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
                Debug.WriteLine("===OK===");
            }
            else
            {
                Debug.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
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
        public int remain_qty { get; set; }
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
            remain_qty = 0;
        }
    }
}
