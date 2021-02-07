using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CS_SMS_LIB;
using Newtonsoft.Json.Linq;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Indicator : Page
    {
        ObservableCollection<string> indList = new ObservableCollection<string>();
        public Indicator()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "표시기 테스트";
            if(global.mqc.isActive)
            {
                IND_GW_Status.Text = "Active";
            }
            else
            {
                IND_GW_Status.Text = "Inactive";
            }
            SetSUBScanner();
            SetMqttHandler();
            indList.Clear();
            string indStr = "";
            int i = 0;
            foreach (string ind in global.mqc.m_indList)
            {
                indStr = indStr + ", " + ind;
                if( (i % 10) == 9 )
                {
                    indList.Add(indStr);
                    indStr = "";
                }
                i++;
            }
            indList.Add(indStr);
        }

        private async void IND_ON_Click(object sender, RoutedEventArgs e)
        {
            //var indicatorBody = await global.api.GetIndicatorList(1, "123");
            if(global.mqc.isConnect)
            {
                CMqttApi.MpsBodyIndOn reqBody = new CMqttApi.MpsBodyIndOn { };
                int box = Convert.ToInt32(IND1_ID.Text);
                int ea = Convert.ToInt32(IND2_ID.Text);
                foreach (var a in global.mqc.m_indList)
                {
                    reqBody.ind_on.Add(new CMqttApi.MpsIndOn { id=a, org_box_qty = box, org_ea_qty = ea });
                }
                global.mqc.ind_on_req(reqBody);
                UpdateUI("IND ON");
            }
            else
            {
                UpdateUI("disconnected");
            }
        }

        private async void IND_OFF_Click(object sender, RoutedEventArgs e)
        {
            if(global.mqc.isConnect)
            {
                CMqttApi.MpsBodyIndOff reqBody = new CMqttApi.MpsBodyIndOff { ind_off = global.mqc.m_indList};
                global.mqc.ind_off_req(reqBody);
                UpdateUI("IND OFF");
            }
            else
            {
                UpdateUI("disconnected");
            }
        }

        private async void LED_ON_Click(object sender, RoutedEventArgs e)
        {
            if(global.mqc.isConnect)
            {
                foreach( var id in global.mqc.m_indList)
                {
                    global.mqc.led_on_req(id);
                }
                UpdateUI("LED ON");
            }
            else
            {
                UpdateUI("disconnected");
            }
        }

        private async void LED_OFF_Click(object sender, RoutedEventArgs e)
        {
            if(global.mqc.isConnect)
            {
                foreach( var id in global.mqc.m_indList)
                {
                    global.mqc.led_off_req(id);
                }
                UpdateUI("LED OFF");
            }
            else
            {
                UpdateUI("disconnected");
            }
        }

        private async void UpdateUIAdd(string barcode)
        {
            Log.Information("UpdateUIAdd : " + barcode);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                indList.Add(barcode);
            });
        }

        private async void UpdateUIDel(string barcode)
        {
            Log.Information("UpdateUIDel: " + barcode);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                indList.Remove(barcode);
            });
        }

        private async void GetIndicatorList(int chute_num, string barcode)
        {
            Log.Information("GetIndicatorList: " + chute_num.ToString() + " ," + barcode);
            var indicatorBody = await global.api.GetIndicatorList(chute_num, barcode);
            if (global.mqc.isConnect)
            {
                CMqttApi.MpsBodyIndOn reqBody = new CMqttApi.MpsBodyIndOn { action = indicatorBody.action, action_type = indicatorBody.action_type, biz_type = indicatorBody.biz_type };
                foreach (var a in indicatorBody.ind_on)
                {
                    int box = Convert.ToInt32(a.org_boxin_qty);
                    int ea = Convert.ToInt32(a.org_ea_qty);
                    reqBody.ind_on.Add(new CMqttApi.MpsIndOn { id=a.id, org_box_qty = box, org_ea_qty = ea, biz_id = a.biz_id, view_type = a.view_type, seg_role = a.seg_role});
                }
                global.mqc.ind_on_req(reqBody);
                UpdateUI("LED ON");
            }
            else
            {
                UpdateUI("disconnected");
            }
        }

        private async void UpdateUI(string context)
        {
            Log.Information("UpdateUI: " + context);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                IND_GW_Status.Text = context;
            });
        }

        private void SetSUBScanner()
        {
            Log.Information("Ind SetSubScanner");
            foreach (var scanner in global.udp.m_scaner)
            {
                Log.Information("set scanner");
                scanner.act0 = (string job, string name, int chute_num, string barcode ) =>
                {
                    Log.Information(job + " " + barcode);
                    if(job == "ADDIND")
                    {
                        global.mqc.add_ind(barcode);
                        UpdateUIAdd(barcode);
                    }
                    else if(job == "DELIND")
                    {
                        global.mqc.del_ind(barcode);
                        indList.Remove(barcode);
                        UpdateUIDel(barcode);
                    }
                    else if(job == "INDICATOR")
                    {
                        GetIndicatorList(chute_num, barcode);
                    }

                    /*
                    if (chute_num == -1)
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Alert("스캐너를 할당하세요!(" + barcode + ")");
                        });
                    }
                    else if (chute_num == 0)
                    {
                        Scanner_Process(barcode, Monitoring_scanner0, 0);
                    }
                    else if (chute_num >= 1 && chute_num <= 48)
                    {
                        global.api.AddGoods(chute_num, barcode);
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            UpdateUI(Monitoring_scanner1, "슈터 " + chute_num.ToString() + " : " + barcode);
                        });
                    }
                    else
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            Alert("(" + barcode + ")할당이 잘못 되었습니다.! chute = " + chute_num.ToString());
                        });
                    }
                    */
                };
            }
            return;
        }
        private void SetMqttHandler()
        {
            Log.Information("SetMqttHandler");
            global.mqc.handleIndOnRes = (CMqttApi.MpsRes<CMqttApi.MpsBodyIndOnRes> res) =>
            {
                Log.Information("handleIndOnRes: " + res.body.id + " biz_flag :" + res.body.biz_flag);
                var json = new JObject();
                json.Add("action", res.body.action);
                json.Add("id", res.body.id);
                json.Add("biz_id", res.body.biz_id);
                json.Add("biz_type", res.body.biz_type);
                json.Add("action_type", res.body.action_type);
                json.Add("biz_flag", res.body.biz_flag);
                json.Add("org_relay", res.body.org_relay);
                json.Add("org_box_qty", res.body.org_box_qty);
                json.Add("org_ea_qty", res.body.org_ea_qty);
                json.Add("res_box_qty", res.body.res_box_qty);
                json.Add("res_ea_qty", res.body.res_ea_qty);
                IndicatorRes(json);
                if (res.body.biz_flag == "full")
                    global.mqc.led_on_req(res.body.id);
            };
        }
        private async void IndicatorRes(JObject json)
        {
            Log.Information("IndicatorRes");
            var indicatorOK = await global.api.IndicatorRes(json);
            ///indicator off
            CMqttApi.MpsBodyIndOff reqBody = new CMqttApi.MpsBodyIndOff { ind_off = indicatorOK.ind_off };
            global.mqc.ind_off_req(reqBody);
        }
    }
}
