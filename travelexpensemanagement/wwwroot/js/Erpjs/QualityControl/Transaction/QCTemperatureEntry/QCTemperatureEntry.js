const allFieldIds = [
    "NumDocNo",
    "DtDocDate",
    "ddlShift",
    "TmTime",
    "ddlDenier",
    "ddlIncharge",
    "ddlOperator",
    "ddlSupervisor",
    "ddlPlantName",
    "TxtRemarks"
];
let docId = "", readOnly;
const vType = 'TAPE'

function getQueryParam(param) {
    return new URLSearchParams(window.location.search).get(param);
}

$(document).ready(function () {
    initPage();
});

async function initPage() {
    try {
        await handleDocLoad();
        $('#DtDocDate').focus();
        setEnterKeyFocus(allFieldIds);
        wireEvents();
    } catch (err) {
        showToast('Initialization failed: ' + err, { type: "error" });
    }
}
function wireEvents() {

    $('#btn-save').on('click', async (e) => {
        e.preventDefault();

        // Validate required fields
        const isValid = await Validate();
        if (!isValid) {
            return;
        }
        const rawValue = document.getElementById('DtDocDate')?.value;
        const V_DATE = formatDateToSqlDateOnly(parseDateSafe(rawValue));
        const V_TIME = getTimeAsDateTimeForSql("TmTime");
        const SHIFT = toNullableString(document.getElementById('ddlShift')?.value);
        const plantCode = parseIntSafe(document.getElementById('ddlPlantName')?.value);
        const VNo = parseIntSafe(document.getElementById('NumDocNo')?.value);
        const UpdateVno = docId ? VNo : 0;

        try {
            const formData = await collectFormData();
            if (docId) {
                UpdateData(formData);
            }
            else {
                checkExistOrNot(V_DATE, V_TIME, SHIFT, plantCode, UpdateVno)
                    .done(async function (data) {
                        if (data?.status && data?.exists) {
                            showToast("Duplicate Parameters VDate,VTime,Shift and Plant Name", { type: "warning" });
                            return;
                        }
                        SaveData(formData);
                    })
                    .fail(function () {
                        showToast("Error while checking Parameter name.", { type: "error" });
                    });
            }
        } catch (err) {
            showToast("Error saving data: " + err.message, { type: "error" });
        }
    });

    $('#tblWinder tbody').on('click', '.btn-winderelete-action', function () {
        const $tbody = $('#tblWinder tbody');
        // Prevent deleting if only one row exists
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        const isLastRow = $row.is(':last-child');
        $row.remove();
        if (isLastRow) {
            const $lastRow = $tbody.find('tr:last');
            if ($lastRow.length > 0 && $lastRow.find('.btn-add-Winder-action').length === 0) {
                $lastRow.find('td:last').prepend(
                    `<button class="act-btn add btn-add-action btn-add-Winder-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>`
                );
            }
        }
    });

    $('#tblMaterial tbody').on('click', '.btn-materialdelete-action', function () {
        const $tbody = $('#tblMaterial tbody');
        // Prevent deleting if only one row exists
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        const isLastRow = $row.is(':last-child');
        $row.remove();
        if (isLastRow) {
            const $lastRow = $tbody.find('tr:last');
            if ($lastRow.length > 0 && $lastRow.find('.btn-add-Material-action').length === 0) {
                $lastRow.find('td:last').prepend(
                    `<button class="act-btn add btn-add-action btn-add-Material-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>`
                );
            }
        }
    });

    $('#tblTestParameter tbody').on('click', '.btn-testParameterdelete-action', function () {
        const $tbody = $('#tblTestParameter tbody');
        // Prevent deleting if only one row exists
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        const isLastRow = $row.is(':last-child');
        $row.remove();
        if (isLastRow) {
            const $lastRow = $tbody.find('tr:last');
            if ($lastRow.length > 0 && $lastRow.find('.btn-add-TestParameter-action').length === 0) {
                $lastRow.find('td:last').prepend(
                    `<button class="act-btn add btn-add-action btn-add-TestParameter-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>`
                );
            }
        }
    });

    $(document).on('input', '[id^=TxtDenier], [id^=TxtBreakingLoad]', function () {
        var row = $(this).closest('tr');
        var denier = parseFloat(row.find(`[id^=TxtDenier]`).val() || 0);
        var breakingLoad = parseFloat(row.find(`[id^=TxtBreakingLoad]`).val() || 0);
        var tenaCityValue = (denier !== 0) ? ((breakingLoad / denier) * 1000) : 0;
        row.find(`[id^=TxtTeracityGpd`).val(tenaCityValue.toFixed(3));

    });

}
//===Check Exist===
function checkExistOrNot(V_DATE, V_TIME, SHIFT, plantCode, UpdateVno) {
    return $.ajax({
        url: '/QCTemperatureEntry/getExistOrNot',
        type: 'GET',
        dataType: 'json',
        data: { V_DATE: V_DATE, V_TIME: V_TIME, SHIFT: SHIFT, plantCode: plantCode, VNo: UpdateVno }
    });
}
//===Doc Load===
async function handleDocLoad() {
    docId = getQueryParam('id');
    readOnly = getQueryParam('readOnly');
    if (docId) {
        await GetDocData(docId, readOnly);

    } else {
        GetDocid();
        const today = new Date();
        const todayDate = today.getFullYear() + '-' +
            (today.getMonth() + 1).toString().padStart(2, '0') + '-' +
            today.getDate().toString().padStart(2, '0');
        $('#DtDocDate').val(todayDate);
        BindHeaderDropDown();
        if (window.compcode === "2" || window.compcode === "5") {
            fillPlanZoneTable(); 
        }
        else {
            addTestParameterRow();
        }
        fillScrewTable();
        addWinderRecordRow();
        addMaterialRecordRow();
        //---------------------------------------------------------------------------------
    }
}
//===Save & Update===
function SaveData(saveDt) {
    $.ajax({
        url: '/QCTemperatureEntry/SaveOrUpdateQcTemperatureEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                showToast("Data Insert successfully", { type: "success" });
                setFormReadOnly();
                readOnly = true;
            } else {
                showToast(response?.message || "Save failed. Please try again.", { type: "error" });
            }
        },
        error: function () {
            showToast("Error occurred while saving. Please contact admin.", { type: "error" });
        }
    });
}
function UpdateData(UpdateDt) {
    $.ajax({
        url: '/QCTemperatureEntry/SaveOrUpdateQcTemperatureEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(UpdateDt),
        dataType: 'json',
        success: function (response) {
            if (response?.status) {
                showToast("Data Update successfully", { type: "success" });
                setFormReadOnly();
                readOnly = true;
            } else {
                showToast("Update failed: " + (response?.message || "Unknown error."), { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Data not updated: " + error, { type: "error" });
        }
    });
}
//===Fill Data By Id===
async function fillHeaderData(headdata) {
    if (!Array.isArray(headdata) || headdata.length === 0) {
        console.error("Invalid or empty header data:", headdata);
        showToast("No header data found to populate the form.", { type: "error" });
        return;
    }
    const data = headdata[0];
    $("#TxtDocId").val(data.DOC_ID ?? "");
    $("#NumDocNo").val(data.V_NO ?? "");
    $("#DtDocDate").val(data.V_DATE ? data.V_DATE.substring(0, 10) : "");
    $("#TmTime").val(data.V_TIME ? data.V_TIME.substring(0, 5) : "");
    $("#TxtRemarks").val(data.REMARK ?? "");
    BindHeaderDropDown({
        inchargeId: data.INCH_CODE ?? 0,
        supervisorId: data.SUP_CODE ?? 0,
        operatorId: data.OPERATORE_CODE ?? 0,
        shiftId: data.SHIFT ?? "",
        denierId: data.DENIER ?? 0,
        plantId: data.DEPT_CODE ?? 0
    });
}

async function fillWinderData(data) {
    const windertable = $('#tblWinder tbody');
    windertable.empty();

    const windItems = data.filter(item => item.TYPE === "WIND");

    for (let index = 0; index < windItems.length; index++) {
        const item = windItems[index];
        const idx = index + 1;

        addWinderRecordRow();
        const winderSelect = (`#ddlWinder${idx}`);
        await loadDropdown({
            type: 'Winder',
            selectElem: winderSelect,
            defaultText: "- Select Winder -",
            selectedValue: item.CODE
        });

        $(`#ddlWinder${idx}`).val(item.WINDER_CODE).trigger('change');
        //$(`#TxtWidth${idx}`).val(item.WIDTH_MM);
        //$(`#TxtDenier${idx}`).val(item.DENIER);
        //$(`#TxtBreakingLoad${idx}`).val(item.BREAKING_LOAD);
        //$(`#TxtTeracityGpd${idx}`).val(item.TENACITY);
        //$(`#TxtElongation${idx}`).val(item.ELONGATION);
        $(`#TxtWidth${idx}`).val(
            item.WIDTH_MM != null ? parseFloat(item.WIDTH_MM).toFixed(2) : ''
        );

        $(`#TxtDenier${idx}`).val(
            item.DENIER != null ? parseFloat(item.DENIER).toFixed(2) : ''
        );

        $(`#TxtBreakingLoad${idx}`).val(
            item.BREAKING_LOAD != null ? parseFloat(item.BREAKING_LOAD).toFixed(2) : ''
        );

        $(`#TxtTeracityGpd${idx}`).val(
            item.TENACITY != null ? parseFloat(item.TENACITY).toFixed(2) : ''
        );

        $(`#TxtElongation${idx}`).val(
            item.ELONGATION != null ? parseFloat(item.ELONGATION).toFixed(2) : ''
        );
    }
}

async function fillMaterialData(data) {
    const materialTable = $('#tblMaterial tbody');
    materialTable.empty();

    const materialItems = data.filter(item => item.TYPE === 'ITEM');

    for (let index = 0; index < materialItems.length; index++) {
        const item = materialItems[index];
        const idx = index + 1;

        addMaterialRecordRow();
        const materialSelect = (`#ddlMaterial${idx}`);
        await loadDropdown({
            type: 'Material',
            selectElem: materialSelect,
            defaultText: "- Select Material -",
            selectedValue: item.CODE
        });

        $(`#ddlMaterial${idx}`).val(item.MAT_CODE).trigger('change');
        $(`#TxtLot${idx}`).val(item.GRADE);
        $(`#TxtNoOfBags${idx}`).val(item.NO_OF_BAGS);
        $(`#TxtPercentage${idx}`).val(
            item.MAT_PER != null ? parseFloat(item.MAT_PER).toFixed(2) : ''
        );
        //$(`#TxtPercentage${idx}`).val(item.MAT_PER);
    }
}
async function fillTestParameterData(data) {
    const testParamTable = $('#tblTestParameter tbody');
    testParamTable.empty();

    const materialItems = data.filter(item => (item.TYPE || item.type) === 'ROOM');

    for (let index = 0; index < materialItems.length; index++) {
        const item = materialItems[index];
        const idx = index + 1;

        addTestParameterRow();
        const ParamSelect = (`#TxtPlantZoneId${idx}`);
        await loadDropdown({
            type: 'TestParameter',
            selectElem: ParamSelect,
            defaultText: "- Select Parameter -",
            selectedValue: item.CODE
        });

        $(`#TxtPlantZoneId${idx}`).val(item.ROOM_CODE || item.rooM_CODE || '').trigger('change');
        $(`#TxTemperature${idx}`).val(item.temp_READ != null
            ? item.temp_READ.toFixed(2)
            : '0.00');
        $(`#TxtRemark${idx}`).val(item.TEMP_REM || '');
        $(`#TxtDateTime${idx}`).val(item.TIME_TAKEN
            ? formatDateToSqlDatetime(item.TIME_TAKEN)
            : '');
    }
}

//===Collect Data For Save & Update===
async function collectFormData() {

    const rawValue = document.getElementById('DtDocDate')?.value;
    const V_DATE = formatDateToSqlDateOnly(parseDateSafe(rawValue));

    const winderItem = await collectWinderData();
    const materialItem = await collectMaterialData();
    const screwItem = await collectScrewData();
    const plantItems = await collectPlantZodeData();
    const DOC_ID = toNullableString(docId);
    const allTapeQualityItems = [
        ...(winderItem || []),
        ...(screwItem || []),
        ...(materialItem || []),
        ...(plantItems || [])
    ];

    const data = {
        V_TYPE: vType,
        V_NO: parseIntSafe(document.getElementById('NumDocNo')?.value),
        V_DATE: V_DATE,
        SHIFT: toNullableString(document.getElementById('ddlShift')?.value),
        V_TIME: getTimeAsDateTimeForSql("TmTime"),
        DENIER: parseDecimalSafe(document.getElementById('ddlDenier')?.value),
        INCH_CODE: parseIntSafe(document.getElementById('ddlIncharge')?.value),
        OPERATORE_CODE: parseIntSafe(document.getElementById('ddlOperator')?.value),
        SUP_CODE: parseIntSafe(document.getElementById('ddlSupervisor')?.value),
        DEPT_CODE: (window.compcode === "2" || window.compcode === "5") ?
            parseIntSafe(document.getElementById('ddlPlantName')?.value) :
            parseIntSafe(document.getElementById('ddlLineNo')?.value) ,
        REMARK: toNullableString(document.getElementById('TxtRemarks')?.value),
        SaveOrUpdate: (!DOC_ID || DOC_ID === "") ? "Save" : "Update",
        TapeQualitys: allTapeQualityItems

    };
    return data;
}

async function collectPlantZodeData() {
    const plantItems = [];
    let tbl = getPlantTblTbody();
    $(tbl).each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);

        const item = {
            SNO: parseIntSafe(idx),
            TYPE: 'ROOM',
            V_DATE: null,
            ROOM_CODE: parseIntSafe($r.find(`#TxtPlantZoneId${idx}`).val()),
            TEMP_READ: parseFloatSafe($r.find(`#TxTemperature${idx}`).val()),
            TEMP_REM: toNullableString($r.find(`#TxtRemark${idx}`).val()),
            SPEED_CODE: null,
            SPEED_READ: null,
            SPEED_READ2: null,
            WINDER_CODE: null,
            WIDTH_MM: null,
            DENIER: null,
            BREAKING_LOAD: null,
            TENACITY: null,
            ELONGATION: null,
            MAT_CODE: null,
            GRADE: null,
            NO_OF_BAGS: null,
            MAT_PER: null,
            TIME_TAKEN: formatDateToSqlDatetime($r.find(`#TxtDateTime${idx}`).val())
        };

        plantItems.push(item);
    });

    return plantItems;
}

