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
    }

    public class DataObject
    {
        public string Name { get; set; }
    }

    public class Class1
    {
        private const string URL = "https://speech-api.kakao.com/demo/uploadState";
        private string urlParameters = "?redis_key=files_db1bdd4cb8d55d08dc458cae0263bee2";

        public Class1()
        {
            Debug.WriteLine("Class1");
        }
        async public void Start()
        {
            Debug.WriteLine("Start");
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URL);
            Debug.WriteLine("Here1");

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            Debug.WriteLine("Here2");

            // List data response.
            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
            Debug.WriteLine("Here3");
            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine("Here4");
                // Parse the response body. Blocking!
                var resp = response.Content.ReadAsStreamAsync();
                //List<Contributor> contributors = JsonConvert.DeserializeObject<List<Contributor>>(resp);
                //contributors.ForEach(Console.WriteLine);
                var serializer = new DataContractJsonSerializer(typeof(Product));
                var repositories = serializer.ReadObject(await resp) as Product;
                Debug.WriteLine(repositories);
                Debug.WriteLine(repositories.access_key);
            }
            else
            {
                Debug.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            Debug.WriteLine("HERE6");
        }
    }
    public class Product
    {
        public string access_key { get; set; }
        public string state { get; set; }
        public string filename { get; set; }
        public string vtt_key { get; set; }
    }
}
