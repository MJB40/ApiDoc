﻿var treeview;
var selectedNode; 
var vDo;

var urlAll = "~/Folder/All?root=true";
var urlDelFolder = "~/Folder/DeleteFolder";

//----------Add
function btnAdd() {

    vDo = "add";
    
    $("#txtFolderName").val(""); 
    $("#myModalAdd").modal('show');
}

function CallBackHandler() {

    var selectedNodes = treeview.treeview('getSelected');
    var nodeID = -1;
    var parentSN = 0;
    
    var sn = 0;

    if (selectedNodes.length > 0) {
        var selectedNode = selectedNodes[0];

        nodeID = selectedNode.nodeId;
        parentSN = selectedNode.data.parentSN;
        sn = selectedNode.data.sn;
    } 

    var txtFolderName = $("#txtFolderName").val(); 
    var url = "/Folder/SaveFolder?FolderName=" + txtFolderName + "&&";

    if (vDo == "add") {
        url += "SN=0" + "&&ParentSN=" + sn ;
    }
    else if (vDo == "update") {

        url += "SN=" + sn + "&&ParentSN=" + parentSN;
    }

    $.get(url, function (result) {
 
        $("#myModalAdd").modal('hide');

        var tvNode = new Array();
        tvNode[0] = nodeID;
        if (vDo == "add") {
            tvNode[1] = { node: { text: result.folderName, href: "", data: result } };
            treeview.treeview("addNode", tvNode);
        }
        else if (vDo == "update") {

            tvNode[1] = { text: result.folderName, href: "", data: result };
            treeview.treeview("editNode", tvNode);
        }
 
    });
     
}

//----------Update
function btnUpdate() {

    vDo = "update";

    var selectedNode = treeview.treeview('getSelected');

    $("#txtFolderName").val(selectedNode[0].text);
     
    $("#myModalAdd").modal('show'); 
     
}

//----------Delete
function btnDelete() {
    $("#myModalDelete").modal('show');
}

function DeleteHandler() {

    var selectedNode = GetSelected();
    var nodeId = selectedNode.nodeId;
    var SN = selectedNode.data.sn;

    var url = urlDelFolder + "?SN=" + SN;

    $.get(url, function (result) {

        if (result > 0) {

            $("#myModalDelete").modal('hide');
            treeview.treeview("deleteNode", [nodeId, { silent: true }]);
        }
    });


}

function GetSelected() {

    var selectedNodes = treeview.treeview('getSelected');
    if (selectedNodes.length > 0)
        return selectedNodes[0];
    return selectedNodes;
}


$(function () {
    
    var defaultData = [
        {
            text: 'Parent 1',
            href: '#parent1',
            tags: ['4'],
            nodes: [
                {
                    text: 'Child 1',
                    href: '#child1',
                    tags: ['2'],
                    nodes: [
                        {
                            text: 'Grandchild 1',
                            href: '#grandchild1',
                            tags: ['0']
                        },
                        {
                            text: 'Grandchild 2',
                            href: '#grandchild2',
                            tags: ['0']
                        }
                    ]
                },
                {
                    text: 'Child 2',
                    href: '#child2',
                    tags: ['0']
                }
            ]
        },
        {
            text: 'Parent 2',
            href: '#parent2',
            tags: ['0']
        },
        {
            text: 'Parent 3',
            href: '#parent3',
            tags: ['0']
        },
        {
            text: 'Parent 4',
            href: '#parent4',
            tags: ['0']
        },
        {
            text: 'Parent 5',
            href: '#parent5',
            tags: ['0']
        }
    ];
     
    $.get(urlAll, function (result) {
         
        treeview = $('#treeview7').treeview({
            color: "#428bca",
            showBorder: false,
            data: result 
        });

        treeview.treeview("collapseAll");
    });

    txtFolderName = $("#txtFolderName");

    treeview.on('nodeSelected', function (event, data) {

        selectedNode = data; 

    }) 
})
 