(function (hangfire) {

    hangfire.HttpJob = (function () {
        function HttpJob() {
            this._initialize();
        }

        HttpJob.prototype._initialize = function () {

            var config = window.Hangfire.httpjobConfig;
            if (!config) return;

            if (config.DashboardName) {
                //更改控制面板标题
                $(".navbar-brand").html(config.DashboardName);
            }

            if (config.DashboardFooter) {
                //更改hangfire版本显示替换为任意值
                $("#footer ul li:first-child").html('<a href="https://github.com/yuzd/Hangfire.Job" target="_blank">' + config.DashboardFooter + '</a>');

            }


            if (config.DashboardTitle) {
                //更改标题
                document.title = config.DashboardTitle;
            }

            if (config.IsReadonly && "True" == config.IsReadonly && config.LogOutButtonName) {
                $('.glyphicon-log-out').parent().html('<span class="glyphicon glyphicon-log-out"></span>' + config.LogOutButtonName);
            }

            //判断是否需要注入
            if (!config.NeedAddNomalHttpJobButton &&
                !config.NeedAddRecurringHttpJobButton &&
                !config.NeedAddCronButton &&
                !config.NeedEditRecurringJobButton) {

                return;
            }


            var button = '';
            var AddCronButton = '';
            var PauseButton = '';
            var EditRecurringJobutton = '';
            var editgeturl = config.GetRecurringJobUrl;
            var pauseurl = config.PauseJobUrl;
            var startBackgroudJobUrl = config.StartBackgroudJobUrl;
            var getlisturl = config.GetJobListUrl;
            var divModel = '';
            var options = {
                schema: {},
                mode: 'code'
            };

            var normal_templete = "{\"JobName\":\"\",\"Method\":\"GET\",\"ContentType\":\"application/json\",\"Url\":\"http://\",\"DelayFromMinutes\":1,\"Data\":{},\"Timeout\":" + config.GlobalHttpTimeOut + ",\"BasicUserName\":\"\",\"BasicPassword\":\"\",\"EnableRetry\":false,\"SendSucMail\":false,\"SendFaiMail\":true,\"Mail\":\"\",\"AgentClass\":\"\"}";
            var recurring_templete = "{\"JobName\":\"\",\"Method\":\"GET\",\"ContentType\":\"application/json\",\"Url\":\"http://\",\"Data\":{},\"Timeout\":" + config.GlobalHttpTimeOut + ",\"Cron\":\"\",\"BasicUserName\":\"\",\"BasicPassword\":\"\",\"QueueName\":\"" + config.DefaultRecurringQueueName + "\",\"EnableRetry\":false,\"SendSucMail\":false,\"SendFaiMail\":true,\"Mail\":\"\",\"AgentClass\":\"\"}";
            //如果需要注入新增计划任务
            if (config.NeedAddNomalHttpJobButton) {
                button =
                    '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float: inherit;margin-left: 10px" id="addHttpJob">' +
                    '<span class="glyphicon glyphicon-plus"> ' + config.AddHttpJobButtonName + '</span>' +
                    '</button>';
                divModel =
                    '<div class="modal inmodal" id="httpJobModal" tabindex="-1" role="dialog" aria-hidden="true">' +
                    '<div class="modal-dialog">' +
                    '<div class="modal-content">' +
                    '<div class="modal-header">' +
                    '<h4 class="modal-title">' + config.AddHttpJobButtonName + '</h4>' +
                    '</div>' +
                    '<div class="modal-body">' +
                    '<div class="editor_holder" style="height: 250px;"></div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-url="' + config.AddHttpJobUrl + '">' + config.SubmitButtonName + '</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>';

            }


            //如果需要注入新增周期性任务
            if (config.NeedAddRecurringHttpJobButton) {
                button =
                    '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float: inherit;margin-left:10px" id="addRecurringHttpJob">' +
                    '<span class="glyphicon glyphicon-plus"> ' + config.AddRecurringJobHttpJobButtonName + '</span>' +
                    '</button>';
                divModel =
                    '<div class="modal inmodal" id="httpJobModal" tabindex="-1" role="dialog" aria-hidden="true">' +
                    '<div class="modal-dialog">' +
                    '<div class="modal-content">' +
                    '<div class="modal-header">' +
                    '<h4 class="modal-title">' + config.AddRecurringJobHttpJobButtonName + '</h4>' +
                    '</div>' +
                    '<div class="modal-body">' +
                    '<div class="editor_holder" style="height: 250px;"></div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-url="' + config.AddRecurringJobUrl + '">' + config.SubmitButtonName + '</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>';
            }


            //如果需要注入编辑任务

            if (config.NeedEditRecurringJobButton) {
                EditRecurringJobutton =
                    '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" id="EditJob">' +
                    '<span class="glyphicon glyphicon-pencil"> ' + config.EditRecurringJobButtonName + '</span>' +
                    '</button>';
                divModel =
                    '<div class="modal inmodal" id="httpJobModal" tabindex="-1" role="dialog" aria-hidden="true">' +
                    '<div class="modal-dialog">' +
                    '<div class="modal-content">' +
                    '<div class="modal-header">' +
                    '<h4 class="modal-title">' + config.EditRecurringJobButtonName + '</h4>' +
                    '</div>' +
                    '<div class="modal-body">' +
                    '<div class="editor_holder" style="height: 250px;"></div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-url="' + config.EditRecurringJobUrl + '">' + config.SubmitButtonName + '</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>';
            }


            if (config.NeedAddCronButton) {
                AddCronButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" id="AddCron">' +
                    '<span class="glyphicon glyphicon-time"> ' + config.AddCronButtonName + '</span>' +
                    '</button>';
            }

            //暂停和启用任务

            PauseButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" data-loading-text="执行中..." disabled id="PauseJob">' +
                '<span class="glyphicon glyphicon-stop"> ' + (config.NeedAddNomalHttpJobButton ? config.StartBackgroudJobButtonName : config.PauseJobButtonName) + '</span>' +
                '</button>';

            if (!button || !divModel) return;
            //新增按钮
            $('.page-header').append(button);
            $('.page-header').append(EditRecurringJobutton);
            $('.page-header').append(AddCronButton);
            $('.btn-toolbar-top').append(PauseButton);
            $(document.body).append(divModel);

            var container = $('.editor_holder')[0];

            try {
                window.jsonEditor = new JSONEditor(container, options);
            } catch (e) {
                Console.log(e);
            }

            $('#addHttpJob').click(function () {
                window.jsonEditor.setText(normal_templete);
                window.jsonEditor.format();
                $('#httpJobModal').modal({ backdrop: 'static', keyboard: false });
                $('#httpJobModal').modal('show');
            });

            $('#addRecurringHttpJob').click(function () {
                $(".modal-title").html(config.AddRecurringJobHttpJobButtonName);
                window.jsonEditor.setText(recurring_templete);
                window.jsonEditor.format();
                $('#httpJobModal').modal({ backdrop: 'static', keyboard: false });
                $('#httpJobModal').modal('show');
            });

            //暂停任务
            $("#PauseJob").click(function (e) {
                if (!$(".js-jobs-list-checkbox").is(':checked')) {
                    swal({
                        title: "",
                        text: "Select Job Item First!",
                        type: "error"
                    });
                    return;
                } else {
                    var jobId = $(".js-jobs-list-checkbox:checked").val();
                    if (config.NeedAddNomalHttpJobButton) {
                        swal({
                            title: "Start Job",
                            text: "Are you sure want to start?",
                            type: "input",
                            showCancelButton: true,
                            closeOnConfirm: false,
                            animation: "slide-from-top",
                            inputPlaceholder: "start param",
                            showLoaderOnConfirm: true,
                            confirmButtonColor: "#DD6B55",
                            confirmButtonText: "Submit",
                            cancelButtonText: "Cancel"
                        }, function (inputValue) {
                            if (inputValue === false) return false;
                            $.ajax({
                                type: "post",
                                url: startBackgroudJobUrl,
                                contentType: "application/json; charset=utf-8",
                                data: JSON.stringify({ "JobName": jobId , "URL": "baseurl", "Data": inputValue, "ContentType": "application/json" }),
                                async: true,
                                success: function (returndata) {

                                    var jobs = $("input[name='jobs[]']:checked", container).map(function () {
                                        return $(this).val();
                                    }).get();

                                    $.post($($('.btn-toolbar button')[0]).data('url'), { 'jobs[]': [jobId] }, function () {
                                        swal({
                                            title: "Success",
                                            text: "start job success",
                                            type: "success"
                                        });
                                        window.location.reload();
                                    });

                                }
                            });
                        });
                        e.stopPropagation();
                        e.preventDefault();
                    } else {
                        $.ajax({
                            type: "post",
                            url: pauseurl,
                            contentType: "application/json; charset=utf-8",
                            data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json" }),
                            async: true,
                            success: function (returndata) {
                            }
                        });
                    }

                }
            });
            GetJobList();
            function GetJobList() {
                $.ajax({
                    type: "post",
                    url: getlisturl,
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ "JobName": $(".js-jobs-list-checkbox:checked").val(), "URL": "baseurl", "ContentType": "application/json" }),
                    async: true,
                    success: function (returndata) {
                        if (config.NeedAddNomalHttpJobButton) {
                            $(".table tbody").find('tr').each(function () {
                                var tdArr = $(this).children();
                                var ss = tdArr.eq(3).text();
                                if (ss.indexOf('| multiple |') >= 0) {
                                    tdArr.eq(2).text('multiple');
                                    $(this).css("color", "blue");
                                }
                            });
                        }

                        if (config.NeedEditRecurringJobButton) {
                            if (!returndata) return;
                            if (returndata.length < 1) return;

                            $(".table tbody").find('tr').each(function () {
                                var tdArr = $(this).children();
                                var ss = tdArr.eq(1).text();
                                for (var i = 0; i < returndata.length; i++) {
                                    if (ss === returndata[i].Id) {
                                        $(this).css("color", "red");
                                        $(this).addClass("Paused");
                                    }
                                }
                            });
                        }

                    }
                });
            }
            //编辑任务
            $("#EditJob").click(function () {
                if (!$(".js-jobs-list-checkbox").is(':checked')) {
                    swal({
                        title: "",
                        text: "Select Job Item First!",
                        type: "error"
                    });
                    return;
                } else {
                    if ($("input[type=checkbox]:checked").val() === "on" && $("table tbody tr").size() > 1) {
                        swal({
                            title: "",
                            text: "Select One Job Only!",
                            type: "error"
                        });
                        return;
                    }
                    $(".modal-title").html(config.EditRecurringJobButtonName);
                    $.ajax({
                        type: "post",
                        url: editgeturl,
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({ "JobName": $(".js-jobs-list-checkbox:checked").val(), "URL": "baseurl", "ContentType": "application/json" }),
                        async: true,
                        success: function (returndata) {
                            window.jsonEditor.setText(JSON.stringify(returndata));
                            window.jsonEditor.format();
                            $('#httpJobModal').modal('show');
                        }

                    });
                }
            });
            //打开cron表达式页面
            $("#AddCron").click(function () {
                window.location.href = config.AddCronUrl;
            });
            $('#addhttpJob_close-model').click(function () {
                $('#httpJobModal').modal('hide');
                window.jsonEditor.setText('{}');
            });


            $('#addhttpJob_save-model').click(function () {
                var url = $(this).data("url");
                if (!url) return;
                var obj = window.jsonEditor.get();
                if (obj && obj.Data) {
                    if (typeof obj.Data === 'string' || obj.Data instanceof String) {
                    } else {
                        obj.Data = JSON.stringify(obj.Data);
                    }

                    if (obj.Data == '{}') obj.Data = '';
                }
                var settings = {
                    "async": true,
                    "url": url,
                    "method": "POST",
                    "contentType": "application/json; charset=utf-8",
                    "dataType": "json",
                    "data": JSON.stringify(obj)
                }
                $.ajax(settings).done(function (response) {

                    swal({
                        title: "Success",
                        type: "success",
                        showCancelButton: false,
                        closeOnConfirm: false,
                        animation: "slide-from-top",
                        confirmButtonColor: "#DD6B55",
                        confirmButtonText: "OK",
                    }, function () {
                        $('#httpJobModal').modal('hide');
                        window.jsonEditor.setText('{}');
                        location.reload();
                    });

                }).fail(function () {
                    swal({
                        title: "",
                        text: "Add job fail！",
                        type: "error"
                    });
                });
            });
            $('.jsoneditor-menu').hide();
        };

        return HttpJob;

    })();

})(window.Hangfire = window.Hangfire || {});