async function collectScrewData() {
    const screwItems = [];

    $('#tblScrew tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);

        const item = {
            SNO: parseIntSafe(idx),
            TYPE: 'SPED',
            V_DATE: null,
            ROOM_CODE: null,
            TEMP_READ: null,
            TEMP_REM: toNullableString($r.find(`#TxtRemark1${idx}`).val()),
            SPEED_CODE: parseIntSafe($r.find(`#TxtScrewId${idx}`).val()),
            SPEED_READ: parseFloatSafe($r.find(`#TxSpeed${idx}`).val()),
            SPEED_READ2: null,
            WINDER_CODE: null,
            WIDTH_MM: null,
            DENIER: null,
            BREAKING_LOAD: null,
            TENACITY: null,
            ELONGATION: null,
            MAT_CODE: null,
            GRADE: null,
            NO_OF_BAGS: null,
            MAT_PER: null,
            TIME_TAKEN: formatDateToSqlDatetime($r.find(`#TxtDateTime${idx}`).val())
        };

        screwItems.push(item);
    });

    return screwItems;
}

async function collectWinderData() {
    const winderItems = [];
    $('#tblWinder tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);
        var winderId = parseIntSafe($r.find(`#ddlWinder${idx}`).val() || 0);

        if (winderId) {
            const item = {
                SNO: parseIntSafe(idx),
                TYPE: 'WIND',
                V_DATE: null,
                ROOM_CODE: null,
                TEMP_READ: null,
                TEMP_REM: null,
                SPEED_CODE: null,
                SPEED_READ: null,
                SPEED_READ2: null,
                WINDER_CODE: winderId,
                WIDTH_MM: parseFloatSafe($r.find(`#TxtWidth${idx}`).val()),
                DENIER: parseFloatSafe($r.find(`#TxtDenier${idx}`).val()),
                BREAKING_LOAD: parseFloatSafe($r.find(`#TxtBreakingLoad${idx}`).val()),
                TENACITY: parseFloatSafe($r.find(`#TxtTeracityGpd${idx}`).val()),
                ELONGATION: parseFloatSafe($r.find(`#TxtElongation${idx}`).val()),
                MAT_CODE: null,
                GRADE: null,
                NO_OF_BAGS: null,
                MAT_PER: null,
                TIME_TAKEN: null
            };
            winderItems.push(item);
        }

    });
    return winderItems;
}

