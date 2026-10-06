
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

        if (data && data.length > 0) {

            const item = data[0];

            $row.find('.TxtREPORT_TYPE').val(item.reporT_TYPE);
            $row.find('.TxtSale_Rate').val(item.sale_Rate);
            $row.find('.TxtTaxable_Rate').val(item.taxable_Rate);
            $row.find('.TxtNet_Wt').val(item.net_Wt);
            $row.find('.TxtPacking_Wt').val(item.packing_Wt);
            $row.find('.TxtPacking_nos').val(item.packing_nos);
        }
    });
    $row.find('.TxtTaxType').on('change', function ()
    {
        const selectedCode = $(this).val();
        const selectedTax = TaxPercentageData.find(x => String(x.code) === String(selectedCode));
        console.log("Selected Tax Type:", selectedTax);
        if (!selectedTax) {
            $row.find('.TxtCgstper').val('0.00');
            $row.find('.TxtSgstPer').val('0.00');
            $row.find('.TxtIGSTPer').val('0.00'); 
            return;
        }
        $row.find('.TxtCgstper').val(Number(selectedTax.cgsT_PER || 0).toFixed(2));
        $row.find('.TxtSgstPer').val(Number(selectedTax.sgsT_PER || 0).toFixed(2));
        $row.find('.TxtIGSTPer').val(Number(selectedTax.igsT_PER || 0).toFixed(2));
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
        $row.find('.TxtIGSTPer') .val($previousRow.find('.TxtIGSTPer').val());
        CalculateRow($row);
    });

}

function CalculateRow($row) {

    const chkCalPCS = $('#ChkPCS').is(':checked');
    const defTonnageRate = String(LoadGeneralSetting?.pubDefTonnageRate ?? '').toLowerCase();
    const defWtCalconBales = String(LoadGeneralSetting?.pubDefWtCalconBales ?? '').toLowerCase();

    const num = (selector, row = $row) => {
        const value = parseFloat(row.find(selector).val());
        return isNaN(value) ? 0 : value;
    };

    let nos = num('.TxtNos');
    let grossQty = num('.TxtGrossQty');
    let netQty = num('.TxtNetQty');
    let rate = num('.TxtRate');
    let packPer = num('.TxtPacKPer');
    let disPer = num('.TxtDisPer');
    let reportType = $.trim($row.find('.TxtREPORT_TYPE').val() || '');
    let taxableRate = num('.TxtTaxable_Rate');
    let netWt = num('.TxtNet_Wt');
    let packingWt = num('.TxtPacking_Wt');
    let packingNos = num('.TxtPacking_nos');

    if (defTonnageRate === 'yes') {

        if (netWt > 0) {

            grossQty = nos * netWt;

            $row.find('.TxtGrossQty').val(grossQty.toFixed(2));
        }

        if (packingNos > 0) {

            netQty = nos * packingNos;

            $row.find('.TxtNetQty').val(netQty.toFixed(2));
        }
    }

    if (defWtCalconBales === 'yes') {

        if (netWt > 0) {

            grossQty = nos * netWt;

            $row.find('.TxtGrossQty').val(grossQty.toFixed(2));
        }


        if (packingWt > 0) {

            netQty = nos * packingWt;

            $row.find('.TxtNetQty').val(netQty.toFixed(2));
        }
    }

    grossQty = num('.TxtGrossQty');
    netQty = num('.TxtNetQty');

    let amount = 0;

    if (!chkCalPCS) {

        if (netQty > 0) {
            if (defTonnageRate === 'yes') {
                if (reportType === 'Hessian' || reportType === 'Sacking') {
                    amount = (netQty * rate) / 100;
                }

                else if (reportType === 'Twine' && netWt > 0) {
                    if (taxableRate > 0) {
                        amount = (netQty * rate) / taxableRate;
                    }
                }

                else {
                    amount = netQty * rate;
                }
            }

            else {
                amount = netQty * rate;
            }
        }
    }


    else {

        if (nos > 0) {
            amount = grossQty * rate;
        }
    }

    let packAmount = amount * packPer / 100;

    let baseAmount = amount + packAmount;

    let discountAmount = baseAmount * disPer / 100;

    const cashDiscountPer = parseFloat($('#NumCashDiscountPer').val()) || 0;

    let cashDiscountAmount = baseAmount * cashDiscountPer / 100;

    let taxableAmount = amount + packAmount - discountAmount - cashDiscountAmount;

    const taxOnInsurance = String(LoadGeneralSetting?.pubDefTaxonInsuInSI ?? '').toLowerCase() === 'yes';

    const taxOnFreight = String(LoadGeneralSetting?.pubDefTaxonFrtInSI ?? '').toLowerCase() === 'yes';

    const insurance = num('.TxtInsuAmount');

    const freight = num('.TxtFreightAmount');


    if (taxOnInsurance) {
        taxableAmount += insurance;
    }

    if (taxOnFreight) {
        taxableAmount += freight;
    }

    let cgstPer = num('.TxtCgstper');

    let sgstPer = num('.TxtSgstPer');

    let igstPer = num('.TxtIGSTPer');

    let cessPer = num('.TxtCessPer');

    let cgstAmount = taxableAmount * cgstPer / 100;

    let sgstAmount = taxableAmount * sgstPer / 100;

    let igstAmount = taxableAmount * igstPer / 100;

    let cessAmount = taxableAmount * cessPer / 100;

    $row.find('.TxtAmount').val(amount.toFixed(2));

    $row.find('.TxtPackAmount').val(packAmount.toFixed(2));

    $row.find('.TxtDisAmount').val(discountAmount.toFixed(2));

    $row.find('.TxtCDiscAmount').val(cashDiscountAmount.toFixed(2));

    $row.find('.TxtCgstAmt').val(cgstAmount.toFixed(2));

    $row.find('.TxtSgstamt').val(sgstAmount.toFixed(2));

    $row.find('.TxtIGSTamt').val(igstAmount.toFixed(2));

    $row.find('.TxtCessamt').val(cessAmount.toFixed(2));

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

    let subtotal = totalAmount + totalPackAmount - totalDiscount - totalCashDiscount;

    let totalBeforeTCS = subtotal + totalCGST + totalSGST + totalIGST + totalCess;

    const insPer = parseFloat($('#NumInsurance1').val()) || 0;

    let insuranceAmount = 0;

    if (freight !== 0) {
        if (taxOnInsurance) {
            insuranceAmount = Math.ceil((subtotal + freight) * (insPer / 100000));
        }
        else {
            insuranceAmount = Math.ceil(((parseFloat($('#NumNetAmount').val()) || 0) + freight) * (insPer / 100000));
        }
    }
    else {
        insuranceAmount = 0;
    }

    let billAmount = totalBeforeTCS;

    const insuranceInBill = String(LoadGeneralSetting?.pubDefInsuInBillAmtInSI ?? '').toLowerCase() === 'yes';

    const freightInBill = String(LoadGeneralSetting?.pubDefFrtInBillAmtInSI ?? '').toLowerCase() === 'yes';

    if (insuranceInBill) {
        billAmount += insuranceAmount;
    }

    if (freightInBill) {

        billAmount += freight;
    }

    const tcsPer = parseFloat($('#NumTCS1').val()) || 0;


    const tcsAmount = Math.ceil(billAmount * tcsPer * 0.01);

    const netAmount = Math.round(billAmount + tcsAmount);

    let roundOff = netAmount - (subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount);

    if (insuranceInBill) {
        roundOff = netAmount - (subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount + insuranceAmount);
    }

    if (freightInBill) {
        roundOff = netAmount - (subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount + freight);
    }

    if (insuranceInBill && freightInBill) {
        roundOff = netAmount - (subtotal + totalCGST + totalSGST + totalIGST + totalCess + tcsAmount + insuranceAmount + freight);
    }

    const tdsPer = parseFloat($('#NumTDS1').val()) || 0;

    const tdsAmount = Math.round(freight * (tdsPer / 100));

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
}

