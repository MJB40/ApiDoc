﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ApiDoc.Models
{
    /// <summary>
    /// 路由数据
    /// </summary>
    public class DBInterfaceModel : InterfaceModel
    {   
        public List<FlowStepModel> Steps;
        public string Auth;// 验证接口参数是否正确

    }
}
