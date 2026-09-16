

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

async function cmbAddress(partycode) {
    try {
        const res = await fetch(`/SalesProformaInvoice/cmbAddress?partycode=${encodeURIComponent(partycode)}`);

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
    $('#ddlStationl').val(party.c_code || '');
    $('#NumPincode').val(party.pincode || '');
    $('#ddlCountry').val(party.c_code || '');
    $('#TxtGST').val(party.gstin || '');


    $('#ddlConsignee').val(party.code || '');
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
        '.TxtCgstper, .TxtSgstPer, .TxtIGSTPer, .TxtCessPer'
    ).on('input change', function () {
        CalculateRow($row);
    });


    // Tax Type change
    $row.find('.TxtTaxType').on('change', function () {

        const selectedCode = $(this).val();

        const selectedTax = TaxPercentageData.find(
            x => String(x.code) === String(selectedCode)
        );

        console.log("Selected Tax Type:", selectedTax);

        if (!selectedTax) {
            $row.find('.TxtCgstper').val('0.00');
            $row.find('.TxtSgstPer').val('0.00');
            $row.find('.TxtIGSTPer').val('0.00');

            CalculateRow($row);
            return;
        }

        $row.find('.TxtCgstper').val(
            Number(selectedTax.cgsT_PER || 0).toFixed(2)
        );

        $row.find('.TxtSgstPer').val(
            Number(selectedTax.sgsT_PER || 0).toFixed(2)
        );

        $row.find('.TxtIGSTPer').val(
            Number(selectedTax.igsT_PER || 0).toFixed(2)
        );

        // Recalculate after tax percentage changes
        CalculateRow($row);
    });

    // Load item details
    if (data.itemCode) {
        SetItemDetails($row);
    }
}

function CollectDetailRows() {

    let details = [];

    $('#tblSalesProformaInvoice tbody tr').each(function () {

        let $row = $(this);

        let detail = {

            // Basic / Product
            ROW_ID: $.trim($row.find('.ID').val()),
            ITEM_CODE: parseInt($row.find('.ddlProductName').val()) || null,
            ITEM_NAME: $.trim($row.find('.ddlProductName option:selected').text()),
            PROD_DESC: $.trim($row.find('.txt_Productdiscr').val()),
            HSN_CODE: $.trim($row.find('.HsnCode').val()),

            // Quantity
            NOS: parseInt($row.find('.TxtNos').val()) || null,
            GROSS_QTY: parseFloat($row.find('.TxtGrossQty').val()) || 0,
            QTY: parseFloat($row.find('.TxtNetQty').val()) || 0,

            // Rate / Amount
            RATE: parseFloat($row.find('.TxtRate').val()) || 0,
            AMOUNT: parseFloat($row.find('.TxtAmount').val()) || 0,

            // Packing
            PACK_PER: parseFloat($row.find('.TxtPacKPer').val()) || 0,
            PACK_AMT: parseFloat($row.find('.TxtPackAmount').val()) || 0,

            // Discount
            DISC_PER: parseFloat($row.find('.TxtDisPer').val()) || 0,
            DISC_AMT: parseFloat($row.find('.TxtDisAmount').val()) || 0,

            // Freight
            FREIGHT_AMT: parseFloat($row.find('.TxtFreight').val()) || 0,
            FRT_AMT: parseFloat($row.find('.TxtFreight').val()) || 0,

            // Tax
            TAX_CODE: parseInt($row.find('.TxtTaxType').val()) || null,

            // CGST
            CGST_PER: parseFloat($row.find('.TxtCgstper').val()) || 0,
            CGST_AMT: parseFloat($row.find('.TxtCgstAmt').val()) || 0,

            // SGST
            SGST_PER: parseFloat($row.find('.TxtSgstPer').val()) || 0,
            SGST_AMT: parseFloat($row.find('.TxtSgstamt').val()) || 0,

            // IGST
            IGST_PER: parseFloat($row.find('.TxtIGSTPer').val()) || 0,
            IGST_AMT: parseFloat($row.find('.TxtIGSTamt').val()) || 0,

            // CESS
            CESS_PER: parseFloat($row.find('.TxtCessPer').val()) || 0,
            CESS_AMT: parseFloat($row.find('.TxtCessamt').val()) || 0,

            // Remark
            REMARK: $.trim($row.find('.TxtRemark').val())
        };

        details.push(detail);
    });

    return details;
}

