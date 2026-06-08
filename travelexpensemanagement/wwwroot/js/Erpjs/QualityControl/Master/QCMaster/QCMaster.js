let parameterOptionsHtml = '';
let PreviousInputDt = "";
let rowId = 0;
let isReadOnly = true;
let isEdit = false;


$(document).ready(function () {
    
    const code = getQueryParam('id');
    const mode = getQueryParam("mode");
    isReadOnly = mode === "view";

    rowId = code;
    $('#TxtName').focus();

    bindDropdown('QCMaster', 'QCGroup', '#ddlQCGroup', ' Select QC Group ', null, null, false, null, false);

    addRow();


    if (code) {
        isEdit = true;
        getQcDataByCode(code, isReadOnly);
    }
    //if (mode === "view") {
    //    setFormReadOnly();
    //}

    $('#ACTIVE').on('change', function () {
        let status = $(this).is(':checked') ? 'Active' : 'Inactive';
        $('#statusText').text(status);
    });

    // Submit main form
    $('#btnSave').on('click', async function (e) {
        e.preventDefault();

        if (!validateRequiredField('#TxtName', 'QC Test Name')) return;

        let isValid = await validateData();
        if (!isValid) {
            return;
        }
        const formData = CollectFormData();

        if (code) {
            EditMasterData(formData);
        }
        else {
            saveData(formData);
        }

    });

    //==========Save Deduct Rate==========
    $('#btnSaveQCDeductRate').on('click', function (e) {
        e.preventDefault();
        const deductRateData = collectDeductRateData();
        if (deductRateData) {
            saveDeductRate(deductRateData);
        }
        else {
            showToast("Failed to collect deduct rates!", { type: "error" });
        }
        
    })
})

function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}

//==========Get By Code========
function getQcDataByCode(code, isReadOnly) {
    $.ajax({
        url: '/QCMaster/GetQCMasterListByCode',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ code: parseInt(code) }),
        success: function (response) {
            bindItemDeptForm(response);
            if (isReadOnly) {
                setFormReadOnly();
            }
        },
        error: function (xhr) {
            showToast('Error: ' + xhr.responseText, {type:"error"});
        }
    });
}

//==========Fill Form========
function bindItemDeptForm(response) {
    if (!response.success || !response.data || !response.data.details) {
        showToast('No details found to bind.', { type:"warning" });
        return;
    }

    const data = response.data;

    console.log("data", data);

    $('#CODE').val(rowId);
    $('#TxtName').val(data.name);
    $('#TxtShortName').val(data.shortName);
    $('#TxtMaxPPM').val(data.maxPPM.toFixed(4));
    $('#ACTIVE').prop('checked', data.active === 1);
    // Load QCGroup dropdown then set value
    bindDropdown('QCMaster', 'QCGroup', '#ddlQCGroup', ' Select QC Group ', data.qcGroup, null, false, null, false);
    const details = data.details;
    const tbody = $('#tblQCMasteradd tbody');
    tbody.empty();
    details.forEach(function (item) {
        addRow(item);
    });
    PreviousInputDt = data.name;
}