async function collectMaterialData() {
    const materialItems = [];
    $('#tblMaterial tbody tr').each(function () {
        const idx = this.id.replace('row', '');
        const $r = $(this);
        var materialId = parseIntSafe($r.find(`#ddlMaterial${idx}`).val() || 0);
        if (materialId) {
            const item = {
                SNO: parseIntSafe(idx),
                TYPE: 'ITEM',
                V_DATE: null,
                ROOM_CODE: null,
                TEMP_READ: null,
                TEMP_REM: null,
                SPEED_CODE: null,
                SPEED_READ: null,
                SPEED_READ2: null,
                WINDER_CODE: null,
                WIDTH_MM: null,
                DENIER: null,
                BREAKING_LOAD: null,
                TENACITY: null,
                ELONGATION: null,
                MAT_CODE: materialId,
                GRADE: toNullableString($r.find(`#TxtLot${idx}`).val()),
                NO_OF_BAGS: parseFloatSafe($r.find(`#TxtNoOfBags${idx}`).val()),
                MAT_PER: parseFloatSafe($r.find(`#TxtPercentage${idx}`).val()),
                TIME_TAKEN: null
            };
            materialItems.push(item);
        }

    });
    return materialItems;
}

//===Doc Details===
function GetDocid() {
    $.ajax({
        url: '/QCTemperatureEntry/GetMaxVNo',
        type: 'GET',
        data: { vType: vType/*, tableName: 'TAPE_QUALITY1'*/ },
        success: function (response) {
            if (response.status === true && response.vNo) {
                $('#NumDocNo').val(response.vNo || '');
                $('#TxtDocId').val(response.docId || '');
            } else {
                $('#txtDocNo').val('');
                $('#TxtDocId').val('');
            }
        },
        error: function (xhr, status, error) {
            showToast('Error fetching Doc ID:' + error, { type: "error" });
        }
    });
}
async function GetDocData(MasterTblId, readOnly) {
    try {
        const response = await $.ajax({
            url: '/QCTemperatureEntry/GetQcTemperatureById',
            type: 'GET',
            data: { id: MasterTblId }
        });
        if (response.status) {
            await fillHeaderData(response.header);
            await fillWinderData(response.detail);
            await fillMaterialData(response.detail);
            if (window.compcode === "2" || window.compcode === "5") {
                fillPlanZoneTable(response.detail, false);
            }
            else {
                fillTestParameterData(response.detail);
            }
            fillScrewTable(response.detail, false);
            
            // await fillItemDetailTable(response.detail);
            if (readOnly === 'true') {
                setFormReadOnly();
            }
        } else {
            showToast('No data returned.', { type: "error" });
        }
    } catch (error) {
        showToast('Failed to load data.', { type: "error" });
        console.error(error);
    }
}

