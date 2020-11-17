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
        public List<string> m_indList;
        public string m_gwID { get; set; }
        public string m_serverTopic { get; set; }
        public string m_brokerAddress { get; set; }
        public string m_user { get; set; }
        public string m_passwd { get; set; }
        public Action<MpsRes<MpsBodyIndOnRes>> handleIndOnRes = null;

        public CMqttApi()
        {
            /*
            m_indList = new List<string> { 
                "F8C6FC", "1135F2", "11364C", "1319C3", "131938", 
                "11364B", "1135FB", "113601", "113650", "1135F6", 
                "11346A", "1136B7", "1135F1", "113476", "1135F9", 
                "11369E", "1319AB", "1136AE", "113506", "113653", 
                "1135FD", "1135F5", "1319AA", "11364F", "11365A" };
            */
            //m_indList = new List<string> { "F8C6FC", "113652" };
            m_indList = new List<string> { 
                "11365C","113646","1135FF","1136B9","1136B7","1136AE",
                "113650","1135F5","1135FD","11365A","113506","113476",
                "11346A","1135F6","1319AA","113601","1135FB","11364F", 
                "11369E","1319AB","1135F1","131938","11364C","113653",
                "1135F9","11364B","1135F2","1319C3","1319D1","1136AF",
                "132162","13193E","1136BE","113659","1136B5","132313",
                "1136AD","1319CC","131940","1319C8","1135E9","1135EE",
                "113656","1136AB","113471","1136A4","11364E","11364A" };
            m_gwID = "kakao/F4BD01";
            //m_gwID = "kakao/F718CA";
            m_serverTopic = "weng";
            m_brokerAddress = "52.78.48.7";
            m_user = "mqadmin";
            m_passwd = "mqadminpassword";
        }

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

                seg_role = new string[] { "B", "P" };
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
                view_type = "0";
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
                health_period = 5;
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
                seg_role = new string[] { "B", "P" };
                biz_id = "";
                org_relay = 0;
                org_box_qty = 0;
                org_ea_qty = 0;
                color = "R";
                view_type = "0";
            }
            public string id { get; set; }
            public string[] seg_role{ get; set; }
            public string biz_id { get; set; }
            public int org_relay { get; set; }
            public int org_box_qty { get; set; }
            public int org_ea_qty { get; set; }
            public string color{ get; set; }
            public string view_type { get; set; }
        }
        public class MpsBodyIndOn: MpsBody
        {
            public MpsBodyIndOn()
            {
                action = "IND_ON_REQ";
                biz_type = "MPS";
                //action_type = "pick";
                action_type = "stock";
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

        public class MpsBodyLEDOn: MpsBody
        {
            public MpsBodyLEDOn()
            {
                action = "LED_ON_REQ";
                id = "";
                led_bar_mode = "B";
                led_bar_intvl = 1;
                led_bar_brtns = 10;
            }
            public string id { get; set; }
            public string led_bar_mode { get; set; }
            public int led_bar_intvl { get; set; }
            public int led_bar_brtns { get; set; }
        }
        public class MpsBodyLEDOff: MpsBody
        {
            public MpsBodyLEDOff()
            {
                action = "LED_OFF_REQ";
                id = "";
            }
            public string id { get; set; }
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

        public class MpsBodyIndOnRes: MpsBody
        {
            public MpsBodyIndOnRes()
            {
            }
            public string id { get; set; }
            public string biz_id { get; set; }
            public string biz_type { get; set; }
            public string action_type { get; set; }
            public string biz_flag { get; set; }
            public int org_relay { get; set; }
            public int org_box_qty { get; set; }
            public int org_ea_qty { get; set; }
            public int res_box_qty { get; set; }
            public int res_ea_qty { get; set; }
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
            try
            {
                string clientId;
                string BrokerAddress = m_brokerAddress;
                mqttFactory = new MqttFactory();
                client = mqttFactory.CreateMqttClient();
                //client.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
                //client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(x => OnConnected());
                //client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnDisconnected());

                // Create TCP based options using the builder.
                var options = new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer(BrokerAddress, 1883)
                    .WithCredentials(m_user, m_passwd)
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
            catch (Exception e)
            {
                Log.Information("Mqtt 접속에러" + e.ToString());
                //product.msg = String.Format("{0} {1} ", url, e.ToString());
            }
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
            if (Topic == m_serverTopic)
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
                        ///전체 ind, led 소등
                        MpsBodyIndOff reqBody = new MpsBodyIndOff();
                        reqBody.ind_off = m_indList;
                        ind_off_req(reqBody);
                        foreach( var id in m_indList)
                            led_off_req(id);
                    }
                    else if (req.body.action == "IND_INIT_RPT")
                    {
                        ReqAck(req);
                        isActive = true;
                    }
                    else if (req.body.action == "IND_ON_RES")
                    {
                        ReqAck(req);
                        if (handleIndOnRes != null)
                            handleIndOnRes(ParseIndOnRes(body));
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

        MpsRes<MpsBodyIndOnRes> ParseIndOnRes(string json)
        {
            MpsRes<MpsBodyIndOnRes> req = new MpsRes<MpsBodyIndOnRes>();
            try
            {
                req = JsonConvert.DeserializeObject<MpsRes<MpsBodyIndOnRes>>(json);
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
            long NowUnixTime = GetNowUTime();

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
            res.body.gw_conf.id = m_gwID;
            res.body.gw_conf.channel = "5";
            res.body.gw_conf.pan = "02D3";

            foreach(string ind in m_indList)
                res.body.ind_list.Add(new MpsInd { id = ind, channel = "5", pan = "02D3", biz_type = "DAS" });
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
            long NowUnixTime = GetNowUTime();
            res.body.svr_time = NowUnixTime;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        void ind_on_res(MpsReq<MpsBody> req)
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
            res.body.gw_conf.id = m_gwID;
            res.body.gw_conf.channel = "5";
            res.body.gw_conf.pan = "02D3";

            foreach(string ind in m_indList)
                res.body.ind_list.Add(new MpsInd { id = ind, channel = "5", pan = "02D3", biz_type = "DAS" });
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
            long NowUnixTime = GetNowUTime();
            res.body.svr_time = NowUnixTime;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        public void ind_on_req (MpsBodyIndOn reqBody)
        {
            MpsRes<MpsBodyIndOn> res = new MpsRes<MpsBodyIndOn>();
            long NowUnixTime = GetNowUTime();
            res.properties = new MpsProperties
            {
                id = Guid.NewGuid().ToString(),
                time = NowUnixTime * 1000,
                source_id = m_serverTopic,
                dest_id = m_gwID,
                is_reply = false,
            };
            res.body = reqBody;
            //res.body = new MpsBodyIndOn();
            //res.body.ind_on.Add(new MpsIndOn { id=ind_id, org_box_qty = box, org_ea_qty = ea_qty, biz_id = biz_id });

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        public void ind_off_req (MpsBodyIndOff reqBody)
        {
            MpsReq<MpsBodyIndOff> res = new MpsReq<MpsBodyIndOff>();
            long NowUnixTime = GetNowUTime();
            res.properties = new MpsProperties
            {
                id = Guid.NewGuid().ToString(),
                time = NowUnixTime * 1000,
                source_id = m_serverTopic,
                dest_id = m_gwID,
                is_reply = false,
            };
            res.body = reqBody;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        public void add_ind (string ind_id)
        {
            Log.Information("Add_ind : " + ind_id);
            if (!m_indList.Exists(x => x == ind_id))
            {
                m_indList.Add(ind_id);
            }
        }
        public void del_ind (string ind_id)
        {
            Log.Information("Del_ind : " + ind_id);
            if (m_indList.Exists(x => x == ind_id))
            {
                m_indList.Remove(ind_id);
            }
        }
        public void led_on_req (string ind_id)
        {
            MpsReq<MpsBodyLEDOn> res = new MpsReq<MpsBodyLEDOn>();
            long NowUnixTime = GetNowUTime();
            res.properties = new MpsProperties
            {
                id = Guid.NewGuid().ToString(),
                time = NowUnixTime * 1000,
                source_id = m_serverTopic,
                dest_id = m_gwID,
                is_reply = false,
            };
            res.body = new MpsBodyLEDOn();
            res.body.id = ind_id;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }

        public void led_off_req (string ind_id)
        {
            MpsReq<MpsBodyLEDOff> res = new MpsReq<MpsBodyLEDOff>();
            long NowUnixTime = GetNowUTime();
            res.properties = new MpsProperties
            {
                id = Guid.NewGuid().ToString(),
                time = NowUnixTime * 1000,
                source_id = m_serverTopic,
                dest_id = m_gwID,
                is_reply = false,
            };
            res.body = new MpsBodyLEDOff();
            res.body.id = ind_id;

            string json = JsonConvert.SerializeObject(res, Formatting.Indented);
            Public(res.properties.dest_id, json);
        }
        private long GetNowUTime()
        {
            // 현재 시간부터 1970년 1월 1일 0시 0분 0초까지를 뺀다.
            TimeSpan tsUnixTimeSpan = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0));
            // 초단위 값을 Int 형으로 형변환
            long NowUnixTime = System.Convert.ToInt64(tsUnixTimeSpan.TotalSeconds);
            return NowUnixTime;
        }
    }
}
