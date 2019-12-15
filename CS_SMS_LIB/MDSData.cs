
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CS_SMS_LIB
{
    public class MDSData
    {
        ///읽는 데이터 
        public int pid {get;set;} = 0;
        public int moduleCnt {get;set;} = 0;
        public int heartBest {get;set;} = 0;
        public SettingData settingData = new SettingData();
        public int currentModuleSpeed {get;set;} = 0;
        public int remainCnt {get;set;} = 0;
        public List<ModuleInfo> moduleInfos = new List<ModuleInfo>();
        public List<TrackingData> positions {get;set;} = new List<TrackingData>();

        ///요청 데이터
        public DistributionData distributionData = new DistributionData();
        public int makePid {get;set;} = 0;
        public MDSData()
        {
            for(int i = 0 ; i < 12 ; i++)
                moduleInfos.Add(new ModuleInfo());
            for(int i = 0 ; i < 40 ; i++)
                positions.Add(new TrackingData());
        }

    }

    public class SettingData
    {
        public int moduleSpeed {get;set;} = 0;
        public List<int> pointSpeed {get;set;} = new List<int>();
        public SettingData()
        {
            for(int i = 0 ; i < 3 ; i++)
                pointSpeed.Add(0);
        }
    }

    public class ChuteInfo
    {
        public int confirmData {get;set;} = 0;
        public int stackCount {get;set;} = 0;
        public int pidNum {get;set;} = 0;
        public int full {get;set;} = 0;
    }
    public class ModuleInfo
    {
        public int status {get;set;} = 0;
        public int alarm {get;set;} = 0;
        public List<ChuteInfo> chuteInfos {get;set;} = new List<ChuteInfo>();
        public List<PrintInfo> printInfos {get;set;} = new List<PrintInfo>();
        public ModuleInfo()
        {
            for(int i = 0 ; i < 4 ; i++)
                chuteInfos.Add(new ChuteInfo());
            for(int i = 0 ; i < 2 ; i++)
                printInfos.Add(new PrintInfo());
        }
    }
    public class PrintInfo
    {
        public int leftChute {get;set;} = 0;
        public int rightChute {get;set;} = 0;
        public int printButton {get;set;} = 0;
        public int plusButton {get;set;} = 0;
        public int minusButton {get;set;} = 0;
    }
    public class TrackingData
    {
        public int chuteNum {get;set;} = 0;
        public int pid {get;set;} = 0;
    }
    public class DistributionData
    {
        public int pid {get;set;} = 0;
        public int chuteNum {get;set;} = 0;
    }
}