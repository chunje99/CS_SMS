using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Serilog;
using Windows.Storage;
using System.Diagnostics;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace CS_SMS_APP
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class Logs : Page
    {
        public Logs()
        {
            this.InitializeComponent();
            ShowLogs();
            Logs_Location.Text = ApplicationData.Current.LocalCacheFolder.Path;
        }

        private void ShowLogs()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            CheckLog();
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.ToString());
                    }
                    Thread.Sleep(300);
                }
            });
        }

        private void CheckLog()
        {
            string logs = "";
            logs = "PID ";
            logs += global.md.mdsData.pid.ToString() + "\n";
            logs += "moduleCnt ";
            logs += global.md.mdsData.moduleCnt.ToString() + "\n";
            logs += "heartBest";
            logs += global.md.mdsData.heartBest.ToString() + "\n";
            logs += "moduleSpeed ";
            logs += global.md.mdsData.settingData.moduleSpeed.ToString() + "\n";
            logs += "currentModuleSpeed ";
            logs += global.md.mdsData.currentModuleSpeed.ToString() + "\n";
            logs += "firstsensor";
            logs += global.md.mdsData.firstSensor.ToString() + "\n";
            logs += "remainCnt";
            logs += global.md.mdsData.remainCnt.ToString() + "\n";
            for (int i = 0; i < 3; i++)
            {
                logs += "point" + i.ToString() + " ";
                logs += global.md.mdsData.settingData.pointSpeed[i].ToString() + "\n";
            }
            logs += "ModuleInfo \n";
            for (int i = 0; i < 12; i++)
            {
                logs += "Module" + (i + 1).ToString() + "\n";
                logs += "\t" + "status ";
                logs += "\t" + global.md.mdsData.moduleInfos[i].status.ToString() + "\n";
                logs += "\t" + "alarm ";
                logs += "\t" + global.md.mdsData.moduleInfos[i].alarm.ToString() + "\n";
                for (int j = 0; j < 4; j++)
                {
                    logs += "\t" + "chute" + (i * 4 + j + 1).ToString() + "\n";
                    logs += "\t" + "\t" + "confirmData ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].confirmData.ToString() + "\n";
                    logs += "\t" + "\t" + "stackCnt ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].stackCount.ToString() + "\n";
                    logs += "\t" + "\t" + "pidNum ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].pidNum.ToString() + "\n";
                    logs += "\t" + "\t" + "full ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].chuteInfos[j].full.ToString() + "\n";
                }
                for (int j = 0; j < 2; j++)
                {
                    logs += "\t" + "PrintInfo_" + (i * 2 + j).ToString() + "\n";
                    logs += "\t" + "\t" + "leftChute ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].leftChute.ToString() + "\n";
                    logs += "\t" + "\t" + "rightChute ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].rightChute.ToString() + "\n";
                    logs += "\t" + "\t" + "lprintButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].lprintButton.ToString() + "\n";
                    logs += "\t" + "\t" + "rprintButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].rprintButton.ToString() + "\n";
                    logs += "\t" + "\t" + "plusButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].plusButton.ToString() + "\n";
                    logs += "\t" + "\t" + "minusButton ";
                    logs += "\t" + "\t" + global.md.mdsData.moduleInfos[i].printInfos[j].minusButton.ToString() + "\n";
                }
            }

            logs += "TrackingData \n";
            for (int i = 0; i < 40; i++)
            {
                logs += "position" + (i + 1).ToString() + "\n";
                logs += "\t" + "chuteNum ";
                logs += "\t" + global.md.mdsData.positions[i].chuteNum.ToString() + "\n";
                logs += "\t" + "pid ";
                logs += "\t" + global.md.mdsData.positions[i].pid.ToString() + "\n";
            }

            LOGS.Text = logs;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CheckLog();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            global.m_mainTopTB.Text = global.m_mainTopPrefix + "LOGS";
        }
    }
}
