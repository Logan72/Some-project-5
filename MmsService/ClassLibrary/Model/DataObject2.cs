using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Model
{
    public class DataObject2
    {
        public DataObject2(string Data_Field, string Value)
        {
            this.Data_Field = Data_Field;
            this.Value = Value;
        }
        public string Value { get; set; }
        public string Data_Field { get; set; }
    }
}
