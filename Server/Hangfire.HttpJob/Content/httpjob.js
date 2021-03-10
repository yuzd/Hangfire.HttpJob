﻿(function (hangfire) {
   
    hangfire.HttpJob = (function () {
        function HttpJob() {
            this._initialize();
        }

        HttpJob.prototype._initialize = function () {
            changeTable();
            var config = window.Hangfire.httpjobConfig;
            if (!config) return;

            if (config.DashboardName) {
                //更改控制面板标题
                $(".navbar-brand").html(config.DashboardName);
            }

            if (config.DashboardFooter) {
                //更改hangfire版本显示替换为任意值
                $("#footer ul li:first-child").html('<a href="https://github.com/yuzd/Hangfire.HttpJob" target="_blank">' + config.DashboardFooter + '</a>');

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
            //增加job
            var AddCronButton = '';
            var GlobalSetButton = '';
            var josnDiv = '';
            var PauseButton = '';
            var DeleteButton = '';
            var EditRecurringJobutton = '';
            var ExportJobsButton = "";
            var ImportJobsButton = "";
            var editgeturl = config.GetRecurringJobUrl;
            var getGlobalJsonurl = config.GetGlobalSettingUrl;
            var postGlobalJsonurl = config.PostGlobalSettingUrl;
            var pauseurl = config.PauseJobUrl;
            var startBackgroudJobUrl = config.StartBackgroudJobUrl;
            var stopBackgroudJobUrl = config.StopBackgroudJobUrl;
            var agentJobDeatilButtonUrl = config.AgentJobDeatilButtonUrl;
            // var getlisturl = config.GetJobListUrl;
            var importJobsUrl = config.ImportJobsUrl;
            var exportJobsUrl = config.ExportJobsUrl;
            var divModel = '';
            var options = {
                schema: {},
                mode: 'code'
            };
            var normalObj = {
                JobName: "",
                Method: "GET",
                ContentType: "application/json",
                Url: "http://",
                DelayFromMinutes: 1,
                RunAt: "",
                Headers: {},
                Data: {},
                Timeout: config.GlobalHttpTimeOut,
                TimeZone: config.DefaultTimeZone,
                AgentClass: "",
                AgentTimeout: 0,
                BasicUserName: "",
                BasicPassword: "",
                QueueName: config.DefaultBackGroundJobQueueName,
                EnableRetry: false,
                RetryTimes: 3,
                RetryDelaysInSeconds: "20,30,60",
                SendSuccess: false,
                SendFail: true,
                Mail: "",
                CallbackEL: ""
            };
         
            var recurringObj = {
                RecurringJobIdentifier: "",
                JobName: "",
                Method: "GET",
                ContentType: "application/json",
                Url: "http://",
                Headers: {},
                Data: {},
                Timeout: config.GlobalHttpTimeOut,
                TimeZone: config.DefaultTimeZone,
                Cron: "",
                AgentClass: "",
                AgentTimeout: 0,
                BasicUserName: "",
                BasicPassword: "",
                QueueName: config.DefaultRecurringQueueName,
                EnableRetry: false,
                RetryTimes: 3,
                RetryDelaysInSeconds: "20,30,60",
                SendSuccess: false,
                SendFail: true,
                Mail: "",
                CallbackEL: ""
            };
            if (config.EnableDingTalk && config.EnableDingTalk == 'true') {
                normalObj.DingTalk = {
                    Token: config.DingtalkToken||"",
                    AtPhones: config.DingtalkPhones||"",
                    IsAtAll: config.DingtalkAtAll == 'true' ? true : false
                }
                recurringObj.DingTalk = {
                    Token: config.DingtalkToken || "",
                    AtPhones: config.DingtalkPhones||"",
                    IsAtAll: config.DingtalkAtAll == 'true'?true:false
                }
            }
            var normal_templete = JSON.stringify(normalObj);     // "{\"JobName\":\"\",\"Method\":\"GET\",\"ContentType\":\"application/json\",\"Url\":\"http://\",\"DelayFromMinutes\":1,\"Headers\":{},\"Data\":{},\"Timeout\":" + config.GlobalHttpTimeOut + ",\"BasicUserName\":\"\",\"BasicPassword\":\"\",\"QueueName\":\"" + config.DefaultBackGroundJobQueueName + "\",\"EnableRetry\":false,\"RetryTimes\":3,\"RetryDelaysInSeconds\":\"20,30,60\",\"SendSucMail\":false,\"SendFaiMail\":true,\"Mail\":\"\",\"AgentClass\":\"\",\"CallbackEL\":\"\"}";
            var recurring_templete = JSON.stringify(recurringObj); // "{\"JobName\":\"\",\"Method\":\"GET\",\"ContentType\":\"application/json\",\"Url\":\"http://\",\"Headers\":{},\"Data\":{},\"Timeout\":" + config.GlobalHttpTimeOut + ",\"Cron\":\"\",\"BasicUserName\":\"\",\"BasicPassword\":\"\",\"QueueName\":\"" + config.DefaultRecurringQueueName + "\",\"EnableRetry\":false,\"RetryTimes\":3,\"RetryDelaysInSeconds\":\"20,30,60\",\"SendSucMail\":false,\"SendFaiMail\":true,\"Mail\":\"\",\"AgentClass\":\"\",\"CallbackEL\":\"\"}";
            // console.log(normal_templete);
            // console.log(recurring_templete);

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
                    ' <div class="modal-body" style="padding:0">' +
                    '<div class="form-group">' +
                    ' <div class="editor_holder" style="height: 400px;"></div>' +
                    '</div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-url="' + config.AddHttpJobUrl + '">' + config.SubmitButtonName + '</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>';

            }

            var jobDetailModel = '<div class="modal inmodal" id="jobDetailModel" tabindex="-1" role="dialog" aria-hidden="true">' +
                '<div class="modal-dialog">' +
                '<div class="modal-content">' +
                '<div class="modal-header">' +
                '<h4 class="modal-title">AgentJob:<span id="jobNameSpan"></span></h4>' +
                '</div>' +
                '<div class="modal-body">' +
                '<div class="jobInfoDiv" style="height: 280px;white-space:normal;word-break:break-all;word-wrap:break-word;overflow-y: auto; overflow-x:hidden;"></div>' +
                '</div>' +
                '<div class="modal-footer">' +
                ' <button type="button" class="btn btn-white" id="getJobDetail_close-model">' + config.CloseButtonName + '</button>' +
                '</div>' +
                ' </div>' +
                ' </div>' +
                ' </div>';


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
                    ' <div class="modal-body" style="padding:0">' +
                    '<div class="form-group">' +
                    ' <div class="editor_holder" style="height: 400px;"></div>' +
                    '</div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-primary" id="addhttpJob_save-model"  data-title="' + config.AddRecurringJobHttpJobButtonName + '" data-url="' + config.AddRecurringJobUrl + '">' + config.SubmitButtonName + '</button>' +
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
                    ' <div class="modal-body" style="padding:0">' +
                    '<div class="form-group">' +
                    ' <div class="editor_holder" style="height: 400px;"></div>' +
                    '</div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-title="' + config.EditRecurringJobButtonName + '" data-url="' + config.EditRecurringJobUrl + '">' + config.SubmitButtonName + '</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>';
            }


            if (config.NeedAddCronButton) {
                AddCronButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" id="AddCron">' +
                    '<span class="glyphicon glyphicon-time"> ' + config.AddCronButtonName + '</span>' +
                    '</button>';

                GlobalSetButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" id="GlobalSet">' +
                    '<span class="glyphicon glyphicon-cog"> ' + config.GlobalSetButtonName + '</span>' +
                    '</button>';
                josnDiv =
                    '<div class="modal inmodal" id="jsonModel" tabindex="-1" role="dialog" aria-hidden="true">' +
                    '<div class="modal-dialog" >' +
                    '<div class="modal-content">' +
                    '<div class="modal-header">' +
                    '<h4 class="modal-title">' + config.GlobalSetButtonName + '</h4>' +
                    '</div>' +
                    ' <div class="modal-body" style="padding:0">' +
                    '<div class="form-group">' +
                    ' <div class="editor_holder2" style="height: 350px;"></div>' +
                    '</div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                    '<button type="button" class="btn btn-danger" id="colseconfig_close-model">' + config.CloseButtonName + '</button>' +
                    '<button type="button" class="btn btn-success" data-title="' + config.GlobalSetButtonName + '"  id="saveconfig_close-model">' + config.SubmitButtonName + '</button>' +
                    '  </div>' +
                    '  </div>' +
                    '</div>' +
                    '</div>';
            }

            //暂停和启用任务
            PauseButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-success" style="float:inherit;margin-left:10px" data-loading-text="..." disabled id="PauseJob">' +
                '<span class="glyphicon glyphicon-stop"> ' + (config.NeedAddNomalHttpJobButton ? config.StartBackgroudJobButtonName : config.PauseJobButtonName) + '</span>' +
                '</button>';
            
            // 获取AgentJob 详情
            var getAgentJobDeatilButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" data-loading-text="..." disabled id="JobDetail">' +
                '<span class="glyphicon glyphicon-record"> ' + (config.AgentJobDeatilButton) + '</span>' +
                '</button>';

            // 导出按钮
            if (config.NeedExportJobsButton) {
                ExportJobsButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float: inherit;margin-left: 10px" id="exportHttpJobs">' +
                    '<span class="glyphicon glyphicon-export"> ' + config.ExportJobsButtonName + '</span>' +
                    '</button>';
            }

            // 导入按钮
            if (config.NeedImportJobsButton) {
                ImportJobsButton =
                    '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float: inherit;margin-left:10px" id="importHttpJobs">' +
                    '<span class="glyphicon glyphicon-import"> ' + config.ImportJobsButtonName + '</span>' +
                    '</button>';
            }

            var importDivModel =
                '<div class="modal inmodal" id="div_import_model" tabindex="-1" role="dialog" aria-hidden="true">' +
                '<div class="modal-dialog">' +
                '<div class="modal-content">' +
                '<div class="modal-header">' +
                '<h4 class="modal-title">' + "导入任务列表" + '</h4>' +
                '</div>' +
                '<div class="modal-body">' +
                '<div class="editor_holder3" style="height: 400px;"></div>' +
                '</div>' +
                '<div class="modal-footer">' +
                ' <button type="button" class="btn btn-white" id="btn_importJobs_close">' + config.CloseButtonName + '</button>' +
                '<button type="button" class="btn btn-primary" id="btn_importJobs_save" data-url="' + importJobsUrl + '">' + config.SubmitButtonName + '</button>' +
                '</div>' +
                ' </div>' +
                ' </div>' +
                ' </div>';

            if (!button || !divModel) return;
            //新增按钮
            $('.page-header').append(button);
            $('.page-header').append(EditRecurringJobutton);
            $('.page-header').append(AddCronButton);
            $('.page-header').append(GlobalSetButton);
            $('.page-header').append(ExportJobsButton); // 导出任务列表
            $('.page-header').append(ImportJobsButton); // 导入任务列表
            //$('.btn-toolbar-top').append(PauseButton);
            //$('.btn-toolbar-top').append(getAgentJobDeatilButton);


            $(document.body).append(divModel);
            $(document.body).append(jobDetailModel);
            $(document.body).append(importDivModel);
            $(document.body).append(josnDiv);
            var removeUrl = '';
            if (config.NeedEditRecurringJobButton) {

                //对删除按钮进行改造
                //var removeButtonName = $($('.js-jobs-list-command')[1]).text().trim();
                //removeUrl = $($('.js-jobs-list-command')[1]).data('url');
                //$($('.js-jobs-list-command')[1]).remove();
                //DeleteButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-danger" style="float:inherit;margin-left:10px" data-loading-text="..." disabled id="DelJob">' +
                //    '<span class="glyphicon glyphicon-play"> ' + removeButtonName + '</span>' +
                //    '</button>';

                //$('.btn-toolbar-top').append(DeleteButton);

                //启动或暂停
                $('.btn-toolbar-top').append(PauseButton);

                //带参数执行
                var PauseAgentButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-primary" style="float:inherit;margin-left:10px" data-loading-text="..." disabled id="StartAgentJob">' +
                    '<span class="glyphicon glyphicon-play"> ' + (config.StartBackgroudJobButtonName) + '</span>' +
                    '</button>';

                $('.btn-toolbar-top').append(PauseAgentButton);


                //停止
                var StopButton = '<button type="button" class="js-jobs-list-command btn btn-sm btn-danger" style="float:inherit;margin-left:10px" data-loading-text="stop agent job..." disabled id="StopJob">' +
                    '<span class="glyphicon glyphicon-stop"> ' + config.StopBackgroudJobButtonName + '</span>' +
                    '</button>';
                $('.btn-toolbar-top').append(StopButton);

                $('.btn-toolbar-top').append(getAgentJobDeatilButton);

            }

            var container = $('.editor_holder')[0];
            var container2 = $('.editor_holder2')[0];
            var container3 = $('.editor_holder3')[0];

            try {
                window.jsonEditor = new JSONEditor(container, options);
                window.jsonViewEditor = new JSONEditor(container2, options);
                window.jsonEditor3 = new JSONEditor(container3, options);
            } catch (e) {
                console.log(e);
            }


            //try {
            //    window.jsonEditor3 = new JSONEditor(container3, options);
            //} catch (e2) {
            //    console.log(e2);
            //}

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

            //JobAgent停止
            $('#StopJob').click(function (e) {

                if ($("input[type=checkbox]:checked").length > 2) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭'?"只能选择一个任务": "Select One Job Only!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                var jobId = $(".js-jobs-list-checkbox:checked").val();
                if (!jobId) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务" :"Please Select One Job!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                //var corn = $($($(".js-jobs-list-checkbox:checked")[0]).parent().parent()).children().eq(2).text().indexOf('0 0 31 2 *');
                //if (corn != -1) {
                //    //已经暂停了

                //}
                var text = $($($(".js-jobs-list-checkbox:checked")[0]).parent().parent()).children().eq(4).html();
                if (!text || text.indexOf('Agent:') < 0) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "当前选择的任务非JobAgent类型!" :"Selected Job is not a jobAgent!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }
                swal({
                    title: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "您确定要发送停止指令吗!":"Are you sure want to stop?",
                    type: "success",
                    showCancelButton: true,
                    closeOnConfirm: false,
                    animation: "slide-from-top",
                    confirmButtonColor: "#DD6B55",
                    confirmButtonText: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "提交" : "Submit",
                    cancelButtonText: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "取消" : "Cancel"
                }, function () {
                    $.ajax({
                        type: "post",
                        url: stopBackgroudJobUrl,
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json" }),
                        async: true,
                        success: function (returndata) {
                            $.post($($('.btn-toolbar button')[0]).data('url'), { 'jobs[]': [jobId] }, function () {
                                window.location.reload();
                            });
                        }
                    });
                });
                e.stopPropagation();
                e.preventDefault();
            });

            //获取agentJob的执行情况
            $("#JobDetail").click(function (e) {

                if ($("input[type=checkbox]:checked").length > 2) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" : "Select One Job Only!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                var jobId = $(".js-jobs-list-checkbox:checked").val();
                if (!jobId) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!": "Please Select One Job!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                var text = $($($(".js-jobs-list-checkbox:checked")[0]).parent().parent()).children().eq(4).html();
                if (!text || text.indexOf('Agent:') < 0) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "当前选择的任务非JobAgent类型!" : "Selected Job is not a jobAgent!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                swal({
                    title: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "注意" :"JobAgent",
                    text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "您确定要发送获取运行详情指令吗!" :"Are you sure to get JobDetail?",
                    type: "warning",
                    showCancelButton: true,
                    closeOnConfirm: false,
                    animation: "slide-from-top",
                    inputPlaceholder: "start param",
                    showLoaderOnConfirm: true,
                    confirmButtonColor: "#DD6B55",
                    confirmButtonText: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "提交" :"Submit",
                    cancelButtonText: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "取消" : "Cancel"
                }, function () {
                    $.ajax({
                        type: "post",
                        url: agentJobDeatilButtonUrl,
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json", "Cron": (config.NeedEditRecurringJobButton ? "1" : "") }),
                        async: true,
                        success: function (returndata) {
                            swal.close()
                            if (returndata && returndata.JobName) {
                                $('#jobNameSpan').html(returndata.JobName);
                            }

                            $('.jobInfoDiv').html(returndata.Info || '');
                            $('#jobDetailModel').modal({ backdrop: 'static', keyboard: false });
                            $('#jobDetailModel').modal('show');
                        }
                    });
                });

                e.stopPropagation();
                e.preventDefault();
            });

            $('#StartAgentJob').click(function (e) {

                if ($("input[type=checkbox]:checked").length > 2) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" :  "Select One Job Only!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }
                var jobId = $(".js-jobs-list-checkbox:checked").val();
                if (!jobId) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" : "Please Select One Job!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                swal({
                    title: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "运行" :  "Start Job",
                    text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "确定要发送运行指令吗?" :  "Are you sure want to start?",
                    type: "input",
                    showCancelButton: true,
                    closeOnConfirm: false,
                    animation: "slide-from-top",
                    inputPlaceholder: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "可输入本次运行的参数" :  "start param",
                    showLoaderOnConfirm: true,
                    confirmButtonColor: "#DD6B55",
                    confirmButtonText: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "提交" : "Submit",
                    cancelButtonText: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "取消" : "Cancel"
                }, function (inputValue) {
                    if (inputValue === false) return false;
                    $.ajax({
                        type: "post",
                        url: startBackgroudJobUrl,
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "Data": inputValue, "ContentType": "application/json" }),
                        async: true,
                        success: function (returndata) {

                            $.post($($('.btn-toolbar button')[0]).data('url'), { 'jobs[]': [jobId] }, function () {
                                swal({
                                    title: "Success",
                                    text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "运行指令发送成功" :"start job success",
                                    type: "success"
                                });
                                window.location.reload();
                            });

                        }
                    });
                });
                e.stopPropagation();
                e.preventDefault();
            });

            //删除job
            $('#DelJob').click(function (e) {
              
                var jobId = [];
                $.each($(".js-jobs-list-checkbox:checked"), function (index, d) { jobId.push($(d).val()) });
                if (jobId.length<1) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" :  "Please Select One Job!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }
                swal({
                    title: "Remove Job",
                    text: "Are you sure want to remove?",
                    type: "warning",
                    showCancelButton: true,
                    closeOnConfirm: false,
                    animation: "slide-from-top",
                    inputPlaceholder: "start param",
                    showLoaderOnConfirm: true,
                    confirmButtonColor: "#DD6B55",
                    confirmButtonText: "Submit",
                    cancelButtonText: "Cancel"
                }, function () {
                    $.ajax({
                        type: "post",
                        url: agentJobDeatilButtonUrl,
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json", "Cron": (config.NeedEditRecurringJobButton ? "1" : "") }),
                        async: true,
                        success: function (returndata) {
                            $.post(removeUrl, { 'jobs[]': jobId}, function () {
                                window.location.reload();
                            });
                        }
                    });
                });

                e.stopPropagation();
                e.preventDefault();
            });
            //暂停任务
            $("#PauseJob").click(function (e) {

                if ($("input[type=checkbox]:checked").length > 2) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" :  "Select One Job Only!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }
                var jobId = $(".js-jobs-list-checkbox:checked").val();
                if (!jobId) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" : "Please Select One Job!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                $.ajax({
                    type: "post",
                    url: pauseurl,
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json" }),
                    async: true,
                    success: function (returndata) {
                        window.location.reload();
                    },
                    fail: function (errText) {
                        swal({
                            title: "",
                            text: errText.responseText || "fail！",
                            type: "error"
                        });
                    }
                });
            });


            //全局配置
            $('#GlobalSet').click(function (e) {
                $(".modal-title").html(config.GlobalSetButtonName);
                $.ajax({
                    type: "post",
                    url: getGlobalJsonurl,
                    contentType: "html",
                    async: true,
                    success: function (returndata) {
                        if (!returndata || returndata.length < 1) {
                            $('#jsonModel').modal({
                                backdrop: 'static',
                                keyboard: false
                            });
                            return;
                        }
                        if (returndata.substr(0, 4) == 'err:') {
                            swal({
                                title: "",
                                text: returndata,
                                type: "error"
                            });
                            return;
                        }
                        window.jsonViewEditor.setText(returndata);
                        window.jsonViewEditor.format();
                        $('#jsonModel').modal({
                            backdrop: 'static',
                            keyboard: false
                        });
                    },
                    fail: function (errText) {
                        swal({
                            title: "",
                            text: errText.responseText || "get json fail！",
                            type: "error"
                        });
                    }

                });

            });

            function jsonSave() {
                var json = window.jsonViewEditor.getText();
                if (json.length < 1) {
                    swal({
                        title: "",
                        text: 'please input json',
                        type: "error"
                    });
                    return;
                }

                $.ajax({
                    type: "post",
                    url: postGlobalJsonurl,
                    contentType: "html",
                    data: json,
                    async: true,
                    success: function (returndata) {
                        if (returndata.substr(0, 4) == 'err:') {
                            swal({
                                title: "",
                                text: returndata,
                                type: "error"
                            });
                            return;
                        } else {
                            swal({
                                title: "",
                                text: 'save success',
                                type: "success"
                            });
                        }
                        clearJsonEditor();
                    },
                    fail: function (errText) {
                        swal({
                            title: "",
                            text: errText.responseText || "save fail！",
                            type: "error"
                        });
                    }

                });
            }

            //编辑任务
            $("#EditJob").click(function (e) {
                if ($("input[type=checkbox]:checked").length > 2) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" : "Select One Job Only!",
                        type: "error"
                    });
                    return;
                }
                var jobId = $(".js-jobs-list-checkbox:checked").val();
                if (!jobId) {
                    swal({
                        title: "",
                        text: window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? "只能选择一个任务!" : "Please Select One Job!",
                        type: "error"
                    });
                    e.stopPropagation();
                    e.preventDefault();
                    return;
                }

                $(".modal-title").html(config.EditRecurringJobButtonName);
                $.ajax({
                    type: "post",
                    url: editgeturl,
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json" }),
                    async: true,
                    success: function (returndata) {
                        window.jsonEditor.setText(JSON.stringify(returndata));
                        window.jsonEditor.format();
                        $('#httpJobModal').modal('show');
                    },
                    fail: function (errText) {
                        swal({
                            title: "",
                            text: errText.responseText || "edit job fail！",
                            type: "error"
                        });
                    }

                });
            });
            //打开cron表达式页面
            $("#AddCron").click(function () {
                window.open(config.AddCronUrl);
            });
            $('#addhttpJob_close-model').click(function () {
                clearJsonEditor();

            });
            $('#colseconfig_close-model').click(function () {
                clearJsonEditor();
            });

            $('#saveconfig_close-model').click(function () {
                jsonSave();
            });


            $('#getJobDetail_close-model').click(function () {
                $('#jobNameSpan').html('');
                $('#jobDetailModel').modal('hide');
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
                if (obj && obj.Success && obj.Success.Data) {
                    if (typeof obj.Success.Data === 'string' || obj.Success.Data instanceof String) {
                    } else {
                        obj.Success.Data = JSON.stringify(obj.Success.Data);
                    }

                    if (obj.Success.Data == '{}') obj.Success.Data = '';
                }
                if (obj && obj.Fail && obj.Fail.Data) {
                    if (typeof obj.Fail.Data === 'string' || obj.Fail.Data instanceof String) {
                    } else {
                        obj.Fail.Data = JSON.stringify(obj.Fail.Data);
                    }

                    if (obj.Fail.Data == '{}') obj.Fail.Data = '';
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
                        clearJsonEditor();
                        location.reload();
                    });

                }).fail(function (errText) {
                    swal({
                        title: "",
                        text: errText.responseText || "Add job fail！",
                        type: "error"
                    });
                });
            });

            $('.jsoneditor-menu').hide();

            var tmpjoblist = null;
            $('#exportHttpJobs').click(function () {
                var settings = getAjaxSetting(exportJobsUrl, null);
                $.ajax(settings)
                    .done(function (response) {
                        tmpjoblist = response;
                        $(".modal-title").html(config.ExportJobsButtonName);
                        $("#imhttpJob_save-model").disabled = true;
                        setJson2JsonEditor(3, JSON.stringify(tmpjoblist));
                        $('#div_import_model').modal({ backdrop: 'static', keyboard: false });
                        $('#btn_importJobs_save').hide();
                        $('#div_import_model').modal('show');
                    }).fail(function () {
                        swal({
                            title: "",
                            text: "Export Jobs Fail！",
                            type: "error"
                        });
                    });
            });

            $('#importHttpJobs').click(function () {
                //var settings = getAjaxSetting(exportJobsUrl, null);
                //$.ajax(settings)
                //    .done(function (response) {
                //        tmpjoblist = response;
                //        $(".modal-title").html(config.ImportJobsButtonName);
                //        setJson2JsonEditor(3, JSON.stringify(tmpjoblist));
                //    })
                //    .fail(function () {
                //        setJson2JsonEditor(3, "{}");
                //    });
                $(".modal-title").html(config.ImportJobsButtonName);
                setJson2JsonEditor(3, "[]");
                $('#btn_importJobs_save').show();
                $('#div_import_model').modal({ backdrop: 'static', keyboard: false });
                $('#div_import_model').modal('show');
            });

            $("#btn_importJobs_close").click(function () {
                clearJsonEditor();
            });

            $("#btn_importJobs_save").click(function () {
                var url = $(this).data("url");
                if (!url) return;
                console.log(url);
                var obj = getIOEditorData();
                if (obj && obj.Data) {
                    if (typeof obj.Data === "string" || obj.Data instanceof String) {
                    } else {
                        obj.Data = JSON.stringify(obj.Data);
                    }

                    if (obj.Data === '{}') obj.Data = '';
                }
                if (obj.Data === "") {
                    window.swal({
                        title: "",
                        text: "内容不能为空！",
                        type: "error"
                    });
                    return;
                }

                var settings = getAjaxSetting(url, JSON.stringify(obj));
                $.ajax(settings)
                    .done(function (response) {
                        console.log(response);
                        window.swal({
                            title: "Success",
                            type: "success",
                            showCancelButton: false,
                            closeOnConfirm: false,
                            animation: "slide-from-top",
                            confirmButtonColor: "#DD6B55",
                            confirmButtonText: "OK"
                        }, function () {
                            clearJsonEditor();
                            location.reload();
                        });

                    }).fail(function () {
                        window.swal({
                            title: "",
                            text: "Add job fail！",
                            type: "error"
                        });
                    });
            });

            function setJson2JsonEditor(edit, json) {
                if (edit === 1) { // for job 
                    window.jsonEditor.setText(json);
                    window.jsonEditor.format();
                }
                else if (edit === 2) { // for config
                    window.jsonViewEditor.setText(json);
                    window.jsonViewEditor.format();
                }
                else if (edit === 3) { // for import|export
                    window.jsonEditor3.setText(json);
                    window.jsonEditor3.format();
                }
            }

            function clearJsonEditor() {

                $('#jobDetailModel').modal("hide");
                $('#httpJobModal').modal("hide");
                $('#div_import_model').modal("hide");
                $('#jsonModel').modal('hide');

                window.jsonEditor.setText("{}");
                if (window.jsonViewEditor)window.jsonViewEditor.setText("{}");
                if (window.jsonEditor3)window.jsonEditor3.setText("{}");
            }

            function getIOEditorData() {
                var obj = window.jsonEditor3.get();
                return obj;
            }

            function getAjaxSetting(url, data) {
                var settings = {
                    async: true,
                    url: url,
                    method: "POST",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    data: data
                }
                return settings;
            }


            //设置job详细
            window.AgentJobDetail = function(name, isAgent) {
                var jobId = Base64.decode(name);

                if (!jobId) {
                    return;
                }

                $(".modal-title").html(config.EditRecurringJobButtonName + (isAgent?"【AgentJob】":""));
                $.ajax({
                    type: "post",
                    url: editgeturl,
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ "JobName": jobId, "URL": "baseurl", "ContentType": "application/json" }),
                    async: true,
                    success: function (returndata) {
                        window.jsonEditor.setText(JSON.stringify(returndata));
                        window.jsonEditor.format();
                        $('#httpJobModal').modal('show');
                    },
                    fail: function (errText) {
                        swal({
                            title: "",
                            text: errText.responseText || "edit job fail！",
                            type: "error"
                        });
                    }

                });


            }
        };

        return HttpJob;

    })();





    if (!window.location.pathname.endsWith('/servers') || !window.Hangfire.httpjobConfig.GetAgentServerListUrl) {
        return;
    }


    $.ajax({
        type: "post",
        url: window.Hangfire.httpjobConfig.GetAgentServerListUrl,
        contentType: "html",
        async: true,
        success: function (returndata) {
            if (!returndata || returndata.length < 1) {
                return;
            }
            if (returndata.substr(0, 4) == 'err:') {
                swal({
                    title: "",
                    text: returndata,
                    type: "error"
                });
                return;
            }

            $($('.row')[0]).append($(returndata));
            Hangfire.page._initialize();
        },
        fail: function (errText) {
            swal({
                title: "",
                text: errText.responseText || "get json fail！",
                type: "error"
            });
        }
    });
  

})(window.Hangfire = window.Hangfire || {});


