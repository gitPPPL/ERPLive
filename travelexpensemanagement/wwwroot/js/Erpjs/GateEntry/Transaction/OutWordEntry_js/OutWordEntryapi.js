

async function loadItemMaster() {
    const res = await fetch("/OutwardEntry/DDLItemMaster");
    const data = await res.json();
    itemMap = {};
    data.forEach(i => itemMap[i.value] = i.text);
}

async function loadDeptMaster() {
    const res = await fetch("/OutwardEntry/DDLDeptMaster");
    const data = await res.json();
    DeptMap = {};
    data.forEach(i => DeptMap[i.value] = i.text);
}

async function loadUnit() {
    const res = await fetch("/OutwardEntry/DDLUnit");
    const data = await res.json();
    UnitMap = {};
    data.forEach(i => UnitMap[i.value] = i.text);
}

async function DDLVtype() {
    const res = await fetch("/OutwardEntry/DDlVType");
    const list = await res.json();
    const ddl = $("#ddlDocType");
    ddl.empty().append('');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));
}

async function DDLParty() {
    const res = await fetch("/OutwardEntry/DDlParty");
    const list = await res.json();
    const ddl = $("#ddlPartyName");

    ddl.empty().append('<option value="">Select Party Name</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Party --",
        allowClear: true
    });
}

async function DDLcity_mast() {
    const res = await fetch("/OutwardEntry/DDLcity_mast");
    const list = await res.json();
    const ddl = $("#ddlCity");
    ddl.empty().append('<option value="">-- Select Party City--</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));
}

async function GetVNo(Vtype, tableName) {
    const res = await fetch(`/OutwardEntry/GetVNo?Vtype=${encodeURIComponent(Vtype)}&tableName=${encodeURIComponent(tableName)}`);
    const data = await res.json();
    if (data.v_NO) {
        $('#NumDocNo').val(data.v_NO);
    } else {
        console.warn('No document number received');
    }
}

async function loadPartyAddresses(partyId) {
    const res = await fetch(`/OutwardEntry/fetchPartyAdd?PartyId=${encodeURIComponent(partyId)}`);
    const list = await res.json();
    const ddl = $("#ddlPartyNameByAddress");
    ddl.empty().append('');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));
}

async function fetchPartyDetails(partyId) {
    const baseUrl = "/OutwardEntry/GetDataByPartyCode";
    const queryParams = `PartyId=${encodeURIComponent(partyId)}`;
    const url = `${baseUrl}?${queryParams}`;
    const res = await fetch(url);
    const details = await res.json();
    if (details.length) {
        const d = details[0];

        $("#TxtAdd1PD").val(d.add1 || "");
        $("#TxtAdd2PD").val(d.add2 || "");
        $("#TxtAdd3PD").val(d.add3 || "");
        $("#ddlCity").val(d.city_Code || "");
        $("#NumPincode").val(d.pincode || "");
        $("#TxtState").val(d.state || "");
        $("#TxtGSTNo").val(d.gstin || "");
    } else {
        clearFields();
    }
}

async function GetDataByPartyandAddressidCodeAsync(partyId, addressId) {
    const baseUrl = "/OutwardEntry/GetDataByPartyandAddressidCode";
    const queryParams = `PartyId=${encodeURIComponent(partyId)}&AddressId=${encodeURIComponent(addressId)}`;
    const url = `${baseUrl}?${queryParams}`;

    const res = await fetch(url);
    const details = await res.json();

    if (details.length) {
        const d = details[0];

        $("#TxtAdd1PD").val(d.add1 || "");
        $("#TxtAdd2PD").val(d.add2 || "");
        $("#TxtAdd3PD").val(d.add3 || "");
        $("#ddlCity").val(d.city_Code || "");
        $("#NumPincode").val(d.pincode || "");
        $("#TxtState").val(d.state || "");
        $("#TxtGSTNo").val(d.gstin || "");
    } else {
        clearFields();
    }
}

function LoadFormByID(rowId, vtype) {
    $.ajax({
        url: '/OutwardEntryList/GetDataByCode',
        method: 'POST',
        data: {
            rowId: rowId,
            vtype: vtype
        },
        success: function (result) {
            if (!result.success || !result.data || !result.data.header) {
                showToast("Invalid or missing response data.", { type: "error" });
                return;
            }

            const header = result.data.header;
            const details = result.data.details;

            console.log("header", header);
            console.log("Data", details);

            if (header.v_TYPE === "OURT") {
                document.getElementById("Conditionnaldesignid").style.display = "contents";
            } else {
                document.getElementById("Conditionnaldesignid").style.display = "none";
            }
            document.getElementById("ddlPartyName").disabled = true;
            $('#TxtCode').val(header.doC_ID || '');
            $('#ddlDocType').val(header.v_TYPE || '');
            $('#NumDocNo').val(header.v_NO || '');
            $('#DtDocDate').val(formatDate(header.v_DATE));
            $('#DtExpectedDateReturn').val(formatDate(header.returN_DATE));
            DDLParty().then(() => { $('#ddlPartyName') .val(header.partY_CODE || '') .trigger('change');
            });
            $('#TxtVehicleNo').val(header.trucK_NO || '');
            $('#txtResponsiblePerson').val(header.responsiblE_PERSONB || '');
            $('#TxtWayBillNo').val(header.waybilL_NO || '');
            $('#TxtRemarks').val(header.remarks || '');
            $('#TxtAdd1PD').val(header.add1 || '');
            $('#TxtAdd2PD').val(header.add2 || '');
            $('#TxtAdd3PD').val(header.add3 || '');

            $('#ddlCity').val(header.partY_CITY || '');
            $('#TxtGSTNo').val(header.partY_GST || '');
            $('#NumPincode').val(header.partY_PINCODE || '');
            $('#ddlType').val(header.iteM_TYPE || '');
            $('#DtTxtDocDate').val(header.v_TIME || '');
            loadPartyAddresses(header.partY_CODE).then(() => {
                $('#ddlPartyNameByAddress').val(header.partY_ADDRESSID || '');
            });

            const $tbody = $("#tblOutwardEntry tbody");
            $tbody.empty();

            (details || []).forEach(detail => {
                const rowData = {
                    code: detail.iteM_CODE || '',
                    itemName: detail.iteM_CODE || '',
                    department: detail.depT_CODE || '',
                    unit: detail.uoM_CODE || '',
                    no: detail.nos || '',
                    quantity: detail.qty || '',
                    remarks: detail.remarks || '',
                    refType: detail.reF_TYPE || '',
                    refNo: detail.reF_NO || ''
                };

                addRow($tbody, rowData);
            });
        },
        error: function (xhr, status, error) {
            showToast("Error loading form data:", { type: "error" });

        }
    });
}

