using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Model
{
    public class ApiMmsResponse1
    {
        public int code { get; set; }
        public string message { get; set; }
        public DataObject1[] data { get; set; }
    }
}