function GetSalesInvoiceDetails() {

    const details = [];

    $('#tblSalesInvoice tbody tr').each(function () {
        const $row = $(this);
        const detail = {
            ITEM_CODE: parseInt($.trim($row.find('.ddlProductName').val()), 10) || 0,
            ITEM_NAME: $.trim($row.find('.ddlProductName option:selected').text()) || "",
            NOS: parseInt($.trim($row.find('.TxtNos').val()), 10) || 0,
            GROSS_QTY: parseFloat($.trim($row.find('.TxtGrossQty').val())) || 0,
            QTY: parseFloat($.trim($row.find('.TxtNetQty').val())) || 0,
            FOR_RATE: parseFloat($.trim($row.find('.TxtRateINUSD').val())) || 0,
            RATE: parseFloat($.trim($row.find('.TxtRate').val())) || 0,
            AMOUNT: parseFloat($.trim($row.find('.TxtAmount').val())) || 0,
            PACK_PER: parseFloat($.trim($row.find('.TxtPacKPer').val())) || 0,
            PACK_AMT: parseFloat($.trim($row.find('.TxtPackAmount').val())) || 0,
            PACKING_WT: parseFloat($.trim($row.find('.TxtPackWeight').val())) || 0,
            DISC_PER: parseFloat($.trim($row.find('.TxtDisPer').val())) || 0,
            DISC_AMT: parseFloat($.trim($row.find('.TxtDisAmount').val())) || 0,
            TAX_CODE: parseInt($.trim($row.find('.TxtTaxType').val()), 10) || 0,
            CGST_PER: parseFloat($.trim($row.find('.TxtCgstper').val())) || 0,
            CGST_AMT: parseFloat($.trim($row.find('.TxtCgstAmt').val())) || 0,
            SGST_PER: parseFloat($.trim($row.find('.TxtSgstPer').val())) || 0,
            SGST_AMT: parseFloat($.trim($row.find('.TxtSgstamt').val())) || 0,
            IGST_PER: parseFloat($.trim($row.find('.TxtIGSTPer').val())) || 0,
            IGST_AMT: parseFloat($.trim($row.find('.TxtIGSTamt').val())) || 0,
            CESS_PER: parseFloat($.trim($row.find('.TxtCessPer').val())) || 0,
            CESS_AMT: parseFloat($.trim($row.find('.TxtCessamt').val())) || 0,
            REMARK: $.trim($row.find('.TxtRemark').val()) || "",
            PACK_NO: parseInt($.trim($row.find('.TxtPackno').val()), 10) || 0,
            LOT_No: $.trim($row.find('.TxtLotNo').val()) || "",
            SAUDA_TYPE: $.trim($row.find('.TxtSaudaType').val()) || "",
            SAUDA_NO: parseInt($.trim($row.find('.TxtSaudaNo').val()), 10) || 0,
            SAUDA_RATE: parseFloat($.trim($row.find('.TxtSaudaRate').val())) || 0,
            ORD_TYPE: $.trim($row.find('.TxtOrderType').val()) || "",
            ORD_NO: parseInt($.trim($row.find('.TxtOrderNo').val()), 10) || 0,
            ORD_RATE: parseFloat($.trim($row.find('.TxtOrderRate').val())) || 0,
            DCN_TYPE: $.trim($row.find('.TxtDocType').val()) || "",
            DCN_NO: parseInt($.trim($row.find('.TxtDocNo').val()), 10) || 0,
            HSN_CODE: $.trim($row.find('.TxtHsnCode').val()) || "",
            FREIGHT_AMT: parseFloat($.trim($row.find('.TxtFreightAmount').val())) || 0,
            INSU_AMT: parseFloat($.trim($row.find('.TxtInsuAmount').val())) || 0,
            CDISC_AMT: parseFloat($.trim($row.find('.TxtCDiscAmount').val())) || 0,
            WBQTY: parseFloat($.trim($row.find('.TxtWBQuantity').val())) || 0
        };

        details.push(detail);
    });

    return details;
}

