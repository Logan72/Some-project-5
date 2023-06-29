using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Model
{
    public class ApiMmsInputParameter1
    {
        public string ServiceName { get; set; }
        public string ActionName { get; set; }
        public ConditionObject Condition { get; set; }
    }
}
