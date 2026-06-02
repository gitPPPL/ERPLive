
const allFieldIds = [
    "DtDocDate",
    "ddlPlace"
];

// const itemRecords = [
//     'TxtCode',
//     'TxtVNo',
//     'ddlShift',
//     'TxtLamination',
//     'ddlSupervisor',
//     'ddlOperator',
//     'TxtItemName',
//     'TxtMeter',
//     'TxtNetWt',
//     'TxtFabricSize',
//     'TxtUnLamGram',
//     'TxtCoating',
//     'TxtLamGram',
//     'TxtLAMGSM',
//     'TxtRollNo',
//     'TxtNWarpWay',
//     'TxtWarpWay',
//     'TxtNWeftWay',
//     'TxtWeftWay',
//     'TxtSupervisor',
//     'TxtOperator',
//     'ddlStrength',
//     'ddlStatus',
//     'TxtElongWarp',
//     'TxtElongWeft',
//     'TxtRemarks',
//     'TxtSupCode',
//     'TxtOpeCode'
// ];

let docId = "", readOnly;
let lastChecked = 'RDPendingQC';
let IsChanges = false;
let currentPage = 1;
let pageSize = 10;

function getQueryParam(param) {
    return new URLSearchParams(window.location.search).get(param);
}

$(document).ready(function () {
    initPage();
});

//=====Page Load
async function initPage() {
    try {
        await handleDocLoad();
        setEnterKeyFocus(allFieldIds);
        wireEvents();
    } catch (err) {
        showToast('Initialization failed: ' + err, { type: "error" });
    }
}

//=====Events
function wireEvents() {

    $('#btn_update').on('click', async (e) => {
        e.preventDefault();

        if (!validateRequiredField('#DtDocDate', 'Doc Date') ||
            !validateRequiredField('#ddlPlace', 'Place Type')) return;

        const isvalid = validate();
        if (!isvalid) {
            return;
        }
        await processTenacityData();
        try {
            const data = await collectFormData();
            if (data.LaminationDetails && data.LaminationDetails.length > 0) {
                UpdateData(data);
            } //else {
            //    showToast("", { type: "warning" });
            //    return;
            //}


        } catch (err) {
            showToast("Error saving data: " + err, { type: "error" });
        }
    });

    $('#RDPendingQC').on('click', function () {
        if (lastChecked === 'RDPendingQC') {
            $(this).prop('checked', false);
            lastChecked = null;
        } else {
            lastChecked = 'RDPendingQC';
        }
    });

    $('#RDAll').on('click', function () {
        if (lastChecked === 'RDAll') {
            $(this).prop('checked', false);
            lastChecked = null;
        } else {
            lastChecked = 'RDAll';
        }
    });

    $('#btn_show_data').on('click', async function () {
        var selectedQC = $('input[name="qcFilter"]:checked').val();
        var placeCode = $('#ddlPlace').val();
        var date = $('#DtDocDate').val();
        var plantCode = $('#ddlPlant').val();

        if (!placeCode) {
            showToast("Please select place!", { type: "warning" });
            return;
        }

        checkModificationAllowed(selectedQC, placeCode, date, plantCode)

        //await GetdataBasedOnQC(selectedQC, placeCode, date, plantCode);
    });

    //=====Supervisor change
    $(document).on('change', 'select[id^=ddlSupervisor]', function () {
        const supervisorId = $(this).val();
        var rowId = $(this).closest('tr');
        var laminationName = rowId.find("input[id^='TxtLamination']").val();
        var shift = rowId.find("select[id^='ddlShift']").val();
        fillSupervisorForSameLamination(laminationName, supervisorId, shift);
    });

    //=====Operator change
    $(document).on('change', 'select[id^=ddlOperator]', function () {
        const supervisorId = $(this).val();
        var rowId = $(this).closest('tr');
        var laminationName = rowId.find("input[id^='TxtLamination']").val();
        var shift = rowId.find("select[id^='ddlShift']").val();
        fillOperatorForSameLamination(laminationName, supervisorId, shift);
    });

    //=====Warp way and weft way calculation
    $(document).on('change', 'input[id^="TxtNWarpWay"], input[id^="TxtWarpWay"], input[id^="TxtNWeftWay"], input[id^="TxtWeftWay"]', function () {

        const id = this.id;
        const rowNum = id.match(/\d+$/)?.[0];

        if (!rowNum) return;

        const nWarpWay = $(`#TxtNWarpWay${rowNum}`);
        const warpWay = $(`#TxtWarpWay${rowNum}`);
        const nWeftWay = $(`#TxtNWeftWay${rowNum}`);
        const weftWay = $(`#TxtWeftWay${rowNum}`);

        if (id.startsWith('TxtNWarpWay')) {
            warpWay.val(
                ((parseFloat(nWarpWay.val()) || 0) / 9.8).toFixed(2)
            );
        }

        if (id.startsWith('TxtWarpWay')) {
            nWarpWay.val(
                ((parseFloat(warpWay.val()) || 0) * 9.8).toFixed(2)
            );
        }

        if (id.startsWith('TxtNWeftWay')) {
            weftWay.val(
                ((parseFloat(nWeftWay.val()) || 0) / 9.8).toFixed(2)
            );
        }

        if (id.startsWith('TxtWeftWay')) {
            nWeftWay.val(
                ((parseFloat(weftWay.val()) || 0) * 9.8).toFixed(2)
            );
        }
    });
}
//=====Fill Supervisor For Same Lam
function fillSupervisorForSameLamination(laminationName, supervisorId, shift) {
    if (laminationName !== "" && supervisorId > 0 && shift != "") {
        const $rows = $('#tblLaminationQCEntry tbody tr');
        $rows.each(function () {
            const $row = $(this);
            const rowLaminationName = $row.find("input[id^='TxtLamination']").val();
            const rowshift = $row.find("select[id^='ddlShift']").val();
            if (rowLaminationName === laminationName && rowshift === shift) {
                // $row.find("select[id^='ddlSupervisor']").val(supervisorId);
                bindDropdown('LaminationQCEntry', 'Supervisor', $row.find("select[id^='ddlSupervisor']"), 'Select Supervisor', supervisorId, null, false, null, true),
                    $row.find("input[id^='TxtSupCode']").val(supervisorId);
            }
        });
    } else {
        showToast("No data found.", { type: "info" });
    }
}

