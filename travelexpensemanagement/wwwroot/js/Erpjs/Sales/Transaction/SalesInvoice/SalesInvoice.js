function AddRow(data = {}) {
    let tbody = $('#tblSalesInvoice tbody');
    let newRow = `
        <tr class="no-border-input">
            <td>  <input class="erppagetable-control ID" value="${data.ID ?? ''}" readonly /> </td>
            <td>  <select class="erppagetable-control ddlProductName"> <option value="">-- Select Product  --</option>  ${ProductList}  </select> </td>

            <td>  <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtGrossQty" value="${data.grossQty ?? ''}" />  </td>
            <td>  <input type="number" class="erppagetable-control TxtNetQty" value="${data.NetQty ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtRate" value="${data.Rate ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}"   />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPacKPer" value="${data.PackPer ?? ''}"  />  </td>
            <td>  <input type="number" class="erppagetable-control TxtPackAmount" value="${data.PackAmt ?? ''}"   />  </td>        
            <td>  <input type="number" class="erppagetable-control TxtPackWeight" value="${data.PackWght ?? ''}"   />  </td>        
            <td>  <input type="number"   class="erppagetable-control TxtDisPer" value="${data.DisPer ?? ''}"  /> </td>
            <td>  <input type="number" class="erppagetable-control TxtDisAmount"  value="${data.Disamt ?? ''}"  />   </td>
            <td>  <select class="erppagetable-control TxtTaxType"> <option value="">-- Select Tax Type  --</option>  ${TaxTypeList}  </select> </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstper"  value="${data.CgstPer ?? ''}"    />   </td>
            <td>  <input type="number" class="erppagetable-control TxtCgstAmt" value="${data.CgstAmt ?? ''}"     />  </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstPer" value="${data.SgstPer ?? ''}"    /> </td>
            <td>  <input type="number" class="erppagetable-control TxtSgstamt" value="${data.SgstAmt ?? ''}"    /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTPer" value="${data.IgstPer ?? ''}"    /> </td>
            <td>  <input type="number" class="erppagetable-control TxtIGSTamt" value="${data.IgstAmt ?? ''}"     /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessPer" value="${data.CessPer ?? ''}"   /> </td>
            <td>  <input type="number" class="erppagetable-control TxtCessamt" value="${data.CessAmt ?? ''}"   /> </td>
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

    $row.find('.ddlProductName').on('change', function () {
        const ItemCode = $(this).val();
        $row.find('.ID').val(ItemCode);
    });

    $row.find('.TxtTaxType').on('change', function () {

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

    // Load item details
    if (data.itemCode) {
        SetItemDetails($row);
    }
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

        ]);
      
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
                    Rate: item.rate ?? item.foR_RATE ?? '',
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

        if (item.V_DATE) {
            const date = new Date(item.V_DATE);

            V_DATE =
                String(date.getDate()).padStart(2, '0') + '/' +
                String(date.getMonth() + 1).padStart(2, '0') + '/' +
                date.getFullYear();
        }

        const row = `
            <tr data-index="${index}">

                <!-- Select -->
                <td>
                    <input type="checkbox"
                           class="pending-row-check"
                           data-index="${index}">
                </td>

                <!-- Document Id -->
                <td>${item.DOC_ID ?? ''}</td>

                <!-- Voucher Type -->
                <td>${item.V_TYPE ?? ''}</td>

                <!-- Voucher No -->
                <td>${item.V_NO ?? ''}</td>

                <!-- Voucher Date -->
                <td>${V_DATE}</td>

                <!-- Item Name -->
                <td>${item.Item_Name ?? ''}</td>

                <!-- Item Unit -->
                <td>${item.Item_Unit ?? ''}</td>

                <!-- HSN Code -->
                <td>${item.HSN_Code ?? ''}</td>

                <!-- Nos -->
                <td class="text-end">${item.Nos ?? 0}</td>

                <!-- Gross -->
                <td class="text-end">${item.Gross ?? 0}</td>

                <!-- Quantity -->
                <td class="text-end">${item.Qty ?? 0}</td>

                <!-- Rate -->
                <td class="text-end">${item.Rate ?? 0}</td>

                <!-- Amount -->
                <td class="text-end">${item.Amount ?? 0}</td>

                <!-- Discount % -->
                <td class="text-end">${item.Disc_Per ?? 0}</td>

                <!-- Discount Amount -->
                <td class="text-end">${item.Disc_Amt ?? 0}</td>

                <!-- CGST % -->
                <td class="text-end">${item.CGST_Per ?? 0}</td>

                <!-- CGST Amount -->
                <td class="text-end">${item.CGST_Amt ?? 0}</td>

                <!-- SGST % -->
                <td class="text-end">${item.SGST_Per ?? 0}</td>

                <!-- SGST Amount -->
                <td class="text-end">${item.SGST_Amt ?? 0}</td>

                <!-- IGST % -->
                <td class="text-end">${item.IGST_Per ?? 0}</td>

                <!-- IGST Amount -->
                <td class="text-end">${item.IGST_Amt ?? 0}</td>

                <!-- Packing % -->
                <td class="text-end">${item.PACK_Per ?? 0}</td>

                <!-- Packing Amount -->
                <td class="text-end">${item.PACK_Amt ?? 0}</td>

                <!-- Remark -->
                <td>${item.Remark ?? ''}</td>

                <!-- Type -->
                <td>${item.Type ?? ''}</td>

                <!-- Serial No -->
                <td class="text-end">${item.SNO ?? ''}</td>

                <!-- Item Code -->
                <td class="text-end">${item.Item_Code ?? ''}</td>

                <!-- Action -->
                <td class="hidden-col">
                    <button type="button"
                            class="btn btn-sm btn-primary pending-select-btn"
                            data-index="${index}">
                        Select
                    </button>
                </td>

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
            Item_Code
        });

    });

    return selectedRows;
}