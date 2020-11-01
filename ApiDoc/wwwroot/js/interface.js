﻿var _SN = 0;
 

//保存接口
function btnSaveIntterface_Click() {

    var objSN = $("#txtSN");
    var vSN = objSN.val();
    var vTitle = $("#txtTitle").val();
    var vUrl = $("#txtUrl").val();
    var vSerializeType = $("#cbxSerializeType").val();
    var vMethod = $("#cbxMethod").val();
    var vFKSN = $("#txtFKSN").val();
    var vExecuteType = $("#cbxExecuteType").val();
    var vIsTransaction = document.getElementById("txtStepIsTransaction").checked;
    var vIsStop = $("#txtIsStop").val();
    var vDataType = $("#cbxDataType").val();

    $.post(urlInterfaceSave, {
        SN: vSN,
        Title: vTitle,
        Url: vUrl,
        Method: vMethod,
        FKSN: vFKSN,
        SerializeType: vSerializeType,
        IsTransaction: vIsTransaction,
        ExecuteType: vExecuteType,
        IsStop: vIsStop,
        DataType: vDataType
    }, function (data) {

            popToastSuccess("保存成功!");

            _SN = data.sn;
            $("#txtSN").val(data.sn);
    })
}

//停用接口
function btnStopIntterface_Click() {

    var isStop = $("#iPlay").hasClass("text-success");

    var url = urlInterfaceStop + "?SN=" + _SN + "&bStop=" + isStop;
    var data = { SN: _SN, bStop: isStop };
    $.get(url, function (result) {

        if (result > 0) {
            if (isStop) {
                $("#iPlay").removeClass("text-success");
                $("#txtIsStop").val(1);
            }
            else {
                $("#iPlay").addClass("text-success");
                $("#txtIsStop").val(0);
            }
        }
        else {

            popToastWarning("停用失败!"); 
        }
    });
}

//上传接口到路由
function btnUpLoad_Click() {

    var SN = $("#txtSN").val();
    var url = urlRouteUpLoad + "?SN=" + SN;
    $.get(url, function (myResponse) {

        if (myResponse.dataType == 0) {

            popToastSuccess("发布接口成功!");
             
        }
        else {
            alert( myResponse.exception );
        }
    });
}

//删除接口路由信息
function btnDownLoad_Click() {
 
    var vUrl = $("#txtUrl").val();
    var url = urlRouteDelete + "?Url=" + vUrl;
    $.get(url, function (myResponse) {

        if (myResponse.dataType == 0) {

            //成功
            alert("删除成功");
        }
        else {
            alert(myResponse.exception);
        }
    });

}

//弹出测试窗口
function btnShow_CS_Click() {

    var url = window.location.protocol + "//" + window.location.host + urlRoot + $("#txtUrl").val(); 
    $("#lblUrl").html(url); 

    var method = $("#cbxMethod").val();

    var paraList = document.getElementsByName("chkParam_");
    var csJson = ""; 
    for (var i = 0; i < paraList.length; i++) { 
        json = paraList[i].value;
        json = eval("(" + json + ")"); 
         
        if (method == "Get") {
            if (csJson != "") {
                csJson += "&";
            }
            if (json.DataType == "varchar") {
                csJson += json.ParamName + "='" + json.DefaultValue + "'";
            }
            else if (json.DataType == "int") {
                csJson += json.ParamName + "=" + json.DefaultValue;
            }
            else {
                csJson += json.ParamName + "='" + json.DefaultValue + "'";
            }
        }
        else if (method == "Post") {
            if (csJson != "") {
                csJson += ",";
            }
            csJson += json.ParamName + ":'" + json.DefaultValue + "'"
        }
    }

    if (method == "Post")
    {
        csJson = "{" + csJson + "}";
    }

    $("#txtInput").val(csJson); 
    $("#txtResult").val("");
    var model = $("#myModalCS");
    model.modal('show');
        
}

//--------------------------------------------------测试
function btnSendCS() {

    var url = window.location.protocol + "//" + window.location.host + urlRoot + $("#txtUrl").val();
    var vdata = $("#txtInput").val();
    var txtResult = $("#txtResult");
    var method = $("#cbxMethod").val();
    if (method == "Get") { 
        if (txtInput != "") {
            url = url + "?" + txtInput;
             
        }
    }

    $.get(urlAuthor, function (author) {

        $.ajax({
            headers: { 'Authorize': author },
            url: url,
            type: method,
            datType: "JSON",
            contentType: "application/json",
            data: vdata,
            async: false,
            success: function (result) {
                txtResult.val(result);
            }
        })

    });
    
}

function checkAll(sender, checkName) {

    var objList = document.getElementsByName(checkName)
    for (var i = 0; i < objList.length; i++) {
        objList[i].checked = sender.checked;
    }
}
 
$(function () {

    //初始化提示信息
    var option = { animation: true, delay: 2000 };
    $('.toast').toast(option); 
    $('[data-toggle="tooltip"]').tooltip();

    //$("#myParamList").height(50);

    _SN = $("#txtSN").val();
});