//===Fill Plant Zone and Screw Table====
function fillPlanZoneTable(data = null, isImported = false) {
    $.ajax({
        url: '/QCTemperatureEntry/GetPlantZoneList',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                const tbody = $('#tblPlantZone tbody');
                tbody.empty();

                // Filter the passed data to include only type ROOM
                const plantZoneValues = (data || []).filter(item => item.TYPE === 'ROOM');

                let rowCount = 1;
                const zonesToRender = isImported
                    ? response.data.filter(pz => plantZoneValues.some(p => p.ROOM_CODE === pz.CODE))
                    : response.data;
                zonesToRender.forEach(function (item) {
                    // Default empty values
                    let temperature = '';
                    let remark = '';
                    let datetime = '';

                    if (plantZoneValues.length > 0) {
                        const match = plantZoneValues.find(p => p.ROOM_CODE === item.CODE);

                        if (match) {
                            temperature = match.TEMP_READ != null
                                ? match.TEMP_READ.toFixed(2)
                                : '';
                            remark = match.TEMP_REM ?? '';
                            datetime = match.TIME_TAKEN
                                //? new Date(match.TIME_TAKEN).toISOString().slice(0, 16)
                                ? formatDateToSqlDatetime(match.TIME_TAKEN)
                                : '';
                        }
                    }
                    //addPlantZoneRecordRow()
                    const newRow = `
                            <tr class="no-border-input" id="row${rowCount}">
                                <td>
                                    <input type="hidden" class="form-control" id="TxtPlantZoneId${rowCount}" value="${item.CODE}" />
                                    <input type="text" class="form-control" id="TxtPlantZoneName${rowCount}" value="${item.NAME}" readonly />
                                </td>
                                <td><input type="text" class="form-control temperature-input" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxTemperature${rowCount}" value="${temperature}" /></td>
                                <td><input type="text" class="form-control" maxlength="100" id="TxtRemark${rowCount}" value="${remark}" /></td>
                                <td><input type="datetime-local" class="form-control" id="TxtDateTime${rowCount}" value="${datetime}" disabled/></td>
                            </tr>
                        `;
                    
                    tbody.append(newRow);
                    rowCount++;
                });
                setFocusInColumn('.temperature-input');
            } else {
                showToast("Failed to load plant zones.", { type: "error" });
            }
        },
        error: function () {
            showToast("Error while loading plant zones.", { type: "error" });
        }
    });
}
function fillScrewTable(data = null, isImported = false) {
    $.ajax({
        url: '/QCTemperatureEntry/GetScrewList',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                const tbody = $('#tblScrew tbody');
                tbody.empty();
                const screwValues = (data || []).filter(item => item.TYPE === 'SPED');

                let rowCount = 1;
                const screwsToRender = isImported
                    ? response.data.filter(s => screwValues.some(p => p.SPEED_CODE === s.CODE))
                    : response.data;
                screwsToRender.forEach(function (item) {
                    let speed = '';
                    let remark = '';
                    let datetime = '';

                    if (screwValues.length > 0) {
                        const match = screwValues.find(p => p.SPEED_CODE === item.CODE);

                        if (match) {
                            speed = match.SPEED_READ != null
                                ? match.SPEED_READ.toFixed(2)
                                : '';
                            remark = match.TEMP_REM ?? '';
                            datetime = match.TIME_TAKEN
                                //? new Date(match.TIME_TAKEN).toISOString().slice(0, 16)
                                ? formatDateToSqlDatetime(match.TIME_TAKEN)
                                : '';
                        }
                    }

                    const newRow = `
                            <tr class="no-border-input" id="row${rowCount}">
                                <td>
                                    <input type="hidden" class="form-control" id="TxtScrewId${rowCount}" value="${item.CODE}" />
                                    <input type="text" class="form-control" id="TxtScrewName${rowCount}" value="${item.NAME}" readonly />
                                </td>
                                <td><input type="text" class="form-control speed-input" maxlength="10" oninput="allowOnlyDecimal(this)" id="TxSpeed${rowCount}" value="${speed}" /></td>
                                <td><input type="text" class="form-control" maxlength="100" id="TxtRemark1${rowCount}" value="${remark}" /></td>
                                <td><input type="datetime-local" class="form-control" id="TxtDateTime1${rowCount}" value="${datetime}" disabled/></td>
                            </tr>
                        `;
                    tbody.append(newRow);
                    rowCount++;
                });
                setFocusInColumn('.speed-input');
            } else {
                showToast("Failed to load screws.", { type: "error" });
            }
        },
        error: function () {
            showToast("Error while loading screw data.", { type: "error" });
        }
    });
}
//========================================
//          Add Rows
//========================================

    //===WINDER===