//=====Fill Operator For Same Lam
function fillOperatorForSameLamination(laminationName, operatorId, shift) {
    if (laminationName !== "" && operatorId > 0 && shift != "") {
        const $rows = $('#tblLaminationQCEntry tbody tr');

        $rows.each(function () {
            const $row = $(this);
            const rowLaminationName = $row.find("input[id^='TxtLamination']").val();
            const rowshift = $row.find("select[id^='ddlShift']").val();
            if (rowLaminationName === laminationName && rowshift === shift) {
                // $row.find("select[id^='ddlOperator']").val(operatorId);
                bindDropdown('LaminationQCEntry', 'Operator', $row.find("select[id^='ddlOperator']"), 'Select Operator', operatorId, null, false, null, true),
                    $row.find("input[id^='TxtOpeCode']").val(operatorId);
            }
        });
    } else {
        showToast("No data found.", { type: "info" });
    }
}

//=====Get QC Data
async function GetdataBasedOnQC(selectedValue, placeCode, date, plantCode) {
    $.ajax({
        url: '/LaminationQCEntry/GetQCDataList',
        type: 'GET',
        data: {
            QcAllOrPending: selectedValue,
            PlaceCode: placeCode,
            date: date,
            plantCode: plantCode
        },
        success: async function (response) {
            if (response.status && response.data.length > 0) {
                const data = response.data;
                const Docid = data[0].DOC_ID || 0;
                $('#TxtDocId').val(Docid);

                const $tbody = $('#tblLaminationQCEntry tbody');
                $tbody.empty();
                //let html = '';
                for (let i = 0; i < data.length; i++) {
                    const html = buildRowHTML(i + 1, data[i]);
                    //html += buildRowHTML(i + 1, data[i]);
                    $tbody.append(html);
                }
                //$tbody.html(html);
                const tasks = [];
                for (let i = 0; i < data.length; i++) {
                    const selectedValues = {
                        supervisor: data[i].LAMSUP_CODE || null,
                        operator: data[i].LAMOP_CODE || null,
                        strength: data[i].TENA_CODE_A || null,
                        status: data[i].StatusCode || null,
                        shift: data[i].Shift || null
                    };

                    tasks.push(bindDropdownData(i + 1, selectedValues));
                }

                await Promise.all(tasks);
                setTimeout(() => {
                    $('#TxtNWarpWay1').focus();   // first row input
                }, 0);
                // for(let i=0; i < data.length; i++){
                //  setEnterKeyFocusOnTable?.(i + 1);
                // }

            } else {
                $('#tblLaminationQCEntry tbody').empty();
                showToast("No data found.", { type: "info" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Failed to load data.", { type: "error" });
        }
    });
}

//=====Add row
function buildRowHTML(rowNum, item) {
    return `
             <tr class="no-border-input" id="row${rowNum}">
                 <td style="display:none;">
                     <input type="hidden" id="TxtCode${rowNum}" name="TxtCode${rowNum}" value="${item.DOC_ID || ''}" />
                 </td>
                 <td><input type="text" class="form-control" id="TxtVNo${rowNum}" name="TxtVNo${rowNum}" value="${item.RefNo || ''}" readonly/></td>
                 <td>
                     <select class="form-control" id="ddlShift${rowNum}" name="ddlShift${rowNum}" disabled>
                         <option value=""></option>
                     </select>
                 </td>
                 <td><input type="text" class="form-control" id="TxtLamination${rowNum}" name="TxtLamination${rowNum}" value="${item.LamName || ''}" readonly/></td>
                 <td>
                     <select class="form-control" id="ddlSupervisor${rowNum}" name="ddlSupervisor${rowNum}">
                         <option value=""></option>
                     </select>
                 </td>
                 <td>
                     <select class="form-control" id="ddlOperator${rowNum}" name="ddlOperator${rowNum}">
                         <option value=""></option>
                     </select>
                 </td>
                 <td><input type="text" class="form-control" id="TxtItemName${rowNum}" name="TxtItemName${rowNum}" value="${item.ItemName || ''}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtMeter${rowNum}" name="TxtMeter${rowNum}" value="${item.Meter || '0'}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtNetWt${rowNum}" name="TxtNetWt${rowNum}" value="${parseFloat(item.NetWt || 0).toFixed(3)}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtFabricSize${rowNum}" name="TxtFabricSize${rowNum}" value="${parseFloat(item.UnlamSize || 0).toFixed(2)}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtUnLamGram${rowNum}" name="TxtUnLamGram${rowNum}" value="${parseFloat(item['Unlam Gram.'] || 0).toFixed(3)}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtCoating${rowNum}" name="TxtCoating${rowNum}" value="${item.Coating || '0'}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtLamGram${rowNum}" name="TxtLamGram${rowNum}" value="${parseFloat(item['Gram.'] || 0).toFixed(3)}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtLAMGSM${rowNum}" name="TxtLAMGSM${rowNum}" value="${item.GSM || '0'}" readonly/></td>
                 <td><input type="text" class="form-control" id="TxtRollNo${rowNum}" name="TxtRollNo${rowNum}" value="${item.RollNo || ''}" readonly/></td>
                 <td><input type="number" class="form-control" id="TxtNWarpWay${rowNum}" name="TxtNWarpWay${rowNum}" value="${parseFloat(item.NWarpWay).toFixed(2) || ''}" /></td>
                 <td><input type="number" class="form-control" id="TxtWarpWay${rowNum}" name="TxtWarpWay${rowNum}" value="${parseFloat(item.WarpWay).toFixed(2) || ''}" /></td>
                 <td><input type="number" class="form-control" id="TxtNWeftWay${rowNum}" name="TxtNWeftWay${rowNum}" value="${parseFloat(item.NWeftWay).toFixed(2) || ''}" /></td>
                 <td><input type="number" class="form-control" id="TxtWeftWay${rowNum}" name="TxtWeftWay${rowNum}" value="${parseFloat(item.WeftWay).toFixed(2) || ''}" /></td>
                 <td>
                     <select class="form-control" id="ddlStrength${rowNum}" name="ddlStrength${rowNum}">
                         <option value=""></option>
                     </select>
                 </td>
                 <td>
                     <select class="form-control" id="ddlStatus${rowNum}" name="ddlStatus${rowNum}">
                         <option value=""></option>
                     </select>
                 </td>
                 <td><input type="number" class="form-control" id="TxtElongWarp${rowNum}" name="TxtElongWarp${rowNum}" value="${parseFloat(item.Elong_Warp || 0).toFixed(2)}" /></td>
                 <td><input type="number" class="form-control" id="TxtElongWeft${rowNum}" name="TxtElongWeft${rowNum}" value="${parseFloat(item.Elong_Weft || 0).toFixed(2)}" /></td>
                 <td><input type="text" class="form-control" id="TxtRemarks${rowNum}" name="TxtRemarks${rowNum}" value="${item.Remarks || ''}" /></td>
                 <td><input type="text" class="form-control" id="TxtSupCode${rowNum}" name="TxtSupCode${rowNum}" value="${item.LAMSUP_CODE || ''}" readonly/></td>
                 <td><input type="text" class="form-control" id="TxtOpeCode${rowNum}" name="TxtOpeCode${rowNum}" value="${item.LAMOP_CODE || ''}" readonly/></td>
               </tr>
               `;
}

//=====Dropdowns
function bindDropdownData(rowCount, selectedValues = {}) {
    const supervisorSelect = `#ddlSupervisor${rowCount}`;
    const operatorSelect = `#ddlOperator${rowCount}`;
    const strengthSelect = `#ddlStrength${rowCount}`;
    const statusSelect = `#ddlStatus${rowCount}`;
    const shiftSelect = `#ddlShift${rowCount}`;

    return Promise.all([
        bindDropdown('LaminationQCEntry', 'Supervisor', supervisorSelect, 'Select Supervisor', selectedValues.supervisor, null, false, null, true),
        bindDropdown('LaminationQCEntry', 'Operator', operatorSelect, 'Select Operator', selectedValues.operator, null, false, null, true),
        bindDropdown('LaminationQCEntry', 'Strength', strengthSelect, 'Select Strength', selectedValues.strength, null, false, null, true),
        bindDropdown('LaminationQCEntry', 'Status', statusSelect, 'Select Status', selectedValues.status || 1, null, false, null, true),
        bindDropdown('LaminationQCEntry', 'Shift', shiftSelect, 'Select Shift', selectedValues.shift, null, false, null, true),
    ]);
}

//=====Doc Load
async function handleDocLoad() {
    const today = new Date();
    const todayDate = today.getFullYear() + '-' + (today.getMonth() + 1).toString().padStart(2, '0') + '-' + today.getDate().toString().padStart(2, '0');
    $('#DtDocDate').val(todayDate);
    $('#dtFromDate').val(todayDate);
    $('#dtToDate').val(todayDate);
    bindDropdown('LaminationQCEntry', 'Place', '#ddlPlace', 'Select Place', null, null, false, null, true);
    bindDropdown('LaminationQCEntry', 'Plant', '#ddlPlant', 'Select Plant', null, null, false, null, true);
}

//=====Update
function UpdateData(UpdateDt) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/LaminationQCEntry/UpdateLamination',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(UpdateDt),
            dataType: 'json',
            success: function (response) {
                if (response?.status) {
                    showToast(response.message || "Data updated successfully", { type: "success" });
                    resolve(response);
                } else {
                    showToast("Update failed: " + (response?.message || "Unknown error."), { type: "error" });
                    reject(response.message);
                }
            },
            error: function (xhr, status, error) {
                showToast("Data not updated: " + error, { type: "error" });
                reject(error);
            }
        });
    });
}

