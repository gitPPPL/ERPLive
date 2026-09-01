let deviceList = [];

const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('docId');
const vtype = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(async function () {

    await GetVNo();
    await BindAllHeaderDropdown();
    await wireEvent();

    if (id) {
        await LoadEditData();
    } else {
        addNewRow();
    }
        
    if (isReadOnly) {
        setFormReadOnly();
    }

    $('#btn_save').on('click', async function (e) {
        e.preventDefault();

        await SaveData();

    });
     
});

async function wireEvent() {
    SetCurrentDate();

    //-------------------------------
    // Fill Data on change of Emp 
    //-------------------------------
    $('#ddlEmployeeName').on('change', async function () {

        const empCode = $(this).val();

        if (!empCode) {
            $('#txtdepartment').val('');
            $('#txtdesignation').val('');
            $('#txtusedby').val('');
            return;
        }

        try {

            const response = await fetch(
                `/ITInventoryEntry/GetEmployeeDetails?empCode=${empCode}`
            );

            const result = await response.json();

            if (result.success) {
                $('#txtdepartment').val(result.employee.deptName || '');
                $('#txtdesignation').val(result.employee.desgName || '');
                $('#txtusedby').val(result.employee.name || '');
            } else {
                $('#txtdepartment').val('');
                $('#txtdesignation').val('');
                $('#txtusedby').val('');

                console.log(result.message);
            }

        } catch (error)
        {
            console.error('Error loading employee details:', error);
        }
    });

    //--------------------------------------------
    // Add Footer Row
    //--------------------------------------------
    $(document).on("click", ".act-btn.add", function () {
        addNewRow();
    });

    //--------------------------------------------
    // Delete Footer Row
    //--------------------------------------------
    $(document).on("click", ".act-btn.delete", function () {
        const tbody = $("#tblDeviceInfromation tbody");
        const rowCount = tbody.find("tr").length;
        if (rowCount <= 1) {
            showToast("At least one row is required.", { type: "warning" });
            return;
        }
        $(this).closest("tr").remove();
    });

    $(document).on('change', '.asset-type', async function () {

        const row = $(this).closest('tr');

        const selectedOption = $(this).find('option:selected');

        const assetTypeCode = selectedOption.val();

        const assetTypeName = selectedOption.text().trim();

        const shortName = selectedOption.data('shortname');

        console.log("Asset Type Code:", assetTypeCode);
        console.log("Asset Type Name:", assetTypeName);
        console.log("Short Name:", shortName);

        if (!assetTypeCode) {

            row.find('.asset-srno').val('');
            row.find('.asset-code').val('');

            return;
        }

        if (!shortName) {
            showToast("Short Name not found for selected Asset Type.", {type: "warning"});
            row.find('.asset-srno').val('');
            row.find('.asset-code').val('');

            return;
        }

        try {

            const response = await fetch(
                `/ITInventoryEntry/GetNextAssetSrNo?assetType=${encodeURIComponent(assetTypeName)}`
            );

            if (!response.ok) {
                throw new Error("Failed to get Asset Serial Number");
            }

            const result = await response.json();

            if (!result.success) {

                console.error(result.message);

                row.find('.asset-srno').val('');
                row.find('.asset-code').val('');

                showToast(result.message || "Unable to generate Asset Serial Number.", {
                    type: "error"
                });

                return;
            }

            const assetSrNo = result.assetSrNo;

            row.find('.asset-srno').val(assetSrNo);

            const assetRno = String(assetSrNo).padStart(3, '0');

            const assetCode = `${shortName}/${assetRno}`;

            row.find('.asset-code').val(assetCode);

            console.log("Asset Serial No:", assetSrNo);
            console.log("Asset Code:", assetCode);

        }
        catch (error) {

            console.error("Error generating Asset Code:", error);

            row.find('.asset-srno').val('');
            row.find('.asset-code').val('');

            showToast("Error generating Asset Code.", {type: "error"});
        }
    });
}

function SetCurrentDate() {

    const today = new Date();

    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');

    $('#DtDocDate').val(`${yyyy}-${mm}-${dd}`);
}

