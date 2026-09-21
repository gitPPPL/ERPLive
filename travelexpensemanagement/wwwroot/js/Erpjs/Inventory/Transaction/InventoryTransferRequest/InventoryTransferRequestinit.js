

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


$(document).ready(async function ()
{

    try {
        SetFYDate('DtDocDate', LoginDate);
        SetFYDate('DtFromDate', LoginDate);
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
            checkApprovalStatus(vtype, rowId, 'ISSUE1');
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
        if (!validateRequiredField('#ddlHDepartment', 'Please select a Daepartment')) return;
        if (!validateRequiredField('#ddlShift', 'Please select a Shift')) return;
        if (!validateRequiredField('#ddlPlace', 'Please select a Place')) return;
        if (!validateRequiredField('#ddlHodName', 'Please select a HOD Name')) return;

        if (!validateInventoryDetails()) {
            return;
        }

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
                DEPT_CODE: $('#ddlHDepartment').val() || '',
                SHIFT: $('#ddlShift').val() || '',
                PLACE_CODE: $('#ddlPlace').val() || '',
                EMP_CODE: $('#ddlHodName').val() || '',
                DOC_ID: $('#CODE').val() || '',
                REMARKS: $('#TxtRemarks').val() || ''
            },

            Details: GetInventoryOpeningData()
        };
        console.log("requestData", requestData);

        $.ajax({
            url: '/InventoryTransferRequest/SavedData',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(requestData),

            success: function (response) {

                console.log("response", response);

                if (response.status == "Success")
                {
                 showToast(response.message, "Success");
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

    //kks

    $(document).on('click', '#btn_Sendapproval', function () {
        var FromName = window.location.pathname.split('/')[1];
        let vNo = $('#NumDocno').val();
        let vtype = $('#ddlDocType').val();

        $.ajax({
            url: '/Approval/CheckPendingUser',
            type: 'POST',
            data: { vNo: vNo, vType: vtype },
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
                    OpenApprovalModal({ DocType: vtype, DocNo: vNo, TableName: 'ISSUE1' });
                    return;
                }
                // Approval_Code != 8
                OpenSendForApprovalModal({ DocType: vtype, DocNo: vNo, UserCode: null, UserName: null,
                    DocDate: null, TableName: 'ISSUE1', FromName, FromName
                });

            },
            error: function (xhr, status, error) {
                console.log(error);
                alert('Error while checking approval status.');
            }
        });

    });

    $(document).on('click', '#btn_Approved', function () {
        OpenApprovalModal({ DocType: vtype, DocNo: vNo,  TableName: 'ISSUE1' });
    });

    //kks

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



});
