
const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('id');
const vtype = urlParams.get('vType');
let isReadOnly = urlParams.get('readOnly') === 'true';
let isEditMode = !!(id && vtype);

$(document).ready(async function () {

    await wireEvents();
    await BindInsuranceTypeDDL();
    await BindBillTypeDDL();

    if (id && vtype) {
        await LoadEditData();
    } else {
        SetCurrentDate();
        await GetVNo();
    }

    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
        await SaveAndUpdateData();
    });

});

async function wireEvents() {

    SetCurrentDate();

    //==========================
    // Set Value for bills
    //==========================
  
    $('#ddlBillType').on('change', async function () {

        $('#txtFromBillNo').hide();
        $('#txtToBillNo').hide();

        $('#ddlFromBillNo').show();
        $('#ddlToBillNo').show();

        await BindFromBillNoDDL($(this).val());
        await BindToBillNoDDL($(this).val());
    });

    //=================================
    //Set value for dates and amount
    //=================================
    $('#ddlInsuranceType').on('change', async function () {

        const selected = $(this).find(':selected');

        if (!selected.val()) {
            $('#DtPolicyIssueDate').val('');
            $('#DtPolicyExpireDate').val('');
            $('#TxtPolicyAmount').text('0.00');
            return;
        }

        const insuCode = selected.val();

        $('#DtPolicyIssueDate').val(
            formatDateForInput(selected.attr('data-eff-date'))
        );

        $('#DtPolicyExpireDate').val(
            formatDateForInput(selected.attr('data-exp-date'))
        );

        await GetPolicyBalanceAmount(insuCode);
    });
}

function SetCurrentDate() {
    const today = new Date();

    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');

    $('#DtDocumentDate').val(`${year}-${month}-${day}`);
}

async function GetVNo() {
    try {
        const response = await fetch('/InsuranceCoverNote/GenerateVNo');

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            return;
        }
        $('#NumDocNo').val(data.v_NO);

        const docId = 'INSU' + $('#NumDocNo').val();
        console.log("docId", docId);

    } catch (error) {
        console.error('Error generating VNo:', error);
    }
}

async function BindInsuranceTypeDDL() {
    try {

        const response = await fetch('/InsuranceCoverNote/InsuranceTypeDDL');

        if (!response.ok) {
            throw new Error('Failed to load Insurance Type');
        }

        const data = await response.json();

        const ddl = $('#ddlInsuranceType');

        ddl.empty();

        ddl.append($('<option>', {
            value: '',
            text: 'Select Insurance Type'
        }));

        data.forEach(item => {

            ddl.append($('<option>', {
                value: item.value,
                text: item.text,
                'data-policy-amt': item.policyAmt,
                'data-eff-date': item.effDate,
                'data-exp-date': item.expDate
            }));

        });

    } catch (error) {

        console.error('Error loading Insurance Type:', error);

        showToast("Error Loading Insurance Type", {
            type: "error"
        });

    }
}

async function BindBillTypeDDL() {
    try {

        const response = await fetch('/InsuranceCoverNote/BillTypeDDL');

        if (!response.ok) {
            throw new Error('Failed to load Bill Type');
        }

        const data = await response.json();

        const ddl = $('#ddlBillType');

        ddl.empty();

        ddl.append($('<option>', { value: '', text: 'Select Bill Type' }));

        data.forEach(item => {

            ddl.append($('<option>', { value: item.value, text: item.text }));

        });

    } catch (error) {

        console.error('Error loading Bill Type:', error);

        showToast("Error Loading Bill Type", { type: "error" });

    }

}

async function BindFromBillNoDDL(vType) {
    const ddl = $('#ddlFromBillNo');

    ddl.empty();
    ddl.append($('<option>', {value: '', text: 'Select From Bill'}));

    if (!vType) {
        return;
    }

    try {
        const response = await fetch(`/InsuranceCoverNote/FromBillNoDDL?vType=${encodeURIComponent(vType)}`);

        if (!response.ok) {
            throw new Error('Failed to load From Bill No');
        }

        const data = await response.json();
        console.log("FromBillNo:", data);
        data.forEach(item => {
            ddl.append($('<option>', {
                value: item.value,
                text: item.text
            }));
        });

    } catch (error) {
        console.error('Error loading From Bill No:', error);
        showToast("Error Loading From Bill No", { type: "error" });
    }
}

async function BindToBillNoDDL(vType) {
    const ddl = $('#ddlToBillNo');

    ddl.empty();
    ddl.append($('<option>', {value: '', text: 'Select To Bill'}));

    if (!vType) {
        return;
    }

    try {
        const response = await fetch(`/InsuranceCoverNote/FromBillNoDDL?vType=${encodeURIComponent(vType)}`);

        if (!response.ok) {
            throw new Error('Failed to load To Bill No');
        }

        const data = await response.json();
        console.log("ToBillNo:", data);
        data.forEach(item => {
            ddl.append($('<option>', {
                value: item.value,
                text: item.text
            }));
        });

    } catch (error) {
        console.error('Error loading To Bill No:', error);
        showToast("Error Loading To Bill No", { type: "error" });
    }
}

