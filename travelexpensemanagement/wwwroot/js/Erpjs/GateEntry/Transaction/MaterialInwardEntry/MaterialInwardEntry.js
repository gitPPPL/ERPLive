    function isItemInMainTable(itemCode) {
        let exists = false;
        $('#tblInwardEntry tbody tr').each(function () {
        const code = $(this).find('td:eq(0)').text().trim();
        if (code === itemCode) {
        exists = true;
        return false;
        }
        });
        return exists;
    }
    function saveInwardEntry() {
        const   PARTY_CODE        = parseInt($('#ddlPartyName').val()) || null;
        const   PARTY_NAME        = $('#ddlPartyName option:selected').text();
        const   V_TYPE            = $('#ddlDocType').val();
        const   STATUS            = parseInt($('#ddlDocStatus').val()) || null;
        const   V_NO              = parseInt($('#TxtDocNo').val()) || null;
        const   R_DATE            = formatDate($("#TxtRptDate").val()) || null;
        const   BILL_NO           = $.trim($('#TxtBillNo').val()) || null;
        const   BILL_DATE         = formatDate($("#DtPartyBillDate").val()) || null;
        const   CHALL_NO          = $.trim($('#TxtChallanNo').val()) || null;
        const   CHALL_DATE        = formatDate($("#TxtChallanDate").val()) || null;
        const   BILL_AMT          = parseFloat($('#TxtBillAmt').val()) || 0.0;
        const   TRUCK_NO          = $.trim($('#TxtVehicleNo').val()) || null;
        const   TRANSPORT_CODE    = parseInt($('#TxtTransporter').val()) || null;
        const   DRIVER_NAME       = $.trim($('#TxtDriverName').val()) || null;
        const   DRIVER_NO         = $.trim($('#TxtDriverMobile').val()) || null;
        const   WAYBILL_NO        = $.trim($('#TxtEWayNo').val()) || null;
        const   EWB_DATE          = formatDate($("#DtEWayDate").val()) || null;
        const   EWB_EXPDATE       = formatDate($("#TxtEWayDate").val()) || null;
        const   EWB_INVNO         = $.trim($('#TxtEWBInvNo').val()) || null;
        const   EWB_INVAMT        = parseFloat($('#TxtEWBInvAmt').val()) || 0.0;
        const   V_DATE            = formatDate($("#InDate").val()) || null;
        const   OUT_DATE          = formatDate($("#DtVehicleOutTime").val()) || null;
        const   R_TIME            = $.trim($('#TiRptDate').val()) || null;
        const   SHIP_BILLDATE     = formatDate($("#ShipBillDate").val()) || null;
        const   SHIP_PARTY        = parseInt($('#ddlShipFrom').val()) || null;
        const   SHIP_BILLNO       = $.trim($('#ShipBillNo').val()) || null;
        const   TRANSIT_NO        = parseInt($('#ddlTransit').val()) || null;

        if (!R_DATE && !R_TIME)
        {
         if (!validateRequiredField('#TxtRptDate', 'Please select Reporting Date and Time.')) return;               
        }

        if (!SHIP_BILLNO) {
            if (!BILL_NO && !CHALL_NO) {
                showToast("Bill No./Challan No. is compulsary.", { type: "warning" });
                return;
            }
        }

        if (BILL_NO && !BILL_DATE)
        {         
             if (!validateRequiredField('#DtPartyBillDate', 'Please select Party Bill Date.')) return;
        }

        if (CHALL_NO && !CHALL_DATE)
        {
            if (!validateRequiredField('#TxtChallanDate', 'Please select Challan Date.')) return;
        }         

        if (!validateRequiredField('#TxtVehicleNo', 'Please fill Vehicle No')) return;

        if (TRUCK_NO)
            {
                var numericPart = TRUCK_NO.replace(/\D/g, '');
                var lastFour = numericPart.slice(-4);

                if (lastFour)
                {
        
                if (!validateRequiredField('#TxtDriverName', 'Please enter Driver Name.')) return;
                    if (!DRIVER_NO || DRIVER_NO.toString().length !== 10) 
                    {
                        showToast("Please enter a valid 10-digit mobile number.", { type: "warning" });
                        $("#TxtDriverMobile").addClass("is-invalid").focus();
                        return;
                    }
                    else
                    {
                        $("#TxtDriverMobile").removeClass("is-invalid");
                    }
            }
        }

        if (WAYBILL_NO)
        {
            if (!validateRequiredField('#DtEWayDate', 'Please select EWayBill Date.')) return;
            if (!validateRequiredField('#TxtEWayDate', 'Please select EWayBill Expiry Date.')) return;
            if (!validateRequiredField('#TxtEWBInvNo', 'Please fill EWB Party Inv No.')) return;
            if (!validateRequiredField('#TxtEWBInvAmt', 'Please fill EWB Party Inv Amount.')) return;                                                   
        }

        if (R_DATE > V_DATE)
        {
          if (!validateRequiredField('#TxtRptDate', 'Reporting Date cannot be greater than In Date.')) return;                     
        }

        if (BILL_DATE > V_DATE)
        {
         if (!validateRequiredField('#DtPartyBillDate', 'Bill Date cannot be greater than In Date.')) return;
        }

        if (SHIP_PARTY && !SHIP_BILLNO)
        {
         if (!validateRequiredField('#ddlShipFrom', 'Shipping Bill No. is required.')) return;
        }

        if (SHIP_BILLNO && !SHIP_PARTY)
        {
         if (!validateRequiredField('#ShipBillNo', 'Shipping Party is required.')) return;                  
        }

        if (["INST", "INFU", "INRM"].includes(V_TYPE))
        {
            if (BILL_AMT == 0 && !TRANSIT_NO && !WAYBILL_NO)
            {

                invalidateField("TxtBillAmt", `Please enter the Bill Amount. This field is required.`, "warning");
                return;
            }

            if (BILL_AMT > PubDefEWaybillAmt && (!TRANSIT_NO || !WAYBILL_NO))
            {
                showToast(`Transit No./Ewaybill compulsory if Bill Amount > ${PubDefEWaybillAmt}`, { type: "info" });
            }
        }     

        if (TRANSIT_NO && EWB_EXPDATE)
        {
            const expDate = new Date(EWB_EXPDATE);
            const inDate = new Date(V_DATE);
            if (expDate < inDate)
            {
                showToast("Waybill expired on " + EWB_EXPDATE, { type: "info" });            
            }
        }        
        
        const Header = {
        V_TYPE: $('#ddlDocType').val(),
        V_NO: V_NO,
        DOC_ID: $.trim($('#TxtCode').val()) || null,
        V_DATE: V_DATE,
        OUT_DATE: OUT_DATE,
        V_TIME: $.trim($('#InTime').val()) || null,
        R_DATE: R_DATE,
        R_TIME: R_TIME,
        OUT_TIME:  $.trim($('#TiVehicleOutTime').val()) || null,
        DISP_PLAN_NO: parseInt($('#TxtPONo').val()) || null,
        DISP_PLAN_TYPE: $('#TxtPONo').val(),
        PARTY_CODE: PARTY_CODE,
        PARTY_ADDRESSID: parseInt($('#ddladdressline1').val()) || null,
        BILL_NO: BILL_NO,
        BILL_DATE: BILL_DATE,
        BILL_AMT: BILL_AMT,
        CHALL_NO: CHALL_NO,
        CHALL_DATE: CHALL_DATE,
        TRUCK_NO: TRUCK_NO,
        TRANSPORT_CODE: TRANSPORT_CODE,
        DRIVER_NAME: DRIVER_NAME,
        DRIVER_NO: DRIVER_NO,
        EWB_DATE: EWB_DATE,
        EWB_EXPDATE: EWB_EXPDATE,
        EWB_INVNO: EWB_INVNO,
        EWB_INVAMT: EWB_INVAMT,
        PARTY_WBSLIPNO: $.trim($('#TxtWbSlipNo').val()) || null,
        PARTY_WBGRWT: parseFloat($('#TxtGrWt').val()) || 0.0,
        PARTY_WBTRWT: parseFloat($('#TxtTrWt').val()) || 0.0,
        PARTY_WBTIME: formatDate($("#DtWBTime").val()) || null,
        PARTY_EWBCITY: parseInt($('#ddlPartyCity').val()) || null,
        TRANSIT_NO: parseInt($('#ddlTransit').val()) || null,
        WAYBILL_NO: WAYBILL_NO,
        REMARKS: $.trim($('#TxtRemarks').val()) || null,
        Remarks2: $.trim($('#txt_VehicleRemarks').val()) || null,
        ADD1: $.trim($('#TxtAddLine1').val()) || null,
        ADD2: $.trim($('#TxtAddLine2').val()) || null,
        ADD3: $.trim($('#TxtAddLine3').val()) || null,
        PARTY_CITY: parseInt($('#ddlcity').val()) || null,
        PARTY_GST: $.trim($('#TxtGSTNo').val()) || null,
        PARTY_PINCODE: $.trim($('#TxtPAN').val()) || null,
        SHIP_PARTY: parseInt($('#ddlShipFrom').val()) || null,
        SHIP_BILLNO: $.trim($('#ShipBillNo').val()) || null,
        SHIP_BILLDATE: formatDate($("#ShipBillDate").val()) || null,
        RETURN_TYPE: $.trim($('#VehicleReturn').val()) || null,
        CONTAINER_NO: $.trim($('#TxtContainerNo').val()) || null,
        GR_NO: $.trim($('#TxtGRNo').val()) || null,
        GR_DATE: formatDate($("#DtGRDate").val()) || null,
        STATUS: STATUS,
        action: $.trim($('#TxtCode').val()) ? 'UPDATE' : 'INSERT',
        PAN_NO: $.trim($('#TxtPAN').val()) || null,
        PARTY_NAME : PARTY_NAME
        };

        const Deatils = collectTableRowData();

        if (!Deatils || Deatils.length === 0)
        {
            showToast("Please fill at least one row in Detail", { type: "Warning" });
            return;
        }

        const itemCodeSet = new Set();

        for (let i = 0; i < Deatils.length; i++) {
         const row = Deatils[i];

        if (row.ITEM_CODE !== null) {
            if (itemCodeSet.has(row.ITEM_CODE)) {
            showToast(`Duplicate ITEM_CODE: ${row.ITEM_CODE} (Row ${i + 1})`, { type: "warning" });
            focusCell(i, 0);
            return;
            }
        itemCodeSet.add(row.ITEM_CODE);

            if (row.DEPT_CODE === null) {
            showToast(`Department required (Row ${i + 1})`, { type: "warning" });
            focusCell(i, 11);
            return;
            }

            if (row.UOM_NAME === null) {
            showToast(`Unit required (Row ${i + 1})`, { type: "warning" });
            focusCell(i, 3);
            return;
            }

            if (row.NOS === null) {
            showToast(`NOS required (Row ${i + 1})`, { type: "warning" });
            focusCell(i, 4);
            return;
            }

            if (row.QTY === null) {
            showToast(`Quantity required (Row ${i + 1})`, { type: "warning" });
            focusCell(i, 5);
            return;
            }

            if (!row.EMPTY) {
            showToast(`EMPTY field required (Row ${i + 1})`, { type: "warning" });
            focusCell(i, 7);
            return;
            }

            if (V_TYPE == "INFU" || V_TYPE == "INST" || V_TYPE == "INRM") {
            if (!row.REF_TYPE && !row.reF_NO) {
                showToast(`Reference Type and Reference No. required (Row ${i + 1})`, { type: "warning" });
                focusCell(i, 9);
            return;
            }
            }
        }
    }

        const payload = {
            Header: Header,
            Deatils: Deatils
        };


        console.log("Save payload", payload);

        $("#btn-save").prop("disabled", true);

        $.ajax({
            url: '/InwardEntry/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),

            success: function (response)
            {
                console.log("Save Response in saveInwardEntry Function", response);

            if (response.status === "Success") {   
                showToast("Saved successfully!", { type: "success" });        

                setTimeout(function () { window.location.href = '/InwardEntry/Index?id=' + V_NO + '&vtype=' + encodeURIComponent(V_TYPE) + '&mode=view'; }, 3000);                                
                          
            }

            else if (response.status === "VALIDATION")
            {
                showToast(response.message, { type: "warning" });             
            }
            else
            {
                showToast(response.message, { type: "error" });
            }
        },

            error: function (xhr) {
            let errorMessage = "Something went wrong.";

        if (xhr.status === 400) {
            errorMessage = "Bad Request: " + xhr.responseText;
            } else if (xhr.status === 500) {
            errorMessage = "Server error: " + xhr.responseText;
            } else {
            errorMessage = "Unexpected error: " + xhr.statusText;
            }
            showToast(errorMessage, {type: "error" });
            },

            complete: function () {
            $("#btn-save").prop("disabled", false);
            }
        });
    }
    function focusCell(rowIndex, colIndex) {
     const row = document.querySelectorAll('#tblInwardEntry tbody tr')[rowIndex];
    if (!row) return;

    const cell = row.querySelectorAll('td')[colIndex];
    if (!cell) return;
    cell.style.border = "2px solid red";
    cell.style.backgroundColor = "#ffe6e6";
    cell.scrollIntoView({behavior: "smooth", block: "center" });
    const input = cell.querySelector('input, select, textarea');
    if (input) {
        input.focus();
                }
}
    function setFormReadOnly() {

    const form = document.getElementById("InwardEntryForm");
    if (!form) return;

    form.classList.add("erppage-readonly");
    form.classList.add("readonly-mode");

    // Hide approval controls initially
    $("#btn_approval").hide();
    $("#btn_Sendapproval").hide();
    $("#span_approved").hide();

    // Inputs
    form.querySelectorAll("input").forEach(el => {

        if (el.type === "hidden") return;

        if (
            el.type === "text" ||
            el.type === "date" ||
            el.type === "time" ||
            el.type === "number"
        ) {
            el.readOnly = true;
        }
        else {
            el.disabled = true;
        }
    });

    // Textareas
    form.querySelectorAll("textarea").forEach(el => {
        el.readOnly = true;
    });

    // Selects
    form.querySelectorAll("select").forEach(el => {
        el.disabled = true;
    });

    // Buttons
    form.querySelectorAll("button").forEach(btn => {

        // Skip approval buttons
        if (
            btn.id === "btn_approval" ||
            btn.id === "btn_Sendapproval"
        ) {
            btn.disabled = false;
            return;
        }

        const txt = (btn.innerText || "").trim().toLowerCase();

        if (
            !txt.includes("back") &&
            !txt.includes("close")
        ) {
            btn.disabled = true;
        }
    });

    // Icons
    form.querySelectorAll(`
        .input-icon,
        .fa-search,
        .fa-cog,
        .fa-database,
        .fa-ellipsis-h
    `).forEach(icon => {

        icon.style.pointerEvents = "none";
        icon.style.opacity = "0.5";
        icon.style.cursor = "not-allowed";
    });

    // Modal triggers
    form.querySelectorAll("[data-bs-toggle='modal']").forEach(el => {

        el.removeAttribute("data-bs-toggle");
        el.removeAttribute("data-bs-target");

        el.style.pointerEvents = "none";
        el.style.opacity = "0.5";
        el.style.cursor = "not-allowed";
    });

    // Table controls
    form.querySelectorAll(`
        table input,
        table select,
        table textarea,
        table button,
        table .fa,
        table span
    `).forEach(el => {

        // Skip approval status
        if (el.id === "span_approved")
            return;

        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
            el.readOnly = true;
        }
        else if (
            el.tagName === "SELECT" ||
            el.tagName === "BUTTON"
        ) {
            el.disabled = true;
        }

        el.style.pointerEvents = "none";
        el.style.opacity = "0.5";
    });

    // Allow tabs
    $('.erppage-tab[data-tab="partydetails"]').prop('disabled', false);
    $('.erppage-tab[data-tab="shippinginfo"]').prop('disabled', false);
    $('.erppage-tab[data-tab="billchallan"]').prop('disabled', false);
}
    function collectTableRowData() {
            const table = document.getElementById('tblInwardEntry');
    if (!table) return [];
    const rows = table.querySelectorAll('tbody tr');
    const rowData = [];

                rows.forEach(row => {
                    const itemSelect = row.querySelector('.ItemName');
    const deptSelect = row.querySelector('.DeptName');
    const unitSelect = row.querySelector('.unit');
    const itemCode = parseInt(row.querySelector('.itemCode')?.value);
    if (!itemCode) return;
                    const getSelectData = (select) => {
                        if (!select) return {code: null, name: '' };
    const code = select.value ? parseInt(select.value) : null;
                        const name = select.selectedOptions.length > 0  ? select.selectedOptions[0].text  : '';
    return {code, name};
                    };

    const item = getSelectData(itemSelect);
    const dept = getSelectData(deptSelect);
    const unit = getSelectData(unitSelect);

    rowData.push({
        ITEM_CODE: itemCode,
    ITEM_NAME: item.name,
    DEPT_CODE: dept.code,
    Department: dept.name,
    UOM_CODE: unit.code,
    UOM_NAME: unit.name,
    NOS: parseInt(row.querySelector('.nos')?.value) || null,
    QTY: parseFloat(row.querySelector('.quantity')?.value) || null,
    SHIP_RATE: parseFloat(row.querySelector('.shiprate')?.value) || null,
    EMPTY: row.querySelector('.Empty')?.value || '',
    REMARKS: row.querySelector('.remarks')?.value || '',
    REF_TYPE: row.querySelector('.refType')?.value || '',
    REF_NO: parseInt(row.querySelector('.refNo')?.value) || null
                    });
                });

    return rowData;
            }
    function formatDate(dateStr) {
              if (!dateStr) return '';
    const d = new Date(dateStr);
    if (isNaN(d)) return '';

    return d.getFullYear() + '-' +
    String(d.getMonth() + 1).padStart(2, '0') + '-' +
    String(d.getDate()).padStart(2, '0');
            }

    async function LoadDropDown() {
    try {
        await Promise.all([
            DDLVtype(),
            DDLParty(),
            DDLShipFrom(),
            DDDocStatus(),
            DDlPartyCity(),
            LoadItemMaster(),
            LoadUnitMaster(),
            LoadDeptMaster(),
            DDlTransportname(),
            DDlCity(),
            DDlState(),
            DDlpono()           

        ]);
        } catch (error) {
         showToast("Error loading dropdowns", { type: "error" });

        }
    }
   function populateTable(data) {

    const tbody = $("#tblellipsisIconmodal tbody");
    tbody.empty();

    const uniqueRows = new Set();

    data.forEach(function (row) {

        const key = `${row.saudA_NO}_${row.iteM_CODE}`;

        if (uniqueRows.has(key)) {
            return; // Skip duplicate row
        }

        uniqueRows.add(key);

        let tr = `<tr>
            <td><input type="checkbox" class="rowCheckbox" /></td>
            <td>${row.saudA_NO}</td>
            <td>${row.saudaDate}</td>
            <td>${row.itemName}</td>
            <td>${row.iteM_CODE}</td>
            <td>${row.qty}</td>
            <td>${row.rate}</td>
            <td>${row.supplieR_INVNO}</td>
            <td>${row.supplieR_INVDATE}</td>
            <td>${row.supplieR_INVAMT}</td>
            <td>${row.containeR_NO}</td>
            <td>${row.grS_WEIGHT}</td>
            <td>${row.conT_SIZE}</td>
            <td>${row.v_no}</td>
            <td style="display:none;"></td>
        </tr>`;

        tbody.append(tr);
    });
}
   function getSelectedPendingOrderRows() {
        const selectedRows = [];
        $('#tblpendingordermodal tbody tr').each(function () {
        const checkbox = $(this).find('.rowCheckbox');
    if (checkbox.is(':checked')) {
        const row = $(this).children('td');
        const rowData = {
            itemCode: row.eq(1).text().trim(),
            itemName: row.eq(2).text().trim(),
            unit: row.eq(3).text().trim(),
            nos: row.eq(4).text().trim(),
            qty: row.eq(5).text().trim(),
            balQty: row.eq(6).text().trim(),
            docType: row.eq(7).text().trim(),
            docNo: row.eq(8).text().trim(),
            docDate: row.eq(9).text().trim(),
            rate: row.eq(10).text().trim(),
            remarks: row.eq(11).text().trim(),
            department: row.eq(12).text().trim(),
            deptCode: row.eq(13).text().trim(),
            emptY_YN: row.eq(14).text().trim(),
            UOM_CODE: row.eq(15).text().trim()
        };
        selectedRows.push(rowData);
    }
   });

        return selectedRows;
    }
   function populateInwardEntryTable(selectedData) {

    const $tbody = $('#tblInwardEntry tbody');

    $.each(selectedData, function (idx, item) {

        // Find first empty row
        let $emptyRow = $tbody.find('tr').filter(function () {

            const itemCode = $.trim($(this).find('.itemCode').val());
            const itemName = $(this).find('.ItemName').val();

            return (!itemCode && !itemName);
        }).first();

        if ($emptyRow.length) {

            // Populate existing empty row
            $emptyRow.find('.itemCode').val(item.itemCode);
            $emptyRow.find('.ItemName').val(item.itemCode).trigger('change');
            $emptyRow.find('.DeptName').val(item.deptCode).trigger('change');
            $emptyRow.find('.unit').val(item.uoM_CODE).trigger('change');

            $emptyRow.find('.nos').val(item.nos);
            $emptyRow.find('.quantity').val(item.balQty);
            $emptyRow.find('.shiprate').val(item.rate);
            $emptyRow.find('.Empty').val(item.emptY_YN);

            $emptyRow.find('.remarks').val(item.remarks);
            $emptyRow.find('.refType').val(item.docType);
            $emptyRow.find('.refNo').val(item.docNo);

        } else {

            // No empty row found → add new row
            addRow($tbody, {
                itemCode: item.itemCode,
                itemId: item.itemCode,
                DeptCode: item.deptCode,
                DepttName: item.deptCode,
                unit: item.uoM_CODE,
                nos: item.nos,
                qty: item.balQty,
                shipRate: 0,
                empty: item.emptY_YN,
                remarks: item.remarks,
                refType: item.docType,
                refNo: item.docNo
            });

        }
    });
}

   async function checkValidDate() {
        const data = {
            vdate: $("#InDate").val(),
            vtype: $("#ddlDocType").val(),
            vno: $("#TxtDocNo").val()
        };
        try {
            const response = await fetch('/InwardEntry/CheckValidDate', {
            method: 'POST',
            headers: {
            'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

            const result = await response.json();

            if (result.status === false) {
            showToast("result.message", { type: "warning" });
            return false;
            }

            return true;

        } catch (error) {
        showToast("result.message", { type: "warning" });
        return false;
        }
   } 
   function addRow($tbody, data = {}) {

    const isINMS = $('#ddlDocType').val() === 'INMS';
    const isNewRow = !data || Object.keys(data).length === 0;

    const normalStyle = "background-color:#fff;opacity:1;color:#000;";

    // ================= ITEM =================
    let itemOptions = `<option value="">Select</option>`;
    $.each(itemList, function (i, item) {
        const selected = item.value == data.itemId ? "selected" : "";
        itemOptions += `<option value="${item.value}" data-code="${item.code}" ${selected}>${item.text}</option>`;
    });

    // ================= DEPARTMENT =================
    let deptOptions = `<option value="">Select</option>`;
    $.each(deptList, function (i, item) {
        const selected = item.value == data.DepttName ? "selected" : "";
        deptOptions += `<option value="${item.value}" ${selected}>${item.text}</option>`;
    });

    // ================= UNIT =================
    let unitOptions = `<option value="">Select</option>`;
    $.each(unitList, function (i, item) {
        const selected = item.value == data.unit ? "selected" : "";
        unitOptions += `<option value="${item.value}" ${selected}>${item.text}</option>`;
    });

    // ================= ROW HTML =================
    const row = `
    <tr class="no-border-input">

        <td>
            <input type="text" class="erppagetable-control itemCode numeric-only"
                   style="${normalStyle}"
                   value="${data.itemCode ?? ''}" readonly />
        </td>

        <td>
            <select class="erppagetable-control ItemName searchable-item"
                    style="${normalStyle}; width:350px;">
                ${itemOptions}
            </select>
        </td>

        <td>
            <select class="erppagetable-control DeptName" style="${normalStyle}">
                ${deptOptions}
            </select>
        </td>

        <td>
            <select class="erppagetable-control unit" style="${normalStyle}">
                ${unitOptions}
            </select>
        </td>

        <td>
            <input type="text" class="erppagetable-control nos numeric-only"
                   maxlength="4"
                   style="${normalStyle}"
                   value="${data.nos ?? ''}" />
        </td>

        <td>
            <input type="text" class="erppagetable-control quantity numeric-only"
                   maxlength="10"
                   style="${normalStyle}"
                   value="${data.qty ?? ''}" />
        </td>

        <td>
            <input type="text" class="erppagetable-control shiprate numeric-only"
                   maxlength="13"
                   style="${normalStyle}"
                   value="${data.shipRate ?? ''}" />
        </td>

        <td>
            <select class="erppagetable-control Empty">
                <option value="">Select</option>
                <option value="Yes" ${data.empty === 'Yes' ? 'selected' : ''}>Yes</option>
                <option value="No" ${data.empty === 'No' ? 'selected' : ''}>No</option>
            </select>
        </td>

        <td>
            <input type="text" class="erppagetable-control remarks"
                   maxlength="225"
                   style="${normalStyle}"
                   value="${data.remarks ?? ''}" />
        </td>

        <td>
            <input type="text" class="erppagetable-control refType"
                   maxlength="4"
                   style="${normalStyle}"
                   value="${data.refType ?? ''}" readonly />
        </td>

        <td>
            <input type="text" class="erppagetable-control refNo"
                   maxlength="9"
                   style="${normalStyle}"
                   value="${data.refNo ?? ''}" readonly />
        </td>

        <td class="action-col">
            <div class="action-wrap">
                <button class="act-btn add btn-add btn-add-row" title="Add Row" style="cursor:pointer;"><i class="fa fa-plus-circle"></i></button>
                <button class="act-btn delete btn-delete btn-delete-action" title=" Row" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
            </div>
        </td>

    </tr>`;

    // ================= APPEND ROW =================
    $tbody.append(row);

    const $row = $tbody.find('tr:last');

    // ================= SELECT2 =================
    $row.find('.searchable-item').select2({
        placeholder: "Search Item",
        width: '100%'
    });

    // ================= RULE ENGINE =================
    function applyRules() {

        const refType = $.trim($row.find('.refType').val());
        const refNo = $.trim($row.find('.refNo').val());

        const isINMS = $('#ddlDocType').val() === 'INMS';

        // Always readonly
        $row.find('.refType, .refNo').prop('readonly', true);

        if (isINMS) {
            $row.find('.ItemName').prop('disabled', false);
            $row.find('.DeptName').prop('disabled', false);
            $row.find('.unit').prop('disabled', false);
            // ENTRY FIELDS ENABLED
            $row.find('.nos').prop('readonly', false);
            $row.find('.quantity').prop('readonly', false);
            $row.find('.shiprate').prop('readonly', false);
            $row.find('.remarks').prop('readonly', false);
            $row.find('.Empty').prop('disabled', false);

        }
        else
        {
            // NON INMS → MASTER ALWAYS DISABLED
            $row.find('.ItemName').prop('disabled', true);
            $row.find('.DeptName').prop('disabled', false);
            $row.find('.unit').prop('disabled', false);
            // ENTRY FIELDS ENABLED
            $row.find('.nos').prop('readonly', false);
            $row.find('.quantity').prop('readonly', false);
            $row.find('.shiprate').prop('readonly', false);
            $row.find('.remarks').prop('readonly', false);
            $row.find('.Empty').prop('disabled', false);
        }

        $row.find('.ItemName').trigger('change.select2');
    }

    applyRules();

    // ================= EVENTS =================
    $row.find('.btn-add-row').on('click', function () {
        addRow($('#tblInwardEntry tbody'));
    });

    $row.find('.btn-delete-action').on('click', function () {
        $(this).closest('tr').remove();
    });

    $row.find('.numeric-only').on('input', function () {
        this.value = this.value.replace(/[^0-9.]/g, '');
    });

    if (isNewRow) {
        $row.find('.itemCode').val('');
    }

    $tbody.find('.btn-add-row').show();
}

   async function getcontainerdata(Container_No) {
          try {
            const res = await $.ajax({
              url: '/InwardEntry/GetSEARCHCONTAINER',
              type: 'GET',
              data: { Container_No: Container_No }
            });

            if (res && res.supplier) {
              $('#ddlPartyName').val(res.supplier).trigger('change');
                await DDlPartyAdd(res.supplier);
                await GetPartyAdress(res.supplier);
                const Vno = document.getElementById('TxtDocNo')?.value || '';
                const v_type = document.getElementById('ddlDocType')?.value || '';
                const indate = document.getElementById('InDate')?.value || '';
                await fetchTransitno(v_type, Vno, res.supplier, indate);
                } else {
                showToast("Invalid response or supplier missing", { type: "error" });

                }
          }
          catch (error) {
            showToast(err, { type: "warning" });
          }
        }
  function validateMobile(input) {
          input.value = input.value.replace(/\D/g, '');
          if (input.value.length > 10) {
            input.value = input.value.slice(0, 10);
          }
}
 function getSelectedRows() {
            const selectedData = [];

            $("#tblellipsisIconmodal tbody tr").each(function () {
                const checkbox = $(this).find(".rowCheckbox");

                if (checkbox.is(":checked")) {
                    const rowData = {
                        saudA_NO: $(this).find("td:eq(1)").text(),
                        saudaDate: $(this).find("td:eq(2)").text(),
                        itemName: $(this).find("td:eq(3)").text(),
                        iteM_CODE: $(this).find("td:eq(4)").text(),
                        qty: $(this).find("td:eq(5)").text(),
                        rate: $(this).find("td:eq(6)").text(),
                        supplieR_INVNO: $(this).find("td:eq(7)").text(),
                        supplieR_INVDATE: $(this).find("td:eq(8)").text(),
                        supplieR_INVAMT: $(this).find("td:eq(9)").text(),
                        containeR_NO: $(this).find("td:eq(10)").text(),
                        grS_WEIGHT: $(this).find("td:eq(11)").text(),
                        conT_SIZE: $(this).find("td:eq(12)").text(),
                        v_no: $(this).find("td:eq(13)").text()
                    };

                    selectedData.push(rowData);
                }
            });

            return selectedData;
}

 async function LoadFormByID(id, vtype) {
    try {
        const res = await $.ajax({
            url: '/InwardEntryList/GetDataByCode',
            method: 'POST',
            data: { code: id, vtype: vtype }
        });

        if (res.success) {
            const header = res.data.header;
            const Details = res.data.details;            

            if (header.partY_WBSLIPNO !== '') {

                $('#TxtGrWt, #TxtTrWt, #TxtWbTime, #DtWBTime')
                    .removeClass('erppage-input')
                    .addClass('erppage-redinput');

            } else {

                $('#TxtGrWt, #TxtTrWt, #TxtWbTime, #DtWBTime')
                    .removeClass('erppage-redinput')
                    .addClass('erppage-input');
            }
            $('#ddlDocType').val(header.v_TYPE || '');
            $('#TxtPONo').val(header.disP_PLAN_NO || '').trigger('change');
            $('#TxtTransporter').val(header.transporT_CODE || '');
            $('#TxtCode').val(header.doC_ID || '');
            $('#TxtDocNo').val(header.v_NO || '');
            $('#InDate').val(formatDate(header.v_DATE) || '');
            $('#DtVehicleOutTime').val(formatDate(header.Out_Date) || '');
            $('#InTime').val(header.v_TIME || '');
            $('#ddlPartyName').val(header.partY_CODE).trigger('change');
            $('#TxtAddLine1').val(header.add1 || '');
            $('#TxtAddLine2').val(header.add2 || '');
            $('#TxtAddLine3').val(header.add3 || '');
            $('#TxtCity').val(header.city || '');
            $('#ddlcity').val(header.partY_CITY || '');
            $('#TxtPincode').val(header.partY_PINCODE || '');
            $('#TxtState').val(header.state || '');
            $('#TxtGSTNo').val(header.partY_GST || '');
            $('#TxtPAN').val(header.paN_NO || '');
            $('#ddlShipFrom').val(header.shiP_PARTY).trigger('change');
            $('#txtShipAddress').val(header.shipAddress || '');
            $('#ShipBillNo').val(header.shiP_BILLNO || '');
            $('#ShipBillDate').val(formatDate(header.shiP_BILLDATE) || '');
            $('#DtVehicleOutTime').val(formatDate(header.ouT_DATE) || '');
            $('#TiVehicleOutTime').val(header.ouT_TIME || '');
            $('#VehicleReturn').val(header.returN_TYPE || '');         
            $('#TxtRptDate').val(formatDate(header.r_DATE) || '');
            $('#TiRptDate').val(header.r_TIME || '');
            $('#TxtBillNo').val(header.bilL_NO || '');
            $('#DtPartyBillDate').val(formatDate(header.bilL_DATE) || '');
            $('#TxtChallanNo').val(header.chalL_NO || '');
            $('#TxtChallanDate').val(formatDate(header.chalL_DATE) || '');
            $('#TxtBillAmt').val(header.bilL_AMT || '');
            $('#ddlDocStatus').val(header.status || '');
            $('#TxtEWayNo').val(header.waybilL_NO || '');
            $('#DtEWayDate').val(formatDate(header.ewB_DATE) || '');
            $('#TxtEWayDate').val(formatDate(header.ewB_DATE) || '');
            $('#TxtEWBInvNo').val(header.ewB_INVNO || '');
            $('#TxtEWBInvAmt').val(header.ewB_INVAMT || '');
            $('#TxtWbSlipNo').val(header.partY_WBSLIPNO || '');
            $('#TxtGrWt').val(header.partY_WBGRWT || '');
            $('#TxtTrWt').val(header.partY_WBTRWT || '');
            $('#DtWBTime').val(formatDate(header.partY_WBTIME) || '');
            $('#TxtWbTime').val(header.partY_WBTIME || '');
            $('#ddlPartyCity').val(header.partY_EWBCITY || '');
            $('#TxtContainerNo').val(header.containeR_NO || '');
            $('#TxtRemarks').val(header.remarks || '');
            $('#TxtVehicleNo').val(header.trucK_NO || '');
            $('#TxtGRNo').val(header.gR_NO || '');
            $('#DtGRDate').val(formatDate(header.gR_DATE) || '');
            $('#TxtDriverName').val(header.driveR_NAME || '');
            $('#TxtDriverMobile').val(header.driveR_NO || '');
            $('#txt_VehicleRemarks').val(header.remarks2 || '');


            Details.forEach(item => {
                addRow($('#tblInwardEntry tbody'), {
                    itemCode: item.iteM_CODE,
                    itemId: item.iteM_CODE,
                    DepttName: item.depT_CODE,
                    unit: item.uoM_CODE,
                    nos: item.nos,
                    qty: item.qty,
                    shipRate: item.shiP_RATE,
                    empty: item.empty,
                    remarks: item.remarks,
                    refType: item.reF_TYPE,
                    refNo: item.reF_NO
                });
            });

            await DDlPartyAdd(header.partY_CODE);
            $('#ddladdressline1').val(header.partY_ADDRESSID || '');

           await  fetchTransitno(header.v_TYPE, header.v_NO, header.partY_CODE, formatDate(header.ewB_DATE));
            $('#ddlTransit').val(header.transiT_NO || '');

        }
    } catch (err) {
        showToast("Something went wrong while loading the form.", { type: "error" });
    }
}