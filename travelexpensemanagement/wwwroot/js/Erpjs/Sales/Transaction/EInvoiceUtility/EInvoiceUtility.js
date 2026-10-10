var invoiceRows = [];
var selectedInvoice = null;
var ACTION_BTNS = '#button_generate_einvoice, #button_generate_ewaybill, #button_generate_irn, #button_generate_qr, #button_cancel_irn, #chkCancelEinvoice';

let compCode = "";

$(document).ready(async function () {
    $('#ddlDocumentType').focus();
    loadApiMode();
    await WireEvents();
    await BindDropdowns();
});

async function BindDropdowns() {
    await Promise.all([
        bindDropdown("EInvoiceUtility", "doctype", '#ddlDocumentType', '', 'SAGT', null, true, null, false),
    ]);
    $('#ddlDocumentType').trigger('change');
}

async function WireEvents() {
    // Document type changed
    $('#ddlDocumentType').on('change', function () {
        const vType = ($(this).val() || '').trim();
        $('#NumDocNo').val('');
        $('#chkCancelEwaybill, #chkCancelEinvoice').prop('checked', false);
        loadData(vType);
    });

    // Grid checkbox checked
    $('#tblEInvoice tbody').on('change', '.chk-row', function () {
        var $tbody = $('#tblEInvoice tbody');

        if (!this.checked) {
            clearSelection();
            return;
        }

        $tbody.find('.chk-row').not(this).prop('checked', false);
        $tbody.find('tr').removeClass('order-received-row');

        var $tr = $(this).closest('tr');
        $tr.addClass('order-received-row');

        selectInvoice(invoiceRows[$tr.data('index')]);

    });

    // Cancel E-Invoice checkbox 
    $('#chkCancelEinvoice').on('change', function () {
        const checked = $(this).is(':checked');
        if (!checked) $('#txtIrn').val('');
        $('#divCancelEinvoice').toggle(checked);
    });

    // Cancel E-Waybill checkbox 
    $('#chkCancelEwaybill').on('change', function () {
        const checked = $(this).is(':checked');
        if (!checked) $('#txtEwaybillNo').val('');
        $('#divCancelEwaybill').toggle(checked);
    });

    // chkEWb checked
    $('#chkEwb').on('change', function () {
        $('#txtEinvJson').prop('disabled', !$(this).is(':checked'));
    });

    // Signed Einvoice
    $('#button_signed_einvoice').on('click', function () {
        const vType = ($('#ddlDocumentType').val() || '').trim();
        const vNo = parseInt($('#NumDocNo').val(), 10) || 0;

        if (!vNo) {
            showToast('Please select Voucher No. to retrieve Signed EInvoice.', { type: "warning" });
            return;
        }
        GetSignedEInvoiceData(vType, vNo);
    });

    // chkEinv checked
    $('#chkEinv').on('change', function () {
        const checked = $(this).is(':checked');
        $('#txtEinvJson').prop('disabled', !checked);
        $('#button_generate_irn').toggle(checked);
    });

    // Retrieve E-WayBill No clicked
    $('#button_retrieve_ewaybill').on('click', function () {
        const vType = ($('#ddlDocumentType').val() || '').trim();
        const vNo = parseInt($('#NumDocNo').val(), 10) || 0;

        if (!vNo) {
            showToast('Please select Voucher No.', { type: "warning" });
            return;
        }

        GetRetrievedEWayBillNo(vType, vNo);
    });

    // Retrieve IRN No clicked
    $('#button_retrieve_irn').on('click', function () {
        const vType = ($('#ddlDocumentType').val() || '').trim();
        const vNo = parseInt($('#NumDocNo').val(), 10) || 0;

        if (!vNo) {
            showToast('Please select Voucher No.', { type: "warning" });
            return;
        }

        GetRetrievedIRN(vType, vNo);
    });

    // Generate E-Invoice
    $('#button_generate_einvoice').on('click', async function () {
        await generateEInvoice();
    });

    // Authenticate
    $('#button_authentication').on('click', async function () {
        await authenticate();
    });

    // Generate IRN
    $('#button_generate_irn').on('click', async function () {
        await generateIRNFromJson();
    });

    // Save Json
    $('#button_save_json').on('click', function () {
        saveJson();
    });

    // Create JSON
    $('#button_create_json').on('click', async function () {
        await createJsonOnly();
    });

    // Generate QR from Signed JSON
    $('#button_generate_qr').on('click', async function () {
        await createQrFromSignedJson();
    });

    // Create EWB JSON
    $('#button_create_ewb_json').on('click', async function () {
        await createEwbJson();
    });

    // Generate E-WayBill
    $('#button_generate_ewaybill').on('click', async function () {
        await EWayBill();
    });

    // Cancel IRN
    $('#button_cancel_irn').on('click', async function () {
        await cancelIRN();
    });

    // Cancel E-Waybill
    $('#button_cancel_ewaybill').on('click', async function () {
        await cancelEwaybill();
    });

    // Exit Button Click
    document.getElementById("button_exit").addEventListener("click", function () {
        window.location.href = "/Dashboard";
    });
}