function addWinderRecordRow() {
    const tbody = $('#tblWinder tbody');
    tbody.find('.btn-add-action').remove();
    const rowCount = tbody.find('tr').length + 1;

    const newRow = `
            <tr class="no-border-input" id="row${rowCount}">
                <td>
                    <select class="form-control" style="width:400px;" id="ddlWinder${rowCount}">
                        <option value="">- Select Winder -</option>
                    </select>
                </td>
                <td><input type="text" class="form-control" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxtWidth${rowCount}" /></td>
                <td><input type="text" class="form-control" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxtDenier${rowCount}" /></td>
                <td><input type="text" class="form-control" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxtBreakingLoad${rowCount}" /></td>
                <td><input type="text" class="form-control" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxtTeracityGpd${rowCount}" /></td>
                <td><input type="text" class="form-control" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxtElongation${rowCount}" /></td>
                <td class="action-col">   
                    <button class="act-btn add btn-add-action btn-add-Winder-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                    <button class="act-btn delete btn-delete-action btn-winderelete-action" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                </td>
            </tr>
        `;

    tbody.append(newRow);

    const winderSelect = (`#ddlWinder${rowCount}`);

    

    loadDropdown({
        type: 'Winder',
        selectElem: winderSelect,
        defaultText: "- Select Winder -"
    });
}

    //===MATERIAL===
