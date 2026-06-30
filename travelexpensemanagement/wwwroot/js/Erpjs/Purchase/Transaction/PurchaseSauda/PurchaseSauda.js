async function fetchDDlParty(PartyId) {
    try {
        const response = await fetch(`/PurchaseSauda/GetDataByPartyCode?PartyId=${PartyId}`);
        const data = await response.json();
        console.log("data", data);
        $('#txtAddress1').val(data?.supplier?.address1 || '');
        $('#txtAddress2').val(data?.supplier?.address2 || '');
        $('#txtAddress3').val(data?.supplier?.address3 || '');
        $('#txtContactNo').val(data?.supplier?.mobile || '');
        $('#txtStation').val(data?.supplier?.cityCode || '');
        $('#txtCountry').val(data?.supplier?.country || '');
        $('#txtDeliveryFrom').val(data?.supplier?.name || '');
        $('#ddlShipFrom').val(data?.supplier?.code || '').trigger('change');

        $('#ddlPaymentTerm').val(data?.partermcode || '');
        $('#ddlFreightTerm').val(data?.sauda?.frtTerm || '');
        $('#txtDeliveryTerm').val(data?.sauda?.delTerm || '');
        $('#ddlItemName').val(data?.sauda?.itemCode || '');
        $('#ddlItemType').val(data?.sauda?.itemType || '');

    } catch (error) {
        console.error('Failed to fetch party data:', error);
    }
}

async function DDLPartyMast() {
    const res = await fetch("/PurchaseSauda/DDLPartyMast");
    const list = await res.json();
    const ddl = $("#ddlPartyName");

    ddl.empty().append('<option value="">Select Party Name</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Party Name --",
        allowClear: true,
        width: '100%'
    });

}

