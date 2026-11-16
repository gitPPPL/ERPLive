var selectedParties = {};
var userlevel = window.userlevel;

let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
let compPhone = "";
let compEmail = "";

$(document).ready(async function () {
    if (userlevel != 1) {
        $('#rdoOutstanding').hide();
        $('label[for="rdoOutstanding"]').hide();
    }
    getGlobalValues();

    WireEvents();

    $('#DtFromDate, #DtToDate').val(getCurrentDateYMD());

    await bindHeaderDropdowns();
});


// ======================== EVENTS ========================
function WireEvents() {

    // Mail From change
    $('#ddlMailFrom').on('change', async function () {

        $('input[name="MailType"]').prop('checked', false);
        await bindDropdown("DocumentEmailing", "documenttype", '#ddlDocumentType', '--Select Document Type--', null, null, true, null, true);
    });

    // Selected / All Party radio button
    $('input[name="PartyOption"]').on('change', function () {

        var isSelected = $('#rdoPartySelected').is(':checked');

        $('#divSelectedParty').toggle(isSelected);

        if (!isSelected) {
            ClearSelectedParties();
        }
    });

    // Party selection using Select2
    $('#ddlPartyName').on('select2:select', function (e) {

        if (!$('#rdoPartySelected').is(':checked')) return;

        var $option = $(this).find('option:selected');

        var code = $option.val();
        var name = $option.text();

        console.log('Party Code:', code);
        console.log('Party Name:', name);

        if (!code) return;

        AddSelectedParty(code, name);

        // Reset dropdown
        $(this).val('').trigger('change.select2');

    });

    // Remove selected party
    $('#tblSelectedParty').on('click', '.btn-remove-party', function () {

        var code = $(this).closest('tr').attr('data-code');

        RemoveSelectedParty(code);
    });

    // Mail Type radio buttons
    $('input[name="MailType"]').on('change', async function () {

        var mailType = $(this).val();

        // clear grid (MainGrid.DataSource = Nothing)
        $('#tblEmailSetting tbody').empty();
        $('#chkAllRecords').prop('checked', false);

        if (mailType.toLowerCase() === 'outstanding') {
            $('#TxtMessage').val('');
            return;
        }

        // doctype + message
        $.getJSON('/DocumentEmailing/GetMailTypeInfo', { mailType: mailType }, function (res) {
            if (!res || !res.success) return;

            docType = res.docType;
            $('#TxtMessage').val(res.message);
        });

        // reload Document Type for the selected mail type
        await bindDropdown("DocumentEmailing", "documenttype", '#ddlDocumentType',
            '--Select Document Type--', null, null, true, mailType, true);
    });

    // Fill Data button click
    $('#btn-FillData').on('click', async function () {
        await FillData();
    });

    // Select all records
    $('#chkAllRecords').on('change', function () {
        $('#tblEmailSetting tbody .chk-record').prop('checked', $(this).is(':checked'));
    });

    // Row checkbox change: header checkbox sync
    $('#tblEmailSetting tbody').on('change', '.chk-record', SyncSelectAllCheckbox);

    // Select records by RNo range
    $('#btn-SelectRecords').on('click', function () {

        var from = parseInt($('#NumFromRNo').val(), 10) || 0;
        var to = parseInt($('#NumToRNo').val(), 10) || 0;

        if (from === 0 || to === 0) {
            showToast('Required From RNo and To RNo.', { type: "warning" });
            return;
        }

        if (to < from) {
            showToast('Invalid given Range.', { type: "warning" });
            return;
        }

        var $rows = $('#tblEmailSetting tbody tr');

        // uncheck everything first
        $rows.find('.chk-record').prop('checked', false);

        if (to <= $rows.length) {
            for (var i = from - 1; i <= to - 1; i++) {
                var $tr = $rows.eq(i);

                // tick only when Vtype (2nd column) is not blank
                if ($tr.find('td').eq(1).text().trim().length > 0) {
                    $tr.find('.chk-record').prop('checked', true);
                }
            }
        } else {
            showToast('Total Records should be <= given Range.', { type: "warning" });
            $('#NumToRNo').val($rows.length);
        }

        SyncSelectAllCheckbox();
    });

    // Clear (uncheck) all records
    $('#btn-Clear').on('click', function () {
        $('#tblEmailSetting tbody .chk-record').prop('checked', false);
        SyncSelectAllCheckbox();
    });

    // Document Type change
    $('#ddlDocumentType').on('change', async function () {
        // clear grid
        $('#tblEmailSetting tbody').empty();
        $('#chkAllRecords').prop('checked', false);

        var mailType = ($('input[name="MailType"]:checked').val() || '').toLowerCase();

        if (mailType === 'salesinvoiceagent') {
            await bindDropdown("DocumentEmailing", "agentbysales", '#ddlAgentName', '--Select Agent--', null, null, false, null, true);
        }
        else if (mailType === 'salesinvoicecustomer') {
            await bindDropdown("DocumentEmailing", "customerbysales", '#ddlAgentName', '--Select Agent--', null, null, false, null, true);
        }
    });

    // Mail button Click
    $('#btn-Mail').on('click', async function () {
        await MailDocuments();
    });

    // Exit
    $('#btn-Exit').on('click', function () {
        window.location.href = '/Dashboard/Index';
    });
}


