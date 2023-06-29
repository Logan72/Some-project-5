using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
//using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary.Model;
using ClassLibrary.Sever_connection.Keyence_PLC;
using ClassLibrary.Sever_connection.Mitsubishi_PLC;
using ClassLibrary.Sever_connection.Omron_PLC;
using ClassLibrary.Supporting_functions;
//using Newtonsoft.Json.Linq;

namespace ClassLibrary
{
    public static class MmsCommunication
    {
        public static string apiAddressSend;
        static ConcurrentDictionary<string, bool> flags = new ConcurrentDictionary<string, bool>();
        public static string keyenceResetCommand;
        public static string mitsubishiResetCommand;
        public static string omronResetCommand;
        public static string userDefinedPLC;
        public static string mitsubishiTypeId;
        public static string omronTypeId;
        private static int count = 0;

        public static async Task<R> getResponse<P, R>(string address, P parameter)
        {
            var response = Activator.CreateInstance<R>();

            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri(address);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJta2FjIGl0IHF1eW5oIG5ndXllbiIsImlhdCI6MTY3NTEzMDkyNzU0NCwiZXhwIjoxNjc1MzkwMTI3NTQ0fQ.2Q-9OcpNN4IhQV0dSIO5kgZoCtA4xFWUsi9UWT7VeOw");

                HttpResponseMessage responsePost = await client.PostAsJsonAsync(address, parameter);

                //var jObject = JObject.FromObject(parameter);

                if (responsePost.IsSuccessStatusCode)
                {
                    response = await responsePost.Content.ReadAsAsync<R>();
                }
            }
            return response;
        }

        public static List<List<DataObject1>> GetLists(DataObject1[] dataObjects)
        {
            Array.Sort<DataObject1>(dataObjects, delegate (DataObject1 x, DataObject1 y)
            {
                return x.Machine_Id.CompareTo(y.Machine_Id);
            });

            List<List<DataObject1>> result = new List<List<DataObject1>>();

            List<DataObject1> list = new List<DataObject1>();

            string previousMachine = null;

            foreach (DataObject1 dataObject in dataObjects)
            {
                if (previousMachine != dataObject.Machine_Id) // forming lists of commands belonging to each machine
                {
                    previousMachine = dataObject.Machine_Id;

                    if (list.Count != 0)
                    {
                        List<DataObject1> clone = new List<DataObject1>(list);

                        list.Clear();

                        result.Add(clone);
                    }

                    list.Add(dataObject);
                }
                else
                {
                    list.Add(dataObject);
                }
            }

            if (list.Count != 0) // dealing with the last list that can't be dealt with in the above foreach loop
            {
                result.Add(list);
            }

            return result;
        }

        public static void executeCommands(List<List<DataObject1>> list)
        {
            bool flag;
            foreach (List<DataObject1> machine in list)
            {
                if ((machine[0].Machine_Id != "mkac_omronPLC_0014") || (flags.TryGetValue(machine[0].Machine_Id, out flag) && !flag)) continue;

                createRunThread(machine);
            }
        }