function addMaterialRecordRow() {
    const tbody = $('#tblMaterial tbody');
    tbody.find('.btn-add-action').remove();
    const rowCount = tbody.find('tr').length + 1;

    const newRow = `
            <tr class="no-border-input" id="row${rowCount}">
                <td>
                    <select class="form-control" style="width:400px;" id="ddlMaterial${rowCount}">
                        <option value="">- Select Material -</option>
                    </select>
                </td>
                <td><input type="text" class="form-control" maxlength="30" id="TxtLot${rowCount}" /></td>
                <td><input type="text" class="form-control" maxlength="7" oninput="allowOnlyNumbers(this)" id="TxtNoOfBags${rowCount}" /></td>
                <td><input type="text" class="form-control" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxtPercentage${rowCount}" /></td>
                <td class="action-col">   
                    <button class="act-btn add btn-add-action btn-add-Material-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                    <button class="act-btn delete btn-delete-action btn-materialdelete-action" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                </td>
            </tr>
        `;

    tbody.append(newRow);

    const materialSelect = (`#ddlMaterial${rowCount}`);
    loadDropdown({
        type: 'Material',
        selectElem: materialSelect,
        defaultText: "- Select Material -"
    });
}

    //====TEST PARAMETER===
function addTestParameterRow() {
        const tbody = $('#tblTestParameter tbody');
        tbody.find('.btn-add-action').remove();
        const rowCount = tbody.find('tr').length + 1;

        const newRow = `
            <tr class="no-border-input" id="row${rowCount}">
                <td>
                    <select class="form-control" style="width:400px;" id="TxtPlantZoneId${rowCount}">
                        <option value="">- Select Parameter -</option>
                    </select>
                </td>
               <td><input type="text" class="form-control temperature-input" maxlength="9" oninput="allowOnlyDecimal(this)" id="TxTemperature${rowCount}"/></td>
                                <td><input type="text" class="form-control" maxlength="100" id="TxtRemark${rowCount}"/></td>
                <td><input type="datetime-local" class="form-control" id="TxtDateTime${rowCount}" disabled/></td>
                <td class="action-col">   
                    <button class="act-btn add btn-add-action btn-add-TestParameter-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                    <button class="act-btn delete btn-delete-action btn-testParameterdelete-action" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                </td>
            </tr>
        `;

        tbody.append(newRow);

        const testParamSelect = (`#TxtPlantZoneId${rowCount}`);
        loadDropdown({
            type: 'TestParameter',
            selectElem: testParamSelect,
            defaultText: "- Select Parameter -"
        });
    }

//===Add Row
$(document).on('click', '.btn-add-Winder-action', function () {
    addWinderRecordRow();
});
$(document).on('click', '.btn-add-Material-action', function () {
    addMaterialRecordRow();
});
$(document).on('click', '.btn-add-TestParameter-action', function () {
    addTestParameterRow();
});
//========================================
//             DROPDOWNS
//========================================

//===Header Dropdowns===
async function BindHeaderDropDown(data = {}) {
    let operatorPlaceholder = '';
    if (window.compcode === "2" || window.compcode === "5") {
        operatorPlaceholder = '--Select Operator Name--';
    }
    else {
        operatorPlaceholder = '--Select Chemist Name--';
    }
    await Promise.all([
        bindDropdown('QCTemperatureEntry', 'Employee', '#ddlIncharge', '-- Select Incharge Name --', data.inchargeId || null, null, false, null, true),
        bindDropdown('QCTemperatureEntry', 'Employee', '#ddlSupervisor', '-- Select Supervisor Name --', data.supervisorId || null, null, false, null, true),
        bindDropdown('QCTemperatureEntry', 'Employee', '#ddlOperator', operatorPlaceholder, data.operatorId || null, null, false, null, true),
        bindDropdown('QCTemperatureEntry', 'Shift', '#ddlShift', '-- Select Shift --', data.shiftId || null, null, true, null, false),
        bindDropdown('QCTemperatureEntry', 'Denier', '#ddlDenier', '-- Select Denier --', data.denierId || null, null, false, null, true),
        bindDropdown('QCTemperatureEntry', 'Plant', '#ddlPlantName', '-- Select Plant --', data.plantId || null, null, false, null, true),
        //----------------------------------------------------------------------------------------------------------------------------------
        bindDropdown('QCTemperatureEntry', 'Line', '#ddlLineNo', '-- Select Line --', data.plantId || null, null, false, null, true),
        //bindDropdown('QCTemperatureEntry', 'Employee', '#ddlChemist', '-- Select Chemist Name --', data.chemistId || null, null, false, null, true),
    ]);
}