// ================= SELECT ALL CHECKBOX SYNC =============
function SyncSelectAllCheckbox() {

    var total = $('#tblEmailSetting tbody .chk-record').length;
    var checked = $('#tblEmailSetting tbody .chk-record:checked').length;

    $('#chkAllRecords').prop('checked', total > 0 && total === checked);
}


// ================= SELECTED PARTY LIST ==================
function AddSelectedParty(code, name) {

    code = String(code);

    if (Object.prototype.hasOwnProperty.call(selectedParties, code)) {
        return;
    }

    selectedParties[code] = name;

    RenderSelectedParties();

    console.log('Selected Parties:', selectedParties);
}
function RemoveSelectedParty(code) {

    delete selectedParties[String(code)];

    RenderSelectedParties();
}
function ClearSelectedParties() {

    selectedParties = {};

    RenderSelectedParties();
}
function RenderSelectedParties() {

    var $tbody = $('#tblSelectedParty tbody').empty();

    $.each(selectedParties, function (code, name) {

        var $row = $('<tr>').attr('data-code', code);

        $row.append(`
            <td width="100%">${name}</td>
            <td width="1%" class="" style="white-space:nowrap;">
                <button type="button" class="erppage-btn erppage-btn-remove btn-remove-party" title="Remove">
                    <i class="fa fa-xmark"></i>
                </button>
            </td>
        `);

        $tbody.append($row);
    });
}


// ================= GET SELECTED PARTY CODES =============
function GetSelectedPartyCodes() {

    return $('#rdoPartySelected').is(':checked')
        ? Object.keys(selectedParties)
        : [];
}


// ================= DROPDOWNS =============================
async function bindHeaderDropdowns() {

    return Promise.all([
        bindDropdown("DocumentEmailing", "mail", '#ddlMailFrom', '--Select Mail From--', null, null, true, null, false),
        bindDropdown("DocumentEmailing", "documenttype", '#ddlDocumentType', '--Select Document Type--', null, null, true, null, true),
        bindDropdown("DocumentEmailing", "party", '#ddlPartyName', '--Select Party--', null, null, false, null, true),
        bindDropdown("DocumentEmailing", "agent", '#ddlAgentName', '--Select Agent--', null, null, false, null, true),
        bindDropdown("DocumentEmailing", "acgroup", '#ddlAcGroup', '--Select A/c Group--', null, null, false, null, true),
        bindDropdown("DocumentEmailing", "itemtype", '#ddlSalesItemType', '--Select Item Type--', null, null, false, null, true)
    ]);
}


