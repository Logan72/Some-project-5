using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ClassLibrary.Model
{
    public class ApiMmsInputParameter2
    {
        public string Machine_Id { get; set; }
        public string Command_Type { get; set; }
        public string Data_String { get; set; }
        public List<DataObject2> Data_Array { get; set; }
    }
}