function CalculateRow($row) {

    let netQty = parseFloat($row.find('.TxtNetQty').val()) || 0;
    let rate = parseFloat($row.find('.TxtRate').val()) || 0;
    let packPer = parseFloat($row.find('.TxtPacKPer').val()) || 0;
    let disPer = parseFloat($row.find('.TxtDisPer').val()) || 0;
    let freight = parseFloat($row.find('.TxtFreight').val()) || 0;

    // Amount = Net Qty × Rate
    let amount = netQty * rate;
    // Packing Amount = Amount × Packing %
    let packAmt = amount * packPer / 100;
    // Discount Amount = (Amount + Packing Amount) × Discount %
    let disAmt = (amount + packAmt) * disPer / 100;

    let taxableAmount = amount + packAmt - disAmt +  freight;

    // =========================================================
    // TAX CALCULATION
    // =========================================================
    let cgstPer = parseFloat($row.find('.TxtCgstper').val()) || 0;
    let sgstPer = parseFloat($row.find('.TxtSgstPer').val()) || 0;
    let igstPer = parseFloat($row.find('.TxtIGSTPer').val()) || 0;
    let cessPer = parseFloat($row.find('.TxtCessPer').val()) || 0;

    let cgstAmt = taxableAmount * cgstPer / 100;
    let sgstAmt = taxableAmount * sgstPer / 100;
    let igstAmt = taxableAmount * igstPer / 100;
    let cessAmt = taxableAmount * cessPer / 100;

    // =========================================================
    // SET CURRENT ROW VALUES
    // =========================================================
    $row.find('.TxtAmount').val(amount.toFixed(2));
    $row.find('.TxtPackAmount').val(packAmt.toFixed(2));
    $row.find('.TxtDisAmount').val(disAmt.toFixed(2));

    $row.find('.TxtCgstAmt').val(cgstAmt.toFixed(2));
    $row.find('.TxtSgstamt').val(sgstAmt.toFixed(2));
    $row.find('.TxtIGSTamt').val(igstAmt.toFixed(2));
    $row.find('.TxtCessamt').val(cessAmt.toFixed(2));

    // =========================================================
    // TOTAL OF ALL ROWS
    // =========================================================

    let totalNos = 0;
    let totalGrossQty = 0;
    let totalNetQty = 0;
    let totalAmount = 0;
    let totalPackAmount = 0;
    let totalDiscount = 0;
    let totalFreight = 0;
    let totalCGST = 0;
    let totalSGST = 0;
    let totalIGST = 0;
    let totalCess = 0;

    $('#tblSalesProformaInvoice tbody tr').each(function () {

        let $r = $(this);

        // Ignore empty rows
        if (!$r.find('.ddlProductName').val()) {
            return;
        }
        totalNos += parseFloat($r.find('.TxtNos').val()) || 0;
        totalGrossQty += parseFloat($r.find('.TxtGrossQty').val()) || 0;
        totalNetQty += parseFloat($r.find('.TxtNetQty').val()) || 0;
        totalAmount += parseFloat($r.find('.TxtAmount').val()) || 0;
        totalPackAmount += parseFloat($r.find('.TxtPackAmount').val()) || 0;
        totalDiscount += parseFloat($r.find('.TxtDisAmount').val()) || 0;
        totalFreight += parseFloat($r.find('.TxtFreight').val()) || 0;
        totalCGST += parseFloat($r.find('.TxtCgstAmt').val()) || 0;
        totalSGST += parseFloat($r.find('.TxtSgstamt').val()) || 0;
        totalIGST += parseFloat($r.find('.TxtIGSTamt').val()) || 0;
        totalCess += parseFloat($r.find('.TxtCessamt').val()) || 0;
    });

    let subtotal = totalAmount + totalPackAmount - totalDiscount +  totalFreight;
    let totalBeforeTCS =  subtotal + totalCGST + totalSGST +  totalIGST + totalCess;
    let tcsPer =  parseFloat($('#NumTCS1').val()) || 0;
    let tcsAmt = Math.ceil( totalBeforeTCS * tcsPer / 100 );
    let netAmount = Math.round( totalBeforeTCS + tcsAmt );
    let roundOff =  netAmount - ( subtotal + totalCGST +  totalSGST + totalIGST +  totalCess +  tcsAmt  );
    let insPer = parseFloat($('#NumInsurance1').val()) || 0;
    let insAmt = 0;
    if (insPer > 0)
    {
        insAmt = Math.round( netAmount * (insPer / 100000) );
    }


    // Total Amount
    $('#Numtotalamount').val(totalAmount.toFixed(2));
    // Total Packing Amount
    $('#NumPacking2').val(totalPackAmount.toFixed(2));
    // Total Discount Amount
    $('#NumDiscount2').val(totalDiscount.toFixed(2));
    // Total Freight
    $('#NumFreight').val(totalFreight.toFixed(2));
    // Subtotal
    $('#NumSubTotal').val(subtotal.toFixed(2));
    // =========================================================
    // QUANTITY TOTALS
    // =========================================================
    // Total NOS
    $('#NumTotalNos').val(totalNos.toFixed(2));
    // Total Gross Qty
    $('#NumGrossQty').val(totalGrossQty.toFixed(2));
    // Total Net Qty
    $('#NumNetQty').val(totalNetQty.toFixed(2));
    // =========================================================
    // GST TOTALS
    // =========================================================
    // CGST Amount
    $('#NumCGST2').val(totalCGST.toFixed(2));
    // SGST Amount
    $('#NumSGST2').val(totalSGST.toFixed(2));
    // IGST Amount
    $('#NumIGST2').val(totalIGST.toFixed(2));
    // CESS Amount
    $('#NumCESS2').val(totalCess.toFixed(2));
    // =========================================================
    // TCS
    // =========================================================
    $('#NumTCS2').val(tcsAmt.toFixed(2));
    // =========================================================
    // ROUND OFF
    // =========================================================
    $('#NumRoundOff').val(roundOff.toFixed(2));
    // =========================================================
    // NET AMOUNT
    // =========================================================
    $('#NumNetAmount').val(netAmount.toFixed(2));
    // =========================================================
    // INSURANCE
    // =========================================================
    $('#NumInsurance2').val(insAmt.toFixed(2));
}

