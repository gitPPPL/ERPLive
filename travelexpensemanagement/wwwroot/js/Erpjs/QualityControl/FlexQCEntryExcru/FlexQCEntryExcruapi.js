
async function LoadFormByID(id) {
    try {
        const res = await $.ajax({
            url: '/FlexQCEntryExcruList/GetDataByCode',
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

            $tbody.empty();
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

async function GetVNo() {
    try {
        const res = await fetch('/FlexQCEntryExcru/GetVNo');
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
            loadItemNameDropdown(),
            DDLItem(),
            DDLGridStatus()
        ]);

    } catch (error) {
        console.error("Error loading dropdowns:", error);
    }
}
function loadItemNameDropdown() {
    $.ajax({
        url: '/FlexQCEntryExcru/DDLGridItem',
        method: 'GET',
        success: function (data) {
            itemNameOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}
function DDLGridStatus() {
    $.ajax({
        url: '/FlexQCEntryExcru/DDLGridStatus',
        method: 'GET',
        success: function (data) {
            DDLGridStatuslist = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });


}

async function DDlInspBy() {
    try {
        const response = await fetch('/FlexQCEntryExcru/DDLInspBy');

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
        const response = await fetch('/FlexQCEntryExcru/DDLPordPlace');
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
        const response = await fetch('/FlexQCEntryExcru/DDLChemist');

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
        const response = await fetch('/FlexQCEntryExcru/DDLQCIncharge');

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

    $.ajax({ url: '/FlexQCEntryExcruList/GetDataCopyForm',
        type: 'GET',
        dataType: 'json',
        data: { DeptCode: deptCode, Shifttype: shiftType, v_date: vDate },

        beforeSend: function () {
            $('#loader').show();
        },

        success: function (response) {

            console.log("fetchData response", response);

            if (response.success === false) {
                toastr.info(`No Bags are Pending for QC of Date: ${vDate}, Shift: ${shiftType}`);

                return;
            }

            if (Array.isArray(response.data) && response.data.length > 0) {       
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
                                   <td>${item.jumbo_No ?? ''}</td>
                                <td>${item.wbQty ?? ''}</td>
                             
                                <td>${item.grossQty ?? ''}</td>
                                <td>${item.tareQty ?? ''}</td>
                                <td>${item.qty ?? ''}</td>
                                <td>${item.v_TYPE ?? ''}</td>
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
        url: '/FlexQCEntryExcruList/GetDataTotalppmChangge',
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
function TransitReport() {

    if (!rowId) {
        showToast("Please Save The Data Before Printing The Report.", { type: "info" });
        return;
    }

    let reportName = "";
    let RPTNAME = "";

    const v_no = $('#NumDocNo').val();
    const ProdPlace = $('#ddlProdPlace').val();

    let FromDate = $('#DtFrom').val();
    let ToDate = $('#DtTo').val();

    if (FromDate) {
        const f = FromDate.split('-');
        FromDate = `${f[2]}/${f[1]}/${f[0]}`;
    }

    if (ToDate) {
        const t = ToDate.split('-');
        ToDate = `${t[2]}/${t[1]}/${t[0]}`;
    }

    let formula = "";
    let payload = "";


        reportName = "QC_Flakes";
        RPTNAME = "Flakes QC Report";

        formula =
            " {PROD2_QC.V_TYPE} = 'SFQC' " +
            " AND {PROD2_QC.V_No} = " + v_no +
            " AND {PROD2_QC.COMP_CODE} = " + globalVars.CompCode +
            " AND {PROD2_QC.YEAR_CODE} = " + globalVars.FYearCode +
            " AND {PROD2_QC.BRANCH_CODE} = " + globalVars.BranchCode + " ";    

        payload = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: database,
            Parameters: {
                comp_name: globalVars.CompanyName || "",
                comp_add1: globalVars.Address1 || "",
                comp_add2: globalVars.Address2 || "",
                F1: `From Date ${FromDate} To ${ToDate}`,
                RPTNAME: RPTNAME
            }
        };

    const now = new Date();

    const timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: "http://localhost:24085/Report/PendingQCReport",
        type: "POST",
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: {
            responseType: "blob"
        },

        success: function (response) {

            const blob = new Blob([response], {
                type: "application/pdf"
            });

            const fileName = `${reportName}_${timestamp}.pdf`;

            const link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = fileName;

            document.body.appendChild(link);
            link.click();

            URL.revokeObjectURL(link.href);
            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {

            console.error("Status:", xhr.status);
            console.error("Error:", error);

            if (xhr.responseText) {
                console.error(xhr.responseText);
            }

            showToast("Failed to generate report.", { type: "error" });
        }
    });
}

async function DDLItem(Cheackbox = false)
{
    try {
        const response = await fetch(`/FlexQCEntryExcru/DDLItem?Cheackbox=${Cheackbox}`);

        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const data = await response.json();
        const ddl = $('#ddlItem');
        ddl.empty().append('<option value="">-- Select Item Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item Name:", error);
        toastr.error('Error loading Item Name: ' + error.message);
    }
}
function summaryReport(btnName = "" ) {
    if (!rowId) {
        showToast("Please Save The Data Before Printing The Report.", { type: "info" });
        return;
    }

    let reportName = "";

    if (btnName == "Summary")
    {
        reportName = "Flakes_QC2";
    }
    else
    {
        reportName = "QC_Flakes";
    }

    const ItemName = $('#ddlItem').val();
    const ShipType = $('#ddlShipType').val();

    let FromDate = $('#DtFrom').val(); 
    let ToDate = $('#DtTo').val();

    if (!FromDate || !ToDate)
    {
        showToast("Please select From Date and To Date.", { type: "info" });
        return;
    }

    const f = FromDate.split('-');
    const t = ToDate.split('-');

    const crystalFromDate = `DATE(${f[0]},${f[1]},${f[2]})`;
    const crystalToDate = `DATE(${t[0]},${t[1]},${t[2]})`;

    const displayFromDate = `${f[2]}/${f[1]}/${f[0]}`;
    const displayToDate = `${t[2]}/${t[1]}/${t[0]}`;
  
    const RPTNAME = "Flakes QC Report";

    let formula = "{PROD2_QC.V_TYPE} = 'SFQC'";
        formula += ` AND {PROD2_QC.v_date} in ${crystalFromDate} TO ${crystalToDate}`;
        formula += ` AND {PROD2_QC.COMP_CODE} = ${globalVars.CompCode}`;
        formula += ` AND {PROD2_QC.YEAR_CODE} = ${globalVars.FYearCode}`;
        formula += ` AND {PROD2_QC.BRANCH_CODE} = ${globalVars.BranchCode}`;

    if (ItemName && parseInt(ItemName) > 0) {
        formula += ` AND {PROD2_QC.ITEM_CODE} = ${ItemName}`;
    }

    if (ShipType) {
        formula += ` AND {PROD2_QC.SUPPLY_TYPE} = '${ShipType}'`;
    }

    const payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            F1: `From Date ${displayFromDate} To ${displayToDate}`,
            RPTNAME: RPTNAME
        }
    };

    const now = new Date();

    const timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: "http://localhost:24085/Report/PendingQCReport",
        type: "POST",
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: {
            responseType: "blob"
        },
        success: function (response) {
            const blob = new Blob([response], {
                type: "application/pdf"
            });

            const fileName = `${reportName}_${timestamp}.pdf`;
            const link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = fileName;

            document.body.appendChild(link);
            link.click();

            URL.revokeObjectURL(link.href);
            document.body.removeChild(link);
        },
        error: function (xhr, status, error) {

            console.error("Status:", xhr.status);
            console.error("Error:", error);
            console.error(xhr.responseText);

            showToast("Failed to generate report.", {
                type: "error"
            });
        }
    });
}