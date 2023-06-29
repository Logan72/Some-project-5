using ClassLibrary;
using ClassLibrary.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Threading;
using System.IO;

namespace MmsService
{
    public partial class MmsService : ServiceBase
    {        
        string apiAddressGet;
        public MmsService()
        {
            InitializeComponent();            
        }

        protected override void OnStart(string[] args)
        {         
            StringBuilder sb = new StringBuilder();

            AccessIni.GetPrivateProfileString("API", "get", "", sb, 128, AccessIni.iniPath);
            apiAddressGet = sb.ToString();

            AccessIni.GetPrivateProfileString("API", "send", "", sb, 128, AccessIni.iniPath);
            MmsCommunication.apiAddressSend = sb.ToString();

            AccessIni.GetPrivateProfileString("ResetCommands", "keyenceResetCommand", "", sb, 64, AccessIni.iniPath);
            MmsCommunication.keyenceResetCommand = sb.ToString();

            AccessIni.GetPrivateProfileString("ResetCommands", "mitsubishiResetCommand", "", sb, 64, AccessIni.iniPath);
            MmsCommunication.mitsubishiResetCommand = sb.ToString();

            AccessIni.GetPrivateProfileString("ResetCommands", "omronResetCommand", "", sb, 64, AccessIni.iniPath);
            MmsCommunication.omronResetCommand = sb.ToString();

            AccessIni.GetPrivateProfileString("Machine_Type_Id", "userDefinedPLC", "", sb, 64, AccessIni.iniPath);
            MmsCommunication.userDefinedPLC = sb.ToString();

            AccessIni.GetPrivateProfileString("Machine_Type_Id", "mitsubishiTypeId", "", sb, 64, AccessIni.iniPath);
            MmsCommunication.mitsubishiTypeId = sb.ToString();

            AccessIni.GetPrivateProfileString("Machine_Type_Id", "omronTypeId", "", sb, 64, AccessIni.iniPath);
            MmsCommunication.omronTypeId = sb.ToString();

            SubMethod();
            //Thread.Sleep(30000);
        }

        protected override void OnStop()
        {
        }

        async void SubMethod()
        {
            ApiMmsInputParameter1 apiMmsParameter1 = new ApiMmsInputParameter1();
            apiMmsParameter1.Condition = new ConditionObject();

            StringBuilder sb = new StringBuilder();

            AccessIni.GetPrivateProfileString("MmsInitialCall", "ActionName", "", sb, 64, AccessIni.iniPath);
            apiMmsParameter1.ActionName = sb.ToString();

            AccessIni.GetPrivateProfileString("MmsInitialCall", "ServiceName", "", sb, 64, AccessIni.iniPath);
            apiMmsParameter1.ServiceName = sb.ToString();

            AccessIni.GetPrivateProfileString("MmsInitialCall", "Gateway_Id", "", sb, 64, AccessIni.iniPath);
            apiMmsParameter1.Condition.Gateway_Id = sb.ToString();

            AccessIni.GetPrivateProfileString("TimerInterval", "interval", "", sb, 16, AccessIni.iniPath);
            string intervalStr = sb.ToString();
            int interval = 0;            

            var response1 = default(ApiMmsResponse1);
            var listOfLists = default(List<List<DataObject1>>);

            try
            {
                interval = Convert.ToInt16(intervalStr);
                response1 = await MmsCommunication.getResponse<ApiMmsInputParameter1, ApiMmsResponse1>(apiAddressGet, apiMmsParameter1);
                listOfLists = MmsCommunication.GetLists(response1.data);
            }
            catch (Exception ex)
            {
                WriteToFile("Some error happend, " + DateTime.Now);

                return;
            }

            Timer timer = new Timer(interval);
            timer.Elapsed += delegate (Object o, ElapsedEventArgs e)
            {
                MmsCommunication.executeCommands(listOfLists);
            };
            //timer.Elapsed += OnElapsedTime;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        //private void OnElapsedTime(object source, ElapsedEventArgs e)
        //{
        //    WriteToFile("MmsService is still running at " + DateTime.Now);
        //}

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
