let rowDropdownData = {
    itemTypes: [],
    locations: [],
    vehicleTypes: []
};

const urlParams = new URLSearchParams(window.location.search);
const vNo = urlParams.get('id');
const vType = urlParams.get('vtype');
const isReadOnly = urlParams.get('readOnly') === "true";

let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
let compPhone = "";
let compGSTIN = "";
let compPAN = "";
let compEmail = "";

var controllerName = window.location.pathname.split('/')[1];

$(document).ready(async function () {
    checkPermissionForEntryPage(controllerName);
    getGlobalValues();
    await wireEvents();

    try {
        await Promise.all([
            BindDropdowns(),
            LoadRowDropdowns(),
            LoadTransporters()
        ]);

        if (vNo && !isNaN(vNo)) {
            LoadExistingRFQ(vType, vNo);
        }
        else {
            AddTransportRow();
        }

    } catch (e) {
        showToast('Error loading dropdowns: ' + e.message, { type: "warning" });
    }

    $("#DtDocumentDate").val(getCurrentDateYMD());

    ToggleSendRequest(isReadOnly);
});

//=============DROPDOWNS=================
async function BindDropdowns() {
    await Promise.all([
        bindDropdown("TransportRateRequestForQuotation", "doctype", '#ddlDocumentType', '', 'TRFQ', null, true, null, false),
        bindDropdown("TransportRateRequestForQuotation", "refno", '#ddlRefNo', ' Select Ref No ', null, null, false, null, true)
    ]);
    $('#ddlDocumentType').trigger('change');
}
function setDropdown(selector, value) {
    $(selector).val(value).trigger('change');
}

// Row dropdowns
async function LoadRowDropdowns() {
    const [itemTypes, locations, vehicleTypes] = await Promise.all([
        GetDropdownOptions("itemtype"),
        GetDropdownOptions("location"),
        GetDropdownOptions("vehicletype")
    ]);

    rowDropdownData.itemTypes = itemTypes;
    rowDropdownData.locations = locations;
    rowDropdownData.vehicleTypes = vehicleTypes;
}
async function GetDropdownOptions(type) {
    const response = await fetch(
        `/TransportRateRequestForQuotation/GetDropdown?type=${encodeURIComponent(type)}`
    );

    if (!response.ok) {
        throw new Error(`Failed to load ${type} dropdown`);
    }

    const result = await response.json();

    const data = Array.isArray(result)
        ? result
        : result.data ?? result.Data ?? result.result ?? result.Result ?? [];

    return data.map(item => ({
        value: String(item.value ?? item.VALUE ?? item.code ?? item.CODE ?? ""),
        text: String(item.text ?? item.TEXT ?? item.name ?? item.NAME ?? "")
    }));
}
function FillDropdown(dropdown, data, selectedValue = "") {
    dropdown.empty();
    dropdown.append(new Option("Select", ""));

    data.forEach(item => {
        dropdown.append(new Option(item.text, item.value));
    });

    dropdown.val(String(selectedValue ?? ""));
}
function initSelect2($ddl) {
    if ($ddl.hasClass("select2-hidden-accessible")) return;

    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true,
        width: '100%'
    });

    $ddl.on('select2:open', function () {
        setTimeout(function () {
            let searchBox = document.querySelector(
                '.select2-container--open .select2-search__field'
            );
            if (searchBox) searchBox.focus();
        }, 0);
    });
}


