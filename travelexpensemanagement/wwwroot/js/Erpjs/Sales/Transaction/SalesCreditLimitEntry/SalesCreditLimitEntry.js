const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
let isReadOnly = urlParams.get('readOnly') === 'true';

let partyList = [];
let isLoadByEdit = false;

const DocType = "CLMT";
const DBTableName = "CREDIT_LIMIT";

$(document).ready(async function () {
    if (!rowId) {
        await GetVNo();
    }
    const currentDate = getCurrentDateYMD();
    $('#DtDocDate').val(currentDate);

    if (!isReadOnly) {
        $('#DtDocDate').focus();
    }

    await bindPartyDropdown();
    wireEvents();

    if (rowId && !isNaN(rowId)) {
        await GetDataById();
        checkApprovalStatus(DocType, rowId, DBTableName);
    }
})

//=============GENERATE VNO=================
async function GetVNo() {
    try {
        const res = await fetch('/SalesCreditLimitEntry/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=============DROPDOWN=================
function bindPartyDropdown() {

    return $.getJSON('/SalesCreditLimitEntry/GetDropdown?type=party')
        .done(function (data) {

            partyList = data;

            const ddl = $('#ddlPartyName');

            ddl.empty();
            ddl.append('<option value="">--Select Party--</option>');

            $.each(data, function (i, item) {

                ddl.append(
                    $('<option>', {
                        value: String(item.value),
                        text: item.text
                    })
                );

            });

            initSelect2(ddl);

        })
        .fail(function (xhr) {
            console.error("Party dropdown error:", xhr);
        });
}
function initSelect2($ddl) {
    if (!$ddl.length) return;

    // Already initialized
    if ($ddl.hasClass('select2-hidden-accessible')) return;

    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true
    });

    $ddl.on('select2:open', function () {
        setTimeout(function () {
            const searchBox = document.querySelector('.select2-container--open .select2-search__field');
            if (searchBox) searchBox.focus();
        }, 0);
    });
}

//=============DROPDOWN=================
function wireEvents() {
    $('#ddlPartyName').on('change', function () {

        const partyCode = $(this).val();

        const party = partyList.find(x => x.value == partyCode);

        if (!party)
            return;

        console.log("Party Change: ", party);
        $('#NumOldCreditLimitAmount').val(party.CR_LIMIT || '');
        $('#NumOldInsCreditLimitDays').val(party.CR_DAYS || '');
        $('#NumOldOurCreditLimitDays').val(party.OURCR_DAYS || '');
        $('#ddlApprovalType').val(party.APPROVAL_TYPE || '');
        $('#ddlOurApproval').val(party.OURAPPROVAL_TYPE || '');

        getDrCrAmtByPartyCode(partyCode);

        //if (isLoadByEdit) return;
        $('#txtGroupCode').val(party.GROUP_CODE || '');
        $('#txtGroupName').val(party.GroupName || '');
    });

    $('#btn_save').on('click', async function (e) {
        e.preventDefault();

        const isValid = await ValidateSalesCreditLimit();
        if (!isValid) return;

        try {
            SaveSalesCreditLimit();
        }
        catch (error) {
            console.error(error);
        }
    })
}
function getDrCrAmtByPartyCode(code) {

    $.get('/SalesCreditLimitEntry/GetDrCrAmtByPartyCode', { code: code })
        .done(function (response) {

            if (response.success) {
                const drAmt = response.drAmt || 0;
                const crAmt = response.crAmt || 0;
                const osAmount = drAmt - crAmt;

                console.log('Debit Amount:', drAmt);
                console.log('Credit Amount:', crAmt);
                console.log('Outstanding Amount:', osAmount);
                $('#NumOSAmount').val(osAmount.toFixed(2));

                (drAmt > crAmt) ? $('#lblDrCr').text("O/S Amount (Dr)") : $('#lblDrCr').text("O/S Amount (Cr)");
            }
            else {
                console.error(response.message);
                $('#NumOSAmount').val('0');
            }
        })
        .fail(function (xhr) {
            console.error('Error getting outstanding amount:', xhr);
            $('#NumOSAmount').val('0');
        });
}

//=============Save & Update===========
function CollectHeaderData() {

    const model = {
        V_NO: parseInt($('#NumDocNo').val()) || null,
        V_DATE: $('#DtDocDate').val() || null,

        PARTY_CODE: parseInt($('#ddlPartyName').val()) || null,
        GR_CODE: parseInt($('#txtGroupCode').val()) || null,

        CR_LIMIT: parseFloat($('#NumNewCreditLimitAmount').val()) || 0,
        CR_DAYS: parseInt($('#NumNewInsCreditLimitDays').val()) || 0,

        EFF_FROM: null,

        REMARKS: $('#txtremarks').val() || null,

        OURCR_DAYS: parseInt($('#NumNewOurCreditLimitDays').val()) || 0,

        OURAPPROVAL_TYPE: $('#ddlOurApproval').val() || null,
        APPROVAL_TYPE: $('#ddlApprovalType').val() || null,

        ACTION: (rowId && !isNaN(rowId)) ? "UPDATE" : "INSERT"
    };

    return model;
}
function SaveSalesCreditLimit() {
    const request = CollectHeaderData();
    console.log("Request Data For Save: ", request);

    $.ajax({
        url: '/SalesCreditLimitEntry/SaveSalesCreditLimit',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (res) {
            if (res.success) {
                showToast(res.message, { type: "success" });
                setTimeout(() => window.location.href = '/SalesCreditLimitEntry/Index?id=' + encodeURIComponent($('#NumDocNo').val()) + '&readOnly=true', 1500);
                if (isReadOnly) {
                    const vNo = $('#NumDocNo').val();
                    checkApprovalStatus(DocType, vNo, DBTableName);
                }
            }
            else {
                showToast(res.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            console.error(xhr);
            showToast("An error occurred while saving!", { type: "error" });
        }
    })
}

//=============Validations============

async function ValidateSalesCreditLimit() {
    if (!validateRequiredField('#NumDocNo', 'Doc No.') || !validateRequiredField('#txtremarks', 'Remarks'))
    {
        return false;
    };

    const crLimitAmt = parseFloat($('#NumNewCreditLimitAmount').val()) || 0;
    const crLimitDays = parseInt($('#NumNewInsCreditLimitDays').val()) || 0;
    const ourCrLimitDays = parseInt($('#NumNewOurCreditLimitDays').val()) || 0;
    // Credit Limit Amount

    if (crLimitAmt <= 0) {
        setInvalid($('#NumNewCreditLimitAmount'), 'Credit Limit amount required and should be greater than 0.');
        return false;
    }

    // Credit Limit Days
    if (crLimitDays <= 0) {
        setInvalid($('#NumNewInsCreditLimitDays'), 'Credit Limit Days required and should be greater than 0.');
        //return false;
    }

    // Our Credit Limit Days
    if (crLimitDays > 0 && ourCrLimitDays <= 0) {
        setInvalid($('#NumNewOurCreditLimitDays'), 'Our Credit Limit Days required and should be greater than 0.');
        //return false;
    }

    // Validate VDate
    const isValidVDate = await checkValidDate();
    if (!isValidVDate) return false;

    return true;
}
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/SalesCreditLimitEntry/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;
    } catch (error) {
        console.error("Error:", error);
        return false;
    }
}

//=============EDIT & VIEW ============
async function GetDataById() {
    try {
        isLoadByEdit = true;
        const res = await $.get(`/SalesCreditLimitEntry/GetDataById?vNo=${rowId}`);

        if (!res.success)
            showToast(res.message, { type: "warning" });
        console.log("Edit Data: ", res);
        await FillFormData(res.data);
    }
    catch (error) {
        console.error(error);
        showToast("Error in fetching data by Id.", { type: "error" });
    }
    finally {
        isLoadByEdit = false;
    }
}
async function FillFormData(data) {

    if (!data) {
        return;
    }

    $("#NumDocNo").val(data.v_NO ?? "");
    if (data.V_DATE) {
        $('#DtDocDate').val(data.v_DATE.substring(0, 10));
    }
    $("#ddlPartyName").val(data.partY_CODE ?? "").trigger("change");
    $("#txtGroupCode").val(data.gR_CODE ?? "");
    $("#txtGroupName").val(data.gR_NAME ?? "");
    $("#ddlApprovalType").val(data.approvaL_TYPE ?? "");
    $("#ddlOurApproval").val(data.ourapprovaL_TYPE ?? "");
    $("#NumNewCreditLimitAmount").val(data.cR_LIMIT ?? 0);
    $("#NumNewInsCreditLimitDays").val(data.cR_DAYS ?? 0);
    $("#NumNewOurCreditLimitDays").val(data.ourcR_DAYS ?? 0);
    $("#txtremarks").val(data.remarks ?? "");

    if (data.isFinalUser) {
        $("#btnShowMail").show();
    }

    if (isReadOnly) {
        SetFormReadOnly();
    }
}
function SetFormReadOnly() {
    const form = $('#SalesCreditLimitform');
    form.addClass('erppage-readonly');
    $('#btn_save').hide();
}

//=============Approval================
$(document).on('click', '#btn_Sendapproval', function () {
    var FromName = window.location.pathname.split('/')[1];
    $.ajax({
        url: '/Approval/CheckPendingUser',
        type: 'POST',
        data: {
            vNo: $('#NumDocNo').val(),
            vType: DocType
        },
        success: function (response) {
            console.log('Response:', response);
            // Pending with another user
            if (response.success === false) {
                showToast(`Pending With Another User : ${response.fullName} (${response.userCode})`,
                    { type: "warning" });
                return;
            }
            // Approval_Code = 5
            if (response.approvalCode8 === true) {
                OpenApprovalModal({
                    DocType: DocType,
                    DocNo: $('#NumDocNo').val(),
                    TableName: DBTableName
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: DocType,
                DocNo: $('#NumDocNo').val(),
                UserCode: null,
                UserName: null,
                DocDate: null,
                TableName: DBTableName,
                FromName, FromName
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
        DocType: DocType,
        DocNo: $('#NumDocNo').val(),
        TableName: DBTableName
    });
});
