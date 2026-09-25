

async function LoadDropdown() {
    try {
        await Promise.all([
            cmbV_type(),
            DDlDoNo(),
            cmbCityName(),
            ddlPartyName(),
            cmbSalesThrough(),
            cmbFormType(),
            cmbTaxType(),
            DDlSaudaNo(),
            DDlIssueNo(),
            cmbGodown(),
            cmbProdType(),
            cmbWBNO(),
            DDlDocStatus(),
            DDlTransPort(),
            cmbProductName(),
            DDlLoadParty(),
            DDlWBParty(),
            DDlTransPortMode()
        ]);
    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbV_type() {
    try {
        const res = await fetch('/SalesInvoice/DDlVType');
        const data = await res.json();
        const ddl = $('#ddlDocumentType');
        // ddl.empty().append('<option value="">---Select Party Name---</option>');
        ddl.empty();
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function DDlTransPortMode() {
    try {
        const res = await fetch('/SalesInvoice/DDlTransPortMode');
        const data = await res.json();
        const ddl = $('#ddlMode');
         ddl.empty().append('<option value="">---Select Mode--</option>');
 
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function DDlDoNo() {
    try {
        const res = await fetch('/SalesInvoice/DDlDoNo');
        const data = await res.json();

        const ddl = $('#ddlDONo');

        ddl.empty();
        ddl.append('<option value="">--- Select Do No ---</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">
                    ${item.value} - ${item.text}
                </option>`
            );
        });

        // Initialize searchable dropdown
        ddl.select2({  placeholder: '--- Select Do No ---', allowClear: true,  width: '100%'  });

    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function cmbCityName() {
    try {
        const res = await fetch('/SalesInvoice/cmbCityName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlStation');
        const ddl1 = $('#ddlSupplyStation');

        ddl.empty();
        ddl1.empty();

        ddl.append('<option value="">--- Select City ---</option>');
        ddl1.append('<option value="">--- Select City ---</option>');

        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;

            ddl.append(option);
            ddl1.append(option);
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function cmbFormType() {
    try {
        const res = await fetch('/SalesInvoice/cmbFormType');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlFormType');
        ddl.empty();  
        ddl.append('<option value="">--- Select Form Type ---</option>');
        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function cmbSalesThrough() {
    try {
        const res = await fetch('/SalesInvoice/cmbSalesThrough');
        const data = await res.json();

        const ddl = $('#ddlSalesThrough');

        ddl.empty();
        ddl.append('<option value="">--- Select Sales Through ---</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">
                    ${item.value} - ${item.text}
                </option>`
            );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Sales Through---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function ddlPartyName() {
    try {
        const res = await fetch('/SalesInvoice/cmbPartyName');
        const data = await res.json();

        console.log("data", data);

        partyData = data;

        const ddl = $('#ddlPartyName');
        const ddl1 = $('#ddlConsignee');

        ddl.empty();
        ddl1.empty();

        ddl.append('<option value="">---Select Party Name---</option>');
        ddl1.append('<option value="">---Select Party Name---</option>');

        data.forEach(item => {
            const option = `<option value="${item.code}">${item.p_name}</option>`;

            ddl.append(option);
            ddl1.append(option);
        });

        // Initialize searchable dropdown
        ddl.select2({
            placeholder: "---Select Party Name---",
            allowClear: true,
            width: '100%'
        });

        ddl1.select2({
            placeholder: "---Select Party Name---",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        console.error("Error loading Party Name:", error);
    }
}

function selectedPartyData() {
    const selectedCode = $('#ddlPartyName').val();  

    const party = partyData.find(
        x => String(x.code) === String(selectedCode)
    );

    console.log("Party Data List ", party);

    if (!party) {
        console.log("Party Data not found");
        return;
    }
    console.log("Selected Party:", party);

    $('#TxtAddressL1').val(party.adD1 || '');
    $('#TxtAddressL2').val(party.adD2 || '');
    $('#TxtAddressL3').val(party.adD3 || '');
    $('#ddlStation').val(party.citY_CODE || '');
    $('#NumPincode').val(party.pincode || '');
    $('#NumGSTNo').val(party.gstin || '');

    $('#ddlConsignee').val(party.code || '').trigger('change');
    $('#ddlSalesThrough').val(party.agenT_CODE || '').trigger('change');
    $('#TxtSupplyAddressL1').val(party.adD1 || '');
    $('#TxtSupplyAddressL2').val(party.adD2 || '');
    $('#TxtSupplyAddressL3').val(party.adD3 || '');
    $('#ddlSupplyStation').val(party.citY_CODE || '');
    $('#NumSupplyPIN').val(party.pincode || '');
    $('#NumGSTNoL').val(party.gstin || '');
}

function selectedConsigneeData() {
    const selectedCode = $('#ddlConsignee').val();

    const party = partyData.find(
        x => String(x.code) === String(selectedCode)
    );
    console.log("Party Data List ", party);

    if (!party) {
        console.log("Party Data not found");
        return;
    }
    console.log("Selected Party:", party);

    $('#ddlSalesThrough').val(party.agenT_CODE || '').trigger('change');
    $('#TxtSupplyAddressL1').val(party.adD1 || '');
    $('#TxtSupplyAddressL2').val(party.adD2 || '');
    $('#TxtSupplyAddressL3').val(party.adD3 || '');
    $('#ddlSupplyStation').val(party.citY_CODE || '');
    $('#NumSupplyPIN').val(party.pincode || '');
    $('#NumGSTNoL').val(party.gstin || '');
}


async function cmbTaxType() {
    try {
        const res = await fetch('/SalesInvoice/cmbTaxType');
        const data = await res.json();
        console.log("Tax Type data", data);
        TaxPercentageData = data;
        const ddl = $('#ddlTaxType');

        ddl.empty();
        ddl.append('<option value="">---Select Tax Type---</option>');

        data.forEach(item => {
            const option = `<option value="${item.code}">${item.name}</option>`;
            ddl.append(option);
        });

        TaxTypeList = data.map(x => `<option value="${x.code}">${x.name}</option>`).join('');

    } catch (error) {
        console.error("Error loading Party Name:", error);
    }
}

async function GetVNo(Vtype, tableName) {
    const res = await fetch(`/SalesInvoice/GetVNo?Vtype=${encodeURIComponent(Vtype)}&tableName=${encodeURIComponent(tableName)}`);
    const data = await res.json();
    if (data.v_NO)
    {
        $('#NumInvoiceNo').val(data.v_NO);
    }
}


function DeleteRow(button)
{
    let row = $(button).closest('tr');
    if (row.length === 0) {
        return;
    }
    row.remove();
}

async function DDlPackNo(PARTY_CODE) {
    try {
        const v_type = $('#ddlDocumentType').val() || "";
        const v_typetext = $('#ddlDocumentType option:selected').text().trim();
        const V_DATE = $('#DtDocumentDate').val() || "";

        const res = await $.ajax({
            url: '/SalesInvoice/DDlPackNo',
            type: 'GET',
            data: {
                v_type: v_type,
                v_typetext: v_typetext,
                V_DATE: V_DATE,
                PARTY_CODE: PARTY_CODE || ""
            },
            dataType: 'json'
        });

        const ddl = $('#ddlPackNo');

        ddl.empty();
        ddl.append('<option value="">--- Select Pack No ---</option>');

        res.forEach(item => {
            ddl.append(
                `<option value="${item.value}">${item.value} - ${item.text}</option>`
            );
        });

    } catch (error) {
        console.error("DDlPackNo Error:", error);
    }
}

async function DDlSaudaNo() {
    try {
        const res = await fetch('/SalesInvoice/DDlSaudaNo');
        const data = await res.json();

        console.log("Sauda no Data", data);

        const ddl = $('#ddlSaudaNo');

        ddl.empty();
        ddl.append('<option value="">--- Select Sauda No ---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.v_no}">  ${item.v_no} - ${item.v_TYPE} </option>` );
        });

        ddl.select2({ placeholder: '--- Select Sauda No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Sauda No:", error);
    }
}

async function cmbPartyAddress(partycode) {
    try {
        const res = await fetch(`/SalesInvoice/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
        const data = await res.json();

        const ddl = $('#ddladdressL1');
        ddl.empty();

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Address:", error);
    }
}

async function cmbConsigneeAddress(partycode) {
    try {
        const res = await fetch(`/SalesInvoice/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
        const data = await res.json();

        const ddl = $('#ddlsupplyaddressL1');
        ddl.empty();

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Address:", error);
    }
}

async function DDlIssueNo() {
    try {
        const res = await fetch('/SalesInvoice/DDlIssueNo');
        const data = await res.json();

        console.log("Sauda no Data", data);

        const ddl = $('#ddlIssueNo');

        ddl.empty();
        ddl.append('<option value="">--- Select Issue No ---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.v_no}">  ${item.v_no} - ${item.v_TYPE} </option>`);
        });

        ddl.select2({ placeholder: '--- Select Issue No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Issue No:", error);
    }
}

async function cmbGodown() {
    try {
        const res = await fetch('/SalesInvoice/cmbGodown');
        const data = await res.json();
        const ddl = $('#ddlGodown');
        ddl.empty().append('<option value="">---Select Godown---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function cmbProdType() {
    try {
        const res = await fetch('/SalesInvoice/cmbProdType');
        const data = await res.json();
        const ddl = $('#ddlProductType');
        ddl.empty().append('<option value="">---Select Prod Type---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Prod Type:", error);
    }
}

async function cmbWBNO() {
    try {
        const res = await fetch('/SalesInvoice/cmbWBNO');
        const data = await res.json();

        const ddl = $('#ddlWBNo');

        ddl.empty();
        ddl.append('<option value="">--- Select Do No ---</option>');

        data.forEach(item => {
            ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>`  );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Do No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Do No:", error);
    }
}

async function DDlDocStatus() {
    try {
        const res = await fetch('/SalesInvoice/DDlDocStatus');
        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        // ddl.empty().append('<option value="">---Select Party Name---</option>');
        ddl.empty();
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function DDlTransPort() {
    try {
        const res = await fetch('/SalesInvoice/DDlTransPort');
        const data = await res.json();

        console.log(" tran data", data);


        const ddl = $('#ddlTransport');
         ddl.empty().append('<option value="">---Select Transport Name---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.code}">${item.name}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}


async function cmbProductName() {
    try {
        const res = await fetch('/SalesInvoice/cmbProductName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status} `);
        }
        const data = await res.json();
        console.log("cmbProductName:", data);
        if (!Array.isArray(data)) {
            throw new Error("cmbProductName response is not an array");
        }

        ProductList = data.map(x =>
            `<option value="${x.code}">${x.itemname}</option>`
        ).join('');


        return ProductList;

    } catch (error) {
        console.error("Error loading ItemName:", error);
        throw error;
    }
}


async function DDlLoadParty() {
    try {
        const res = await fetch('/SalesInvoice/DDlLoadParty');
        const data = await res.json();
        const ddl = $('#ddl_LoadParty');
         ddl.empty().append('<option value="">---Select Load Party Name---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Load Party Name:", error);
    }
}

async function DDlWBParty() {
    try {
        const res = await fetch('/SalesInvoice/DDlWBParty');
        const data = await res.json();
        const ddl = $('#ddl_WBParty');
         ddl.empty().append('<option value="">---Select WB Party Name---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading WB Party Name:", error);
    }
}



