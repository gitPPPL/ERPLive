
let poList = [];

$(document).ready(async function () {

    wireEvents();
    SetCurrentDate();
    await GetDocTypeDDl();
    await BindAllDropdown();

    if ($("#tblImportPaymentEntry tbody tr").length === 0) {
        addNewRow();
    }

});

async function wireEvents() {

    $("#ddldoctype").trigger("focus");

    $("#ddldoctype").on("focus", function () {
        $(this).prop("disabled", true);
    });

    //===========================
    // Get Our Bank Details 
    //===========================
    $('#ddlourbank').on('change', async function () {

        const bankCode = $(this).val();

        if (!bankCode) {
            $('#txtOurActNo').val('');
            $('#txtOurSwift').val('');
            $('#txtADCode').val('');
            return;
        }

        const response = await fetch(`/ImportPaymentEntry/GetOurBankDetails?bankCode=${bankCode}`);
        const data = await response.json();
        console.log("Bank details fetched:", data);
        $('#Numacno').val(data.accountNo);
        $('#Txtourshift').val(data.swiftCode);
        $('#Txtadcode').val(data.adCode);
    });

    //================================
    // Get Beneficiary Bank Details
    //================================
    $('#ddlbeneficiarybank').on('change', async function () {

        const bankCode = $(this).val();
        const vType = $('#ddldoctype').val();

        try {

            const response = await fetch(`/ImportPaymentEntry/GetBeneficiaryBankDetails?bankCode=${bankCode}&vType=${encodeURIComponent(vType)}`);

            if (!response.ok) {
                throw new Error("Failed to load beneficiary bank details.");
            }

            const data = await response.json();

            console.log("Beneficiary Bank Details :", data);

            $('#Numbeneficiaryacno').val(data.accountNo);
            $('#Txtbeneficiaryaba').val(data.aba);
            $('#Txtbeneficiaryrouting').val(data.routingNo);
            $('#NumbeneficiaryCode').val(data.sortCode);
            $('#Txtbeneficiaryshift').val(data.swiftCode);
            $('#Txtbeneficiarybankaddress').val(data.bankAdd);

        }
        catch (error) {
            console.error(error);
        }
    });

    //================================
    // Correspondence Bank Details
    //================================
    $('#ddlcorrespondancebank').on('change', async function () {

        const bankCode = $(this).val();
        const vType = $('#ddldoctype').val();

        const response = await fetch(`/ImportPaymentEntry/GetBeneficiaryBankDetails?bankCode=${bankCode}&vType=${encodeURIComponent(vType)}`);
        const data = await response.json();

        $('#Numcorrespondanceacno').val(data.accountNo);
        $('#Txtcorrespondanceaba').val(data.aba);
        $('#Txtcorrespondancerouting').val(data.routingNo);
        $('#NumcorrespondanceCode').val(data.sortCode);
        $('#Txtcorrespondanceshift').val(data.swiftCode);
        $('#Txtcorrespondancebankaddress').val(data.bankAdd);
    });

    //===========================
    // Party Change
    //===========================
    $('#txtSupplierName').on('autocompleteselect', async function (event, ui) {

        console.log("autocompleteselect fired");
        console.log(ui.item);
        const partyCode = ui.item.code;

        if (!partyCode) return;

        await GetPartyDetails(partyCode);
        await FillPartyDetailsInPartB();
        await LoadPODropdown(partyCode);

    });

    //========================================
    //  Add and Delete Row(Footer Table)
    //========================================
    $('#tblImportPaymentEntry').on('click', '.add', function () {
        addNewRow();
    });

    $('#tblImportPaymentEntry').on('click', '.delete', function () {
        const totalRows = $('#tblImportPaymentEntry tbody tr').length;

        if (totalRows === 1) {
            showToast("At least one row is required.", { type: "warning" });
            return;
        }

        $(this).closest('tr').remove();
    });

}