// ===============API CREDENTIALS=========================
async function loadApiMode() {
    try {
        const response = await fetch('/EInvoiceUtility/GetApiMode', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json();

        if (!data.success) {
            showToast(data.message || 'Unable to load E-Invoice API configuration.', { type: "error" });
            return;
        }

        compCode = data.compcode;
        $('#lblApiMode').text(data.isLive ? 'Live API/EInvoice' : 'TEST API/Sandbox');

    }
    catch (error) {
        console.error('GetApiMode error:', error);
        alert('Error loading E-Invoice API configuration.');
    }
}


// ================== GET INVOICE DATA ==============
function loadData(vType) {
    $.ajax({
        url: '/EInvoiceUtility/GetEInvoiceList',
        type: 'GET',
        data: { vType: vType },
        dataType: 'json',
        success: function (res) {
            if (res.status) {
                bindTable(res.data);
            } else {
                bindTable([]);
                showToast(res.message, { type: "error" });
            }
        },
        error: function () {
            bindTable([]);
            console.error('Server error while loading invoices.');
            showToast('Error occurred on loading data.', { type: "error" });
        }
    });
}
function bindTable(data) {
    invoiceRows = data || [];

    var $body = $('#tblEInvoice tbody');
    $body.empty();

    if (!data || data.length === 0) {
        $body.append('<tr><td colspan="16" align="center">No invoices found</td></tr>');
        return;
    }

    $.each(invoiceRows, function (i, r) {
        var approved = (r.approvalStatus || '').toLowerCase() === 'approved'
            ? '<span class="erppagestatus-badge erppagestatus-active">Approved</span>'
            : (r.approvalStatus || '');

        var cs = r.cancelStatus || '';
        var status = cs === '' ? ''
            : '<span class="erppagestatus-badge ' +
            (cs.toLowerCase() === 'ok' ? 'erppagestatus-active' : 'erppagestatus-inactive') + '">' + cs + '</span>';

        var rowClass = cs === 'Cancelled' ? ' class="po-generated-row"' : '';

        $body.append(
            '<tr data-index="' + i + '"' + rowClass + '>' +
            '<td align="center" class="freeze-item"><input type="checkbox" class="chk-row"></td>' +
            '<td>' + r.vType + '</td>' +
            '<td>' + r.vNo + '</td>' +
            '<td>' + r.vDate + '</td>' +
            '<td align="right">' + Number(r.netAmount || 0).toFixed(2) + '</td>' +
            '<td>' + (r.billName || '') + '</td>' +
            '<td>' + (r.billGst || '') + '</td>' +
            '<td>' + (r.billPincode || '') + '</td>' +
            '<td>' + (r.shipName || '') + '</td>' +
            '<td>' + (r.shipGst || '') + '</td>' +
            '<td>' + (r.shipPincode || '') + '</td>' +
            '<td>' + approved + '</td>' +
            '<td>' + (r.eInvoiceStatus || '') + '</td>' +
            '<td>' + (r.ewbStatus || '') + '</td>' +
            '<td>' + status + '</td>' +
            '<td>' + (r.tranType || '') + '</td>' +
            '</tr>'
        );
    });
}
function clearSelection() {
    selectedInvoice = null;
    $('#tblEInvoice tbody tr').removeClass('order-received-row');
    $('#tblEInvoice tbody .chk-row').prop('checked', false);

    $('#NumDocNo').val('');

    $('#txtEwbJson, #txtEinvJson, #txtIrn, #txtEwaybillNo').val('');
    $('#txtEinvJson').prop('disabled', true);

    $('#chkEinv, #chkEwb, #cbCancelIRN, #chkWithEwaybill').prop('checked', false);

    $('#lblApproved').text('');
    resetInvoiceState();
}
function selectInvoice(r) {
    selectedInvoice = r;
    $('#NumDocNo').val(r.vNo);
    $('#txtEinvJson, #txtEwbJson').val('');
    $('#icnAuthenticated').css('visibility', 'hidden');
    loadInvoiceStatus(r.vType, r.vNo);
}


// ================== GET INVOICE STATUS ==============
function loadInvoiceStatus(vType, vNo) {
    resetInvoiceState();
    if (!vType || !vNo) return;

    $.ajax({
        url: '/EInvoiceUtility/GetInvoiceStatus',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: function (res) {
            if (res.status) {
                applyInvoiceStatus(res.data);
            } else {
                showToast(res.message, { type: "error" });
            }
        },
        error: function () {
            showToast('Error occurred while loading invoice status.', { type: "error" });
        }
    });
}
function setButtons(selector, enabled) {
    $(selector)
        .prop('disabled', !enabled)
        .css('pointer-events', enabled ? '' : 'none');
}

function resetInvoiceState() {
    $('#lblEinvGenerated, #lblEwbGenerated, #lblEinvCancelled').hide();
    setButtons(ACTION_BTNS, true);
}

function applyInvoiceStatus(d) {
    $('#lblApproved').text(d.approvalStatus).show();

    // STATUS = 2 -> everything disabled
    if (d.isCancelled) {
        $('#lblEinvCancelled').show();
        setButtons(ACTION_BTNS, false);
        return;
    }

    // IRN exists -> E-Invoice and Gen IRN disabled
    if (d.irn) {
        $('#lblEinvGenerated').show();
        setButtons('#button_generate_einvoice, #button_generate_irn', false);

        // E-Waybill exists -> E-Waybill button disabled
        if (d.ewaybillNo) {
            $('#lblEwbGenerated').show();
            setButtons('#button_generate_ewaybill', false);
        }
    }
}


// =================== GET SIGNED E_Invoice ============
function GetSignedEInvoiceData(vType, vNo) {
    $.ajax({
        url: '/EInvoiceUtility/GetSignedJson',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: function (res) {
            if (res.status) {
                $('#txtEwbJson').val(res.data);
            } else {
                showToast(res.message, { type: "error" });
            }
        },
        error: function () {
            showToast('Error occurred while retrieving Signed E-Invoice.', { type: "error" });
        }
    });
}


// =================== GET E-WayBill No ============
function GetRetrievedEWayBillNo(vType, vNo) {
    $.ajax({
        url: '/EInvoiceUtility/GetEWayBillNo',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: function (res) {
            if (!res.status) {
                showToast(res.message, { type: "error" });
            } else if (!res.data) {
                showToast('E-WayBill No not generated.', { type: "info" });
            } else {
                $('#txtEwaybillNo').val(res.data);
            }
        },
        error: function () {
            showToast('Error occurred while retrieving E-WayBill No.', { type: "error" });
        }
    });
}


// =================== GET IRN ============
function GetRetrievedIRN(vType, vNo) {
    $.ajax({
        url: '/EInvoiceUtility/GetIRN',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: function (res) {
            if (!res.status) {
                showToast(res.message, { type: "error" });
            } else if (!res.data) {
                showToast('IRN not generated.', { type: "info" });
            } else {
                $('#txtIrn').val(res.data);
            }
        },
        error: function () {
            showToast('Error occurred while retrieving IRN.', { type: "error" });
        }
    });
}


// =================== Authencication =================
async function authenticate() {
    try {
        const response = await $.ajax({
            url: '/EInvoiceUtility/Authenticate',
            type: 'POST'
        });

        if (response.success) {
            $('#icnAuthenticated').css('visibility', 'visible');
        } else {
            $('#icnAuthenticated').css('visibility', 'hidden');
        }
    }
    catch (error) {
        $('#icnAuthenticated').css('visibility', 'hidden');
        console.error(error);
    }
}


//=================== GENERATE E-INVOICE =================
async function generateEInvoice() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();
    var approval = $("#lblApproved").text().trim();
    try {

        var btn = $("#button_generate_einvoice");
        var keepDisabled = false;
        btn.prop("disabled", true);

        var mode = await $.ajax({ url: "/EInvoiceUtility/GetApiMode", type: "GET" });

        if (mode.success && mode.isLive && mode.datasource === "192.168.1.217" && mode.username.substring(0, 3).toUpperCase() === "API") {
            var confirmResult = await Swal.fire({
                title: "Live API",
                text: "Live API. Do you want to continue?",
                icon: "question",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "No",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            if (!confirmResult.isConfirmed) return;
        }

        if (!vNo) {
            setInvalid($("#NumDocNo"), "Please select Voucher No. to generate EInvoice.");
            return;
        }

        if (approval.toLowerCase() !== "approved") {
            setInvalid($("#NumDocNo"), "Voucher Not Approved.");
            return;
        }

        var response = await $.ajax({
            url: "/EInvoiceUtility/EInvoice",
            type: "POST",
            data: {
                vNo: vNo,
                vType: vType,
                cessNAValue: $("#chkCessNAddVal").is(":checked")
            }
        });

        console.log(response);

        if (response.requestJson) {
            $('#icnJSONCreated').css('visibility', 'visible');
            $("#txtEinvJson").val(response.requestJson);
            if ($("#chkAutoSaveJson").is(":checked")) {
                downloadTextFile(compCode + vNo + ".json", response.requestJson);
            }
        }
        else {
            $('#icnJSONCreated').css('visibility', 'hidden');
        };


        if (response.authenticated) {
            $('#icnAuthenticated').css('visibility', 'visible');
        } else {
            $('#icnAuthenticated').css('visibility', 'hidden');
        }

        if (!response.success) {
            showToast(response.message, { type: "error" });

            if (response.irnGenerated) {
                if (response.signedJson) {
                    $('#icnIRNGenerated').css('visibility', 'visible');
                    $("#txtEwbJson").val(response.signedJson);
                }
                else {
                    $('#icnIRNGenerated').css('visibility', 'hidden');
                }
                loadData(vType);
                loadInvoiceStatus(vType, vNo);
            }
            return;
        }

        if (response.qrGenerated) {
            $('#icnQRCode').css('visibility', 'visible');
        }
        else {
            $('#icnQRCode').css('visibility', 'hidden');
        }

        showToast(response.message, { type: "success" });
        keepDisabled = true;

        if (response.signedJson) $("#txtEwbJson").val(response.signedJson);

        loadData(vType);
        loadInvoiceStatus(vType, vNo);
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while generating E-Invoice.", { type: "error" });
    }
    finally {
        if (!keepDisabled) btn.prop("disabled", false);
    }
}


//=================== GENERATE IRN FROM EDITED JSON =================
async function generateIRNFromJson() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();
    var approval = $("#lblApproved").text().trim();
    var jsonText = ($("#txtEinvJson").val() || "").trim();


    var btn = $("#button_generate_irn");
    var keepDisabled = false;
    btn.prop("disabled", true);

    try {
        // "Live API. Do you want to continue?"
        var mode = await $.ajax({ url: "/EInvoiceUtility/GetApiMode", type: "GET" });

        if (mode.success && mode.isLive && mode.datasource === "192.168.1.217" && mode.username.substring(0, 3).toUpperCase() === "API") {
            var confirmResult = await Swal.fire({
                title: "Live API",
                text: "Live API. Do you want to continue?",
                icon: "question",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "No",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            if (!confirmResult.isConfirmed) return;
        }

        if (!vNo) {
            setInvalid($("#NumDocNo"), "Please select Voucher No. to generate EInvoice.");
            return;
        }
        if (approval.toLowerCase() !== "approved") {
            setInvalid($("#NumDocNo"), "Voucher Not Approved.");
            return;
        }
        if (!jsonText) {
            showToast("JSON Text Empty.", { type: "error" });
            return;
        }

        var response = await $.ajax({
            url: "/EInvoiceUtility/GenerateIRNFromJson",
            type: "POST",
            data: { vNo: vNo, vType: vType, jsonText: jsonText }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });

            if (response.irnGenerated) {
                if (response.signedJson) {
                    $('#icnIRNGenerated').css('visibility', 'visible');
                    $("#txtEwbJson").val(response.signedJson);
                }
                else {
                    $('#icnIRNGenerated').css('visibility', 'hidden');
                }
                loadData(vType);
                loadInvoiceStatus(vType, vNo);
            }
            return;
        }

        if (response.qrGenerated) {
            $('#icnQRCode').css('visibility', 'visible');
        }
        else {
            $('#icnQRCode').css('visibility', 'hidden');
        }

        showToast(response.message, { type: "success" });
        keepDisabled = true;

        if (response.signedJson) $("#txtEwbJson").val(response.signedJson);

        loadData(vType);
        loadInvoiceStatus(vType, vNo);
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while generating IRN.", { type: "error" });
    }
    finally {
        if (!keepDisabled) btn.prop("disabled", false);
    }
}

function downloadTextFile(fileName, text) {
    var blob = new Blob([text], { type: "application/json;charset=utf-8" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
}
function saveJson() {
    var vNo = $("#NumDocNo").val();
    var jsonText = ($("#txtEinvJson").val() || "").trim();

    if (!jsonText) {
        showToast("JSON text is empty.", { type: "error" });
        return;
    }

    var fileName = compCode + vNo + ".json";
    downloadTextFile(fileName, jsonText);
    showToast("Json saved as " + fileName, { type: "success" });
}

//=================== CREATE JSON ONLY =================
async function createJsonOnly() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();

    if (!vNo) {
        setInvalid($("#NumDocNo"), "Voucher No. not selected.");
        return;
    }

    var btn = $("#button_create_json");
    btn.prop("disabled", true);
    $('#icnJSONCreated').css('visibility', 'hidden');

    try {
        var response = await $.ajax({
            url: "/EInvoiceUtility/CreateJsonOnly",
            type: "POST",
            data: {
                vNo: vNo,
                vType: vType,
                cessNAValue: $("#chkCessNAddVal").is(":checked")
            }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });
            return;
        }

        $("#txtEinvJson").val(response.requestJson);
        $('#icnJSONCreated').css('visibility', 'visible');

        if ($("#chkAutoSaveJson").is(":checked")) {
            downloadTextFile(compCode + vNo + ".json", response.requestJson);
        }

        showToast(response.message, { type: "success" });
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while creating JSON.", { type: "error" });
    }
    finally {
        btn.prop("disabled", false);
    }
}


//=================== CREATE QR FROM SIGNED JSON =================
async function createQrFromSignedJson() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();
    var signedJson = ($("#txtEwbJson").val() || "").trim();

    //Saved QR Image/IRN flushed if any, Do you want to create QR image from Signed JSON?
    var confirmResult = await Swal.fire({
        title: "Create QR Image",
        text: "Saved QR Image/IRN flushed if any, Do you want to create QR image from Signed JSON?",
        icon: "question",
        showCancelButton: true,
        confirmButtonText: "Yes",
        cancelButtonText: "No",
        allowOutsideClick: false,
        allowEscapeKey: false
    });
    if (!confirmResult.isConfirmed) return;

    if (!vNo) {
        setInvalid($("#NumDocNo"), "Please select Voucher No. for which QR Code need to be generate.");
        return;
    }
    if (!signedJson) {
        setInvalid($("#txtEwbJson"), "Signed JSON is empty.");
        return;
    }

    var btn = $("#button_generate_qr");
    btn.prop("disabled", true);
    $('#icnQRCode').css('visibility', 'hidden');

    try {
        var response = await $.ajax({
            url: "/EInvoiceUtility/CreateQrFromSignedJson",
            type: "POST",
            data: { vNo: vNo, vType: vType, signedJson: signedJson }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });
            return;
        }

        $('#icnQRCode').css('visibility', 'visible');
        showToast(response.message, { type: "success" });

        loadData(vType);
        loadInvoiceStatus(vType, vNo);
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while creating QR image.", { type: "error" });
    }
    finally {
        btn.prop("disabled", false);
    }
}


