const $tbody = $('#tblTransportRateQuotation tbody');
const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
const Vtype = "";
let transportData = [];
var globalVars = window.globalVariables || {};
var database = window.database || "";
var LoadGeneralSetting = window.LoadGeneralSetting || {};
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];


$(document).ready(function () {
    SetFYDate('DtDocumentDate', LoginDate);
    LoadDropDown()
        .then(() => {
            if (!rowId)
            {         
                GetVNo();
            }
            else
            {
                LoadFormByID(rowId);
                if (mode = "view")
                {
                    readOnlyForm();
                }
            }
            addRow({}, transportData);
        })
        .catch(error => {
            console.error("Error loading dropdown:", error);
        });

    $(document).on('click', '.btn-add-row, .btn-add-action', function () {
        addRow({}, transportData);
    });

    $("#btn_load").click(async function (e) {
        e.preventDefault();
        await GetTransportData();
    });

    $("#Btn_Whatsapp").click(async function (e) {
        e.preventDefault();
        var mobileNumber = $("#NumMobile").val();
        var phoneRegex = /^\d{10}$/;

        if (phoneRegex.test(mobileNumber)) {
            try {
                await SendMessage(mobileNumber, "Hi, this is Test whatsapp.", "123");
            } catch (error) {
                console.error("Error while sending message: ", error);
                alert("An error occurred while sending the message.");
            }
        } else {
            alert("Please enter a valid mobile number with 10 digits.");
        }
    });

    $(document).on('click', '.btn-delete-action', function () {
        const $row = $(this).closest('tr');
        const isLastRow = $row.is(':last-child');
        $row.remove();
        if (isLastRow) {
            const $lastRow = $tbody.find('tr:last');
            if ($lastRow.length > 0 && $lastRow.find('.btn-add-action').length === 0) {
                $lastRow.find('td:last').prepend(
                    `<i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>`
                );
            }
        }
    });

    $('#ddlDONo').on('select2:select', async function (e) {
        try {
            const doNo = $(this).val();
            const V_NO = $.trim($('#NumDocumentNo').val())
                ? parseFloat($.trim($('#NumDocumentNo').val()))
                : null;

            await fetchDataByDo(doNo, V_NO);
            console.log('Data fetched successfully');
        } catch (error) {
            console.error('Error during fetch:', error);
            alert('An error occurred while fetching data.');
        }
    });

    $("#btn-save").click(function (e) {
        e.preventDefault();

        const DOC_ID = $.trim($('#TxtCode').val()) || null;
        const V_NO = $.trim($('#NumDocumentNo').val()) ? parseFloat($.trim($('#NumDocumentNo').val())) : null;
        const V_DATE = formatDate($("#DtDocumentDate").val()) || null;
        const DO_NO = parseInt($('#ddlDONo').val()) || 0;
        const DO_TYPE = $.trim($('#Vtpye').val()) || "";
        const BILL_CODE = parseInt($('#TxtBillTo').val()) || 0;

        let BILL_NAME = "";
        if (BILL_CODE) {
            BILL_NAME = $('#TxtBillTo option:selected').text().trim() || "";
            BILL_NAME = BILL_NAME.replace(/\s?\(.*\)$/, "").trim() || "";
        }

        const SHIP_ADD1 = $.trim($('#TxtAddressL1').val()) || "";
        const SHIP_ADD2 = $.trim($('#TxtAddressL2').val()) || "";
        const SHIP_ADD3 = $.trim($('#TxtAddressL3').val()) || "";
        const REMARKS = $.trim($('#TxtRemarks').val()) || "";
        const QTY = $.trim($('#NumQuantity').val()) || "";
        const ACTION = !DOC_ID ? 'INSERT' : 'UPDATE';

        if (!V_DATE) {
            toastr.warning("Please select a Voucher Date.");
            $("#DtDocumentDate").focus();
            return;
        }

        if (!V_NO) {
            toastr.warning("Please select a Voucher No.");
            $("#NumDocumentNo").focus();
            return;
        }

        if (!DO_NO) {
            toastr.warning("Please select a DO No.");
            $("#ddlDONo").focus();
            return;
        }
        const payload = {
            Header: {
                DOC_ID: DOC_ID,
                BILL_CODE: BILL_CODE,
                BILL_NAME: BILL_NAME,
                V_NO: V_NO,
                V_DATE: V_DATE,
                DO_NO: DO_NO,
                DO_TYPE: DO_TYPE,
                SHIP_ADD1: SHIP_ADD1,
                SHIP_ADD2: SHIP_ADD2,
                SHIP_ADD3: SHIP_ADD3,
                REMARKS: REMARKS,
                QTY: QTY ? parseFloat(QTY) : 0,
                ACTION: ACTION
            },
            Detail: collectTableData()
        };
        $("#btn-save").prop("disabled", true);
        $.ajax({
            url: '/TransportRateQuotation/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    toastr.success("Saved successfully!");
                    window.location.href = '/TransportRateQuotation/Index?id=' + encodeURIComponent(rowId) + '&readOnly=true'
                } else {
                    toastr.error(response.message || "Save failed.");
                }
            },
            error: function (xhr, status, error) {
                let errorMessage = "Something went wrong.";
                if (xhr.status === 400) {
                    errorMessage = "Bad Request: " + xhr.responseText;
                } else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                } else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }
                console.error("Error: ", errorMessage);
                toastr.error(errorMessage);
            },
            complete: function () {

                $("#btn-save").prop("disabled", false);
            }
        });
    });

});