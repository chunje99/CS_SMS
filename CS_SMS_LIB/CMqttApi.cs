using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;

namespace CS_SMS_LIB
{
    public class CMqttApi
    {
        IMqttClient client;
        MqttFactory mqttFactory;
        string clientId;
        public bool isConnect = false;
        public bool isActive = false;

        public class MpsProperties : ICloneable
        {
            public string id { get; set; }
            public long time { get; set; }
            public string dest_id { get; set; }
            public string source_id { get; set; }
            public bool is_reply { get; set; }

            public object Clone() { return this.MemberwiseClone(); }
        }
        public class MpsBody
        {
            public string action { get; set; }
        }
        public class MpsBodyTimeSync : MpsBody
        {
            public MpsBodyTimeSync()
            {
                action = "TIMESYNC_RES";
            }
            public long svr_time { get; set; }
        }
        public class MpsInd
        {
            public MpsInd()
            {
                id = "";
                channel = "";
                pan = "";
                biz_type = "";
            }
            public string id { get; set; }
            public string channel { get; set; }
            public string pan { get; set; }
            public string biz_type { get; set; }
        }
        public class MpsIndConf
        {
            public MpsIndConf()
            {

                seg_role = new string[] { "R", "B" };
                alignment = "R";
                btn_mode = "B";
                btn_intvl = 3;
                bf_on_msg_t = 0;
                bf_on_delay = 0;
                cncl_delay = 20;
                blink_if_full = false;
                off_use_res = false;
                led_bar_mode = "S";
                led_bar_intvl = 3;
                led_bar_brtns = 10;
                view_type = "2";
            }
            public string[] seg_role { get; set; }
            public string alignment { get; set; }
            public string btn_mode { get; set; }
            public int btn_intvl { get; set; }
            public int bf_on_msg_t { get; set; }
            public int bf_on_delay { get; set; }
            public int cncl_delay { get; set; }
            public bool blink_if_full { get; set; }
            public bool off_use_res { get; set; }
            public string led_bar_mode { get; set; }
            public int led_bar_intvl { get; set; }
            public int led_bar_brtns { get; set; }
            public string view_type { get; set; }
        }

        public class MpsBodyGwInit : MpsBody
        {
            public MpsBodyGwInit()
            {
                action = "GW_INIT_RES";
                gw_version = "1.0.0.0";
                gw_conf = new MpsGwConf();
                ind_version = "1.0.0.0";
                ind_list = new List<MpsInd>();
                ind_conf = new MpsIndConf();
                svr_time = 0;
                health_period = 0;
            }
            public class MpsGwConf
            {
                public MpsGwConf()
                {
                    id = "";
                    channel = "";
                    pan = "";
                }
                public string id { get; set; }
                public string channel { get; set; }
                public string pan { get; set; }
            }
            public string gw_version { get; set; }
            public MpsGwConf gw_conf { get; set; }
            public string ind_version { get; set; }
            public List<MpsInd> ind_list { get; set; }
            public MpsIndConf ind_conf { get; set; }
            public long svr_time { get; set; }
            public int health_period { get; set; }
        }

        public class MpsIndOn
        {
            public MpsIndOn()
            {
                id = "";
                seg_role = new string[] { "R", "B" };
                biz_id = "";
                org_relay = 0;
                org_box_qty = 0;
                color = "R";
                view_type = "3";
            }
            public string id { get; set; }
            public string[] seg_role{ get; set; }
            public string biz_id { get; set; }
            public int org_relay { get; set; }
            public int org_box_qty { get; set; }
            public string color{ get; set; }
            public string view_type { get; set; }
        }
        public class MpsBodyIndOn: MpsBody
        {
            public MpsBodyIndOn()
            {
                action = "IND_ON_REQ";
                biz_type = "MPS";
                action_type = "pick";
                read_only = false;
                ind_on = new List<MpsIndOn>();
            }
            public string biz_type { get; set; }
            public string action_type { get; set; }
            public bool read_only { get; set; }
            public List<MpsIndOn> ind_on { get; set; }
        }

        public class MpsBodyIndOff: MpsBody
        {
            public MpsBodyIndOff()
            {
                action = "IND_OFF_REQ";
                end_off_flag = false;
                force_flag = true;
                ind_off = new List<string>();
            }
            public bool end_off_flag { get; set; }
            public bool force_flag { get; set; }
            public List<string> ind_off { get; set; }
        }

