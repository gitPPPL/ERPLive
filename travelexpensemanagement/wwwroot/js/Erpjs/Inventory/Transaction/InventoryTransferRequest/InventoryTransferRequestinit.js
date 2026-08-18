

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

let ItemNameList = "";
let unitnameList = "";
let ItemmakeList = "";
let ItemDeptList = "";


$(document).ready(async function ()
{

    try {
        SetFYDate('DtDocDate', LoginDate);
        SetFYDate('DtFromDate', LoginDate);
        SetFYDate('DtToDate', LoginDate);
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
            if (mode === "view") {
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

        // If no item is selected
        if (!itemCode) {
            $row.find('.ddlUnit').val('').trigger('change');
            $row.find('.ItemCode').val('');
            return;
        }

        try {

            const res = await $.ajax({
                url: '/InventoryTransferRequest/GetDataByItemcode',
                type: 'POST',
                data: {
                    ItemCode: itemCode
                }
            });

            console.log('Response:', res);

            if (res && res.status && res.data) {

                const unitCode = res.data.unit_code || '';

                // Set FromPlace for first row
                // Set ToPlace for other rows
                if ($row.index() === 0) {
                    $row.find('.FromPlace').val(deptCode);
                } else {
                    $row.find('.ToPlace').val(deptCode);
                }

                // Set Item Code

                $row.find('.ItemCode').val(itemCode);

                // Set Unit
                $row.find('.ddlUnit')
                    .val(unitCode)
                    .trigger('change');

            } else {

                console.warn('Item data not found:', res);

                $row.find('.ddlUnit')
                    .val('')
                    .trigger('change');

                $row.find('.ItemCode').val('');
            }

        } catch (error) {

            console.error('Error fetching item data:', error);

            $row.find('.ddlUnit')
                .val('')
                .trigger('change');
        }
    });




    $('#btn_save').on('click', function ()
    {

        let V_TYPE = $('#ddlDocType').val();
        let V_NO = $('#NumDocno').val();
        let V_DATE = $('#DtDocDate').val();
        let REMARKS = $('#TxtRemarks').val();
        let action = $.trim($('#CODE').val()) ? 'UPDATE' : 'INSERT';
        let inventoryData = GetInventoryOpeningData();

        let requestData = {
            Header: {
                action: rowId ? 'UPDATE' : 'INSERT',
                V_TYPE: V_TYPE,
                V_NO: V_NO,
                V_DATE: V_DATE,
                REMARKS: REMARKS,
                action: action
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
                    showToast(response.message,"Success");
                }
                else {
                    showToast(response.message,"Error");
                }

            },
            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                showToast("Error while saving data.", "Error");
            }
        });

    });

});
