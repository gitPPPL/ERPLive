
const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
var globalVars = window.globalVariables || {};
var database = window.database || "";
var LoadGeneralSetting = window.LoadGeneralSetting || {};
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];

$(document).ready(async function ()
{
    SetFYDate('DtDocumentDate', LoginDate);
    await LoadDropdown();

    if (rowId)
    {
        LoadData();
        const v_type = $('#DDlDocumentType').val();
        checkApprovalStatus(v_type, rowId, 'EINVOICE_CANAPP');

        if (mode == "view")
        {
            SetFormReadOnly();
        }
    }
    else
    {
        const v_type = $('#DDlDocumentType').val();
        GetVNo(v_type);
        AddRow(); 
    }
    
    $('#btn_LoadData').on('click', async function ()
    {
        await Getdata();
    });

    $("#btn-save").click(async function (e) {
        e.preventDefault();

        if (!validateRequiredField('#DDlDocumentType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#NumDocumentNo', 'Please Fill Voucher No')) return;
        if (!validateRequiredField('#DtDocumentDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#DDlEInvoiceDocument', 'Please select a EInvoice Document.')) return;         

        const Details = CollectTableData();

        if (!ValidateRows()) {

            console.log("ValidateRows", ValidateRows);

            return;
        }


        const model = {
            Details: Details
        };

        console.log("FINAL MODEL:");
        console.log(JSON.stringify(model, null, 2));
        $("#btn-save").prop("disabled", true);
        $.ajax({
            url: '/EInvoiceCancellationApproval/SavedData',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify(model),
            success: function (response) {
                console.log("Response:", response);
                if (response.success === true)
                {

                    const v_type = $('#DDlDocumentType').val();
                    const v_no = $('#NumDocumentNo').val();

                    const DOC_ID = v_type + v_no;

                    toastr.success("Saved successfully!");
                    setTimeout(function () { window.location.href = `/EInvoiceCancellationApproval/Index?id=${DOC_ID}&mode=view`; }, 1000);
                }
                else if (response.success === false || validation == true)
                {
                    toastr.warning(response.message );

                }
                else if (response.success === false)
                {
                    toastr.error(response.message || "Save failed.");

                }
    
            },

            error: function (xhr) {

                console.log("HTTP Status:", xhr.status);
                console.log("Response:", xhr.responseText);

                let errorMessage = "Something went wrong.";

                if (xhr.status === 400) {
                    errorMessage = "Bad Request: " + xhr.responseText;
                }
                else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                }
                else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }

                toastr.error("Error: " + errorMessage);
            },

            complete: function () {
                $("#btn-save").prop("disabled", false);
            }
        });
    });

    $(document).off('change', '.chk_Select').on('change', '.chk_Select', function () {
        const row = $(this).closest('tr');
        if ($(this).is(':checked'))
        {
            row.find('.TxtStatus').val('Close');
        }
        else
        {
            row.find('.TxtStatus').val('Open');
        }

    });

    //kks
    $(document).on('click', '#btn_Sendapproval', function () {
        var FromName = window.location.pathname.split('/')[1];
        let vNo = $('#NumDocumentNo').val();
        let vtype = $('#DDlDocumentType').val();

        $.ajax({
            url: '/Approval/CheckPendingUser',
            type: 'POST',
            data: { vNo: vNo, vType: vtype },
            success: function (response) {
                console.log('Response:', response);
                // Pending with another user
                if (response.success === false)
                {
                    showToast(`Pending With Another User (${response.userCode})`, { type: "warning" });
                    return;
                }
                // Approval_Code = 5
                if (response.approvalCode8 === true)
                {
                    OpenApprovalModal({ DocType: vtype, DocNo: vNo, TableName: 'EINVOICE_CANAPP' });
                    return;
                }
                // Approval_Code != 8
                OpenSendForApprovalModal({ DocType: vtype, DocNo: vNo, UserCode: null, UserName: null,
                    DocDate: null, TableName: 'EINVOICE_CANAPP', FromName, FromName
                });

            },
            error: function (xhr, status, error) {
                console.log(error);
                alert('Error while checking approval status.');
            }
        });

    });

    $(document).on('click', '#btn_Approved', function ()
    {
        let vNo = $('#NumDocumentNo').val();
        let vtype = $('#DDlDocumentType').val();

        OpenApprovalModal({ DocType: vtype, DocNo: vNo, TableName: 'EINVOICE_CANAPP' });
    });
    //kks

});