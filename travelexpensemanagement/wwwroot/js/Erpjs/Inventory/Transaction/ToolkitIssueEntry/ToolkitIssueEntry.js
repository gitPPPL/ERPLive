
const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

let compCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
let wsid = "";
let userid = "";

$(document).ready(async function () {

    try {
        getGlobalValues();
        wireEvents();
        await loadAllDropdowns();
        const currentDate = getCurrentDateYMD();
        $('#DtDocDate, #DtFromdate, #DtTodate').val(currentDate);

        if (!isNaN(rowId) && rowId > 0) {
            await getDataById();
        }
        else {
            
            const vType = $('#ddlDocType').val();
            if (vType) {
                await GetVNo(vType);
            }
        }

        if (isReadOnly) {
            setFormReadOnly();
        }

        $('#ddlDocType').prop('disabled', true);
        $('#DtDocDate').focus();

    } catch (error) {
        console.error('Dropdown loading failed:', error);
        showToast('Failed to load dropdowns: ' + error.message, { type: 'error' });
    }
});

//=============EVENTS=================
function wireEvents() {
    $('#btn_save').on('click', function (e) {
        e.preventDefault();

        if (!validateRequiredField('#NumDocNo', 'Voucher No') || !validateRequiredField('#DtDocDate', 'Voucher Date') ||
            !validateRequiredField('#ddlItemName', 'Item Name') || !validateRequiredField('#Numquantity', 'Quantity') ||
            !validateRequiredField('#ddlEmployeeName', 'Employee Name') || !validateRequiredField('#ddlUnit', 'Unit') ||
            !validateRequiredField('#ddlFromDept', 'From department') || !validateRequiredField('#ddlToDept', 'To department')) return;

        saveOrUpdate();
    })
}

//=============DROPDOWNS=================
async function loadAllDropdowns() {

    const [
        docTypes,
        //items,
        employees,
        places,
        departments
    ] = await Promise.all([
        getDdlData('doctype'),
        //getDdlData('item'),
        getDdlData('employee'),
        getDdlData('place'),
        getDdlData('department')
    ]);

    fillDropdown('#ddlDocType', docTypes);
    //fillDropdown('#ddlItemName', items, true);
    fillDropdown('#ddlEmployeeName', employees, true);
    fillDropdown('#ddlUnit', places);
    fillDropdown('#ddlFromDept', departments, true);
    fillDropdown('#ddlToDept', departments, true);
    fillDropdown('#ddlPrintEmployeeName', employees, true);

    loadItemList('#ddlItemName');
}
async function getDdlData(type) {
    return await $.ajax({
        url: '/ToolkitIssueEntry/GetDdlList',
        type: 'GET',
        data: { type: type },
        global: false
    });
}
function fillDropdown(selector, data, useSelect2 = false) {

    const ddl = $(selector);

    ddl.empty();

    if (useSelect2) {
        ddl.append('<option value="">-- Select --</option>');
    }

    $.each(data, function (index, item) {
        ddl.append(
            $('<option>', {
                value: item.Value,
                text: item.Text
            })
        );
    });

    if (useSelect2) {
        initSelect2(ddl);
    }
}
function loadItemList(dropdownId) {
    const ddl = $(dropdownId);

    ddl.select2({
        placeholder: "-- Select --",
        allowClear: true,
        // 1. Change this to 0 so it triggers as soon as the dropdown opens
        minimumInputLength: 0,
        ajax: {
            url: '/ToolkitIssueEntry/GetItemList',
            dataType: 'json',
            delay: 250,
            global: false,
            data: function (params) {
                return {
                    // If params.term is undefined (on first click), pass an empty string
                    searchTerm: params.term || "",
                    page: params.page || 1
                };
            },
            processResults: function (data, params) {
                params.page = params.page || 1;

                const results = data.data.map(item => ({
                    id: item.Value,
                    text: item.Text
                }));

                return {
                    results: results,
                    pagination: {
                        // Triggers infinite scroll loading if there are more items
                        more: (params.page * 30) < data.totalCount
                    }
                };
            },
            cache: true
        }
    });
}

