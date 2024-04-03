using Newtonsoft.Json;
using Service_RFID_BIG_READER.Database;
using Service_RFID_BIG_READER.DTO;
using Service_RFID_BIG_READER.Entity;
using Service_RFID_BIG_READER.Reader_DLL;
using Service_RFID_BIG_READER.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Service_RFID_BIG_READER.Reader
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

        public TagInfo(string epc, string tid, string password,  string crc, string rssi, string ant, bool isNg)
        {
            this.epc = epc;
            this.tid = tid;
            this.password = password;
            this.crc = crc;
            this.rssi = rssi;
            this.atn = ant;
            this.isNg = isNg;
        }

       

        private async Task<DTOTagInfo> InHandle(Reader.TagInfo tagInfo)
        {
            DTOTagInfo dto = await SqliteDataAccess.FindByKey("tidNg", tagInfo.epc);
            if (dto == null)
            {
                Service1.PublishMessage(JsonConvert.SerializeObject(this), TopicPub.IN_NOTFOUND_TAG);
                return null;
            }
            return dto;

            /*if (!CheckRfidTagTime(dto.lastUpdate))
                return DTOTagInfo;

            await SqliteDataAccess.UpdateByKey(
                "TagInfo", "lastUpdate",
                DateTime.UtcNow.ToString(SqliteDataAccess._timeFormat),
                "tidNg", tagInfo.epc
                );

            if (dto.isIn)
                return false;

            await SqliteDataAccess.UpdateByKey(
               "TagInfo", "isIn", "1",
               "tidNg", tagInfo.epc
               );

            
            return true;*/
        }

        private async Task<DTOTagInfo> OutHandle(Reader.TagInfo tagInfo)
        {

            DTOTagInfo dto = await SqliteDataAccess.FindByKey("epcNg", tagInfo.epc);
            if (dto == null)
            {
                //"Không tìm thấy"
                return null;
            }
            return dto;

            /*if (!CheckRfidTagTime(dto.lastUpdate))
                return false;


            await SqliteDataAccess.UpdateByKey(
               "TagInfo", "lastUpdate",
               DateTime.UtcNow.ToString(SqliteDataAccess._timeFormat),
               "epcNg", tagInfo.epc
               );

            if (!dto.isIn)
                return false;
            await SqliteDataAccess.UpdateByKey(
              "TagInfo", "isIn", "0",
              "epcNg", tagInfo.epc
              );
            
            return true;*/
        }
    }
}
