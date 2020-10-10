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
    public class FolderDAL : BaseDAL, IFolderDAL
    { 
        public FolderDAL(ILogger<BaseDAL> logger, IDbHelper db) :base(logger, db)
        {
          
        }
   
        public List<TreeViewItem> All()
        {
            List<TreeViewItem> list = new List<TreeViewItem>();

            try
            { 
                string strSql = "select * from api_folder";
                DataTable dt = db.FillTable(strSql);

                DataRow[] rows = dt.Select("ParentSN=0");
                if (rows.Length > 0)
                {
                    foreach (DataRow dataRow in rows)
                    {
                        TreeViewItem tvItem = CreateTreeViewItem(dataRow);
                        tvItem.nodes = GetChildFolders(dt, int.Parse(dataRow["SN"].ToString()));
                        list.Add(tvItem);
                    }
                }
            }
            catch (Exception ex)
            { 
                throw ex;
            }
            return list;
        }

        private List<TreeViewItem> GetChildFolders(DataTable dt, int parentSN)
        {
            List<TreeViewItem> list = new List<TreeViewItem>();
            DataRow[] rows = dt.Select("ParentSN=" + parentSN.ToString());
            if (rows.Length > 0)
            {
                foreach (DataRow dataRow in rows)
                {
                    TreeViewItem tvItem = CreateTreeViewItem(dataRow);

                    tvItem.nodes = GetChildFolders(dt, int.Parse(dataRow["SN"].ToString()));
                    list.Add(tvItem);
                }
            }
            return list;
        }

        private TreeViewItem CreateTreeViewItem(DataRow dataRow)
        {
            FolderModel info = new FolderModel();
            info.SN = int.Parse(dataRow["SN"].ToString());
            info.ParentSN = int.Parse(dataRow["ParentSN"].ToString());
            info.FolderName = dataRow["FolderName"].ToString();
            info.RoutePath = dataRow["RoutePath"].ToString();

            List<object> tags = new List<object>();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                tags.Add(dataRow[column.ColumnName]);
            }

            string text = dataRow["FolderName"].ToString();
            string RoutePath = dataRow["RoutePath"].ToString();
            if (RoutePath != "")
            {
                text += "-" + RoutePath;
            }
            TreeViewItem tvItem = new TreeViewItem()
            {
                text = text,
                href = "",
                data = info
            };

            return tvItem;
        }

        private void DeleteChildFolder(int SN)
        { 
            string strSql = "select SN,FolderName,ParentSN from api_folder";
            DataTable dt = db.FillTable(strSql);
        }
    }
}