        public class MpsReq<T>
        {
            public MpsReq()
            {
                properties = new MpsProperties();
            }
            public MpsProperties properties;
            public T body;
        }
        public class MpsRes<T>
        {
            public MpsRes()
            {
                properties = new MpsProperties();
            }
            public MpsProperties properties;
            public T body;
        }

        private async Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {eventArgs.ApplicationMessage.ConvertPayloadToString()} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";

            /*
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (AddReceivedMessagesToList.IsChecked == true)
                {
                    ReceivedMessages.Items.Add(item);
                }
            });
            */
        }

        private void OnConnected()
        {
            //Connect();
        }

        private void OnDisconnected()
        {
            //Connect();
        }

        public async Task Connect()
        {
            string clientId;
            string BrokerAddress = "dothing.iptime.org";
            mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();
            //client.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
            //client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnConnected());
            //client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnDisconnected());

            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId("Client1")
                .WithTcpServer(BrokerAddress, 1883)
                .WithCredentials("dothing", "dothing")
                .Build();

            await client.ConnectAsync(options);
            isConnect = true;
            Log.Information("Connect");


            //client = new MqttClient(BrokerAddress);
            //client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            //clientId = Guid.NewGuid().ToString();
            clientId = "TTTEEESSSTTT";
            //client.Connect(clientId, "dothing", "dothing");

            client.UseApplicationMessageReceivedHandler(e =>
            {
                Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine();
                Log.Information("Received" + e.GetHashCode());
                string ReceivedMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                handleEvent(e.ApplicationMessage.Topic, ReceivedMessage);
            });

        }

        public async Task Disconnect()
        {
            Log.Information("Disconnect");
            await client.DisconnectAsync();
        }