async function LoadData() {
    try {

        const res = await $.ajax({
            url: '/SalesInvoiceList/GetDataByCode',
            type: 'Post',
            data: { DOC_ID: rowId }
        });

        console.log("LoadData response:", res);

        let Header = res.data.header;
        let Details = res.data.details;

        console.log("Header", Header);
        console.log("Details", Details);
            
        await Promise.all([
            DDlPackNo(Header?.bilL_CODE || ''),
            cmbPartyAddress(Header?.bilL_CODE || ''),
            cmbConsigneeAddress(Header?.shiP_CODE || ''),
            DDlLicNO(Header?.licencE_TYPE || '')

        ])
        // Header

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
        $('#ddlPackNo').val(Header.pacK_NO);
        $('#ddlSaudaNo').val(Header.saudA_NO || '').trigger('change');
        $('#ddlIssueNo').val(Header.issuE_NO || '').trigger('change');
        $('#ddlGodown').val(Header.godowN_CODE);
        $('#TxtTransRemarks').val(Header.remark);
        $('#ChkDefectiveGoods').prop('checked', Header.defectivE_GOODS == 1);
        $('#ChkPCS').prop('checked', Header.caL_ONPCS == 1);
        $('#ChkDetail').prop('checked', Header.prinT_DETAIL == 1);
        $('#ddlProductType').val(Header.iteM_TYPE);
        $('#txt_saudarate').val(Header.saudA_RATE);
        $('#NumOtherTotalAmount').val(Header.amount);
        $('#ddlWBNo').val(Header.wB_NO || '').trigger('change');
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
        $('#NumTaxFreight1').val(Header.frT_TAXPER);
        $('#NumTaxFreight2').val(Header.frT_TAXAMT);
        $('#NumFreightAmount').val(Header.frT_AMT);
        $('#NumDistance').val(Header.tpT_DISTANCE);
        $('#ddlMode').val(Header.tpT_MODE);
        $('#ddlPaymentTerm').val(Header.paymenT_TERM);
        $('#TxtDeliveryTerm').val(Header.deliverY_TERMS);
        $('#txt_WayBillNo').val(Header.waybilL_NO);
        $('#txt_ToPayFrt').val(Header.frT_TOPAY);
        $('#txt_loadPer').val(Header.loaD_PER);
        $('#txt_LoadAmt').val(Header.loaD_AMT);
        $('#txt_ldRemark').val(Header.loaD_REM);
        $('#ddl_LoadParty').val(Header.loaD_AC);
        $('#txt_WBamt').val(Header.wB_AMT);
        $('#txt_wbRemark').val(Header.wB_REM);
        $('#ddl_WBParty').val(Header.wB_AC);

        $('#txxt_ExRate').val(Header.exrate);
        $('#ddl_currency').val(Header.currency);
        $('#txxt_ForIssu').val(Header.foB_INSU);
        $('#txxt_pol').val(Header.porT_LOADING);
        $('#dt_LutDate').val(formatDate(Header.luT_DATE));
        $('#txxt_finalDestination').val(Header.finaL_DEST);
        $('#txxt_ShipBillNo').val(Header.sB_NO);
        $('#dt_ShipDate').val(formatDate(Header.sB_DATE));
        $('#ddllictype').val((Header.licencE_TYPE));
        $('#DDL_LicNo').val((Header.licencE_NO));
        $('#txt_LicDT').val(formatDate(Header.licencE_DATE));
        $('#txxt_FobValue').val((Header.foB_VALUE));
        $('#txxt_FobOther').val((Header.foB_OTHER));
        $('#txxt_POD').val((Header.porT_DISCHARGE));
        $('#txxt_FDCountry').val((Header.finaL_DEST_COUNTRY));
        $('#txxt_PortCode').val((Header.porT_CODE));
        $('#txt_FOBFRT').val((Header.foB_FRT));
        $('#txt_LutNo').val((Header.luT_NO));
        $('#txt_receiptat').val((Header.placE_RECEIPT));
        $('#txt_BillOfLanding').val((Header.billoF_LADING));
        $('#txt_InsuranceDe').val((Header.insU_DETAIL));
        $('#DDL_Bank').val((Header.banK_CODE));

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












                    itemCode: item.iteM_CODE ?? ''
                });

            });
        }
    }
    catch (error) {
        console.error("Error loading data:", error);
    }
}

