

async function LoadDropdown() {
    try {
        await Promise.all([
            cmbV_type(),
            DDlDoNo(),
            cmbCityName(),
            ddlPartyName(),
            cmbSalesThrough(),
            cmbFormType(),
            cmbTaxType()
          
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
        ddl.append('<option value="">--- Select Do No ---</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}">
                    ${item.value} - ${item.text}
                </option>`
            );
        });

        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Do No ---', allowClear: true, width: '100%' });

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

    if (!selectedCode) {
        clearPartyFields();
        return;
    }

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
            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}" readonly  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPacKPer" value="${data.PackPer ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPackAmount" value="${data.PackAmt ?? ''}" readonly  />  </td>        
            <td>  <input type="number" class="erppagetable-control TxtPackWeight" value="${data.PackWght ?? ''}" readonly  />  </td>        
            <td>  <input type="text"   class="erppagetable-control TxtDisPer" value="${data.DisPer ?? ''}"  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtDisAmount"  value="${data.Disamt ?? ''}" readonly />   </td>
            <td>  <input type="number" class="erppagetable-control TxtFreight" value="${data.Freight ?? ''}"    />  </td>
            <td>  <select class="erppagetable-control TxtTaxType"> <option value="">-- Select Tax Type  --</option>  ${TaxTypeList}  </select> </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstper"  value="${data.CgstPer ?? ''}" readonly   />   </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstAmt" value="${data.CgstAmt ?? ''}"  readonly   />  </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstPer" value="${data.SgstPer ?? ''}"  readonly  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstamt" value="${data.SgstAmt ?? ''}"  readonly  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTPer" value="${data.IgstPer ?? ''}"  readonly  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTamt" value="${data.IgstAmt ?? ''}"   readonly  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessPer" value="${data.CessPer ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessamt" value="${data.CessAmt ?? ''}" readonly  /> </td>
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
    $row.find('.TxtTaxType').val(data.TaxType ?? '');
    // Calculate when values change
    $row.find(
        '.TxtNetQty, .TxtRate, .TxtPacKPer, .TxtDisPer, ' +
        '.TxtCgstper, .TxtSgstPer, .TxtIGSTPer,.TxtCessPer,.TxtFreight').on('input change', function () {
        CalculateRow($row);
    });

    // Tax Type change
    $row.find('.TxtTaxType').on('change', function () {

        const selectedCode = $(this).val();

        const selectedTax = TaxPercentageData.find( x => String(x.code) === String(selectedCode) );

        console.log("Selected Tax Type:", selectedTax);

        if (!selectedTax) {
            $row.find('.TxtCgstper').val('0.00');
            $row.find('.TxtSgstPer').val('0.00');
            $row.find('.TxtIGSTPer').val('0.00');

            CalculateRow($row);
            return;
        }

        $row.find('.TxtCgstper').val( Number(selectedTax.cgsT_PER || 0).toFixed(2) );
        $row.find('.TxtSgstPer').val( Number(selectedTax.sgsT_PER || 0).toFixed(2) );
        $row.find('.TxtIGSTPer').val( Number(selectedTax.igsT_PER || 0).toFixed(2) );

        CalculateRow($row);
    });

    // Load item details
    if (data.itemCode) {
        SetItemDetails($row);
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


async function DDlPackNo() {
    try {

        var v_type = $('#ddlDocumentType').val();
        var v_typetext = $('#ddlDocumentType option:selected').text();
        var V_DATE = $('#DtDocumentDate').val();
        var PARTY_CODE = $('#ddlPartyName').val();

        const res = await $.ajax({
            url: 'SalesInvoice/DDlPackNo',
            type: 'GET',
            data: {
                v_type: v_type,
                v_typetext: v_typetext,
                V_DATE: V_DATE,
                PARTY_CODE: PARTY_CODE
            }
        });


        console.log("DDlPackNo", res);




        console.log("Pack No", res);
    } catch (error) {
        console.log("error", error);
    }
}