// ================= FILL DATA =============================
async function FillData() {

    var mailType = $('input[name="MailType"]:checked').val();
    
    if (!mailType) {
        showToast('Please select Mail Type.', { type:"warning" });
        return;
    }

    var isOutstanding = (mailType.toLowerCase() === 'outstanding');

    // VB: Outstanding me Document Type nahi hota
    if (!isOutstanding && !$('#ddlDocumentType').val()) {
        showToast('Please select Document Type.', {type:"warning"});
        return;
    }

    // VB: Outstanding me Party / Group / Agent me se ek zaroori
    if (isOutstanding && GetSelectedPartyCodes().length === 0 && !$('#ddlAcGroup').val() && !$('#ddlAgentName').val()) {
        showToast('Please select Party or Group or Agent.', {type:"warning"});
        return;
    }

    var $btn = $('#btn-FillData').prop('disabled', true);

    $('#chkAllRecords').prop('checked', false);

    try {
        var res = await $.ajax({
            url: '/DocumentEmailing/FillData',
            type: 'GET',
            traditional: true,   // partyCodes=1&partyCodes=2 ke format me bhejta hai
            data: {
                mailType: mailType,
                docType: $('#ddlDocumentType').val() || '',
                fromDate: $('#DtFromDate').val(),
                toDate: $('#DtToDate').val(),
                partyCodes: GetSelectedPartyCodes(),
                agentCode: $('#ddlAgentName').val() || 0,
                itemType: $('#ddlSalesItemType').val() || '',
                acGroup: $('#ddlAcGroup').val() || 0
            }
        });

        if (!res || !res.success) {
            showToast((res && res.message) || 'Unable to load data.', {type:"error"});
            return;
        }

        $('#lblTotalRowsCount').text("Total Rows => " + res.data.length);
        RenderEmailRecords(res.data);

    } catch (err) {
        console.error(err);
        showToast('Error while loading data.', {type:"error"});
    } finally {
        $btn.prop('disabled', false);
    }
}
function RenderEmailRecords(rows) {

    var $tbody = $('#tblEmailSetting tbody').empty();

    $.each(rows || [], function (_, r) {

        var $tr = $('<tr>').attr('data-vtype', r.VTYPE).attr('data-vno', r.VNO).attr('data-pcode', r.PCODE);

        $tr.append($('<td>').append('<input type="checkbox" class="erppage-checkbox-input chk-record">'));
        $tr.append($('<td>').text(r.VTYPE ?? ''));
        $tr.append($('<td>').text(r.VNO ?? ''));
        $tr.append($('<td>').text(r.VDATE ?? ''));
        $tr.append($('<td>').text(r.NAME ?? ''));
        $tr.append($('<td>').text(r.INVNO ?? ''));
        $tr.append($('<td>').text(r.INVDATE ?? ''));
        $tr.append($('<td style="text-align:right;">').text(r.AMT != null ? Number(r.AMT).toFixed(2) : ''));
        $tr.append($('<td>').text(r.EMAIL ?? ''));

        $tbody.append($tr);
    });
}


//==============Global Variables=========
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/DocumentEmailing/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            companyName = d.companyName;
            add1 = d.add1;
            add2 = d.add2;
            db = d.db;
            compPhone = d.phone;
            compEmail = d.email;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}


// ================= PRINT (MAIL_CONFIRM) ==================
function FormatDateDMY(date) {

    var dd = String(date.getDate()).padStart(2, '0');
    var mm = String(date.getMonth() + 1).padStart(2, '0');

    return dd + '.' + mm + '.' + date.getFullYear();
}
async function DocumentEmailingReport() {
    try {
        var $checked = $('#tblEmailSetting tbody .chk-record:checked');

        if ($checked.length === 0) {
            showToast('Please select a row to print.', { type: "warning" });
            return;
        }

        if ($checked.length > 1) {
            showToast('Please select only one row to print.', { type: "warning" });
            return;
        }

        var $row = $checked.closest('tr');

        var cells = $row.find('td');

        // VB: Cells(2) = Srno (party code), Cells(7) = Inv Amt
        var partyCode = Number(cells.eq(2).text().trim()) || 0;
        var balAmt = Number(cells.eq(7).text().trim()) || 0;

        var reportName = "MAIL_CONFIRM";

        var toDate = $('#DtToDate').val();   // yyyy-MM-dd
        var asOnDate = toDate ? FormatDateDMY(new Date(toDate + 'T00:00:00')) : '';

        var formulaFields = {
            Reportname: reportName,
            selectionFormula: "{SUBGROUP_MAST.CODE}=" + partyCode,
            Database: db,

            Parameters: {
                comp_name: companyName,
                comp_add1: add1,
                comp_add2: add2,
                comp_phone: compPhone,
                email: compEmail,
                BalAmt: balAmt,
                AsOnDate: asOnDate,
                F4: FormatDateDMY(new Date())
            }
        };

        console.log("Mail Confirm Report Data:", formulaFields);

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
                console.error("Mail Confirm Report Error:", error);

                if (xhr.response instanceof Blob) {
                    console.error("Server Response:", await xhr.response.text());
                } else {
                    console.error("Server Response:", xhr.responseText);
                }

                showToast("Error generating Mail Confirm report.", { type: "error" });
            }
        });
    }
    catch (ex) {
        console.error("DocumentEmailingReport() Error:", ex);
        showToast("Error generating Mail Confirm report.", { type: "error" });
    }
}