//=================== CREATE EWB JSON =================
async function createEwbJson() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();

    if (!$("#chkWithEwaybill").is(":checked")) {
        setInvalid($("#chkWithEwaybill"), "Please select EWaybill option.");
        return;
    }

    if (!vNo) {
        setInvalid($("#NumDocNo"), "Please select Voucher No.");
        return;
    }

    var btn = $("#button_create_ewb_json");
    btn.prop("disabled", true);

    try {
        var response = await $.ajax({
            url: "/EInvoiceUtility/CreateEwbJsonOnly",
            type: "POST",
            data: { vNo: vNo, vType: vType }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });
            return;
        }

        $("#txtEinvJson").val(response.requestJson);
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while creating E-Waybill JSON.", { type: "error" });
    }
    finally {
        btn.prop("disabled", false);
    }
}

//=================== GENERATE E-WAYBILL =================
function EWayBill(takeConfirmation = false) {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();
    var jsonText = $("#txtEinvJson").val();
    var cbEwayBill = $("#chkWithEwaybill").is(":checked");
    var useEwbJson = $("#chkEwb").is(":checked");
   
    $.ajax({
        url: '/EInvoiceUtility/EWayBill',
        type: 'POST',
        data: {
            vNo: vNo,
            vType: vType,
            useEwbJson: useEwbJson,
            jsonText: jsonText,
            cbEwayBill: cbEwayBill,
            takeConfirmation: takeConfirmation
        },
        success: function (res) {
            console.log("E-WayBill Response: ", res);
            if (!res.success) {
                if (res.continueEwaybillQuestion) {
                    Swal.fire({
                        title: 'Confirmation',
                        text: res.message,
                        icon: 'question',
                        showCancelButton: true,
                        confirmButtonText: 'Yes',
                        cancelButtonText: 'No'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            EWayBill(true);
                        } else {
                            return;
                        }
                    });
                }
                else {
                    showToast(res.message, { type: "warning" })
                }
                return;
            }
            if (res.ewbGenerated) {
                $('#lblEwbGenerated').show();
                $('#statusEwaybill').css('visibility', 'visible');
            }
            else {
                $('#lblEwbGenerated').hide();
                $('#statusEwaybill').css('visibility', 'hidden');
            }

            if (res.ewaybillNo) {
                $('#txtEinvJson').val(res.requestJson);
                $('#txtEwbJson').val(res.signedJson);
            }
            else {
                $('#txtEinvJson').val('');
                $('#txtEwbJson').val('');
            }

            loadData(vType);
            loadInvoiceStatus(vType, vNo);
        },
        error: function (xhr) {
            alert(xhr.responseText || 'Error while processing E-Way Bill.');
        }
    });
}

