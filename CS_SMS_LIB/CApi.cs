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
        public string Domain { get; set; } = "http://sms-api.wtest.biz";
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
        public async void GetChute(string barcode)
        {
            m_chute = m_chute % 12;
            m_chute++;
            return;
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
                // Parse the response body. Blocking!
                var resp = response.Content.ReadAsStreamAsync();
                //List<Contributor> contributors = JsonConvert.DeserializeObject<List<Contributor>>(resp);
                //contributors.ForEach(Console.WriteLine);
                var serializer = new DataContractJsonSerializer(typeof(Product));
                var repositories = serializer.ReadObject(await resp) as Product;
                Debug.WriteLine(barcode);
                Debug.WriteLine(repositories.status);
                Debug.WriteLine(repositories.chute);
                m_chute = repositories.chute;
            }
            else
            {
                Debug.WriteLine("{0} {1} {2}", (int)response.StatusCode, response.ReasonPhrase, barcode);
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
        public int chute { get; set; }
    }
}
