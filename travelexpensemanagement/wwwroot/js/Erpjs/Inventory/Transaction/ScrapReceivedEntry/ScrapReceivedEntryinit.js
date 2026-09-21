

const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const vtype = rowId ? rowId.substring(0, 4) : '';
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
var globalVars = window.globalVariables || {};
var database = window.database || "";
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];
let ItemNameList = "";
let unitnameList = "";
let ItemmakeList = "";
let ItemDeptList = "";
let ScrapNameList = "";


$(document).ready(async function ()
{
    try {
        SetFYDate('DtDocDate', LoginDate);
        SetFYDate('DtFrom', LoginDate);
        SetFYDate('DtToDate', LoginDate);

        checkPermissionForEntryPage(controllerName);
        await LoadDropDown();
        AddRow();

        if (rowId == null)
        {
            let v_type = $('#ddlDocType').val();
            if (v_type)
            {
                await GetVNo(v_type);
            }
        }

        else
        {          
            await LoadData(rowId);
            if (mode === "view")
            {
                setFormReadOnly();
            }
        }

    }
    catch (error)
    {
        console.error("An error occurred:", error);
    }

    $('#ddlDocType').on('change', async function () {

        let v_type = $(this).val();

        if (v_type) {
            await GetVNo(v_type);
        }

    });

    $(document).on('input', '.TxtQty, .TxtRate', function () {

        let row = $(this).closest('tr');

        let qty = parseFloat(row.find('.TxtQty').val()) || 0;
        let rate = parseFloat(row.find('.TxtRate').val()) || 0;

        row.find('.TxtAmount').val(qty * rate);
    });

    $(document).on('change', '.ddlItemname', async function () {

        const $row = $(this).closest('tr');
        const itemCode = $(this).val();
        const deptCode = $('#ddlHDepartment').val();

        if (!itemCode) {
            $row.find('.ddlUnit').val('').trigger('change');
            $row.find('.ItemCode').val('');
            return;
        }

        try {


            if ($row.index() === 0)
            {
                $row.find('.FromPlace').val(deptCode);
                $row.find('.ToPlace').val(deptCode);
            }
            else {
                const $prevRow = $row.prev('tr');
                const prevFromPlace = $prevRow.find('.FromPlace').val() || '';
                const prevToPlace = $prevRow.find('.ToPlace').val() || '';
                $row.find('.FromPlace').val(prevFromPlace);
                $row.find('.ToPlace').val(prevToPlace);
            }

            $row.find('.ItemCode').val(itemCode);

            const res = await $.ajax({
                url: '/InventoryTransferRequest/GetDataByItemcode',
                type: 'POST',
                data: {
                    ItemCode: itemCode
                }
            });

            if (res && res.status && res.data)
            {
                const unitCode = res.data.unit_code || '';         
                $row.find('.ddlUnit') .val(unitCode) .trigger('change');
            }
            else
            {
                $row.find('.ddlUnit') .val('')  .trigger('change');
            }

        } catch (error) {

            console.error('Error fetching item data:', error);

            $row.find('.ddlUnit')  .val('') .trigger('change');
        }
    });

    $('#btn_save').on('click', async function ()
    {
        if (!validateRequiredField('#ddlDocType', 'Please select a Doc Type')) return;
        if (!validateRequiredField('#NumDocno', 'Please select a Doc No')) return;
        if (!validateRequiredField('#DtDocDate', 'Please select a Doc Date')) return;


        if (!validateScrapReceivedDetails()) {
            return;
        }


        //if (!validateInventoryDetails()) {
        //    return;
        //}

        const isValid = await checkValidDate();
        if (isValid === false) {
            return;
        }  

        let action = $.trim($('#CODE').val()) ? 'UPDATE' : 'INSERT';

        let requestData = {
            Header: {
                action: action,
                V_TYPE: $('#ddlDocType').val() || '',
                V_NO: $('#NumDocno').val() || 0,
                V_DATE: $('#DtDocDate').val() || '',
                STATUS: $('#ddlStatus').val() || '',
                PARTY: $('#ddlACName').val() || '',
                PLACE_CODE: $('#ddlPlace').val() || '',         
                DOC_ID: $('#CODE').val() || '',
                REMARK: $('#TxtRemarks').val() || ''
            },

            Details: GetScrapReceivedEntryData()
        };
        console.log("requestData", requestData);

        $.ajax({
            url: '/ScrapReceivedEntry/SavedData',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(requestData),

            success: function (response) {

                console.log("response", response);

                if (response.status == "Success")
                {
                    showToast(response.message, "Success");

                    setTimeout(function () {
                        window.location.href = '/ScrapReceivedEntry/Index?id=' + rowId + '&mode=view';
                    }, 3000);                               

                }
                else if (response.status == "Info")
                {
                    showToast(response.message, "info");
                }
                else
                {
                 showToast(response.message, "error");
                }
            },

            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                showToast("Error while saving data.", "Error");
            }
        });

    });

    $(document).on('click', '#btn_print', async function () {

        try {

            let V_TYPE = $('#ddlDocType').val();
            let From_DATE = $('#DtFromDate').val();
            let To_DATE = $('#DtToDate').val();
            let DEPT_CODE = $('#ddlDepartment').val();

            const res = await $.ajax({
                url: '/InventoryTransferRequest/CreateView',
                type: 'POST',
                data: {
                    V_TYPE: V_TYPE,
                    From_DATE: From_DATE,
                    To_DATE: To_DATE,
                    DEPT_CODE: DEPT_CODE
                }
            });


            console.log("res", res);

            TransitReport();
                  

        } catch (error) {

            console.error('Error:', error);

        }

    });

    $(document).on('click', '#btn_pendinglist', async function ()
    {
        let v_date = $('#DtDocDate').val();
        try {

            const res = await $.ajax({
                url: '/ScrapReceivedEntry/GetPendingData',
                type: 'GET',
                data: { v_date: v_date }
            });

            $('#tblpendinglist tbody').empty();

            if (Array.isArray(res)) {
                res.forEach(function (data) {
                    AddPendingRow(data);
                });
            }

        } catch (error) {
            console.error("Error loading pending data:", error);
        }
    });

    $(document).on('click', '#btn_seletedRow', function () {

        const selectedData = GetSelectedPendingRows();

        console.log('Selected Rows:', selectedData);

        $('#tblScrapreceivedentry tbody').empty();

        selectedData.forEach(function (data)
        {

            AddRow({
                itemCode: data.ITEM_CODE,
                qty: data.open_qty,
                weight: '',
                froM_DEPT: data.TO_DEPT,
                PARTY_CODE: data.PARTY_CODE,
                remarks: ''
            });
        });
    });


    $('#btn_dailyreport').on('click', async function () {
        try {
            let FromData = $('#DtFrom').val();
            let ToDate = $('#DtToDate').val();
            let ItemCdoe = $('#ddlitem').val();
            let DeptCode = $('#ddlDepartment').val();
            let UnitCode = $('#ddlUnit').val();

            const res = await $.ajax({
                url: '/ScrapReceivedEntry/DailyReport',
                type: 'GET',
                data: {
                    From_DATE: FromData,
                    To_DATE: ToDate,
                    itemcode: ItemCdoe,
                    DEPT_CODE: DeptCode,
                    UnitCode: UnitCode
                }
            });
            console.log('Daily Report', res);

            DailyReportTransitReport();



        }
        catch (error) {
            console.log('Error', error);
        }  

    });

    $('#btn_pendingdeptreport').on('click', async function () {
        try {
            let FromData = $('#DtFrom').val();
            let ToDate = $('#DtToDate').val();
            let ItemCdoe = $('#ddlitem').val();
            let DeptCode = $('#ddlDepartment').val();
            let UnitCode = $('#ddlUnit').val();

            const res = await $.ajax({
                url: '/ScrapReceivedEntry/PendingDept',
                type: 'GET',
                data: {
                    From_DATE: FromData,
                    To_DATE: ToDate,
                    itemcode: ItemCdoe,
                    DEPT_CODE: DeptCode,
                    UnitCode: UnitCode
                }
            });
            console.log('Daily Report', res);

            PendingDeptTransitReport();



        }
        catch (error) {
            console.log('Error', error);
        }

    });

    $('#btn_ScrapIssueReport').on('click', async function () {
        try
        {  
            ScrapIssueTransitReport();
        }
        catch (error)
        {
            console.log('Error', error);
        }

    });

    $('#btn_ScrapReceivedReport').on('click', async function () {
        try
        {  
            RecdPrintTransitReport();
        }
        catch (error)
        {
            console.log('Error', error);
        }

    });


    


    $('#btn_ScrapStockReport').on('click', async function () {
        try {
            let FromData = $('#DtFrom').val();
            let ToDate = $('#DtToDate').val();
            let ItemCdoe = $('#ddlitem').val();
            let DeptCode = $('#ddlDepartment').val();
            let UnitCode = $('#ddlUnit').val();

            const res = await $.ajax({
                url: '/ScrapReceivedEntry/ScrapStocREPORT',
                type: 'GET',
                data: {
                    From_DATE: FromData,
                    To_DATE: ToDate,
                    itemcode: ItemCdoe,
                    DEPT_CODE: DeptCode,
                    UnitCode: UnitCode
                }
            });
            console.log('Daily Report', res);

            ScrapStocTransitReport();



        }
        catch (error) {
            console.log('Error', error);
        }

    });




});

