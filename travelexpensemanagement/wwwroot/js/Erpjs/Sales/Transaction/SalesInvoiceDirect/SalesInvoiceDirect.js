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

    });

    $row.find('.TxtTaxType').on('change', function () {

        const selectedCode = $(this).val();
        const selectedTax = TaxPercentageData.find(x => String(x.code) === String(selectedCode));
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

    $row.find('.ddlProductName').on('change', async function () {

        const ItemCode = $(this).val();

        $row.find('.ID').val(ItemCode);

        if (!ItemCode)
        {
            return;
        }

        //const data = await GetCalRate(ItemCode);

        //console.log("Returned Data:", data);

        //if (data && data.length > 0) {

        //    const item = data[0];

        //    $row.find('.TxtREPORT_TYPE').val(item.reporT_TYPE);
        //    $row.find('.TxtSale_Rate').val(item.sale_Rate);
        //    $row.find('.TxtTaxable_Rate').val(item.taxable_Rate);
        //    $row.find('.TxtNet_Wt').val(item.net_Wt);
        //    $row.find('.TxtPacking_Wt').val(item.packing_Wt);
        //    $row.find('.TxtPacking_nos').val(item.packing_nos);
        //}
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
    });

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
function LimitNumberLength(input, maxLength)
{
    let value = input.value.toString();

    if (value.length > maxLength) {
        input.value = value.substring(0, maxLength);
    }
}