async function DDLCityMast() {
    try {
        const res = await fetch('/PurchaseSauda/DDLCityMast');
        const data = await res.json();
        const ddl = $('#txtStation');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function DDLstatus() {
    try {
        const res = await fetch('/PurchaseSauda/DDLstatus');
        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Status:", error);
    }
}

async function DDLShipFrom() {
    const res = await fetch("/PurchaseSauda/DDLShipFrom");
    const list = await res.json();
    const ddl = $("#ddlShipFrom");

    ddl.empty().append('<option value="">Select Ship From</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Ship From--",
        allowClear: true,
        width: '100%'
    });

}

async function ddlDeliveryFrom() {
    try {
        const res = await fetch('/PurchaseSauda/DDLShipFrom');
        const data = await res.json();
        const ddl = $('#ddlDeliveryFrom');
        ddl.empty().append('<option value="">-- Select Delivery From --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Delivery From:", error);
    }
}

async function DDLPurchaseThrough() {
    try {
        const res = await fetch('/PurchaseSauda/DDLPurchaseThrough');
        const data = await res.json();

        const ddl = $('#ddlPurchaseThrough');
        ddl.empty().append('<option value="">-- Select Purchase Through --</option>');

        if (Array.isArray(data)) {
            data.forEach(item => {
                ddl.append(`<option value="${item.value}">${item.value} - ${item.text}</option>`);
            });
        } else {
            console.warn('Unexpected data format:', data);
        }

    } catch (error) {
        console.error("Error loading Purchase Through:", error);
        toastr.error("Failed to load Purchase Through options.");
    }
}

async function DDLItemMaster() {
    const res = await fetch("/PurchaseSauda/DDLItemMaster");
    const list = await res.json();
    const ddl = $("#ddlItemName");

    ddl.empty().append('<option value="">Select Item Name</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Item Name --",
        allowClear: true,
        width: '100%'
    });




}

async function DDLTaxRate() {
    try {
        const res = await fetch('/PurchaseSauda/DDLTaxRate');
        const data = await res.json();
        const ddl = $('#ddlTaxRate');
        ddl.empty().append('<option value="">-- Select Tax Rate --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Tax Rate:", error);
    }
}

async function DDLPaymentTerm() {
    try {
        const res = await fetch('/PurchaseSauda/DDLPaymentTerm');
        const data = await res.json();
        const ddl = $('#ddlPaymentTerm');
        ddl.empty().append('<option value="">-- Select Payment Term --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Payment Term:", error);
    }
}

async function DDLBrokerName() {
    try {
        const res = await fetch('/PurchaseSauda/DDLBrokerName');
        const data = await res.json();
        const ddl = $('#ddlBrokerName');
        ddl.empty().append('<option value="">-- Select Broker Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Broker Name:", error);
    }
}

async function DDLDispatchForm() {
    try {
        const res = await fetch('/PurchaseSauda/DDLDispatchForm');
        const data = await res.json();
        const ddl = $('#ddlDispatchFrom');
        ddl.empty().append('<option value="">-- Select Dispatch From --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Dispatch Form:", error);
    }
}





async function GetVNo() {
    try {
        const res = await fetch('/PurchaseSauda/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#txtDocNo').val(data.v_NO);
        $('#TxtDispatchDocNo').val(data.v_NO);

    } catch (e) {
        console.error('GetVNo failed:', e);
        toastr.error('Error loading Document Number: ' + e.message);
    }
}




function setFormReadOnly() {
    const form = $('#PurchaseSaudaForm');
    form.find('input, select, textarea, button').prop('disabled', true);
    form.find('textarea').css('background-color', '#f0f0f0');
    form.find('table tbody tr').each(function () {
        $(this).find('input, select, textarea').prop('disabled', true);
        $(this).css('background-color', '#f9f9f9');
    });
    form.find('.btn-save').hide();
    const v_no = $('#TxtCode').val();
    GetFinalUser(v_no);
}




async function checkValidDate() {
    const data = {
        vdate: $("#dtDocDate").val(),
        vtype: 'PAUD',
        vno: $("#txtDocNo").val()
    };
    try {
        const response = await fetch('/PurchaseSauda/CheckValidDate', {
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
        showToast(result.message, { type: "warning" });
        return false;
    }
}




function recalculateNetRate() {
    const baseRate = parseFloat($('#numRate').val()) || 0;
    const discountPercent = parseFloat($('#numDiscount').val()) || 0;
    let taxRate = 0;
    const selectedText = $('#ddlTaxRate').find('option:selected').text();
    const taxMatch = selectedText.match(/\d+(\.\d+)?/);
    if (taxMatch) {
        taxRate = parseFloat(taxMatch[0]);
    }
    const discountAmount = baseRate * discountPercent / 100;
    const rateAfterDiscount = baseRate - discountAmount;
    const taxAmount = rateAfterDiscount * taxRate / 100;
    const roundedTax = Math.round(taxAmount);
    const finalNetRate = rateAfterDiscount + taxAmount;
    $('#numNetRate').val(finalNetRate.toFixed(2));
    $('#numTaxRate').val(taxRate.toFixed(2));
}

function formatDate(dateStr) {
    if (!dateStr) return null;

    const date = new Date(dateStr);
    if (isNaN(date)) return null;

    // Extract local date parts
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0'); // Months 0-11
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
}




async function LoadDropDown() {
    try {
        await Promise.all([
            DDLShipFrom(),
            DDLPurchaseThrough(),
            DDLTaxRate(),
            DDLPaymentTerm(),
            DDLBrokerName(),
            DDLDispatchForm(),
            DDLPartyMast(),
            DDLItemMaster(),
            ddlDeliveryFrom(),
            DDLCityMast(),
            DDLstatus()
        ]);
    } catch (error) {
        console.error("Error loading dropdowns:", error);
    }
}


async function GetFinalUser(v_no) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/FinalUser',
            type: 'GET',
            data: { v_no: v_no },
            dataType: 'json'
        });

        console.log("dd", res);

        if (res.cretePurchaseorder != "Approved") {
            $('#btn_CreatePurchaseOrder').prop('disabled', true);
        }

        if (res.finalUser && res.finalUser.toUpperCase() === "FINAL" || mode === "view") {
            $("#btn_ModificationOrder").show();
            $("#modification_count").show().text("Modification(" + (res.modificationcount || 0) + ")");
            $('#btn_ModificationOrder').prop('disabled', false);
        } else {
            $("#btn_ModificationOrder").hide();
            $("#modification_count").hide().text("");
        }

    } catch (error) {
        console.error("Error fetching payment term:", error);
    }
}

async function CheackSendMail() {

    if (!rowId) {
        showToast(`Please save the data before Send Mail.`, { type: "info" });
        return;
    }
    const v_no = parseInt($('#txtDocNo').val()) || 0;

    const res = await $.ajax({
        url: '/PurchaseSauda/CheackMail',
        type: 'GET',
        data: { v_no: v_no },
        dataType: 'json'
    });

    if (res.status == false) {
        toastr.warning(res.message);
        return;
    }

    Swal.fire({
        title: "Do you want to send mail?",
        icon: "question",
        showCancelButton: true,
        confirmButtonText: "Yes",
        cancelButtonText: "No"
    }).then((result) => {

        if (result.isConfirmed) {
            SendMail();
        }
    });
}

async function SendMail() {
    try {

        let PartyCode = $('#ddlPartyName').val();
        const vno = parseInt($('#txtDocNo').val()) || 0;

        // 🔥 STEP 1: GET REPORT FILE
        const report = await GetTransitReportFile();

        // STEP 2: SEND TO CONTROLLER
        let formData = new FormData();
        formData.append("PartyCode", PartyCode);
        formData.append("vno", vno);

        formData.append("file", report.file, report.fileName);

        const res = await $.ajax({
            url: '/PurchaseSauda/SendMail',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        });

        console.log("Mail response:", res);
        return res;

    } catch (error) {
        console.error("Error:", error);
    }
}
function GetTransitReportFile() {

    return new Promise((resolve, reject) => {

        if (!rowId) {
            showToast(`Please save the data before printing the report.`, { type: "info" });
            reject("No rowId");
            return;
        }

        var reportName = "SAUDA_PURCH";

        var v_no = $('#TxtCode').val();
        var v_type = "PAUD";

        var formula =
            "{SAUDA.COMP_CODE} = " + globalVars.CompCode +
            " and {SAUDA.YEAR_CODE} = " + globalVars.FYearCode +
            " and {SAUDA.BRANCH_CODE} = " + globalVars.BranchCode +
            " and {SAUDA.V_NO} = " + v_no +
            " and {SAUDA.V_TYPE} = '" + v_type + "'";

        var payload = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: database,
            Parameters: {
                comp_name: globalVars.CompanyName || "",
                comp_add1: globalVars.Address1 || "",
                comp_phone: globalVars.Phone || "",
                RPTNAME: "PURCHASE CONTRACT/ORDER"
            }
        };

        $.ajax({
            url: 'http://localhost:24085/Report/PendingQCReport',
            type: 'POST',
            data: JSON.stringify(payload),
            contentType: "application/json",
            xhrFields: { responseType: 'blob' },

            success: function (response) {

                // 🔥 return file instead of downloading
                const file = new Blob([response], { type: 'application/pdf' });

                const v_no = $('#TxtCode').val();
                const now = new Date();

                const timestamp =
                    String(now.getDate()).padStart(2, '0') +
                    String(now.getMonth() + 1).padStart(2, '0') +
                    String(now.getFullYear()).slice(-2) + "_" +
                    String(now.getHours()).padStart(2, '0') +
                    String(now.getMinutes()).padStart(2, '0') +
                    String(now.getSeconds()).padStart(2, '0');

                const fileName = `SAUDA_PURCH_${v_no}_${timestamp}.pdf`;

                resolve({ file, fileName });
            },

            error: function (xhr) {
                reject(xhr);
            }
        });
    });
}

function TransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "SAUDA_PURCH";

    var v_no = $('#TxtCode').val();
    var v_type = "PAUD";

    var formula =
        "{SAUDA.COMP_CODE} = " + globalVars.CompCode +
        " and {SAUDA.YEAR_CODE} = " + globalVars.FYearCode +
        " and {SAUDA.BRANCH_CODE} = " + globalVars.BranchCode +
        " and {SAUDA.V_NO} = " + v_no +
        " and {SAUDA.V_TYPE} = '" + v_type + "'";

    // Prepare the payload for the API
    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            COMP_GST: globalVars.GST || "",
            COMP_PAN: globalVars.PAN || "",
            COMP_CIN: globalVars.COMP_CIN || "",
            COMP_EMAIL: globalVars.Email || "",
            comp_phone: globalVars.Phone || "",
            RPTNAME: "PURCHASE CONTRACT/ORDER"
        }
    };

    // Timestamp for file name
    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport', // check port
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' }, // Important for PDF

        success: function (response) {
            // Convert response to a Blob
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            // Trigger download
            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {
            if (xhr.status === 0) {
                console.error("Cannot connect to API. Is the backend running?");
            } else {
                console.error('Error generating report:', xhr.status, xhr.statusText, error);
                xhr.responseText && console.error('Response:', xhr.responseText);
            }
        }
    });
}

