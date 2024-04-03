using Newtonsoft.Json;
using Service_RFID_BIG_READER.Reader;
using Service_RFID_BIG_READER.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Service_RFID_BIG_READER.Reader_DLL.Options;

namespace Service_RFID_BIG_READER.Reader_DLL
{
    internal class SwitchDLL : InterfaceRfidAPI
    {
        SYC_R16_DLL_DEFINE sYC_R16_DLL_DEFINE = null;
        ZTX_G20_DLL_DEFINE zTX_G20_DLL_DEFINE = null;
        ReaderType readerType;
        public SwitchDLL(ReaderType readerType)
        {
            this.readerType = readerType;
        }
        public bool Connect(string port)
        {
            bool result = false;
            if (readerType == ReaderType.SYC_R16)
            {
                Service1.WriteLog("SYC_R16");
                if (sYC_R16_DLL_DEFINE == null)
                    sYC_R16_DLL_DEFINE = new SYC_R16_DLL_DEFINE();
                result = sYC_R16_DLL_DEFINE.Connect(port);
                return result;
            }
            if (readerType == ReaderType.ZTX_G20)
            {
                Service1.WriteLog("ZTX_G20");
                if (zTX_G20_DLL_DEFINE == null)
                    zTX_G20_DLL_DEFINE = new ZTX_G20_DLL_DEFINE();
                result = zTX_G20_DLL_DEFINE.Connect(port);
                
                return result;
            }
            return result;
        }

        public bool Disconnect(Options.ConnectType connectType)
        {
            throw new NotImplementedException();
        }

        public void Inventory()
        {
            if (readerType == ReaderType.SYC_R16)
            {
                SYC_R16_DLLProject.ReadSingle();
                return;
            }
            if (readerType == ReaderType.ZTX_G20)
            {
                zTX_G20_DLL_DEFINE.Inventory();
                return;
            }
        }
    }
}