//找出已经暂停的job
var pausedjob = [];
function GetPausedJobs() {
    $(".Paused").each(function () {
        pausedjob.push($(this).children().eq(1).text());
    });
}
//搜索框拓展,在查找的记录中查询，无需查库
var jobSearcher = new function () {

    var createSearchBox = function () {
        $('#search-box').closest('div').remove();
        $('.js-jobs-list').prepend('<div class="search-box-div">' +
            '<input type="text" id="search-box" placeholder="' + (window.Hangfire.httpjobConfig.SearchPlaceholder) + '">' +
            //'<img class="loader-img" src ="" />' +
            '<span class="glyphicon glyphicon-search" id="loaddata"> Checking...</span>' +
            '<p id="total-items"></p>' +
            '</div>');
    };
    this.Init = function () {
        createSearchBox();
    };
    this.BindEvents = function () {
        $('#search-box').bind('change', function (e) {
            if (this.value.length === 0)
                window.location.reload();
            else
                GetPausedJobs();
            FilterJobs(this.value);
        });
    };
    function FilterJobs(keyword) {
        $('#loaddata').css('visibility', 'unset');
        //在所有查询结果中查找满足条件的，模糊匹配时区分大小写
        //只读面板下筛选数据操作
        if (window.location.href.indexOf('read') >= 0) {
            $(".table-responsive table").load(window.location.href.split('?')[0] + "?from=0&count=1000 .table-responsive table",
                function () {
                    var table = $('.table-responsive').find('table');
                    var filtered = ($(".page-header").text().substr(0, 4) === '定期作业' || $(".page-header").text().substr(0, 4) === 'Recu') ? $(table).find('td[class=min-width]:contains(' + keyword + ')').closest('tr') : $(table).find('a[class=job-method]:contains(' + keyword + ')').closest('tr');
                    $(table).find('tbody tr').remove();
                    $(table).find('tbody').append(filtered);
                    //如果作业已经暂停，则用红色字体标识
                    $(table).find('tbody').find('tr').each(function () {
                        var tdArr = $(this).children();
                        var ss = tdArr.eq(1).text();
                        for (var i = 0; i < pausedjob.length; i++) {
                            if (ss === pausedjob[i]) {
                                $(this).css("color", "red");
                            }
                        }
                    });
                    $('#loaddata').css('visibility', 'hidden');
                    $('#total-items').text("Check Result: " + filtered.length);
                });
            return;
        }
        //非只读数据下,筛选数据
        $(".table-responsive table").load(window.location.href.split('?')[0] + "?from=0&count=1000 .table-responsive table",
            function () {
                var table = $('.table-responsive').find('table');
                var filtered = ($(".page-header").text().substr(0, 4) === '定期作业' || $(".page-header").text().substr(0, 4) === 'Recu') ? $(table).find('input.js-jobs-list-checkbox[value*=' + keyword + ']').closest('tr') : $(table).find('a[class=job-method]:contains(' + keyword + ')').closest('tr');
                $(table).find('tbody tr').remove();
                $(table).find('tbody').append(filtered);
                //如果作业已经暂停，则用红色字体标识
                $(table).find('tbody').find('tr').each(function () {
                    var tdArr = $(this).children();
                    var ss = tdArr.eq(1).text();
                    for (var i = 0; i < pausedjob.length; i++) {
                        if (ss === pausedjob[i]) {
                            $(this).css("color", "red");
                        }
                    }
                });
                $('#loaddata').css('visibility', 'hidden');
                $('#total-items').text("Check Result: " + filtered.length);
            });
    }
};
jobSearcher.Init();
jobSearcher.BindEvents();
function loadHttpJobModule() {
    Hangfire.httpjob = new Hangfire.HttpJob();
}

if (window.attachEvent) {
    window.attachEvent('onload', loadHttpJobModule);
} else {
    if (window.onload) {
        var curronload = window.onload;
        var newonload = function (evt) {
            curronload(evt);
            loadHttpJobModule(evt);
        };
        window.onload = newonload;
    } else {
        window.onload = loadHttpJobModule;
    }
}
