using AppWinform_main.DTO;
using AppWinform_main.Entity;
using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Dapper.SqlMapper;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AppWinform_main.Database
{
    internal class SqliteDataAccess
    {
        public static string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private static IDbConnection conn = new SQLiteConnection(LoadConnectionString());

        public static List<DTOTagInfo> LoadTag()
        {
            List<ETagInfo> entities;
            IEnumerable<ETagInfo> output = conn.Query<ETagInfo>("select * from TagInfo", new DynamicParameters());
            entities = output.ToList();
            if (entities.Count == 0) return null;
            //  conn.ExecuteAsync("ALTER TABLE TagInfo ADD COLUMN password TEXT NOT NULL");
            List<DTOTagInfo> dtos = new List<DTOTagInfo>();
            foreach (ETagInfo entity in entities)
            {
                dtos.Add(new DTOTagInfo(
                    entity.nameNg, entity.nameXe, entity.epcNg,
                    entity.epcXe, entity.tidNg, entity.tidXe,
                    entity.passNg, entity.passXe, entity.typeXe,
                    entity.lastUpdate, entity.isInNg == 1 ? true : false, entity.isInXe == 1 ? true : false
                   ));
            }
            return dtos;
        }

        public static async Task<List<ETagInfoSync>> SyncDatbase()
        {
            List<ETagInfoSync> entities;
            Task<IEnumerable<ETagInfoSync>> output = conn.QueryAsync<ETagInfoSync>("select tidNg, isInNg, isInXe from TagInfo", new DynamicParameters());
            await output;
            entities = output.Result.ToList();
            if (entities.Count == 0) return null;
            //  conn.ExecuteAsync("ALTER TABLE TagInfo ADD COLUMN password TEXT NOT NULL");
            return entities;


        }
        public static async Task<DTOTagInfo> FindByKey(string key, string value)
        {
            DTOTagInfo dto;
            Task<ETagInfo?> entity = conn.QueryFirstOrDefaultAsync<ETagInfo>($"select * from TagInfo where {key}='{value}'");
            await entity;
            if (entity.Result == null)
            {
                return null;
            }
            ETagInfo e = entity.Result;
            dto = new DTOTagInfo(
                e.nameNg, e.nameXe, e.epcNg,
                e.epcXe, e.tidNg, e.tidXe,
                e.passNg, e.passXe, e.typeXe,
                e.lastUpdate.ToLocalTime(), e.isInNg == 1 ? true : false, e.isInXe == 1 ? true : false,
                e.imgBienSoPath, e.imgNgPath, e.imgXePath);
            return dto;
        }

        public static async Task<DTOTagInfo> UpdateByKey(string table, string[] key, string[] value, string keyCondition, string valueCondition)
        {
            if (value.Length == 0 || key.Length == 0) return null;
            if (value.Length != key.Length) return null;

            int length = key.Length;
            string query = $"UPDATE {table} SET ";

            for (int i = 0; i < length; i++)
            {
                query += $"{key[i]} = '{value[i]}'";
                if (i < length - 1) query += ", ";
            }
            query += $"WHERE {keyCondition} = '{valueCondition}'";

            await conn.ExecuteAsync(query);

            return await FindByKey(keyCondition, valueCondition);
        }

        public static async Task<DTOTagInfo> UpdateByKey(string table, string key, string value, string keyCondition, string valueCondition)
        {
            string query = $"UPDATE {table} SET ";
            query += $"{key} = '{value}'";
            query += $"WHERE {keyCondition} = '{valueCondition}'";
            await conn.ExecuteAsync(query);
            return await FindByKey(keyCondition, valueCondition);
        }

        public static void SaveTag(DTOTagInfo dto)
        {
            ETagInfo entity = new ETagInfo(
                dto.nameNg, dto.nameXe, dto.epcNg,
                dto.epcXe, dto.tidNg, dto.tidXe,
                dto.passNg, dto.passXe, dto.typeXe, dto.isInNg ? 1 : 0, dto.isInXe ? 1 : 0
                );
            conn.Execute("insert into TagInfo (" +
                "nameNg, nameXe, epcNg," +
                "epcXe, tidNg, tidXe," +
                "passNg, passXe, typeXe, isIn) " +
                "values (" +
                "@nameNg, @nameXe, @epcNg," +
                "@epcXe, @tidNg, @tidXe," +
                "@passNg, @passXe, @typeXe, @isIn)", entity);
        }

        private static string LoadConnectionString(string id = "Default")
        {
            string t = $@"Data Source ={AppContext.BaseDirectory}Database\Database.db; Version = 3;";
            return t;
        }
    }
}