async function FetchPendindorderno(PartyCode, Type, v_date) {
    try {
        const res = await fetch(`/OutwardEntryList/GetDataByPendingorder?PartyCode=${PartyCode}&Type=${Type}&v_date=${v_date}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const result = await res.json();

        if (result.success) {

            const details = result.data || [];

            if (details.length === 0) {
                showToast("No pending orders found.", { type: "info" });
                return; // Stop further execution
            }


            pendingData = details.map(detail => ({
                Vouchertype: detail.v_type,
                VoucherNo: detail.v_no,
                VoucherDate: formatDate(detail.v_DATE),
                ItemCode: detail.item_code,
                ItemName: detail.item_name,
                Qty: detail.qty,
                PQty: detail.p_QTY,
                remarks: detail.remark,
                nos: detail.nos,
                UnitName: detail.uniT_NAME,
                UnitCode: detail.uniT_CODE,
                SRno: detail.srno,
                selected: false
            }));

            currentPage = 1;
            renderPendingTable();

            const modalElement = document.getElementById('pendingorders');
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
 

        } else {
            showToast(`Failed to load pending orders: ${result.message}`, { type: "error" });
        }
    }
    catch (error) {
        console.error(error);
        showToast(`Failed to load pending orders`, { type: "error" });
    }
}

function TransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }




    var reportName = "gatepass1";
        // Get input values
    var v_no = $('#NumDocNo').val();
    var v_type = $('#ddlDocType').val();

    // Ensure global variables exist





    // Build Crystal Reports selection formula
    var formula =
        "{GATE1.comp_code} = " + globalVars.CompCode +
        " and {GATE1.Year_code} = " + globalVars.FYearCode +
        " and {GATE1.branch_code} = " + globalVars.BranchCode +
        " and {GATE1.V_no} = " + v_no +
        " and {GATE1.v_type} = '" + v_type + "'";

    // Prepare the payload for the API
    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            GST: globalVars.GST || "",
            PAN: globalVars.PAN || "",
            COMP_PHONE: globalVars.Phone || "",
            EMAIL: globalVars.Email || "",
            RPTNAME: "FACTORY GATE PASS FOR OUTGOING MATERIAL"
        }
    };

    // Timestamp for file name
    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    // AJAX call to the Crystal Report API
    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport', // check port
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' }, // Important for PDF

        success: function (response) {
            // Convert response to a Blob
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            // Trigger download
            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {
            if (xhr.status === 0) {
                console.error("Cannot connect to API. Is the backend running?");
            } else {
                console.error('Error generating report:', xhr.status, xhr.statusText, error);
                xhr.responseText && console.error('Response:', xhr.responseText);
            }
        }
    });
}



async function fetchPendingOrderHeaderData(REF_TYPE, REF_NO) {
    try {
        const baseUrl = "/OutwardEntry/GetPendingrowHeaderData";
        const queryParams = `REF_TYPE=${encodeURIComponent(REF_TYPE)}&REF_NO=${encodeURIComponent(REF_NO)}`;
        const url = `${baseUrl}?${queryParams}`;

        const res = await fetch(url);
        if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);

        const details = await res.json();
        if (details.length) {
            const d = details[0];
            $("#TxtVehicleNo").val(d.vehiclE_NO || "");
            $("#TxtWayBillNo").val(d.ewaybilL_NO || "");
            $("#TxtAdd1PD").val(d.bilL_ADD1 || "");
            $("#TxtAdd2PD").val(d.bilL_ADD2 || "");
            $("#TxtAdd3PD").val(d.bilL_ADD3 || "");
            $("#ddlCity").val(d.bilL_CITY || "");
            $("#TxtState").val(d.statE_CODE || "");     
            $("#TxtGSTNo").val(d.bilL_GST || "");
            $("#NumPincode").val(d.bilL_PINCODE || "");
        } else {
            $("#TxtVehicleNo").val( "");
            $("#TxtWayBillNo").val( "");
            $("#TxtAdd1PD").val("");
            $("#TxtAdd2PD").val("");
            $("#TxtAdd3PD").val("");
            $("#ddlCity").val("");
            $("#TxtState").val("");
            $("#TxtGSTNo").val("");
            $("#NumPincode").val("");
        }
    } catch (error) {
        console.error("Error fetching pending order header data:", error);
  
    }
}