async function GetPolicyBalanceAmount(insuCode) {

    try {

        const response = await fetch(
            `/InsuranceCoverNote/GetPolicyBalanceAmount?insuCode=${encodeURIComponent(insuCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load Policy Balance Amount');
        }

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            $('#TxtPolicyAmount').text('0.00');
            return;
        }

        $('#TxtPolicyAmount').text(data.policyBalance || '0.00');

    } catch (error) {
        console.error('Error loading Policy Balance Amount:', error);
        $('#TxtPolicyAmount').text('0.00');
        showToast("Error Loading Policy Balance Amount", {type: "error"});
    }
}

function formatDateForInput(dateValue) {

    if (!dateValue) {
        return '';
    }

    return dateValue.toString().substring(0, 10);
}

async function SaveAndUpdateData() {

    const vNo = $('#NumDocNo').val();
    const vDate = $('#DtDocumentDate').val();
    const insuCode = $('#ddlInsuranceType').val();
    const billType = $('#ddlBillType').val();
    const fromBillNo = isEditMode ? $('#txtFromBillNo').val() : $('#ddlFromBillNo').val();
    const toBillNo = isEditMode ? $('#txtToBillNo').val() : $('#ddlToBillNo').val();

    const isValidDate = await checkValidDate();
    if (!isValidDate) return;

    const model = {
        DocId: 'INSU' + vNo,
        InsuCode: Number(insuCode),
        VType: 'INSU',
        VNo: Number(vNo),
        VDate: vDate,
        BillType: billType,
        FBillNo: Number(fromBillNo),
        TBillNo: Number(toBillNo),
        Srno: null
    };

    try {
        $('#btn-save').prop('disabled', true);

        const response = await fetch('/InsuranceCoverNote/SaveAndUpdateData', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(model)
        });

        if (!response.ok) {
            throw new Error('Failed to save Insurance Cover Note.');
        }

        const result = await response.json();

        if (result.success) {
            showToast(result.message, { type: "success" });

            $('#TxtDocId').val(model.DocId);
            isReadOnly = true;
            SetReadOnlyMode();

        } else {
            showToast(result.message || "Failed to save data.", {
                type: "error"
            });
        }

    } catch (error) {
        console.error('Save error:', error);
        showToast(error.message, { type: "error" });

    } finally {
        $('#btn-save').prop('disabled', false);
    }
}

async function LoadEditData() {
    try {
        const response = await fetch(
            `/InsuranceCoverNote/LoadEditData?vNo=${encodeURIComponent(id)}&vType=${encodeURIComponent(vtype)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load Insurance Cover Note.');
        }

        const data = await response.json();

        console.log("Edit Data:", data);

        if (data.success === false) {
            showToast(data.message || "Record not found.", { type: "error" });
            return;
        }

        $('#NumDocNo').val(data.V_NO);
        $('#TxtDocId').val(data.DOC_ID);
        $('#DtDocumentDate').val(formatDateForInput(data.V_DATE));

        $('#ddlInsuranceType').val(String(data.INSU_CODE)).trigger('change');
        $('#ddlBillType').val(data.BILL_TYPE);
        $('#ddlFromBillNo').hide();
        $('#ddlToBillNo').hide();

        $('#txtFromBillNo').val(data.F_BILL_NO).show();

        $('#txtToBillNo').val(data.T_BILL_NO).show();

        console.log("Selected From:", data.F_BILL_NO);
        console.log("Selected To:", data.T_BILL_NO);

        if (isReadOnly) {
            SetReadOnlyMode();
        }

    } catch (error) {
        console.error('Load edit error:', error);
        showToast(error.message, { type: "error" });
    }
}

function SetReadOnlyMode() {

    $('#policyAmountRow').hide();
    $('#btn-save').hide();

    $('#InsuranceCoverNoteform').find('input, select').prop('disabled', true);
}

async function checkValidDate() {

    const data = {
        vdate: $("#DtDocumentDate").val(),
        vtype: "INSU",
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/InsuranceCoverNote/CheckValidDate', {
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
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

//========================
// REPORT
//========================
async function InsuranceEntry() {

    var reportName = "INSURANCE";

    var vType = "INSU";
    var vNo = $('#NumDocNo').val();

    var fBillNo = $('#ddlFromBillNo').val();
    var tBillNo = $('#ddlToBillNo').val();
    var billType = $('#ddlBillType').val();

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", { type: "error" });
        return;
    }

    // =========================
    // GET ITEM NAME
    // =========================

    var itemName = "";

    try {

        const response = await $.ajax({
            url: '/InsuranceCoverNote/GetInsuranceItemName',
            type: 'GET',
            data: {
                fBillNo: fBillNo,
                tBillNo: tBillNo,
                billType: billType
            }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });
            return;
        }

        itemName = response.itemName || "";

    } catch (error) {

        console.error("Error getting insurance item name:", error);
        showToast("Unable to get item name.", { type: "error" });
        return;
    }

    // =========================
    // SELECTION FORMULA
    // =========================

    var SelForMul =
        "{SALE3.V_TYPE}='" + vType + "'" +
        " AND {SALE3.V_NO}=" + vNo +
        " AND {SALE3.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {SALE3.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {SALE3.BRANCH_CODE}=" + window.globalVariables.branchCode;

    // =========================
    // REPORT DATA
    // =========================

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,

            comp_phone: "Mobile : " + window.globalVariables.compPhone,

            INSU: itemName,

            INSU_AMT: "0"
        }
    };

    console.log("Insurance Report Data:", formulaFields);
    console.log("Selection Formula:", SelForMul);

    // =========================
    // TIMESTAMP
    // =========================

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp =
        `${day}${month}${year}_${hours}${minutes}${seconds}`;

    // =========================
    // GENERATE REPORT
    // =========================

    $.ajax({

        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',

        data: JSON.stringify(formulaFields),

        contentType: "application/json",

        xhrFields: {
            responseType: 'blob'
        },

        success: function (response) {

            var file = new Blob(
                [response],
                { type: 'application/pdf' }
            );

            var fileName =
                `${reportName}_${timestamp}.pdf`;

            var link =
                document.createElement('a');

            link.href =
                URL.createObjectURL(file);

            link.download =
                fileName;

            document.body.appendChild(link);

            link.click();

            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {

            console.error(
                'Error generating report:',
                error
            );

        }
    });
}

