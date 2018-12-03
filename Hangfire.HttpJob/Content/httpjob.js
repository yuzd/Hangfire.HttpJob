(function (hangfire) {

    hangfire.HttpJob = (function () {
        function HttpJob() {
            this._initialize();
        }

        HttpJob.prototype._initialize = function () {
            var config = window.Hangfire.httpjobConfig;
            if (!config) return;

            //判断是否需要注入
            if (!config.NeedAddNomalHttpJobButton && !config.NeedAddRecurringHttpJobButton) {
                return;
            }


            var button ='';
            var divModel = '';
            var options = {
                schema: {},
                mode: 'code'
            };

            var normal_templete = "{\"Method\":\"POST\",\"ContentType\":\"application/json\",\"Url\":\"\",\"DelayFromMinutes\":1,\"Data\":\"\",\"Timeout\":" + config.GlobalHttpTimeOut + ",\"BasicUserName\":\"\",\"BasicPassword\":\"\",\"JobName\":\"\"}";
            var recurring_templete = "{\"Method\":\"POST\",\"ContentType\":\"application/json\",\"Url\":\"\",\"Data\":\"\",\"Timeout\":" + config.GlobalHttpTimeOut + ",\"Cron\":\"\",\"BasicUserName\":\"\",\"BasicPassword\":\"\",\"JobName\":\"\"}";
            //如果需要注入新增计划任务
            if (config.NeedAddNomalHttpJobButton) {
                button =
                    '<button type="button" class="btn" style="float: inherit;margin-right: 10px" id="addHttpJob">' +
                    config.AddHttpJobButtonName +
                    '</button>';
                divModel =
                    '<div class="modal inmodal" id="httpJobModal" tabindex="-1" role="dialog" aria-hidden="true">' +
                    '<div class="modal-dialog">' +
                    '<div class="modal-content">' +
                    '<div class="modal-header">' +
                '<h4 class="modal-title">' + config.AddHttpJobButtonName +'</h4>' +
                    '</div>' +
                    '<div class="modal-body">' +
                    '<div class="editor_holder" style="height: 250px;"></div>' +
                    '</div>' +
                    '<div class="modal-footer">' +
                ' <button type="button" class="btn btn-white" id="addhttpJob_close-model">' + config.CloseButtonName +'</button>' +
                '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-url="' + config.AddHttpJobUrl +'">' + config.SubmitButtonName+'</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>';
               
            }


            //如果需要注入新增周期性任务
            if (config.NeedAddRecurringHttpJobButton) {
                button =
                    '<button type="button" class="btn" style="float: inherit;margin-right: 10px" id="addRecurringHttpJob">' +
                config.AddRecurringJobHttpJobButtonName +
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
                '<button type="button" class="btn btn-primary" id="addhttpJob_save-model" data-url="' + config.AddRecurringJobUrl+'">' + config.SubmitButtonName + '</button>' +
                    '</div>' +
                    ' </div>' +
                    ' </div>' +
                    ' </div>'; 
            }

            if (!button || !divModel) return;
            //新增按钮
            $('.page-header').append(button);
            $(document.body).append(divModel);

            var container = $('.editor_holder')[0];
            
            try {
                window.jsonEditor = new JSONEditor(container, options);
            } catch (e) {
            }

            $('#addHttpJob').click(function () {
                window.jsonEditor.setText(normal_templete);
                window.jsonEditor.format();
                $('#httpJobModal').modal({ backdrop: 'static', keyboard: false });
                $('#httpJobModal').modal('show');
            });

            $('#addRecurringHttpJob').click(function () {
                window.jsonEditor.setText(recurring_templete);
                window.jsonEditor.format();
                $('#httpJobModal').modal({ backdrop: 'static', keyboard: false });
                $('#httpJobModal').modal('show');
            });

            
            $('#addhttpJob_close-model').click(function() {
                $('#httpJobModal').modal('hide');
                window.jsonEditor.setText('{}');
            });


            $('#addhttpJob_save-model').click(function () {
                var url = $(this).data("url");
                if (!url) return;
                var settings = {
                    "async": true,
                    "url": url,
                    "method": "POST",
                    "contentType": "application/json; charset=utf-8",
                    "dataType": "json",  
                    "data": JSON.stringify(window.jsonEditor.get())
                }
                $.ajax(settings).done(function (response) {

                    alert('success');
                    $('#httpJobModal').modal('hide');
                    window.jsonEditor.setText('{}');
                    location.reload();
                }).fail(function () {
                    alert("error");
                });
            });


            $('.jsoneditor-menu').hide();
        };

        return HttpJob;

    })();

})(window.Hangfire = window.Hangfire || {});

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