function changeTable() {
    if (window.location.pathname.endsWith('/failed')) {
        $(".table tbody").show();
        return;
    }
    var isreJob = window.location.pathname.endsWith('/recurring');
    var config = window.Hangfire.httpjobConfig;
    $(".table tbody").find('tr').each(function () {
        var tdArr = $(this).children();
        var s1 = tdArr.eq(1).text();

        var isAgentJob = false;
        var isHttpJob = false;
        if (s1.indexOf('|') >= 0) {
            var ss1 = s1.split('|');
            if (s1.indexOf('Agent:') > 0) {
                //第一个是详细信息 第二个是className 第三个是别名(可能为空) 第四个是host
                var h = '<span class="label label-primary" title="" data-original-title="' +
                    ss1[0] +
                    '">' +
                    ss1[1] +
                    '</span>' +
                    (ss1[2] == ''
                        ? ''
                        : '<span class="label label-primary left5" title="" data-original-title="">' +
                        ss1[2] +
                        '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-primary" title="" data-original-title="">' + ss1[3] + '</span>' :'<span class="label label-primary left5" title="" data-original-title="">' + ss1[3] + '</span>');
                tdArr.eq(1).html(h);
            } else {
                var h = '<span class="label label-success" title="" data-original-title="' +
                    ss1[0] +
                    '">' +
                    ss1[1] +
                    '</span>' +
                    (ss1[2] == ''
                        ? ''
                        : '<span class="label label-success left5" title="" data-original-title="">' +
                        ss1[2] +
                        '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-success" title="" data-original-title="">' + ss1[3] + '</span>' : '<span class="label label-success left5" title="" data-original-title="">' + ss1[3] + '</span>');
                tdArr.eq(1).html(h);
            }

        }

        var ss = tdArr.eq(2).text();


        if (ss.indexOf('|') >= 0) {
            var ss1 = ss.split('|');
            if (ss.indexOf('Agent:') > 0) {
                //第一个是详细信息 第二个是className 第三个是别名(可能为空) 第四个是host
                var h = '<span class="label label-primary" title="" data-original-title="' +
                    ss1[0] +
                    '">' +
                    ss1[1] +
                    '</span>' +
                    (ss1[2] == ''
                        ? ''
                        : '<span class="label label-primary left5" title="" data-original-title="">' +
                        ss1[2] +
                        '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-primary" title="" data-original-title="">' + ss1[3] + '</span>' :'<span class="label label-primary left5" title="" data-original-title="">' + ss1[3] + '</span>' );
                tdArr.eq(2).html(h);
            } else {
                var h = '<span class="label label-success" title="" data-original-title="' +
                    ss1[0] +
                    '">' +
                    ss1[1] +
                    '</span>' +
                    (ss1[2] == ''
                        ? ''
                        : '<span class="label label-success left5" title="" data-original-title="">' +
                        ss1[2] +
                        '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-success" title="" data-original-title="">' + ss1[3] + '</span>' : '<span class="label label-success left5" title="" data-original-title="">' + ss1[3] + '</span>' );
                tdArr.eq(2).html(h);
            }
           
        }

        var ss2 = tdArr.eq(4).text();
        if (ss2.indexOf('|') >= 0) {
            var ss1 = ss2.split('|');
            if (ss2.indexOf('Agent:') > 0) {
                isAgentJob = true;
                var h = '<span class="label label-primary" title="" data-original-title="' +
                    ss1[0] +
                    '">' +
                    ss1[1] +
                    '</span>' +
                    (ss1[2] == ''
                        ? ''
                        : '<span class="label label-primary left5" title="" data-original-title="">' +
                        ss1[2] +
                        '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-primary" title="" data-original-title="">' + ss1[3] + '</span>' : '<span class="label label-primary left5" title="" data-original-title="">' + ss1[3] + '</span>');
                tdArr.eq(4).html(h);
            } else {
                var h = '<span class="label label-success" title="" data-original-title="' +
                    ss1[0] +
                    '">' +
                    ss1[1] +
                    '</span>' +
                    (ss1[2] == ''
                        ? ''
                        : '<span class="label label-success left5" title="" data-original-title="">' +
                        ss1[2] +
                        '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-success" title="" data-original-title="">' + ss1[3] + '</span>' : '<span class="label label-success left5" title="" data-original-title="">' + ss1[3] + '</span>');
               tdArr.eq(4).html(h);
               isHttpJob = true;
            }
           
        }

        if (tdArr.eq(3).text().indexOf('China Standard') > 0) {
            tdArr.eq(3).html(tdArr.eq(3).html().replace(' Standard Time', ''));
        } else {
            var ss3 = tdArr.eq(3).text();
            if (ss3.indexOf('|') >= 0) {
                var ss1 = ss3.split('|');
                if (ss3.indexOf('Agent:') > 0) {
                    isAgentJob = true;
                    var h = '<span class="label label-primary" title="" data-original-title="' +
                        ss1[0] +
                        '">' +
                        ss1[1] +
                        '</span>' +
                        (ss1[2] == ''
                            ? ''
                            : '<span class="label label-primary left5" title="" data-original-title="">' +
                            ss1[2] +
                            '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-primary" title="" data-original-title="">' + ss1[3] + '</span>' : '<span class="label label-primary left5" title="" data-original-title="">' + ss1[3] + '</span>');
                    tdArr.eq(3).html(h);
                } else {
                    var h = '<span class="label label-success" title="" data-original-title="' +
                        ss1[0] +
                        '">' +
                        ss1[1] +
                        '</span>' +
                        (ss1[2] == ''
                            ? ''
                            : '<span class="label label-success left5" title="" data-original-title="">' +
                            ss1[2] +
                            '</span>') + (isreJob ? '<hr style="margin:0"><span class="label label-success" title="" data-original-title="">' + ss1[3] + '</span>' : '<span class="label label-success left5" title="" data-original-title="">' + ss1[3] + '</span>');
                    tdArr.eq(3).html(h);
                }
                
            }
        }

        if (isreJob && isAgentJob) {
            tdArr.eq(1).html('<a style="" title="" data-original-title="' + (window.Hangfire.httpjobConfig.CloseButtonName == '关闭'?'点击查看详情':'Detail')+'" href="javascript:;" onclick="window.AgentJobDetail(\'' + Base64.encode(tdArr.eq(1).text()) + '\',true)">' + tdArr.eq(1).text()+'</a>');
        }

        if (isreJob && isHttpJob) {
            tdArr.eq(1).html('<a style="" title="" data-original-title="' + (window.Hangfire.httpjobConfig.CloseButtonName == '关闭' ? '点击查看详情' : 'Detail') +'" href="javascript:;" onclick="window.AgentJobDetail(\'' + Base64.encode(tdArr.eq(1).text()) + '\',false)">' + tdArr.eq(1).text() + '</a>');
        }

        if (config.ShowTag && "True" == config.ShowTag && config.NeedAddRecurringHttpJobButton) {
            var tag = tdArr.eq(1).html();
            if (tag.indexOf('Go to Tag') == -1) {
                tdArr.eq(1).append('<a class="label label-success text-uppercase" title="" data-original-title="Go to Tag Page" href="' + config.AppUrl + '/tags/search/' + tdArr.eq(1).text() + '" target="_blank">Tag</a>');
            }
           
        }
    });
    $(".table tbody").show();
    if (document.documentElement.lang == "zh") {
        try {
            moment.locale('zh-cn');
            var updateRelativeDates = function () {
                $('*[data-moment]').each(function () {
                    var $this = $(this);
                    var timestamp = $this.data('moment');

                    if (timestamp) {
                        var time = moment(timestamp, 'X');
                        $this.html(time.fromNow())
                            .attr('title', time.format('llll'))
                            .attr('data-original-title', time.format("l LTS"))
                            .attr('data-container', 'body');
                    }
                });

                $('*[data-moment-title]').each(function () {
                    var $this = $(this);
                    var timestamp = $this.data('moment-title');

                    if (timestamp) {
                        var time = moment(timestamp, 'X');
                        $this.prop('title', time.format('llll'))
                            .attr('data-original-title', time.format("l LTS"))
                            .attr('data-container', 'body');
                    }
                });

                $('*[data-moment-local]').each(function () {
                    var $this = $(this);
                    var timestamp = $this.data('moment-local');

                    if (timestamp) {
                        var time = moment(timestamp, 'X');
                        $this.html(time.format('l LTS'));
                    }
                });
            };

            updateRelativeDates();
        } catch (e) {

        } 
      
    }
    $('*[title]').tooltip();
}

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
            '<input type="text" id="search-box" placeholder="' + (!window.Hangfire.httpjobConfig.NeedAddRecurringHttpJobButton ? window.Hangfire.httpjobConfig.SearchPlaceholder : window.Hangfire.httpjobConfig.SearchPlaceholder + " " + window.Hangfire.httpjobConfig.SearchPlaceholderExt) + '">' +
            //'<img class="loader-img" src ="" />' +
            '<span class="glyphicon glyphicon-search" id="loaddata"> Checking...</span>' +
            '<p id="total-items"></p>' +
            '</div>');
    };
    this.Init = function () {
        createSearchBox();

        if (location.search.indexOf('?search=') === 0) {
            var searchValue = location.search.replace('?search=', '');
            $('#search-box').val(searchValue);
            setTimeout(function () { FilterJobs(searchValue); }, 500);
        }
    };

    this.BindEvents = function () {
        $('#search-box').bind('change', function (e) {
            if (this.value.length === 0)
                window.location.reload();
            //else
            //    GetPausedJobs();
            FilterJobs(this.value);
        });
    };

    
    

    function FilterJobs(keyword) {
        $('#loaddata').css('visibility', 'unset');
        //在所有查询结果中查找满足条件的，模糊匹配时区分大小写
        //只读面板下筛选数据操作
        //if (window.location.href.indexOf('read') >= 0) {
        //    $(".table-responsive table").load(window.location.href.split('?')[0] + "?from=0&count=1000 .table-responsive table",
        //        function () {
        //            var table = $('.table-responsive').find('table');
        //            if (keyword.indexOf('name:') == 0 && location.href.endsWith('/recurring')) {
        //                var filtered = $(table).find('td.width-30:contains(' + keyword.substr(5) + ')').closest('tr');
        //            } else {
        //                var filtered = (location.href.endsWith('/recurring')) ? $(table).find('input.js-jobs-list-checkbox[value*=' + keyword + ']').closest('tr') : $(table).find('a[class=job-method]:contains(' + keyword + ')').closest('tr');
        //            }
        //            $(table).find('tbody tr').remove();
        //            //如果是failed页面 需要在每个下面多加一个tr 否则会导致样式问题
        //            if (location.href.endsWith('jobs/failed')) {
        //                for (var j = 0; j < filtered.length; j++) {
        //                    $(table).find('tbody').append(filtered[j]);
        //                    $(table).find('tbody').append('<tr></tr>');
        //                }
        //                $('.js-jobs-list .expander').remove();
        //            } else {
        //                $(table).find('tbody').append(filtered);
        //            }
        //            //如果作业已经暂停，则用红色字体标识
        //            //$(table).find('tbody').find('tr').each(function () {
        //            //    var tdArr = $(this).children();
        //            //    var ss = tdArr.eq(1).text();
        //            //    for (var i = 0; i < pausedjob.length; i++) {
        //            //        if (ss === pausedjob[i]) {
        //            //            $(this).css("color", "red");
        //            //        }
        //            //    }
        //            //});
        //            $('#loaddata').css('visibility', 'hidden');
        //            $('#total-items').text("Check Result: " + filtered.length);
        //            changeTable();
        //        });
        //    return;
        //}
        //非只读数据下,筛选数据
        $(".table-responsive table").load(window.location.href.split('?')[0] + "?from=0&count=1000 .table-responsive table",
            function () {
                var table = $('.table-responsive').find('table');
                if (keyword.indexOf('name:') == 0 && location.pathname.endsWith('/recurring')) {
                    var filtered = $(table).find('td.width-30:contains(' + keyword.substr(5) +')').closest('tr');
                } else {
                    var filtered = (location.pathname.endsWith('/recurring')) ? $(table).find('input.js-jobs-list-checkbox[value*=' + keyword + ']').closest('tr') : $(table).find('a[class=job-method]:contains(' + keyword + ')').closest('tr');
                }
                 
                $(table).find('tbody tr').remove();
                //如果是failed页面 需要在每个下面多加一个tr 否则会导致样式问题
                if (location.pathname.endsWith('jobs/failed')) {
                    for (var j = 0; j < filtered.length; j++) {
                        $(table).find('tbody').append(filtered[j]);
                        $(table).find('tbody').append('<tr></tr>');
                    }
                    $('.js-jobs-list .expander').remove();
                } else {
                    $(table).find('tbody').append(filtered);
                }
                //如果作业已经暂停，则用红色字体标识
                //$(table).find('tbody').find('tr').each(function () {
                //    var tdArr = $(this).children();
                //    var ss = tdArr.eq(1).text();
                //    for (var i = 0; i < pausedjob.length; i++) {
                //        if (ss === pausedjob[i]) {
                //            $(this).css("color", "red");
                //        }
                //    }
                //});
                $('#loaddata').css('visibility', 'hidden');
                $('#total-items').text("Check Result: " + filtered.length);
                changeTable();
            });
    }
};
jobSearcher.Init();
jobSearcher.BindEvents();
function loadHttpJobModule() {
    Hangfire.httpjob = new Hangfire.HttpJob();

    //$('.container').addClass('container-fluid').removeClass('container');
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


