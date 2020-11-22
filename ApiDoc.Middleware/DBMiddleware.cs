﻿using ApiDoc.IDAL;
using ApiDoc.Models;
using ApiDoc.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic; 
using System.Data;  
using System.Net.Http;
using System.Reflection;
using System.Text;  
using System.Threading.Tasks; 
using System;
using Newtonsoft.Json; 
using Microsoft.Extensions.Logging; 
using Autofac;
using System.Data.Common;
using System.IO;
using ApiDoc.Models.Components;
using Newtonsoft.Json.Linq;
using JMS;
using Microsoft.AspNetCore.Http.Features;

namespace ApiDoc.Middleware
{
    public class DBMiddleware
    {

        private const string _ReqFilter = "SQLTEXT_"; //接收的是复杂语句

        private readonly RequestDelegate next;
        private readonly IParamDAL paramDAL;
        private readonly IDbHelper dbHelp;
        private readonly DBRouteValueDictionary routeDict;
        private readonly MyConfig myConfig;
        private readonly ILogger<DBMiddleware> logger;
        private readonly IComponentContext componentContext;
        private HttpContext context;
          
        public DBMiddleware(RequestDelegate _next,
                            IInterfaceDAL interfaceDAL,
                            IFlowStepDAL flowStepDAL,
                            IParamDAL paramDAL,
                            IDbHelper dbHelp,
                            DBRouteValueDictionary _routeDict,
                            IConfiguration config,
                            MyConfig myConfig,
                            ILogger<DBMiddleware> logger,
                            IComponentContext componentContext)
        {
            this.next = _next;
            this.paramDAL = paramDAL;
            this.dbHelp = dbHelp;
            this.routeDict = _routeDict;
            this.myConfig = myConfig;
            this.logger = logger;
            this.componentContext = componentContext;
            string SqlConnStr = config.GetConnectionString("ApiDocConnStr");

            //List<FilterCondition> filters = new List<FilterCondition>();
            //FilterCondition filter = new FilterCondition();
            //filter.BracketL = "(";
            //filter.BracketR = ")";
            //filter.ColumnName = "SN";
            //filter.Condition = ConditionTypeEnum.Equal;
            //filter.Value = "123";
            //filter.ValueType = "";
            //filter.JoinType = "And";
            //filters.Add(filter);

            //filter = new FilterCondition();
            //filter.BracketL = "(";
            //filter.BracketR = ")";
            //filter.ColumnName = "SN";
            //filter.Condition = ConditionTypeEnum.Equal;
            //filter.Value = "123";
            //filter.JoinType = "";
            //filters.Add(filter);

            //Dictionary<string, object> dist = new Dictionary<string, object>();
            //dist.Add("SN", 1);
            //dist.Add(_ReqFilter, filters);

            //string json = JsonConvert.SerializeObject(dist);

            //加载路由集合 
            List<InterfaceModel> dtInterface = interfaceDAL.Query(false);
            foreach (InterfaceModel model in dtInterface)
            {
                //加载步骤
                int SN = model.SN;
                string Url = model.Url;  
                model.Steps = flowStepDAL.QueryOfParam(SN);

                //接口参数
                string auth = "";
                foreach (ParamModel param in this.paramDAL.Query(SN))
                {
                    if (auth != "")
                    {
                        auth += "_";
                    }
                    auth += param.ParamName.Trim();
                }
                auth = auth.ToLower();
                model.Auth = auth;

                if (!routeDict.ContainsKey(Url))
                {
                    routeDict.Add(Url, model);
                }
            }

            logger.LogInformation("加载接口完成,共" + dtInterface.Count.ToString());

        }

        /// <summary>
        /// 获取响应内容
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task<string> GetResponse()
        {
            //读流
            context.Request.EnableBuffering();
            var requestReader = new StreamReader(context.Request.Body);
            var requestContent = requestReader.ReadToEnd();
            context.Request.Body.Position = 0;

            using (var ms = new MemoryStream())
            {
                context.Response.Body = ms;
                RequestDelegate task = new RequestDelegate((context) => {

                    var form = context.Request.Form;
                    return null;
                });
                await next(context);

                ms.Position = 0;
                var responseReader = new StreamReader(ms);
                var responseContent = responseReader.ReadToEnd();
                ms.Position = 0;
            } 
            return null;
        }
      
