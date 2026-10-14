

async function LoadDropdown() {
    try {
        await Promise.all([
            cmbV_type(),
            DDlStatus(),
            DDlCURRENCY_MAST(),
            cmbPaymentTerm(),
            ddlPartyName(),
            cmbCityName(),
            cmbCOUNTRY_MAST(),      
            cmbSaleTh(),
            cmbTaxType(),
            cmbProductName(),
            cmbTransport(),
            cmbSoldBy()
        ]);
    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbV_type() {
    try {
        const res = await fetch('/SalesProformaInvoice/DDlVType');
        const data = await res.json();
        const ddl = $('#ddlInvType');
        // ddl.empty().append('<option value="">---Select Party Name---</option>');
        ddl.empty();
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function DDlStatus() {
    try {
        const res = await fetch('/SalesProformaInvoice/DDlStatus');
        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        // ddl.empty().append('<option value="">---Select Party Name---</option>');
        ddl.empty();
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Status:", error);
    }
}

async function cmbSaleTh()
{
    try {
        const res = await fetch('/SalesProformaInvoice/cmbSaleTh');
        const data = await res.json();
        const ddl = $('#ddlSalesThrough');

        ddl.empty().append('<option value="">---Select Sales Through---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Sales Through:", error);
    }
}

async function DDlCURRENCY_MAST() {
    try {
        const res = await fetch('/SalesProformaInvoice/DDlCURRENCY_MAST');
        const data = await res.json();
        const ddl = $('#ddlCurrency');
        ddl.empty().append('<option value="">---Select CURRENCY ---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Currency:", error);
    }
}

async function cmbPaymentTerm() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbPaymentTerm');
        const data = await res.json();
        const ddl = $('#ddlPaymentTerm');
        ddl.empty().append('<option value="">---Select Payment Term---</option>');
   
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Currency:", error);
    }
}

async function cmbCityName() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbCityName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlStationSa');
        const ddl1 = $('#ddlStationl');

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
        const res = await fetch(`/SalesProformaInvoice/cmbAddress?partycode=${encodeURIComponent(partycode)}`);
        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddladdressl1');
        ddl.empty();
        data.forEach(item => { const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
        });

    } catch (error) {
        console.error("Error  Party Address:", error);
    }
}

async function cmbConsigneeAddress(partycode) {
    try {
        const res = await fetch(`/SalesProformaInvoice/cmbAddress?partycode=${encodeURIComponent(partycode)}`);

        if (!res.ok)
        {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();  
        const ddl1 = $('#ddladdressl1Sa');

        ddl1.empty();

        data.forEach(item => {  const option = `<option value="${item.value}">${item.text}</option>`;
            ddl1.append(option);
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function cmbTransport() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbTransport');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlTransport');


        ddl.empty();
        ddl.append('<option value="">--- Select Transport ---</option>');

        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function cmbCOUNTRY_MAST() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbCOUNTRY_MAST');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlCountry');
        const ddl1 = $('#ddlCountrySa');

        ddl.empty();
        ddl1.empty();

        ddl.append('<option value="">--- Select Country ---</option>');
        ddl1.append('<option value="">--- Select Country ---</option>');

        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;

            ddl.append(option);
            ddl1.append(option);
        });

    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function ddlPartyName() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbPartyName');
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

async function cmbTaxType() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbTaxType');
        const data = await res.json();
        console.log("Tax Type data", data);
        TaxPercentageData = data;
        const ddl = $('#ddlTaxType');

        ddl.empty();
        ddl.append('<option value="">---Select Tax Type---</option>');

        data.forEach(item => { const option = `<option value="${item.code}">${item.name}</option>`;
            ddl.append(option);
        });

        TaxTypeList = data.map(x => `<option value="${x.code}">${x.name}</option>` ).join('');

    } catch (error) {
        console.error("Error loading Party Name:", error);
    }
}

async function cmbProductName() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbProductName');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${ res.status } `);
        }
        const data = await res.json();
        console.log("cmbProductName:", data);
        if (!Array.isArray(data)) {
            throw new Error("cmbProductName response is not an array");
        }
        ProductDataList = data;
        ProductList = data.map(x =>
            `<option value="${x.code}">${x.shortname}</option>`
        ).join('');


        return ProductList;

    } catch (error) {
        console.error("Error loading ItemName:", error);
        throw error;
    }
}

