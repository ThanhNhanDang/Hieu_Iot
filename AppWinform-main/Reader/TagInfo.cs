using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppWinform_main.Reader
{
    internal class TagInfo
    {
        public string epc { get; set; }
        public string tid { get; set; }
        public string password { get; set; }  
        public string crc { get; set; }
        public string rssi { get; set; }
        public string atn { get; set; }
        public bool isNg { get; set; }
        public TagInfo()
        {
            epc = string.Empty;
            tid = string.Empty;
            crc = string.Empty;
            rssi = string.Empty;
            atn = string.Empty;
        }

    }
}