// ================= EXPORT SALES INVOICE ==================
async function ExportSalesInvoices() {

    if ($('#tblEmailSetting tbody tr').length === 0) return;

    var sleep = function (ms) { return new Promise(function (r) { setTimeout(r, ms); }); };

    var $btn = $('#btn-ExportSalesInvoice').prop('disabled', true);

    try {
        var $rows = $('#tblEmailSetting tbody tr').filter(function () {
            return $(this).find('.chk-record').is(':checked') &&
                $(this).find('td').eq(2).text().trim() !== '';
        });

        if ($rows.length === 0) {
            showToast('Please select record(s) to export.', { type: "warning" });
            return;
        }

        for (var i = 0; i < $rows.length; i++) {

            var cells = $($rows[i]).find('td');
            var vType = cells.eq(1).text().trim();
            var vNo = cells.eq(2).text().trim();

            if (vType === '') continue;

            var info = await $.getJSON('/DocumentEmailing/PrepareInvoiceExport', { vType: vType, vNo: vNo });

            if (!info || !info.success) {
                throw new Error((info && info.message) || 'Unable to prepare invoice.');
            }

            var formulaFields = {
                Reportname: info.reportName,
                selectionFormula: info.selectionFormula,
                Database: db,
                Parameters: info.parameters
            };

            console.log("Report Data:", formulaFields);

            await $.ajax({
                url: 'http://localhost:34088/Report/PendingQCReport',
                type: 'POST',
                data: JSON.stringify(formulaFields),
                contentType: "application/json",
                xhrFields: { responseType: 'blob' },

                success: function (response) {
                    console.log("PDF response:", response);
                    var file = new Blob([response], { type: 'application/pdf' });

                    var fileName = `${vType}${vNo}.pdf`;
                    var link = document.createElement('a');
                    link.href = URL.createObjectURL(file);
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    URL.revokeObjectURL(link.href);
                },

                error: function (xhr, status, error) {
                    console.error("Error generating report:", error);
                    console.error("Response:", xhr.responseText);
                }
            });

            await sleep(500);
        }

        showToast('Sale Invoice(s) Successfully Exported.', { type: "success" });
    }
    catch (ex) {
        console.error("Error generating report:", ex);
    }
    finally {
        $btn.prop('disabled', false);
    }
}


// ================= SEND MAIL ==================
async function MailDocuments() {

    var rows = [];

    $('#tblEmailSetting tbody tr').each(function () {
        var $tr = $(this);
        var c = $tr.find('td');

        if ($tr.find('.chk-record').is(':checked') && c.eq(2).text().trim() !== '') {
            rows.push({
                vType: c.eq(1).text().trim(),
                vNo: c.eq(2).text().trim(),
                vDate: c.eq(3).text().trim(),
                name: c.eq(4).text().trim(),
                invNo: c.eq(5).text().trim(),
                amt: Number(c.eq(7).text().trim()) || 0,
                drAmt: Number($tr.attr('data-dramt')) || 0,
                email: c.eq(8).text().trim(),
                pCode: String($tr.attr('data-pcode') ?? '')
            });
        }
    });

    var $btn = $('#btn-Mail').prop('disabled', true);

    try {
        var res = await $.ajax({
            url: '/DocumentEmailing/SendMail',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                mailType: $('input[name="MailType"]:checked').val() || '',
                message: $('#TxtMessage').val(),
                toDate: $('#DtToDate').val(),
                rows: rows
            })
        });

        if (!res || !res.success) {
            showToast((res && res.message) || 'Error while sending mail.', { type: "error" });
            return;
        }

        $.each(res.results || [], function (_, r) {
            showToast(r.message, { type: r.success ? "success" : "error" });
        });
    }
    catch (err) {
        console.error(err);
        showToast('Error while sending mail.', { type: "error" });
    }
    finally {
        $btn.prop('disabled', false);
    }
}