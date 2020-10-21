using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using System.Security.Cryptography.X509Certificates;
using CS_SMS_LIB;
using Serilog;
using System.Threading.Tasks;

namespace MQTT_test
{

    class Program
    {
        static void Start()
        {
            Task task = Task.Run(async () =>
            {
                while (true)
                {
                    Log.Information("Wait");
                    await Task.Delay(1000); // Pause 1 second before checking state again
                }
            } );

            // Uncomment this and step through `CheckForStateChange`.
            // When the execution hangs, you'll know what's causing the
            // postbacks to the UI thread and *may* be able to take it out.
            // task.Wait();
        }

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            Console.WriteLine("Hello World!");

            //MQClient mqc = new MQClient();
            CMqttApi mqc = new CMqttApi();
            await mqc.Connect();
            //mqc.Subscribe("do01/F4BD01");
            //mqc.Subscribe("dothing_server");
            await mqc.Subscribe("dothing_server");

            string json = @"{
                  Message : ""Hello"",
                  Drives: [
                    ""DVD read/writer"",
                    ""500 gigabyte hard drive""
                  ]
                }";
            JObject jobj = JObject.Parse(json); //문자를 객체화
            await mqc.Public("TEST", jobj.ToString());
            //mqc.Public("do01/F4BD01");
            //mqc.Public("dothing_server");
            Task task1 = Task.Run(async () =>  // <- marked async
            {
                while (true)
                {
                    Log.Information("Wait");
                    await Task.Delay(5000);
                }
            });

            Task.WaitAll(new[] { task1});
        }
    }
}