async function GetPendingDetails() {

    let BillCode = $('#ddlPartyName').val();

    try {

        const res = await $.ajax({
            url: '/SalesInvoiceList/GetPendingDataCode',
            type: 'POST',
            data: {
                BillCode: BillCode
            }
        });

        console.log("Pending Details Response:", res);
            

            ShowPendingDetails(res.data);

     

    }
    catch (error) {

        console.error("GetPendingDetails Error:", error);

        $('#tblpurchaseordermodal tbody').empty();
    }
}
function ShowPendingDetails(data) {

    const $tbody = $('#tblpurchaseordermodal tbody');

    $tbody.empty();

    if (!Array.isArray(data) || data.length === 0) {
        $tbody.append(`
            <tr>
                <td colspan="28" class="text-center">
                    No pending details found
                </td>
            </tr>
        `);
        return;
    }

    data.forEach((item, index) => {
        let V_DATE = '';

        if (item.V_DATE)
        {
            const date = new Date(item.V_DATE);

            V_DATE = String(date.getDate()).padStart(2, '0') + '/' + String(date.getMonth() + 1).padStart(2, '0') + '/' + date.getFullYear();
        }

        const row = `
            <tr data-index="${index}">

                <!-- Select -->
                <td>  <input type="checkbox" class="pending-row-check" data-index="${index}"> </td>
                <td>${item.DOC_ID ?? ''}</td>
                <td>${item.V_TYPE ?? ''}</td>
                <td>${item.V_NO ?? ''}</td>
                <td>${V_DATE}</td>
                <td>${item.Item_Name ?? ''}</td>
                <td>${item.Item_Unit ?? ''}</td>
                <td>${item.HSN_Code ?? ''}</td>
                <td class="text-end">${item.Nos ?? 0}</td>
                <td class="text-end">${item.Gross ?? 0}</td>
                <td class="text-end">${item.Qty ?? 0}</td>
                <td class="text-end">${item.Rate ?? 0}</td>
                <td class="text-end">${item.Amount ?? 0}</td>
                <td class="text-end">${item.Disc_Per ?? 0}</td>
                <td class="text-end">${item.Disc_Amt ?? 0}</td>
                <td class="text-end">${item.CGST_Per ?? 0}</td>
                <td class="text-end">${item.CGST_Amt ?? 0}</td>
                <td class="text-end">${item.SGST_Per ?? 0}</td>
                <td class="text-end">${item.SGST_Amt ?? 0}</td>
                <td class="text-end">${item.IGST_Per ?? 0}</td>
                <td class="text-end">${item.IGST_Amt ?? 0}</td>
                <td class="text-end">${item.PACK_Per ?? 0}</td>
                <td class="text-end">${item.PACK_Amt ?? 0}</td>
                <td>${item.Remark ?? ''}</td>
                <td>${item.Type ?? ''}</td>
                <td class="text-end">${item.SNO ?? ''}</td>
                <td class="text-end">${item.Item_Code ?? ''}</td>
                 <td class="text-end">${item.REPORT_TYPE ?? ''}</td>
                 <td class="text-end">${item.Sale_Rate ?? ''}</td>
                 <td class="text-end">${item.Taxable_Rate ?? ''}</td>
                 <td class="text-end">${item.Net_Wt ?? ''}</td>
                 <td class="text-end">${item.Packing_Wt ?? ''}</td>
                 <td class="text-end">${item.Packing_nos ?? ''}</td>
                <td class="hidden-col"> <button type="button" class="btn btn-sm btn-primary pending-select-btn"  data-index="${index}">  Select </button>  </td>

            </tr>
        `;

        $tbody.append(row);
    });
}
function GetSelectedPendingRow() {

    let selectedRows = [];

    $('#tblpurchaseordermodal tbody .pending-row-check:checked').each(function () {

        let $row = $(this).closest('tr');

        let DOC_ID = $.trim($row.find('td:eq(1)').text());
        let V_TYPE = $.trim($row.find('td:eq(2)').text());
        let V_NO = $.trim($row.find('td:eq(3)').text());
        let V_DATE = $.trim($row.find('td:eq(4)').text());
        let Item_Name = $.trim($row.find('td:eq(5)').text());
        let Item_Unit = $.trim($row.find('td:eq(6)').text());
        let HSN_Code = $.trim($row.find('td:eq(7)').text());
        let Nos = $.trim($row.find('td:eq(8)').text());
        let Gross = $.trim($row.find('td:eq(9)').text());
        let Qty = $.trim($row.find('td:eq(10)').text());
        let Rate = $.trim($row.find('td:eq(11)').text());
        let Amount = $.trim($row.find('td:eq(12)').text());
        let Disc_Per = $.trim($row.find('td:eq(13)').text());
        let Disc_Amt = $.trim($row.find('td:eq(14)').text());
        let CGST_Per = $.trim($row.find('td:eq(15)').text());
        let CGST_Amt = $.trim($row.find('td:eq(16)').text());
        let SGST_Per = $.trim($row.find('td:eq(17)').text());
        let SGST_Amt = $.trim($row.find('td:eq(18)').text());
        let IGST_Per = $.trim($row.find('td:eq(19)').text());
        let IGST_Amt = $.trim($row.find('td:eq(20)').text());
        let PACK_Per = $.trim($row.find('td:eq(21)').text());
        let PACK_Amt = $.trim($row.find('td:eq(22)').text());
        let Remark = $.trim($row.find('td:eq(23)').text());
        let Type = $.trim($row.find('td:eq(24)').text());
        let SNO = $.trim($row.find('td:eq(25)').text());
        let Item_Code = $.trim($row.find('td:eq(26)').text());
        let REPORT_TYPE = $.trim($row.find('td:eq(27)').text());
        let Sale_Rate = $.trim($row.find('td:eq(27)').text());
        let Taxable_Rate = $.trim($row.find('td:eq(28)').text());
        let Net_Wt = $.trim($row.find('td:eq(29)').text());
        let Packing_Wt = $.trim($row.find('td:eq(30)').text());
        let Packing_nos = $.trim($row.find('td:eq(31)').text());

        selectedRows.push({
            DOC_ID,
            V_TYPE,
            V_NO,
            V_DATE,
            Item_Name,
            Item_Unit,
            HSN_Code,
            Nos,
            Gross,
            Qty,
            Rate,
            Amount,
            Disc_Per,
            Disc_Amt,
            CGST_Per,
            CGST_Amt,
            SGST_Per,
            SGST_Amt,
            IGST_Per,
            IGST_Amt,
            PACK_Per,
            PACK_Amt,
            Remark,
            Type,
            SNO,
            Item_Code,
            REPORT_TYPE,
            Sale_Rate,
            Taxable_Rate,
            Net_Wt,
            Packing_Wt,
            Packing_nos
        });

    });

    return selectedRows;
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

async function GetCalRate(Itemcode) {
    try {
        const res = await $.ajax({
            url: '/SalesInvoice/CalRate',
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

function CalculateRow($row)
{

    const chkCalPCS = $('#ChkPCS').is(':checked');
    const defTonnageRate =  String(LoadGeneralSetting?.pubDefTonnageRate ?? '').toLowerCase();
    const defWtCalconBales = String(LoadGeneralSetting?.pubDefWtCalconBales ?? '').toLowerCase();

    const num = (selector, row = $row) => {
        const value = parseFloat(row.find(selector).val());
        return isNaN(value) ? 0 : value;
    };

    let nos = num('.TxtNos');
    let grossQty = num('.TxtGrossQty');
    let netQty = num('.TxtNetQty');
    let rate = num('.TxtRate');
    let packPer = num('.TxtPacKPer');
    let disPer = num('.TxtDisPer');
    let reportType = $.trim($row.find('.TxtREPORT_TYPE').val() || '');
    let taxableRate = num('.TxtTaxable_Rate');
    let netWt = num('.TxtNet_Wt');
    let packingWt = num('.TxtPacking_Wt');
    let packingNos = num('.TxtPacking_nos');

    if (defTonnageRate === 'yes') {

        if (netWt > 0) {

            grossQty = nos * netWt;

            $row.find('.TxtGrossQty') .val(grossQty.toFixed(2));
        }

        if (packingNos > 0) {

            netQty =  nos * packingNos;

            $row.find('.TxtNetQty') .val(netQty.toFixed(2));
        }
    }

    if (defWtCalconBales === 'yes') {

        if (netWt > 0) {

            grossQty = nos * netWt;

            $row.find('.TxtGrossQty') .val(grossQty.toFixed(2));
        }


        if (packingWt > 0) {

            netQty = nos * packingWt;

            $row.find('.TxtNetQty') .val(netQty.toFixed(2));
        }
    }

    grossQty = num('.TxtGrossQty');
    netQty = num('.TxtNetQty');

    let amount = 0;

    if (!chkCalPCS) {

        if (netQty > 0)
        {             
            if (defTonnageRate === 'yes') {
                if (reportType === 'Hessian' || reportType === 'Sacking')
                {
                    amount = (netQty * rate) / 100;
                }

                else if (reportType === 'Twine' && netWt > 0)
                {
                    if (taxableRate > 0)
                    {
                        amount = (netQty * rate) / taxableRate;
                    }
                }
     
                else
                {
                    amount = netQty * rate;
                }
            }

            else
            {
                amount = netQty * rate;
            }
        }
    }


    else {

        if (nos > 0)
        {
            amount =  grossQty * rate;
        }
    }

    let packAmount = amount * packPer / 100;

    let baseAmount = amount + packAmount;

    let discountAmount = baseAmount * disPer / 100;

    const cashDiscountPer = parseFloat($('#NumCashDiscountPer').val()) || 0;

    let cashDiscountAmount =  baseAmount *  cashDiscountPer /  100;

    let taxableAmount =  amount +   packAmount - discountAmount - cashDiscountAmount;

    const taxOnInsurance = String( LoadGeneralSetting?.pubDefTaxonInsuInSI ?? '' ).toLowerCase() === 'yes';

    const taxOnFreight = String( LoadGeneralSetting?.pubDefTaxonFrtInSI ?? ''  ).toLowerCase() === 'yes';

    const insurance =  num('.TxtInsuAmount');

    const freight = num('.TxtFreightAmount');


    if (taxOnInsurance)
    {
        taxableAmount += insurance;
    }

    if (taxOnFreight)
    {
        taxableAmount += freight;
    }

    let cgstPer =  num('.TxtCgstper');

    let sgstPer = num('.TxtSgstPer');

    let igstPer = num('.TxtIGSTPer');

    let cessPer = num('.TxtCessPer');

    let cgstAmount = taxableAmount * cgstPer / 100;

    let sgstAmount =  taxableAmount * sgstPer / 100;

    let igstAmount =  taxableAmount * igstPer / 100;

    let cessAmount =  taxableAmount * cessPer / 100;

    $row.find('.TxtAmount') .val(amount.toFixed(2));

    $row.find('.TxtPackAmount') .val(packAmount.toFixed(2));

    $row.find('.TxtDisAmount') .val(discountAmount.toFixed(2));

    $row.find('.TxtCDiscAmount') .val(cashDiscountAmount.toFixed(2));

    $row.find('.TxtCgstAmt') .val(cgstAmount.toFixed(2));

    $row.find('.TxtSgstamt') .val(sgstAmount.toFixed(2));

    $row.find('.TxtIGSTamt') .val(igstAmount.toFixed(2));

    $row.find('.TxtCessamt') .val(cessAmount.toFixed(2));

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


    $('#tblSalesInvoice tbody tr').each(function ()
    {

        const $r = $(this);

        if (!$r.find('.ddlProductName').val()) {
            return;
        }

        totalNos += num('.TxtNos', $r);

        totalGrossQty +=  num('.TxtGrossQty', $r);

        totalNetQty +=  num('.TxtNetQty', $r);

        totalAmount += num('.TxtAmount', $r);

        totalPackAmount +=  num('.TxtPackAmount', $r);

        totalDiscount += num('.TxtDisAmount', $r);

        totalCashDiscount +=  num('.TxtCDiscAmount', $r);

        totalCGST += num('.TxtCgstAmt', $r);

        totalSGST += num('.TxtSgstamt', $r);

        totalIGST +=  num('.TxtIGSTamt', $r);

        totalCess +=  num('.TxtCessamt', $r);
    });

    let subtotal =  totalAmount +  totalPackAmount -   totalDiscount - totalCashDiscount;

    let totalBeforeTCS = subtotal + totalCGST + totalSGST +  totalIGST +  totalCess;

    const insPer = parseFloat($('#NumInsurance1').val()) || 0;

    let insuranceAmount = 0;

    if (freight !== 0)
    {
        if (taxOnInsurance)
        {
            insuranceAmount = Math.ceil(  (  subtotal + freight  ) *  (insPer / 100000) );
        }
        else
        {
            insuranceAmount = Math.ceil( ( (parseFloat($('#NumNetAmount').val()) || 0) + freight ) * (insPer / 100000) );
        }
    }
    else
    {
        insuranceAmount = 0;
    }

    let billAmount = totalBeforeTCS;

    const insuranceInBill =  String( LoadGeneralSetting?.pubDefInsuInBillAmtInSI ?? '' ).toLowerCase() === 'yes';

    const freightInBill =  String( LoadGeneralSetting?.pubDefFrtInBillAmtInSI ?? ''  ).toLowerCase() === 'yes';

    if (insuranceInBill)
    {
        billAmount += insuranceAmount;
    }

    if (freightInBill) {

        billAmount +=  freight;
    }

    const tcsPer = parseFloat($('#NumTCS1').val()) || 0;


    const tcsAmount = Math.ceil(  billAmount * tcsPer * 0.01  );

    const netAmount = Math.round(billAmount + tcsAmount);

    let roundOff =  netAmount - ( subtotal + totalCGST + totalSGST +   totalIGST +  totalCess + tcsAmount );

    if (insuranceInBill)
    {
        roundOff = netAmount -  (  subtotal + totalCGST +  totalSGST +  totalIGST +  totalCess +  tcsAmount +  insuranceAmount );
    }

    if (freightInBill)
    {
        roundOff =  netAmount -  (  subtotal +  totalCGST + totalSGST +  totalIGST +  totalCess +  tcsAmount +  freight  );
    }

    if (insuranceInBill && freightInBill)

    {
        roundOff = netAmount -  (  subtotal +  totalCGST + totalSGST + totalIGST + totalCess +  tcsAmount + insuranceAmount + freight );
    }

    const tdsPer = parseFloat($('#NumTDS1').val()) || 0;

    const tdsAmount = Math.round( freight * (tdsPer / 100)  );

    $('#NumOtherTotalAmount')  .val(totalAmount.toFixed(2));
    $('#NumOtherPacking2')  .val(totalPackAmount.toFixed(2));
    $('#NumOtherDiscount2') .val(totalDiscount.toFixed(2));
    $('#NumOtherCashDiscount2') .val(totalCashDiscount.toFixed(2));
    $('#NumOtherTotalNos') .val(totalNos.toFixed(2));
    $('#NumGrossQty') .val(totalGrossQty.toFixed(2));
    $('#NumNetQty') .val(totalNetQty.toFixed(2));
    $('#NumOtherSubTotal') .val(subtotal.toFixed(2));
    $('#NumOtherCGST2') .val(totalCGST.toFixed(2));
    $('#NumOtherSGST2')  .val(totalSGST.toFixed(2));
    $('#NumOtherIGST2') .val(totalIGST.toFixed(2));
    $('#NumOtherCESS2') .val(totalCess.toFixed(2));
    $('#NumOtherTCS2')  .val(tcsAmount.toFixed(2));
    $('#NumOtherRoundOff')  .val(roundOff.toFixed(2));
    $('#NumOtherNetAmount')  .val(netAmount.toFixed(2));   
    $('#NumInsurance2') .val(insuranceAmount.toFixed(2));
    $('#NumTDS2') .val(tdsAmount.toFixed(2));
}

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocumentDate").val(),
        vtype: $("#ddlDocumentType").val(),
        vno: $("#NumInvoiceNo").val()
    };
    try {
        const response = await fetch('/SalesInvoice/CheckValidDate', {
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

async function GetTransitReportFile(citype, exportPrint, si = false, ci = false, lc = false) {

    if (!rowId) {
        showToast("Please save the data before printing the report.", {
            type: "info"
        });
        throw new Error("No docId");
    }

    let reportName = "";
    let RPTNAME = "";

    const v_no = $('#NumInvoiceNo').val();
    const v_type = $('#ddlDocumentType').val();
    const FrtAmt = $('#NumFreightAmount').val();
    const TdsAmt = $('#NumTDSFreight2').val();
    const CGSTAmt = $('#NumCGSTAmount').val();
    const SGSTAmt = $('#NumSGSTAmount').val();
    const IGSTAMT = $('#NumIGSTAmount').val();
    const godownNo = $('#ddlGodown').val();
    const godownType = $('#ddlGodown option:selected').text().trim();
    const PackType =  $.trim($('#ddlPackNo option:selected').text())  .split('-') .pop() .trim() || "";

    const $firstRow = $('#tblSalesInvoice tbody tr').eq(0);
    const SaudaType = $.trim($firstRow.find('.TxtSaudaType').val() || "");
    const SaudaNo = $.trim($firstRow.find('.TxtSaudaNo').val() || "");
    const WithoutBag = $('#chk_PackSlipWithoutBag').is(':checked') ? 1 : 0;
    const cbDetail = $('#ChkDetail').is(':checked') ? 1 : 0;
    const cbWithSign = $('#chk_PrintWithSignature').is(':checked') ? 1 : 0;
    const cbwithFreightFOB = $('#ch_report').is(':checked') ? 1 : 0;

    // ---------------------------------------------------------
    // Get Pack Nos
    // ---------------------------------------------------------

    let packNos = "";

    $('#tblSalesInvoice tbody tr').each(function () {

        const $row = $(this);

        const itemName =
            $.trim($row.find('.ddlProductName').val() || "");

        const packNo =
            $.trim($row.find('.TxtPackno').val() || "");

        if (itemName !== "" && packNo !== "") {
            packNos += packNo + ",";
        }
    });

    packNos = packNos.replace(/,$/, "");

    // ---------------------------------------------------------
    // Print Validation
    // ---------------------------------------------------------

    const validation = await $.ajax({
        url: '/SalesInvoice/PrintValidation',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',

        data: JSON.stringify({

            V_TYPE: v_type,
            V_NO: parseInt(v_no) || null,
            FrtAmt: parseFloat(FrtAmt) || null,
            TdsAmt: parseFloat(TdsAmt) || null,
            CGSTAmt: parseFloat(CGSTAmt) || null,
            SGSTAmt: parseFloat(SGSTAmt) || null,
            IGSTAMT: parseFloat(IGSTAMT) || null,
            PackType: PackType,
            exportPrint: exportPrint,
            packNos: packNos,
            WithoutBag: WithoutBag,
            ci: ci,
            si: si,
            lc: lc,
            cbDetail: cbDetail,
            SaudaType: SaudaType,
            SaudaNo: parseInt(SaudaNo) || null,
            godownNo: godownNo,
            godownType: parseInt(godownType) || null,
            citype: citype
        })
    });


    // ---------------------------------------------------------
    // Validation Response
    // ---------------------------------------------------------

    if (validation.message1 != '')
    {
        showToast(validation.message1, { type: "warning" });
        return;
    }
    if (validation.message2 != '')
    {
        showToast(validation.message2, { type: "warning" });
        return;
    }

    if (validation.message3 != '')
    {
        showToast(validation.message3, { type: "warning" });
        return;
    }

    if (validation.message4 != '')
    {
        showToast(validation.message4, { type: "warning" });
        return;
    }

    if (validation.success == false)
    {
        showToast(validation.message, { type: "warning" });
        return;
    }

    console.log("globalVars:", globalVars);
    console.log("validation:", validation);

    reportName = validation.reportName || "";

    // ---------------------------------------------------------
    // Container Detail Subreport Formula
    // VB.NET Equivalent:
    //
    // Dim subSel As String = ""
    // subSel = "{tempContainerdetail.SI_TYPE}='" & cmbvtype.SelectedValue & "'"
    // subSel &= " AND {tempContainerdetail.SI_NO}=" & txtvno.Text
    // subSel &= " AND {tempContainerdetail.COMP_CODE}=" & pubCompCode
    //
    // RPT.Subreports("rptContainerDetail.rpt").RecordSelectionFormula = subSel
    // ---------------------------------------------------------

    let containerDetailFormula = "";

    if (exportPrint === true)
    {
        containerDetailFormula = "{tempContainerdetail.SI_TYPE} = '" + v_type +  "'" + " AND {tempContainerdetail.SI_NO} = " + (parseInt(v_no) || 0) + " AND {tempContainerdetail.COMP_CODE} = " +
        globalVars.CompCode;
    }


    // ---------------------------------------------------------
    // Report Name
    // ---------------------------------------------------------

    let dft = "";


    if (v_type === "SAGT" || v_type === "SASI" || v_type === "SAST")
    {
        RPTNAME = "TAX INVOICE" + dft;
    }
    else if (v_type === "SACH")
    {
        RPTNAME = "DELIVERY CHALLAN" + dft;
    }
    else if (v_type === "SAJI") {

        RPTNAME = "DELIVERY CHALLAN (JOBWORK)" + dft;
    }
    else {

        RPTNAME = "BILL OF SUPPLY" + dft;
    }


    // ---------------------------------------------------------
    // Main Report Selection Formula
    // ---------------------------------------------------------

    const formula =  "{SALE1.V_TYPE} = '" +  v_type + "'" +
        " AND {SALE1.V_NO} = " +
        (parseInt(v_no) || 0) +
        " AND {SALE1.COMP_CODE} = " +
        globalVars.CompCode +
        " AND {SALE1.YEAR_CODE} = " +
        globalVars.FYearCode +
        " AND {SALE1.BRANCH_CODE} = " +
        globalVars.BranchCode;

    // ---------------------------------------------------------
    // With Signature
    // ---------------------------------------------------------

    let withSign = "";

    if ( cbWithSign === 1 && String(globalVars.CompCode) === "1" ) {

        withSign = "1";
    }


    // ---------------------------------------------------------
    // With Freight
    // ---------------------------------------------------------

    let withFreight = "";

    if (cbwithFreightFOB === 1) {

        withFreight = "1";
    }


    // ---------------------------------------------------------
    // Godown Address
    // ---------------------------------------------------------

    const godownAdd =  validation.godownAdd || "";

    let godownAddText = "";

    if (godownAdd !== "")
    {
        godownAddText = "Ship From : " + godownAdd;
    }

    const txtnet_amt =  parseFloat( $('#NumOtherNetAmount').val() ) || 0;


    const inWord = numberToWordsCurrency(  txtnet_amt, "Rs.", "PAISE"  );



    let INUSD = "";

    const exRate =
        parseFloat(
            $('#NumExRate').val()
        ) || 0;


    const currency =
        $('#ddlCurrency option:selected')
            .text()
            .trim() || "Rs";


    if (exRate > 0) {

        let usdAmt = 0;


        $('#tblSalesInvoice tbody tr').each(function ()
        {

            const $row = $(this);
            const qty = $('#ChkPCS').is(':checked') ? parseFloat(  $row.find('.TxtNos').val()  ) || 0  : parseFloat(  $row.find('.TxtNetQty').val()  ) || 0;
            const rate =  parseFloat(  $row.find('.TxtRate').val() ) || 0;
            usdAmt += qty * rate;
        });


        usdAmt =
            Math.round(
                usdAmt * 1000
            ) / 1000;


        if (usdAmt > 0) {

            INUSD =
                numberToWordsCurrency(
                    Math.round(
                        usdAmt * 100
                    ) / 100,

                    currency,

                    "CENT"
                );

        }
        else {

            INUSD = " .";
        }
    }


    // ---------------------------------------------------------
    // Challan Reference
    // ---------------------------------------------------------

    let challanRef = "";


    if (String(globalVars.CompCode) === "1") {

        const challanNo =
            $.trim(
                validation.challanNo || ""
            );


        if (challanNo.length > 1) {

            challanRef =
                "Agst. Challan No.:" +
                challanNo;
        }
    }


    // ---------------------------------------------------------
    // Company Name 1
    // ---------------------------------------------------------

    let compName1 = "";


    if (
        String(globalVars.CompCode) !== "3" &&
        String(globalVars.CompCode) !== "8"
    ) {

        compName1 =
            "An ISO 9001:2015 Certified Company";
    }


    // ---------------------------------------------------------
    // Company Registration
    // ---------------------------------------------------------

    let compReg = "";


    if (
        String(globalVars.CompCode) !== "3"
    ) {

        compReg =
            "Reg.Office :" +
            (globalVars.RegAdd1 || "") +
            ", " +
            (globalVars.RegAdd2 || "") +
            "  CIN :" +
            (globalVars.CINNO || "");
    }


    // =========================================================
    // PAYLOAD
    // =========================================================

    const payload = {

        // Main Report
        Reportname: reportName,

        // Main Report Record Selection Formula
        selectionFormula: formula,

        // Database
        Database: database,


        // -----------------------------------------------------
        // Parameters
        // -----------------------------------------------------

        Parameters: {

            comp_name:
                globalVars.CompanyName || "",

            comp_add1:
                globalVars.Address1 || "",

            comp_add2:
                globalVars.Address2 || "",

            comp_phone:
                "Mobile : " +
                (globalVars.Phone || ""),

            PAN:
                "PAN NO.   : " +
                (globalVars.PAN || ""),

            EMAIL:
                "Email   : " +
                (globalVars.Email || ""),

            Website:
                "Web     : " +
                (globalVars.pubCompWebsite || ""),

            GST:
                "GST NO.   : " +
                (globalVars.GST || "")
        },


        // =====================================================
        // SUBREPORT
        // =====================================================

        Subreports: {

            "rptContainerDetail.rpt": {

                RecordSelectionFormula:
                    containerDetailFormula
            }
        }
    };


    // ---------------------------------------------------------
    // Optional Parameters
    // ---------------------------------------------------------

    if (withSign !== "") {

        payload.Parameters.withSign =
            withSign;
    }


    if (withFreight !== "") {

        payload.Parameters.withFreight =
            withFreight;
    }


    if (godownAddText !== "") {

        payload.Parameters.godownAdd =
            godownAddText;
    }


    if (compName1 !== "") {

        payload.Parameters.comp_name1 =
            compName1;
    }


    if (compReg !== "") {

        payload.Parameters.Comp_reg =
            compReg;
    }


    if (inWord !== "") {

        payload.Parameters.INWORD =
            inWord;
    }


    if (INUSD !== "") {

        payload.Parameters.INUSD =
            INUSD;
    }


    if (challanRef !== "") {

        payload.Parameters.challanRef =
            challanRef;
    }


    if (RPTNAME !== "") {

        payload.Parameters.RPTNAME =
            RPTNAME;
    }


    // ---------------------------------------------------------
    // Debug
    // ---------------------------------------------------------

    console.log(
        "Main Formula:",
        formula
    );

    console.log(
        "Container Detail Formula:",
        containerDetailFormula
    );

    console.log(
        "Final Payload:",
        payload
    );


    // ---------------------------------------------------------
    // Timestamp
    // ---------------------------------------------------------

    const now = new Date();

    const timestamp =
        String(now.getDate())
            .padStart(2, '0') +

        String(now.getMonth() + 1)
            .padStart(2, '0') +

        String(now.getFullYear())
            .slice(-2) +

        "_" +

        String(now.getHours())
            .padStart(2, '0') +

        String(now.getMinutes())
            .padStart(2, '0') +

        String(now.getSeconds())
            .padStart(2, '0');


    // ---------------------------------------------------------
    // Generate Report
    // ---------------------------------------------------------

    $.ajax({

        url:
            'http://localhost:24085/Report/PendingQCReport',

        type: 'POST',

        data:
            JSON.stringify(payload),

        contentType:
            "application/json",

        xhrFields: {
            responseType: 'blob'
        },


        success: function (response) {

            const file =
                new Blob(
                    [response],
                    {
                        type: 'application/pdf'
                    }
                );


            const fileName =
                `${reportName}_${timestamp}.pdf`;


            const link =
                document.createElement('a');


            link.href =
                URL.createObjectURL(file);


            link.download =
                fileName;


            document.body.appendChild(
                link
            );


            link.click();


            document.body.removeChild(
                link
            );

        },


        error: function (
            xhr,
            status,
            error
        ) {

            if (xhr.status === 0) {

                console.error(
                    "Cannot connect to API. Is the backend running?"
                );

            }
            else {

                console.error(
                    'Error generating report:',
                    xhr.status,
                    xhr.statusText,
                    error
                );


                if (xhr.responseText) {

                    console.error(
                        'Response:',
                        xhr.responseText
                    );
                }
            }
        }
    });
}

function numberToWordsCurrency(amount, currency = "Rs.", decimalName = "PAISE") {

    amount = parseFloat(amount) || 0;

    const ones = [
        "",
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine",
        "Ten",
        "Eleven",
        "Twelve",
        "Thirteen",
        "Fourteen",
        "Fifteen",
        "Sixteen",
        "Seventeen",
        "Eighteen",
        "Nineteen"
    ];

    const tens = [
        "",
        "",
        "Twenty",
        "Thirty",
        "Forty",
        "Fifty",
        "Sixty",
        "Seventy",
        "Eighty",
        "Ninety"
    ];


    function convertLessThanThousand(num) {

        let result = "";

        if (num >= 100) {

            result += ones[Math.floor(num / 100)] + " Hundred ";

            num %= 100;
        }

        if (num >= 20) {

            result += tens[Math.floor(num / 10)];

            if (num % 10 !== 0) {
                result += " " + ones[num % 10];
            }

        }
        else if (num > 0) {

            result += ones[num];
        }

        return result.trim();
    }


    function convertIndianNumber(num) {

        if (num === 0) {
            return "Zero";
        }

        let result = "";

        // Crore
        if (num >= 10000000) {

            const crore =
                Math.floor(num / 10000000);

            result +=
                convertIndianNumber(crore) +
                " Crore ";

            num %= 10000000;
        }


        // Lakh
        if (num >= 100000) {

            const lakh =
                Math.floor(num / 100000);

            result +=
                convertLessThanThousand(lakh) +
                " Lakh ";

            num %= 100000;
        }


        // Thousand
        if (num >= 1000) {

            const thousand =
                Math.floor(num / 1000);

            result +=
                convertLessThanThousand(thousand) +
                " Thousand ";

            num %= 1000;
        }


        // Remaining
        if (num > 0) {

            result +=
                convertLessThanThousand(num);
        }

        return result.trim();
    }


    // ---------------------------------------------------------
    // Separate rupees and paise
    // ---------------------------------------------------------

    const rupees =
        Math.floor(amount);

    const paise =
        Math.round((amount - rupees) * 100);


    // ---------------------------------------------------------
    // Currency prefix
    // ---------------------------------------------------------

    let currencyText = currency;

    if (currency === "Rs." || currency === "Rs") {
        currencyText = "Rupees";
    }


    // ---------------------------------------------------------
    // Rupees
    // ---------------------------------------------------------

    let result =
        currencyText + " " +
        convertIndianNumber(rupees);


    // ---------------------------------------------------------
    // Paise
    // ---------------------------------------------------------

    if (paise > 0) {

        result +=
            " and " +
            convertLessThanThousand(paise) +
            " " +
            decimalName;
    }


    // ---------------------------------------------------------
    // Only
    // ---------------------------------------------------------

    result += " Only";


    return result;
}

async function GetPackingSlipPrint(typ)
{

    if (!rowId) {
        showToast("Please save the data before printing the report.", { type: "info" });
  
    }

    let reportName = "";
    let RPTNAME = "";
    const v_no = $('#NumInvoiceNo').val();
    const v_type = $('#ddlDocumentType').val();
    const V_Date = formatDate($('#DtDocumentDate').val());     
    const PackType = $.trim($('#ddlPackNo option:selected').text()).split('-').pop().trim() || "";
    const WithoutBag = $('#chk_PackSlipWithoutBag').is(':checked') ? 1 : 0;
    const ProdType = $('#ddlProductType').val();
    const $firstRow = $('#tblSalesInvoice tbody tr').eq(0);

    let packNos = "";
    let rowCount = 0;

    $('#tblSalesInvoice tbody tr').each(function () {

        const $row = $(this);

        const itemName = $.trim($row.find('.ddlProductName').val() || "");

        const packNo = $.trim($row.find('.TxtPackno').val() || "");

        if (itemName !== "" && packNo !== "")
        {
            packNos += packNo + ",";
        }

        if (itemName !== "") {
            rowCount++;
        }


    });

    packNos = packNos.replace(/,$/, "");

    const validation = await $.ajax({
        url: '/SalesInvoice/GetPackingSlipPrintValidation',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({
            V_NO: parseInt(v_no) || null, 
            V_TYPE: v_type,
            V_Date: V_Date,
            PackType: PackType,
            WithoutBag: WithoutBag,
            ProdType: ProdType     ,   
            packNos: packNos,
            rowscount: rowCount
        })
    });

    if (validation.success == false)
    {
        showToast(validation.message, { type: "warning" });
    }

    if (typ == "Banmk")
    {
        reportName = "rptPackingSlipExportBank";
    }
    else if (typ == "Custom")
    {
        reportName = "rptPackingSlipExportCustom";
    }
    else
    {
        reportName = "rptPackingSlipExport";
    }
    
    const formula =
        "{SALE1.V_TYPE} = '" + v_type + "'" +
        " AND {SALE1.V_NO} = " + (parseInt(v_no) || 0) +
        " AND {SALE1.COMP_CODE} = " + globalVars.CompCode +
        " AND {SALE1.BRANCH_CODE} = " + globalVars.BranchCode +
        " AND {SALE1.YEAR_CODE} = " + globalVars.FYearCode;

    const containerDetailFormula =
        "{tempContainerdetail.SI_TYPE} = '" + v_type + "'" +
        " AND {tempContainerdetail.SI_NO} = " + (parseInt(v_no) || 0) +
        " AND {tempContainerdetail.COMP_CODE} = " + globalVars.CompCode;

    // Packing Slip subreport formula
    const packingSlipFormula =
        "{tempExportPackingSlip.SI_TYPE} = '" + v_type + "'" +
        " AND {tempExportPackingSlip.SI_NO} = " + (parseInt(v_no) || 0) +
        " AND {tempExportPackingSlip.COMP_CODE} = " + globalVars.CompCode +
        " AND {tempExportPackingSlip.BRANCH_CODE} = " + globalVars.BranchCode +
        " AND {tempExportPackingSlip.YEAR_CODE} = " + globalVars.FYearCode;

    const payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,

        // Subreport selection formulas
        SubreportFormulas: {
            "rptContainerDetail.rpt": containerDetailFormula,
            "rptPackingSlipExportDetail2.rpt": packingSlipFormula
        },

        // Crystal Report FormulaFields / Parameters
        Parameters: {
            RPTNAME: "PACKING LIST",
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            comp_phone: "Mobile : " + (globalVars.Phone || ""),
            PAN: "PAN NO.   : " + (globalVars.PAN || ""),
            GST: "GST NO.   : " + (globalVars.GST || ""),
            EMAIL: "Email   : " + (globalVars.Email || ""),
            Website: "Web   : " + (globalVars.pubCompWebsite || "")
        }
    };
    
    if (RPTNAME !== "")
    {
        payload.Parameters.RPTNAME = RPTNAME;
    }

    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' },

        success: function (response) {

            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;


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
function LimitNumberLength(input, maxLength)
{
    let value = input.value.toString();

    if (value.length > maxLength) {
        input.value = value.substring(0, maxLength);
    }
}
