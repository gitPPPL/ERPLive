let isEditLoading = false;
let marketRateItems = [];
let globalItemMap = {};

//getting 
const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(function () {
    $("#ddlDocType").focus();
    $('#DtDocDate').val(new Date().toISOString().split('T')[0]);
    $('#DtEffFrom').val(new Date().toISOString().split('T')[0]);
    $('#DtExpiredOn').val(new Date().toISOString().split('T')[0]);

    $.when(
        loadItemList(compCode),
        loadDocTypeList(),
        loadItemGroupTypeList(compCode)
    ).done(function () {

        if (!isNaN(rowId) && rowId > 0) {
            loadFullItemMarketRateByVno(rowId);
        }
        else {
            $('#ddlItemGroup').val('Raw').trigger('change');
            addNewRowBelow(true);
            loadNextDocNo(yearCode);
        }

    });

    $('#ddlDocType').change(function () {

        if ($(this).val()) {

            $(this).prop('disabled', true);

            $('#DtDocDate').focus();

        }

    });

    $('#ddlItemGroup').change(function () {
        if (isEditLoading) return;
        const groupType = $(this).val();

        loadItemList(compCode, groupType).done(function () {
            console.log("addNewRowBelow called");
            $('#tblPurchaseBillPassEntry tbody').empty();
            marketRateItems = [];

            addNewRowBelow(true);
        });
    });

    $(document).on('change', 'select[name^="itemName"]', function () {

        const currentValue = $(this).val();
        const currentRow = $(this).closest('tr');

        let duplicateFound = false;

        $('#tblPurchaseBillPassEntry tbody tr').each(function () {

            if ($(this)[0] === currentRow[0]) {
                return true;
            }

            const otherValue = $(this).find('select[name^="itemName"]').val();

            if (otherValue === currentValue && currentValue !== '') {
                duplicateFound = true;
                return false;
            }
        });

        if (duplicateFound) {
            showToast("This Item Is Already Added", { type: "warning" });

            $(this).val('').trigger('change');

            const rowIndex = currentRow.index();
            $(`#itemCode_${rowIndex}`).val('');

            return;
        }

        const rowIndex = currentRow.index();
        $(`#itemCode_${rowIndex}`).val(currentValue);
    });

    $(document).on('input', '[id^="minRate_"], [id^="maxRate_"]', function () {
        let value = $(this).val();

        // Allow only 2 decimal places
        if (!/^\d{0,16}(\.\d{0,2})?$/.test(value)) {
            $(this).val(value.slice(0, -1));
        }
    });
});

function loadNextDocNo(yearC) {
    $.ajax({
        url: '/ItemMarketRate/GetNextV_NO',
        type: 'GET',
        data: { yearCode: yearC },
        success: function (response) {
            $('#NumDocNo').val(response);
        },
        error: function (xhr, status, error) {
            showToast("Error loading Item Make: " + error, { type: "error" });
        }
    });
}

function loadDocTypeList() {
    docTypeMap = {};
    return $.ajax({
        url: '/ItemMarketRate/GetDocumentTypeList',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var ddl = $('#ddlDocType');
            ddl.empty();

            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.value + '">' + item.text + '</option>');
                docTypeMap[item.value] = item.text;
            });
        },
        error: function (xhr, status, error) {
            showToast("Error Loading DocType List: " + error, { type: "error" });
        }
    });
}

function loadItemGroupTypeList(compC) {
    docTypeMap = {};
    return $.ajax({
        url: '/ItemMarketRate/GetItemGroupTypeList',
        type: 'GET',
        dataType: 'json',
        data: { cCode: compC },
        success: function (data) {
            console.log(data);
            var ddl = $('#ddlItemGroup');
            ddl.empty();
            ddl.append('<option value="">-- Select Item Group --</option>');

            $.each(data, function (index, item) {
                ddl.append('<option value="' + item + '">' + item + '</option>');
                docTypeMap[item] = item;
            });

            ddl.select2({
                placeholder: "Select Item Group",
                allowClear: true,
                width: '100%'
            });

            ddl.on('select2:open', function () {
                setTimeout(function () {
                    const searchBox = document.querySelector('.select2-container--open .select2-search__field');
                    if (searchBox) {
                        searchBox.focus();
                    }
                }, 50);
            });

            //if (data.includes("Raw")) {
            //    ddl.val("Raw").trigger('change');
            //}
        },
        error: function (xhr, status, error) {
            showToast("Error Loading DocType List: " + error, { type: "error" });
        }
    });
}