async function paymentterm(partyCode, payTermCode) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/Paymentterm',
            type: 'GET',
            data: { partyCode: partyCode },
            dataType: 'json'
        });

        console.log("dd", res);

        if (String(res).trim() !== String(payTermCode ?? '').trim()) {
            toastr.info("Payment Term is not matching with Party Master, please check it");
        }

        return res;

    } catch (error) {
        console.error("Error fetching payment term:", error);
    }
}

async function GetTaxRate(taxrate) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/GetTaxRate',
            type: 'GET',
            data: { taxrate: taxrate },
            dataType: 'json'
        });

        console.log("dd", res);


        $('#numTaxRate').val(res);


        return res;

    } catch (error) {
        console.error("Error fetching payment term:", error);
    }
}

async function loadModificationdata() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseSaudaList/GetModificationData',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];

            if (!data || data.length === 0) {
                toastr.info("Purchase Modification Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseModification(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}
function renderPurchaseModification(data) {
    console.log("Rendering Purchase Modification Data:", data);
    const tbody = $('#modificationList tbody');
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`
            <tr>
                <td colspan="4" class="text-center">No data found</td>
            </tr>
        `);
    } else {
        data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.saudaNo ?? ''}</td>
                    <td>${item.party ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.rate ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.modifyDate ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('modificationModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}