        private static void createRunThread(List<DataObject1> list) //create and run a thread for each machine
        {
            flags.AddOrUpdate(list[0].Machine_Id, false, (k, v) => false);

            Thread thread = new Thread(new ThreadStart(async delegate ()
            {
                ApiMmsInputParameter2 apiMmsInputParameter2 = new ApiMmsInputParameter2();
                apiMmsInputParameter2.Machine_Id = list[0].Machine_Id;
                apiMmsInputParameter2.Data_Array = new List<DataObject2>();

                try
                {
                    if (list[0].Machine_Type_Id == userDefinedPLC)
                    {
                        Communication_PLC communication_PLC = new Communication_PLC();

                        if (communication_PLC.TcpConnect(list[0].Ip, Convert.ToInt32(list[0].Port)))
                        {
                            count++;
                            foreach (DataObject1 commandData in list)
                            {
                                string data;

                                if (commandData.Command_Type.Equals("R"))
                                {
                                    data = communication_PLC.SendCommand(commandData.Command);

                                    if (!(data.Equals("timeout") || data.Equals("not connect")))
                                    {
                                        data = data.Replace("\r", "").Replace("\n", "");

                                        switch (commandData.Value_Type)
                                        {
                                            case "1":
                                                data = data.TrimStart('0');

                                                break;
                                            case "2":
                                                try
                                                {
                                                    data = Support.HexStringToString(data);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine("there's something wrong");
                                                }
                                                break;
                                        }

                                        apiMmsInputParameter2.Data_Array.Add(new DataObject2(commandData.Data_Field, data));
                                    }
                                    else
                                    {
                                        //Console.WriteLine(++DataObject.errorCount + "########################################################################################################\n########################################################################################################");
                                        //throw new Exception(data);
                                        Console.WriteLine("some error");
                                    }

                                    Console.WriteLine(data + " =====FROM===== " + commandData.Ip + " (" + commandData.Command + ") " + data.Length);
                                }
                                else if (commandData.Command_Type.Equals("ReadString"))
                                {
                                    data = communication_PLC.SendCommand(commandData.Command);

                                    if (data.Equals("timeout") || data.Equals("not connect"))
                                    {
                                        data = "0|";
                                    }

                                    Console.WriteLine(data + " =====FROM===== " + commandData.Ip + " (" + commandData.Command + ") " + data.Length + " count " + count);

                                    apiMmsInputParameter2.Data_String = data;
                                    apiMmsInputParameter2.Command_Type = commandData.Command_Type;
                                }
                                //else if (commandData.Command_Type.Equals("W"))
                                //{
                                //    if (Convert.ToInt32(commandData.Value_Type) == 2)
                                //    {
                                //        commandData.Data_Field = Support.StringToHexString(commandData.Data_Field);
                                //    }

                                //    data = communication_PLC.SendCommand(commandData.Command + " " + commandData.Data_Field);

                                //    if (!(data.Equals("timeout") || data.Equals("not connect")))
                                //    {
                                //        data = data.Replace("\r", "").Replace("\n", "");
                                //    }
                                //    else
                                //    {
                                //        //Console.WriteLine(++DataObject.errorCount + "########################################################################################################\n########################################################################################################");
                                //        //throw new Exception(data);
                                //    }

                                //    Console.WriteLine("writing " + commandData.Ip + " (" + commandData.Command + ") " + data);
                                //}
                            }

                            var response2 = await MmsCommunication.getResponse<ApiMmsInputParameter2, ApiMmsResponse2>(apiAddressSend, apiMmsInputParameter2);
                            if (response2.Message == "reset")
                            {
                                communication_PLC.SendCommand(response2.Message);
                            }

                            communication_PLC.TcpClose();
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                        }
                        else
                        {
                            Console.WriteLine(list[0].Ip + " is disconnected.");
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                            //throw new Exception(list[0].IP + " is disconnected");
                        }
                    }
                    else if (list[0].Machine_Type_Id == mitsubishiTypeId)
                    {
                        McProtocolTcp mcProtocolTcp = new McProtocolTcp(list[0].Ip, 5040, McFrame.MC3E);

                        if (await mcProtocolTcp.Open() == 0)
                        {
                            foreach (DataObject1 commandData in list)
                            {
                                string[] commandArg = commandData.Command.Split(' ');

                                if (commandData.Command_Type.Equals("R"))
                                {
                                    try
                                    {
                                        string data;

                                        data = await mcProtocolTcp.ReadAndGetResult(commandArg[0], Convert.ToInt32(commandArg[1]), Convert.ToInt32(commandData.Value_Type), Convert.ToInt32(commandData.Value_Number));

                                        apiMmsInputParameter2.Data_Array.Add(new DataObject2(commandData.Data_Field, data));

                                        Console.WriteLine(data + " =====FROM===== " + commandData.Ip + " (" + commandData.Command + ") " + data.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        //Console.WriteLine(++DataObject.errorCount + "########################################################################################################\n########################################################################################################");
                                        Console.WriteLine("error =====FROM===== " + commandData.Ip + " (" + commandData.Command + ")");
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                //else if (commandData.Command_Type.Equals("W"))
                                //{
                                //    try
                                //    {
                                //        await mcProtocolTcp.WriteDeviceBlock(commandArg[0], Convert.ToInt16(commandData.Data_Field));
                                //        Console.WriteLine("writing " + commandData.Data_Field + " into " + commandData.Ip + " (" + commandData.Command + ") OK");
                                //    }
                                //    catch (Exception ex)
                                //    {
                                //        //Console.WriteLine(++DataObject.errorCount + "########################################################################################################\n########################################################################################################");
                                //        Console.WriteLine("writing " + commandData.Data_Field + " into " + commandData.Ip + " (" + commandData.Command + ") error");
                                //        Console.WriteLine(ex.Message);
                                //    }
                                //}
                            }

                            var response2 = await MmsCommunication.getResponse<ApiMmsInputParameter2, ApiMmsResponse2>(apiAddressSend, apiMmsInputParameter2);
                            if (response2.Message == "reset")
                            {
                                await mcProtocolTcp.WriteDeviceBlock(mitsubishiResetCommand, 0);
                            }

                            mcProtocolTcp.Dispose();
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                        }
                        else
                        {
                            Console.WriteLine(list[0].Ip + " is disconnected.");
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                            //throw new Exception(list[0].IP + " is disconnected");
                        }
                    }
                    else if (list[0].Machine_Type_Id == omronTypeId)
                    {
                        OmronConnection omronConnection = new OmronConnection(list[0].Ip, Convert.ToInt32(list[0].Port));

                        if (omronConnection.PLC_Ping())
                        {
                            foreach (DataObject1 commandData in list)
                            {
                                string[] commandArg = commandData.Command.Split(' ');

                                if (commandData.Command_Type.Equals("R"))
                                {
                                    try
                                    {
                                        string data;

                                        data = omronConnection.ReadDM(commandArg[0], Convert.ToInt32(commandArg[1]), Convert.ToInt32(commandArg[2]), Convert.ToInt32(commandData.Value_Type));

                                        apiMmsInputParameter2.Data_Array.Add(new DataObject2(commandData.Data_Field, data));

                                        Console.WriteLine(data + " =====FROM===== " + commandData.Ip + " (" + commandData.Command + ") " + data.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        //Console.WriteLine(++DataObject.errorCount + "########################################################################################################\n########################################################################################################");
                                        Console.WriteLine("error =====FROM===== " + commandData.Ip + " (" + commandData.Command + ")");
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                //else if (commandData.Command_Type.Equals("W"))
                                //{
                                //    try
                                //    {
                                //        if (omronConnection.WriteDM(commandArg[0], Convert.ToInt32(commandArg[1]), Convert.ToInt32(commandArg[2]), commandData.Data_Field, Convert.ToInt32(commandData.Value_Type)))
                                //        {
                                //            Console.WriteLine("writing " + commandData.Data_Field + " into " + commandData.Ip + " (" + commandData.Command + ") OK");
                                //        }
                                //        else Console.WriteLine("writing " + commandData.Data_Field + " into " + commandData.Ip + " (" + commandData.Command + ") error");

                                //    }
                                //    catch (Exception ex)
                                //    {
                                //        //Console.WriteLine(++DataObject.errorCount + "########################################################################################################\n########################################################################################################");
                                //        Console.WriteLine("writing " + commandData.Data_Field + " into " + commandData.Ip + " (" + commandData.Command + ") error");
                                //        Console.WriteLine(ex.Message);
                                //    }
                                //}
                            }

                            var response2 = await MmsCommunication.getResponse<ApiMmsInputParameter2, ApiMmsResponse2>(apiAddressSend, apiMmsInputParameter2);

                            //JObject jObject = JObject.FromObject(apiMmsInputParameter2);

                            if (response2.Message == "reset")
                            {
                                //flags.TryUpdate(list[0].Machine_Id, false, true);
                                string[] commandArg = omronResetCommand.Split(' ');
                                omronConnection.WriteDM(commandArg[0], Convert.ToInt32(commandArg[1]), Convert.ToInt32(commandArg[2]), "0", 1);
                                //Console.WriteLine(omronConnection.ReadDM("DM", 2015, 1));
                            }

                            omronConnection.Close();
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                        }
                        else
                        {
                            Console.WriteLine(list[0].Ip + " is disconnected.");
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                            //throw new Exception(list[0].IP + " is disconnected");
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }));

            thread.Start();
        }
    }
}
