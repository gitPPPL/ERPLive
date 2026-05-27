
async function LoadFormByID(id) {
    try {
        const res = await $.ajax({
            url: '/FlakesQCEntryList/GetDataByCode',
            method: 'GET',
            data: { code: id }
        });

        if (res.success) {
            const header = res.data.header;
            const details = res.data.deatils;

            console.log('payload', res);

            $('#TxtCode').val(header.doC_ID || '');
            $('#NumDocNo').val(header.v_NO || '');
            $('#DtDocDate').val(formatDate(header.v_DATE) || '');
            $('#ddlShift').val(header.shift || '');
            $('#DtTime').val(header.qctime || '');
            $('#ddlQCIncharge').val(header.qC_INCHARGE || '').trigger('change');
            $('#ddlChemist').val(header.chemist || '').trigger('change');
            $('#ddlInspBy').val(header.emP_CODE || '').trigger('change');
            $('#ddlProdPlace').val(header.placE_CODE || '').trigger('change');
            $('#TxtRemarks').val(header.remarks || '');

            populateTableRowsFromData(details);

        } else {
            console.error('Error in API response:', res.message);
            toastr.error(res.message || "Failed to load data.");
        }
    } catch (err) {
        console.error("Failed to load data:", err);
        toastr.error("Something went wrong while loading the form.");
    }
}
function loadItemNameDropdown() {
    $.ajax({
        url: '/FlakesQCEntry/DDLGridItem',
        method: 'GET',
        success: function (data) {
            itemNameOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });


}

async function GetVNo() {
    try {
        const res = await fetch('/FlakesQCEntry/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();

        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    } catch (e) {
        toastr.warning('Error loading Document Number: ' + e.message);
    }
}

async function LoadDropDown() {
    try {
        await Promise.all([
            DDlInspBy(),
            DDLPordPlace(),
            DDLChemist(),
            DDLQCIncharge(),
            loadItemNameDropdown()
        ]);


    } catch (error) {
        console.error("Error loading dropdowns:", error);
    }
}

async function DDlInspBy() {
    try {
        const response = await fetch('/FlakesQCEntry/DDLInspBy');

        if (!response.ok)
            throw new Error(`HTTP error! status: ${response.status}`);

        const data = await response.json();

        const ddl = $('#ddlInspBy');

        ddl.empty().append('<option value="">-- Select Insp. By --</option>');

        data.forEach(item => {

            // Showing Code + Text
            ddl.append(`
                    <option value="${item.value}">
                        ${item.value} - ${item.text}
                    </option>
                `);
        });

        // Enable Search Box
        ddl.select2({
            placeholder: "-- Select Insp. By --",
            allowClear: true,
            width: '100'
        });

    } catch (error) {
        console.error("Error loading Insp. By:", error);
        toastr.error('Error loading Insp. By: ' + error.message);
    }
}

async function DDLPordPlace() {
    try {
        const response = await fetch('/FlakesQCEntry/DDLPordPlace');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const data = await response.json();
        const ddl = $('#ddlProdPlace');
        ddl.empty().append('<option value="">-- Select Prod. Place --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Prod. Place:", error);
        toastr.error('Error loading Prod. Place: ' + error.message);
    }
}

async function DDLChemist() {
    try {
        const response = await fetch('/FlakesQCEntry/DDLChemist');

        if (!response.ok)
            throw new Error(`HTTP error! status: ${response.status}`);

        const data = await response.json();

        const ddl = $('#ddlChemist');

        ddl.empty().append('<option value="">-- Select Chemist --</option>');

        data.forEach(item => {

            // Showing Code + Text
            ddl.append(`
                    <option value="${item.value}">
                        ${item.value} - ${item.text}
                    </option>
                `);
        });

        // Enable Search Box
        ddl.select2({
            placeholder: "-- Select Chemist --",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        console.error("Error loading Chemist:", error);
        toastr.error('Error loading Chemist: ' + error.message);
    }
}

async function DDLQCIncharge() {
    try {
        const response = await fetch('/FlakesQCEntry/DDLQCIncharge');

        if (!response.ok)
            throw new Error(`HTTP error! status: ${response.status}`);

        const data = await response.json();

        const ddl = $('#ddlQCIncharge');

        ddl.empty().append('<option value="">-- Select QC Incharge --</option>');

        data.forEach(item => {

            // Showing Code + Text
            ddl.append(`
                    <option value="${item.value}">
                        ${item.value} - ${item.text}
                    </option>
                `);
        });

        // Enable Search Box
        ddl.select2({
            placeholder: "-- Select QC Incharge --",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        console.error("Error loading QC Incharge:", error);
        toastr.error('Error loading QC Incharge: ' + error.message);
    }
}
function fetchData(deptCode, shiftType, vDate) {

    const $tbody = $('#tblCopyFrommodal tbody');
    $tbody.empty();

    $.ajax({
        url: '/FlakesQCEntryList/GetDataCopyForm',
        type: 'GET',
        dataType: 'json',
        data: {
            DeptCode: deptCode,
            Shifttype: shiftType,
            v_date: vDate
        },

        beforeSend: function () {
            // Optional loader
            $('#loader').show();
        },

        success: function (response) {

            console.log("response", response);

            if (response.success === false) {
                toastr.info(`No Bags are Pending for QC of Date: ${vDate}, Shift: ${shiftType}`);

                return;
            }

            if (Array.isArray(response.data) && response.data.length > 0) {

                // Build complete HTML first
                let rows = '';

                response.data.forEach(item => {

                    rows += `
                            <tr>
                                <td>
                                    <input type="checkbox" class="selectItem" />
                                </td>

                                <td>${item.bagNo ?? ''}</td>
                                <td>${item.itemName ?? ''}</td>
                                <td>${item.prodPlace ?? ''}</td>
                                <td>${item.lotNo ?? ''}</td>
                                <td>${item.wbQty ?? ''}</td>
                                <td>${item.grossQty ?? ''}</td>
                                <td>${item.tareQty ?? ''}</td>
                                <td>${item.qty ?? ''}</td>
                                <td>${item.vType ?? ''}</td>
                                <td>${item.vNo ?? ''}</td>

                                <td hidden>${item.itemCode ?? ''}</td>
                                <td hidden>${item.deptCode ?? ''}</td>
                                <td hidden>${item.deptName ?? ''}</td>
                            </tr>
                        `;
                });

                // Single DOM update
                $tbody.html(rows);

                // Open modal after rendering
                const modalElement = document.getElementById('CopyFromModal');
                const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
                modal.show();

            }
        },

        error: function (xhr, status, error) {

            console.error(error);
            console.error(xhr.responseText);

            toastr.error('Error fetching data');
        },

        complete: function () {
            $('#loader').hide();
        }
    });
}
function fetchAndUpdateItemData($row, totalPpm) {
    const itemCode = parseInt($row.find('.ITEM_CODE').val(), 10);
    const depotCode = parseInt($row.find('.DEPT_CODE').val(), 10);
    const HDPE = parseInt($row.find('.HD').val(), 10);
    const PVCPPM = parseInt($row.find('.DNR').val(), 10);
    const PCLowMelt = parseInt($row.find('.PC_LOWMELT').val(), 10);
    const Wrapper = parseInt($row.find('.CPRDN').val(), 10);
    const Metal = parseInt($row.find('.TIME1_WIDTH').val(), 10);
    const Stone = parseInt($row.find('.TIME2_WIDTH').val(), 10);
    const Rubber = parseInt($row.find('.TIME3_WIDTH').val(), 10);
    const Glue = parseInt($row.find('.GLUE_CONTENT').val(), 10);
    const Yellowp = parseInt($row.find('.YELLOWP').val(), 10);
    const BLUEP = parseInt($row.find('.BLUEP').val(), 10);
    const OTHERP = parseInt($row.find('.OTHERP').val(), 10);
    const YELLOW160C = parseInt($row.find('.YELLOW160C').val(), 10);



    if (isNaN(itemCode) || isNaN(depotCode) || totalPpm <= 0) {
        console.warn('Invalid inputs for fetching item data', { totalPpm, itemCode, depotCode });
        return;
    }

    const $select = $row.find('.item-name-select');
    $select.prop('disabled', true);

    $.ajax({
        url: '/FlakesQCEntryList/GetDataTotalppmChangge',
        method: 'POST',
        data: {
            totalPpm: totalPpm,
            itemCode: itemCode,
            depotCode: depotCode,
            HDPE: HDPE,
            PVCPPM: PVCPPM,
            PCLowMelt: PCLowMelt,
            Wrapper: Wrapper,
            Metal: Metal,
            Stone: Stone,
            Rubber: Rubber,
            Glue: Glue,
            Yellowp: Yellowp,
            BLUEP: BLUEP,
            OTHERP: OTHERP,
            YELLOW160C: YELLOW160C
        },
        success: function (data) {
            if (!data || typeof data !== 'object') {
                console.warn('Invalid response format:', data);
                return;
            }

            console.log('After Data', data);

            const itemName = data.itemName?.trim();
            const GRD = data.grd?.trim();
            const newItemCode = parseInt(data.itemCode, 10);

            if (!itemName) {
                console.warn('itemName not found in response:', data);
                return;
            }

            let optionFound = false;
            $select.find('option').each(function () {
                if ($(this).text().trim() === itemName) {
                    $select.val($(this).val());
                    optionFound = true;
                    return false;
                }
            });

            if (!optionFound) {
                const optionValue = newItemCode || '';
                const newOption = new Option(itemName, optionValue, true, true);
                $select.append(newOption);
                $select.val(optionValue);
            }

            if (!isNaN(newItemCode) && newItemCode !== itemCode) {
                $row.find('.ITEM_CODE').val(newItemCode);
            }

            if (GRD && GRD.trim() !== '') {
                $row.find('.GRADE').val(GRD);
            }

        },
        error: function (xhr, status, error) {
            if (xhr.status === 404) {
                console.error('API endpoint not found (404):', '/FlakesQCEntryList/GetDataTotalppmChangge');
            } else {
                console.error(`API error (${xhr.status}): ${error}`);
            }
        },
        complete: function () {
            $select.prop('disabled', false);
        }
    });
}
function bindRowValueChange() {
    $tbody.on('input', '.HD, .DNR, .PC_LOWMELT, .CPRDN, .TIME1_WIDTH, .TIME2_WIDTH, .TIME3_WIDTH, .GLUE_CONTENT, .OTHERS', function () {
        const $row = $(this).closest('tr');
        const totalPpm = calculateTotalForRow($row);
        fetchAndUpdateItemData($row, totalPpm);
    });

}