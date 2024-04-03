using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service_RFID_BIG_READER.Util
{
    internal class ConfigMapping
    {
        public static byte ENC_PASS = Convert.ToByte(ConfigurationManager.AppSettings["encPass"]);
    }
}