//=============GENERATE VNO=================
async function GetVNo(vtype) {
    try {
        const res = await fetch(`/TransportRateRequestForQuotation/GetVNo/?vType=${encodeURIComponent(vtype)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocumentNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}


//=============EVENTS=================
async function wireEvents() {
    // DocType Change
    $("#ddlDocumentType").on('change', async function () {
        const selectedType = $(this).val();
        if (!selectedType || vNo) return;
        
        try {
            await GetVNo(selectedType);
        } catch (err) {
            console.error("Error loading Document No:", err);
        }
    });

    // Import Dispatch Planning
    $("#btn-Import").on('click', async function () {
        await ImportDispatch();
    });

    // Add a blank row
    $("#tblTransportRequest").on("click", ".add-row", function () {
        const $currentRow = $(this).closest("tr");
        $currentRow.find(".add-row").remove();

        AddTransportRow();
    });

    // Delete row
    $("#tblTransportRequest").on("click", ".del-row", function () {
        const $tbody = $('#tblTransportRequest tbody');

        if ($tbody.find('tr').length === 1) return;

        $(this).closest('tr').remove();

        // Remove Add button from all rows
        $tbody.find('.add-row').remove();

        // Add Add button only to the last row
        $tbody.find('tr:last .action-wrap').append(`
            <button type="button" class="act-btn add add-row" title="Add Row">
                <i class="fa fa-plus-circle"></i>
            </button>
        `);

        UpdateSerialNumbers();
    });

    // Select all / sync header checkbox in transporter list
    $("#chkAllTransporter").on("change", function () {
        $("#tblTransporter tbody .chk-transporter").prop("checked", this.checked);
    });
    $("#tblTransporter").on("change", ".chk-transporter", function () {
        const total = $("#tblTransporter tbody .chk-transporter").length;
        const checked = $("#tblTransporter tbody .chk-transporter:checked").length;
        $("#chkAllTransporter").prop("checked", total > 0 && total === checked);
    });

    // Save
    $("#btn-save").on("click", async function (e) {
        e.preventDefault();

        await SaveRequest();
    });

    // Mail
    $('#btn-SendEmail').on('click', sendTransportEmail);

    // WhatsApp
    $('#btn-SendWhatsApp').on('click', sendTransportWhatsApp);

    // City
    $("#tblTransportRequest").on("change", ".location-input", async function () {
        if ($(this).data("skip-pincode-change")) return;

        const $row = $(this).closest("tr");
        const cityCode = $(this).val() || 0;

        $row.attr("data-city-code", cityCode);
        $row.find(".pincode-input").val("");

        if (!cityCode) return;

        const pinCode = await GetCityPincode(cityCode);
        $row.find(".pincode-input").val(pinCode);
    });
}


//=============IMPORT DISPATCH LIST=================
async function ImportDispatch() {
    const refNo = $("#ddlRefNo").val();

    if (!refNo) {
        showToast("Please select Ref No.", { type: "warning" });
        return;
    }

    try {
        $("#btn-Import").prop("disabled", true);

        const response = await fetch(
            `/TransportRateRequestForQuotation/ImportDispatch?refNo=${encodeURIComponent(refNo)}`
        );

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const result = await response.json();

        if (result.status === false || result.Status === false) {
            throw new Error(result.message ?? result.Message ?? "Unable to import dispatch data.");
        }

        const data = Array.isArray(result)
            ? result
            : result.data ?? result.Data ?? result.result ?? result.Result ?? [];

        if (!data.length) {
            showToast("No records found for selected Ref No.", { type: "warning" });
            return;
        }

        const tbody = $("#tblTransportRequest tbody");
        tbody.empty();

        data.forEach(item => AddTransportRow(item));

        showToast("Dispatch data imported successfully.", { type: "success" });

    } catch (e) {
        showToast("Error importing dispatch data: " + e.message, { type: "warning" });
    } finally {
        $("#btn-Import").prop("disabled", false);
    }
}


//=============CREATE ROW=================
function AddTransportRow(item = {}) {
    const tbody = $("#tblTransportRequest tbody");

    const row = $(`
        <tr class="no-border-input" data-party-code="0" data-city-code="0" data-ref-no="">
            <td class="sno"></td>
            <td><input type="number" class="form-control weight-input" min="0" value=""></td>
            <td><select class="form-control item-type-input"></select></td>
            <td><select class="form-control location-input"></select></td>
            <td><input type="text" class="form-control pincode-input" maxlength="10"></td>
            <td><select class="form-control vehicle-type-input"></select></td>
            <td><input type="text" class="form-control remarks-input" maxlength="500"></td>
            <td class="action-col">
                <div class="action-wrap">
                    <button type="button" class="act-btn delete del-row" title="Delete">
                        <i class="fa fa-trash"></i>
                    </button>
                    <button type="button" class="act-btn add add-row" title="Add Row">
                        <i class="fa fa-plus-circle"></i>
                    </button>
                </div>
            </td>
        </tr>
    `);

    row.attr("data-party-code", item.partyCode ?? 0);
    row.attr("data-city-code", item.cityCode ?? 0);
    row.attr("data-ref-no", item.refNo ?? "");

    row.find(".weight-input").val(item.totalQty ?? "");
    row.find(".pincode-input").val(item.pinCode ?? "");
    row.find(".remarks-input").val(item.remarks ?? "");

    FillDropdown(row.find(".item-type-input"), rowDropdownData.itemTypes, item.saleGroup);
    FillDropdown(row.find(".location-input"), rowDropdownData.locations, item.cityCode);
    FillDropdown(row.find(".vehicle-type-input"), rowDropdownData.vehicleTypes, item.vehicleType);

    tbody.append(row);

    initSelect2(row.find('.item-type-input'));
    initSelect2(row.find('.location-input'));
    initSelect2(row.find('.vehicle-type-input'));

    UpdateSerialNumbers();
}

// Update serial numbers
function UpdateSerialNumbers() {
    $("#tblTransportRequest tbody tr").each(function (index) {
        $(this).find(".sno").text(index + 1);
    });
}


//=============TRANSPORTERS=================
async function LoadTransporters() {
    const data = await GetDropdownOptions("transporter");
    const tbody = $("#tblTransporter tbody").empty();

    data.forEach(item => {
        tbody.append(`
            <tr data-code="${item.value}" data-name="${$('<div>').text(item.text).html()}">
                <td><input type="checkbox" class="erppage-checkbox-input chk-transporter"></td>
                <td>${$('<div>').text(item.text).html()}</td>
            </tr>`);
    });
}
function GetSelectedTransporters() {
    const list = [];
    $("#tblTransporter tbody tr").each(function () {
        if ($(this).find(".chk-transporter").is(":checked")) {
            list.push({
                Code: parseInt($(this).data("code")),
                Name: $(this).data("name")
            });
        }
    });
    return list;  
}
function SetSelectedTransporters(list) {
    const codes = (list || []).map(x => String(x.Code ?? x.code));
    $("#tblTransporter tbody tr").each(function () {
        $(this).find(".chk-transporter").prop("checked", codes.includes(String($(this).data("code"))));
    });
}


//=============VALIDATION=================
function ValidateData() {
    // Header
    if (!validateRequiredField('#ddlDocumentType', 'Document Type') || !validateRequiredField('#NumDocumentNo', 'Document No')) return false;

    // Rows
    const rows = $("#tblTransportRequest tbody tr");
    if (rows.length === 0) {
        showToast("No Records in row.", { type: "warning" });
        return false;
    }

    const headerRefNo = String($("#ddlRefNo").val() ?? "");
    let isValid = true;

    rows.each(function (index) {
        const $row = $(this);
        const rowNo = index + 1;

        const $weight = $row.find(".weight-input");
        const $itemType = $row.find(".item-type-input");
        const $location = $row.find(".location-input");
        const $vehicle = $row.find(".vehicle-type-input");

        if ((parseFloat($weight.val()) || 0) <= 0) {
            setInvalid($weight, `Weight should not be blank or zero.`);
            isValid = false;
            return false;
        }

        if (!$location.val()) {
            setInvalid($location, `Delivery city name should not be blank.`);
            isValid = false;
            return false;
        }

        if (!$itemType.val()) {
            setInvalid($itemType, `Item type should not be blank.`);
            isValid = false;
            return false;
        }

        if (!$vehicle.val()) {
            setInvalid($vehicle, `Vehicle should not be blank.`);
            isValid = false;
            return false;
        }

        const rowRefNo = String($row.attr("data-ref-no") ?? "");
        if (rowRefNo !== "" && rowRefNo !== headerRefNo) {
            setInvalid($("#ddlRefNo"), `Row ${rowNo}: Ref no does not match with the imported rows.`);
            isValid = false;
            return false;
        }
    });

    return isValid;
}


//=============PAYLOAD=================
function BuildPayload() {
    const headerRefNo = parseInt($("#ddlRefNo").val()) || null;

    return {
        vType: $("#ddlDocumentType").val(),
        vNo: parseInt($("#NumDocumentNo").val()),
        vDate: $("#DtDocumentDate").val(),
        refType: headerRefNo ? "DISP" : null,
        refNo: headerRefNo,
        transportName: JSON.stringify(GetSelectedTransporters()),
        isEdit: (vNo && !isNaN(vNo) && vType) ? true : false,
        rows: $("#tblTransportRequest tbody tr").map(function () {
            const $r = $(this);
            const rowRefNo = parseInt($r.attr("data-ref-no")) || null;
            return {
                weight: parseFloat($r.find(".weight-input").val()) || 0,
                itemType: $r.find(".item-type-input").val() || "",
                delCityCode: parseInt($r.find(".location-input").val()) || 0,
                vehicleType: $r.find(".vehicle-type-input").val() || "",
                deliveryPincode: $r.find(".pincode-input").val() || "",
                remarks: $r.find(".remarks-input").val() || "",
                partyCode: parseInt($r.attr("data-party-code")) || null,
                refType: rowRefNo ? "DISP" : null,
                refNo: rowRefNo
            };
        }).get()
    };
}


//=============SAVE=================
async function SaveRequest() {
    if (!ValidateData()) return;

    if (GetSelectedTransporters().length === 0) {
        showToast("Please select at least one transporter.", { type: "warning" });
        return;
    }

    const payload = BuildPayload();

    try {
        $("#btn-save").prop("disabled", true);

        const response = await fetch("/TransportRateRequestForQuotation/Save", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const result = await response.json();
        if (result.status === false) throw new Error(result.message ?? "Unable to save.");

        showToast(result.message ?? "Saved successfully.", { type: "success" });
    } catch (e) {
        showToast("Error saving: " + e.message, { type: "warning" });
    } finally {
        $("#btn-save").prop("disabled", false);
    }
}


//=============LOAD EXISTING RFQ=================
async function LoadExistingRFQ(vType, vNo) {
    if (!vType || !vNo) {
        showToast("Invalid document details.", { type: "warning" });
        return;
    }

    try {
        const response = await fetch(
            `/TransportRateRequestForQuotation/GetById?vType=${encodeURIComponent(vType)}&vNo=${encodeURIComponent(vNo)}`
        );

        if (!response.ok)
            throw new Error(`HTTP ${response.status}`);

        const result = await response.json();

        if (!result.status)
            throw new Error(result.message || "Unable to load RFQ.");

        if (!result.data)
            throw new Error("RFQ data not found.");

        // Bind retrieved data into controls
        BindExistingRFQ(result.data);

    } catch (e) {
        showToast("Error loading RFQ: " + e.message, { type: "warning" });
    }
}
function BindExistingRFQ(data) {

    console.log("Get By ID Data: ", data);

    //========== HEADER BINDING ==========

    $("#ddlDocumentType").val(data.vType).trigger("change.select2");
    $("#NumDocumentNo").val(data.vNo);

    if (data.vDate)
        $("#DtDocumentDate").val(String(data.vDate).split("T")[0]);

    // Reference details
    $("#ddlRefNo").val(data.refNo ?? "").trigger("change");

    //========== TRANSPORTER BINDING ==========

    let transporters = [];

    try {
        transporters = typeof data.transportName === "string" ? JSON.parse(data.transportName || "[]") : data.transportName || [];
    } catch {
        transporters = [];
    }

    SetSelectedTransporters(transporters);

    //========== DETAIL GRID BINDING ==========

    const tbody = $("#tblTransportRequest tbody");
    tbody.empty();

    (data.rows || []).forEach(item => {
        AddTransportRow({
            totalQty: item.weight,
            saleGroup: item.itemType,
            cityCode: item.delCityCode,
            vehicleType: item.vehicleType,
            pinCode: item.deliveryPincode,
            remarks: item.remarks,
            partyCode: item.partyCode,
            refNo: item.refNo
        });
    });

    if (isReadOnly) {
        setFormReadonly();
    }
}


// ============= READONLY MODE =============
function setFormReadonly() {
    const form = $('#TransportRequestForm');
    $('#btn-save').hide();
    form.addClass('erppage-readonly');

    $('#tblTransportRequest .del-row, #tblTransportRequest .add-row, #btn-Import')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
    // Keep WhatsApp mobile and Email fields editable
    ToggleSendRequest(true);
}


// ============= SEND REQUEST ENABLE/DISABLE =============
function ToggleSendRequest(enable) {
    const section = $('#fsSendRequest');

    section.find('input:not([type="checkbox"]), select, textarea')
        .prop('disabled', !enable)
        .css({
            'pointer-events': enable ? 'auto' : 'none',
            'cursor': enable ? 'auto' : 'not-allowed',
            'background-color': enable ? '#fff' : '',
            'color': enable ? '#333' : ''
        });

    section.find('input[type="checkbox"], button')
        .prop('disabled', !enable)
        .css({
            'pointer-events': enable ? 'auto' : 'none',
            'cursor': enable ? 'pointer' : 'not-allowed',
            'opacity': enable ? '1' : ''
        });

    section.css('pointer-events', 'auto');
}


//==============Global Variables=========
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/TransportRateRequestForQuotation/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            compCode = d.compCode;
            yearCode = d.yearCode;
            branchCode = d.branchCode;
            companyName = d.companyName;
            add1 = d.add1;
            add2 = d.add2;
            db = d.db;
            compPhone = d.phone;
            compGSTIN = d.gst;
            compPAN = d.pan;
            compEmail = d.email;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}


//===============Report================
async function TransportRateReport() {
    try {
        var vType = ($("#ddlDocumentType").val() || "").trim();
        var vNo = ($("#NumDocumentNo").val() || "").trim();

        if (!vType || !vNo || isNaN(Number(vNo))) {
            showToast("Voucher Type and Voucher No are required.", { type: "warning" });
            return;
        }

        var reportName = "gatepass1";

        var formula =
            "{RFQ1.comp_code}=" + compCode +
            " and {RFQ1.Year_code}=" + yearCode +
            " and {RFQ1.branch_code}=" + branchCode +
            " and {RFQ1.V_no}=" + Number(vNo) +
            " and {RFQ1.v_type}='" + vType + "'";

        var formulaFields = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: db,

            Parameters: {
                comp_name: companyName,
                comp_add1: add1,
                comp_add2: add2,
                GST: "GSTIN : " + compGSTIN,
                PAN: "PAN : " + compPAN,
                COMP_PHONE: "Phone : " + compPhone,
                EMAIL: "Email : " + compEmail,
                RPTNAME: "FACTORY GATE PASS FOR OUTGOING MATERIAL"
            }
        };

        console.log("Gate Pass Report Data:", formulaFields);

        $.ajax({
            url: 'http://localhost:34088/Report/PendingQCReport',
            type: 'POST',
            data: JSON.stringify(formulaFields),
            contentType: "application/json",
            xhrFields: { responseType: 'blob' },

            success: function (response) {
                var file = new Blob([response], { type: 'application/pdf' });

                var link = document.createElement('a');
                link.href = URL.createObjectURL(file);
                link.download = `${reportName}.pdf`;

                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

                setTimeout(function () {
                    URL.revokeObjectURL(link.href);
                }, 1000);
            },

            error: async function (xhr, status, error) {
                console.error("Gate Pass Report Error:", error);

                if (xhr.response instanceof Blob) {
                    console.error("Server Response:", await xhr.response.text());
                } else {
                    console.error("Server Response:", xhr.responseText);
                }

                showToast("Error generating Gate Pass report.", { type: "error" });
            }
        });
    }
    catch (ex) {
        console.error("TransportRateReport() Error:", ex);
        showToast("Error generating Gate Pass report.", { type: "error" });
    }
}


//===============Send Mail===============
function sendTransportEmail() {
    const sendToTransporter = $('#ChkSendToTransporter').is(':checked');
    const emailId = ($('#TxtEmailId').val() || '').trim();

    const transporterCodes = GetSelectedTransporters().map(t => t.Code);

    // Validation 
    if (sendToTransporter && transporterCodes.length === 0) {
        showToast('Please select at least one transporter.', {type:"warning"});
        return;
    }

    if (!sendToTransporter && emailId === '') {
        setInvalid($('#TxtEmailId'), 'Enter email to send.');
        return;
    }

    if (!sendToTransporter && !isValidEmail(emailId)) {
        setInvalid($('#TxtEmailId'), 'Please enter a valid email address.');
        return;
    }

    const request = {
        vType: $('#ddlDocumentType').val(),
        vNo: parseInt($('#NumDocumentNo').val()) || 0,
        sendToTransporter: sendToTransporter,
        emailId: emailId,
        transporterCodes: transporterCodes
    };

    const $btn = $('#btn-SendEmail');
    $btn.prop('disabled', true);

    $.ajax({
        url: '/TransportRateRequestForQuotation/SendEmail',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (response) {
            showToast(response.message, { type: response.success ? 'success' : 'error' });
        },
        error: function () {
            showToast('Unable to send email.', { type:"error"});
        },
        complete: function () {
            $btn.prop('disabled', false);
        }
    });
}
function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}


//===============Send WhatsApp===============
function sendTransportWhatsApp() {
    const sendToTransporter = $('#ChkSendToTransporter').is(':checked');
    const mobileNo = ($('#NumMobileNo').val() || '').trim();
    const transporterCodes = GetSelectedTransporters().map(t => t.Code);

    if (sendToTransporter && transporterCodes.length === 0) {
        showToast('Please select at least one transporter.', { type: 'warning' });
        return;
    }

    if (!sendToTransporter && mobileNo === '') {
        setInvalid($('#NumMobileNo'), 'Enter mobile no. to send whatsapp.');
        return;
    }

    if (!sendToTransporter && !/^\d{10,15}$/.test(mobileNo)) {
        setInvalid($('#NumMobileNo'), 'Please enter a valid mobile no.');
        return;
    }

    const request = {
        vType: $('#ddlDocumentType').val(),
        vNo: parseInt($('#NumDocumentNo').val()) || 0,
        sendToTransporter: sendToTransporter,
        mobileNo: mobileNo,
        transporterCodes: transporterCodes
    };

    const $btn = $('#btn-SendWhatsApp');
    $btn.prop('disabled', true);

    $.ajax({
        url: '/TransportRateRequestForQuotation/SendWhatsApp',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (response) {
            showToast(response.message, { type: response.success ? 'success' : 'warning' });
        },
        error: function () {
            showToast('Unable to send WhatsApp message.', { type: 'warning' });
        },
        complete: function () {
            $btn.prop('disabled', false);
        }
    });
}


//===============City PinCode===============
async function GetCityPincode(cityCode) {
    try {
        const response = await fetch(
            `/TransportRateRequestForQuotation/GetCityPincode?cityCode=${encodeURIComponent(cityCode)}`
        );

        if (!response.ok) throw new Error("Unable to get PIN Code");

        const result = await response.json();
        console.log("Pincode: ", result)
        return result;

    } catch (e) {
        console.error("GetCityPincode Error:", e);
        return "";
    }
}