async function LoadEditData() {
    if (!id) return;

    try {
        const response = await fetch(
            `/ITInventoryEntry/LoadEditData?docId=${encodeURIComponent(id)}`
        );

        const result = await response.json();

        if (!result.success) {
            console.error(result.message);
            return;
        }

        // ==========================================
        // HEADER
        // ==========================================

        const h = result.header;

        $('#NumDocno').val(h.V_NO ?? '');
        $('#DtDocDate').val(h.V_DATE ? h.V_DATE.substring(0, 10) : '');
        $('#ddlUserstatus').val(h.EMP_STATUS ?? '').trigger('change');

        // ==========================================
        // USER INFORMATION
        // ==========================================

        $('#ddlUnitName').val(h.UNIT_NAME ?? '').trigger('change');
        $('#txtdepartment').val(h.DEPT ?? '');
        $('#txtdesignation').val(h.DESG ?? '');

        $('#ddlEmployeeName').val(h.EMP_CODE ?? '').trigger('change');
        $('#txtusedby').val(h.USED_BY ?? '');
        $('#txtlocation').val(h.LOCATION ?? '');
        $('#ddlEmailLicType').val(h.EMAILLIC_TYPE ?? '').trigger('change');
        $('#txtemail').val(h.EMAIL ?? '');
        $('#txtemailpassword').val(h.EMAIL_PASS ?? '');
        $('#ddlWindowLicType').val(h.WINDOWLIC_TYPE ?? '').trigger('change');
        $('#txtwindowlickey').val(h.WINLIC_KEY ?? '');
        $('#ddlOfficeLicType').val(h.OFFICELIC_TYPE ?? '').trigger('change');
        $('#TxtIntercomNo').val(h.INTERCOM ?? '');
        $('#AssmobileNo').val(h.MOB_NO ?? '');

        // ==========================================
        // SERVER INFORMATION
        // ==========================================

        $('#txtsystemIPAddress').val(h.IPADDRESS ?? '');
        $('#txtassignedserverIP').val(h.SERVER_IP ?? '');
        $('#txtassignedserveruname').val(h.SERVER_USERNAME ?? '');

        $('#txtVPNuname').val(h.VPN_USERNAME ?? '');
        $('#txtVPNPassword').val(h.VPN_PASSWORD ?? '');

        $('#txtdataserveruname').val(h.DS_USERNAME ?? '');
        $('#txtdataserverpassword').val(h.DS_PASSWORD ?? '');

        $('#txtclouduname').val(h.CLOUD_USERNAME ?? '');
        $('#txtCloudpassword').val(h.CLOUD_PASSWORD ?? '');

        $('#txtremarks').val(h.REMARKS ?? '');

        // ==========================================
        // DEVICE / FOOTER
        // ==========================================

        const devices = result.devices || [];

        const tbody = $("#tblDeviceInfromation tbody");

        // Existing rows remove
        tbody.empty();

        deviceList = devices;

        if (devices.length === 0) {
            addNewRow();
        }
        else {

            for (const device of devices) {
                addNewRow();

                const row = tbody.find("tr:last");

                await BindDeviceList(row);

                row.find(".asset-cat").val(device.ASSET_CAT ?? '');
                row.find(".asset-type").val(device.ASSET_TYPE ?? '');
                row.find(".asset-srno").val(device.ASSET_SRNO ?? '');
                row.find(".asset-code").val(device.ASSET_CODE ?? '');

                row.find(".serial-no").val(device.SERIAL_NO ?? '');
                row.find(".device-name").val(device.DEVICE_NAME ?? '');
                row.find(".device-make").val(device.DEVICE_TYPE ?? '');
                row.find(".device-model").val(device.DEVICE_MODEL ?? '');

                row.find(".purchase-date").val(device.PURCHASE_DATE ? device.PURCHASE_DATE.substring(0, 10) : '');

                row.find(".purchase-from").val(device.PURCHASE_FROM ?? '');

                row.find(".warranty-status").val(device.WARRANTY_STATUS ?? '');
                row.find(".device-status").val(device.DEVICE_STATUS ?? '');
                row.find(".reason").val(device.REASON ?? '');

                row.find(".issue-date").val(device.ISSUE_DATE ? device.ISSUE_DATE.substring(0, 10) : '' );

                row.find(".return-date").val(device.RETURN_DATE ? device.RETURN_DATE.substring(0, 10) : '');

                row.find(".qty").val(device.QTY ?? '');
                row.find(".purpose").val(device.PURPOSE ?? '');
            }
        }

        console.log("Edit Header:", h);
        console.log("Edit Devices:", devices);

    }
    catch (error) {
        console.error("Error loading edit data:", error);
    }
}

async function GetVNo() {
    try {
        const response = await fetch('/ITInventoryEntry/GenerateVNo');

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            return;
        }

        $('#NumDocno').val(data.v_NO);

    } catch (error) {
        console.error('Error generating VNo:', error);
    }
}

async function BindAllHeaderDropdown() {

    await Promise.all([

        bindDropdown('ITInventoryEntry', 'AssetType', '#ddlAssetType1', 'Select Asset', null, null, false, null, false),
        bindDropdown('ITInventoryEntry', 'EmployeeName', '#ddlEmployeeName', 'Select Employee', null, null, false, null, true),

    ]);
}