async function GetVNo(Vtype, tableName) {
    const res = await fetch(`/SalesProformaInvoice/GetVNo?Vtype=${encodeURIComponent(Vtype)}&tableName=${encodeURIComponent(tableName)}`);
    const data = await res.json();
    if (data.v_NO)
    {
        $('#NumSerialNo').val(data.v_NO);
    }
}
function selectedPartyData()
{
    const selectedCode = $('#ddlPartyName').val();

    const party = partyData.find(
        x => String(x.code) === String(selectedCode)
    );

    if (!party) {
        console.log("Party Data not found");
        return;
    }
    console.log("Selected Party:", party);

    $('#txtaddressL1').val(party.add1 || '');
    $('#txtaddressL2').val(party.add2 || '');
    $('#txtaddressL3').val(party.add3 || '');
    $('#ddlStationl').val(party.c_code || '');
    $('#NumPincode').val(party.pincode || '');
    $('#ddlCountry').val(party.c_code || '');
    $('#TxtGST').val(party.gstin || '');

    $('#ddlConsignee').val(party.code || '').trigger('change');
    $('#txtaddressL1Sa').val(party.add1 || '');
    $('#txtaddressL2Sa').val(party.add2 || '');
    $('#txtaddressL3Sa').val(party.add3 || '');
    $('#ddlStationSa').val(party.c_code || '');
    $('#NumPincodeSa').val(party.pincode || '');
    $('#ddlCountrySa').val(party.c_code || '');
    $('#TxtGSTSa').val(party.gstin || '');
}
function selectedConsigneePartyData() {
    const selectedCode = $('#ddlConsignee').val();

    const party = partyData.find( x => String(x.code) === String(selectedCode) );

    if (!party)
    {
        console.log("Party Data not found");
        return;
    }
    console.log("Selected Party:", party);

    $('#txtaddressL1Sa').val(party.add1 || '');
    $('#txtaddressL2Sa').val(party.add2 || '');
    $('#txtaddressL3Sa').val(party.add3 || '');
    $('#ddlStationSa').val(party.c_code || '');
    $('#NumPincodeSa').val(party.pincode || '');
    $('#ddlCountrySa').val(party.c_code || '');
    $('#TxtGSTSa').val(party.gstin || '');
}

async function cmbSoldBy() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbSoldBy');
        const data = await res.json();
        const ddl = $('#ddlSoldBy');

        ddl.empty().append('<option value="">---Select  Sold By---</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading  Sold By:", error);
    }
}

function ValidateDetailTable() {
    let isValid = true;
    let firstInvalidRow = null;
    let hasItem = false;

    let totalCGST = 0;
    let totalSGST = 0;
    let totalIGST = 0;

    $('#tblSalesProformaInvoice tbody tr').each(function () {
        let $row = $(this);
        const productCode = $.trim($row.find('.ddlProductName').val());
        const netQty = $.trim($row.find('.TxtNetQty').val());
        $row.find('.ddlProductName, .TxtNetQty').removeClass('is-invalid');

        if (productCode !== '')
        {
            hasItem = true;

            if (netQty === '' || parseFloat(netQty) <= 0) {
                $row.find('.TxtNetQty').addClass('is-invalid');
                if (!firstInvalidRow)
                {
                    firstInvalidRow = $row;
                }
                isValid = false;
            }
        }
        totalCGST += parseFloat($row.find('.TxtCgstAmt').val()) || 0;
        totalSGST += parseFloat($row.find('.TxtSgstamt').val()) || 0;
        totalIGST += parseFloat($row.find('.TxtIGSTamt').val()) || 0;
    });

    if (!hasItem) {

        const $firstRow =  $('#tblSalesProformaInvoice tbody tr').first();

        $firstRow.find('.ddlProductName').addClass('is-invalid');

        toastr.warning('At least one item is required.');

        $('html, body').animate({
            scrollTop: $firstRow.offset().top - 150
        }, 300);

        $firstRow.find('.ddlProductName').focus();

        return false;
    }

    if (!isValid) {

        toastr.warning('Net Qty is required for the selected item.');

        if (firstInvalidRow) {

            $('html, body').animate({
                scrollTop: firstInvalidRow.offset().top - 150
            }, 300);

            firstInvalidRow.find('.TxtNetQty').focus();
        }

        return false;
    }

    if ((totalCGST + totalSGST) > 0 && totalIGST > 0)
    {
        toastr.warning( 'Both GST Tax Rate is not applicable in one Proforma Invoice (CGST + SGST & IGST).' );
        return false;
    }

    if (Math.abs(totalCGST - totalSGST) > 0.01)
    {
        toastr.warning( 'CGST & SGST Amount Should Be Same');
        return false;
    }

    return true;
}
function DeleteRow(button)
{
    let row = $(button).closest('tr');
    if (row.length === 0) {
        return;
    }
    row.remove();
}

async function AddressPartyData(PartyCode, AddressId) {
    try {
        const res = await $.ajax({
            url: '/SalesProformaInvoice/GetAddressData',
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

        // Billing Address
        $('#txtaddressL1').val(data.add1 || '');
        $('#txtaddressL2').val(data.add2 || '');
        $('#txtaddressL3').val(data.add3 || '');
        $('#ddlStationl').val(data.city_Code || '');
        $('#NumPincode').val(data.pincode || '');
        $('#ddlCountry').val(data.countryCode || '');
        $('#TxtGST').val(data.gstin || '');

        // Shipping / Consignee Address
        $('#txtaddressL1Sa').val(data.add1 || '');
        $('#txtaddressL2Sa').val(data.add2 || '');
        $('#txtaddressL3Sa').val(data.add3 || '');
        $('#ddlStationSa').val(data.city_Code || '');
        $('#NumPincodeSa').val(data.pincode || '');
        $('#ddlCountrySa').val(data.countryCode || '');
        $('#TxtGSTSa').val(data.gstin || '');

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
            url: '/SalesProformaInvoice/GetAddressData',
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
        $('#txtaddressL1Sa').val(data.add1 || '');
        $('#txtaddressL2Sa').val(data.add2 || '');
        $('#txtaddressL3Sa').val(data.add3 || '');
        $('#ddlStationSa').val(data.city_Code || '');
        $('#NumPincodeSa').val(data.pincode || '');
        $('#ddlCountrySa').val(data.countryCode || '');
        $('#TxtGSTSa').val(data.gstin || '');

        console.log("res", res);
        return res;
    }
    catch (error) {
        console.log("error", error);
    }
}


