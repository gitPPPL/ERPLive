var allFieldIds = [
    "ddlDocType",
    "ddlPartyName",
    "TxtPartyGSTIN",
    "TxtOtherPartyGST",
    "ddlStatus",
    "NumWaybillNo",
    "DtFormDate",
    "DtExpiryDate",
    "NumOrderNo",
    "TxtBillNo",
    "DtBillDate",
    "NumSGSTAmt",
    "NumCGSTAmt",
    "NumIGSTAmount",
    "NumCessAmt",
    "NumOtherAmount",
    "NumTotalAmount",
    "NumBillAmount",
    "TxtTruckNo",
    "TxtTransport",
    "TxtGRNo",
    "chkGRDate",
    "DtGRDate",
    "TxtItemDesc",
    "NumHSNCode",
    "TxtNoItem"
];
let readOnly = false;
$(document).ready(async function () {
    const urlParams = new URLSearchParams(window.location.search);
    const rowId = urlParams.get('id');
    const mode = urlParams.get('mode');
    const vtype = urlParams.get('vtype');
    readOnly = (mode === 'view');
    //=======Set focus========
    $('#ddlDocType').focus();
    if (mode !== "view" && rowId !== null) {
        $('#DtFormDate').focus().addClass('erppage-input');
    }
    setEnterKeyFocus(allFieldIds);
    //=====Set Current Date==========
    const currentDate = getCurrentDateYMD();
    $('#DtBillDate, #DtGRDate, #DtFormDate, #DtExpiryDate, #DtFromDate, #DtToDate').val(currentDate);
    //==Gr Date Toggle====
    Dtdisable();
    toggleDate('#chkGRDate', '#DtGRDate');
    // Load dropdowns and form data if editing
    LoadDropDown().then(() => {
        if (rowId) {
            return LoadFormByID(rowId, vtype);
        }
    }).catch(error => {
        console.error("An error occurred:", error);
    });
    //======View Mode====
    if (mode === "view") {
        setFormReadOnly();

        $('#TransitEntryForm').after(
            '<span class="badge bg-secondary ms-2">Read‑Only Mode</span>'
        );
    }
    // Event handlers
    $("#ddlDocType").on('change', function () {
        if (rowId == null) {
            GetVNo(this.value);
            document.getElementById("ddlDocType").disabled = true;
        }
        bindDropdown('TransitEntry', 'PartyName', '#ddlPartyName', '-- Select Party Name --', null, null, false, this.value, true);
    });
    $("#ddlPartyName").on('change', function () {
        fetchPartyGstin(this.value);
    });
    $('#btn-save').on('click', function (e) {
        e.preventDefault();

        // Validations
        if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;

        if (!validateRequiredField('#NumDocNo', 'Doc No')) return;

        if (!validateRequiredField('#ddlPartyName', 'Party Name')) return;
        if (!validateRequiredField('#NumWaybillNo', 'WayBill No')) return;
        if (!validateRequiredField('#TxtBillNo', 'Bill No')) return;
        if (!validateRequiredField('#DtBillDate', 'Bill Date')) return;
        if (!validateRequiredField('#NumBillAmount', 'Bill Amount')) return;

        if (!validateRequiredField('#TxtPartyGSTIN', 'Party Gstin')) return;
        if (!validateRequiredField('#DtFormDate', 'Create Date')) return;
        if (!validateRequiredField('#DtExpiryDate', 'Expiry Date')) return;

        if (!validateExpiryDate()) return;

        const payload = CollectFormData();

        let vNo = $('#NumDocNo').val();
        let formNo = $('#NumWaybillNo').val();
        let docId = $.trim($('#TxtCode').val());
        checkExistOrNot(vNo, formNo).done(function (res) {
            console.log(res);
            if (res.status === true && res.data === true && docId === '') {
                showToast(`Waybill No ${formNo} already exists!`, { type: "warning" });
                return;
            }
            // Disable button to prevent multiple submissions
            $("#btn-save").prop("disabled", true);

            // AJAX Save
            $.ajax({
                url: '/TransitEntry/Savedata',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                success: function (response) {
                    if (response.success) {
                        showToast(response.message, { type: "success" });
                        setTimeout(() => window.location.href = '/TransitEntryList/Index', 1000);
                    } else {
                        showToast(response.message || "Save failed.", { type: "error" });
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
                    console.error("Error: ", errorMessage);
                    showToast(errorMessage, { type: "error" });
                },
                complete: function () {
                    $("#btn-save").prop("disabled", false);
                }
            });
        });

    });
    $(document).on('input', '#NumBillAmount, #NumSGSTAmt, #NumCGSTAmt, #NumIGSTAmount, #NumCessAmt, #NumOtherAmount', function () {
        let totalAmt = 0;
        let basicAmt = parseIntSafe($('#NumBillAmount').val()) || 0;
        let SGSTAmt = parseIntSafe($('#NumSGSTAmt').val()) || 0;
        let CGSTAmt = parseIntSafe($('#NumCGSTAmt').val()) || 0;
        let IGSTAmt = parseIntSafe($('#NumIGSTAmount').val()) || 0;
        let cessAmt = parseIntSafe($('#NumCessAmt').val()) || 0;
        let otherAmt = parseIntSafe($('#NumOtherAmount').val()) || 0;
        totalAmt = basicAmt + SGSTAmt + CGSTAmt + IGSTAmt + cessAmt + otherAmt;
        $('#NumTotalAmount').val(totalAmt || 0);
    })
});

//===Collect Data to save and update
function CollectFormData() {
    let GR_DATE = null;
    if ($('#chkGRDate').is(':checked')) {
        const dateVal = formatDate($('#DtGRDate').val());
        GR_DATE = dateVal ? dateVal : null;
    }
    else {
        GR_DATE = null;
    }

    const Header = {
        V_TYPE: $('#ddlDocType').val() || null,
        V_NO: parseInt($('#NumDocNo').val()) || null,
        DOC_ID: $.trim($('#TxtCode').val()) || null,
        FORM_NO: $.trim($('#NumWaybillNo').val()) || null,
        FORM_DATE: formatDate($("#DtFormDate").val()) || null,
        EXPIRY_DATE: formatDate($("#DtExpiryDate").val()) || null,
        PARTY_CODE: parseInt($('#ddlPartyName').val()) || null,
        PARTY_GSTIN: $.trim($('#TxtPartyGSTIN').val()) || null,
        OTHER_GSTIN: $.trim($('#TxtOtherPartyGST').val()) || null,
        NOS: parseFloat($('#TxtNoItem').val()) || null,
        BILL_NO: $.trim($('#TxtBillNo').val()) || null,
        BILL_DATE: formatDate($("#DtBillDate").val()) || null,
        GR_NO: $.trim($('#TxtGRNo').val()) || null,
        GR_DATE: GR_DATE,
        TRUCK_NO: $.trim($('#TxtTruckNo').val()) || null,
        TRANSPORT: $.trim($('#TxtTransport').val()) || null,
        ORD_NO: parseInt($('#NumOrderNo').val()) || null,
        ORD_TYPE: $.trim($('#TxtOrderType').val()) || null,
        HSN_CODE: parseInt($('#NumHSNCode').val()) || null,
        ITEM_DESC: $.trim($('#TxtItemDesc').val()) || null,
        BILL_AMT: parseFloat($('#NumBillAmount').val()) || null,
        SGST_AMT: parseFloat($('#NumSGSTAmt').val()) || null,
        CGST_AMT: parseFloat($('#NumCGSTAmt').val()) || null,
        IGST_AMT: parseFloat($('#NumIGSTAmount').val()) || null,
        CESS_AMT: parseFloat($('#NumCessAmt').val()) || null,
        //CESS_NONADVOLAMT: parseFloat($('#NumCessNAmount').val()) || null,
        OTHER_AMT: parseFloat($('#NumOtherAmount').val()) || null,
        TOTAL_AMT: parseFloat($('#NumTotalAmount').val()) || null,
        STATUS: parseInt($('#ddlStatus').val()) || null,
        GATE_NO:          /* parseInt($('#TxtGRNo').val()) || */ null,
        GATE_DATE:        /* formatDate($("#chkGRDate").val()) || */ null,
        ARRIVAL_DATE:     /* formatDate($("#DtArrivalDate").val()) || */ null,
        action: $.trim($('#TxtCode').val()) ? 'UPDATE' : 'INSERT'
    };
    return Header;
}

//=== Helper Funtions
async function isExist() {
    const exist = await checkExist();
    if (exist === false) {
        return true;
    }
    return false;
}
function Dtdisable() {
    const currentDate = getCurrentDateYMD();
    $('#chkGRDate').prop('checked', false);
    $('#DtGRDate').prop('disabled', true);
    $('#DtGRDate').val(currentDate);
}
function toggleDate(chk, input) {
    $(chk).on('change', function () {
        $(input).prop('disabled', !$(this).is(':checked'));
    });
}
function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    if (isNaN(d)) return '';
    return d.toISOString().slice(0, 10);
}
//==Vno
async function GetVNo(Vtype) {
    try {
        const res = await fetch(`/TransitEntry/GetVNo?Vtype=${encodeURIComponent(Vtype)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const data = await res.json();

        if (data.error) throw new Error(data.error);
        if (!data.v_NO) throw new Error('Response missing V_NO');

        $('#NumDocNo').val(data.v_NO);
    } catch (e) {
        console.error('GetVNo failed:', e);
        alert('Error loading Document Number: ' + e.message);
    }
}
//==Dropdowns
async function LoadDropDown(VTypeId) {
    try {
        await bindDropdown('TransitEntry', 'DocType', '#ddlDocType', '-- Select VType --', null, null, false, null);
        await bindDropdown('TransitEntry', 'DocStatus', '#ddlStatus', '-- Select Status --', null, null, true, null);
        await bindDropdown('TransitEntry', 'PartyName', '#ddlPartyName', '-- Select Party Name --', null, null, false, "", true);
    } catch (error) {
        console.error("Error loading dropdowns:", error);
    }
}
//==GST Fetch
async function fetchPartyGstin(Partycode) {
    try {
        const response = await fetch(`/TransitEntry/fetchPartyGstinNo?Partycode=${Partycode}`);
        const data = await response.json();
        if (data.length > 0 && data[0].gstin) {
            $('#TxtPartyGSTIN').val(data[0].gstin);
        } else {
            $('#TxtPartyGSTIN').val('');
        }
    } catch (error) {
        console.error('Failed to fetch ship from address:', error);
    }
}
//==Get by Id
function LoadFormByID(code, vtype) {
    $.ajax({
        url: '/TransitEntryList/GetDataByID',
        type: 'GET',
        data: { code, vtype },
        success: function (res) {
            console.log('data', res);

            if (!res.success || !res.data) {
                showToast("No Data found for the given ID.", { type: "warning" });
                return;
            }

            populateForm(res.data);
        },
        error: function (xhr) {
            showToast("Error loading data: ", { type: "error" });
        }
    });
}
//==Fill Form
function populateForm(data) {
    $("#TxtCode").val($.trim(data.v_NO));
    $("#ddlDocType").val($.trim(data.v_TYPE)).prop('disabled', true);
    $("#NumDocNo").val($.trim(data.v_NO));
    bindDropdown('TransitEntry', 'PartyName', '#ddlPartyName', '-- Select Party Name --', data.partY_CODE, null, false, null, true);
    $("#TxtPartyGSTIN").val($.trim(data.partY_GSTIN));
    $("#TxtBillNo").val($.trim(data.bilL_NO));
    $("#DtBillDate").val(formatDateYMD(data.bilL_DATE));
    $("#NumBillAmount").val($.trim(data.bilL_AMT));
    $("#NumHSNCode").val(data.hsN_CODE);
    $("#TxtOtherPartyGST").val($.trim(data.otheR_GSTIN));
    $("#TxtNoItem").val($.trim(data.nos));
    $("#TxtItemDesc").val($.trim(data.iteM_DESC));
    $("#TxtGRNo").val($.trim(data.gR_NO));

    if (data.gR_DATE !== null && data.gR_DATE !== '') {
        $('#DtGRDate').val(formatDateYMD(data.gR_DATE));
        $('#chkGRDate').prop('checked', true);
        $('#DtGRDate').prop('disabled', false);
    }
    else {
        const currentDate = getCurrentDateYMD();
        $('#chkGRDate').prop('checked', false);
        $('#DtGRDate').prop('disabled', true);
        $('#DtGRDate').val(currentDate);
    }

    
    $("#TxtTruckNo").val($.trim(data.trucK_NO));
    $("#NumSGSTAmt").val($.trim(data.sgsT_AMT));
    $("#NumCGSTAmt").val($.trim(data.cgsT_AMT));
    $("#NumCessAmt").val($.trim(data.cesS_AMT));
    //$("#NumCessNAmount").val($.trim(data.cesS_NONADVOLAMT));
    $("#NumWaybillNo").val($.trim(data.forM_NO));
    $("#DtFormDate").val(formatDateYMD(data.forM_DATE));
    $("#DtExpiryDate").val(formatDateYMD(data.expirY_DATE));
    $("#NumOrderNo").val($.trim(data.orD_NO));
    $("#NumIGSTAmount").val($.trim(data.igsT_AMT));
    $("#NumOtherAmount").val($.trim(data.otheR_AMT));
    $("#NumTotalAmount").val($.trim(data.totaL_AMT));
    $("#ddlStatus").val($.trim(data.status));
    $("#TxtTransport").val($.trim(data.transport));
    
}
//==Readonly
function setFormReadOnly() {
    const form = $('#TransitEntryForm');
    form.find('input').prop('disabled', true);
    form.find('textarea').css('background-color', '#f0f0f0');
    const ddlParty = $('#ddlPartyName');
    ddlParty.prop('disabled', false);
    ddlParty.on('mousedown', function (e) {
        e.preventDefault();
        this.blur();
    });
    ddlParty.css({
        'pointer-events': 'none',
        'background-color': '#e9ecef'
    });
    $('#btn-save').hide();
    form.addClass('erppage-readonly');
}
//==CheckExist
function checkExistOrNot(vNo, formNo) {
    return $.ajax({
        url: '/TransitEntry/GetExist',
        type: 'GET',
        dataType: 'json',
        data: { vNo: vNo, form_No: formNo }
    });
}
//==Validate Expiry Date
function validateExpiryDate() {
    const formDate = $('#DtFormDate').val();
    const expiryDate = $('#DtExpiryDate').val();
    const currentDate = getCurrentDateYMD();

    if (formDate && expiryDate) {
        if (new Date(expiryDate) < new Date(formDate)) {
            showToast('Expiry Date must be greater than From Date.', { type: "warning" });
            $('#DtExpiryDate').val(currentDate); // clear invalid expiry date
            return false;
        }
    }
    return true;
}
function setEnterKeyFocus(sequence) {
    sequence.forEach((id, index) => {
        $(`#${id}`).on('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                // ===== Checkbox Handling =====
                if ($(this).is(':checkbox')) {

                    $(this).prop('checked', !$(this).prop('checked'));

                    // Trigger change event if needed
                    $(this).trigger('change');
                }
                if (index + 1 < sequence.length) {
                    $(`#${sequence[index + 1]}`).focus();
                }
            }
        });
    });
}