let globalItemList = [];

function loadItemList(compC, groupType) {
    return $.ajax({
        url: '/ItemMarketRate/GetItemList',
        type: 'GET',
        dataType: 'json',
        data: { cCode: compC, groupType: groupType }
    }).then(function (data) {
        console.log("Item", data);
        globalItemList = data;
        console.log(globalItemList[0]);
        return data;
    });
}

$(document).on('change', 'select[name^="itemName"]', function () {
    const selectedCode = $(this).val();
    const row = $(this).closest('tr');
    const rowIndex = row.index();

    // Update itemCode input
    $(`#itemCode_${rowIndex}`).val(selectedCode);

    // Optional: also update in the array
    if (marketRateItems[rowIndex]) {
        marketRateItems[rowIndex].iteM_CODE = selectedCode;
    }
});

function loadFullItemMarketRateByVno(vNo) {
    isEditLoading = true;

    $.ajax({
        url: '/ItemMarketRate/GetItemMarketRateByVno',
        type: 'GET',
        data: { vNo },
        success: function (res) {

            if (!res.success || !res.header) {
                showToast("Item Market Rate not Found", { type: "warning" });
                return;
            }

            const header = res.header;
            marketRateItems = res.items || [];

            $('#ddlDocType').val(header.v_TYPE).trigger('change');
            $('#DtDocDate').val(header.v_DATE?.substring(0, 10));
            $('#NumDocNo').val(header.v_NO);

            //$('#ddlItemGroup').val(header.mgrouP_TYPE);
            $('#ddlItemGroup').val(header.mgrouP_TYPE).trigger('change');
            $('#DtEffFrom').val(header.efF_DATE?.substring(0, 10));
            $('#DtExpiredOn').val(header.exP_DATE?.substring(0, 10));
            $('#TxtRemarks').val(header.remarks);

            const renderRows = () => {

                const tbody = $('#tblPurchaseBillPassEntry tbody');
                tbody.empty();

                marketRateItems.forEach((row, i) => {
                    const isLastRow = (i === marketRateItems.length - 1);

                    const rowHtml = generateRowHtml(row, i, globalItemMap, isLastRow);
                    tbody.append(rowHtml);
                });

                initItemSelect2('select[id^="itemName_"]');

                //  Readonly mode
                if (isReadOnly) {
                    applyReadOnlyMode();
                }

                isEditLoading = false;
            };

            if (typeof loadItemList === "function") {
                loadItemList(compCode, header.mgrouP_TYPE)
                    .done(function () {
                        renderRows();
                    })
                    .fail(function () {
                        showToast("Item list load failed", { type: "error" });
                    });
            } else {
                renderRows();
            }
        },

        error: function (xhr) {
            showToast("Failed To Load Item Market Rate: " + xhr.responseText, { type: "error" });
            isEditLoading = false;
        }
    });
}