async function SaveData() {

    const header = {

        // ==============================
        // DOCUMENT
        // ==============================
        V_NO: parseInt($('#NumDocno').val()) || null,
        V_DATE: $('#DtDocDate').val() || null,
        V_TYPE: 'ITIV',
        EMP_STATUS: $('#ddlUserstatus').val() || null,

        // ==============================
        // USER INFORMATION
        // ==============================
        UNIT_NAME: $('#ddlUnitName').val() || null,
        DEPT: $('#txtdepartment').val() || null,
        DESG: $('#txtdesignation').val() || null,
        EMP_CODE: parseInt($('#ddlEmployeeName').val()) || null,
        USED_BY: $('#txtusedby').val() || null,
        LOCATION: $('#txtlocation').val() || null,
        EMAILLIC_TYPE: $('#ddlEmailLicType').val() || null,
        EMAIL: $('#txtemail').val() || null,
        EMAIL_PASS: $('#txtemailpassword').val() || null,
        WINDOWLIC_TYPE: $('#ddlWindowLicType').val() || null,
        WINLIC_KEY: $('#txtwindowlickey').val() || null,
        OFFICELIC_TYPE: $('#ddlOfficeLicType').val() || null,
        INTERCOM: $('#TxtIntercomNo').val() || null,
        MOB_NO: $('#AssmobileNo').val() || null,

        // ==============================
        // SERVER INFORMATION
        // ==============================
        IPADDRESS: $('#txtsystemIPAddress').val() || null,
        SERVER_IP: $('#txtassignedserverIP').val() || null,
        SERVER_USERNAME: $('#txtassignedserveruname').val() || null,
        VPN_USERNAME: $('#txtVPNuname').val() || null,
        VPN_PASSWORD: $('#txtVPNPassword').val() || null,
        DS_USERNAME: $('#txtdataserveruname').val() || null,
        DS_PASSWORD: $('#txtdataserverpassword').val() || null,
        CLOUD_USERNAME: $('#txtclouduname').val() || null,
        CLOUD_PASSWORD: $('#txtCloudpassword').val() || null,
        REMARKS: $('#txtremarks').val() || null

    };

    const deviceList = getDeviceListFromTable();

    const payload = {
        ...header,
        Devices: deviceList
    };

    console.log("Header:", header);
    console.log("Devices:", deviceList);
    console.log("Complete Payload:", payload);
   
    // ==============================
    // SAVE API
    // ==============================
    try {

        const response = await fetch('/ITInventoryEntry/SaveAndUpdateData', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },

            body: JSON.stringify(payload)

        });

        const result = await response.json();
        console.log("Save Response:", result);

        if (result.success) {
            showToast(result.message || "Data Saved Successfully", { type: "success" });
        } else {
            showToast("Unable To Save Data: " + result.message, { type: "error" });
        }

    } catch (error) {
        console.error("Save Error:", error);
        showToast("An Error Occurred While Saving Data: " + error, { type: "error" });
    }
}

async function addNewRow() {

    const tbody = $("#tblDeviceInfromation tbody");

    const row = `
        <tr>

            <td class="hidden-col">
                <input type="hidden" class="erppagetable-control code">
            </td>

            <td>
                <select class="erppagetable-control asset-cat">
                    <option value="">Select Asset Cat.</option>
                    <option value="Regular">Regular</option>
                    <option value="Temporary">Temporary</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control asset-type">
                    <option value="">Select Asset Type</option>
                </select>
            </td>

            <td>
                <input type="text" class="erppagetable-control asset-srno">
            </td>

            <td>
                <input type="text" class="erppagetable-control asset-code">
            </td>

            <td>
                <input type="text" class="erppagetable-control serial-no">
            </td>

            <td>
                <input type="text" class="erppagetable-control device-name">
            </td>

            <td>
                 <input type="text" class="erppagetable-control device-make"/>
            </td>

            <td>
                <input type="text" class="erppagetable-control device-model">
            </td>

            <td>
                <input type="date" class="erppagetable-control purchase-date">
            </td>

            <td>
                <input type="text" class="erppagetable-control purchase-from">
            </td>

            <td>
                <select class="erppagetable-control warranty-status">
                   <option value="">Select Warranty Status</option>
                    <option value="UnderWarrenty">Under Warrenty</option>
                    <option value="Expired">Expired</option>
                </select>
            </td>

            <td>
                <select class="erppagetable-control device-status">
                     <option value="">Select Device Status</option>
                    <option value="Active">Active</option>
                    <option value="Discarded">Discarded</option>
                </select>
            </td>

            <td>
                <input type="text" class="erppagetable-control reason">
            </td>

            <td>
                <input type="date" class="erppagetable-control issue-date">
            </td>

            <td>
                <input type="date" class="erppagetable-control return-date">
            </td>

            <td>
                <input type="number" class="erppagetable-control qty" min="0">
            </td>
            
            <td>
                <input type="text" class="erppagetable-control purpose">
            </td>

             <td class="action-col">
                <div class="action-wrap">
                     <button class="act-btn add" title="Add Row"><i class="fa fa-plus"></i></button>
                     <button class="act-btn delete"><i class="fa fa-trash"></i></button>
                </div>
            </td>

        </tr>
    `;

    tbody.append(row);

    const newRow = tbody.find("tr:last");
    BindDeviceList(newRow);

}