//============DropDowns=========
function ddlParameter(selector, selectedValue = null) {
    const $ddl = $(selector);
    // Use cached options if already loaded
    if (parameterOptionsHtml !== '') {

        $ddl.html(parameterOptionsHtml);

        if (!$ddl.hasClass('select2-hidden-accessible')) {
            initSelect2($ddl)
        }

        $ddl.val(selectedValue || '').trigger('change');
        return;
    }
    $.ajax({
        url: '/QCMaster/GetddlParameter',
        type: 'GET',
        dataType: 'json',
        //data: { type: "Parameter"},
        success: function (response) {
            console.log("ddl Param data", response.data);
            if (response.success) {
                parameterOptionsHtml =
                    '<option value="">-- Select Parameter --</option>';

                $.each(response.data, function (i, item) {
                    parameterOptionsHtml += `<option value="${item.Code}" data-unit="${item.Unit}" data-ucode="${item.Ucode}">${item.Name}</option>`;
                });

                $ddl.html(parameterOptionsHtml);

                initSelect2($ddl)

                if (selectedValue && $ddl.find(`option[value="${selectedValue}"]`).length > 0) {
                    $ddl.val(selectedValue).trigger('change');
                } else {
                    $ddl.val('').trigger('change');
                }
            }
            else {
                showToast("Error in loading Parameters.", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading parameters:", error);
        }
    });
}

//=========Initialize Select2============
function initSelect2($ddl) {
    $ddl.select2({
        placeholder: '-- Select Parameter --',
        allowClear: true,
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

//============Add Row===========
function addRow(item = {}) {
    
    const moreBtn = isEdit
        ? `<button type="button" class="act-btn more erppage-dropdownaction-btn">
                    <i class="fa fa-minus-circle"></i>
                    </button>`
        : '';


    const tbody = $('#tblQCMasteradd tbody');
    tbody.find('.btn-add-action').remove();

    let row = `
        <tr class="no-border-input">
            <td class="d-none"><input type="text" name="Code" class="form-control txtCode" value="${item.code || ''}"/></td>
            <td>
                <select name="Parameter" class="form-control ddlParameter">
                   ${parameterOptionsHtml}
                </select>
            </td>
            <td><input type="text" name="Unit" class="form-control TxtUnit" value="${item.unit || ''}" readonly/></td>
            <td><input type="number" name="StdResult" class="form-control TxtStdResult" value="${item.stdResult != null ? parseFloat(item.stdResult).toFixed(4) : ''}"/></td>
            <td>
                <select name="DeductQty" class="form-control ddlDeductQty">
                    <option value="">- Deduct Qty -</option>
                    <option value="Fix">Fix</option>
                    <option value="%">%</option>
                    <option value="Wgt">Wgt</option>
                </select>
            </td>
            <td>
                <select name="DeductType" class="form-control ddlDeductType">
                    <option value="">- Deduct Type -</option>
                    <option value="Landed">Landed</option>
                    <option value="Fix">Fix</option>
                    <option value="ColDiff">ColDiff</option>
                    <option value="BasePrice">BasePrice</option>
                    <option value="BaseLanded">BaseLanded</option>
                    <option value="GraceBaseLanded">GraceBaseLanded</option>
                    <option value="NA">NA</option>
                </select>
            </td>
            <td>
                <select name="Ppm" class="form-control ddlPPM">
                    <option value=""> Select PPM </option>
                    <option value="YES">YES</option>
                    <option value="NO">NO</option>
                </select>
            </td>
            <td><input type="number" step="0.01" name="BasePrice" class="form-control TxtBasePrice" value="${item.basePrice != null ? parseFloat(item.basePrice).toFixed(4) : ''}"/></td>
            <td><input type="text" name="Remarks" class="form-control TxtRemarks" value="${item.remarks || ''}" maxlength="255"/></td>
            <td class="action-col">
                    <button class="act-btn add btn-add-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                    <button class="act-btn delete btn-delete-action" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                    ${moreBtn}  
            </td>
        </tr>`;

    tbody.append(row);

    //const $newDdl = $('#tblQCMasteradd tbody tr:last .ddlParameter');
    const $row = tbody.find('tr:last');

    //=========Bind dropdowns============
    ddlParameter($row.find('.ddlParameter'), item.parameterValue || null);
    $row.find('.ddlDeductQty').val(item.deductQty);
    $row.find('.ddlDeductType').val(item.deductType);
    $row.find('.ddlPPM').val(item.ppm);
}

//=======Add=========
$(document).on('click', '.btn-add-action', function () {
    addRow();
});

//=======Delete=========
$('#tblQCMasteradd tbody').on('click', '.btn-delete-action', function () {
    const $tbody = $('#tblQCMasteradd tbody');
    // Prevent deleting if only one row exists
    if ($tbody.find('tr').length === 1) {
        return;
    }
    const $row = $(this).closest('tr');
    const isLastRow = $row.is(':last-child');
    $row.remove();
    if (isLastRow) {
        const $lastRow = $tbody.find('tr:last');
        if ($lastRow.length > 0 && $lastRow.find('.btn-add-action').length === 0) {
            $lastRow.find('td:last').prepend(
                `<button class="act-btn add btn-add-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>`
            );
        }
    }
});

//========Fill Unit From Parameter========
$(document).on('change', '.ddlParameter', function () {
    const $row = $(this).closest('tr');

    const unit = $(this).find(':selected').data('unit') || '';

    $row.find('.TxtUnit').val(unit);
    setTimeout(() => {
        checkDuplicateParameters();
    }, 0);
});
//========Set Focus========
$(document).on('select2:select', '.ddlParameter', function () {

    const $row = $(this).closest('tr');
    
    setTimeout(() => {
        $row.find('.TxtStdResult').focus();
    }, 50);
});

//========Collect Form Data===========
function CollectFormData() {
    const formData = {
        code: rowId,
        Name: $('#TxtName').val() || '',
        ShortName: $('#TxtShortName').val() || '',
        QCGroup: parseInt($('#ddlQCGroup').val()) || 0,
        MaxPPM: parseFloat($('#TxtMaxPPM').val()) || 0,
        active: $('#ACTIVE').is(':checked') ? 1 : 0,
        Details: []
    };

    $('#tblQCMasteradd tbody tr').each(function () {
        const $row = $(this);
        formData.Details.push({
            Code: parseInt($row.find('.txtCode').val()) || 0,
            Parameter: parseInt($row.find('.ddlParameter').val()) || 0,
            Unit: parseInt($row.find(':selected').data('ucode')) || 0,
            StdResult: parseFloat($row.find('.TxtStdResult').val()) || 0,
            DeductQty: $row.find('.ddlDeductQty').val() || '',
            DeductType: $row.find('.ddlDeductType').val() || '',
            Ppm: $row.find('.ddlPPM').val() || '',
            BasePrice: parseFloat($row.find('.TxtBasePrice').val()) || 0,
            Remarks: $row.find('.TxtRemarks').val() || ''
        });
    });
    return formData;
}

//=========Save========
function saveData(formData) {

    const Name = formData?.Name?.trim();

    checkExistOrNot(Name)
        .done(function (data) {
            if (data?.status && data?.exists) {
                showToast("QC Test Name Already Exists.", { type: "warning" });
                return;
            }
            console.log("formData", formData);
            $.ajax({
                url: '/QCMaster/InsertDataQcMaster',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(formData),
                success: function (response) {
                    if (response.success) {
                        showToast('Data saved successfully!', { type: "success" });
                        setTimeout(() => {
                            window.location.href = '/QCMasterList/Index';
                        }, 1500)
                    }
                },
                error: function (xhr, status, error) {
                    showToast('Error: ' + error, { type: "error" });
                }
            });
        })
        .fail(function () {
            showToast("Error while checking QC Test name.", { type: "error" });
        });
}

//=======Edit============
function EditMasterData(masterData) {
    if (PreviousInputDt !== masterData.Name) {
        checkExistOrNot(masterData.Name)
            .done(function (data) {
                if (data?.status && data?.exists) {
                    showToast("QC Test Name Already Exists.", { type: "warning" });
                    return;
                }
                updateData(masterData);
            })
            .fail(function () {
                showToast("Error while checking QC Test Name.", { type: "error" });
            });
    } else {
        updateData(masterData);
    }
}
//=========Update========
function updateData(formData) {
    $.ajax({
        url: '/QCMaster/UpdateDataQcMaster',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function (response) {
            if (response.success) {
                showToast('Data Updated successfully!', { type: "success" });
                setTimeout(() => {
                    window.location.href = '/QCMasterList/Index';
                }, 1500)
            }
        },
        error: function (xhr, status, error) {
            showToast('Error: ' + error, {type:"error"});
        }
    });
}

//=========Check Exist========
function checkExistOrNot(inputData) {
    return $.ajax({
        url: '/QCMaster/getExistOrNot',
        type: 'GET',
        dataType: 'json',
        data: { inputData: inputData }
    });
}

//======Open Deduct Rate Modal============
$(document).on("click", ".erppage-dropdownaction-btn", function (e) {
    e.preventDefault();

    const $btn = $(this);
    const $row = $btn.closest("tr");

    $(".erppage-dropdownaction-menu").remove();

    // ===== Extract row values (same as your contextmenu logic) =====
    //const code = parseInt($row.find(".txtCode").val()) || 0;
    const code = $('#CODE').val();
    
    const parameterText =
        $row.find(".ddlParameter option:selected").text().trim() || "";

    const deductTypeText =
        $row.find(".ddlDeductType option:selected").text().trim() || "";

    const parameterId =
        parseInt($row.find(".ddlParameter").val()) || 0;

    //==========Check for NA======
    const deductType = $row.find(".ddlDeductType").val();
    if (!deductType || deductType === "NA") {
        showToast("Please select Deduct Type!", { type: "warning" })
        return;
    }

    // ===== Set modal values =====
    $("#lblParameterText").text(parameterText);
    $("#lblDeductTypeText").text(deductTypeText);
    $("#hdnCode").val(code);
    $("#hdnnextQcpCode").val(parameterId);

    console.log(code, parameterId);

    GetDeductdata(code, parameterId);

    
});

//============Get Deduct Rates==========
function GetDeductdata(code, parameterId) {
    $.ajax({
        url: '/QCMaster/CheckDeductRates',
        type: 'POST',
        data: JSON.stringify({ code: code, parameterId: parameterId }),
        contentType: 'application/json',
        success: function (response) {
            if (response.success) {
                if (response.data) {
                    showDeductRateListPopup(response.data);
                } else {
                    showDeductRateListPopup([]);
                }
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast('Error retrieving data: ' + error, {type:"error"});
        }
    });

}

//============Show Deduct Rates PopUp==========
function showDeductRateListPopup(deductRates) {
    const $tbody = $('#tblParameterDR tbody');
    $tbody.empty();
    if (!Array.isArray(deductRates) || deductRates.length === 0) {
        addDeductRateRows();
    } else {
        deductRates.forEach(rate => {
            addDeductRateRows(rate);
        });
    }
    
    // ===== Open modal =====
    const modal = new bootstrap.Modal(
        document.getElementById("qcparameterbtnadd")
    );
    modal.show();
}

//============Add Deduct Rate Rows==========
function addDeductRateRows(rate = {}) {
    const $tbody = $('#tblParameterDR tbody');
    $tbody.find('.btn-add-deductRate-row').remove();
    const fromVal = parseFloat(rate.fromResult || 0).toFixed(4);
    const toVal = parseFloat(rate.toResult || 0).toFixed(4);
    const rateVal = parseFloat(rate.deductRate || 0).toFixed(4);

    const rowHtml = `
                <tr>
                    <td><input type="number" class="form-control form-control-sm from-result" placeholder="From" value="${fromVal}"></td>
                    <td><input type="number" class="form-control form-control-sm to-result" placeholder="To" value="${toVal}"></td>
                    <td><input type="number" class="form-control form-control-sm deduct-rate" placeholder="Rate" value="${rateVal}"></td>
                    <td>
                        <select class="form-control form-control-sm deduct-type">
                            <option value="Base" ${rate.deductType === 'Base' ? 'selected' : ''}>Base</option>
                            <option value="Landed" ${rate.deductType === 'Landed' ? 'selected' : ''}>Landed</option>
                            <option value="Landed Half" ${rate.deductType === 'Landed Half' ? 'selected' : ''}>Landed Half</option>
                        </select>
                    </td>
                    <td>
                        <button type="button" class="btn btn-success btn-sm btn-add-row btn-add-deductRate-row"><i class="fa fa-plus"></i></button>
                        <button class="act-btn delete btn-delete-action btn-delete-Deduct" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                    </td>
                </tr>
            `;
    $tbody.append(rowHtml);
}

$(document).on('click', '.btn-add-deductRate-row', function () {
    if (!validateDeductRateOrder()) return;
    addDeductRateRows();
});

$('#tblParameterDR tbody').on('click', '.btn-delete-Deduct', function () {
    const $tbody = $('#tblParameterDR tbody');
    // Prevent deleting if only one row exists
    if ($tbody.find('tr').length === 1) {
        return;
    }
    const $row = $(this).closest('tr');
    const isLastRow = $row.is(':last-child');
    $row.remove();
    if (isLastRow) {
        const $lastRow = $tbody.find('tr:last');
        if ($lastRow.length > 0 && $lastRow.find('.btn-add-action').length === 0) {
            $lastRow.find('td:last').prepend(
                `<button type="button" class="btn btn-success btn-sm btn-add-row btn-add-deductRate-row"><i class="fa fa-plus"></i></button>`
            );
        }
    }
});
//============Collect Deduct Rates==========
function collectDeductRateData() {
    const code = parseInt($('#hdnCode').val()) || 0;
    const nextQcpCode = parseInt($('#hdnnextQcpCode').val()) || 0;
    const ded_type = $('#lblDeductTypeText').val() || '';
    let rowData = [];
    $('#tblParameterDR tbody tr').each(function () {
        const from = $(this).find('.from-result').val();
        const to = $(this).find('.to-result').val();
        const rate = $(this).find('.deduct-rate').val();
        const type = $(this).find('.deduct-type').val() || '';
        rowData.push({
            From: parseFloat(from) || 0,
            To: parseFloat(to) || 0,
            Rate: parseFloat(rate) || 0,
            Type: type,
            Code: code,
            nextQcpCode: nextQcpCode,
            ded_type: ded_type
        });
    });
    return rowData;
}

//============Save Deduct Rates==========
function saveDeductRate(rowData) {
    let isValidOrder = validateDeductRateOrder();
    if (!isValidOrder) {
        return;
    }
    const modalEl = document.getElementById("qcparameterbtnadd");
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    $.ajax({
        url: '/QCMaster/SaveDeductRates',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(rowData),
        success: function (response) {
            if (response.success) {
                showToast('Data saved successfully!', { type:"success" });
                if (modalInstance) {
                    modalInstance.hide();
                }
            }
            else {
                if (modalInstance) {
                    modalInstance.hide();
                }
                showToast('Failed to save deduct rate: ' + response.message, { type:"error" });
            }
        },
        error: function (xhr, status, error) {
            showToast('Error saving data: ' + error, { type:"error" });
        }
    });
}

//===========Validation==============
async function validateData() {

    try {

        // No Record To Save
        const rows = $('#tblQCMasteradd tbody tr');

        let hasData = false;

        rows.each(function () {

            const parameter = $(this).find('.ddlParameter').val();

            if (parameter && parameter !== '') {
                hasData = true;
                return false; // break loop
            }
        });

        if (!hasData) {
            showToast('No Record to save.', { type: 'warning' });
            return false;
        }

        let isDuplicate = checkDuplicateParameters();
        if (isDuplicate) {
            return false;
        }

        let ppmYesFound = false;

        // Row Validation
        for (let i = 0; i < rows.length; i++) {

            const row = $(rows[i]);
            const parameterInput = row.find('.ddlParameter');
            const basePriceInput = row.find('.TxtBasePrice');

            const code = row.find('.txtCode').val();
            const parameter = $(parameterInput).val();
            const parameterName = $(parameterInput).find("option:selected").text();
            const qcpCode = parseInt(row.find('.txtCode').val() || 0);
            const deductType = row.find('.ddlDeductType').val();
            const ppm = row.find('.ddlPPM').val();
            const basePrice = parseFloat($(basePriceInput).val()) || 0;

            // Base Price Validation
            if (qcpCode > 0) {

                if (deductType !== 'NA' && deductType !== '') {

                    let result = await checkDeductRateExist(code, parameter);

                    if (!result.exists) {
                        setInvalid(parameterInput,`Deduct Rate is not filled against ${parameterName} where Deduct type other than 'NA'.`);
                        return false;
                    }

                    if (deductType === 'BasePrice' && basePrice <= 0) {
                        setInvalid(basePriceInput, 'Base Price must be greater than 0, if Deduct type is BasePrice.');
                        row.find('.TxtBasePrice').focus();
                        return false;
                    }
                }
            }

            // PPM Validation
            if (parameter && ppm === 'YES') {
                ppmYesFound = true;
            }
        }

        // MAX PPM Validation
        const maxPPMInput = $('#TxtMaxPPM');
        const maxPPM = parseFloat($(maxPPMInput).val()) || 0;

        if (maxPPM > 0 && !ppmYesFound) {
            showToast('PPM Yes/No must be Yes in any of the Grid Row.', { type: "warning" });
            return false;
        }
        else if (maxPPM <= 0 && ppmYesFound) {
            setInvalid(maxPPMInput,'Max PPM should be greater than 0 if PPM Yes/No is Yes.', { type: "warning" });
            return false;
        }

        return true;
    }
    catch (ex) {
        console.error(ex);
        return false;
    }
}

//===========CHeck Duplicate=======
function checkDuplicateParameters() {
    const seen = {}; 
    let hasDuplicate = false;
    let rows = $('#tblQCMasteradd tbody tr');
    $(rows).each(function () {
        const paramInput = $(this).find('.ddlParameter');
        const param = paramInput.val();

        // Skip empty names 
        if (!param) return;

        if (seen[param]) {
            hasDuplicate = true;
            setInvalid(paramInput, "Duplicate Parameter!");

        } else {
            seen[param] = paramInput;
        }
    });

    return hasDuplicate; 
}

//============Check Deduct Rate Existence=========
function checkDeductRateExist(code, qcpCode) {

    return $.ajax({
        url: '/QCMaster/CheckDeductRateExist',
        type: 'GET',
        dataType: 'json',
        data: {
            code: code,
            qcpCode: qcpCode
        }
    });
}

//============Readonly==========
function setFormReadOnly() {
    const form = $('#QCMasterform');
    
    $('#QCMasterform input, #QCMasterform select').prop('disabled', true);
    $('#customToggle').css('pointer-events', 'none');
    $('#btnSave').hide();
    $('.btn-delete-action, .btn-add-action').prop('disabled', true);
    form.addClass('erppage-readonly');

    //==========Modal Readonly==============
    const DeductModal = $('#qcparameterbtnadd');
    $('#qcparameterbtnadd').on('shown.bs.modal', function () {
        $('#tblParameterDR tbody')
            .find('.btn-delete-Deduct, .btn-add-deductRate-row')
            .prop('disabled', true);
    });
    DeductModal.addClass('erppage-readonly');
    
}

//============Validate order of Deduct Rates==============
function validateDeductRateOrder() {
    const rows = $('#tblParameterDR tbody tr');

    if (rows.length === 0) {
        return true;
    }

    let isValid = true;

    const firstFromInput = $(rows[0]).find('.from-result');
    const firstToInput = $(rows[0]).find('.to-result');

    const firstFrom = parseFloat(firstFromInput.val()) || 0;
    const firstTo = parseFloat(firstToInput.val()) || 0;

    if (firstFrom === firstTo) {
        setInvalid(firstToInput, 'From and To cannot be equal.');
        return false;
    }

    const isAscending = firstTo > firstFrom;

    rows.each(function (index) {
        const fromInput = $(this).find('.from-result');
        const toInput = $(this).find('.to-result');

        const from = parseFloat(fromInput.val()) || 0;
        const to = parseFloat(toInput.val()) || 0;
        
        // Validate against previous row
        if (index > 0) {
            const prevTo =
                parseFloat($(rows[index - 1]).find('.to-result').val()) || 0;

            if (isAscending && from <= prevTo) {
                setInvalid($(fromInput), 'From value must be greater than previous row To value');
                isValid = false;
                return false;
            }

            if (!isAscending && from >= prevTo) {
                setInvalid($(fromInput), 'From value must be less than previous row To value');
                isValid = false;
                return false;
            }
        }
        // Validate current row direction
        if (isAscending && to <= from) {
            setInvalid($(toInput), 'To value must be greater than From value.');
            isValid = false;
            return false;
        }

        if (!isAscending && to >= from) {
            setInvalid($(toInput), 'To value must be less than From value');
            isValid = false;
            return false;
        }
    });

    return isValid;
}
