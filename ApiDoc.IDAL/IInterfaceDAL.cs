﻿
using ApiDoc.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiDoc.IDAL
{
    public interface IInterfaceDAL : IBaseDAL
    {
        List<InterfaceModel> All();
        List<InterfaceModel> All(string title, string url, int fksn);

        string FullPath(int fksn);

    }
}
