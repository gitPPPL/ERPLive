
$(document).ready(async function () {

    wireEvents();
    SetCurrentDate();
    await GetDocTypeDDl();
    await BindAllDropdown();

});

async function wireEvents() {

    $("#ddldoctype").trigger("focus");

    $("#ddldoctype").on("focus", function () {
        $(this).prop("disabled", true);
    });

    //===========================
    // Get Bank Details 
    //==========================
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
}

function SetCurrentDate() {

    const today = new Date().toISOString().split("T")[0];
    $("#Dtdocdate").val(today);

}

function InitializeDocumentAttachedDropdown() {

    InitializeERPFilterDropdown({

        id: "ddlDocumentAttached",

        placeholder: "Select Document(s)",

        data: [
            {
                id: "Proforma Invoice",
                text: "Proforma Invoice"
            },
            {
                id: "Purchase Order Raised",
                text: "Purchase Order Raised"
            },
            {
                id: "Transport Document",
                text: "Transport Document"
            },
            {
                id: "Copy of Bill Entry",
                text: "Copy of Bill Entry"
            },
            {
                id: "Form A2",
                text: "Form A2"
            },
            {
                id: "If Payment is being made",
                text: "If Payment is being made"
            }
        ],

        onChange: function (selectedItems) {

            console.log("Selected Documents :", selectedItems);

        }

    });

}

async function BindAllDropdown() {
    await Promise.all([

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#txtSupplierName', '-- Select Supplier --'),

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#ddlBeeficiaryname', '-- Select Beneficiary --', '#hdnBeneficiaryCode'),

        bindDropdownNew('ImportPaymentEntry', 'SupplierName', '#ddlLender', '-- Select Supplier/Lender --', '#hdnLenderCode'),

        bindDropdown('ImportPaymentEntry', 'OurBank', '#ddlourbank', '-- Select Bank Type --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlbeneficiarybank', '-- Select Beneficiary Bank --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Currency', '#ddlcurrency', '-- Select Currency --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlcorrespondancebank', '-- Select Correspondance bank --', null, null, false, null, true),

        bindDropdown('ImportPaymentEntry', 'Bank', '#ddlSPFCBank', '-- Select SPFC Bank --', null, null, false, null, true),

    ]);

    // Initialize Document Attached Multi Select Dropdown
    InitializeDocumentAttachedDropdown();
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

    } catch (error) {
        showToast("Doc Type Load Failed", { type: "error" });
        console.error("Error fetching DocType dropdown data:", error);
    }

}

//async function GetOurBankDetails(bankCode) {

//    try {
//        const response = await fetch(`/ImportPaymentEntry/GetOurBankDetails?bankCode=${bankCode}`);

//        if (!response.ok) {
//            throw new Error("Failed to load bank details.");
//        }

//        const data = await response.json();

//        console.log("Bank details fetched:", data);
//        $('#Numacno').val(data.accountNo);
//        $('#Txtourshift').val(data.swiftCode);
//        $('#Txtadcode').val(data.adCode);

//    } catch (error) {

//    }
//}

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

async function GetRecord(docNo) {

    const response = await fetch(...);
    const data = await response.json();

    // Fill other controls

    ERPFilterDropdownManager["ddlDocumentAttached"].SetValue([
        "Proforma Invoice",
        "Transport Document",
        "Form A2"
    ]);

}
function ClearForm() {

    $("#Numdocno").val("");
    $("#txtSupplierName").val("");

    ERPFilterDropdownManager["ddlDocumentAttached"].Clear();

}

async function Save() {

    const selectedDocuments =
        ERPFilterDropdownManager["ddlDocumentAttached"].GetValue();

    const documentNames = selectedDocuments.map(x => x.text);

    const model = {

        DocType: $("#ddldoctype").val(),
        DocNo: $("#Numdocno").val(),

        DocumentAttached: documentNames.join(",")

        // other properties...
    };

    console.log(model);

    // fetch/ajax save...
}