        public async Task Subscribe(string Topic)
        {
            Log.Information("Subscribe");
            // subscribe to the topic with QoS 2
            //client.Subscribe(new string[] { Topic }, new byte[] { 2 });   // we need arrays as parameters because we can subscribe to different topics with one call
            await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(Topic).Build());
        }

        public async Task Public(string Topic, string json)
        {
            Log.Information("Public");
            // whole topic
            // publish a message with QoS 2
            //client.Publish(Topic, Encoding.UTF8.GetBytes(json), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
            //client.Publish(Topic, Encoding.UTF8.GetBytes("{\"message\":\"hello\"}"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
            //client.PublishAsync(new MqttTopicFilterBuilder().WithTopic("dothing_server").Build());
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(Topic)
                .WithPayload(json)
                .WithAtMostOnceQoS()
                .WithRetainFlag(false)
                .Build();

            await client.PublishAsync(applicationMessage);
        }

        // this code runs when a message was received
        /*
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Log.Information("Received" + e.GetHashCode());
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            handleEvent(e.Topic, ReceivedMessage);

        }
        */

        void handleEvent(string Topic, string body)
        {
            if (Topic == "dothing_server")
            {
                Log.Information("Topic: " + Topic);
                Log.Information(body);
                MpsReq<MpsBody> req = ParseMpsReq(body);
                if (req.body != null)
                {
                    if (req.body.action == "TIMESYNC_REQ")
                    {
                        ReqAck(req);
                        timesync_res(req);
                    }
                    else if (req.body.action == "GW_INIT_REQ")
                    {
                        ReqAck(req);
                        gw_init_res(req);
                    }
                    else if (req.body.action == "GW_INIT_RPT")
                    {
                        ReqAck(req);
                    }
                    else if (req.body.action == "IND_INIT_RPT")
                    {
                        ReqAck(req);
                        isActive = true;
                    }
                }
                else
                {
                    Log.Information("Parse Error");
                }
            }
            else
            {
                Log.Information("Topic: " + Topic);
                Log.Information(body);
            }
        }

        MpsReq<MpsBody> ParseMpsReq(string json)
        {
            MpsReq<MpsBody> req = new MpsReq<MpsBody>();
            try
            {
                req = JsonConvert.DeserializeObject<MpsReq<MpsBody>>(json);
                if (req.body != null)
                {
                    Log.Information(req.body.action);
                }
            }
            catch (Exception e)
            {
                Log.Information(e.Message);
            }
            return req;
        }

        void ReqAck(MpsReq<MpsBody> req)
        {
            MpsReq<MpsBody> ack = new MpsReq<MpsBody> { body = new MpsBody() };
            string source_id = req.properties.source_id;
            string dest_id = req.properties.dest_id;
            ack.properties = (MpsProperties)req.properties.Clone();
            ack.properties.source_id = req.properties.dest_id;
            ack.properties.dest_id = req.properties.source_id;
            ack.properties.is_reply = true;
            ack.body.action = req.body.action + "_ACK";
            string json = JsonConvert.SerializeObject(ack, Formatting.Indented);
            Public(ack.properties.dest_id, json);
        }

        void timesync_res(MpsReq<MpsBody> req)
        {
            // 현재 시간부터 1970년 1월 1일 0시 0분 0초까지를 뺀다.
            TimeSpan tsUnixTimeSpan = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0));
            // 초단위 값을 Int 형으로 형변환
            long NowUnixTime = System.Convert.ToInt64(tsUnixTimeSpan.TotalSeconds);

            MpsRes<MpsBodyTimeSync> res = new MpsRes<MpsBodyTimeSync> { body = new MpsBodyTimeSync() };
            res.properties = (MpsProperties)res.properties.Clone();
            res.properties.id = req.properties.id;
            //res.properties.source_id = "dothing_server";
            //res.properties.dest_id = "do01/F4BD01";
            res.properties.source_id = req.properties.dest_id;
            res.properties.dest_id = req.properties.source_id;
            res.properties.is_reply = false;
            res.body.svr_time = NowUnixTime;
            res.properties.time = NowUnixTime * 1000;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        void gw_init_res(MpsReq<MpsBody> req)
        {
            MpsRes<MpsBodyGwInit> res = new MpsRes<MpsBodyGwInit>();
            res.properties = new MpsProperties
            {
                id = req.properties.id,
                time = req.properties.time,
                source_id = req.properties.dest_id,
                dest_id = req.properties.source_id,
                is_reply = false,
            };
            res.body = new MpsBodyGwInit();
            res.body.gw_conf.id = "do01/F4BD01";
            res.body.gw_conf.channel = "5";
            res.body.gw_conf.pan = "02D3";
            res.body.ind_list.Add(new MpsInd { id = "F8C6FC", channel = "5", pan = "02D3", biz_type = "DAS" });
            /*
            res.body.ind_conf = new MpsIndConf
            {
                seg_role = new string[] { "B", "p" },
                alignment = "R",
                btn_mode = "B",
                btn_intvl = 3,
                bf_on_msg_t = 0,
                bf_on_delay = 0,
                cncl_delay = 20,
                blink_if_full = false,
                off_use_res = false,
                led_bar_mode = "S",
                led_bar_intvl = 3,
                led_bar_brtns = 10,
                view_type = "2"
            };
            res.body.svr_time = 123123;
            res.body.health_period = 0;
            */

            // 현재 시간부터 1970년 1월 1일 0시 0분 0초까지를 뺀다.
            TimeSpan tsUnixTimeSpan = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0));
            // 초단위 값을 Int 형으로 형변환
            long NowUnixTime = System.Convert.ToInt64(tsUnixTimeSpan.TotalSeconds);
            res.body.svr_time = NowUnixTime;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        public void ind_on_req (string ind_id, int relay, int box )
        {
            MpsRes<MpsBodyIndOn> res = new MpsRes<MpsBodyIndOn>();
            // 현재 시간부터 1970년 1월 1일 0시 0분 0초까지를 뺀다.
            TimeSpan tsUnixTimeSpan = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0));
            // 초단위 값을 Int 형으로 형변환
            long NowUnixTime = System.Convert.ToInt64(tsUnixTimeSpan.TotalSeconds);
            res.properties = new MpsProperties
            {
                id = Guid.NewGuid().ToString(),
                time = NowUnixTime * 1000,
                source_id = "dothing_server",
                dest_id = "do01/F4BD01",
                is_reply = false,
            };
            res.body = new MpsBodyIndOn();
            res.body.ind_on.Add(new MpsIndOn { id=ind_id, org_relay= relay, org_box_qty = box });

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        public void ind_off_req (List<string> ind_ids)
        {
            MpsRes<MpsBodyIndOff> res = new MpsRes<MpsBodyIndOff>();
            // 현재 시간부터 1970년 1월 1일 0시 0분 0초까지를 뺀다.
            TimeSpan tsUnixTimeSpan = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0));
            // 초단위 값을 Int 형으로 형변환
            long NowUnixTime = System.Convert.ToInt64(tsUnixTimeSpan.TotalSeconds);
            res.properties = new MpsProperties
            {
                id = Guid.NewGuid().ToString(),
                time = NowUnixTime * 1000,
                source_id = "dothing_server",
                dest_id = "do01/F4BD01",
                is_reply = false,
            };
            res.body = new MpsBodyIndOff();
            res.body.ind_off = ind_ids;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }
    }
}
