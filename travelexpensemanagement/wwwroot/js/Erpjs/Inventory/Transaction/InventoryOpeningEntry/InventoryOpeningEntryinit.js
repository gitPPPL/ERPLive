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


$(document).ready(async function () {

    try {

        await LoadDropDown();
        AddRow();
        if (rowId == null) {
            let v_type = $('#ddlDocType').val();

            if (v_type) {
                await GetVNo(v_type);
            }
        }

    } catch (error) {

        console.error("An error occurred:", error);

    }


    $('#ddlDocType').on('change', async function () {

        let v_type = $(this).val();

        if (v_type) {
            await GetVNo(v_type);
        }

    });




    //$(`#row${rowCount} .TxtQty, #row${rowCount} .TxtRate`).on('input', function () {

    //    let qty = parseFloat($(`#row${rowCount} .TxtQty`).val()) || 0;
    //    let rate = parseFloat($(`#row${rowCount} .TxtRate`).val()) || 0;

    //    $(`#row${rowCount} .TxtAmount`).val((qty * rate).toFixed(2));
    //});



});
