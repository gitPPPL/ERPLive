    const urlParams = new URLSearchParams(window.location.search);
    const rowId = urlParams.get('id');
    const mode = urlParams.get('mode');
    const vtype = urlParams.get('vtype');
    const PubDefEWaybillAmt = 50000;
    var   PubUserLevel='@PubUserLevel';
    var   LoginDate = '@logindate';
    var   itemList = [];
    var   deptList = [];
    var   unitList = [];

    $(document).ready(async function () {
        await LoadDropDown();
    if (rowId)
    {
        await LoadFormByID(rowId, vtype);
        $('#ddlDocType').prop('disabled', true);
        $('#InDate').prop('disabled', true);
        $('#InTime').prop('disabled', true);
        $('.erppagelist-toolbar-end').show();
        $('#btn_approval').show();
        if (mode === "view")
        {
         setFormReadOnly();
        }
    }
    else
    {
        if(PubUserLevel == 1)
        {
            $('#InDate').prop('disabled', false);
            $('#InTime').prop('disabled', false);
        }
        else
        {
            $('#InDate').prop('disabled', true);
            $('#InTime').prop('disabled', true);
        }

            $('#ddlDocStatus').prop('disabled', true);
            let today = new Date().toISOString().split('T')[0];
            $('#InDate').attr('min', LoginDate);
            $('#TxtRptDate').val(today);
            let now = new Date();
            $('#InTime').val(now.toTimeString().slice(0,8));
            $('#TiRptDate').val(now.toTimeString().slice(0,8));
            GetVNo($('#ddlDocType').val());
        }

        $('#ddladdressline1').on('change' , function() {
            let selectedPartyValue = $('#ddlPartyName').val();
            let selectedAddressValue = $('#ddladdressline1').val();
            fetchDDlParty(selectedPartyValue, selectedAddressValue);
        });

        $(document).on('input', '.numeric-only', function () {
            this.value = this.value.replace(/[^0-9.]/g, '');
        });

        $('#TxtBillNo').on('change', function() {
            if ($(this).val()) {
             $('#span_partybilldate').show();
            } else {
             $('#span_partybilldate').hide();
            }
        });

        $('#TxtChallanNo').on('change', function() {
            if ($(this).val()) {
                $('#span_ChallanDatedate').show();
            } else {
                $('#span_ChallanDatedate').hide();
            }
        });

    $('#TxtVehicleNo').on('change', function() {
        if ($(this).val())
        {
            $('#span_drivername').show();
            $('#span_driverMobileNo').show();
            $('#span_tranportarname').show();
        }
    else
        {
            $('#span_drivername').hide();
            $('#span_driverMobileNo').hide();
            $('#span_tranportarname').hide();
        }
    });

    $('#TxtEWayNo').on('change', function() {
        if ($(this).val())
        {
            $('#span_EWayBillDate').show();
            $('#span_EWayBillExpiryDate').show();
            $('#span_EWBPartyInvNo').show();
            $('#span_EWBPartyInvAm').show();
        }
        else
        {
            $('#span_EWayBillDate').hide();
            $('#span_EWayBillExpiryDate').hide();
            $('#span_EWBPartyInvNo').hide();
            $('#span_EWBPartyInvAm').hide();
        }
    });

    $('#TxtWbSlipNo').on('change', function() {
    if ($(this).val())
        {
            $('#span_PartyWBGrWt').show();
            $('#span_PartyWBTrWt').show();
            $('#span_PartyWBTime').show();
        }
        else
        {
            $('#span_PartyWBGrWt').hide();
            $('#span_PartyWBTrWt').hide();
            $('#span_PartyWBTime').hide();
         }
    });

    $("#btn-save").click(async function (e) {
     e.preventDefault();
    const PARTY_CODE = parseInt($('#ddlPartyName').val()) || null;
    const V_NO = parseInt($('#TxtDocNo').val()) || null;
    const BILL_NO = $('#TxtBillNo').val().trim();
    const V_TYPE = $('#ddlDocType').val();
        if (BILL_NO) {
            const validation = await BillNoValidation(PARTY_CODE, BILL_NO, V_NO);
            if (!validation.success)
            {
                return;
            }
        }

        const isValid = await checkValidDate();
        if (isValid === false) {
            return;
        }

    const gateValidation = await GatenoValidation(V_TYPE, V_NO);
        if (!gateValidation.success) {
            return;
        }
        saveInwardEntry();
    });

    $('#ddlShipFrom').on('change', function() {
        fetchShipFromAdd(this.value);
                });

    $("#ddlDocType").change(function () {
        $(this).prop('disabled', true);
    const docType = this.value;
    if (rowId == null) {
        GetVNo(docType);
                }

    if (docType === "INMS") {
        $("#tblInwardEntry tbody").empty();
    addRow($("#tblInwardEntry tbody"), { });
                } else {
        $("#tblInwardEntry tbody").empty();
    addRow($("#tblInwardEntry tbody"), { });
                }
            });

    $("#btnpendingorderno").click(function () {
                const selectedValue = $('#ddlPartyName').val();
    const V_TYPE = $('#ddlDocType').val();
    const V_DATE = $('#TxtRptDate').val();

    if (!selectedValue) {
        invalidateField('ddlPartyName', 'Please Select Party Name!', 'info');
    return;
                    }

    if (!V_TYPE) {
        showToast("Please Select Voucher Type!", { type: "info" });
    $('#ddlDocType').addClass('is-invalid').focus();
    return;
                } else {
        $('#ddlDocType').removeClass('is-invalid');
                }

    if (!V_DATE) {
        showToast("Please Select Voucher Date!", { type: "info" });
    $('#TxtRptDate').addClass('is-invalid').focus();
    return;
                } else {
        $('#TxtRptDate').removeClass('is-invalid');
                }

    FetchPendingOrderNo(selectedValue, V_TYPE, V_DATE);
            });

    $(document).on('change', '#selectAllPR', function () {
        $('#tblpendingordermodal tbody .rowCheckbox').prop('checked', $(this).is(':checked'));
                  });

    $(document).on('change', '#tblpendingordermodal tbody .rowCheckbox', function () {
                const $currentRow = $(this).closest('tr');
    const currentItemCode = $currentRow.find('td:eq(1)').text().trim();

    if ($(this).is(':checked')) {
        let isDuplicate = false;

    $('#tblpendingordermodal tbody tr').each(function () {
                        const $row = $(this);
    const itemCode = $row.find('td:eq(1)').text().trim();
    const isChecked = $row.find('.rowCheckbox').is(':checked');

    if ($row[0] !== $currentRow[0] && isChecked && itemCode === currentItemCode) {
        isDuplicate = true;
    return false;
                        }
                    });

    if (isDuplicate || isItemInMainTable(currentItemCode)) {
        showToast("Duplicate Item Code not allowed: " + currentItemCode, { type: "error" });
    $(this).prop('checked', false);
    return;
                }
            }

    const total = $('#tblpendingordermodal tbody .rowCheckbox').length;
    const checked = $('#tblpendingordermodal tbody .rowCheckbox:checked').length;
                $('#selectAllPR').prop('checked', total > 0 && total === checked);
            });

    $('#ddlPartyName').on('change' , function() {
                    const PartyId = this.value;
    const Vno = document.getElementById('TxtDocNo')?.value || '';
    const v_type = document.getElementById('ddlDocType')?.value || '';
    const indate = document.getElementById('InDate')?.value || '';
    $('#ddlDocType').prop('disabled', true);
    fetchTransitno(v_type, Vno, PartyId, indate);
    GetPartyAdress(PartyId);
    DDlPartyAdd(PartyId);
    $('#ddlDocStatus').prop('disabled', true);
                });

    $('#Btn_selectedData').on('click', function () {
            const selectedData = getSelectedPendingOrderRows();
    if (selectedData.length === 0) {
        showToast("Please select at least one row.", { type: "warning" });
    return;
            }

    // Use Bootstrap 5 modal API to hide the modal
    const modalElement = document.getElementById('pendingorders');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.hide();  // Correct way to hide modal in Bootstrap 5

    // Perform other actions after modal is hidden
    populateInwardEntryTable(selectedData);
        });

    $('#btn_setting').on('click', function () {
            GetVehicledetail();
        });

    $('#btn_database').on('click', function () {
     GetFasttagVehicledetail();
    });

    $('#btn_RCDetail').on('click', function () {
     GetVehicledata();
    });

    $('#btn_FastagDetail').on('click', function () {
        loadFastagDetails();
    });

    $(document).on('click', '.delete-btn', function () {
        if (confirm('Are you sure you want to delete this row?')) {
            $(this).closest('tr').remove();
        }
    });

    $(document).on('change', '.ItemName', function () {
        const code = $(this).val();
        $(this).closest('tr').find('.itemCode').val(code);
    });

    $('#SEARCHCONTAINER').on('click' , function() {
     var Container_No  = $('#TxtContainerNo').val();
        if(Container_No)
        {
            $('#TxtContainerNo').removeClass('is-invalid');
            getcontainerdata(Container_No);
        }
    else
        {
            showToast("Please Fill Container_No", { type: "info" });
            $('#TxtContainerNo').addClass('is-invalid').focus();
            return false;
        }

    });

    $('#btn_backtolist').on('click' , function(){
     backToList();
    });

    $('#EwayBillbtn').on('click', function () {
                    var V_date = $('#InDate').val();

    if (V_date) {
        GetEwaybillno(V_date, "IN");
                } else {
        showToast("Date is empty", { type: "warning" });
                }
            });

    $('#BtnPartyBillno').on('click', async function () {
                try {
                    var SUPPLIER = $('#ddlPartyName').val();

    const res = await $.ajax({
        url: '/InwardEntryList/GetDataByPARTTYBILLNO',
    type: 'GET',
    data: {SUPPLIER: SUPPLIER }
                    });

    const data = res.data;
    populateTable(data);

                } catch (error) {
        showToast(error, { type: "error" });
                }
            });

    $('#btn_partybillnoselect').on('click', function () {
            const selectedrows = getSelectedRows();

    if (selectedrows.length === 0) {
        showToast("No rows selected", { type: "warning" });
    return;
            }

    const  row = selectedrows[0];
    $('#TxtBillNo').val(row.supplieR_INVNO);
    $('#DtPartyBillDate').val(row.supplieR_INVDATE);
    $('#TxtBillAmt').val(row.supplieR_INVAMT);
    $('#TxtContainerNo').val(row.containeR_NO);
          });

        });

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
        const PARTY_CODE    = parseInt($('#ddlPartyName').val()) || null;
        const PARTY_NAME    = $('#ddlPartyName option:selected').text();
        const V_TYPE        = $('#ddlDocType').val();
        const STATUS        = parseInt($('#ddlDocStatus').val()) || null;
        const V_NO          = parseInt($('#TxtDocNo').val()) || null;
        const R_DATE        = formatDate($("#TxtRptDate").val()) || null;
        const BILL_NO       = $.trim($('#TxtBillNo').val()) || null;
        const BILL_DATE     = formatDate($("#DtPartyBillDate").val()) || null;
        const CHALL_NO      = $.trim($('#TxtChallanNo').val()) || null;
        const CHALL_DATE    = formatDate($("#TxtChallanDate").val()) || null;
        const BILL_AMT      = parseFloat($('#TxtBillAmt').val()) || 0.0;
        const TRUCK_NO      = $.trim($('#TxtVehicleNo').val()) || null;
        const TRANSPORT_CODE= parseInt($('#TxtTransporter').val()) || null;
        const DRIVER_NAME   = $.trim($('#TxtDriverName').val()) || null;
        const DRIVER_NO     = $.trim($('#TxtDriverMobile').val()) || null;
        const WAYBILL_NO    = $.trim($('#TxtEWayNo').val()) || null;
        const EWB_DATE      = formatDate($("#DtEWayDate").val()) || null;
        const EWB_EXPDATE   = formatDate($("#TxtEWayDate").val()) || null;
        const EWB_INVNO     = $.trim($('#TxtEWBInvNo').val()) || null;
        const EWB_INVAMT    = parseFloat($('#TxtEWBInvAmt').val()) || 0.0;
        const V_DATE        = formatDate($("#InDate").val()) || null;
        const OUT_DATE      = formatDate($("#DtVehicleOutTime").val()) || null;
        const R_TIME        = $.trim($('#TiRptDate').val()) || null;
        const SHIP_BILLDATE = formatDate($("#ShipBillDate").val()) || null;
        const SHIP_PARTY    = parseInt($('#ddlShipFrom').val()) || null;
        const SHIP_BILLNO   = $.trim($('#ShipBillNo').val()) || null;
        const TRANSIT_NO    = parseInt($('#ddlTransit').val()) || null;

        if (!validateRequiredField('#ddlDocType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#TxtDocNo', 'Please select a Voucher No')) return;
        if (!validateRequiredField('#ddlDocStatus', 'Please select a Status.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party.')) return;

        if (!R_DATE && !R_TIME) {
         if (!validateRequiredField('#TxtRptDate', 'Please select Reporting Date and Time.')) return;               
        }

        if (BILL_NO && !BILL_DATE) {         
             if (!validateRequiredField('#DtPartyBillDate', 'Please select Party Bill Date.')) return;
        }

        if (CHALL_NO && !CHALL_DATE) {
            if (!validateRequiredField('#TxtChallanDate', 'Please select Challan Date.')) return;
        }

        if (!validateRequiredField('#TxtBillAmt', 'Please fill Bill Amount.')) return;
        if (!validateRequiredField('#TxtVehicleNo', 'Please fill Vehicle No')) return;



        if (TRUCK_NO) {
            var numericPart = TRUCK_NO.replace(/\D/g, '');
            var lastFour = numericPart.slice(-4);

        if (lastFour) {
        
            if (!validateRequiredField('#TxtDriverName', 'Please enter Driver Name.')) return;
            if (!DRIVER_NO || DRIVER_NO.toString().length !== 10) {

        showToast("Please enter a valid 10-digit mobile number.", { type: "warning" });
        $("#TxtDriverMobile").addClass("is-invalid").focus();
        return;
        } else {
        $("#TxtDriverMobile").removeClass("is-invalid");
        }
        }
        }

    if (WAYBILL_NO) {
                       if (!validateRequiredField('#DtEWayDate', 'Please select EWayBill Date.')) return;
    if (!validateRequiredField('#TxtEWayDate', 'Please select EWayBill Expiry Date.')) return;
    if (!validateRequiredField('#TxtEWBInvNo', 'Please fill EWB Party Inv No.')) return;
    if (!validateRequiredField('#TxtEWBInvAmt', 'Please fill EWB Party Inv Amount.')) return;                                                       

                }

                if (R_DATE > V_DATE) {
                  if (!validateRequiredField('#TxtRptDate', 'Reporting Date cannot be greater than In Date.')) return;                     
                }

                if (BILL_DATE > V_DATE) {
                 if (!validateRequiredField('#DtPartyBillDate', 'Bill Date cannot be greater than In Date.')) return;
                }

    if (SHIP_PARTY && !SHIP_BILLNO) {
                      if (!validateRequiredField('#ddlShipFrom', 'Shipping Bill No. is required.')) return;
                    }

    if (SHIP_BILLNO && !SHIP_PARTY) {
                    if (!validateRequiredField('#ShipBillNo', 'Shipping Party is required.')) return;                  
                    }

    if (["INST", "INFU", "INRM"].includes(V_TYPE)) {
                            if (BILL_AMT == 0 && !TRANSIT_NO && !WAYBILL_NO) {        
                             if (!validateRequiredField('#TxtBillAmt', 'Bill Amount compulsory for')) return;
                      
                            }
                        }

                        if (BILL_AMT > PubDefEWaybillAmt && (!TRANSIT_NO || !WAYBILL_NO)) {

        showToast("Transit No. and E-Way Bill required.", { type: "info" });
    if (!TRANSIT_NO) {
        $("#ddlTransit").addClass("is-invalid").focus();
                        } else {
        $("#TxtEWayNo").addClass("is-invalid").focus();
                        }
    return;
                    }

    if (TRANSIT_NO && EWB_EXPDATE) {
                        const expDate = new Date(EWB_EXPDATE);
    const inDate = new Date(V_DATE);

    if (expDate < inDate) {
        showToast("Waybill expired on " + EWB_EXPDATE, { type: "info" });
    return;
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
    TRANSPORT_CODE: $.trim($('#TxtTransporter').val()) || null,
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

    if (!Deatils || Deatils.length === 0) {
        showToast("Please fill at least one row in Detail", { type: "info" });
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
                    if (!row.REF_TYPE) {
        showToast(`Reference Type required (Row ${i + 1})`, { type: "warning" });
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

    $("#btn-save").prop("disabled", true);

    $.ajax({
        url: '/InwardEntry/SavedData',
    type: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(payload),

    success: function (response) {
                    if (response.status === "Success") {
                        if (response.message) {
        showToast("Saved successfully!", { type: "success" });
                        }
    setTimeout(function () {window.location.href = '/InwardEntry/Index?id=' + V_NO + '&vtype=' + encodeURIComponent(V_TYPE) + '&mode=view'; }, 3000);
                    }
    else {
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
    return {success: false };
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
    return {success: false };
            }
        }

    function setFormReadOnly() {
                    const form = document.getElementById("InwardEntryForm");
    if (!form) return;
    // add readonly class
    form.classList.add('erppage-readonly');
    // Hide approval button
    $('.erppagelist-toolbar-end').hide();
    $('#btn_approval').hide();

    // Disable all inputs except hidden
    const inputs = form.querySelectorAll("input");
                    inputs.forEach(el => {
                        if (el.type !== "hidden") {
                            if (
    el.type === "text" ||
    el.type === "date" ||
    el.type === "time" ||
    el.type === "number"
    ) {
        el.setAttribute("readonly", true);
                            } else {
        el.setAttribute("disabled", true);
                            }
                        }
                    });

    // Disable all textareas
    const textareas = form.querySelectorAll("textarea");
                    textareas.forEach(el => {
        el.setAttribute("readonly", true);
                    });

    // Disable all selects
    const selects = form.querySelectorAll("select");
                    selects.forEach(el => {
        el.setAttribute("disabled", true);
                    });

    // Disable all buttons except Back/List buttons if needed
    const buttons = form.querySelectorAll("button");
                    buttons.forEach(btn => {
                        const txt = btn.innerText.trim().toLowerCase();

    if (
    !txt.includes("back") &&
    !txt.includes("close")
    ) {
        btn.setAttribute("disabled", true);
                        }
                    });

    // Disable clickable icons/spans
    const clickableIcons = form.querySelectorAll(`
    .input-icon,
    .fa-search,
    .fa-cog,
    .fa-database,
    .fa-ellipsis-h
    `);

                    clickableIcons.forEach(icon => {
        icon.style.pointerEvents = "none";
    icon.style.opacity = "0.5";
    icon.style.cursor = "not-allowed";
                    });

    // Disable modal triggers
    const modalTriggers = form.querySelectorAll("[data-bs-toggle='modal']");
                    modalTriggers.forEach(el => {
        el.removeAttribute("data-bs-toggle");
    el.removeAttribute("data-bs-target");
    el.style.pointerEvents = "none";
    el.style.opacity = "0.5";
    el.style.cursor = "not-allowed";
                    });

    // Disable table controls
    const tableControls = form.querySelectorAll(`
    table input,
    table select,
    table textarea,
    table button,
    table .fa,
    table span
    `);

                    tableControls.forEach(el => {
                        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
        el.setAttribute("readonly", true);
                        } else if (el.tagName === "SELECT" || el.tagName === "BUTTON") {
        el.setAttribute("disabled", true);
                        }

    el.style.pointerEvents = "none";
    el.style.opacity = "0.5";
                    });
    $('.erppage-tab[data-tab="partydetails"]').prop('disabled', false);
    $('.erppage-tab[data-tab="shippinginfo"]').prop('disabled', false);
    $('.erppage-tab[data-tab="billchallan"]').prop('disabled', false);
    // Add readonly class for CSS styling
    form.classList.add("readonly-mode");
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
            await DDLVtype(),
            await DDLParty(),
            await DDLShipFrom(),
            await DDDocStatus(),
            await DDlPartyCity(),
            await LoadItemMaster(),
            await LoadUnitMaster(),
            await LoadDeptMaster(),
            await DDlTransportname(),
            await DDlCity(),
            await DDlState(),
        ]);
        } catch (error) {
         showToast("Error loading dropdowns", { type: "error" });

        }
    }

    function populateTable(data) {
                  const tbody = $("#tblellipsisIconmodal tbody");
    tbody.empty();

    data.forEach(function (row) {
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

    function DDlPartyAdd(PartyId) {
        return $.ajax({
        url: '/InwardEntry/fetchSelectedAddress',
        type: 'POST',
        data: {PartyId: PartyId },
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

    async function GetPartyAdress(PartyId) {
        try {
        const url = `/InwardEntry/GetPartyAddressbyCode?PartyId=${encodeURIComponent(PartyId)}`;
        const response = await fetch(url);
        const data = await response.json();

        const d = (data && data.length > 0) ? data[0] : { };
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
            const d = (data && data.length > 0) ? data[0] : { };
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

    async function fetchTransitno(v_type, v_no, partycode, ExpiryDate) {
          try {
            const queryParams = new URLSearchParams({v_type, v_no, partycode, ExpiryDate});
    const response = await fetch(`/InwardEntry/DDlTransitNo?${queryParams.toString()}`);
    if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
    const result = await response.json();
    const ddl = $('#ddlTransit');
    ddl.empty().append('<option value="">-- Select Transit No --</option>');

    if (result.status && Array.isArray(result.data)) {
        result.data.forEach(item => {
            ddl.append(`<option value="${item}">${item}</option>`);
        });
            }

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
                if (!PartyCode || !V_TYPE || !V_DATE) {
        showToast("Invalid parameters passed", { type: "error" });
    return;
                }

    const result = await $.ajax({
        url: '/InwardEntryList/GetDataByPendingorder',
    type: 'GET',
    data: {PartyCode, V_TYPE, V_DATE}
                });

    if (!result || !result.success) {
        showToast("Failed to fetch data", { type: "error" });
    return;
                }

    if (!result.data || result.data.length === 0) {
                  const PartyName = $('#ddlPartyName option:selected').text();
    showToast(`Data Not Found For this party: ${PartyName}`, {type: "error" });
    return;
                }

    // Show modal
    const modalElement = document.getElementById('pendingorders');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();

    // Table rendering
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

              } catch (error) {
        showToast("Failed to load pending orders", { type: "error" });
              }
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
    $tbody.empty();

     $.each(selectedData, function (idx, item) {
            addRow($tbody, {
                itemCode: item.itemCode,
                itemId: item.itemCode,
                DepttName: item.deptCode,
                unit: item.UOM_CODE,
                nos: item.nos,
                qty: item.qty,
                shipRate: item.rate,
                empty: item.emptY_YN,
                remarks: item.remarks,
                refType: item.docType,
                refNo: item.docNo
            });
      });
    }

    async function LoadFormByID(id, vtype) {
    try {
        const res = await $.ajax({
            url: '/InwardEntryList/GetDataByCode',
            method: 'POST',
            data: {code: id, vtype: vtype }
        });

    if (res.success) {
        const header = res.data.header;
        const Details = res.data.details;
        $('#ddlDocType').val(header.v_TYPE || '');
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
        $('#TxtPONo').val(header.disP_PLAN_NO || '');
        $('#TxtRptDate').val(formatDate(header.r_DATE) || '');
        $('#TiRptDate').val(header.r_TIME || '');
        $('#TxtBillNo').val(header.bilL_NO || '');
        $('#DtPartyBillDate').val(formatDate(header.bilL_DATE) || '');
        $('#TxtChallanNo').val(header.chalL_NO || '');
        $('#TxtChallanDate').val(formatDate(header.chalL_DATE) || '');
        $('#TxtBillAmt').val(header.bilL_AMT || '');
        $('#ddlDocStatus').val(header.status || '');
        $('#ddlTransit').val(header.transiT_NO || '');
        $('#TxtEWayNo').val(header.waybilL_NO || '');
        $('#DtEWayDate').val(formatDate(header.ewB_DATE)|| '');
        $('#TxtEWayDate').val(formatDate(header.ewB_DATE) || '');
        $('#TxtEWBInvNo').val(header.ewB_INVNO || '');
        $('#TxtEWBInvAmt').val(header.ewB_INVAMT || '');
        $('#TxtWbSlipNo').val(header.partY_WBSLIPNO || '');
        $('#TxtGrWt').val(header.gR_NO || '');
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
            itemCode: "345",
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
        }
        } catch (err) {
        showToast("Something went wrong while loading the form.", { type: "error" });
        }
    }

    async function GetFasttagVehicledetail() {
    try {
        const rcNumber = $('#TxtVehicleNo').val();
        const VType = $('#ddlDocType').val();
        const VNo = $('#TxtDocNo').val();

        if(!rcNumber)
        {
            showToast("Vehicle No Not Found", { type: "info" });
            $('#TxtVehicleNo').addClass('is-invalid').focus();
            return;
        }
        else
        {
            $('#TxtVehicleNo').removeClass('is-invalid');
        }

        const res = await $.ajax({
            url: `/InwardEntry/GetVehcleFastaginfo`,
            data : {rc_number : rcNumber , VType : VType ,VNo : VNo },
            type: 'GET',
            dataType: 'json',
        });

      console.log("fasttag api", res);

        if (res && res.success)
        {
        showToast("FastTag Info Saved Successfully", { type: "success" });
        } else
        {
            const apiError = JSON.parse(res.value.message);
            console.log("apiError", apiError);
            showToast(apiError.message, { type: "info" });
        }
        } catch (err)
        {
            showToast("Error fetching vehicle details.", { type: "error" });
        }
    }

    async function GetVehicledetail() {
   try {
        const rcNumber = $('#TxtVehicleNo').val();
        const VType = $('#ddlDocType').val();
        const VNo = $('#TxtDocNo').val();
        if(!rcNumber)
        {
            showToast("Vehicle No Not Found", { type: "info" });
            $('#TxtVehicleNo').addClass('is-invalid').focus();
            return;
        }
        else
        {
            $('#TxtVehicleNo').removeClass('is-invalid');
        }

        const res = await $.ajax({
            url: `/InwardEntry/GetVehcleinfo`,
            data : {rc_number : rcNumber , VType : VType ,VNo : VNo },
            type: 'GET',
            dataType: 'json',
        });

        if (res && res.success) {
         showToast("Vehicle Info Saved Successfully", { type: "success" });
        } else {
            const apiError = JSON.parse(res.value.message);
            console.log("apiError", apiError);

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
 
    function addRow($tbody, data = { }) {
                const isINMS = $('#ddlDocType').val() !== 'INMS';
    const isNewRow = !data || Object.keys(data).length === 0;

    const normalStyle = "background-color:#fff;opacity:1;color:#000;";

    let itemOptions = `<option value="">Select</option>`;
    $.each(itemList, function (i, item) {
                const selected = item.value == data.itemId ? "selected" : "";
    itemOptions += `<option value="${item.value}" data-code="${item.code}" ${selected}> ${item.text} </option>`;
            });

    // DEPARTMENT
    let deptOptions = `<option value="">Select</option>`;
    $.each(deptList, function (i, item) {
                    const selected = item.value == data.DepttName ? "selected" : "";
    deptOptions += `<option value="${item.value}" ${selected}>${item.text}</option>`;
                });

    // UNIT
    let unitOptions = `<option value="">Select</option>`;
    $.each(unitList, function (i, item) {
                    const selected = item.value == data.unit ? "selected" : "";
    unitOptions += `<option value="${item.value}" ${selected}>${item.text}</option>`;
                });

    const row = `
    <tr class="no-border-input">
        <td>
            <input type="text" class="form-control itemCode numeric-only" style="${normalStyle}" value="${data.itemCode ?? ''}" readonly />   </td>
    </td>

    <td> <select class="form-control ItemName searchable-item" style="${normalStyle}; width: 350px;">  ${itemOptions} </select>
    </td>

    <td>
        <select class="form-control DeptName" style="${normalStyle}"> ${deptOptions}  </select>
    </td>

    <td>
        <select class="form-control unit" style="${normalStyle}">  ${unitOptions} </select>
    </td>

    <td>
        <input type="text" class="form-control nos numeric-only" style="${normalStyle}" value="${data.nos ?? ''}" />   </td>
    <td>
        <input type="text" class="form-control quantity numeric-only" style="${normalStyle}" value="${data.qty ?? ''}" />
    </td>
    <td>
        <input type="text" class="form-control shiprate numeric-only" style="${normalStyle}" value="${data.shipRate ?? ''}" />
    </td>

    <td>
        <select class="form-control Empty">
            <option value="" ${(data.empty ?? '') === '' ? 'selected' : ''}>Select</option>
            <option value="Yes" ${data.empty === 'Yes' ? 'selected' : ''}>Yes</option>
            <option value="No" ${data.empty === 'No' ? 'selected' : ''}>No</option>
        </select>
    </td>

    <td>
        <input type="text" class="form-control remarks" style="${normalStyle}" value="${data.remarks ?? ''}" />
    </td>

    <td>
        <input type="text" class="form-control refType" style="${normalStyle}" value="${data.refType ?? ''}" />
    </td>

    <td>
        <input type="text" class="form-control refNo" style="${normalStyle}" value="${data.refNo ?? ''}" />
    </td>

    <td>
        <i class="fa fa-plus btn-add-row text-success me-2" style="cursor:pointer;"></i>
        <i class="fa fa-trash btn-delete-action text-danger" style="cursor:pointer;"></i>
    </td>
</tr>
`;

                $tbody.append(row);
                const $row = $tbody.find('tr:last');

                $row.find('.searchable-item').select2({
                    placeholder: "Search Item",
                    width: '100%'
                });

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


                if (isINMS) {
                    $row.find('input').prop('readonly', true).attr('style', normalStyle);
                    $row.find('select').prop('disabled', true).attr('style', normalStyle);
                    $tbody.find('.btn-add-row').hide();
                } else {
                    $tbody.find('.btn-add-row').show();
                }
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
    
    async function GetEwaybillno() {
            try {
                const res = await $.ajax({
                    url: '/InwardEntry/GetEWayBillData',
                    type: 'GET',
                    data: { edate: $('#InDate').val(),inoutdata :"OUT"  },
                    dataType: 'json'
                });

              if(res.success)
              {
               showToast("Successfully", { type: "success" });
              }

            } catch (error) {
                showToast(error, { type: "error" });
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