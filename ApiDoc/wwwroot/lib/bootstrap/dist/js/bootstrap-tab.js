$(function () {

    var tab = $.fn.tab;
    var tab = $.fn['tab'].Constructor;

    tab.addTab = function (options) {

        var id = "tab_item_" + options.id;
        var container_id = "tab_container_" + options.id;

        //�ж��Ƿ��Ѵ���ָ��ID��tab
        if ($("#" + id).length > 0) {
            throw "��ǰID��Tab�Ѵ��ڣ�";
        }
 
        var li = $('<li class="nav - item">');
        var a = $('<a class="nav - link active" id="' + id + '" data-toggle="tab" href="#' + container + '"  role="tab" aria-controls="home" aria-selected="true">' + options.text);

        //�ϲ�li��aԪ��
        li.append(a);

       // var ul = $(this);
        var ul = tab;
        //�ϲ�ul��liԪ��
        ul.append(li);

        //��������ʾ��ǰli
        $(li).tab("show");

        //����div����Ԫ��
        //var div = $("<div />", {
        //    "id": options.id,
        //    "class": "tab-pane fade in active",
        //});

        var tabpanel = '<div class="tab-pane fade show active " id="' + container + '" role="tabpanel" aria-labelledby="nav-home-tab">' +
            '<iframe src="' + options.url + '" frameborder="1" scrolling="yes" style="width:100%;height:200px;background-color:white"></iframe>' +
            '</div>';

        var div = $(tabpanel);

        //���ݴ��ı���htmlƬ��
        typeof options.content == "string" ? div.append(options.content) : div.html(options.content);

        var container = $(".tab-content");
        container.append(div);

        //�����ɺ���ʾdiv
        $(div).siblings().removeClass("active");
    }

})
