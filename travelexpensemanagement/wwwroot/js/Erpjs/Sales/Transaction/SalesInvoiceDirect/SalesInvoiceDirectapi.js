
async function LoadDropdown()
{
    try {
        await Promise.all([
            cmbV_type(),
            DDlFormType(),
            DDlMode(), 
            DDlLoadParty(),
            DDlWBParty(),
            DDlDoNo(),
            ddlPartyName(),
            cmbCityName(),
            cmbSalesThrough(),
            cmbTaxType(),
            cmbPurchaseNo()

        ]);
    } catch (error)
    {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbV_type() {
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlVType');
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

async function DDlFormType()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlFormType');
        const data = await res.json();
        const ddl = $('#ddlProductType');
        ddl.empty().append('<option value="">---Select Prod Type---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function DDlMode()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlMode');
        const data = await res.json();
        const ddl = $('#ddlMode');
        ddl.empty().append('<option value="">---Select Mode Type---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Mode Type:", error);
    }
}

async function DDlLoadParty()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlLoadParty');
        const data = await res.json();
        const ddl = $('#ddl_LoadParty');
        ddl.empty().append('<option value="">---Select Load Party---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Load Party:", error);
    }
}

async function DDlWBParty()
{
    try {
        const res = await fetch('/SalesInvoiceDirect/DDlLoadParty');
        const data = await res.json();
        const ddl = $('#ddl_WBParty');
        ddl.empty().append('<option value="">---Select Load Party---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Load Party:", error);
    }
}

async function GetVNo(Vtype, tableName) {
    const res = await fetch(`/SalesInvoiceDirect/GetVNo?Vtype=${encodeURIComponent(Vtype)}&tableName=${encodeURIComponent(tableName)}`);
    const data = await res.json();
    if (data.v_NO) {
        $('#NumInvoiceNo').val(data.v_NO);
    }
}

async function DDlDoNo()
{
    try
    {
        const res = await fetch('/SalesInvoiceDirect/DDlDoNo');
        const data = await res.json();

        const ddl = $('#ddlDONo');

        ddl.empty();
        ddl.append('<option value="">--- Select Do No ---</option>');

        data.forEach(item => {
            ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Do No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function ddlPartyName() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbPartyName');
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

    $('#TxtAddressL1').val(party.add1 || '');
    $('#TxtAddressL2').val(party.adD2 || '');
    $('#TxtAddressL3').val(party.adD3 || '');
    $('#ddlStation').val(party.citY_CODE || '');
    $('#NumPincode').val(party.pincode || '');
    $('#NumGSTNo').val(party.gstin || '');

    $('#ddlConsignee').val(party.code || '').trigger('change');
    $('#ddlSalesThrough').val(party.agenT_CODE || '').trigger('change');
    $('#TxtSupplyAddressL1').val(party.add1 || '');
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
    $('#TxtSupplyAddressL1').val(party.add1 || '');
    $('#TxtSupplyAddressL2').val(party.adD2 || '');
    $('#TxtSupplyAddressL3').val(party.adD3 || '');
    $('#ddlSupplyStation').val(party.citY_CODE || '');
    $('#NumSupplyPIN').val(party.pincode || '');
    $('#NumGSTNoL').val(party.gstin || '');
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

async function cmbPartyAddress(partycode) {
    try {
        const res = await fetch(`/SalesInvoiceDirect/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
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
        const res = await fetch(`/SalesInvoiceDirect/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
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

async function cmbSalesThrough() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbSalesThrough');
        const data = await res.json();

        const ddl = $('#ddlSalesThrough');

        ddl.empty();
        ddl.append('<option value="">--- Select Sales Through ---</option>');

        data.forEach(item => {
            ddl.append(  `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Sales Through---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}


async function AddressPartyData(PartyCode, AddressId) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoiceDirect/GetAddressData',
            type: 'GET',
            data: {
                PartyCode: PartyCode,
                AddressId: AddressId
            }
        });

        const data = res[0];

        if (!data) {
            console.log("No address data found");
            return;
        }

        console.log("Address Data", data);


        // Billing Address
        $('#TxtAddressL1').val(data.add1 || '');
        $('#TxtAddressL2').val(data.add2 || '');
        $('#TxtAddressL3').val(data.add3 || '');
        $('#ddlStation').val(data.city_Code || '');
        $('#NumPincode').val(data.pincode || ''); 
        $('#NumGSTNoL').val(data.gstin || '');

        // Shipping / Consignee Address
        $('#TxtSupplyAddressL1').val(data.add1 || '');
        $('#TxtSupplyAddressL2').val(data.add2 || '');
        $('#TxtSupplyAddressL3').val(data.add3 || '');
        $('#ddlSupplyStation').val(data.city_Code || '');
        $('#NumSupplyPIN').val(data.pincode || '');  
        $('#NumGSTNo').val(data.gstin || '');

        console.log("Address Data:", data);

        return data;
    }
    catch (error) {
        console.log("error", error);
    }
}

async function AddressConsigneeData(PartyCode, AddressId) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoiceDirect/GetAddressData',
            type: 'GET',
            data: {
                PartyCode: PartyCode,
                AddressId: AddressId
            }
        });


        const data = res[0];

        if (!data) {
            console.log("No address data found");
            return;
        }
        // Shipping / Consignee Address
        $('#TxtSupplyAddressL1').val(data.add1 || '');
        $('#TxtSupplyAddressL2').val(data.add2 || '');
        $('#TxtSupplyAddressL3').val(data.add3 || '');
        $('#ddlSupplyStation').val(data.city_Code || '');
        $('#NumSupplyPIN').val(data.pincode || '');
        $('#NumGSTNo').val(data.gstin || '');

        console.log("res", res);
        return res;
    }
    catch (error) {
        console.log("error", error);
    }
}


async function cmbTaxType() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbTaxType');
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


async function cmbPurchaseNo() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbPurchaseNo');
        const data = await res.json();

        const ddl = $('#ddlPurchaseNo');

        ddl.empty();
        ddl.append('<option value="">--- Select Purchase No ---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}"> ${item.value} - ${item.text}  </option>`);
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Purchase No ---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Purchase No:", error);
    }
}


async function cmbDocStatus() {
    try {
        const res = await fetch('/SalesInvoiceDirect/cmbDocStatus');
        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        ddl.empty();
        ddl.append('<option value="">--- Select Doc Status ---</option>');
        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
    
        });

    } catch (error) {
        console.error("Error loading  DocStatus:", error);
    }
}