//=====Collect Data For Update
async function collectFormData() {
    const items = [];
    // const docid = $('#TxtDocId').val() || "0";
    $('#tblLaminationQCEntry tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);

        const warpWay = parseFloatSafe($r.find(`#TxtWarpWay${idx}`).val());
        const weftWay = parseFloatSafe($r.find(`#TxtWeftWay${idx}`).val());
        const nWarpWay = parseFloatSafe($r.find(`#TxtNWarpWay${idx}`).val());
        const docid = $r.find(`#TxtCode${idx}`).val();

        if ((warpWay > 0) && (weftWay > 0)) {
            const item = {
                Docid: docid,
                NWARPWAY_RES: nWarpWay,
                WARPWAY_RES: warpWay,
                NWEFTWAY_RES: parseFloatSafe($r.find(`#TxtNWeftWay${idx}`).val()),
                WEFTWAY_RES: weftWay,

                ELONG_WARP: parseFloatSafe($r.find(`#TxtElongWarp${idx}`).val()),
                ELONG_WEFT: parseFloatSafe($r.find(`#TxtElongWeft${idx}`).val()),

                QC_REMARKS: $r.find(`#TxtRemarks${idx}`).val() || null,

                STATUS_CODE_A: parseIntSafe($r.find(`#ddlStatus${idx}`).val()),
                TENA_CODE_A: parseIntSafe($r.find(`#ddlStrength${idx}`).val()),

                LAMSUP_CODE: parseIntSafe($r.find(`#TxtSupCode${idx}`).val()),
                LAMSUP_NAME: $r.find(`#ddlSupervisor${idx} option:selected`).text() || null,

                LAMOP_CODE: parseIntSafe($r.find(`#TxtOpeCode${idx}`).val()),
                LAMOP_NAME: $r.find(`#ddlOperator${idx} option:selected`).text() || null,

                // QCUSER: null 
            };

            items.push(item);
        }
    });

    return { LaminationDetails: items };
}

