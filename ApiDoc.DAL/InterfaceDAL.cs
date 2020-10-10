﻿using ApiDoc.IDAL;
using ApiDoc.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ApiDoc.DAL
{
    public class InterfaceDAL : BaseDAL, IInterfaceDAL
    {
        public InterfaceDAL(ILogger<BaseDAL> logger, IDbHelper db) :base(logger, db)
        {
             
        }
 
        public List<InterfaceModel> All()
        {
            List<InterfaceModel> list = new List<InterfaceModel>();

            try
            {
                string strSql = "select * from api_interface";
                DataTable dt = db.FillTable(strSql); 
                foreach (DataRow dataRow in dt.Rows)
                {
                    InterfaceModel info = new InterfaceModel();
                    base.CreateModel(info, dataRow);
                    list.Add(info);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("DbCommand->api_interface=>Insert(s) 出错\r\n" + ex.Message);
                throw ex;
            }
            return list;
        }

        public List<InterfaceModel> All(string title, string url, int fksn)
        {
            List<InterfaceModel> list = new List<InterfaceModel>();

            try
            {
                string strSql = "with temp as( select * from api_folder where SN =" + fksn.ToString();
                strSql += " union all ";
                strSql += "select b.* from temp a inner join api_folder b on b.[ParentSN] = a.SN ) ";
                strSql += "select b.* from api_interface b inner join temp c on c.SN = b.FKSN";
                strSql += " and title like '%" + title + "%'";
                strSql += " and url like '%" + url + "%'";

                //所以数据
                if (fksn == 0)
                {
                    strSql = "select * from api_interface"; 
                }

                DataTable dt = db.FillTable(strSql);

                foreach (DataRow dataRow in dt.Rows)
                { 
                    InterfaceModel info = new InterfaceModel();
                    base.CreateModel(info, dataRow);
                    list.Add(info);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("DbCommand->api_interface=>Insert(s) 出错\r\n" + ex.Message);
                throw ex;
            }
            return list;
        }
    }
}