async function LoadData() {
    try {

        const res = await $.ajax({
            url: '/SalesProformaInvoiceList/GetDataByCode',
            type: 'Post',
            data: { DOC_ID: rowId }
        });

        console.log("LoadData response:", res); 

        let Header = res.data.header;
        let Details = res.data.details;

        $('#CODE').val(Header.doC_ID);
        $('#ddlInvType').val(Header.v_TYPE);
        $('#NumSerialNo').val(Header.v_NO);
        $('#DtDate').val(formatDate(Header.v_DATE));
        $('#ddlSupplyType').val(Header.supplY_TYPE);
        $('#ddlCurrency').val($('#ddlCurrency option').filter(function () { return $.trim($(this).text()) === $.trim(Header.imporT_CURRENCY); }).val()).trigger('change');
        $('#ddlPartyName').val(Header.bilL_CODE);
        $('#txtaddressL1').val(Header.bilL_ADD1);
        $('#txtaddressL2').val(Header.bilL_ADD2);
        $('#txtaddressL3').val(Header.bilL_ADD3);
        $('#ddlStationl').val(Header.bilL_CITY);
        $('#NumPincode').val(Header.bilL_PINCODE);
        $('#ddlCountry').val(Header.bilL_COUNTRY);
        $('#TxtGST').val(Header.bilL_GST);
        $('#ddlSalesThrough').val(Header.agenT_CODE);
        $('#ddlConsignee').val(Header.shiP_CODE);
        $('#txtaddressL1Sa').val(Header.shiP_ADD1);
        $('#txtaddressL2Sa').val(Header.shiP_ADD2);
        $('#txtaddressL3Sa').val(Header.shiP_ADD3);
        $('#ddlStationSa').val(Header.shiP_CITY);
        $('#NumPincodeSa').val(Header.shiP_PINCODE);
        $('#ddlCountrySa').val(Header.shiP_COUNTRY);
        $('#TxtGSTSa').val(Header.shiP_GST);

        $('#ddlProdType').val(Header.iteM_TYPE);
        $('#TxtARNNo').val(Header.gR_NO);
        $('#DtARNdate').val(formatDate(Header.gR_DATE));
        $('#txtModeofTransport').val(Header.vehiclE_NO);
        $('#ddlTransport').val(Header.transporT_CODE);
        $('#TxtPortLoading').val(Header.porT_LOADING);
        $('#TxtPortDisch').val(Header.porT_DISCHARGE);
        $('#ddlIncoterm').val(Header.incoterm);
        $('#ddlShipment').val(Header.shipmenT_TYPE);
        $('#TxtModepayment').val(Header.modeoF_PAYMENT);
        $('#ddlContainerSize').val(Header.containeR_SIZE);
        $('#TxtBuyerorderno').val(Header.buyeR_ORDNO);
        $('#Numtotalamount').val(Header.amount);
        $('#NumPacking2').val(Header.pacK_AMT);
        $('#NumDiscount2').val(Header.disC_AMT);
        $('#NumFreight').val(Header.frT_AMT);
        $('#NumSubTotal').val(Header.frT_AMT);
        $('#NumCGST2').val(Header.cgsT_AMT);
        $('#NumSGST2').val(Header.sgsT_AMT);
        $('#NumIGST2').val(Header.igsT_AMT);
        $('#NumTotalNos').val(Header.toT_NOS);
        $('#NumCESS2').val(Header.cesS_AMT);
        $('#NumTCS1').val(Header.tcS_PER);
        $('#NumTCS2').val(Header.tcS_AMT);
        $('#NumRoundOff').val(Header.rounD_OFF);
        $('#NumNetAmount').val(Header.namount);
        $('#ddlDocStatus').val(Header.status);
        $('#txtRemarks').val(Header.remark);
        $('#NumGrossQty').val(Header.toT_GROSS);
        $('#NumNetQty').val(Header.toT_NET);
        $('#ddlSoldBy').val(Header.solD_BY);
        $('#txtPriceValidity').val(Header.finaL_DEST);
        $('#ddlPaymentTerm').val(Header.paY_TERM);
        $('#txtTransportation').val(Header.transporT_CODE);
        $('#NumWeighmentQty1').val(Header.wB_NO);
        $('#NumWeighmentQty2').val(Header.wB_QTY);
        $('#txtPackaging').val(Header.insU_DETAIL);
        $('#txtDeliverySchedule').val(Header.deL_SCH);
        // Load Detail Rows
        let tbody = $('#tblSalesProformaInvoice tbody');
        tbody.empty();

        if (Array.isArray(Details) && Details.length > 0) {

            Details.forEach(detail => {

                AddRow({
                    ID: detail.iteM_CODE ?? '',
                    Productcode: detail.iteM_CODE ?? '',
                    Prodisc: detail.proD_DESC ?? '',
                    Hsncode: detail.hsN_CODE ?? '',
                    nos: detail.nos ?? 0,
                    grossQty: detail.grosS_QTY ?? 0,
                    NetQty: detail.qty ?? 0,
                    Rate: detail.rate ?? 0,
                    Amount: detail.amount ?? 0,
                    PackPer: detail.pacK_PER ?? 0,
                    PackAmt: detail.pacK_AMT ?? 0,
                    DisPer: detail.disC_PER ?? 0,
                    Disamt: detail.disC_AMT ?? 0,
                    Freight: detail.freighT_AMT ?? 0,
                    TaxType: detail.taX_CODE ?? '',
                    CgstPer: detail.cgsT_PER ?? 0,
                    CgstAmt: detail.cgsT_AMT ?? 0,
                    SgstPer: detail.sgsT_PER ?? 0,
                    SgstAmt: detail.sgsT_AMT ?? 0,
                    IgstPer: detail.igsT_PER ?? 0,
                    IgstAmt: detail.igsT_AMT ?? 0,
                    CessPer: detail.cesS_PER ?? 0,
                    CessAmt: detail.cesS_AMT ?? 0,
                    Remark: detail.remark ?? ''
                });

            });

        } else {
            AddRow();
        }







    }
    catch (error) {
        console.error("Error loading data:", error);
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

        // Remove previous validation
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