        public async Task Invoke(HttpContext context)
        {
            this.context = context;
            string path = context.Request.Path.ToString();
            switch (path)
            {
                case "/CS":
                    await this.WriteAsync("欢迎浏览ApiDoc");
                    return;
                case "/CSDB":
                    return;
            }
          
            if (this.routeDict.ContainsKey(path))
            {
 
                    InterfaceModel response = this.routeDict[path];
                    Dictionary<string, object> paramList = null; //接口参数
                    string msg;
                    bool bOK = this.CheckDataBase(response, out paramList, out msg);
                    if (!bOK) //如果验证失败
                    {
                        await this.InvokeException(msg);
                    }
                    else
                    {
                        if (response.IsJms)
                        {
                            await this.InvokeJms(context, paramList);
                        }
                        else
                        {

                            await InvokeDB(context, paramList);
                        }
                    } 
            }
            else
            {
                await next(context);
            }
        }

        private async Task InvokeDB(HttpContext context, Dictionary<string, object> paramList)
        { 
            string version = "";

            //检测返回结构: DataResult(默认), layui:
            bool bOK = context.Request.Headers.TryGetValue("Version", out StringValues strLayui);
            if (bOK)
            {
                version = strLayui.ToString().ToLower();
            }

            string path = context.Request.Path.ToString();
            InterfaceModel response = this.routeDict[path];
            string exceMsg = ""; //存储异常的存储过程名  

            IDbConnection connection = this.componentContext.ResolveNamed<IDbConnection>(response.DataType);
            string connStr = this.myConfig[response.DataType].ApiDocConnStr;
            connection.ConnectionString = connStr;
            IDbTransaction tran = null;
            try
            {
                connection.Open();

                if (response.IsTransaction)
                {
                    tran = connection.BeginTransaction();
                }

                //执行第一步 
                string text = this.InvokeStep(response, connection, tran, paramList, version, out exceMsg);
                if (exceMsg != "")
                {
                    if (response.IsTransaction)
                    {
                        tran.Rollback();
                    }

                    await this.InvokeException(exceMsg);
                    return;
                }

                if (response.IsTransaction)
                {
                    tran.Commit();
                }

                await this.WriteAsync(text);
            }
            catch (System.Exception ex)
            {
                if (response.IsTransaction)
                {
                    tran.Rollback();
                }
                await this.InvokeException(ex.Message);
            }
            finally
            {
                connection.Close();
            }

        }

        //验证数据库类型
        private bool CheckDataBase(InterfaceModel response, out Dictionary<string, object> dict, out string msg)
        {
            msg = "";
            dict = new Dictionary<string, object>();

            if (response.DataType == "" || response.DataType == null)
            {
                msg = "没有配置数据库类型";
                return false;
            }

            if (response.DataType != "SqlServer" && response.DataType != "Oracle" && response.DataType != "MySql")
            {
                msg = "不支持数据库类型:" + response.DataType;
                return false;
            }

            string method = response.Method.ToLower();
            if (method != "post" && method != "get")
            {
                msg = "此平台只支持post,get";
                return false;
            }

            //参数验证
            string auth;  //参数规则  
            //从Request中获取参数集合 
            dict = this.CreateDict(method, out auth);
            auth = auth.ToLower();
            if (response.Auth != auth)
            {
                msg = "接收参数[" + response.Auth + "]与规则[" + auth + "]不匹配";
                return false;
            }

            //步骤
            if (response.Steps.Count == 0)
            {
                msg = response.Url + "没有任何步骤，请维护";
                return false;
            } 

            return true;
        }

