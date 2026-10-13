function AddRow(data = {}) {
    let tbody = $('#tblSalesInvoice tbody');
    let newRow = `
        <tr class="no-border-input">
            <td>  <input class="erppagetable-control ID" value="${data.ID ?? ''}" readonly /> </td>
            <td>  <select class="erppagetable-control ddlProductName"> <option value="">-- Select Product  --</option>  ${ProductList}  </select> </td>
            <td>  <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtGrossQty" value="${data.grossQty ?? ''}" />  </td>
            <td>  <input type="number" class="erppagetable-control TxtNetQty" value="${data.NetQty ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtRateINUSD" value="${data.RateINUSD ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtRate" value="${data.Rate ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}"  readonly  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPacKPer" value="${data.PackPer ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPackAmount" value="${data.PackAmt ?? ''}" readonly  />  </td>        
            <td>  <input type="number" class="erppagetable-control TxtPackWeight" value="${data.PackWght ?? ''}"   />  </td>        
            <td>  <input type="number"   class="erppagetable-control TxtDisPer" value="${data.DisPer ?? ''}"  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtDisAmount"  value="${data.Disamt ?? ''}"  readonly />   </td>
            <td>  <select class="erppagetable-control TxtTaxType"> <option value="">-- Select Tax Type  --</option>  ${TaxTypeList}  </select> </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstper"  value="${data.CgstPer ?? ''}"   readonly />   </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstAmt" value="${data.CgstAmt ?? ''}"   readonly  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstPer" value="${data.SgstPer ?? ''}"   readonly /> </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstamt" value="${data.SgstAmt ?? ''}"   readonly /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTPer" value="${data.IgstPer ?? ''}"  readonly  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTamt" value="${data.IgstAmt ?? ''}"  readonly /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessPer" value="${data.CessPer ?? ''}"  readonly /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessamt" value="${data.CessAmt ?? ''}" readonly  /> </td>
            <td>  <input type="text" class="erppagetable-control TxtRemark" value="${data.Remark ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtPackno" value="${data.Packno ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtLotNo" value="${data.Lotno ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtSaudaType" value="${data.SaudaType ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtSaudaNo" value="${data.SaudaNo ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtSaudaRate" value="${data.SaudaRate ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtOrderType" value="${data.OrderType ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtOrderNo" value="${data.OrderNo ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtOrderRate" value="${data.OrderRate ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtDocType" value="${data.DocType ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtDocNo" value="${data.DocNo ?? ''}"   /> </td>
            <td>  <input type="text" class="erppagetable-control TxtHsnCode" value="${data.HsnCode ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtFreightAmount" value="${data.FreightAmount ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtInsuAmount" value="${data.InsuAmount ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCDiscAmount" value="${data.CDiscAmount ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtWBQuantity" value="${data.WBQuantity ?? ''}"   /> </td>

            <td class="hidden-col">  <input type="text" class="erppagetable-control TxtREPORT_TYPE" value="${data.REPORT_TYPE ?? ''}"   /> </td>
            <td class="hidden-col">  <input type="number" class="erppagetable-control TxtSale_Rate" value="${data.Sale_Rate ?? ''}"   /> </td>
            <td class="hidden-col">  <input type="number" class="erppagetable-control TxtTaxable_Rate" value="${data.Taxable_Rate ?? ''}"   /> </td>
            <td class="hidden-col">  <input type="number" class="erppagetable-control TxtNet_Wt" value="${data.Net_Wt ?? ''}"   /> </td>
            <td class="hidden-col">  <input type="number" class="erppagetable-control TxtPacking_Wt" value="${data.Packing_Wt ?? ''}"   /> </td>
            <td class="hidden-col">  <input type="number" class="erppagetable-control TxtPacking_nos" value="${data.Packing_nos ?? ''}"   /> </td>
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

    $row.find('.TxtNos, .TxtGrossQty, .TxtNetQty, .TxtRate, ' +
        '.TxtPacKPer, .TxtDisPer, .TxtCgstper,.TxtSgstPer,.TxtIGSTPer,.TxtCessPer').on('input change', function () {
            CalculateRow($row);
    });

    $row.find('.TxtTaxType').on('change', function () {

        const selectedCode = $(this).val();
        const selectedTax = TaxPercentageData.find(x => String(x.code) === String(selectedCode));
        if (!selectedTax) {
            $row.find('.TxtCgstper').val('0.00');
            $row.find('.TxtSgstPer').val('0.00');
            $row.find('.TxtIGSTPer').val('0.00');
            CalculateRow($row);
            return;
        }

        $row.find('.TxtCgstper').val(Number(selectedTax.cgsT_PER || 0).toFixed(2));
        $row.find('.TxtSgstPer').val(Number(selectedTax.sgsT_PER || 0).toFixed(2));
        $row.find('.TxtIGSTPer').val(Number(selectedTax.igsT_PER || 0).toFixed(2));
        CalculateRow($row);
    });

    $row.find('.ddlProductName').on('change', async function () {

        const ItemCode = $(this).val();

        $row.find('.ID').val(ItemCode);

        if (!ItemCode)
        {
            return;
        }

        const data = await GetCalRate(ItemCode);

        console.log("Returned Data:", data);

        if (data && data.length > 0)
        {
            const item = data[0];
            $row.find('.TxtREPORT_TYPE').val(item.reporT_TYPE);
            $row.find('.TxtSale_Rate').val(item.sale_Rate);
            $row.find('.TxtTaxable_Rate').val(item.taxable_Rate);
            $row.find('.TxtNet_Wt').val(item.net_Wt);
            $row.find('.TxtPacking_Wt').val(item.packing_Wt);
            $row.find('.TxtPacking_nos').val(item.packing_nos);
        }
    });
    
    $row.find('.TxtNos').on('input', function () {
        const $rows = $('#tblSalesInvoice tbody tr');
        if ($rows.length <= 1)
        {
            return;
        }
        const nos = $.trim($(this).val());
        if (nos === '')
        {
            return;
        }
        const $previousRow = $row.prev('tr');
        if ($previousRow.length === 0)
        {
            return;
        }
        $row.find('.TxtTaxType') .val($previousRow.find('.TxtTaxType').val());
        $row.find('.TxtCgstper') .val($previousRow.find('.TxtCgstper').val());
        $row.find('.TxtSgstPer')  .val($previousRow.find('.TxtSgstPer').val());
        $row.find('.TxtIGSTPer').val($previousRow.find('.TxtIGSTPer').val());
        CalculateRow($row);
    });
}
function CalculateRow($row) {
    try {
        const settings = LoadGeneralSetting || {};
        const isYes = value => String(value ?? '').trim().toLowerCase() === 'yes';
        const num = (selector, $r = $row) => {
            const value = parseFloat($r.find(selector).val());
            return Number.isFinite(value) ? value : 0;
        };

        const setVal = (selector, value, $r = $row) => {
            $r.find(selector).val(Number(value || 0).toFixed(2));
        };

        const defTonnageRate = isYes(settings.pubDefTonnageRate);
        const defWtCalconBales = isYes(settings.pubDefWtCalconBales);
        const taxOnInsurance = isYes(settings.pubDefTaxonInsuInSI);
        const taxOnFreight = isYes(settings.pubDefTaxonFrtInSI);
        const insuranceInBill = isYes(settings.pubDefInsuInBillAmtInSI);
        const freightInBill = isYes(settings.pubDefFrtInBillAmtInSI);

        // ----------------------------------
        // 1. Quantity calculation
        // ----------------------------------
        const nos = num('.TxtNos');

        let grossQty = num('.TxtGrossQty');
        let netQty = num('.TxtNetQty');
        const netWt = num('.TxtNet_Wt');
        const packingWt = num('.TxtPacking_Wt');
        const packingNos = num('.TxtPacking_nos');
        const reportType = $.trim(  $row.find('.TxtREPORT_TYPE').val() || '' );
        const rate = num('.TxtRate');
        const taxableRate = num('.TxtTaxable_Rate');

        if (defTonnageRate)
        {
            if (netWt > 0) {
                grossQty = nos * netWt;
                setVal('.TxtGrossQty', grossQty);
            }

            if (packingNos > 0) {
                netQty = nos * packingNos;
                setVal('.TxtNetQty', netQty);
            }
        }

        if (defWtCalconBales)
        {
            if (netWt > 0) {
                grossQty = nos * netWt;
                setVal('.TxtGrossQty', grossQty);
            }

            if (packingWt > 0) {
                netQty = nos * packingWt;
                setVal('.TxtNetQty', netQty);
            }
        }

        // Read calculated quantities again
        grossQty = num('.TxtGrossQty');
        netQty = num('.TxtNetQty');

        // ----------------------------------
        // 2. Amount calculation
        // Matches VB.NET Cells(8)
        // ----------------------------------
        let amount = 0;

        if ($.trim($row.find('.ddlProductName').val() || '') !== '' && netQty > 0)
        {
            if (defTonnageRate)
            {
                if (reportType === 'Hessian' ||
                    reportType === 'Sacking') {

                    amount = (netQty * rate) / 100;
                }
                else if (reportType === 'Twine' && netWt > 0) {
                    // Avoid division by zero
                    amount = taxableRate !== 0
                        ? (netQty * rate) / taxableRate
                        : 0;
                }
                else {
                    amount = netQty * rate;
                }
            }
            else
            {
                amount = netQty * rate;
            }
        }

        // ----------------------------------
        // 3. Packing and discount
        // ----------------------------------
        const packPer = num('.TxtPacKPer');
        const disPer = num('.TxtDisPer');

        const packAmount = amount * packPer / 100;
        const baseAmount = amount + packAmount;
        const discountAmount = baseAmount * disPer / 100;

        // Cash discount is calculated on amount + packing amount
        const cashDiscountPer = parseFloat($('#NumOtherCashDiscount1').val()) || 0;

        const cashDiscountAmount =  baseAmount * cashDiscountPer / 100;

        // ----------------------------------
        // 4. Taxable amount
        // ----------------------------------
        let taxableAmount = amount + packAmount - discountAmount - cashDiscountAmount;

        const insurance = num('.TxtInsuAmount');
        const freight = num('.TxtFreightAmount');

        if (taxOnInsurance) {
            taxableAmount += insurance;
        }

        if (taxOnFreight) {
            taxableAmount += freight;
        }

        // ----------------------------------
        // 5. Tax calculation
        // ----------------------------------
        const cgstPer = num('.TxtCgstper');
        const sgstPer = num('.TxtSgstPer');
        const igstPer = num('.TxtIGSTPer');
        const cessPer = num('.TxtCessPer');

        // VB.NET calculates tax amounts only when Amount > 0
        const taxableForTax = amount > 0 ? taxableAmount : 0;

        const cgstAmount = taxableForTax * cgstPer / 100;
        const sgstAmount = taxableForTax * sgstPer / 100;
        const igstAmount = taxableForTax * igstPer / 100;
        const cessAmount = taxableForTax * cessPer / 100;

        // ----------------------------------
        // 6. Update current row
        // ----------------------------------
        setVal('.TxtAmount', amount);
        setVal('.TxtPackAmount', packAmount);
        setVal('.TxtDisAmount', discountAmount);
        setVal('.TxtCDiscAmount', cashDiscountAmount);
        setVal('.TxtCgstAmt', cgstAmount);
        setVal('.TxtSgstamt', sgstAmount);
        setVal('.TxtIGSTamt', igstAmount);
        setVal('.TxtCessamt', cessAmount);

        // ----------------------------------
        // 7. Recalculate totals for all rows
        // ----------------------------------
        let totalNos = 0;
        let totalGrossQty = 0;
        let totalNetQty = 0;

        let totalAmount = 0;
        let totalPackAmount = 0;
        let totalDiscount = 0;
        let totalCashDiscount = 0;

        let totalCGST = 0;
        let totalSGST = 0;
        let totalIGST = 0;
        let totalCess = 0;

        $('#tblSalesInvoice tbody tr').each(function () {
            const $r = $(this);

            if (!$r.find('.ddlProductName').val()) {
                return;
            }

            totalNos += num('.TxtNos', $r);
            totalGrossQty += num('.TxtGrossQty', $r);
            totalNetQty += num('.TxtNetQty', $r);
            totalAmount += num('.TxtAmount', $r);
            totalPackAmount += num('.TxtPackAmount', $r);
            totalDiscount += num('.TxtDisAmount', $r);
            totalCashDiscount += num('.TxtCDiscAmount', $r);
            totalCGST += num('.TxtCgstAmt', $r);
            totalSGST += num('.TxtSgstamt', $r);
            totalIGST += num('.TxtIGSTamt', $r);
            totalCess += num('.TxtCessamt', $r);
        });

        // ----------------------------------
        // 8. Subtotal and amount before TCS
        // ----------------------------------
        const subtotal = totalAmount + totalPackAmount - totalDiscount - totalCashDiscount;
        let billAmount = subtotal + totalCGST + totalSGST + totalIGST + totalCess;
        // ----------------------------------
        // 9. Insurance calculation
        // Matches VB.NET insurance formula
        // ----------------------------------
        const insPer =  parseFloat($('#NumInsurance1').val()) || 0;

        const headerFreight = parseFloat($('#NumConsFreight').val()) || parseFloat($('#NumFreightAmount').val()) || 0;
        // VB.NET uses the existing net amount in this branch
        const existingNetAmount =  parseFloat($('#NumNetAmount').val()) || 0;

        let insuranceAmount;

        if (taxOnInsurance)
        {
            insuranceAmount = Math.ceil((subtotal + headerFreight) * (insPer / 100000));
        }
        else
        {
            insuranceAmount = Math.ceil( (existingNetAmount + headerFreight) *  (insPer / 100000) );
        }

        // ----------------------------------
        // 10. Include insurance/freight in bill
        // ----------------------------------
        if (insuranceInBill)
        {
            billAmount += insuranceAmount;
        }

        if (freightInBill)
        {
            billAmount += headerFreight;
        }

        // ----------------------------------
        // 11. TCS and net amount
        // ----------------------------------
        const tcsPer = parseFloat($('#NumTCS1').val()) || 0;

        const tcsAmount = Math.ceil(  billAmount * tcsPer * 0.01 );

        const netAmount = Math.round(  billAmount + tcsAmount );

        // ----------------------------------
        // 12. Round-off
        // Matches VB.NET condition order
        // ----------------------------------
        let roundOff = netAmount - ( subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount );

        if (insuranceInBill)
        {
            roundOff = netAmount - ( subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount +  insuranceAmount  );
        }

        if (freightInBill) {
            roundOff = netAmount - ( subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount + headerFreight );
        }

        if (insuranceInBill && freightInBill) {
            roundOff = netAmount - (   subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount +  insuranceAmount + headerFreight );
        }

        // ----------------------------------
        // 13. TDS
        // ----------------------------------
        const tdsPer =  parseFloat($('#NumTDS1').val()) || 0;

        const tdsAmount = Math.round(  headerFreight * (tdsPer / 100)  );

        // ----------------------------------
        // 14. Update header totals
        // ----------------------------------
        $('#NumOtherTotalAmount').val(totalAmount.toFixed(2));
        $('#NumOtherPacking2').val(totalPackAmount.toFixed(2));
        $('#NumOtherDiscount2').val(totalDiscount.toFixed(2));
        $('#NumOtherCashDiscount2').val(totalCashDiscount.toFixed(2)); 
        $('#NumOtherTotalNos').val(totalNos.toFixed(2));
        $('#NumGrossQty').val(totalGrossQty.toFixed(2));
        $('#NumNetQty').val(totalNetQty.toFixed(2));
        $('#NumOtherSubTotal').val(subtotal.toFixed(2));
        $('#NumOtherCGST2').val(totalCGST.toFixed(2));
        $('#NumOtherSGST2').val(totalSGST.toFixed(2));
        $('#NumOtherIGST2').val(totalIGST.toFixed(2));
        $('#NumOtherCESS2').val(totalCess.toFixed(2));
        $('#NumOtherTCS2').val(tcsAmount.toFixed(2));
        $('#NumOtherRoundOff').val(roundOff.toFixed(2));
        $('#NumOtherNetAmount').val(netAmount.toFixed(2));
        $('#NumInsurance2').val(insuranceAmount.toFixed(2));
        $('#NumTDS2').val(tdsAmount.toFixed(2));

    } catch (error) {
        console.error('CalculateRow error:', error);
    }
}
function getString(selector) {
    const value = $.trim($(selector).val() || "");
    return value === "" ? null : value;
}
function getInt(selector) {
    const value = $.trim($(selector).val() || "");

    if (value === "") {
        return null;
    }

    const number = parseInt(value, 10);

    return Number.isNaN(number) ? null : number;
}
function getDecimal(selector) {
    const value = $.trim($(selector).val() || "");

    if (value === "") {
        return null;
    }

    const number = parseFloat(value);

    return Number.isNaN(number) ? null : number;
}
function getDate(selector) {
    const value = $.trim($(selector).val() || "");

    if (value === "") {
        return null;
    }

    return formatDate(value);
}

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocumentDate").val(),
        vtype: $("#ddlDocumentType").val(),
        vno: $("#NumInvoiceNo").val()
    };
    try {
        const response = await fetch('/SalesInvoiceDirect/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.status === false) {
            showToast("result.message", { type: "warning" });
            return false;
        }

        return true;

    } catch (error) {
        showToast("result.message", { type: "warning" });
        return false;
    }
} 
function LimitNumberLength(input, maxLength)
{
    let value = input.value.toString();

    if (value.length > maxLength) {
        input.value = value.substring(0, maxLength);
    }
}
function GetTableDetails() {
    const details = [];

    $('#tblSalesInvoice tbody tr').each(function (index) {

        const $row = $(this);

        const getValue = (selector) => {
            return $.trim($row.find(selector).val() || '');
        };

        const getDecimal = (selector) => {
            const value = getValue(selector);
            return value === '' ? null : parseFloat(value);
        };

        const getInt = (selector) => {
            const value = getValue(selector);
            return value === '' ? null : parseInt(value, 10);
        };

        const detail = {
            ITEM_CODE: getInt('.ddlProductName'),
            ITEM_NAME: $row.find('.ddlProductName option:selected').text().trim(),
            NOS: getInt('.TxtNos'),
            GROSS_QTY: getDecimal('.TxtGrossQty'),
            QTY: getDecimal('.TxtNetQty'),
            RATE: getDecimal('.TxtRate'),
            AMOUNT: getDecimal('.TxtAmount'),
            PACK_PER: getDecimal('.TxtPacKPer'),
            PACK_AMT: getDecimal('.TxtPackAmount'),
            PACKING_WT: getDecimal('.TxtPackWeight'),
            DISC_PER: getDecimal('.TxtDisPer'),
            DISC_AMT: getDecimal('.TxtDisAmount'),
            TAX_CODE: getInt('.TxtTaxType'),
            CGST_PER: getDecimal('.TxtCgstper'),
            CGST_AMT: getDecimal('.TxtCgstAmt'),
            SGST_PER: getDecimal('.TxtSgstPer'),
            SGST_AMT: getDecimal('.TxtSgstamt'),
            IGST_PER: getDecimal('.TxtIGSTPer'),
            IGST_AMT: getDecimal('.TxtIGSTamt'),
            CESS_PER: getDecimal('.TxtCessPer'),
            CESS_AMT: getDecimal('.TxtCessamt'),
            REMARK: getValue('.TxtRemark'),
            PACK_NO: getInt('.TxtPackno'),
            LOT_No: getValue('.TxtLotNo'),
            SAUDA_TYPE: getValue('.TxtSaudaType'),
            SAUDA_NO: getInt('.TxtSaudaNo'),
            SAUDA_RATE: getDecimal('.TxtSaudaRate'),
            ORD_TYPE: getValue('.TxtOrderType'),
            ORD_NO: getInt('.TxtOrderNo'),
            ORD_RATE: getDecimal('.TxtOrderRate'),
            HSN_CODE: getValue('.TxtHsnCode'),
            FREIGHT_AMT: getDecimal('.TxtFreightAmount'),
            INSU_AMT: getDecimal('.TxtInsuAmount'),
            CDISC_AMT: getDecimal('.TxtCDiscAmount'),
            WBQTY: getDecimal('.TxtWBQuantity'),
            REPORT_TYPE: getValue('.TxtREPORT_TYPE'),
            Sale_Rate: getDecimal('.TxtSale_Rate'),
            Taxable_Rate: getDecimal('.TxtTaxable_Rate'),
            Net_Wt: getDecimal('.TxtNet_Wt'),
            Packing_nos: getDecimal('.TxtPacking_nos'),
            SNO: index + 1
        };

        // Add only rows having a product
        if (detail.ITEM_CODE !== null) {
            details.push(detail);
        }
    });

    return details;
}

async function LoadData() {
    try {

        const res = await $.ajax({
            url: '/SalesInvoiceDirectList/GetDataByCode',
            type: 'Post',
            data: { DOC_ID: rowId }
        });

        console.log("LoadData response:", res);

        let Header = res.data.header;
        let Details = res.data.details;

        console.log("Header", Header);
        console.log("Details", Details);
    
        // Header


        await Promise.all([
             cmbPartyAddress(Header?.bilL_CODE || ''),
            cmbConsigneeAddress(Header?.shiP_CODE || '')         
        ])


        $('#TxtCode').val(Header.doC_ID);
        $('#ddlDocumentType').val(Header.v_TYPE);
        $('#NumInvoiceNo').val(Header.v_NO);
        $('#DtDocumentDate').val(formatDate(Header.v_DATE));
        $('#ddlPartyName').val(Header.bilL_CODE || '').trigger('change');
        $('#TxtAddressL1').val(Header.bilL_ADD1);
        $('#TxtAddressL2').val(Header.bilL_ADD2);
        $('#TxtAddressL3').val(Header.bilL_ADD3);
        $('#NumGSTNoL').val(Header.bilL_GST);
        $('#ddlStation').val(Header.bilL_CITY);
        $('#NumPincode').val(Header.bilL_PINCODE);
        $('#ddlDONo').val(Header.do_NO || '').trigger('change');


        $('#ddlSalesThrough').val(Header.agenT_CODE || '').trigger('change');
        $('#ddlSupplyType').val(Header.supplY_TYPE);
        $('#ddlConsignee').val(Header.shiP_CODE || '').trigger('change');
        $('#TxtSupplyAddressL1').val(Header.shiP_ADD1);
        $('#TxtSupplyAddressL2').val(Header.shiP_ADD2);
        $('#TxtSupplyAddressL3').val(Header.shiP_ADD3);
        $('#NumGSTNo').val(Header.shiP_GST);
        $('#ddlSupplyStation').val(Header.shiP_CITY);
        $('#NumSupplyPIN').val(Header.shiP_PINCODE);
        $('#ddlFormType').val(Header.forM_CODE);

        $('#ddlTransactionType').val(Header.traN_TYPE);
        $('#ddlTaxType').val(Header.taX_CODE);
        $('#ddlProductType').val(Header.taX_CODE);
        $('#ddlPurchaseNo').val(Header.taX_CODE);
        $('#TxtTransRemarks').val(Header.remark);

        $('#NumOtherTotalAmount').val(Header.amount);
        $('#NumOtherPacking1').val(Header.pacK_PER);
        $('#NumOtherPacking2').val(Header.pacK_AMT);
        $('#NumOtherDiscount1').val(Header.disC_PER);
        $('#NumOtherDiscount2').val(Header.disC_AMT);
        $('#NumOtherCashDiscount1').val(Header.cdisC_PER);
        $('#NumOtherCashDiscount2').val(Header.cdisC_AMT);

        $('#NumOtherCGST1').val(Header.cgsT_PER);
        $('#NumOtherCGST2').val(Header.cgsT_AMT);
        $('#NumOtherSGST1').val(Header.sgsT_PER);
        $('#NumOtherSGST2').val(Header.sgsT_AMT);
        $('#NumOtherIGST1').val(Header.igsT_PER);
        $('#NumOtherIGST2').val(Header.igsT_AMT);

        $('#NumOtherTotalNos').val(Header.toT_NOS);

        $('#NumOtherCESS1').val(Header.cesS_PER);
        $('#NumOtherCESS2').val(Header.cesS_AMT);
        $('#NumOtherTCS1').val(Header.tcS_PER);
        $('#NumOtherTCS2').val(Header.tcS_AMT);
        $('#NumOtherRoundOff').val(Header.rounD_OFF);
        $('#NumOtherNetAmount').val(Header.namount);
        $('#ddlDocStatus').val(Header.status);
        $('#NumGrossQty').val(Header.toT_GROSS);
        $('#NumNetQty').val(Header.toT_NET);
        $('#NumWbQty').val(Header.wB_QTY);
        $('#ddlTransport').val(Header.transporT_CODE);
        $('#NumGRNo').val(Header.gR_NO);
        $('#DtGRDate').val(formatDate(Header.gR_DATE));
        if (Header.gR_DATE != null && Header.gR_DATE !== '') {
            $('#chkGRDate').prop('checked', true);
        } else {
            $('#chkGRDate').prop('checked', false);
        }

        $('#TxtTruckNo').val(Header.vehiclE_NO);
        $('#TxtDriverName').val(Header.driveR_NAME);
        $('#NumDriverMob').val(Header.driveR_NO);
        $('#NumInsurance1').val(Header.insU_PER);
        $('#NumInsurance2').val(Header.insU_AMT);
        $('#NumTDSFreight1').val(Header.tdS_PER);
        $('#NumTDSFreight2').val(Header.tdS_AMT);
        $('#ddlSaudaNo').val(Header.saudA_NO || '').trigger('change');
        $('#ddlPurchaseNo').val(Header.issuE_NO || '').trigger('change');
        $('#ddlGodown').val(Header.godowN_CODE);
        $('#ChkDefectiveGoods').prop('checked', Header.defectivE_GOODS == 1);
        $('#ChkPCS').prop('checked', Header.caL_ONPCS == 1);
        $('#ChkDetail').prop('checked', Header.prinT_DETAIL == 1);
        $('#ddlProductType').val(Header.iteM_TYPE);
        $('#txt_saudarate').val(Header.saudA_RATE);      
        $('#ddlWBNo').val(Header.wB_NO || '').trigger('change');    
        $('#NumFreightAmount').val(Header.frT_AMT);
        $('#NumDistance').val(Header.tpT_DISTANCE);
        $('#ddlMode').val(Header.tpT_MODE);
        $('#ddlPaymentTerm').val(Header.paY_TERM);
        $('#TxtDeliveryTerm').val(Header.deliverY_TERMS);
        $('#NumTaxFreight1').val(Header.frT_TAXPER);
        $('#NumTaxFreight2').val(Header.frT_TAXAMT);
        $('#txt_WayBillNo').val(Header.waybilL_NO);
        $('#txt_ToPayFrt').val(Header.frT_TOPAY);
        $('#txt_loadPer').val(Header.loaD_PER);   
        $('#txt_LoadAmt').val(Header.loaD_AMT);
        $('#txt_ldRemark').val(Header.loaD_REM);
        $('#ddl_LoadParty').val(Header.loaD_AC);
        $('#txt_WBamt').val(Header.wB_AMT);
        $('#txt_wbRemark').val(Header.wB_REM);
        $('#ddl_WBParty').val(Header.wB_AC);

      
        // Details
        $('#tblSalesInvoice tbody').empty();

        if (Array.isArray(Details) && Details.length > 0) {

            Details.forEach(item => {

                AddRow({
                    ID: item.iteM_CODE ?? '',
                    Productcode: item.iteM_CODE ?? '',
                    nos: item.nos ?? '',
                    grossQty: item.grosS_QTY ?? '',
                    NetQty: item.qty ?? '',
                    RateINUSD: item.foR_RATE ?? item.foR_RATE ?? '',
                    Rate: item.rate ?? item.rate ?? '',
                    Amount: item.amount ?? '',
                    PackPer: item.pacK_PER ?? '',
                    PackAmt: item.pacK_AMT ?? '',
                    PackWght: item.packinG_WT ?? '',
                    DisPer: item.disC_PER ?? '',
                    Disamt: item.disC_AMT ?? '',
                    TaxType: item.taX_CODE ?? '',
                    CgstPer: item.cgsT_PER ?? '',
                    CgstAmt: item.cgsT_AMT ?? '',
                    SgstPer: item.sgsT_PER ?? '',
                    SgstAmt: item.sgsT_AMT ?? '',
                    IgstPer: item.igsT_PER ?? '',
                    IgstAmt: item.igsT_AMT ?? '',
                    CessPer: item.cesS_PER ?? '',
                    CessAmt: item.cesS_AMT ?? '',
                    Remark: item.remark ?? '',
                    Packno: item.pacK_NO ?? '',
                    Lotno: item.loT_No ?? '',
                    SaudaType: item.saudA_TYPE ?? '',
                    SaudaNo: item.saudA_NO ?? '',
                    SaudaRate: item.saudA_RATE ?? '',
                    OrderType: item.orD_TYPE ?? '',
                    OrderNo: item.orD_NO ?? '',
                    OrderRate: item.orD_RATE ?? '',
                    DocType: item.dcN_TYPE ?? '',
                    DocNo: item.dcN_NO ?? '',
                    HsnCode: item.hsN_CODE ?? '',
                    FreightAmount: item.freighT_AMT ?? '',
                    InsuAmount: item.insU_AMT ?? '',
                    CDiscAmount: item.cdisC_AMT ?? '',
                    WBQuantity: item.wbqty ?? '',
                    itemCode: item.iteM_CODE ?? '',

                    REPORT_TYPE: item.reporT_TYPE ?? '',
                    Sale_Rate: item.sale_Rate ?? '',
                    Taxable_Rate: item.taxable_Rate ?? '',
                    Net_Wt: item.net_Wt ?? '',
                    Packing_Wt: item.packinG_WT ?? '',
                    Packing_nos: item.packing_nos ?? ''

                });

            });
        }
    }
    catch (error) {
        console.error("Error loading data:", error);
    }
}

async function GetCalRate(Itemcode) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoiceDirect/CalRate',
            type: 'GET',
            data: { Itemcode: Itemcode }
        });

        console.log('CalRate Response:', res);

        return res;   // important
    }
    catch (error) {
        console.log("GetCalRate Error:", error);
        return null;
    }
}