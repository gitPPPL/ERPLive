async function DDLVtype() {
    try {
        const res = await fetch('/InwardEntry/DDlVType');
        const data = await res.json();
        const ddl = $('#ddlDocType');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("rror loading VType:", { type: "error" });

    }
}

async function DDLParty() {
    try {
        const res = await fetch('/InwardEntry/DDlParty');
        const data = await res.json();
        const ddl = $('#ddlPartyName');

        ddl.empty().append('<option value="">-- Select Party Name --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        ddl.select2({
            placeholder: "-- Select Party Name --",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        showToast("Error loading Party:", { type: "error" });
    }
}

async function DDLShipFrom() {
    try {
        const res = await fetch('/InwardEntry/DDlShipFrom');
        const data = await res.json();
        const ddl = $('#ddlShipFrom');

        if ($.fn.select2 && ddl.hasClass("select2-hidden-accessible")) {
            ddl.select2('destroy');
        }

        ddl.empty().append('<option value="">-- Select Ship From --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        ddl.select2({
            placeholder: "-- Select Ship From --",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        showToast(error, { type: "error" });

    }
}

async function DDDocStatus() {
    try {
        const res = await fetch('/InwardEntry/DDDocStatus');
        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("Error loading Doc Status:", { type: "error" });

    }
}

async function DDlTransportname() {
    try {
        const res = await fetch('/InwardEntry/DDlTransportName');
        const data = await res.json();
        const ddl = $('#TxtTransporter');
        ddl.empty().append('<option value="">-- Select Transport Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("Error loading Transporter", { type: "error" });

    }
}

async function DDlPartyCity() {
    try {
        const res = await fetch('/InwardEntry/DDlPartycity');
        const data = await res.json();
        const ddl = $('#ddlPartyCity');
        ddl.empty().append('<option value="">-- Select Party City --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("Error loading Party City:", { type: "error" });

    }
}

async function DDlCity() {
    try {
        const res = await fetch('/InwardEntry/DDlPartycity');
        const data = await res.json();
        const ddl = $('#ddlcity');
        ddl.empty().append('<option value="">Select City</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("Error loading  City:", { type: "error" });

    }
}

async function DDlState() {
    try {
        const res = await fetch('/InwardEntry/DDlstate');
        const data = await res.json();
        const ddl = $('#TxtState');
        ddl.empty().append('<option value="">Select State</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("Error loading  State:", { type: "error" });

    }
}

async function DDlpono() {
    try {
        const res = await fetch('/InwardEntry/DDlpono');
        const data = await res.json();

        const ddl = $('#TxtPONo');

        ddl.empty().append('<option value="">Select Po No</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">${item.text} (${item.value})</option>`
            );
        });

        // Destroy existing Select2 if already initialized
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.select2('destroy');
        }

        ddl.select2({
            placeholder: "Search PO No",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        showToast("Error loading PO No", { type: "error" });
        console.error(error);
    }
}
function DDlPartyAdd(PartyId) {
    return $.ajax({
        url: '/InwardEntry/fetchSelectedAddress',
        type: 'POST',
        data: { PartyId: PartyId },
        success: function (data) {
            const ddl = $('#ddladdressline1');
            ddl.empty().append('<option value="">-- Select Address --</option>');
            data.forEach(item => {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
        },
        error: function (xhr, status, error) {
            showToast("Error loading  Party Address:", { type: "error" });

        }
    });
}

async function BillNoValidation(PARTY_CODE, BILL_NO, V_NO) {
    try {
        const response = await $.ajax({
            url: '/InwardEntry/BillNoValidation',
            type: 'GET',
            data: {
                PARTY_CODE: PARTY_CODE,
                BILL_NO: BILL_NO,
                V_NO: V_NO
            }
        });

        // Proper condition check
        if (response.success === false) {
            showToast("Invalid Bill No", { type: "error" });
            return response;
        }
        return response;

    } catch (error) {
        showToast("Validation Error", { type: "error" });
        return { success: false };
    }
}

async function GatenoValidation(V_TYPE, V_NO) {
    try {
        const response = await $.ajax({
            url: '/InwardEntry/GatenoValidation',
            type: 'GET',
            data: {
                V_TYPE: V_TYPE,
                V_NO: V_NO
            }
        });

        // Correct condition
        if (response.success === false) {
            showToast(response.message, { type: "error" });
            return response;
        }

        return response;

    } catch (error) {
        showToast("Validation Error", { type: "error" });
        return { success: false };
    }
}

async function GetPartyAdress(PartyId) {
    try {
        const url = `/InwardEntry/GetPartyAddressbyCode?PartyId=${encodeURIComponent(PartyId)}`;
        const response = await fetch(url);
        const data = await response.json();

        const d = (data && data.length > 0) ? data[0] : {};
        $('#ddladdressline1').val(d.addresS_ID ?? "");
        $('#TxtAddLine1').val(d.add1 ?? "");
        $('#TxtAddLine2').val(d.add2 ?? "");
        $('#TxtAddLine3').val(d.add3 ?? "");
        $('#ddlcity').val(d.city_Code ?? "");
        $('#TxtPincode').val(d.pincode ?? "");
        $('#TxtState').val(d.statE_CODE ?? "");
        $('#TxtGSTNo').val(d.gstin ?? "");
        $('#TxtPAN').val(d.pan ?? "");

    } catch (error) {
        showToast("Error fetch party data:", { type: "error" });

    }
}

async function fetchDDlParty(PartyId, AddressId) {
    try {
        const url = `/InwardEntry/GetDataByPartyCode?PartyId=${encodeURIComponent(PartyId)}&AddressId=${encodeURIComponent(AddressId)}`;
        const response = await fetch(url);
        const data = await response.json();
        const d = (data && data.length > 0) ? data[0] : {};
        $('#TxtAddLine1').val(d.add1 ?? "");
        $('#TxtAddLine2').val(d.add2 ?? "");
        $('#TxtAddLine3').val(d.add3 ?? "");
        $('#TxtCity').val(d.city_Code ?? "");
        $('#TxtPincode').val(d.pincode ?? "");
        $('#TxtState').val(d.statE_CODE ?? "");
        $('#TxtGSTNo').val(d.gstin ?? "");
        $('#TxtPAN').val(d.pan ?? "");

    } catch (error) {
        showToast("Failed to fetch party data:", { type: "error" });

    }
}

async function fetchShipFromAdd(ShipFromID) {
    try {
        const response = await fetch(`/InwardEntry/fetchShipFromAdd?ShipFromID=${ShipFromID}`);
        const data = await response.json();

        if (data.length > 0 && data[0].address) {
            $('#txtShipAddress').val(data[0].address);
        } else {
            $('#txtShipAddress').val('');
        }
    } catch (error) {
        showToast(" Failed to fetch ship from address:", { type: "error" });

    }
}

async function fetchTransitno(v_type, v_no, partycode, ExpiryDate, selectedTransit, mode) {
    try {

        const queryParams = new URLSearchParams({
            v_type,
            v_no,
            partycode,
            ExpiryDate,
            mode
        });

        const response = await fetch(`/InwardEntry/DDlTransitNo?${queryParams.toString()}`);

        if (!response.ok)
            throw new Error(`HTTP error! Status: ${response.status}`);

        const result = await response.json();

        const ddl = $('#ddlTransit');

        // ✅ build HTML once (FAST)
        let options = '<option value="">-- Select Transit No --</option>';

        if (result.status && Array.isArray(result.data)) {
            options += result.data
                .map(x => `<option value="${x}">${x}</option>`)
                .join('');
        }

        ddl.html(options);

        // ✅ set value WITHOUT triggering change (IMPORTANT)
        ddl.val(selectedTransit || '');

    } catch (error) {
        showToast("Error loading Transit Numbers", { type: "error" });
    }
}

async function GetVNo(Vtype) {
    try {
        const res = await fetch(`/InwardEntry/GetVNo?Vtype=${encodeURIComponent(Vtype)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const data = await res.json();

        if (data.error) throw new Error(data.error);
        if (!data.v_NO) throw new Error('Response missing V_NO');

        $('#TxtDocNo').val(data.v_NO);
    } catch (e) {
        showToast("Error loading Document Number: ", { type: "error" });

    }
}

async function FetchPendingOrderNo(PartyCode, V_TYPE, V_DATE) {
    try {
        if (!PartyCode) {
            showToast("Please Select Party Name", { type: "error" });
            return;
        }
        if (!V_TYPE) {
            showToast("Please select Voucher Type", { type: "error" });
            return;
        }

        if (!V_DATE) {
            showToast("Please Select voucher Date", { type: "error" });
            return;
        }

        const result = await $.ajax({
            url: '/InwardEntryList/GetDataByPendingorder',
            type: 'GET',
            data: { PartyCode, V_TYPE, V_DATE }
        });

        if (!result || !result.success) {
            showToast("Failed to fetch data", { type: "error" });
            return;
        }

        if (!result.data || result.data.length === 0) {
            const PartyName = $('#ddlPartyName option:selected').text();
            showToast(`Data Not Found For this party: ${PartyName}`, { type: "info" });
            return;
        }
        else {

            $('#ddlPartyName').prop('disabled', true);
            const modalElement = document.getElementById('pendingorders');
            const myModal = new bootstrap.Modal(modalElement);
            myModal.show();

            const tableBody = document.querySelector('#tblpendingordermodal tbody');
            tableBody.innerHTML = '';

            result.data.forEach(item => {
                const row = `
                <tr>
                    <td><input type="checkbox" class="rowCheckbox" /></td>
                    <td>${item.iteM_CODE ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.uniT_NAME ?? ''}</td>
                    <td>${item.packinG_NOS ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.balqty ?? ''}</td>
                    <td>${item.docType ?? ''}</td>
                    <td>${item.docNo ?? ''}</td>
                    <td>${item.docDate ?? ''}</td>
                    <td>${item.rate ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.department ?? ''}</td>
                    <td style="display:none;">${item.deptCode ?? ''}</td>
                    <td>${item.emptY_YN ?? ''}</td>
                    <td style="display:none;">${item.uoM_CODE ?? ''}</td>
                </tr>
                `;
                tableBody.insertAdjacentHTML('beforeend', row);
            });


        }    

    } catch (error) {
        showToast("Failed to load pending orders", { type: "error" });
    }
}

async function GetFasttagVehicledetail() {
    try {
        const rcNumber = $('#TxtVehicleNo').val();
        const VType = $('#ddlDocType').val();
        const VNo = $('#TxtDocNo').val();

        if (!rcNumber) {
            showToast("Please Fill Vehicle No.", { type: "info" });
            return;
        }

        const res = await $.ajax({
            url: `/InwardEntry/GetVehcleFastaginfocall`,
            data: {
                rc_number: rcNumber,
                VType: VType,
                VNo: VNo
            },
            type: 'GET',
            dataType: 'json',
        });

        // SUCCESS CASE
        if (res.success && res.data?.value?.status !== 403) {
            showToast("FastTag Info Saved Successfully", { type: "success" });
        }
        // ERROR CASE
        else {
            let errorMessage = "Something went wrong";

            // Parse API error details
            if (res.data?.value?.details) {
                try {
                    const apiError = JSON.parse(res.data.value.details);
                    errorMessage = apiError.message || errorMessage;
                } catch (e) {
                    console.log("Error parsing API details", e);
                }
            }

            showToast(errorMessage, { type: "info" });
        }

    } catch (err) {
        console.log(err);
        showToast("Error fetching vehicle details.", { type: "error" });
    }
}

async function GetVehicledetail() {
    try {
        const rcNumber = $('#TxtVehicleNo').val();
        const VType = $('#ddlDocType').val();
        const VNo = $('#TxtDocNo').val();
        if (!rcNumber) {
            showToast("Please Fill Vehicle No.", { type: "info" });           
            return;
        }     
        const res = await $.ajax({
            url: `/InwardEntry/GetVehcleinfo`,
            data: { rc_number: rcNumber, VType: VType, VNo: VNo },
            type: 'GET',
            dataType: 'json',
        });
        console.log("res", res);
        if (res.value.success == true) {
            showToast("Vehicle Info Saved Successfully", { type: "success" });
        } else {
            const apiError = JSON.parse(res.value.message);
            showToast(apiError.message, { type: "info" });
        }
    } catch (err) {
        showToast("Error fetching vehicle details.", { type: "error" });
    }
}

async function GetVehicledata() {
    try {
        const res = await $.ajax({
            url: `/InwardEntry/GetVehicledetail`,
            type: 'GET',
            data: {
                v_no: $('#TxtDocNo').val(),
                v_type: $('#ddlDocType').val()
            },
            dataType: 'json',
        });

        $('#tblRCDetaillist tbody').empty();
        $("#RCDetailLabel").text(res.rc_number);

        let row = `<tr>
        <td>${res.client_id || ''}</td>
        <td>${res.rc_number || ''}</td>
        <td>${formatDate(res.registration_date)}</td>
        <td>${res.owner_name || ''}</td>
        <td>${res.father_name || ''}</td>
        <td>${res.present_address || ''}</td>
        <td>${res.permanent_address || ''}</td>
        <td>${res.mobile_number || ''}</td>
        <td>${res.vehicle_category || ''}</td>
        <td>${res.vehicle_chasi_number || ''}</td>
        <td>${res.vehicle_engine_number || ''}</td>
        <td>${res.maker_description || ''}</td>
        <td>${res.maker_model || ''}</td>
        <td>${res.body_type || ''}</td>
        <td>${res.fuel_type || ''}</td>
        <td>${res.color || ''}</td>
        <td>${res.norms_type || ''}</td>
        <td>${formatDate(res.fit_up_to)}</td>
        <td>${res.financer || ''}</td>
        <td>${res.financed ? 'Yes' : 'No'}</td>
        <td>${res.insurance_company || ''}</td>
        <td>${res.insurance_policy_number || ''}</td>
        <td>${formatDate(res.insurance_upto)}</td>
        <td>${formatDate(res.manufacturing_date)}</td>
        <td>${res.manufacturing_date_formatted || ''}</td>
        <td>${res.registered_at || ''}</td>
        <td>${res.data_updated_by || ''}</td>
        <td>${res.less_info ? 'Yes' : 'No'}</td>
        <td>${formatDate(res.tax_upto)}</td>
        <td>${formatDate(res.tax_paid_upto)}</td>
        <td>${res.cubic_capacity || ''}</td>
        <td>${res.vehicle_gross_weight || ''}</td>
        <td>${res.no_cylinders || ''}</td>
        <td>${res.seat_capacity || ''}</td>
        <td>${res.sleeper_capacity || ''}</td>
        <td>${res.standing_capacity || ''}</td>
        <td>${res.wheelbase || ''}</td>
        <td>${res.unladen_weight || ''}</td>
        <td>${res.vehicle_category_description || ''}</td>
        <td>${res.pucc_number || ''}</td>
        <td>${formatDate(res.pucc_upto)}</td>
        <td>${res.permit_number || ''}</td>
        <td>${formatDate(res.permit_issue_date)}</td>
        <td>${formatDate(res.permit_valid_from)}</td>
        <td>${formatDate(res.permit_valid_upto)}</td>
        <td>${res.permit_type || ''}</td>
        <td>${res.national_permit_number || ''}</td>
        <td>${formatDate(res.national_permit_upto)}</td>
        <td>${res.national_permit_issued_by || ''}</td>
        <td>${res.non_use_status || ''}</td>
        <td>${formatDate(res.non_use_from)}</td>
        <td>${formatDate(res.non_use_to)}</td>
        <td>${res.blacklist_status || ''}</td>
        <td>${res.noc_details || ''}</td>
        <td>${res.owner_number || ''}</td>
        <td>${res.rc_status || ''}</td>
        <td>${res.masked_name ? 'Yes' : 'No'}</td>
        <td>${res.challan_details || ''}</td>
    </tr>`;

        $('#tblRCDetaillist tbody').append(row);

    } catch (err) {
        showToast("Error fetching vehicle details.", { type: "error" });

    }
}
function loadFastagDetails() {
    $.ajax({
        url: '/InwardEntry/GetFasttagdetail',
        type: 'GET',
        data: { v_no: $('#TxtDocNo').val(), v_type: $('#ddlDocType').val() },
        success: function (response) {

            let tbody = $('#tblFastagDetaillist tbody');
            tbody.empty();

            if (response.message) {
                tbody.append(`<tr><td colspan="11">${response.message}</td></tr>`);
                return;
            }
            $('#FastDetailLabel').text($('#TxtVehicleNo').text());

            $.each(response, function (i, item) {

                let row = `<tr>
                                <td>${item.client_id}</td>
                                <td>${item.rc_number}</td>
                                <td>${item.bankName}</td>
                                <td>${item.tagId}</td>
                                <td>${item.status}</td>
                                <td>${item.laneDirection}</td>
                                <td>${formatDate(item.transactionDateTime)}</td>
                                <td>${item.seqNo}</td>
                                <td>${item.tollPlazaGeoCode}</td>
                                <td>${item.tollPlazaName}</td>
                                <td>${item.vehicleType}</td>
                            </tr>`;

                tbody.append(row);
            });
            $('#FastDetail').modal('show');
        },
        error: function (err) {

        }
    });
}

async function LoadItemMaster() {
    try {
        const res = await fetch('/InwardEntry/DDlItemMast');
        if (!res.ok) throw new Error("Item load failed");

        itemList = await res.json();
    } catch (err) {


        showToast(err, { type: "warning" });
    }
}

async function LoadUnitMaster() {
    try {
        const res = await fetch('/InwardEntry/DDlUnitMast');
        if (!res.ok) throw new Error("Unit load failed");
        unitList = await res.json();
    } catch (err) {

        showToast("Error loading UNIT master", { type: "warning" });
    }
}

async function LoadDeptMaster() {
    try {
        const res = await fetch('/InwardEntry/DDlDeptMast');
        if (!res.ok) throw new Error("Department load failed");
        deptList = await res.json();
    } catch (err) {
        showToast(err, { type: "warning" });
    }
}

async function GetEwaybillno() {
    try {

        const res = await $.ajax({
            url: '/InwardEntry/GetEWayBillDatacall',
            type: 'GET',
            data: {
                edate: $('#InDate').val(),
                inoutdata: "OUT"
            }        
        });

        console.log("Response:", res);

        if (res.success) {
            showToast("Successfully", { type: "success" });
        } else {
            showToast(res.message || "Failed", { type: "info" });
        }

    } catch (error) {

        console.log("Full Error:", error);

        showToast(error.responseText || error.statusText || "Parser Error", {
            type: "error"
        });
    }
}

async function openApprovalModal() {
    const v_type = $('#ddlDocType').val();
    const v_no = $('#TxtDocNo').val();

    await Promise.all([
        DDlForwordTo(),
        DDlAPPStatus(),
        DDlAPPRemark()
    ]);
    try {
        const res = await $.ajax({
            url: '/InwardEntry/Approval',
            data: {
                v_type: v_type,
                v_no: v_no
            },
            type: 'GET',
            dataType: 'json'
        });
        console.log("res", res);
        if (res.success == false) {
            showToast(res.message, { type: "info" });
            return;
        }
        else
        {         
            $("#approvedModal").modal('show');
        }   
    } catch (error) {
        console.error("Approval request failed:", error);
    }
}

async function sendopenApprovalModal() {
    const v_type = $('#ddlDocType').val();
    const v_no = $('#TxtDocNo').val();
    await Promise.all([
        DDlSendTo(),
        DDlApprovalRemark()
    ]);
    try {
        const res = await $.ajax({
            url: '/InwardEntry/Approval',
            data: {
                v_type: v_type,
                v_no: v_no
            },
            type: 'GET',
            dataType: 'json'
        });

        if (res.success == false) {
            showToast(res.message, { type: "info" });           
            console.log("res", res);
            return;
        }
        else {
            $("#SendForapprovedModal").modal('show');
        }

    } catch (error) {
        console.error("Approval request failed:", error);
    }
}

async function Approvalbtn() {

    const v_type = $('#ddlDocType').val();
    const v_no = $('#TxtDocNo').val();

    try {

        const res = await $.ajax({
            url: '/InwardEntry/Approvalbtn',
            type: 'GET',
            dataType: 'json',
            data: {
                v_type: v_type,
                v_no: v_no
            }
        });

        // Hide everything first
        $('#btn_approval').hide().prop('disabled', true);
        $('#btn_Sendapproval').hide().prop('disabled', true);
        $('#span_approved').hide();

        switch (res.message) {

            case "ApprovalWindow":
                $('#btn_approval') .show() .prop('disabled', false);
                break;

            case "SendForApproval":
                $('#btn_Sendapproval') .show()  .prop('disabled', false);
                break;

            case "DocumentApproved":
                $('#span_approved') .show();
                break;

            default:
                console.log("No approval action available.");
                break;
        }

    } catch (err) {

        console.error("Approval button error:", err);

        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Unable to check approval status.'
        });
    }
}

async function DDlApprovalRemark() {
    try {
        const res = await fetch('/InwardEntry/DDlApprovalRemark');
        const data = await res.json();

        const list = $('#remarksList');
        list.empty();

        data.forEach(item => {
            list.append(`<option value="${item.text}"></option>`);
        });
    }
    catch (error) {
        showToast("Error loading Remark:", { type: "error" });
    }
}

async function DDlSendTo() {
    try {     
        const v_type = $('#ddlDocType').val();
        const res = await fetch(`/InwardEntry/DDlSendTo?v_type=${encodeURIComponent(v_type)}`);

        const data = await res.json();

        const ddl = $('#ddlsendto');

        ddl.empty().append('<option value="">-- Select Send To --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    } catch (error) {
        showToast("Error loading SendTo:", { type: "error" });
        console.error(error);
    }
}

async function SendApproval() {

    try {

        const res = await $.ajax({
            url: '/InwardEntry/SendApproval',
            type: 'POST',
            dataType: 'json',
            data: {
                vtype: $('#ddlDocType').val(),
                vno: $('#TxtDocNo').val(),
                vDate: $('#InDate').val(),
                appStatus: "OPEN",
                appRemark: $('#ddlsendRemarks').val(),
                SendTo: $('#ddlsendto').val(),
                menuCode: "112",
                formName: "frmInwardEntry",
                deptName: "",
                STATUS: "OPEN",
                sendName: $('#ddlsendto option:selected').text(),
                TableName : "GATE1"
            }
        });

        console.log(res);
        if (res.status === "Success") {
            $("#SendForapprovedModal").modal('hide');
            setTimeout(function () { window.location.href = '/InwardEntry/Index?id=' + rowId + '&vtype=' + encodeURIComponent(vtype) + '&mode=view' ; }, 100);                                
            showToast(res.message, { type: "success" });        
            return;
        }

        else if (res.message == "Document Processed.")
        {
            showToast(res.message, { type: "info" });
        }

        else {
            showToast(res.message, { type: "error" });
        }    

    }
    catch (error) {

        console.log(error);

        showToast("Server Error", { type: "error" });
    }
}

async function DDlForwordTo() {
    try {
        const v_type = $('#ddlDocType').val();
        const v_no = $('#TxtDocNo').val();

        const res = await fetch(
            `/InwardEntry/DDlForwordTo?v_type=${encodeURIComponent(v_type)}&v_no=${encodeURIComponent(v_no)}`
        );

        const data = await res.json();
        console.log('data', data);
        const ddl = $('#ddlForwardTo');

        ddl.empty().append('<option value="">-- Select Send To --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

    } catch (error) {
        showToast("Error loading SendTo:", { type: "error" });
        console.error(error);
    }
}

async function DDlAPPStatus() {
    try {
        const res = await fetch('/InwardEntry/DDlAPPStatus');
        const data = await res.json();
        const ddl = $('#ddlApprovalStatus');
        ddl.empty().append('<option value="">-- Select Approval Status --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        showToast("Error loading Approval Status:", { type: "error" });

    }
}

async function DDlAPPRemark() {
    try {
        const res = await fetch('/InwardEntry/DDlAPPRemark');
        const data = await res.json();

        const datalist = $('#appremarksList');
        datalist.empty();

        data.forEach(item => {
            datalist.append(`<option value="${item.text}"></option>`);
        });

    } catch (error) {
        showToast("Error loading Remarks", { type: "error" });
        console.error(error);
    }
}

async function SendWindowApproval() {
    try {

        const AppStatus = parseInt($('#ddlApprovalStatus').val());
        const ForwardTo = parseInt($('#ddlForwardTo').val() || 0);
        const AppRemark = $('#ddlRemarks').val().trim();
        const AppStatusText = $('#ddlApprovalStatus option:selected').text();

        if (!validateRequiredField('#ddlApprovalStatus', 'Please select Approval Status'))
            return;

        // Forward user required for status 7 or 8
        if ((AppStatus === 7 || AppStatus === 8) && ForwardTo === 0) {
            showToast("Please select Forward User.", { type: "warning" });
            return;
        }

        // Remarks required
        if (!AppRemark && (AppStatus === 4 || AppStatus === 5 || AppStatus === 7)) {
            showToast(`Remarks required for ${AppStatusText}`, { type: "warning" });
            return;
        }

        let Status = (AppStatus === 5 || AppStatus === 8)
            ? "CLOSE"
            : "OPEN";

        const res = await $.ajax({
            url: '/InwardEntry/SendApproval',
            type: 'POST',
            dataType: 'json',
            data: {
                vtype: $('#ddlDocType').val(),
                vno: $('#TxtDocNo').val(),
                vDate: $('#InDate').val(),
                appStatus: AppStatusText,
                appRemark: AppRemark,
                SendTo: ForwardTo,
                menuCode: "112",
                formName: "frmInwardEntry",
                deptName: "",
                STATUS: Status,
                sendName: $('#ddlForwardTo option:selected').text(),
                TableName: "GATE1"
            }
        });
        console.log(res);

        if (res.status === "Success") {
            $("#approvedModal").modal('hide');
            showToast(res.message, { type: "success" });
            setTimeout(function () { window.location.href = '/InwardEntry/Index?id=' + rowId + '&vtype=' + encodeURIComponent(vtype) + '&mode=view' ; }, 100);                                
       
           
            return;
        } else {
            showToast(res.message, { type: "error" });
        }

    } catch (error) {
        console.error(error);
        showToast("Server Error", { type: "error" });
    }
}

async function GetTransitnodata(TransitNo) {
    try {
        const url = `/InwardEntry/GetTransitData?VoucherNo=${encodeURIComponent(TransitNo)}`;
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`HTTP Error: ${response.status}`);
        }

        const data = await response.json();

        const d = (Array.isArray(data) && data.length > 0) ? data[0] : null;

        if (d) {
            console.log("Transit Data:", d);

            $('#TxtEWayNo').val(d.forM_NO || '');
            $('#TxtBillAmt').val(d.totaL_AMT || '');
            $('#TxtEWBInvAmt').val(d.totaL_AMT || '');
            $('#TxtEWBInvNo').val(d.bilL_NO || '');
            $('#DtEWayDate').val(
                d.forM_DATE
                    ? d.forM_DATE.split(' ')[0].split('-').reverse().join('-')
                    : ''
            );

            $('#TxtEWayDate').val(
                d.expirY_DATE
                    ? d.expirY_DATE.split(' ')[0].split('-').reverse().join('-')
                    : ''
            );
        } else {
            console.log("No Transit Data found");
            $('#TxtEWayNo').val('');
            $('#TxtBillAmt').val('');
            $('#TxtEWBInvAmt').val('');
            $('#TxtEWBInvNo').val('');
            $('#DtEWayDate').val('');
            $('#TxtEWayDate').val('');
        }

    } catch (error) {
        console.error("Error fetching Transit Data:", error);
        showToast("Error fetching Transit Data", { type: "error" });
    }
}