//=================== CANCEL IRN =================
async function cancelIRN() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();
    var irn = ($("#txtIrn").val() || "").trim();

    if (!vNo) {
        setInvalid($("#NumDocNo"), "Please select Voucher No. for which IRN to be cancel.");
        return;
    }

    var btn = $("#button_cancel_irn");
    var keepDisabled = false;
    btn.prop("disabled", true);

    try {
        var payload = { vNo: vNo, vType: vType, irn: irn };

        var response = await $.ajax({
            url: "/EInvoiceUtility/CancelIRN",
            type: "POST",
            data: Object.assign({ confirmed: false }, payload)
        });

        // VB: "Do you want to cancel IRN of this Invoice ?"
        if (response.needConfirm) {
            var c = await Swal.fire({
                title: "Cancel IRN",
                text: response.message,
                icon: "question",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "No",
                allowOutsideClick: false,
                allowEscapeKey: false
            });

            if (!c.isConfirmed) return;

            response = await $.ajax({
                url: "/EInvoiceUtility/CancelIRN",
                type: "POST",
                data: Object.assign({ confirmed: true }, payload)
            });
        }

        if (!response.success) {
            showToast(response.message, { type: "error" });

            if (response.irnCancelled) {
                loadData(vType);
                loadInvoiceStatus(vType, vNo);
            }
            return;
        }

        showToast(response.message, { type: "success" });
        keepDisabled = true;

        $('#chkCancelEinvoice').prop('checked', false).trigger('change');

        loadData(vType);
        loadInvoiceStatus(vType, vNo);
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while cancelling IRN.", { type: "error" });
    }
    finally {
        if (!keepDisabled) btn.prop("disabled", false);
    }
}