//=====Validation
function validate() {
    let isValid = true;
    const tbody = $('#tblLaminationQCEntry tbody');
    if (tbody.find('tr').length === 0) {
        showToast('No records to save.', { type: "warning" });
        isValid = false
        return isvalid;
    }
    tbody.find('tr').each(function () {
        const $row = $(this);
        const supervisorVal = $row.find('select[id^="ddlSupervisor"]');
        const operatorVal = $row.find('select[id^="ddlOperator"]');
        
        if (!validateRequiredField(supervisorVal, 'Supervisor Name') ||
            !validateRequiredField(operatorVal, 'Operator Name')) {
            isValid = false;
            return isValid;
        }
    });

    return isValid;
}

//=====Tenacity Check
async function processTenacityData() {
    debugger;
    const rows = $('#tblLaminationQCEntry tbody tr');

    for (let i = 0; i < rows.length; i++) {
        const $row = $(rows[i]);
        const warpWay = Math.round(parseFloat($row.find(`#TxtWarpWay${i + 1}`).val()) || 0);
        const weftWay = Math.round(parseFloat($row.find(`#TxtWeftWay${i + 1}`).val()) || 0);
        const strName = `${warpWay}-${weftWay}`;

        try {
            const response = await $.ajax({
                url: '/LaminationQCEntry/ProcessTenacityData',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    strName,
                    warpWay,
                    weftWay
                })
            });

            if (response.success) {
                // $row.find(`#ddlStrength${i + 1}`).val(response.tenaMaxcode);
                // $row.find(`#TxtTenacityCode${i + 1}`).val(response.tenaMaxcode);
                const ddl = $row.find(`#ddlStrength${i + 1}`);

                if (ddl.find(`option[value="${response.tenaMaxcode}"]`).length === 0) {
                    ddl.append(
                        new Option(strName, response.tenaMaxcode)
                    );
                }
                ddl.val(response.tenaMaxcode).trigger('change');
            } else {
                showToast(response.message, { type: "error" });
            }
        } catch (error) {
            showToast("Error processing tenacity data: " + error, { type: "error" });
        }
    }
}