        //解析接收的参数
        private Dictionary<string, object> CreateDict(string method, out string auth)
        {
            auth = "";
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (method == "post")
            {
                if (context.Request.ContentType == "application/x-www-form-urlencoded") //纯表单
                {
                    foreach (KeyValuePair<string, StringValues> kv in context.Request.Form)
                    {
                        if (kv.Key == "__RequestVerificationToken")
                        {
                            continue;
                        }
                        if (!dict.ContainsKey(kv.Key))
                        {
                            if (auth != "")
                            {
                                auth += "_";
                            }
                            auth += kv.Key;
                            dict.Add(kv.Key, kv.Value[0]);
                        }
                    }
                }
                else if (context.Request.ContentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase)) //带附件的表单
                { 
                    FormFeature formFeature = new FormFeature(context.Request);
                    IFormCollection iFormCollection = formFeature.ReadForm();

                    foreach (string key in iFormCollection.Keys)
                    { 
                        if (!dict.ContainsKey(key))
                        {
                            if (auth != "")
                            {
                                auth += "_";
                            }
                            auth += key;

                            StringValues stringValues = iFormCollection[key]; 
                            dict.Add(key, stringValues[0]);
                        }
                    } 
                    if (iFormCollection.Files.Count > 0)
                    {
                        IFormFile formFile = iFormCollection.Files[0];
                        //var target = new System.IO.MemoryStream();
                        //formFile.CopyTo(target);

                        //byte[] buffer = target.ToArray();
                        dict.Add(formFile.Name, formFile);

                        if (auth != "")
                        {
                            auth += "_";
                        } 
                        auth += formFile.Name;
                    }

                }
                else
                {
                    var reader = new StreamReader(context.Request.Body);
                    var contentFromBody = reader.ReadToEnd();
                    if (contentFromBody != "")
                    { 
                        dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(contentFromBody); 
                    }

                    foreach (KeyValuePair<string, object> kv in dict)
                    {
                        if( kv.Key != _ReqFilter)
                        {
                            if (auth != "")
                            {
                                auth += "_";
                            }
                            auth += kv.Key;
                        } 
                    }
                }
            }
            else if (method == "get")
            {
                dict = new Dictionary<string, object>();
                foreach (KeyValuePair<string, StringValues> kv in context.Request.Query)
                {
                    if (!dict.ContainsKey(kv.Key))
                    {
                        if (auth != "")
                        {
                            auth += "_";
                        }
                        auth += kv.Key;
                        dict.Add(kv.Key, kv.Value[0]);
                    }
                }
            }

            return dict;
        }
           
        private string InvokeStep(InterfaceModel response, IDbConnection connection, IDbTransaction tran, Dictionary<string, object> dict, string version, out string exceMsg)
        {
            exceMsg = "";

            DataRow dataPre = null;
            string result = "";
            int rowIndex = 0;
            int count = response.Steps.Count; 
            foreach (FlowStepModel step in response.Steps)
            {
                if (step.CommandText == "")
                {
                    exceMsg = step.StepName + " 没有数据库语句，请维护";
                    return "";
                }

                connection.ChangeDatabase(step.DataBase);
                 
                IDbCommand cmd = connection.CreateCommand();
                cmd.CommandTimeout = 0;
                cmd.Connection = connection;

                if (response.IsTransaction)
                {
                    cmd.Transaction = tran;
                }

                IDbDataAdapter sqlDA = this.componentContext.ResolveNamed<IDbDataAdapter>(response.DataType);
                sqlDA.SelectCommand = cmd;

                //初始化参数
                switch (step.CommandType)
                {
                    case "Fun": 
                    case "Text":
                        cmd.CommandType = CommandType.Text;
                        break;
                    case "StoredProcedure":
                        cmd.CommandType = CommandType.StoredProcedure;
                        break;
                }
                cmd.CommandText = step.CommandText;
                cmd.Parameters.Clear();
                bool bOK = CreateParam(response, cmd, dataPre, step, dict, out exceMsg);
                if (!bOK)
                {
                    return "";
                }

                try
                {
                    //执行Sql
                    if (rowIndex == count - 1) //最后一步
                    { 
                        List<FilterCondition> filters = null;
                        if (dict.ContainsKey(_ReqFilter))
                        {
                            JArray ja = (JArray)dict[_ReqFilter];
                            filters = ja.ToObject<List<FilterCondition>>();
                        }

                        if (version == "layui")
                        {
                            result = this.ExecSqlLayui(response, connection, sqlDA, cmd, filters);
                        }
                        else
                        {
                            result = this.ExecSql(response, connection, sqlDA, cmd, filters);
                        }
                    }
                    else
                    {
                        //如果不是最后一步，存储当前步结果
                        DataSet ds = new DataSet();
                        sqlDA.Fill(ds); 

                        if (ds.Tables.Count > 0 && ds.Tables[ds.Tables.Count - 1].Rows.Count > 0 )
                        {
                            DataTable dt = ds.Tables[ds.Tables.Count - 1]; 
                            dataPre = dt.Rows[0];
                        }
                    }

                }
                catch (Exception ex)
                {
                    exceMsg = response.Url　+ ">>" + step.StepName + ">>" + ex.Message; 
                    this.logger.LogError(exceMsg);
                    return "";
                }
                
                rowIndex++;
            } 
            return result;
        }
 