async function BindDeviceList(row) {

    try {

        const response = await fetch('/ITInventoryEntry/GetDeviceList');

        if (!response.ok) {
            throw new Error("Failed to load Device List");
        }

        const data = await response.json();

        const ddl = row.find('.asset-type');

        ddl.empty();
        ddl.append('<option value="">Select Asset Type</option>');

        data.forEach(item => {

            ddl.append(`
                <option value="${item.value}" data-shortname="${item.shortName}">
                    ${item.text}
                </option>
            `);

        });

    } catch (error) {

        console.error("Error loading Device List:", error);

    }
}

function getDeviceListFromTable() {

    const devices = [];

    $("#tblDeviceInfromation tbody tr").each(function () {

        const row = $(this);

        const device = {

            ASSET_CODE: row.find(".asset-code").val() || null,
            ASSET_SRNO: row.find(".asset-srno").val() || null,
            ASSET_CAT: row.find(".asset-cat").val() || null,
            ASSET_TYPE: row.find(".asset-type").val() || null,

            SERIAL_NO: row.find(".serial-no").val() || null,

            DEVICE_TYPE: row.find(".device-make").val() || null,
            DEVICE_MODEL: row.find(".device-model").val() || null,
            DEVICE_NAME: row.find(".device-name").val() || null,

            PURCHASE_DATE: row.find(".purchase-date").val() || null,
            PURCHASE_FROM: row.find(".purchase-from").val() || null,

            WARRANTY_STATUS: row.find(".warranty-status").val() || null,
            DEVICE_STATUS: row.find(".device-status").val() || null,

            REASON: row.find(".reason").val() || null,

            ISSUE_DATE: row.find(".issue-date").val() || null,
            RETURN_DATE: row.find(".return-date").val() || null,

            QTY: parseFloat(row.find(".qty").val()) || null,

            PURPOSE: row.find(".purpose").val() || null
        };

        devices.push(device);
    });

    return devices;
}

function setFormReadOnly() {

    const page = $('#ITInventoryform');
    const printSection = $('#printReportSection');

    page.find('input, textarea, select').not('#printReportSection input').not('#printReportSection textarea').not('#printReportSection select').each(function () {

        const element = $(this);

        if (element.is(':checkbox, :radio')) {
            element.prop('disabled', true);
        }
        else if (element.is('select')) {
            element.prop('disabled', true);
        }
        else {
            element.prop('readonly', true);
        }
    });

    page.find('select.select2-hidden-accessible').not('#printReportSection select.select2-hidden-accessible').each(function () {

            $(this).prop('disabled', true);

            $(this).next('.select2-container').addClass('select2-readonly');
    });

    const table = $('#tblDeviceInfromation');

    table.find('input').prop('readonly', true);
    table.find('textarea').prop('readonly', true);
    table.find('select').prop('disabled', true);

    table.find('.add, .delete').prop('disabled', true);

    printSection.find('input').each(function () {
        $(this).prop('disabled', false).prop('readonly', false);
    });

    printSection.find('textarea').each(function () {
        $(this).prop('disabled', false).prop('readonly', false);
    });

    printSection.find('select').each(function () {
        $(this).prop('disabled', false);
    });

    $('#btn_print').prop('disabled', false).show();

    $('#btn_save').prop('disabled', true).hide();
}



//function setFormReadOnly() {

//    const page = $('#ITInventoryform');
//    const printSection = $('#printReportSection');

//    page.addClass('erppage-readonly');

//    page.find('input, textarea').prop('readonly', true);
//    page.find('input[type="checkbox"], input[type="radio"]').prop('disabled', true);
//    page.find('select').prop('disabled', true);
//    page.find('select.select2-hidden-accessible').each(function () {

//        const select = $(this);

//        select.prop('disabled', true);

//        select.next('.select2-container').addClass('select2-readonly');
//    });

//    const table = $('#tblDeviceInfromation');

//    table.find('input').prop('readonly', true);
//    table.find('textarea').prop('readonly', true);
//    table.find('select').prop('disabled', true);
//    table.find('.add, .delete').prop('disabled', true);
  
   

//    page.find('input, textarea, select').not(printSection.find('input, textarea, select'))
//        .each(function () {

//            const element = $(this);

//            if (element.is('select')) {
//                element.prop('disabled', true);
//            } else {
//                element.prop('readonly', true);
//            }
//        });
//    printSection.find('input[type="checkbox"], input[type="radio"]').prop('disabled', false);
//    $('#btn_print').prop('disabled', false).show();
//    $('#btn_save').prop('disabled', true).hide();
//}


