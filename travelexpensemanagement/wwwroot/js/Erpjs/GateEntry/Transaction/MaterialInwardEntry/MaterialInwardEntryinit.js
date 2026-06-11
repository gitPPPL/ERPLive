

const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
const vtype = urlParams.get('vtype');
const PubDefEWaybillAmt = 50000;
var PubUserLevel = '@PubUserLevel';
var LoginDate = '@logindate';
var itemList = [];
var deptList = [];
var unitList = [];

$(document).ready(async function () {

    await LoadDropDown();
    if (rowId) {
        await LoadFormByID(rowId, vtype);
        //await Approvalbtn();
        checkApprovalStatus(vtype, rowId, 'GATE1');
        $('#ddlDocType').prop('disabled', true);
        $('#InDate').prop('disabled', true);
        $('#InTime').prop('disabled', true);
        $('.erppagelist-toolbar-end').show();
        if (mode === "view") {
            setFormReadOnly();  
            //await Approvalbtn();
            checkApprovalStatus(vtype, rowId, 'GATE1');
        }
    }
    else {
        if (PubUserLevel == 1) {
            $('#InDate').prop('disabled', false);
            $('#InTime').prop('disabled', false);
        }
        else {
            $('#InDate').prop('disabled', true);
            $('#InTime').prop('disabled', true);
        }

        $('#ddlDocStatus').prop('disabled', true);
        let today = new Date().toISOString().split('T')[0];
        $('#InDate').attr('min', LoginDate);
        $('#TxtRptDate').val(today);
        let now = new Date();
        $('#InTime').val(now.toTimeString().slice(0, 8));
        $('#TiRptDate').val(now.toTimeString().slice(0, 8));
        GetVNo($('#ddlDocType').val());
    }

    $('#ddladdressline1').on('change', function () {
        let selectedPartyValue = $('#ddlPartyName').val();
        let selectedAddressValue = $('#ddladdressline1').val();
        fetchDDlParty(selectedPartyValue, selectedAddressValue);
    });

    $(document).on('input', '.numeric-only', function () {
        this.value = this.value.replace(/[^0-9.]/g, '');
    });

    $('#TxtBillNo').on('change', function () {
        if ($(this).val()) {
            $('#span_partybilldate').show();
        } else {
            $('#span_partybilldate').hide();
        }
    });

    $('#TxtBillNo').on('focusout', function () {

        var partyBillNo = $.trim($(this).val());

        if (partyBillNo === '') {
            return;
        }

        if (!validateRequiredField('#ddlPartyName', 'Please select Party Name')) {
            return;
        }

        var partyCode = $('#ddlPartyName').val();

        $.ajax({
            url: '/InwardEntry/GetPasrtyBillNo',
            type: 'GET',
            data: {
                partyCode: partyCode,
                PartyBillNo: partyBillNo
            },
            success: function (response) {        

                if (!response.success == false) {     
                    showToast('Bill No. "' + partyBillNo + '" already exists in Container Tracking Record at Sauda No => ' + response.refSaudaNo, { type: 'warning' });
                    $('#TxtBillNo').focus();
                }
            },
            error: function (xhr, status, error) {

                console.error(error);

                showToast(
                    'Error while checking Bill No.',
                    { type: 'error' }
                );
            }
        });
    });

    $('#TxtChallanNo').on('change', function () {
        if ($(this).val()) {
            $('#span_ChallanDatedate').show();
        } else {
            $('#span_ChallanDatedate').hide();
        }
    });

    $('#TxtVehicleNo').on('change', function () {
        if ($(this).val()) {
            $('#span_drivername').show();
            $('#span_driverMobileNo').show();
            $('#span_tranportarname').show();
        }
        else {
            $('#span_drivername').hide();
            $('#span_driverMobileNo').hide();
            $('#span_tranportarname').hide();
        }
    });

    $('#TxtEWayNo').on('change', function () {
        if ($(this).val()) {
            $('#span_EWayBillDate').show();
            $('#span_EWayBillExpiryDate').show();
            $('#span_EWBPartyInvNo').show();
            $('#span_EWBPartyInvAm').show();
        }
        else {
            $('#span_EWayBillDate').hide();
            $('#span_EWayBillExpiryDate').hide();
            $('#span_EWBPartyInvNo').hide();
            $('#span_EWBPartyInvAm').hide();
        }
    });

    $('#TxtEWayNo').on('focusout', function () {

        var waybillNo = $.trim($(this).val());

        if (waybillNo === '') {
            return;
        }

        if (!validateRequiredField('#ddlPartyName', 'Please select Party Name')) {
            return;
        }

        var partyCode = $('#ddlPartyName').val();
        var partyName = $('#ddlPartyName option:selected').text();

        $.ajax({
            url: '/InwardEntry/GetTransitNoLeaveEwayBill',
            type: 'GET',
            data: {
                partyCode: partyCode,
                waybillNo: waybillNo
            },
            success: function (response) {

                console.log('Response:', response);
                if (response.success) {
                    $('#ddlTransit').val(response.v_NO || '');  
                } else {
                    showToast( 'Incorrect E-Way Bill No. for Party: ' + partyName, { type: 'warning' } );
                    $('#TxtEWayNo').focus();
                }
            },
            error: function (xhr, status, error) {
                console.error(error);
                showToast('Error while checking E-Way Bill.', { type: 'error' });
            }
        });
    });

    //$('#TxtVehicleNo').on('focusout', function () {

    //    var VehicleNo = $.trim($(this).val());

    //    if (VehicleNo === '') {
    //        return;
    //    }

    //    $.ajax({
    //        url: '/InwardEntry/Getvehicleno',
    //        type: 'GET',
    //        data: {
    //            TruckNo: VehicleNo              
    //        },
    //        success: function (response) {
    //            console.log('Response:', response);
    //            if (response.success) {
               
    //            } 
    //        },
    //        error: function (xhr, status, error) {
    //            console.error(error);
    //            showToast('Error while Vehicle No data.', { type: 'error' });
    //        }
    //    });
    //});


    $('#TxtWbSlipNo').on('change', function () {

        if ($(this).val().trim() !== '') {

            $('#TxtGrWt, #TxtTrWt, #TxtWbTime, #DtWBTime')
                .removeClass('erppage-input')
                .addClass('erppage-redinput');

        } else {

            $('#TxtGrWt, #TxtTrWt, #TxtWbTime, #DtWBTime')
                .removeClass('erppage-redinput')
                .addClass('erppage-input');
        }

    });

    $('#TxtWbSlipNo').on('change', function () {

        if ($(this).val().trim() !== '') {

            $('#TxtGrWt, #TxtTrWt, #TxtWbTime, #DtWBTime')
                .removeClass('erppage-input')
                .addClass('erppage-redinput');

        } else {

            $('#TxtGrWt, #TxtTrWt, #TxtWbTime, #DtWBTime')
                .removeClass('erppage-redinput')
                .addClass('erppage-input');
        }

    });

    $("#btn-save").click(async function (e) {
        e.preventDefault();
        const PARTY_CODE = parseInt($('#ddlPartyName').val()) || null;
        const V_NO = parseInt($('#TxtDocNo').val()) || null;
        const BILL_NO = $('#TxtBillNo').val().trim();
        const V_TYPE = $('#ddlDocType').val();

        if (!validateRequiredField('#ddlDocType', 'Please select a Voucher Type')) return;
        if (!validateRequiredField('#TxtDocNo', 'Please select a Voucher No')) return;
        if (!validateRequiredField('#ddlDocStatus', 'Please select a Status.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;  


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

        saveInwardEntry();
    });

    $('#ddlShipFrom').on('change', function () {
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
            addRow($("#tblInwardEntry tbody"), {});
        } else {
            $("#tblInwardEntry tbody").empty();
            addRow($("#tblInwardEntry tbody"), {});
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

    $('#ddlPartyName').on('change', function () {
        const PartyId = this.value;
    
        const Vno = document.getElementById('TxtDocNo')?.value || '';
        const v_type = document.getElementById('ddlDocType')?.value || '';
        const indate = document.getElementById('InDate')?.value || '';
        $('#ddlDocType').prop('disabled', true);
        fetchTransitno(v_type, Vno, PartyId, indate,"", "PARTYSELECT");
        GetPartyAdress(PartyId);
        DDlPartyAdd(PartyId);
        $('#ddlDocStatus').prop('disabled', true);
    });

    $('#Btn_selectedData').on('click', function () {

        const selectedData = getSelectedPendingOrderRows();

        if (!selectedData || selectedData.length === 0) {
            showToast("Please select at least one row.", { type: "warning" });
            return;
        }

        const modalElement = document.getElementById('pendingorders');
        const modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);

        $.ajax({
            url: '/InwardEntryList/SaveSelectedRows',
            type: 'POST',
            traditional: true,
            data: {
                selectedRows: selectedData.map(x => JSON.stringify(x)),
                partycode: $('#ddlPartyName').val(),
                v_type: $('#ddlDocType').val(),
                v_no: $('#TxtDocNo').val()
            },
            success: function (response) {

                console.log("Response:", response);

                if (response.success) {

                    modalInstance.hide();

                    $('#TxtTransporter').val(response.transportCode || '');
                    $('#TxtVehicleNo').val(response.truckNo || '');

                    populateInwardEntryTable(response.rows);

                    showToast(
                        response.count + " row(s) loaded successfully.",
                        { type: "success" }
                    );
                }
                else {
                    showToast(response.message, { type: "warning" });
                }
            },
            error: function (xhr) {
                console.log(xhr.responseText);
                showToast("Error while processing request.", { type: "danger" });
            }
        });
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

    $(document).on('click', '.delete-btn', function (e) {
        e.preventDefault();

        if (confirm('Do you want to delete this row?')) {
            $(this).closest('tr').remove();
        }
    });

    $(document).on('change', '.ItemName', function () {
        const code = $(this).val();
        $(this).closest('tr').find('.itemCode').val(code);
    });

    $('#SEARCHCONTAINER').on('click', function () {
        var Container_No = $('#TxtContainerNo').val();
        if (Container_No) {
            $('#TxtContainerNo').removeClass('is-invalid');
            getcontainerdata(Container_No);
        }
        else {
            showToast("Please Fill Container_No", { type: "info" });
            $('#TxtContainerNo').addClass('is-invalid').focus();
            return false;
        }
    });

    $('#btn_backtolist').on('click', function () {
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
                data: { SUPPLIER: SUPPLIER }
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

        const row = selectedrows[0];
        $('#TxtBillNo').val(row.supplieR_INVNO);
        $('#DtPartyBillDate').val(row.supplieR_INVDATE);
        $('#TxtBillAmt').val(row.supplieR_INVAMT);
        $('#TxtContainerNo').val(row.containeR_NO);
    });

    //$("#btn_approval").on('click', function () {
    //    openApprovalModal();
    //});

    //$("#btn_Sendapproval").on('click', function () {
    //    sendopenApprovalModal();
    //});

    $('#btn_Sendapp').on('click', function () {
        SendApproval();
    });

    $('#btn_approvalok').on('click', function () {
        SendWindowApproval();
    });


    $(document).on('change', '#ddlTransit', function () {
        const transitNo = $(this).val();
        $('#TxtEWayNo').val('');
        $('#TxtBillAmt').val('');
        $('#TxtEWBInvAmt').val('');
        $('#TxtEWBInvNo').val('');
        $('#DtEWayDate').val('');
        $('#TxtEWayDate').val('');
        if (!transitNo) return;
        GetTransitnodata(transitNo);
    });

    $('#TxtPONo').on('change', async function () {
        const Pono = parseInt($('#TxtPONo').val()) || null;
        const PonoText = $('#TxtPONo option:selected')
            .text()
            .replace(/\s*\(\d+\)$/, '');

        console.log("Pono", Pono);
        console.log("PonoText", PonoText);


        const PARTY_CODE = $('#ddlPartyName').val() ? parseInt($('#ddlPartyName').val(), 10)  : null;
        const V_TYPE = $('#ddlDocType').val();
        const V_NO = parseInt($('#TxtDocNo').val()) || null;
        if (!Pono ) {
            return;
        }

        try {
            const response = await $.ajax({
                url: '/InwardEntryList/GetDataPono',   
                type: 'GET',
                data: {
                    Pono: Pono,
                    Ponotext: PonoText,
                    PARTY_CODE: PARTY_CODE,
                    V_TYPE: V_TYPE,
                    V_NO: V_NO
                }
            });

            if (response.success) {
                $('#ddlPartyName').val(response.partY_CODE).trigger('change');
                const $tbody = $('#tblInwardEntry tbody');
                // Clear existing rows
                $tbody.empty();
                // Add rows from response.data
                $.each(response.data, function (i, item) {
                    let deptcode = 0;             
                    if (V_TYPE === "INRM") {
                        deptcode = 145;         
                    }
                    else if (V_TYPE == "INFU") {
                        deptcode = 138;  
                    }
                    else {
                        deptcode = 110; 
                    }
                    addRow($tbody, {
                        itemCode: item.item_code,
                        itemId: item.item_code,      // must match itemList.value
                        unit: item.uniT_CODE,
                        DepttName: deptcode,
                        nos: item.nos,
                        qty: item.qty,
                        empty: 'No',
                        remarks: '',
                        shipRate: '',
                        refType: PonoText,
                        refNo: Pono
                    });
                });
                showToast(response.message || 'Data Fetch Successfully', { type: 'Success' });
            }

            else {
                showToast(response.message || '', { type: 'info' });
            }

        } catch (error) {
            console.error('GetDataPono Error:', error);
            showToast('Error loading data.', { type: 'error' });
        }
    });
    //kks

    $(document).on('click', '#btn_Sendapproval', function () {
        $.ajax({
            url: '/Approval/CheckPendingUser',
            type: 'POST',
            data: {
                vNo: rowId,
                vType: vtype
            },
            success: function (response) {
                console.log('Response:', response);
                // Pending with another user
                if (response.success === false) {
                    showToast(`Pending With Another User (${response.userCode})`,
                        { type: "warning" });
                    return;
                }
                // Approval_Code = 5
                if (response.approvalCode8 === true) {
                    OpenApprovalModal({
                        DocType: vtype,
                        DocNo: rowId,
                        TableName: 'GATE1'
                    });
                    return;
                }
                // Approval_Code != 8
                OpenSendForApprovalModal({
                    DocType: vtype,
                    DocNo: rowId,
                    UserCode: null,
                    UserName: null,
                    DocDate: null,
                    TableName: 'GATE1'
                });
               
            },
            error: function (xhr, status, error) {
                console.log(error);
                alert('Error while checking approval status.');
            }
        });

    });

    $(document).on('click', '#btn_Approved', function () {
        OpenApprovalModal({
            DocType: vtype,
            DocNo: rowId,
            TableName: 'GATE1'
        });
    });

    //kks

});