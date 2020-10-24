﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiDoc.Models;
using System.Data;
using Autofac;
using System.Data.SqlClient;
using ApiDoc.DAL;

namespace ApiDoc.Controllers
{
    public class HomeController : BaseController
    { 
        private readonly IComponentContext componentContext;
　
        public HomeController(IComponentContext componentContext )
        { 
            this.componentContext = componentContext;
            IDbConnection dbConnection = componentContext.Resolve<IDbConnection>();

            //var c = componentContext.Resolve<ClassC>();
            //c.Show();
            //c.D.LogWarning("");
 
        }

        public IActionResult Index()
        {
         　
            ViewData["Nav"] = base.LoadNav("Home");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
         
        public int CS()
        {
            return 200;
        }
    }
}