//=================== CANCEL E-WAYBILL =================
async function cancelEwaybill() {
    var vNo = $("#NumDocNo").val();
    var vType = $("#ddlDocumentType").val();
    var ewbNo = ($("#txtEwaybillNo").val() || "").trim();

    if (!ewbNo) {
        setInvalid($("#txtEwaybillNo"), "Please enter Ewaybill No.");
        return;
    }

    var btn = $("#button_cancel_ewaybill");
    var keepDisabled = false;
    btn.prop("disabled", true);

    try {
        
        // Do you want to cancel Ewaybill?
        var c = await Swal.fire({
            title: "Cancel E-Waybill",
            text: "Do you want to cancel Ewaybill?",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes",
            cancelButtonText: "No",
            allowOutsideClick: false,
            allowEscapeKey: false
        });

        if (!c.isConfirmed) return;

        var response = await $.ajax({
            url: "/EInvoiceUtility/CancelEwayBill",
            type: "POST",
            data: { vNo: vNo || 0, vType: vType, ewbNo: ewbNo}
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });

            if (response.ewbCancelled && vNo) {
                loadData(vType);
                loadInvoiceStatus(vType, vNo);
            }
            return;
        }

        showToast(response.message, { type: "success" });
        keepDisabled = true;

        $('#chkCancelEwaybill').prop('checked', false).trigger('change');

        if (vNo) {
            loadData(vType);
            loadInvoiceStatus(vType, vNo);
        }
    }
    catch (xhr) {
        showToast(xhr?.responseJSON?.message || "Error while cancelling E-Waybill.", { type: "error" });
    }
    finally {
        if (!keepDisabled) btn.prop("disabled", false);
    }
}