//===Table dropdowns
async function loadDropdown({ type, selectElem, defaultText = "- Select -"/*, formatter*/, selectedValue = null }) {
    await bindDropdown('QCTemperatureEntry', type, selectElem, defaultText, selectedValue, null, false, null, true);
}

//===Set Readonly
function setFormReadOnly() {
    const form = $('#QCTemperatureEntryForm');
    form.find('input, textarea, select').prop('disabled', true);
    $('#btn_import').prop('disabled', true).css('pointer-events', 'none');
    $('#btn-save').hide();
    $('.btn-delete-action, .btn-add-Winder-action, .btn-add-Material-action, .btn-add-TestParameter-action, #btn_fill')
        .addClass('disabled')
        .css('pointer-events', 'none');
    form.addClass('erppage-readonly');
}

//========================================
//             VALIDATIONS
//========================================

async function Validate() {
    let operatorToast = '';
    if (window.compcode === "2" || window.compcode === "5") {
        operatorToast = 'Operator Name';
    }
    else {
        operatorToast = 'Chemist Name';
    }
    let isValid = true
    if (
        !validateRequiredField('#NumDocNo', 'Doc No') ||
        !validateRequiredField('#DtDocDate', 'Doc Date') ||
        !validateRequiredField('#ddlShift', 'Shift Type') ||
        !validateRequiredField('#TmTime', 'Time') ||
        // !validateRequiredField('#ddlDenier', 'Denier')
        !validateRequiredField('#ddlOperator', operatorToast)
    ) {
        isValid = false;
        return isValid;
    }
    if (window.compcode === "2" || window.compcode === "5") {
        if (!validateRequiredField('#ddlPlantName', 'Plant Name')) {
            isValid = false;
            return isValid;
        }
    }
    else {
        if (!validateRequiredField('#ddlLineNo', 'Line Name')) {
            isValid = false;
            return isValid;
        } 
    }
    const checkValidation = await checkValidDate();
    if (checkValidation == false) {
        isValid = false;
        return isValid;
    }
    const isWinderValid = validateWinderTable();
    if (!isWinderValid) {
        isValid = false;
        return isValid;
    }
    const isMaterialValid = validateMaterialTable();
    if (!isMaterialValid) {
        isValid = false;
        return isValid;
    }
    const isPlantDuplicate = checkDuplicatePlantZones();
    if (isPlantDuplicate) {
        isValid = false;
        return isValid;
    }
    const isSpeedDuplicate = checkDuplicateSpeedItem();
    if (isSpeedDuplicate) {
        isValid = false;
        return isValid;
    }
    return isValid;
}

//===Validate VDate
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: vType,
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/QCTemperatureEntry/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;
    } catch (error) {
        console.error('Error:', error);
        return false;
    }
}

//===Validate Tables====
function validateWinderTable() {
    let isValid = true;
    const tbody = $('#tblWinder tbody');

    tbody.find('tr').each(function () {
        // Winder select
        const winderSelect = $(this).find('select[id^="ddlWinder"]');
        const winderValue = winderSelect.val();
        if (!winderValue) {
            setInvalid(winderSelect, "Winder name empty!");
            isValid = false;
        }
    });

    return isValid;
}
function validateMaterialTable() {
    let isValid = true;
    const tbody = $('#tblMaterial tbody');

    tbody.find('tr').each(function () {
        // Winder select
        const winderSelect = $(this).find('select[id^="ddlMaterial"]');
        const winderValue = winderSelect.val();
        if (!winderValue) {
            setInvalid(winderSelect, "Material name empty!");
            isValid = false;
        }
    });

    return isValid;
}
function validateMaterialTable() {
    let isValid = true;
    const tbody = $('#tblMaterial tbody');

    tbody.find('tr').each(function () {
        // Winder select
        const winderSelect = $(this).find('select[id^="ddlMaterial"]');
        const winderValue = winderSelect.val();
        if (!winderValue) {
            setInvalid(winderSelect, "Material name empty!");
            isValid = false;
        }
    });

    return isValid;
}