function generateRowHtml(row, i, itemMap, isLastRow = false) {
    const getValue = value => (value === undefined || value === null || value === '') ? '' : value;
    const dropdownOptions = generateDropdownOptions(globalItemList, getValue(row.iteM_CODE));
        return `
            <tr>  
            
                <input type="hidden" id="hidden_${i}" name="hidden[${i}]" value="${getValue(row.v_NO)}" />
              
                <td>
                    <input class="form-control" type="text" id="itemCode_${i}" name="itemCode[${i}]" class="form-control-sm" value="${getValue(row.iteM_CODE)}" readonly />
                </td>
                <td>
                    <select id="itemName_${i}" name="itemName[${i}]" class="form-select">
                        ${dropdownOptions}
                    </select>
                </td>
                <td>
                    <input class="form-control" type="number" id="minRate_${i}" name="minRate[${i}]" class="form-control-sm" value="${getValue(row.miN_RATE)}" />
                </td>
                <td>
                    <input class="form-control" type="number" id="maxRate_${i}" name="maxRate[${i}]" class="form-control-sm" value="${getValue(row.maX_RATE)}" />
                </td>
                <td>
                    <input class="form-control" type="text" id="remarks_${i}" name="remarks[${i}]" class="form-control-sm" value="${getValue(row.remark || '')}" maxlength="100" />
                </td>
                <td class="action-col">
                     ${isLastRow ? `
                     <button class="act-btn add" title="Add Row" onclick="addNewRowBelow()"><i class="fa fa-plus"></i></button>
                       ` : ''}
                    <button class="act-btn delete" title="Delete Row" onclick="deleteRow(this)"><i class="fa fa-trash"></i></button>
                 </td>
            </tr>
        `;
}

function generateDropdownOptions(itemList, selectedValue) {
    console.log("itemList Length =", itemList.length);
    console.log("selectedValue =", selectedValue);

    let options = '<option value=""></option>';

    itemList.forEach(item => {

        const selected = item.value == selectedValue ? 'selected' : '';

        options += `<option value="${item.value}" ${selected}>
                            ${item.value} - ${item.text}
                        </option>`;


    });

    return options;
}

function deleteRow(button) {
    const row = button.closest('tr');
    if (row) {
        row.remove();
    }
}

function addNewRowBelow(suppressValidation = false) {
    const lastRow = $('#tblPurchaseBillPassEntry tbody tr:last');
    let isValid = true;

    if (!suppressValidation && lastRow.length > 0) {

        const item = lastRow.find('select[id^="itemName_"]').val();
        const minRate = lastRow.find('input[id^="minRate_"]').val();
        const maxRate = lastRow.find('input[id^="maxRate_"]').val();

        //only block completely empty last row (if needed)
        if (!item && !minRate && !maxRate) {
            showToast("Please Fill Current Row Before Adding New Row", { type: "warning" });
            return;
        }
    }

    // Remove all plus icons from existing rows
    $('#tblPurchaseBillPassEntry tbody tr').each(function () {
        $(this).find('.fa-plus-circle').remove();
    });

    const emptyRow = {
        v_NO: '',
        iteM_CODE: '',
        miN_RATE: '',
        maX_RATE: '',
        remark: ''
    };

    const newIndex = marketRateItems.length;
    marketRateItems.push(emptyRow);

    const newRowHtml = generateRowHtml(emptyRow, newIndex, globalItemMap, true);
    $('#tblPurchaseBillPassEntry tbody').append(newRowHtml);

    initItemSelect2('#itemName_' + newIndex);
}

function LoadPreviousData() {
    var grpType = $('#ddlItemGroup').val();

    if (!grpType || grpType === "0" || grpType === "--Select Item Group--") {
        showToast("Please Select a Valid Group Type", { type: "warning" });
        return;
    }

    $.ajax({
        url: '/ItemMarketRate/GetItemMarketRate2ByGroupType',
        method: 'POST',
        data: { groupType: grpType },
        success: function (res) {
            const tbody = $('#tblPurchaseBillPassEntry tbody');

            if (!res.success || !Array.isArray(res.items) || res.items.length === 0) {
                showToast("No Previoud Data Found For Selected Group", { type: "warning" });

                //  Clear global array and table
                marketRateItems = [];

                return;
            }

            //  Assign and render
            marketRateItems = res.items;
            tbody.empty();

            marketRateItems.forEach((row, i) => {
                const isLastRow = (i === marketRateItems.length - 1);
                const rowHtml = generateRowHtml(row, i, globalItemMap, isLastRow);
                tbody.append(rowHtml);
            });

            $('select.form-select').select2({ width: '100%' });
            showToast("Previous Data Loaded Successfully", { type: "success" });
        },
        error: function (xhr, status, error) {
            showToast("Error Loading Previoud Data: " + error, { type: "error" });
        }
    });
}

