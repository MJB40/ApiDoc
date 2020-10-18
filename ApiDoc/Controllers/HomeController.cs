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

namespace ApiDoc.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IComponentContext componentContext;

        // private readonly IContainer container;

        public HomeController(ILogger<HomeController> logger, IComponentContext  componentContext )
        {
            _logger = logger;
             
            this.componentContext = componentContext;
            IDbConnection dbConnection = componentContext.Resolve<IDbConnection>();
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
