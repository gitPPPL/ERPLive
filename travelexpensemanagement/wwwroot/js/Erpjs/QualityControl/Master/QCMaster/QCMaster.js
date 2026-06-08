let parameterOptionsHtml = '';
let PreviousInputDt = "";
let rowId = 0;

$(document).ready(function () {
    const code = getQueryParam('id');
    const mode = getQueryParam("mode");
    rowId = code;
    $('#TxtName').focus();

    bindDropdown('QCMaster', 'QCGroup', '#ddlQCGroup', ' Select QC Group ', null, null, false, null, false);

    addRow();


    if (code) {
        getQcDataByCode(code);
    }
    if (mode === "view") {
        setFormReadOnly();
    }

    $('#ACTIVE').on('change', function () {
        let status = $(this).is(':checked') ? 'Active' : 'Inactive';
        $('#statusText').text(status);
    });

    // Submit main form
    $('#btnSave').on('click', function (e) {
        e.preventDefault();

        if (!validateRequiredField('#TxtName', 'QC Test Name')) return;

        const formData = CollectFormData();

        if (code) {
            EditMasterData(formData);
        }
        else {
            saveData(formData);
        }

    });
})

function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}

//==========Get By Code========
function getQcDataByCode(code) {
    $.ajax({
        url: '/QCMaster/GetQCMasterListByCode',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ code: parseInt(code) }),
        success: function (response) {
            bindItemDeptForm(response);
        },
        error: function (xhr) {
            alert('Error: ' + xhr.responseText);
        }
    });
}

//==========Fill Form========
function bindItemDeptForm(response) {
    if (!response.success || !response.data || !response.data.details) {
        toastr.warning('No details found to bind.');
        return;
    }

    const data = response.data;

    console.log("data", data);

    $('#TxtName').val(data.name);
    $('#TxtShortName').val(data.shortName);
    $('#TxtMaxPPM').val(data.maxPPM);
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

//============Readonly==========
function setFormReadOnly() {
    $('#QCMasterform input, #QCMasterform select').prop('disabled', true);
    $('#customToggle').css('pointer-events', 'none');
    $('#btnSubmit, #btnUpdate, ').hide();
    $('.btn-delete').hide();
}

//============DropDowns=========
function ddlParameter(selector, selectedValue = null) {
    const $ddl = $(selector);
    // Use cached options if already loaded
    if (parameterOptionsHtml !== '') {

        $ddl.html(parameterOptionsHtml);

        if (!$ddl.hasClass('select2-hidden-accessible')) {
            $ddl.select2({
                placeholder: '-- Select Parameter --',
                allowClear: true
            });
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

//============Add Row===========
function addRow(item = {}) {
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
            <td><input type="text" name="Unit" class="form-control TxtUnit" value="${item.unit || ''}"/></td>
            <td><input type="number" name="StdResult" class="form-control TxtStdResult" value="${item.stdResult || ''}"/></td>
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
            <td><input type="number" step="0.01" name="BasePrice" class="form-control TxtBasePrice" value="${item.basePrice != null ? parseFloat(item.basePrice).toFixed(2) : ''}"/></td>
            <td><input type="text" name="Remarks" class="form-control TxtRemarks" value="${item.remarks || ''}"/></td>
            <td class="action-col">
                    <button class="act-btn add btn-add-action" title="Add" style="cursor:pointer;"><i class="fa fa-plus"></i></button>
                    <button class="act-btn delete btn-delete-action" title="Delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
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
$(document).on('select2:select', '.ddlParameter', function () {

    const $row = $(this).closest('tr');

    const unit = $(this).find(':selected').data('unit') || '';

    $row.find('.TxtUnit').val(unit);
    
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
                    alert('Error: ' + error);
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
            alert('Error: ' + error);
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
$(document).on('contextmenu', '#tblQCMasteradd tbody tr', function (e) {
    e.preventDefault(); // stop browser menu

    const $row = $(this);

    const modal = new bootstrap.Modal(
        document.getElementById('qcparameterbtnadd')
    );

    const code = $row.find('.txtCode').text().trim();
    const parameterText = $row.find('.ddlParameter option:selected').text().trim() || '';
    const deductTypeText = $row.find('.ddlDeductType option:selected').text().trim() || '';
    const parameterId = parseInt($row.find('.ddlParameter').val()) || 0;

    $('#lblParameterText').text(parameterText);
    $('#lblDeductTypeText').text(deductTypeText);
    $('#hdnCode').val(code);
    $('#hdnnextQcpCode').val(parameterId);
    modal.show();
    //GetDeductdata(code, parameterId);
});

function GetDeductdata(code, parameterId) {
    $.ajax({
        url: '/QCMaster/CheckDeductRates',
        type: 'POST',
        data: JSON.stringify({ code: code, parameterId: parameterId }),
        contentType: 'application/json',
        success: function (response) {
            if (response && response.length > 0) {
                showDeductRateListPopup(response);
            } else {
                showDeductRateListPopup([]);
            }
        },
        error: function (xhr, status, error) {
            alert('Error retrieving data: ' + error);
        }
    });

}
function showDeductRateListPopup(deductRates) {
    console.log('deductRates', deductRates);
    const $tbody = $('#tblParameterDR tbody');
    $tbody.empty();
    if (!Array.isArray(deductRates) || deductRates.length === 0) {
        addDeductRateRows();
        return;
    }
    deductRates.forEach(rate => {
        addDeductRateRows(rate);
    });
    modal.show();
}

function addDeductRateRows(rate = {}) {
    const $tbody = $('#tblParameterDR tbody');
    const rowHtml = `
                <tr>
                    <td><input type="number" class="form-control form-control-sm" placeholder="From" value="${rate.fromResult ?? ''}"></td>
                    <td><input type="number" class="form-control form-control-sm" placeholder="To" value="${rate.toResult ?? ''}"></td>
                    <td><input type="number" class="form-control form-control-sm" placeholder="Rate" value="${rate.deductRate ?? ''}"></td>
                    <td>
                        <select class="form-control form-control-sm">
                            <option value="Base" ${rate.deductType === 'Base' ? 'selected' : ''}>Base</option>
                            <option value="Percent" ${rate.deductType === 'Percent' ? 'selected' : ''}>Percent</option>
                        </select>
                    </td>
                    <td>
                        <button type="button" class="btn btn-success btn-sm btn-add-row btn-add-deductRate-row">
                            <i class="fa fa-plus"></i>
                        </button>
                    </td>
                </tr>
            `;
    $tbody.append(rowHtml);
}
$(document).on('click', '.btn-add-deductRate-row', function () {
    addDeductRateRows();
});

