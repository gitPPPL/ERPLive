async function checkApprovalStatus(vType, vNo, tableName) {
    try {
        const res = await $.ajax({
            url: '/Approval/CheckApprovalStatus',
            type: 'GET',
            data: {
                v_type: vType,
                v_no: vNo,
                tableName: tableName
            }
        });
        console.log("Approval Status:", res.message);
        // Hide buttons first
        $('#btn_Sendapproval').hide().prop('disabled', true);
        $('#btn_Approved').hide().prop('disabled', true);
        switch (res.message) {
            case "GetData":
                ApprovalWindowData = {
                    DocType: vType,
                    DocNo: vNo,
                    TableName: tableName
                };
                $('#btn_Approved')
                    .text('Send For Approval')
                    .show()
                    .prop('disabled', false);
                break;
            case "NullData":
                $('#btn_Sendapproval')
                    .show()
                    .prop('disabled', false);
                break;
            case "DocumentApproved":
                $('#btn_Approved')
                    .text('Approved')
                    .show()
                    .prop('disabled', true);
                break;
            //case "PendingWithOtherUser":
            //    $('#btn_Approved').hide();
            //    $('#btn_Sendapproval').hide();
            //    showToast(
            //        "This document is pending approval with another user.",
            //        { type: "warning" }
            //    );
            //    break;

            case "PendingWithOtherUser":
                $('#btn_Approved').hide();

                $('#btn_Sendapproval')
                    .show()
                    .prop('disabled', false); 

                break;

        }
    }
    catch (error) {

        console.error('Approval Error:', error);

    }
}
var ApprovalData = {
    DocType: '',
    DocNo: 0,
    UserCode: null,
    UserName: null,
    DocDate: null,
    TableName:''
};
function createSendForApprovalModal() {
    if ($('#SendForapprovedModal').length > 0)
        return;
    let modalHtml = `
    <div class="modal fade" id="SendForapprovedModal" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">

                <div class="modal-header">
                    <h5 class="modal-title">Send For Approval</h5>

                    <button type="button"
                            class="btn-close"
                            data-bs-dismiss="modal">
                    </button>
                </div>

                <div class="modal-body">

                    <div class="row">

                        <div class="col-md-6">
                            <label>Send To</label>
                            <select id="ddlsendto" class="form-control">
                                <option value="">Select Send To</option>
                            </select>
                        </div>

                        <div class="col-md-6">
                            <label>Remarks</label>
                            <input type="text" id="ddlsendRemarks" class="form-control" placeholder="Search Remark" />
                        </div>


                    </div>

                </div>

                <div class="modal-footer">

                    <button type="button"
                            class="btn btn-secondary"
                            data-bs-dismiss="modal">
                        Close
                    </button>

                    <button type="button"
                            id="btn_Sendapp"
                            class="btn btn-primary">
                        Send Approval
                    </button>

                </div>

            </div>
        </div>
    </div>`;
    $('body').append(modalHtml);
}
window.OpenSendForApprovalModal = function (data) {
    ApprovalData = data;
    createSendForApprovalModal();
    $('#ddlsendto').empty();
    $('#ddlsendRemarks').empty();
    BindSendToDropdown(data.DocType, data.DocNo, data.TableName);
    BindApprovalRemarks();
    let modal = bootstrap.Modal.getOrCreateInstance(
        document.getElementById('SendForapprovedModal')
    );
    modal.show();
};
function BindSendToDropdown(docType, docNo) {

    $.ajax({
        url: '/Approval/DDlSendTo',
        type: 'GET',
        data: {
            v_type: docType,
            v_no: docNo
        },
        success: function (response) {

            let ddl = $('#ddlsendto');

            ddl.empty();

            ddl.append(
                $('<option>')
                    .val('')
                    .text('Select Send To')
            );

            $.each(response, function (i, item) {

                ddl.append(
                    $('<option>')
                        .val(item.value)
                        .text(item.text)
                );
            });
        },
        error: function (xhr) {

            console.log(xhr);

            alert('Unable to load Send To list.');
        }
    });
}

function BindApprovalRemarks() {

    $.ajax({
        url: '/Approval/DDlApprovalRemark',
        type: 'GET',
        success: function (response) {
            var remarks = [];
            $.each(response, function (i, item) {
                remarks.push(item.text);
            });
            if ($("#ddlsendRemarks").data("ui-autocomplete")) {
                $("#ddlsendRemarks").autocomplete("destroy");
            }
            $("#ddlsendRemarks").autocomplete({
                source: remarks,
                minLength: 0
            });
            $("#ddlsendRemarks").off("focus").on("focus", function () {
                $(this).autocomplete("search", "");
            });
        }
    });
}

