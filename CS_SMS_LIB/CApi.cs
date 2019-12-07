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

namespace CS_SMS_LIB
{
    public class CApi
    {
        //public string URL { get; set; } = "https://speech-api.kakao.com/demo/uploadState";
        //public string urlParameters { get; set} = "?redis_key=files_db1bdd4cb8d55d08dc458cae0263bee2";
        public string URL { get; set; } = "http://sms-api.wtest.biz/v1/product/barcode/";
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
            client.BaseAddress = new Uri(URL+barcode);

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
    }
    public class Product
    {
        public string status { get; set; }
        public int chute { get; set; }
    }
}
