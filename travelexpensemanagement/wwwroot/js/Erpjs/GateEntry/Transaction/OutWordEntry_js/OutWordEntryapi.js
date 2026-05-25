
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

    ddl.empty().append('<option value="">-- Select Party --</option>');
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
                document.getElementById("Conditionnaldesignid").style.display = "flex";
            } else {
                document.getElementById("Conditionnaldesignid").style.display = "none";
            }



            $('#TxtCode').val(header.doC_ID || '');
            $('#ddlDocType').val(header.v_TYPE || '');
            $('#NumDocNo').val(header.v_NO || '');
            $('#DtDocDate').val(formatDate(header.v_DATE));
            DDLParty().then(() => { $('#ddlPartyName') .val(header.partY_CODE || '') .trigger('change');
            });
            $('#TxtVehicleNo').val(header.trucK_NO || '');
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

async function FetchPendindorderno(PartyCode, Type, v_date, BILL_NO) {
    try {
        const res = await fetch(`/OutwardEntryList/GetDataByPendingorder?PartyCode=${PartyCode}&Type=${Type}&v_date=${v_date}&BILL_NO=${BILL_NO}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const result = await res.json();
        if (result.success) {
            const details = result.data || [];
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
        } else {
            showToast(`Failed to load pending orders: ${result.message}`, { type: "error" });
        }
    } catch (error) {
        console.error(error);
        showToast(`Failed to load pending orders`, { type: "error" });
    }
}


