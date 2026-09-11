
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
            cmbAddress(),
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

async function cmbAddress() {
    try {
        const res = await fetch('/SalesProformaInvoice/cmbAddress');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddladdressl1');
        const ddl1 = $('#ddladdressl1Sa');

        ddl.empty();
        ddl1.empty();

        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;

            ddl.append(option);
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
        data.forEach(item => {
            const option = `<option value="${item.code}">${item.name}</option>`;
            ddl.append(option);
        });
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

    if (!selectedCode) {
        clearPartyFields();
        return;
    }

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
    $('#ddlStationSa').val(party.c_code || '');
    $('#NumPincode').val(party.pincode || '');
    $('#ddlCountry').val(party.c_code || '');
    $('#TxtGST').val(party.gstin || '');

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

function AddRow(data = {})
{
    let tbody = $('#tblSalesProformaInvoice tbody');
    let newRow = `
        <tr class="no-border-input">
            <td>  <input class="erppagetable-control ID" value="${data.ID ?? ''}" readonly /> </td>
            <td>  <select class="erppagetable-control ddlProductName"> <option value="">-- Select Product  --</option>  ${ProductList}  </select> </td>
            <td>  <input class="erppagetable-control txt_Productdiscr" value="${data.Prodisc ?? ''}"  /> </td>
            <td>  <input class="erppagetable-control HsnCode" value="${data.Hsncode ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtGrossQty" value="${data.grossQty ?? ''}" />  </td>
            <td>  <input type="number" class="erppagetable-control TxtNetQty" value="${data.NetQty ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtRate" value="${data.Rate ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPacKPer" value="${data.PackPer ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPackAmount" value="${data.PackAmt ?? ''}"   />  </td>        
            <td>  <input type="text"   class="erppagetable-control TxtDisPer" value="${data.DisPer ?? ''}"  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtDisAmount"  value="${data.Disamt ?? ''}" />   </td>
            <td>  <input type="number" class="erppagetable-control TxtFreight" value="${data.Freight ?? ''}"    />  </td>
            <td>  <input type="number" class="erppagetable-control TxtTaxType" value="${data.TaxType ?? ''}"  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstper"  value="${data.CgstPer ?? ''}"   />   </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstAmt" value="${data.CgstAmt ?? ''}"     />  </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstPer" value="${data.SgstPer ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstamt" value="${data.SgstAmt ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTPer" value="${data.IgstPer ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTamt" value="${data.IgstAmt ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessPer" value="${data.CessPer ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessamt" value="${data.CessAmt ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtRemark" value="${data.Remark ?? ''}"   /> </td>

            <td class="action-col">
            <button type="button"  class="act-btn add"  onclick="AddRow()">   <i class="fa fa-plus-circle"></i> </button>
            <button type="button"  class="act-btn delete"   onclick="DeleteRow(this)"> <i class="fa fa-trash"></i>  </button>
            </td>
        </tr>
    `;

    tbody.append(newRow);

    let $row = tbody.find('tr:last');


    $row.find('.ddlProductName').val(data.Productcode ?? '');


    // Load item details
    if (data.itemCode) {
        SetItemDetails($row);
    }
}
