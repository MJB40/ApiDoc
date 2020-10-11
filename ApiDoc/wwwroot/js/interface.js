﻿
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

    $.post(urlInterfaceSave, {
        SN: vSN,
        Title: vTitle,
        Url: vUrl,
        Method: vMethod,
        FKSN: vFKSN,
        SerializeType: vSerializeType,
        IsTransaction: vIsTransaction,
        ExecuteType: vExecuteType
    }, function (data) {

            $('#mySuccess').toast('show');
            $("#txtSN").val(data.sn);
    })
}

//上传接口到路由
function btnUpLoad_Click() {

    var SN = $("#txtSN").val();
    var url = urlRouteUpLoad + "?SN=" + SN;
    $.get(url, function (myResponse) {

        if (myResponse.dataType == 0) {
            //成功
            alert("上传成功");
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

    $("#txtResult").val("");
    var model = $("#myModalCS");
    model.modal('show');
        
}

//--------------------------------------------------测试
function btnSendCS() {
 
    var url = window.location.protocol + "//" + window.location.host + urlRoot + $("#txtUrl").val();
    var txtInput = $("#txtInput").val();
    var txtResult = $("#txtResult");
    var method = $("#cbxMethod").val();
  
    if (method == "Post") {
       
        //var data = $.parseJSON(txtInput);
        var vdata = txtInput;
        //$.post(url, data, function (result) {
        //    txtResult.val(result);
        //});
        $.ajax({
            url: url,
            type: "POST",
            datType: "JSON",
            contentType: "application/json",
            data: vdata,
            async: false,
            success: function (result) {
                txtResult.val(result);
            }
        })
    }
    else if (method == "Get") {

        if (txtInput != "") {
            url = url + "?" + txtInput; 
        } 
        $.get(url, function (result) {
            txtResult.val(result);
        });

    }
}

$(function () {
    var option = { animation: true, delay: 1500 };
    $('.toast').toast(option); 
    $('[data-toggle="tooltip"]').tooltip()
});