        //执行最后的sql
        private string ExecSql(InterfaceModel response, IDbConnection connection, IDbDataAdapter sqlDA, IDbCommand cmd, List<FilterCondition> filters)
        { 
            XmlHelper xmlHelp = new XmlHelper(); 
            string json = "";
            object dataResult = new object();
            object result = new object(); 
            switch (response.ExecuteType)
            {
                case "Scalar":
                    DataResult dataResult1 = new DataResult();
                    dataResult1.DataType = 200;
                    dataResult1.Result = cmd.ExecuteScalar();
                    dataResult = dataResult1;
                    result = dataResult1.Result;
                    break;
                case "Int":
                    IntDataResult intDataResult = new IntDataResult();
                    intDataResult.Result = cmd.ExecuteNonQuery();
                    intDataResult.DataType = 200;
                    dataResult = intDataResult;
                    result = intDataResult.Result;
                    break;
                case "DataSet": 
                    DataSet ds = new DataSet(); 
                    DSDataResult dSDataResult = new DSDataResult();
                    if (filters != null)
                    {
                        //复杂查询需要再过滤一下最后一个表
                        int lastIndex = ds.Tables.Count - 1;
                        string msg;
                        string filter = this.CreateFileterString(filters, out msg);
                        if (msg != "")
                        {
                            throw new Exception(msg); 
                        }

                        sqlDA.Fill(ds);
                        if (ds.Tables.Count > 0 && ds.Tables[lastIndex].Rows.Count > 0)
                        {
                            DataTable table = ds.Tables[ds.Tables.Count - 1]; 
                            table.DefaultView.RowFilter = filter;
                            DataTable dtNew = table.DefaultView.ToTable();
                            ds.Tables.RemoveAt(lastIndex);
                            ds.Tables.Add(dtNew); 
                            dSDataResult.Result = ds; 
                        }
                        else
                        {
                            dSDataResult.Result = ds;
                        }
                    }
                    else
                    {
                        sqlDA.Fill(ds);
                        dSDataResult.Result = ds;  
                    }
                    dataResult = dSDataResult;
                    dSDataResult.DataType = 200;
                    break;
            }
             
            if (response.SerializeType == "Xml")
            {
                switch (response.ExecuteType)
                {
                    case "Scalar":
                        json = xmlHelp.SerializeXML<DataResult>(dataResult);
                        break;
                    case "Int":
                        json = xmlHelp.SerializeXML<IntDataResult>(dataResult);
                        break;
                    case "DataSet":
                        json = xmlHelp.SerializeXML<DSDataResult>(dataResult);
                        break;
                } 
            }
            else if (response.SerializeType == "Json")
            {
                json = JsonConvert.SerializeObject(dataResult); 
            }
            else
            {
                json = result.ToString();
            }
            
            return json;
        }

        //返因LayUI的结构
        private string ExecSqlLayui(InterfaceModel response, IDbConnection connection, IDbDataAdapter sqlDA, IDbCommand cmd, List<FilterCondition> filters)
        {
            XmlHelper xmlHelp = new XmlHelper();
            string json = "";
            LayuiResult dataResult = new LayuiResult();
            dataResult.code = 0;
            dataResult.count = 0;
            dataResult.msg = "";
            dataResult.data = "{0}";

            object result = new object(); 
            switch (response.ExecuteType)
            {
                case "Scalar":
                    result = cmd.ExecuteScalar(); 
                    break;
                case "Int": 
                    result = cmd.ExecuteNonQuery();  
                    break;
                case "DataSet":
                    DataSet ds = new DataSet();
                    sqlDA.Fill(ds);
                    DataTable table = new DataTable();
                    if (filters != null)
                    {
                        //复杂查询需要再过滤一下最后一个表 
                        string msg;
                        string filter = this.CreateFileterString(filters, out msg);
                        if (msg != "")
                        {
                            throw new Exception(msg);
                        } 
                        DataTable table0 = ds.Tables[0];
                        table0.DefaultView.RowFilter = filter;
                        DataTable dtNew = table0.DefaultView.ToTable();
                        table = dtNew;
                    }
                    else
                    { 
                        table = ds.Tables[0];
                    }

                    dataResult.count = table.Rows.Count;
                    result = table;
                    break;
            }

            dataResult.data = result;
            if (response.SerializeType == "Xml")
            {
                
            }
            else if (response.SerializeType == "Json")
            {
                json = JsonConvert.SerializeObject(dataResult); 
            }
            else
            {
                json = result.ToString();
            }

            return json;
        }