//=====Enter Key Focus
function setEnterKeyFocus(sequence) {
    sequence.forEach((id, index) => {
        $(`#${id}`).on('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (index + 1 < sequence.length) {
                    $(`#${sequence[index + 1]}`).focus();
                }
            }
        });
    });
}

//=====Print Report

$('#btn_print').on('click', function () {
    const placeCode = $('#ddlPlace').val();
    if (!placeCode) {
        showToast("Please select place first!!", { type: "warning" });
        return;
    }
    LaminationQcReport();
})
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
function LaminationQcReport() {
    var reportName = "QC_LAM1";
    var plantCode = $('#ddlPlant').val();
    var placeCode = $('#ddlPlace').val();
    var d1 = $('#dtFromDate').val();
    var d2 = $('#dtToDate').val();


    var formula =
        "{Lamination.V_TYPE} = 'RLAM'" +
        " and {Lamination.v_date} in " +
        crystalDate(d1) + " to " + crystalDate(d2);

    // Optional Lamination Type filter (only if selected)
    if (plantCode && plantCode > 0) {
        formula += " and {Lamination.LAM_CODE} = " + parseInt(plantCode);
    }

    formula +=
        " and {Lamination.PLACE_CODE} = " + parseInt(placeCode) +
        " and {Lamination.COMP_CODE} = " + window.globalVariables.compCode +
        " and {Lamination.YEAR_CODE} = " + window.globalVariables.yearCode +
        " and {Lamination.BRANCH_CODE} = " + window.globalVariables.branchCode;

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "LAMINATION QUALITY REPORT"
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
//=========Check Modification Days
function checkModificationAllowed(selectedQC, placeCode, date, plantCode) {
    checkModificationDays({
        controller: 'LaminationQCEntry',
        vDate: date,
        onAllowed: function () {
            //AddOrEditFunction(rowId);
            GetdataBasedOnQC(selectedQC, placeCode, date, plantCode);
        }
    })
}