$(document).on('click', '#btn_Sendapp', function () {
    var sendTo = $('#ddlsendto').val();
    var sendToUserName = $('#ddlsendto').find("option:selected").text().trim();
    //var remarks = $('#ddlsendRemarks option:selected').text();
    var remarks = $('#ddlsendRemarks').val();
    if (!sendTo) {
        toastr.warning('Please select Send To');
        return;
    }
    if (!remarks) {
        toastr.warning('Please select Remarks');
        return;
    }
    var model = {
        DocType: ApprovalData.DocType,
        DocNo: ApprovalData.DocNo,
        SendTo: sendTo,
        SendToUserName: sendToUserName,
        Remarks: remarks,
        tableName: ApprovalData.TableName,
        FromName: ApprovalData.FromName
    };

    $.ajax({
        url: '/Approval/SendForApproval',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (response) {

            if (response.success) {

                toastr.success(response.message);

                setTimeout(function () {

                    bootstrap.Modal
                        .getInstance(document.getElementById('SendForapprovedModal'))
                        ?.hide();

                    location.reload();

                }, 1500);
            }
            else {

                toastr.error(response.message);
            }
        },
        error: function (xhr) {

            console.log(xhr);

            toastr.error('Error while sending approval.');
        },
        error: function (xhr) {
            console.log(xhr);
            alert('Error while sending approval.');
        }
    });

});

///// approval modal popup for approver start Heare
var ApprovalWindowData = {
    DocType: '',
    DocNo: 0,
    TableName: ''
};

function createApprovalModal() {

    if ($('#approvedModal').length > 0)
        return;
    let html = `
    <div class="modal fade" id="approvedModal" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">

                <div class="modal-header">
                    <h5 class="modal-title">Document approved Window</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body">

                    <div class="row">

                        <div class="col-md-4">
                            <label>Approval Status</label>
                            <select id="ddlApprovalStatus" class="form-control"></select>
                        </div>

                        <div class="col-md-4">
                            <label>Forward To</label>
                            <select id="ddlForwardTo" class="form-control">
                                <option value="">Select User</option>
                            </select>
                        </div>

                        <div class="col-md-4">
                            <label>Remarks</label>
                            <input type="text" id="ddlRemarks" class="form-control" />
                        </div>

                    </div>

                </div>

                <div class="modal-footer">

                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                        Close
                    </button>

                    <button type="button" id="btn_Approvedok" class="btn btn-success">
                        Submit Approval
                    </button>

                </div>

            </div>
        </div>
    </div>`;

    $('body').append(html);
}
window.OpenApprovalModal = function (data) {
    ApprovalWindowData = data;
    createApprovalModal();
    $('#ddlApprovalStatus').empty();
    $('#ddlForwardTo').empty();
    $('#ddlRemarks').val('');

    DDlAPPStatus();
    BindForwardTo(data.DocType, data.DocNo);

    let modal = bootstrap.Modal.getOrCreateInstance(
        document.getElementById('approvedModal')
    );

    modal.show();
};

async function DDlAPPStatus() {

    try {
        const res = await fetch('/Approval/DDlAPPStatus');
        const data = await res.json();

        let ddl = $('#ddlApprovalStatus');
        ddl.empty().append('<option value="">Select Status</option>');

        data.forEach(x => {
            ddl.append(`<option value="${x.value}">${x.text}</option>`);
        });

    } catch (e) {
        console.error(e);
    }
}
function BindForwardTo(vType, vNo) {

    $.ajax({
        url: '/Approval/DDlForwordTo',
        type: 'GET',
        data: {
            v_type: vType,
            v_no: vNo
        },
        success: function (res) {

            let ddl = $('#ddlForwardTo');
            ddl.empty();
            ddl.append(`<option value="">Select User</option>`);

            $.each(res, function (i, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });

        }
    });
}

$(document).on('click', '#btn_Approvedok', function () {
    debugger
    var approvalStatus = $('#ddlApprovalStatus').val();

    // Hold
    if (approvalStatus == "4") {
        var modal = bootstrap.Modal.getInstance(
            document.getElementById('approvedModal')
        );

        if (modal) {
            modal.hide();
        }
        return;
    }

    // Forward validation
    if (approvalStatus == "6") {
        var forwardTo = $('#ddlForwardTo').val();
        if (!forwardTo || forwardTo === "") {
            showToast('Please select Forward To user.', {
                type: 'warning'
            });
            $('#ddlForwardTo').focus();
            return;
        }
    }
    SubmitApproval();
});

async function SubmitApproval() {
    try {

        const approvalStatus = parseInt($('#ddlApprovalStatus').val());

        const model = {
            V_TYPE: ApprovalWindowData.DocType,
            V_NO: ApprovalWindowData.DocNo,
            TableName: ApprovalWindowData.TableName,
            ApprovalStatus: approvalStatus,
            ForwardTo: $('#ddlForwardTo').val() || null,
            Remarks: $('#ddlRemarks').val() || null
        };

        console.log("Approval Status:", approvalStatus);
        console.log("Model:", model);

        const res = await $.ajax({
            url: '/Approval/SubmitApproval',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model)
        });

        if (res.success) {
            $('#approvedModal').modal('hide');

            showToast(res.message, {
                type: 'success'
            });
            setTimeout(function () {
                location.reload();
            }, 1000); // 10 seconds
            /*location.reload();*/
        }
        else {
            showToast(res.message, {
                type: 'error'
            });
        }
    }
    catch (ex) {
        console.error(ex);

        showToast('Server Error', {
            type: 'error'
        });
    }
}
///// approval modal popup for approver start Heare