$('#btn-save').click(async function (e) {
    e.preventDefault();

    const isValidDate = await checkValidDate();
    if (!isValidDate) return;

    if (!validateHeader()) {
        return;
    }

    let rowsData = [];
    let isAllRowsValid = true;

    // Header data
    const headerData = {
        COMP_CODE: compCode,
        BRANCH_CODE: branchCode,
        YEAR_CODE: yearCode,
        V_TYPE: $('#ddlDocType').val(),
        V_NO: parseInt($('#NumDocNo').val()) || null,
        V_DATE: $('#DtDocDate').val(),
        EFF_DATE: $('#DtEffFrom').val(),
        EXP_DATE: $('#DtExpiredOn').val(),
        MGROUP_TYPE: $('#ddlItemGroup').val(),
        REMARKS: $('#TxtRemarks').val()
    };

    if (!validateDetails()) {
        return;
    }

    $('#tblPurchaseBillPassEntry tbody tr').each(function () {

        const row = $(this);
        const itemCode = row.find('select[id^="itemName_"]').val();
        const minRate = row.find('input[id^="minRate_"]').val();
        const maxRate = row.find('input[id^="maxRate_"]').val();

        // Skip completely blank row
        if (!itemCode && !minRate && !maxRate) {
            return true;
        }
        rowsData.push({
            COMP_CODE: compCode,
            BRANCH_CODE: branchCode,
            YEAR_CODE: yearCode,
            V_TYPE: headerData.V_TYPE,
            V_NO: headerData.V_NO,
            V_DATE: headerData.V_DATE,
            ITEM_CODE: parseInt(row.find('select[id^="itemName_"]').val()),
            MIN_RATE: parseFloat(row.find('input[id^="minRate_"]').val()),
            MAX_RATE: parseFloat(row.find('input[id^="maxRate_"]').val()),
            AVG_RATE: 0,
            REMARK: row.find('input[id^="remarks_"]').val()
        });
    });

    if (rowsData.length === 0) {
        showToast("Please Add At least One Valid Detail Row Before Saving ", { type: "warning" });
        return;
    }

    if (!isAllRowsValid) {
        // If any row was invalid, stop saving
        return;
    }

    const payload = {
        header: headerData,
        lineRows: rowsData
    };

    $.ajax({
        url: '/ItemMarketRate/SaveItemMarketRate',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                showToast(response.message, {
                    type: "success"
                });
                setTimeout(() => {
                    window.location.href = '/ItemMarketRateList/Index';
                }, 1000);
            } else {
                showToast("Error While Saving Data: " + response.message, { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error Saving Data: " + error, { type: "error" });
        }
    });
});

function validateHeader() {
    let isValid = true;

    if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;
    if (!validateRequiredField('#DtDocDate', 'Doc Date')) return;
    if (!validateRequiredField('#NumDocNo', 'Doc No')) return;
    if (!validateRequiredField('#ddlItemGroup', 'Item Group')) return;

    const effDate = new Date($('#DtEffFrom').val());
    const expDate = new Date($('#DtExpiredOn').val());

    if (effDate > expDate) {
        showToast("Effective From Date cannot be greater than Expiry Date", { type: "warning" });
        return false;
    }

    const diffDays = Math.abs((expDate - effDate) / (1000 * 60 * 60 * 24));

    if (diffDays > 20) {
        showToast(
            "Difference between Effective Date and Expiry Date cannot be more than 20 days",
            { type: "warning" }
        );
        return false;
    }

    // Validate Effective From Date

    if (!effDate) {
        $('#DtEffFrom').addClass('is-invalid');
        toastr.warning('Please enter a valid Effective From date.');
        isValid = false;
    } else {
        $('#DtEffFrom').removeClass('is-invalid');
    }

    if (!expDate) {
        $('#DtExpiredOn').addClass('is-invalid');
        toastr.warning('Please enter a valid Expired On date.');
        isValid = false;
    } else {
        $('#DtExpiredOn').removeClass('is-invalid');
    }

    return isValid;
}

function validateDetails() {
    const selectedItems = [];
    let isValid = true;

    $('#tblPurchaseBillPassEntry tbody tr').each(function () {

        const row = $(this);

        const itemCode = row.find('select[id^="itemName_"]').val();

        const minRateText = row.find('input[id^="minRate_"]').val();
        const maxRateText = row.find('input[id^="maxRate_"]').val();

        const minRate = parseFloat(minRateText);
        const maxRate = parseFloat(maxRateText);
        row.find('select[id^="itemName_"]').removeClass('is-invalid');
        row.find('input[id^="minRate_"]').removeClass('is-invalid');
        row.find('input[id^="maxRate_"]').removeClass('is-invalid');

        if (!itemCode && !minRateText && !maxRateText) {
            return true; // skip completely blank row
        }


        if (selectedItems.includes(itemCode)) {
            row.find('select[id^="itemName_"]').addClass('is-invalid');

            showToast(
                `Duplicate Item Not Allowed (Line ${row.index() + 1})`,
                { type: "warning" }
            );

            isValid = false;
            return false;
        }

        selectedItems.push(itemCode);

        if (!itemCode) {
            row.find('select[id^="itemName_"]').addClass('is-invalid');
            showToast("Please Select Item Name", { type: "warning" });
            isValid = false;
            return false;
        }

        if (isNaN(minRate) || minRate <= 0) {
            row.find('input[id^="minRate_"]').addClass('is-invalid');
            showToast("Please Enter Valid Minimum Rate", { type: "warning" });
            isValid = false;
            return false;
        }

        if (isNaN(maxRate) || maxRate <= 0) {
            row.find('input[id^="maxRate_"]').addClass('is-invalid');
            showToast("Please Enter Valid Maximum Rate", { type: "warning" });
            isValid = false;
            return false;
        }

        if (minRate > maxRate) {
            showToast(
                `Min Rate Cannot Be Greater Than Max Rate At Line ${row.index() + 1}`,
                { type: "warning" }
            );
            isValid = false;
            return false;
        }

        const allowedMax = minRate + (minRate * 5 / 100);

        if (maxRate > allowedMax) {
            showToast(
                `Difference Between Min Rate And Max Rate Cannot Exceed 5% (Line ${row.index() + 1})`,
                { type: "warning" }
            );
            isValid = false;
            return false;
        }
    });

    return isValid;
}

function applyReadOnlyMode() {

    $('#ddlDocType').prop('disabled', true);
    $('#DtDocDate').prop('disabled', true);
    $('#ddlItemGroup').prop('disabled', true);
    $('#DtEffFrom').prop('disabled', true);
    $('#DtExpiredOn').prop('disabled', true);
    $('#TxtRemarks').prop('readonly', true);

    $('#btn-load-data').prop('disabled', true);
    $('#btn-save').hide();

    // Detail table controls disable
    $('#tblPurchaseBillPassEntry').find('input, select, textarea').prop('disabled', true);

    // Add/Delete buttons hide
    $('#tblPurchaseBillPassEntry').find('.act-btn').prop('disabled', true);
}

async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/ItemMarketRate/CheckValidDate', {
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
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

function initItemSelect2(selector) {
    $(selector).select2({
        placeholder: '-- Select Item --',
        allowClear: true,
        width: '100%',

        templateResult: function (data) {
            return data.text;
        },

        templateSelection: function (data) {
            if (!data.id || !data.text) return '';

            const item = globalItemList.find(x => x.value == data.id);

            if (!item) return data.text;

            const parts = item.text.split('-');
            return parts.length > 1 ? parts.slice(1).join('-').trim() : item.text;

        }
    });

    $(selector).on('select2:open', function () {
        setTimeout(function () {
            document.querySelector('.select2-container--open .select2-search__field')?.focus();
        }, 50);
    });
}