        //创建参数
        private bool CreateParam(InterfaceModel response, IDbCommand cmd, DataRow dataPre, FlowStepModel step, Dictionary<string, object> dict,  out string exceMsg)
        {
            exceMsg = "";
            if (step.Params != null)
            { 
                foreach (FlowStepParamModel param in step.Params)
                { 
                    string str = step.StepName + ">>参数" + param.ParamName;
                    object value = null;
                    string paramName = param.ParamName.Trim();
                    if (param.IsPreStep) //如果从上一步取值
                    { 
                        bool bParamName = dataPre.Table.Columns.Contains(paramName);
                        if (dataPre == null || !bParamName)
                        {
                            //如果有默认值
                            if (param.DefaultValue != "")
                            {
                                value = param.DefaultValue;
                            }
                            else
                            {
                                exceMsg = str + "无法从上一步取值值";
                                return false;
                            } 
                        }
                        else
                        {
                            value = dataPre[paramName];
                        } 
                    }
                    else //从Request中取值 
                    {
                        if (dict.ContainsKey(paramName))
                        {
                            if (param.DataType == "Image")
                            { 
                                string directory = Directory.GetCurrentDirectory() + "\\wwwroot\\Image\\";
                                if (!System.IO.Directory.Exists(directory))
                                {
                                    System.IO.Directory.CreateDirectory(directory);
                                }

                                string fileName =  DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString() + ".png";
                                string path = directory + fileName;                                                               //硬盘文件名称　
                                string imagePath = context.Request.Scheme + "://" + context.Request.Host + "/Image/" + fileName;　//网络路径
                                value = imagePath; 
                                object objFile = dict[paramName]; 
                                if (objFile != null)
                                {
                                    IFormFile formFile = (IFormFile)dict[paramName];
                                    using (var stream = System.IO.File.Create(path))
                                    {
                                        formFile.CopyTo(stream); 
                                    }
                                } 
                            }
                            else
                            {
                                value = dict[paramName];
                            } 
                        }
                        else
                        {
                            //如果有默认值
                            if (param.DefaultValue != "")
                            {
                                value = param.DefaultValue;
                            } 
                            else
                            {
                                exceMsg = str + "没有在Request中获取到值";
                                return false;
                            } 
                        } 
                    }

                    DbParameter dbParameter = this.componentContext.ResolveNamed<DbParameter>(response.DataType);
                    dbParameter.ParameterName ="@" + paramName;
                    dbParameter.Value = value;
                    cmd.Parameters.Add(dbParameter);
                }
            }
            return true;
        }
      
        #region jms

        private Dictionary<string, object> CreateParamJms(DataRow dataPre, FlowStepModel step, Dictionary<string, object> dict, out string exceMsg)
        {
            Dictionary<string, object> keyValues = new Dictionary<string, object>();
            exceMsg = "";
            if (step.Params != null)
            {
                foreach (FlowStepParamModel param in step.Params)
                {
                    string str = step.StepName + ">>参数" + param.ParamName;
                    object value = null;
                    string paramName = param.ParamName.Trim();
                    if (param.IsPreStep) //如果从上一步取值
                    {
                        bool bParamName = dataPre.Table.Columns.Contains(paramName);
                        if (dataPre == null || !bParamName)
                        {
                            //如果有默认值
                            if (param.DefaultValue != "")
                            {
                                value = param.DefaultValue;
                            }
                            else
                            {
                                exceMsg = str + "无法从上一步取值值";
                                break;
                            }
                        }
                        else
                        {
                            value = dataPre[paramName];
                        }
                    }
                    else //从Request中取值 
                    {
                        if (dict.ContainsKey(paramName))
                        { 
                           value = dict[paramName]; 
                        }
                        else
                        {
                            //如果有默认值
                            if (param.DefaultValue != "")
                            {
                                value = param.DefaultValue;
                            }
                            else
                            {
                                exceMsg = str + "没有在Request中获取到值";
                                break;
                            }
                        }
                    }

                    keyValues.Add(paramName, value);
                }
            }
            return keyValues;
        }

