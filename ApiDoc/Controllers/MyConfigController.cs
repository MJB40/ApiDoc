﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiDoc.Models.Components;
using ApiDoc.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiDoc.Controllers
{
    public class MyConfigController : BaseController
    {
        private readonly MyConfig myConfig; 
        private JsonFileHelper jsonFileHelper;

        public MyConfigController(IHostEnvironment hostingEnvironment, 
            MyConfig myConfig  )
        {
            string path = hostingEnvironment.ContentRootPath + "\\MyConfig"; 
            jsonFileHelper = new JsonFileHelper(path);
            this.myConfig = myConfig; 
        }
        public IActionResult Index()
        {
            ViewData["Nav"] = base.LoadNav("MyConfig"); 
            MyConfig myConfig1 = jsonFileHelper.Read<MyConfig>(); 
            return View(myConfig1);
        }

        public int SaveAuthorize(string SecurityKey)
        {
            JWTTokenOptions jWTTokenOptions = new JWTTokenOptions();
            jWTTokenOptions.Audience = myConfig.JWTTokenOptions.Audience;
            jWTTokenOptions.Issuer = myConfig.JWTTokenOptions.Issuer;
            jWTTokenOptions.SecurityKey = SecurityKey;
            jsonFileHelper.Write<JWTTokenOptions>("JWTTokenOptions", jWTTokenOptions);
            myConfig.JWTTokenOptions = jWTTokenOptions;
            return 100;
        }

        [HttpPost]
        public int SaveDataType(DataBases DataType)
        { 
            jsonFileHelper.Write<DataBases>("DataType", DataType);
            myConfig.DataType = DataType;
            return 100;
        }
    }
}