//========================================
//             Duplicate item
//========================================
function checkDuplicatePlantZones() {
    const seen = {}; // To track already seen Plant Zone Names
    let hasDuplicate = false;
    let tbl = getPlantTblTbody();
    $(tbl).each(function () {
        const nameInput = (window.compcode === "2" || window.compcode === "5") ?
                            $(this).find('input[id^="TxtPlantZoneName"]') :
                            $(this).find('select[id^="TxtPlantZoneId"]')                            ;
        const name = nameInput.val();
        
        // Skip empty names 
        if (!name) return;

        if (seen[name]) {
            hasDuplicate = true;
            setInvalid(nameInput, "Duplicate plant item!");

        } else {
            seen[name] = nameInput;
        }
    });

    return hasDuplicate; // true if duplicates exist
}
function checkDuplicateSpeedItem() {
    const seen = {}; // To track already seen Plant Zone Names
    let hasDuplicate = false;

    $('#tblScrew tbody tr').each(function () {
        const nameInput = $(this).find('input[id^="TxtScrewName"]');
        const name = nameInput.val()?.trim();

        // Skip empty names
        if (!name) return;

        if (seen[name]) {
            hasDuplicate = true;
            setInvalid(nameInput, "Duplicate Speed item!");

        } else {
            seen[name] = nameInput;
        }
    });

    return hasDuplicate; // true if duplicates exist
}

//===Temp and Speed Column Focus====
function setFocusInColumn(selector) {
    $(selector).on('keydown', function (e) {
        if (e.key === 'Enter' || e.key === 'Tab') {
            const inputs = $(selector);
            const index = inputs.index(this);

            // Check if this is NOT the last input
            if (index < inputs.length - 1) {
                e.preventDefault(); // prevent default only if not last
                const nextInput = inputs.eq(index + 1);
                nextInput.focus();
            }
            // If it's the last input, do nothing and let default behavior occur
        }
    });
}

//===Import===
$('#btn_import').on('click', function () {
    let timeInterval = parseIntSafe($('#TxtImportInterval').val());
    if (timeInterval <= 0) timeInterval = 180;
    let readingCode = parseIntSafe($('#ddlSelectReading').val());
    let type = (readingCode == 0) ? 'ROOM' :
               (readingCode == 1) ? 'SPED' :
               (readingCode == 2) ? 'WIND' : '';
    let shift = $('#ddlShift').val() || '';
    let deptCode = parseIntSafe($('#ddlPlantName').val());
    if (!deptCode) {
        showToast("Please select Plant!!", { type: "warning" });
        $('#ddlPlantName').focus();
    }
    getImportedData(timeInterval, type, shift, deptCode);
})
function getImportedData(timeInterval, type, shift, deptCode) {
    $.ajax({
        url: '/QCTemperatureEntry/ImportDataByReading',
        type: 'GET',
        dataType: 'JSON',
        data: { timeInterval: timeInterval, type: type, shift: shift, deptCode: deptCode, vType: vType },
        success: function (response) {
            if (response?.success) {
                if (response.data) {
                    if (type === 'ROOM') {
                        fillPlanZoneTable(response.data, true);
                    }
                    else if (type === 'SPED') {
                        fillScrewTable(response.data, true)
                    }
                    else if (type === 'WIND') {
                        fillWinderData(response.data);
                    }
                }
            }
        },
        error: function () {
            showToast("Error occurred while importing!", { type: "error" });
        }
    });
}

//===Fill===
function getTestParamDataToFill(deptCode) {
    $.ajax({
        url: '/QCTemperatureEntry/FillDataByLineNo',
        type: 'GET',
        dataType: 'JSON',
        data: { deptCode: deptCode },
        success: function (response) {
            if (response?.success) {
                if (response.data && response.data.length > 0) {
                    console.log(response.data);
                    fillTestParameterData(response.data)
                }
            } 
        },
        error: function () {
            showToast("Error occurred while fetching!", { type: "error" });
        }
    })
}
$('#btn_fill').on('click', function () {
    let deptCode = parseIntSafe($('#ddlLineNo').val());
    if (!deptCode) {
        showToast("Please select Line No!!", { type: "warning" });
        $('#ddlLineNo').focus();
    }
    getTestParamDataToFill(deptCode);
})

//========Helpers======
function getPlantTblTbody() {
    let tbl = '';
    if (window.compcode === "2" || window.compcode === "5") {
        tbl = '#tblPlantZone tbody tr';
    }
    else {
        tbl = '#tblTestParameter tbody tr';
    }
    return tbl;
}
function allowOnlyDecimal(input) {
    input.value = input.value
        .replace(/[^0-9.]/g, '')
        .replace(/(\..*)\./g, '$1');
}
function parseDateSafe(value) {
    const d = new Date(value);
    return isNaN(d.getTime()) ? null : d;
}
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
function formatDateToSqlDatetime(input) {
    if (!input) return null;

    const date = new Date(input);
    if (isNaN(date.getTime())) return null; // Handle invalid dates

    const pad = (n) => n < 10 ? '0' + n : n;
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());
    const seconds = pad(date.getSeconds());

    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
}
function formatDateToSqlDateOnly(date) {
    if (!date || !(date instanceof Date) || isNaN(date.getTime())) return null;

    const pad = (n) => n < 10 ? '0' + n : n;
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());

    return `${year}-${month}-${day}`;
}
function getTimeAsDateTimeForSql(inputId) {
    const timeString = document.getElementById(inputId)?.value;
    if (!timeString) return null;
    return `1990-01-01T${timeString}:00`; // e.g. "1970-01-01T11:53:00"

}