//-------- SELECT2 HELPER -------------
function initSelect2($ddl) {
    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true
    });
    $ddl.on('select2:open', function () {
        setTimeout(function () {
            let searchBox = document.querySelector('.select2-container--open .select2-search__field');

            if (searchBox) {
                searchBox.focus();
            }
        }, 0);
    });
}
function setSelect2Value(selector, value, text) {

    const ddl = $(selector);

    // Remove any existing selected option
    ddl.find('option:selected').remove();

    // Create the option that Select2 doesn't have yet
    const option = new Option(text, value, true, true);

    ddl.append(option).trigger('change');
}

//=============GENERATE VNO=================
async function GetVNo(vType) {
    try {
        const res = await fetch(`/ToolkitIssueEntry/GetVNo?vType=${encodeURIComponent(vType)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        console.log("V_NO: ", data);
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=============SAVE AND UPDATE=================
function collectDataForSave() {
    return request = {
        V_TYPE: ($('#ddlDocType').val() || '').trim(),
        V_NO: parseInt($('#NumDocNo').val()) || 0,
        V_DATE: $('#DtDocDate').val() || '',
        PLACE_CODE: parseInt($('#ddlUnit').val()) || 0,
        ITEM_CODE: parseInt($('#ddlItemName').val()) || 0,
        QTY: parseFloat($('#Numquantity').val()) || 0,
        RATE: parseFloat($('#Numitemrate').val()) || 0,
        AMOUNT: parseFloat($('#Numamount').val()) || 0,
        EMP_CODE: parseInt($('#ddlEmployeeName').val()) || 0,
        FROM_DEPT: parseInt($('#ddlFromDept').val()) || 0,
        TO_DEPT: parseInt($('#ddlToDept').val()) || 0,
        REMARK: ($('#TxtRemarks').val() || '').trim(),
        RECD_QTY: parseFloat($('#Numquantity').val()) || 0,
        DR_AMOUNT: parseFloat($('#Numdebitamount').val()) || 0,
        ACTION: rowId ? "UPDATE" : "INSERT"
    }
}
function saveOrUpdate() {
    const data = collectDataForSave();
    $.ajax({
        url: '/ToolkitIssueEntry/SaveOrUpdate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (res) {
            if (res.success) {
                showToast(res.message, { type: "success" });
                setTimeout(() => {
                    window.location.href = '/ToolkitIssueList/Index';
                }, 1500);
            }
            else {
                showToast(res.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            console.error(xhr)
            showToast("Save Failed!", { type: "error" });
        }
    })
}

//=============EDIT AND VIEW=================
async function getDataById() {
    try {
        const res = await $.ajax({
            url: '/ToolkitIssueEntry/GetDataById',
            type: 'GET',
            data: {
                vType: rowIdVType,
                vNo: rowId
            },
            dataType: 'JSON'
        });

        if (!res.success) {
            showToast(res.message, { type: "warning" });
            return;
        }

        const data = res.data;
        console.log(data);
        fillFormData(data);

    } catch (xhr) {
        console.error(xhr);
        throw new Error('Failed in fetching data by Id');
    }
}
function fillFormData(data) {
    // Normal dropdown
    $('#ddlDocType').val(data.v_TYPE);
    $('#ddlUnit').val(data.placE_CODE);

    // Select2 dropdowns
    //$('#ddlItemName').val(data.iteM_CODE).trigger('change');
    $('#ddlEmployeeName').val(data.emP_CODE).trigger('change');
    $('#ddlFromDept').val(data.froM_DEPT).trigger('change');
    $('#ddlToDept').val(data.tO_DEPT).trigger('change');

    setSelect2Value('#ddlItemName', data.iteM_CODE, data.iteM_NAME);

    $('#NumDocNo').val(data.v_NO);
    $('#DtDocDate').val(formatDateYMD(data.v_DATE));
    $('#Numquantity').val(data.qty);
    $('#Numitemrate').val(data.rate);
    $('#Numamount').val(data.amount);
    $('#TxtRemarks').val(data.remark);
    $('#Numdebitamount').val(data.dR_AMOUNT);
}
function setFormReadOnly() {
    const form = $('#Non-disableFields');
    $('#btn_save').hide();
    form.addClass('erppage-readonly');

}

//=============REPORT=================
//----Issue----
async function GenerateIssueReport() {

    var reportName = "STOOL1";
    const fromDate = $("#DtFromdate").val();
    const toDate = $("#DtTodate").val();
    const empCode = ($("#ddlPrintEmployeeName").val() || "").trim();

    // Crystal Report Formula
    let formula =
        "{STOOL.V_DATE} IN  " + crystalDate(fromDate) + 
        " TO " + crystalDate(toDate) + 
        " AND {STOOL.V_TYPE} = 'TOIS'" +
        " AND {STOOL.COMP_CODE} = " + compCode +
        " AND {STOOL.BRANCH_CODE} = " + branchCode;

    // Employee filter - only if selected
    if (empCode !== "") {
        formula += " AND {STOOL.EMP_CODE} = " + empCode;
    }
    console.log("formula: ", formula);

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,
        Parameters: {
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            RPTNAME: "STORE ITEM ISSUE TO EMPLOYEE"
        }
    };

    var now = new Date();
    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);
    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');
    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34088/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
            console.log('PDF response:', response);
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },
        error: function (xhr, status, error) {
            console.error('Error generating report:', error);
        }
    });

}
//----Balance----
async function PrepareToolKitBalReport() {

    var fromDate = $('#DtFromdate').val();
    var toDate = $('#DtTodate').val();

    try {

        const response = await $.ajax({
            url: '/ToolkitIssueEntry/PrepareToolKitBalReport',
            type: 'POST',
            data: {
                fromDate: fromDate,
                toDate: toDate
            }
        });

        if (response.success) {
            return true;
        }

        showToast(response.message, {type: "warning"});
        return false;

    } catch (xhr) {
        showToast('Error: ' + xhr.responseText, { type: "error" });
        return false;
    }
}
async function GenerateBalReport() {

    const result = await PrepareToolKitBalReport();
    if (!result) {
        showToast("Failed to generate Balance report", { type:"warning" })
        return;
    }
    const fromDate = $("#DtFromdate").val();
    const toDate = $("#DtTodate").val();
    var reportName = "STOOL2";
    const empCode = ($("#ddlPrintEmployeeName").val() || "").trim();

    // Crystal Report Formula
    let formula =
        "{temp_inv1.wsid} = '" + wsid + "'" +
        " AND {temp_inv1.userid} = " + userid +
        " AND {temp_inv1.COMP_CODE} = " + compCode;

    // Employee filter - only if selected
    if (empCode !== "") {
        formula += " AND {temp_inv1.PARTY_CODE} = " + empCode;
    }

    formula += " AND {@CLOSINGQTY}>0";

    console.log("formula: ", formula);

    const dateText = "From " + formatReportDate(fromDate) + " To " + formatReportDate(toDate);

    console.log("dateText: ", dateText);

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,
        Parameters: {
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            RPTNAME: "TOOL KIT BALANCE LIST AT EMPLOYEE",
            f1: dateText
        }
    };

    var now = new Date();
    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);
    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');
    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34088/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
            console.log('PDF response:', response);
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },
        error: function (xhr, status, error) {
            console.error('Error generating report:', error);
        }
    });

}

//=============HELPERS=================
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/ToolkitIssueEntry/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            compCode = d.compCode;
            branchCode = d.branchCode;
            companyName = d.companyName;
            add1 = d.add1;
            add2 = d.add2;
            db = d.db;
            userid = d.userid;
            wsid = d.wsid;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}
function crystalDate(dateStr) {

    if (!dateStr) return "";

    // handle ISO format: yyyy-MM-dd
    var parts = dateStr.includes('-')
        ? dateStr.split('-')
        : dateStr.split('/');

    if (parts.length !== 3) return "";

    // detect format
    var year, month, day;

    if (dateStr.includes('-') && parts[0].length === 4) {
        // yyyy-MM-dd
        year = parts[0];
        month = parts[1];
        day = parts[2];
    } else {
        // dd/MM/yyyy
        day = parts[0];
        month = parts[1];
        year = parts[2];
    }

    return `Date(${year},${parseInt(month)},${parseInt(day)})`;
}
function formatReportDate(dateStr) {

    if (!dateStr) return "";

    var parts = dateStr.includes("-")
        ? dateStr.split("-")
        : dateStr.split("/");

    if (parts.length !== 3) return "";

    var year, month, day;

    if (dateStr.includes("-") && parts[0].length === 4) {
        year = parts[0];
        month = parts[1];
        day = parts[2];
    } else {
        day = parts[0];
        month = parts[1];
        year = parts[2];
    }

    var months = [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];

    return String(parseInt(day)).padStart(2, "0") +
        "/" +
        months[parseInt(month) - 1] +
        "/" +
        year;
}
