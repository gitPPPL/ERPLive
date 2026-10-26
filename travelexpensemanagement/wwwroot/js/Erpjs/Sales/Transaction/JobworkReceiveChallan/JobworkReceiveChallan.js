function AddRow(data = {}) {

    console.log("data", data);

    let tbody = $('#tblJobworkReceiveChallan tbody');
    let newRow = `
        <tr class="no-border-input">
            <td>  <input class="erppagetable-control ID" value="${data.ID ?? ''}" readonly /> </td>
            <td>  <select class="erppagetable-control ddlProductName"> <option value="">-- Select Product  --</option>  ${ProductList}  </select> </td>
            <td>  <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtGrossQty" value="${data.grossQty ?? ''}" />  </td>
            <td>  <input type="number" class="erppagetable-control TxtNetQty" value="${data.NetQty ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtRate" value="${data.Rate ?? ''}"  />  </td>
            <td style="display:none;">  <input type="number" class="erppagetable-control TxtForRate" value="${data.ForRate ?? ''}"  />  </td>

            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}"  readonly  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPacKPer" value="${data.PackPer ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPackAmount" value="${data.PackAmt ?? ''}" readonly  />  </td>        
            <td class="hidden-col">  <input type="number" class="erppagetable-control TxtPackWeight" value="${data.PackWght ?? ''}"   />  </td>        
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

            $row.find('.TxtHsnCode').val(item.hsN_CODE);

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

    const num = (selector, row = $row) => {
        const value = parseFloat(row.find(selector).val());
        return Number.isFinite(value) ? value : 0;
    };

    const round2 = value => Math.round((value + Number.EPSILON) * 100) / 100;

    const itemCode = num('.ID');
    const nos = num('.TxtNos');
    const grossQty = num('.TxtGrossQty');
    const netQty = num('.TxtNetQty');
    const rate = num('.TxtRate');
    const packPer = num('.TxtPacKPer');
    const disPer = num('.TxtDisPer');

    // 2. Calculate Amount
    let amount = 0;

    if (itemCode !== 0)
    {
        if (!chkCalPCS)
        {
            if (netQty > 0)
            {
                amount = netQty * rate;
            }
        }
        else
        {
            if (nos > 0)
            {
                amount = grossQty * rate;
            }
        }
    }

    amount = round2(amount);

    // 3. Calculate Packing Amount
    let packAmount = 0;

    if (packPer > 0)
    {
        packAmount = amount * packPer / 100;
    }

    packAmount = round2(packAmount);

    // 4. Calculate Discount Amount
    const baseAmount = amount + packAmount;

    let discountAmount = 0;

    if (disPer > 0)
    {
        discountAmount = baseAmount * disPer / 100;
    }

    discountAmount = round2(discountAmount);

    // 5. Calculate taxable base
    // VB.NET: Amount + Packing Amount - Discount Amount
    const taxableAmount = baseAmount - discountAmount;

    // 6. Read tax percentages
    const cgstPer = num('.TxtCgstper');
    const sgstPer = num('.TxtSgstPer');
    const igstPer = num('.TxtIGSTPer');
    const cessPer = num('.TxtCessPer');

    // 7. Calculate GST and CESS
    let cgstAmount = 0;
    let sgstAmount = 0;
    let igstAmount = 0;
    let cessAmount = 0;

    if (amount > 0)
    {
        cgstAmount = round2(taxableAmount * cgstPer / 100);
        sgstAmount = round2(taxableAmount * sgstPer / 100);
        igstAmount = round2(taxableAmount * igstPer / 100);
        cessAmount = round2(taxableAmount * cessPer / 100);
    }

    // 8. Update row values
    $row.find('.TxtAmount').val(amount.toFixed(2));
    $row.find('.TxtPackAmount').val(packAmount.toFixed(2));
    $row.find('.TxtDisAmount').val(discountAmount.toFixed(2));
    $row.find('.TxtCgstAmt').val(cgstAmount.toFixed(2));
    $row.find('.TxtSgstamt').val(sgstAmount.toFixed(2));
    $row.find('.TxtIGSTamt').val(igstAmount.toFixed(2));
    $row.find('.TxtCessamt').val(cessAmount.toFixed(2));

    // 9. Calculate totals from all valid item rows
    let totalNos = 0;
    let totalGrossQty = 0;
    let totalNetQty = 0;

    let totalAmount = 0;
    let totalPackAmount = 0;
    let totalDiscount = 0;

    let totalCGST = 0;
    let totalSGST = 0;
    let totalIGST = 0;
    let totalCess = 0;

    // Use the same grid as AddRow()
    $('#tblJobworkReceiveChallan tbody tr').each(function ()
    {
        const $r = $(this);

        if (!$r.find('.ddlProductName').val())
        {
            return;
        }

        totalNos += num('.TxtNos', $r);
        totalGrossQty += num('.TxtGrossQty', $r);
        totalNetQty += num('.TxtNetQty', $r);
        totalAmount += num('.TxtAmount', $r);
        totalPackAmount += num('.TxtPackAmount', $r);
        totalDiscount += num('.TxtDisAmount', $r);
        totalCGST += num('.TxtCgstAmt', $r);
        totalSGST += num('.TxtSgstamt', $r);
        totalIGST += num('.TxtIGSTamt', $r);
        totalCess += num('.TxtCessamt', $r);
    });

    // 10. Calculate subtotal
    const subtotal = round2( totalAmount + totalPackAmount - totalDiscount );
    // 11. Calculate amount before TCS
    const amountBeforeTCS = round2(  subtotal + totalCGST + totalSGST + totalIGST + totalCess );
    // 12. Calculate TCS (VB.NET uses Math.Ceiling)
    const tcsPer = parseFloat($('#NumOtherTCS1').val()) || 0;
    const tcsAmount = Math.ceil( amountBeforeTCS * tcsPer / 100 );
    // 13. Calculate net amount
    const roundOff = parseFloat($('#NumOtherRoundOff').val()) || 0;

    const netAmount = round2( amountBeforeTCS + tcsAmount + roundOff );
    // 14. Calculate insurance (after net amount)
    const insPer = parseFloat($('#NumInsurance1').val()) || 0;
    const insuranceAmount = insPer > 0 ? Math.round(netAmount * (insPer / 100000)) : 0;
    // 15. Calculate TDS on freight
    const tdsPer = parseFloat($('#NumTDSFreight1').val()) || 0;
    const freight = parseFloat($('#NumConsFreight').val()) || 0;
    const tdsAmount = tdsPer > 0 ? Math.round(freight * tdsPer / 100) : 0;

    $('#NumOtherTotalAmount').val(round2(totalAmount).toFixed(2));
    $('#NumOtherPacking2').val(round2(totalPackAmount).toFixed(2));
    $('#NumOtherDiscount2').val(round2(totalDiscount).toFixed(2));
    $('#NumOtherTotalNos').val(round2(totalNos).toFixed(2));
    $('#NumGrossQty').val(round2(totalGrossQty).toFixed(2));
    $('#NumNetQty').val(round2(totalNetQty).toFixed(2));
    $('#NumOtherSubTotal').val(subtotal.toFixed(2));
    $('#NumOtherCGST2').val(round2(totalCGST).toFixed(2));
    $('#NumOtherSGST2').val(round2(totalSGST).toFixed(2));
    $('#NumOtherIGST2').val(round2(totalIGST).toFixed(2));
    $('#NumOtherCESS2').val(round2(totalCess).toFixed(2));
    $('#NumOtherTCS2').val(tcsAmount.toFixed(2));
    $('#NumOtherNetAmount').val(netAmount.toFixed(2));
    $('#NumInsurance2').val(insuranceAmount.toFixed(2));
    $('#NumTDSFreight2').val(tdsAmount.toFixed(2));
    $('#NumOtherRoundOff').val(roundOff.toFixed(2));

}
function GetSalesInvoiceDetails() {

    const details = [];

    $('#tblJobworkReceiveChallan tbody tr').each(function () {
        const $row = $(this);
        const detail = {
            ITEM_CODE: parseInt($.trim($row.find('.ddlProductName').val()), 10) || 0,
            ITEM_NAME: $.trim($row.find('.ddlProductName option:selected').text()) || "",
            NOS: parseInt($.trim($row.find('.TxtNos').val()), 10) || 0,
            GROSS_QTY: parseFloat($.trim($row.find('.TxtGrossQty').val())) || 0,
            QTY: parseFloat($.trim($row.find('.TxtNetQty').val())) || 0,
            RATE: parseFloat($.trim($row.find('.TxtRate').val())) || 0,
            FOR_RATE: parseFloat($.trim($row.find('.TxtForRate').val())) || 0,
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
            HSN_CODE: $.trim($row.find('.TxtHsnCode').val()) || ""
        };

        details.push(detail);
    });

    return details;
}

async function LoadData() {
    try {
        const res = await $.ajax({
            url: '/JobworkReceiveChallanList/GetDataByCode',
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

        ])
        // Header
        $('#TxtCode').val(Header.doC_ID);
        $('#ddlDocumentType').val(Header.v_TYPE);
        $('#NumDocumentNo').val(Header.v_NO);
        $('#DtDocumentDate').val(formatDate(Header.v_DATE));
        $('#ddlPartyName').val(Header.bilL_CODE || '').trigger('change');
        $('#TxtAddressL1').val(Header.bilL_ADD1);
        $('#TxtAddressL2').val(Header.bilL_ADD2);
        $('#TxtAddressL3').val(Header.bilL_ADD3);
        $('#ddlStation').val(Header.bilL_CITY);
        $('#NumPincode').val(Header.bilL_PINCODE);
        $('#ddlSaleThrough').val(Header.agenT_CODE || '').trigger('change');
        $('#ddlSupplyType').val(Header.supplY_TYPE);
        $('#ddlConsignee').val(Header.shiP_CODE || '').trigger('change');
        $('#ddlTransactionThrough').val(Header.traN_TYPE || '').trigger('change');
        $('#TxtTransactionAddressL1').val(Header.shiP_ADD1);
        $('#TxtTransactionAddressL2').val(Header.shiP_ADD2);
        $('#TxtTransactionAddressL3').val(Header.shiP_ADD3);
        $('#TxtTransactionStation').val(Header.shiP_CITY);
        $('#NumTransactionPIN').val(Header.shiP_PINCODE);       
        $('#ddlFormType').val(Header.forM_CODE);
        $('#ddlJobType').val(Header.shipmenT_TYPE);
        $('#ddlProductionType').val(Header.iteM_TYPE);
        $('#ddlReferenceNo').val(Header.reF_NO || '').trigger('change');
        $('#DtReferenceDate').val(formatDate(Header.reF_DATE));
        if (Header.reF_NO != "")
        {
            $('#chkReferenceDate').prop('checked', true);
        }
        else
        {
            $('#chkReferenceDate').prop('checked', false);
        }

        $('#ddlGateNo').val(Header.gatE_NO || '').trigger('change');
        $('#ddlWBNo').val(Header.wB_NO || '').trigger('change');
        $('#ddlPackNo').val(Header.pacK_NO || '').trigger('change');
        $('#ddlSaudaNo').val(Header.saudA_NO || '').trigger('change');
        $('#NumSaudaRate').val(Header.saudA_RATE);
        $('#ddlTaxType').val(Header.taX_CODE);
        $('#ddlNatureJW').val(Header.insU_DETAIL || '').trigger('change');
        $('#NumPartyChallanNo').val(Header.buyeR_ORDNO);
        $('#ChkPCS').prop('checked', Header.caL_ONPCS == 1);
        $('#NumOtherTotalAmount').val(Header.amount);
        $('#NumOtherTotalNos').val(Header.toT_NOS);
        $('#NumGrossQty').val(Header.toT_GROSS);
        $('#NumNetQty').val(Header.toT_NET);
        $('#NumGRNo').val(Header.gR_NO);
        $('#DtGRdate').val(formatDate(Header.gR_DATE));
        if (Header.gR_NO != "") {
            $('#chkGRDate').prop('checked', true);
        } else {
            $('#chkGRDate').prop('checked', false);
        }
        $('#NumOtherCESS1').val(Header.cesS_PER);
        $('#NumOtherCESS2').val(Header.cesS_AMT);
        $('#NumPayFreight').val(Header.frT_TOPAY);
        $('#NumWBQty').val(Header.wB_QTY);
        $('#ddlTransport').val(Header.transporT_CODE);
        $('#NumOtherPacking1').val(Header.pacK_PER);
        $('#NumOtherPacking2').val(Header.pacK_AMT);
        $('#NumOtherDiscount1').val(Header.disC_PER);
        $('#NumOtherDiscount2').val(Header.disC_AMT);
        $('#NumOtherTCS1').val(Header.tcS_PER);
        $('#NumOtherTCS2').val(Header.tcS_AMT);
        $('#NumLoadAmount1').val(Header.loaD_PER);
        $('#NumLoadAmount2').val(Header.loaD_AMT);
        $('#NumWayBillNo').val(Header.waybilL_NO);
        $('#NumTruckNo').val(Header.vehiclE_NO);
        $('#NumOtherSubTotal').val(Header.vehiclE_NO);
        $('#NumOtherRoundOff').val(Header.rounD_OFF);
        $('#ddlLoadParty').val(Header.loaD_AC);
        $('#txtDriverName').val(Header.driveR_NAME);
        $('#NumMobileNo').val(Header.driveR_NO);
        $('#NumOtherCGST1').val(Header.cgsT_PER);
        $('#NumOtherCGST2').val(Header.cgsT_AMT);
        $('#NumOtherNetAmount').val(Header.namount);
        $('#TxtRemarks1').val(Header.loaD_REM);
        $('#TxtRemarks3').val(Header.remark);
        $('#NumOtherSGST1').val(Header.sgsT_PER);
        $('#NumOtherSGST2').val(Header.sgsT_AMT);
        $('#NumInsurance1').val(Header.insU_PER);
        $('#NumInsurance2').val(Header.insU_AMT);
        $('#NumWBAmount').val(Header.wB_AMT);
        $('#TxtRemarks2').val(Header.wB_REM);   
        $('#NumOtherIGST1').val(Header.igsT_PER);
        $('#NumOtherIGST2').val(Header.igsT_AMT);
        $('#NumConsFreight').val(Header.frT_AMT);
        $('#ddlWBParty').val(Header.wB_AC);
        $('#tblJobworkReceiveChallan tbody').empty();

        if (Array.isArray(Details) && Details.length > 0) {
            Details.forEach(item => {
                AddRow({
                    ID: item.iteM_CODE ?? '',
                    Productcode: item.iteM_CODE ?? '',
                    nos: item.nos ?? '',
                    grossQty: item.grosS_QTY ?? '',
                    NetQty: item.qty ?? '',                   
                    Rate: item.rate ?? '',
                    ForRate: item.foR_RATE ?? '',
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
                    itemCode: item.iteM_CODE ?? ''
                });
            });
        }
    }
    catch (error) {
        console.error("Error loading data:", error);
    }
}
function ShowPendingDetails(data)
{
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
            url: '/JobworkReceiveChallan/CalRate',
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

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocumentDate").val(),
        vtype: $("#ddlDocumentType").val(),
        vno: $("#NumInvoiceNo").val()
    };
    try {
        const response = await fetch('/JobworkReceiveChallan/CheckValidDate', {
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

async function TransitReport() {
    try {
        if (!rowId)
        {
            showToast("Please save the data before printing the report.", { type: "info" });
            return;
        }

        const V_NO = parseInt($.trim($("#NumDocumentNo").val()), 10);
        const V_TYPE = $.trim($("#ddlDocumentType").val() || "");
        const COMP_CODE = Number(globalVars.CompCode) || 0;
        const YEAR_CODE = Number(globalVars.FYearCode) || 0;
        const BRANCH_CODE = Number(globalVars.BranchCode) || 0;

        if (!V_TYPE || !Number.isInteger(V_NO) || V_NO <= 0) {
            showToast("Please enter a valid voucher type and voucher number.", { type: "warning" });
            return;
        }

        const validation = await $.ajax({
            url: "/JobworkReceiveChallan/PrintEsingValidation",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({
                V_NO: V_NO,
                V_TYPE: V_TYPE
            })
        });

        console.log("Print validation:", validation);

        if (validation.success === false)
        {
            toastr.warning( validation.message || "Voucher validation failed." );
            return;
        }

        let reportName = "";

        if (V_TYPE === "SAJR")
        {
            if (COMP_CODE === 3)
            {
                reportName = "rptJW";
            } else
            {
                reportName = "INVOICE_ChallanSale";
            }
        }
        else if (COMP_CODE === 1 || COMP_CODE === 2 || COMP_CODE === 3)
        {
            reportName = "INVOICE_GSTRETQR";
        } else if ((COMP_CODE === 4 || COMP_CODE === 5 || COMP_CODE === 7 ||  COMP_CODE === 8) &&  YEAR_CODE > 4 )
        {
            reportName = "INVOICE_GSTRETQR";
        } else
        {
            reportName = "INVOICE_GSTRETQR";
        }

        const RPTNAME = V_TYPE === "SAJR" ? "JOBWORK RECEIVED CHALLAN" : "SALES RETURN / CREDIT NOTE";

        const formula =
            "{SALE1.V_TYPE} = '" + V_TYPE + "'" +
            " AND {SALE1.V_NO} = " + V_NO +
            " AND {SALE1.COMP_CODE} = " + COMP_CODE +
            " AND {SALE1.YEAR_CODE} = " + YEAR_CODE +
            " AND {SALE1.BRANCH_CODE} = " + BRANCH_CODE;

        console.log("Report name:", reportName);
        console.log("Selection formula:", formula);

        const parameters = {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            comp_phone: "Mobile :" + (globalVars.Phone || ""),
            GST: "GSTIN       :" + (globalVars.GST || ""),
            PAN: "PAN NO.   :" + (globalVars.PAN || ""),
            Website: "Web  :" + (globalVars.website || ""),
            EMAIL: "Email   :" + (globalVars.Email || ""),
            RPTNAME: RPTNAME
        };

        const regAdd1 = $.trim(globalVars.RegAdd1 || "");
        const regAdd2 = $.trim(globalVars.RegAdd2 || "");
        const CINNO = $.trim(globalVars.CINNO || "");

        if (regAdd1 !== "" && CINNO !== "")
        {
            parameters.Comp_reg = "Reg.Office :" + regAdd1 + ", " + regAdd2 + "  CIN :" + CINNO;
        }

        const netAmount = $('#NumOtherNetAmount').val();
  
        parameters.INWORD = numberToWordsCurrency( netAmount, "Rs.", "PAISE" );

        const payload = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: database,
            Parameters: parameters
        };

        console.log("Crystal Report payload:", payload);

        const response = await $.ajax({
            url: "http://localhost:24085/Report/PendingQCReport",
            type: "POST",
            data: JSON.stringify(payload),
            contentType: "application/json; charset=utf-8",
            xhrFields: {
                responseType: "blob"
            }
        });

        const file = new Blob([response], {
            type: "application/pdf"
        });

        const now = new Date();

        const timestamp =
            String(now.getDate()).padStart(2, "0") +
            String(now.getMonth() + 1).padStart(2, "0") +
            String(now.getFullYear()).slice(-2) + "_" +
            String(now.getHours()).padStart(2, "0") +
            String(now.getMinutes()).padStart(2, "0") +
            String(now.getSeconds()).padStart(2, "0");

        const fileName = `${reportName}_${timestamp}.pdf`;
        const fileUrl = URL.createObjectURL(file);
        const link = document.createElement("a");

        link.href = fileUrl;
        link.download = fileName;

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        setTimeout(() => URL.revokeObjectURL(fileUrl), 1000);

    } catch (error) {
        if (error.status === 0) {
            console.error(
                "Cannot connect to the report API. Check whether the backend is running."
            );
        } else {
            console.error("Error generating report:", error);

            if (error.responseJSON) {
                console.error("Response:", error.responseJSON);
            } else if (error.responseText) {
                console.error("Response:", error.responseText);
            }

            toastr.error("Unable to generate the report. Please check the console.");
        }
    }
}

async function SendMail() {
    try {

        if (!rowId) {
            showToast(`Please save the data before printing the report.`, { type: "info" });
            return;
        }

        let PartyCode = $('#ddlPartyName').val();
        const vno = parseInt($('#NumInvoiceNo').val()) || 0;
        const v_type = $('#ddlDocumentType').val() || '';

        // Confirmation
        const result = await Swal.fire({
            title: "Do you want to send mail?",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes",
            cancelButtonText: "No"
        });

        if (!result.isConfirmed) {
            return;
        }

        // Step 1: Generate report
        const report = await GetTransitReportFile();

        if (!report || !report.file) {
            toastr.error("Report generation failed.");
            return;
        }

        // Step 2: Prepare FormData
        let formData = new FormData();
        formData.append("PartyCode", PartyCode);
        formData.append("vno", vno);
        formData.append("v_type", v_type);
        formData.append("file", report.file, report.fileName);

        // Step 3: Send mail
        const mailRes = await $.ajax({
            url: '/SalesInvoiceDirect/SendMail',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        });


        if (mailRes.success == true) {
            toastr.success(mailRes.message || "Mail sent successfully.");
        }
        else {
            toastr.error(mailRes.message || "Failed to send mail.");
        }
        return mailRes;

    } catch (error) {
        console.error("Error:", error);
        toastr.error("An error occurred while sending the mail.");
    }
}

async function GetTransitReportFile() {
    try {
        // 1. Check whether the document is saved
        if (!rowId) {
            showToast("Please save the data before printing the report.", {
                type: "info"
            });
            return null;
        }

        // 2. Get voucher details
        const V_NO = parseInt($.trim($("#NumDocumentNo").val()), 10);
        const V_TYPE = $.trim($("#ddlDocumentType").val() || "");

        const COMP_CODE = Number(globalVars.CompCode) || 0;
        const YEAR_CODE = Number(globalVars.FYearCode) || 0;
        const BRANCH_CODE = Number(globalVars.BranchCode) || 0;

        if (!V_TYPE || !Number.isInteger(V_NO) || V_NO <= 0) {
            showToast("Please enter a valid voucher type and voucher number.", {
                type: "warning"
            });
            return null;
        }

        // 3. Validate voucher
        const validation = await $.ajax({
            url: "/JobworkReceiveChallan/PrintEsingValidation",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({
                V_NO: V_NO,
                V_TYPE: V_TYPE
            })
        });

        console.log("Print validation:", validation);

        if (
            validation.success === false ||
            String(validation.success).toLowerCase() === "false"
        ) {
            toastr.warning(
                validation.message || "Voucher validation failed."
            );
            return null;
        }

        // 4. Select report file according to VB.NET logic
        let reportName;

        if (V_TYPE === "SAJR") {
            reportName = COMP_CODE === 3
                ? "rptJW"
                : "INVOICE_ChallanSale";
        } else {
            reportName = "INVOICE_GSTRETQR";
        }

        // 5. Report title
        const RPTNAME = V_TYPE === "SAJR"
            ? "JOBWORK RECEIVED CHALLAN"
            : "SALES RETURN / CREDIT NOTE";

        // 6. Crystal Reports selection formula
        const formula =
            "{SALE1.V_TYPE} = '" + V_TYPE + "'" +
            " AND {SALE1.V_NO} = " + V_NO +
            " AND {SALE1.COMP_CODE} = " + COMP_CODE +
            " AND {SALE1.YEAR_CODE} = " + YEAR_CODE +
            " AND {SALE1.BRANCH_CODE} = " + BRANCH_CODE;

        // 7. Company formula fields
        const parameters = {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            comp_phone: "Mobile :" + (globalVars.Phone || ""),
            GST: "GSTIN       :" + (globalVars.GST || ""),
            PAN: "PAN NO.   :" + (globalVars.PAN || ""),
            Website: "Web  :" + (globalVars.website || ""),
            EMAIL: "Email   :" + (globalVars.Email || ""),
            RPTNAME: RPTNAME
        };

        // 8. Additional company formula fields
        if (COMP_CODE !== 3) {
            if (COMP_CODE !== 8) {
                parameters.comp_name1 =
                    "An ISO 9001:2015 Certified Company";
            }

            const regAdd1 = $.trim(globalVars.RegAdd1 || "");
            const regAdd2 = $.trim(globalVars.RegAdd2 || "");
            const CINNO = $.trim(globalVars.CINNO || "");

            // Match VB.NET: both address and CIN must be present
            if (regAdd1 !== "" && CINNO !== "") {
                parameters.Comp_reg =
                    "Reg.Office :" + regAdd1 +
                    ", " + regAdd2 +
                    "  CIN :" + CINNO;
            }
        }

        // 9. Amount in words
        const netAmount = $('#NumOtherNetAmount').val();

 

        parameters.INWORD = numberToWordsCurrency(
            netAmount,
            "Rs.",
            "PAISE"
        );

        // 10. Build report payload
        const payload = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: database,
            Parameters: parameters
        };

        console.log("Report payload:", payload);

        // 11. Generate PDF
        const pdfBlob = await $.ajax({
            url: "http://localhost:24085/Report/PendingQCReport",
            type: "POST",
            data: JSON.stringify(payload),
            contentType: "application/json; charset=utf-8",
            xhrFields: {
                responseType: "blob"
            }
        });

        // 12. Prepare file name
        const now = new Date();

        const timestamp =
            String(now.getDate()).padStart(2, "0") +
            String(now.getMonth() + 1).padStart(2, "0") +
            String(now.getFullYear()).slice(-2) + "_" +
            String(now.getHours()).padStart(2, "0") +
            String(now.getMinutes()).padStart(2, "0") +
            String(now.getSeconds()).padStart(2, "0");

        const fileName =
            `SalesInvoiceDirect_${V_NO}_${timestamp}.pdf`;

        // 13. Return file to the caller
        const file = new Blob([pdfBlob], {
            type: "application/pdf"
        });

        return {
            file: file,
            fileName: fileName
        };

    } catch (error) {
        console.error("GetTransitReportFile error:", error);

        if (error.status === 0) {
            toastr.error(
                "Cannot connect to the report API. Check whether it is running."
            );
        } else {
            toastr.error("Unable to generate the report.");
        }

        return null;
    }
}