        private async Task InvokeJms(HttpContext context, Dictionary<string, object> paramList)
        {
            string version = "";

            //检测返回结构: DataResult(默认), layui:
            bool bOK = context.Request.Headers.TryGetValue("Version", out StringValues strLayui);
            if (bOK)
            {
                version = strLayui.ToString().ToLower();
            }

            string path = context.Request.Path.ToString();
            InterfaceModel response = this.routeDict[path];
            string exceMsg = ""; //存储异常的存储过程名  

            string Address = this.myConfig.GatewayAddress.Address;
            int port = this.myConfig.GatewayAddress.Port;

            JMSClient tran = new JMSClient(Address, port);
            try
            { 
                DataRow dataPre = null;
                string result = "";
                int rowIndex = 0;
                int count = response.Steps.Count;
                foreach (FlowStepModel step in response.Steps)
                {
                    if (step.CommandText == "")
                    {
                        exceMsg = step.StepName + " 没有数据库语句，请维护";
                        tran.Rollback();
                        await this.InvokeException(exceMsg);
                        return;
                    }

                    //初始化参数
                    string CommandType = "Text";
                    switch (step.CommandType)
                    {
                        case "Fun":
                        case "Text":
                            CommandType = "Text";
                            break;
                        case "StoredProcedure":
                            CommandType = "StoredProcedure";
                            break;
                    }

                    if (string.IsNullOrEmpty(step.ServiceName))
                    {
                        exceMsg = step.StepName + ">>没有选服务名称!";
                        tran.Rollback();
                        await this.InvokeException(exceMsg);
                        return;
                    }

                    IMicroService service = tran.GetMicroService(step.ServiceName); 
                    if (service == null)
                    {
                        tran.Rollback();
                        await this.InvokeException($"无法从{Address}网关中获取服务>ErpService");
                        return;
                    }

                    JmsCommand dbCommand = new JmsCommand();
                    dbCommand.DataBase = step.DataBase;
                    dbCommand.CommandType = CommandType;
                    dbCommand.CommandText = step.CommandText;

                    Dictionary<string, object> paralist = CreateParamJms(dataPre, step, paramList, out exceMsg);
                    if (exceMsg != "")
                    {
                        //创建参数出错
                        await this.InvokeException(exceMsg);
                        return;
                    }
                    dbCommand.Parameters = paralist;

                    try
                    {
                        //执行Sql
                        if (rowIndex == count - 1) //最后一步
                        {
                            List<FilterCondition> filters = null;
                            if (paramList.ContainsKey(_ReqFilter))
                            {
                                JArray ja = (JArray)paramList[_ReqFilter];
                                filters = ja.ToObject<List<FilterCondition>>();
                            }

                            if (version == "layui")
                            {
                                result = this.ExecSqlJmsLayui(response, service, dbCommand, filters);
                            }
                            else
                            {
                                result = this.ExecSqlJms(response, service, dbCommand, filters);
                            }
                        }
                        else
                        {
                            //如果不是最后一步，存储当前步结果
                            DataSet ds = service.Invoke<DataSet>("Fill", dbCommand);
                            if (ds.Tables.Count > 0 && ds.Tables[ds.Tables.Count - 1].Rows.Count > 0)
                            {
                                DataTable dt = ds.Tables[ds.Tables.Count - 1];
                                dataPre = dt.Rows[0];
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        exceMsg = response.Url + ">>" + step.StepName + ">>" + ex.Message;
                        this.logger.LogError(exceMsg);
                        throw new Exception(exceMsg);
                    }

                    rowIndex++;
                }

                tran.Commit();

                await this.WriteAsync(result);
            }
            catch (Exception ex)
            {
                tran.Rollback();
                await this.InvokeException(ex.Message);
            }

        }

        //执行最后的sql
        private string ExecSqlJms(InterfaceModel response, IMicroService service, JmsCommand dbCommand, List<FilterCondition> filters)
        {
            XmlHelper xmlHelp = new XmlHelper();
            string json = "";
            object dataResult = new object();
            object result = new object();
            switch (response.ExecuteType)
            {
                case "Scalar":
                    DataResult dataResult1 = new DataResult();
                    dataResult1.DataType = 200;
                    dataResult1.Result = service.Invoke<object>("ExecuteScalar", dbCommand);
                    dataResult = dataResult1;
                    result = dataResult1.Result;
                    break;
                case "Int":
                    IntDataResult intDataResult = new IntDataResult();
                    intDataResult.Result = service.Invoke<int>("ExecuteNonQuery", dbCommand);  
                    intDataResult.DataType = 200;
                    dataResult = intDataResult;
                    result = intDataResult.Result;
                    break;
                case "DataSet":
                    DataSet ds = new DataSet();
                    DSDataResult dSDataResult = new DSDataResult();
                    if (filters != null)
                    {
                        //复杂查询需要再过滤一下最后一个表
                        int lastIndex = ds.Tables.Count - 1;
                        string msg;
                        string filter = this.CreateFileterString(filters, out msg);
                        if (msg != "")
                        {
                            throw new Exception(msg);
                        }

                        ds = service.Invoke<DataSet>("Fill", dbCommand); 
                        if (ds.Tables.Count > 0 && ds.Tables[lastIndex].Rows.Count > 0)
                        {
                            DataTable table = ds.Tables[ds.Tables.Count - 1];
                            table.DefaultView.RowFilter = filter;
                            DataTable dtNew = table.DefaultView.ToTable();
                            ds.Tables.RemoveAt(lastIndex);
                            ds.Tables.Add(dtNew);
                            dSDataResult.Result = ds;
                        }
                        else
                        {
                            dSDataResult.Result = ds;
                        }
                    }
                    else
                    {
                        ds = service.Invoke<DataSet>("Fill", dbCommand);
                        dSDataResult.Result = ds;
                    }
                    dataResult = dSDataResult;
                    dSDataResult.DataType = 200;
                    break;
            }

            if (response.SerializeType == "Xml")
            {
                switch (response.ExecuteType)
                {
                    case "Scalar":
                        json = xmlHelp.SerializeXML<DataResult>(dataResult);
                        break;
                    case "Int":
                        json = xmlHelp.SerializeXML<IntDataResult>(dataResult);
                        break;
                    case "DataSet":
                        json = xmlHelp.SerializeXML<DSDataResult>(dataResult);
                        break;
                }
            }
            else if (response.SerializeType == "Json")
            {
                json = JsonConvert.SerializeObject(dataResult); 
            }
            else
            {
                json = result.ToString();
            }

            return json;
        }

        private string ExecSqlJmsLayui(InterfaceModel response, IMicroService service, JmsCommand dbCommand, List<FilterCondition> filters)
        {
            XmlHelper xmlHelp = new XmlHelper();
            string json = "";
            LayuiResult dataResult = new LayuiResult();
            dataResult.code = 0;
            dataResult.count = 0;
            dataResult.msg = "";
            dataResult.data = "{0}";

            object result = new object();
            switch (response.ExecuteType)
            {
                case "Scalar":
                    result = service.Invoke<object>("ExecuteScalar", dbCommand);
                    break;
                case "Int":
                    result = service.Invoke<int>("ExecuteNonQuery", dbCommand);
                    break;
                case "DataSet":
                    DataSet ds = service.Invoke<DataSet>("Fill", dbCommand);
                    DataTable table = new DataTable();
                    if (filters != null)
                    {
                        //复杂查询需要再过滤一下最后一个表 
                        string msg;
                        string filter = this.CreateFileterString(filters, out msg);
                        if (msg != "")
                        {
                            throw new Exception(msg);
                        }
                        DataTable table0 = ds.Tables[0];
                        table0.DefaultView.RowFilter = filter;
                        DataTable dtNew = table0.DefaultView.ToTable();
                        table = dtNew;
                    }
                    else
                    {
                        table = ds.Tables[0];
                    }

                    dataResult.count = table.Rows.Count;
                    result = table;
                    break;
            }

            dataResult.data = result;
            if (response.SerializeType == "Xml")
            {

            }
            else if (response.SerializeType == "Json")
            {
                json = JsonConvert.SerializeObject(dataResult);
            }
            else
            {
                json = result.ToString();
            }

            return json;
        }

        #endregion


        private async Task InvokeException(string exception)
        {

            DataResult returnValue = new DataResult();
            returnValue.DataType = -1;
            returnValue.Exception = exception;
            this.logger.LogError(exception);
            string json = JsonConvert.SerializeObject(returnValue);
            await this.WriteAsync(json);
        }

        private async Task WriteAsync(string text)
        {
            // await this.context.Response.WriteAsync(text, Encoding.UTF8);  , Encoding.GetEncoding("GB2312")
            this.context.Response.ContentType = "text/plain;charset=utf-8";
            await this.context.Response.WriteAsync(text);
        }

        private string CreateFileterString(List<FilterCondition> filters, out string msg)
        {
            msg = "";
            StringBuilder tempBracket = new StringBuilder("");//用来校验括号的临时字符串
            StringBuilder tempFilterStr = new StringBuilder("");//过滤字符串临时内容
            int i = 0;
            foreach (FilterCondition filter in filters)
            {
                //记录用来校验括号的临时字符串
                if (filter.BracketL == "(")
                {
                    tempBracket.Append("(");
                    tempFilterStr.Append(" ( ");
                }

                if (filter.BracketR == ")")
                {
                    tempBracket.Append(")");
                }

                //列名
                tempFilterStr.Append(" ").Append(filter.ColumnName);

                //条件
                if (filter.ValueType == "List")
                {
                    if (filter.Condition == ConditionTypeEnum.NotContain || filter.Condition == ConditionTypeEnum.NotEqual)
                    {
                        tempFilterStr.Append(" NOT ");
                    }
                    tempFilterStr.Append(" IN (");

                    string[] _list = filter.Value.Split(",");
                    bool bFist = true;
                    foreach (string _item in _list)
                    {
                        if (!bFist)
                        {
                            tempFilterStr.Append(",");
                            bFist = false;
                        }
                        tempFilterStr.Append("'").Append(_item.Replace("%", "[%]")).Append("'");
                    }
                    tempFilterStr.Append(") ");
                }
                else //其他的按照不同的值类型做不同的比对
                {
                    string sql = CreateFileterString(filter, out msg);
                    if (msg != "")
                    {
                        break;
                    }
                    tempFilterStr.Append(sql);
                }
                if (filter.BracketR == ")")
                {
                    tempFilterStr.Append(" ) ");
                }

                if (!string.IsNullOrEmpty(filter.JoinType) && i != filters.Count - 1)
                {
                    tempFilterStr.Append(" ").Append(filter.JoinType).Append(" ");
                }

                #region 查看括号有没有不全的
                string tempBracketStr = tempBracket.ToString();
                while (tempBracketStr.Contains("(") || tempBracketStr.Contains(")"))
                {
                    if (tempBracketStr.Contains("()"))
                    {
                        tempBracketStr = tempBracketStr.Replace("()", "");
                    }
                    else
                    {
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(tempBracketStr))
                {
                    msg = "左右括号不匹配，请检验！";
                    break;
                }
                #endregion
                i++;
            }
            return tempFilterStr.ToString();
        }

        private string CreateFileterString(FilterCondition _filterCol, out string msg)
        {
            msg = "";

            StringBuilder tempFilterStr = new StringBuilder("");
            if (_filterCol.ValueType == nameof(String))
            {
                if (_filterCol.Condition == ConditionTypeEnum.Equal)
                {
                    tempFilterStr.Append(" = '").Append(_filterCol.Value).Append("' ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.Contain)
                {
                    tempFilterStr.Append(" like '%").Append(_filterCol.Value).Append("%' ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.NotEqual)
                {
                    tempFilterStr.Append(" <> '").Append(_filterCol.Value).Append("' ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.MatchLeft)
                {
                    tempFilterStr.Append(" like '").Append(_filterCol.Value).Append("%' ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.MatchRight)
                {
                    tempFilterStr.Append(" like '%").Append(_filterCol.Value).Append("' ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.NotContain)
                {
                    tempFilterStr.Append(" not like '%").Append(_filterCol.Value).Append("%' ");
                }
                else
                {
                    tempFilterStr.Append(" like '%' ");
                }
            }
            else if (_filterCol.ValueType == nameof(Decimal) || _filterCol.ValueType == nameof(Int32))
            {
                decimal _num = 0;
                if (!decimal.TryParse(_filterCol.Value, out _num))
                {
                    msg = "数据类型转换错误";
                    return "";
                }

                if (_filterCol.Condition == ConditionTypeEnum.Equal)
                {
                    tempFilterStr.Append(" = ").Append(_filterCol.Value).Append(" ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.NotEqual)
                {
                    tempFilterStr.Append(" <> ").Append(_filterCol.Value).Append(" ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.Greater)
                {
                    tempFilterStr.Append(" > ").Append(_filterCol.Value).Append(" ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.Less)
                {
                    tempFilterStr.Append(" < ").Append(_filterCol.Value).Append(" ");
                }
                else
                {
                    tempFilterStr.Append(" = 0 ");
                }
            }
            else if (_filterCol.ValueType == nameof(DateTime))
            {
                if (_filterCol.Condition == ConditionTypeEnum.Equal)
                {
                    tempFilterStr.Append(" = #").Append(_filterCol.Value).Append("# ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.Greater)
                {
                    tempFilterStr.Append(" > #").Append(_filterCol.Value).Append("# ");
                }
                else if (_filterCol.Condition == ConditionTypeEnum.Less)
                {
                    tempFilterStr.Append(" < #").Append(_filterCol.Value).Append("# ");
                }
                else
                {
                    tempFilterStr.Append(" = #").Append(DateTime.Now.Date.ToString()).Append("# ");
                }
            }
            else if (_filterCol.ValueType == nameof(Boolean))
            {
                if (_filterCol.Condition == ConditionTypeEnum.Equal)
                {
                    tempFilterStr.Append(" = '").Append(_filterCol.Value).Append("' ");
                }
            }

            return tempFilterStr.ToString();
        }

    }
}