function SetCurrentDate() {

    const today = new Date().toISOString().split("T")[0];
    $("#Dtdocdate").val(today);

}

async function BindAllDropdown() {
    await Promise.all([

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#txtSupplierName', '-- Select Supplier --'),

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#ddlBeeficiaryname', '-- Select Beneficiary --', '#hdnBeneficiaryCode'),

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#ddlLender', '-- Select Supplier/Lender --', '#hdnLenderCode'),

        bindDropdown('ImportPaymentEntry', 'OurBank', '#ddlourbank', '-- Select Bank Type --' , null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlbeneficiarybank', '-- Select Beneficiary Bank --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Currency', '#ddlcurrency', '-- Select Currency --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlcorrespondancebank', '-- Select Correspondance bank --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlSPFCBank', '-- Select SPFC Bank --', null, null, false, null, true),

    ]);
}

async function GetDocTypeDDl() {

    try {

        const res = await fetch("/ImportPaymentEntry/DocType", {
            method: "GET",
        });

        const data = await res.json();

        const ddl = $("#ddldoctype");
        ddl.empty();

        $.each(data, function (i, item) {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        await GetVNo();

    }catch (error) {
        showToast("Doc Type Load Failed", { type: "error" });
        console.error("Error fetching DocType dropdown data:", error);
    }

}

async function GetVNo() {

    try {
        const vType = $('#ddldoctype').val();
        if (!vType) {
            console.warn("vType is empty");
            return;
        }
        const res = await fetch(`/ImportPaymentEntry/GenerateVNo?vType=${encodeURIComponent(vType)}`);

        if (!res.ok) {
            throw new Error("Network response was not ok");
        }
        const data = await res.json();
        if (data.v_NO) {
            $('#Numdocno').val(data.v_NO);
            const docId = vType + data.v_NO;
        } else {
            console.warn("V_NO not found in response");
        }

    } catch (e) {
        console.error("Error in GetVNo:", e);
    }
}

async function GetPartyDetails(partyCode) {

    try {

        const response = await fetch(`/ImportPaymentEntry/GetPartyDetails?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load party details.");
        }

        const data = await response.json();

        console.log("Party Details :", data);

        //==================================
        // Part B
        //==================================
        $('#ddlLender').val(data.ecbLenderCode);
        $('#hdnLenderCode').val(data.ecbLenderCode);
        $('#txtECBLenderAdd').val(data.ecbAddress);

        //==================================
        // Beneficiary
        //==================================
        $('#ddlBeeficiaryname').val(data.beneficiaryName);
        $('#hdnBeneficiaryCode').val(data.beneficiaryCode);
        $('#txtBeneficiaryActNo').val(data.beneficiaryActNo);
        $('#txtBeneficiaryBankAdd').val(data.beneficiaryBankAddress);

        //==================================
        // Last Import
        //==================================
        $('#ddlImportFor').val(data.importCategory);
        $('#ddlImportRemit').val(data.importRemit);
        $('#ddlPayType').val(data.payType);
        $('#ddlForeignBankCharge').val(data.foreignBankCharge);
        $('#ddlInterestApplicable').val(data.interestApplicable);
        $('#txtROI').val(data.roi);
        $('#txtROIPeriod').val(data.roiPeriod);

        //==================================
        // Bank Details
        //==================================
        $('#ddlbeneficiarybank').val(data.beneficiaryBankCode).trigger('change');
        $('#txtBeneficiarySwift').val(data.beneficiarySwift);
        $('#txtBeneficiaryActNo').val(data.beneficiaryAccount);
        $('#ddlcorrespondancebank').val(data.corrBankCode).trigger('change');
        $('#txtCorrSwift').val(data.corrSwift);
        $('#txtCorrActNo').val(data.corrAccount);

    }
    catch (error) {

        console.error("Error loading party details:", error);
        showToast("Failed to load party details.", { type: "error" });
        
    }
}

async function FillPartyDetailsInPartB() {

    const partyCode = $('#hdnSupplierCode').val();

    if (!partyCode) return;

    try {

        const response = await fetch(`/ImportPaymentEntry/GetPartyDetailsForPartB?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load Party Details.");
        }

        const data = await response.json();
        console.log("FillPartyDetailsInPartB", data);
        $('#ddlLender').val(data.name);
        $('#textaddresslender').val(data.address);

    }
    catch (error) {
        console.error(error);
    }
}

//================================================
//      Footer Table
//================================================

function addNewRow() {

    const tbody = $("#tblImportPaymentEntry tbody");

    const row = `
        <tr>

            <td class="hidden-col">
                <input type="hidden" class="erppagetable-control code">
            </td>

            <td> <select class="erppagetable-control ddlPoNo">
                </select>
            </td>

            <td><input type="date" class="erppagetable-control podate"></td>

            <td><input type="text" class="erppagetable-control invoiceno"></td>

            <td><input type="date" class="erppagetable-control invoicedate"></td>

            <td><input type="number" class="erppagetable-control amount text-end"></td>

            <td><input type="number" class="erppagetable-control quantity text-end"></td>

            <td>
                <select class="erppagetable-control itemname">
                    <option value="">--Select--</option>
                </select>
            </td>

            <td><input type="text" class="erppagetable-control itemdesc"></td>

            <td><input type="text" class="erppagetable-control hsncode"></td>
                                          
            <td><input type="text" class="erppagetable-control country"></td>

            <td>
                <select class="erppagetable-control shipmentmode">
                    <option value="">--Select--</option>
                    <option>Air</option>
                    <option>Sea</option>
                    <option>Road</option>
                </select>
            </td>

            <td><input type="date" class="erppagetable-control shipmentdate"></td>
                                         
            <td><input type="date" class="erppagetable-control dispatchdate"></td>
                                          
            <td><input type="text" class="erppagetable-control shippingcompany"></td>
                                         
            <td><input type="text" class="erppagetable-control portdispatch"></td>
                                         
            <td><input type="text" class="erppagetable-control destinationport"></td>
                                         
            <td><input type="text" class="erppagetable-control blno"></td>
                                       
            <td><input type="date" class="erppagetable-control bldate"></td>
                                        
            <td><input type="text" class="erppagetable-control beno"></td>

            <td><input type="date" class="erppagetable-control bedate"></td>

            <td>
                <select class="erppagetable-control beccy">
                    <option value="">--Select--</option>
                </select>
            </td>

            <td><input type="number" class="erppagetable-control beamount text-end"></td>
                                            
            <td><input type="number" class="erppagetable-control beutilized text-end"></td>
                                            
            <td><input type="number" class="erppagetable-control fobvalue text-end"></td>

            <td class="action-col">
                <div class="action-wrap">
                     <button class="act-btn add" title="Add Row"><i class="fa fa-plus"></i></button>
                     <button class="act-btn delete"><i class="fa fa-trash"></i></button>
                </div>
            </td>

        </tr>
    `;

    tbody.append(row);
    const ddl = tbody.find("tr:last .ddlPoNo");

    ddl.append('<option value="">-- Select PO No --</option>');
        $.each(poList, function (_, item) {
            ddl.append(`
            <option value="${item.vNo}">
                ${item.vType}${item.vNo}
            </option>
        `);

    });

}

async function LoadPODropdown(partyCode) {

    try {

        const response = await fetch(`/ImportPaymentEntry/GetItemMaster?partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error("Failed to load PO.");
        }

        poList = await response.json();

        console.log("Po List Dropdown", poList);
        $('.ddlPoNo').each(function () {

            const ddl = $(this);

            ddl.empty().append('<option value="">-- Select PO No --</option>');

            $.each(poList, function (_, item) {

                ddl.append(`
                    <option value="${item.vNo}">
                        ${item.vType}${item.vNo}
                    </option>
                `);

            });

        });

    } catch (e) {
        console.error(e);
    }
}