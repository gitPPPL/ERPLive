

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
        let REMARKS = $('#TxtRemarks').val();
        let action = $.trim($('#CODE').val()) ? 'UPDATE' : 'INSERT';

        if (!validateRequiredField('#ddlDocType', 'Please select a Doc Type')) return;
        if (!validateRequiredField('#NumDocno', 'Please select a Doc NO')) return;
        if (!validateRequiredField('#DtDocDate', 'Please select a Doc Date')) return;

        let inventoryData = GetInventoryOpeningData();

        // At least one item is required
        let hasItem = inventoryData.some(function (row) {
            return $.trim(row.ITEM_CODE) !== '';
        });

        if (!hasItem) {
            showToast('Please enter at least one item.', 'Error');
            return;
        }

        let requestData = {
            Header: {
                action: action,
                V_TYPE: V_TYPE,
                V_NO: V_NO,
                V_DATE: V_DATE,
                REMARKS: REMARKS
            },
            Details: inventoryData
        };

        $.ajax({
            url: '/InventoryOpeningEntry/SavedData',
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

    $('#btn_StoreReturn').on('click', async function () {

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









    });


});
