

const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');

var globalVars = window.globalVariables || {};
var database = window.database || "";
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];

let ItemNameList = '';
let PlaceFromList = '';
let ItemDetailsList = [];

$(document).ready(async function ()
{
    try {
        SetFYDate('DtDocDate', LoginDate);
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
            let v_no = $('#NumDocno').val();
            DDLDO(v_type, v_no);
        }
    });

    $('#btn_save').on('click', function () {

        let V_TYPE = $('#ddlDocType').val();
        let V_NO = $('#NumDocno').val();
        let V_DATE = $('#DtDocDate').val();
        let STATUS = $('#ddlStatus').val();
        let SHIFT = $('#ddlShift').val();
        let SLIP_NO = $('#NumSlipNo').val();

        let PORD_NO = $('#ddlProdOrdNo').val();
        let PORD_TYPE = $('#ddlProdOrdNo option:selected').text().substring(0, 4);
        let REMARKS = $('#TxtRemarks').val();
        let PLAN_NO = $('#ddlDoNo').val();
        let PLAN_TYPE = $('#ddlDoNo option:selected').text().substring(0, 4);
        let action = $.trim($('#CODE').val()) ? 'UPDATE' : 'INSERT';

        if (!validateRequiredField('#ddlDocType', 'Please select a Doc Type')) return;
        if (!validateRequiredField('#NumDocno', 'Please select a Doc NO')) return;
        if (!validateRequiredField('#DtDocDate', 'Please select a Doc Date')) return;    

        let data = GetInventoryDepartmentIssueDetails();

        let hasItem = data.some(function (row) {
            return $.trim(row.ITEM_CODE) !== '';
        });

        if (!hasItem) {
            showToast('Please enter at least one item.', 'Error');
            return;
        }
       
        let requestData = {
            Header: {
                V_TYPE: V_TYPE,
                V_NO: V_NO,
                V_DATE: V_DATE,
                STATUS: STATUS,
                SHIFT: SHIFT,
                SLIP_NO: SLIP_NO,
                PORD_NO: PORD_NO,
                PORD_TYPE: PORD_TYPE,
                REMARKS: REMARKS,
                PLAN_NO: PLAN_NO ,
                PLAN_TYPE: PLAN_TYPE,
                action: action
            },
            Details: data
        };


        console.log("requestData", requestData);

        $.ajax({
            url: '/InventoryDepartmentIssue/SavedData',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(requestData),

            success: function (response) {

                if (response.success) {
                    showToast(response.message, "Success");
                }
                else {
                    showToast(response.message, "Error");
                }

            },

            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                showToast("Error while saving data.", "Error");
            }
        });

    });

    $(document).on('change', '#tblItemdetails .ddlItemname', function () {

        let $row = $(this).closest('tr');

        SetItemDetails($row);

    });

    $(document).on('input change', '#tblItemdetails .TxtNos, #tblItemdetails .TxtRate', function () {

        let $row = $(this).closest('tr');

        CalculateAmount($row);

    });
     

    $('#btn_AdjustmentIssue').on('click', async function ()
    {
        const v_type = $('#ddlDocType').val();
        await CopyData(v_type);

    });

    $('#btn_StoreReturn').on('click', async function ()
    {
        const v_type = $('#ddlDocType').val();
        await CopyData(v_type);
    });

    $('#selectAll').on('change', function () {

        let isChecked = $(this).is(':checked');

        $('#tbladjustmentissue tbody .chk_box').prop('checked', isChecked);
    });

    $('#btn_copyForm').on('click', function () {

        let data = GetSelectedRowsData();

        console.log("Selected Data", data);

        if (!data || data.length === 0) {
            alert("Please select at least one row.");
            return;
        }

        $('#tblItemdetails tbody').empty();

        data.forEach(function (item) {

            AddRow({
                itemCode: item.itemCode,
                unitCode: item.unitCode,
                unitName: item.unitCode,
                lot: item.vNo,
                nos: item.qty,
                weight: '',
                placeCode: item.placeCode,
                place: item.place,       
                remark: item.remarks,
                rate: '',
                Amount: '',
                LDRate: '',
                LDAmount: '',
                ProdType: '',
                ProdNo: ''
            });

        });

    });









});
