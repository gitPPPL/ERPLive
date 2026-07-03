function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}

const code = getQueryParam('vNo');
const vType = getQueryParam('vType');
let isReadOnly = getQueryParam('readOnly') === 'true';

$(document).ready(function () {
    let currentDocNo = null;
    ddlDocType();
    ddlDocStatus();
    ddlBillFrom();
    GetddlCityBillDetails();
    GetddlstateBillDetails();
    GetddlCityShipDetails();
    GetddlstateShipDetails();
    ddlShipDetails();
    ddlTransportName();
    ddlOrdertype();
    const today = new Date().toISOString().split('T')[0];

    document.getElementById('DtDocDate').value = today;
    const billDate = document.getElementById('DtBillDate');
    if (billDate) billDate.min = today;
    const challanDate = document.getElementById('DtChallanDate');
    if (challanDate) challanDate.min = today;

    ddlDocStatus(() => {
        $('#ddlDocStatus').val(1);
        $('#ddlDocStatus').prop('disabled', true);
    });

    $('#ddlDocType').on('blur', function () {
        $(this).prop('disabled', true);
    });

    if (isReadOnly === "readonly") {
        setFormReadOnly();
        $('#PurchaseReceiptEntryForm').after('<span class="badge bg-secondary ms-2">Read-Only Mode</span>');
    }

    if (code) {
        $.ajax({
            url: '/PurchaseReceiptEntry/GetAllDatadetails',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ VNO: code, vType: vType }),
            success: function (response) {
                const $attachmentTbody = $('#tblAttachmentPRE tbody');
                GetallDatapurchase1purchase2purchase3(response);
            },
            error: function (xhr) {
                toastr.error('Error: ' + xhr.responseText);
            }
        });
    }

    //==========document type changes==========
    $('#ddlDocType').on('change', function () {
        const selectedValue = $(this).val();
        const selectedText = $(this).find("option:selected").text();
        if (selectedValue !== "") {

            sendDocType(selectedValue, selectedText);
            $('#NumDocNo').prop('readonly', true);
        }

    });
    
    //=========Fill Data on Chaneg Gate No==============
    $('#ddlGateNo').on('change', function () {
        var selectedNo = $(this).val(); 
        const selectedText = $(this).find('option:selected').text();
        $('#NumDocNo').prop('readonly', true);

        $('#txtNetAmount').prop('readonly', true);
        $('#ddlBillFrom').prop('disabled', true);
        $('#txtAddLine1').prop('readonly', true);
        $('#txtAddLine2').prop('readonly', true);
        $('#txtAddLine3').prop('readonly', true);
        $('#ddlCity').prop('disabled', true);
        $('#txtPincode').prop('readonly', true);
        $('#ddlState').prop('disabled', true);
        $('#txtGST').prop('readonly', true);

        $('#txtShipAddLine1').prop('readonly', true);
        $('#txtShipAddLine2').prop('readonly', true);
        $('#txtShipAddLine3').prop('readonly', true);
        $('#ddlShipCity').prop('disabled', true);
        $('#ddlShipState').prop('disabled', true);
        $('#NumShipPincode').prop('readonly', true);
        $('#txtShipGST').prop('readonly', true);

        if (selectedNo !== "0" && selectedNo !== "") {
            $.ajax({
                url: '/PurchaseReceiptEntry/GetGatDetailsList',
                type: 'POST',
                data: { StrVNo: selectedNo, StrV_type: selectedText },
                success: function (response) {
                    console.log("Full Data", response);
                    GetalldatafetchGatonchange(response);
                },
                error: function (xhr) {
                    console.error("Error:", xhr.responseText);
                }
            });
        }
    });


    $('#ddlBillFrom').on('change', function () {
        const selectedBillFrom = $(this).val();
        if (selectedBillFrom !== "") {
            getBillFrom(selectedBillFrom); 
        }
    });

    $('#ddladdressBD1').on('change', function () {
        const selectedBillFromValue = $('#ddlBillFrom').val();
        var selectedId = $(this).val();
        getBillDetailsAddLine1(selectedBillFromValue, selectedId)
    });

    $('#ddladdressSD1').on('change', function () {
        const selectedBillFromValue = $('#ddlShipFrom').val();
        var selectedId = $(this).val();
        getShipDetailsAddLine1(selectedBillFromValue, selectedId)
    });

    //=======Transport(freight Pay and Tax) Calulation==========
    $("#NumFreightPay, #NumFrtTax1").on("input", function () {

        const freightPay = parseFloat($("#NumFreightPay").val()) || 0;
        const freightTaxPer = parseFloat($("#NumFrtTax1").val()) || 0;

        const freightTaxAmt = (freightPay * freightTaxPer) / 100;

        $("#NumFrtTax2").val(freightTaxAmt.toFixed(2));

        // VB me yahan land_amt() call hota tha
        $('#tblPurchaseReceiptIR tbody tr').each(function () {
            recalculateRow($(this));
        });

        calculateTotalRecQty();
    });

    //==========Add new row=================
    $('#tblPurchaseReceiptIR tbody tr').each(function () {
        loadItemDropdown($(this).find('.item-name-dropdown'));
        loadTaxTypeDropdown($(this).find('.TaxType-dropdown'));
    });

    $('#btnAddRow').on('click', function () {
        addRow();
    });

    //==========Delete row==================
    $(document).on('click', '.btn-delete-row', function () {
        if ($('#tblPurchaseReceiptIR tbody tr').length > 1) {
            $(this).closest('tr').remove();
        } else {
            toastr.error('At least one row is required.');
        }
    });

    //order  type select drop down List start block
    $('#ddlcopyFromDropdown').on('change', function () {
        var selectedId = $('#ddlcopyFromDropdown').val();

        const allItemCodes = [];
        $('#tblPurchaseReceiptIR tbody tr').each(function () {
            const itemCode = $(this).find('select[name="ItemName"]').val()?.trim() || null;
            if (itemCode) allItemCodes.push(itemCode);
        });

        if (selectedId) {
            const modal = new bootstrap.Modal(document.getElementById('indendorderstoreModal'));
            modal.show();

            $.ajax({
                url: '/PurchaseReceiptEntry/GetOrderDetailsList',
                type: 'GET',
                data: { StrID: selectedId, ItemCodes: allItemCodes },
                traditional: true, 
                success: function (response) {
                    let tbody = $('#tblindendorderstore tbody');
                    tbody.empty(); 

                    if (!response || response.length === 0) {
                        toastr.warning('No order details found for the selected ID.');
                        return;
                    }

                    $.each(response, function (index, item) {
                        let row = '<tr>' +
                            '<td><input type="checkbox" class="rowCheckbox" /></td>' +
                            `<td>${item["VNo"] ?? ''}</td>` +
                            `<td>${item["VType"] ?? ''}</td>` +
                            `<td>${item["VDate"] ?? ''}</td>` +
                            `<td>${item["Item Code"] ?? ''}</td>` +
                            `<td${item["Item Name"] ?? ''}</td>` +
                            `<td>${item["Unit"] ?? ''}</td>` +
                            `<td>${item["Nos"] ?? ''}</td>` +
                            `<td>${item["qty"] ?? ''}</td>` +
                            `<td>0</td>` +
                            `<td>${item["Rate"] ?? ''}</td>` +
                            `<td${item["TaxType"] ?? ''}</td>` +
                            `<td>${item["Pack%"] ?? ''}</td>` +
                            `<td>${item["Disc%"] ?? ''}</td>` +
                            `<td>${item["CGST%"] ?? ''}</td>` +
                            `<td>${item["SGST%"] ?? ''}</td>` +
                            `<td>${item["IGST%"] ?? ''}</td>` +
                            `<td>${item["Cess%"] ?? ''}</td>` +
                            `<td>${item["Cess"] ?? ''}</td>` +
                            `<td>0</td>` +
                            `<td>${item["Oth Amt"] ?? '0'}</td>` +
                            `<td>${item["Make"] ?? ''}</td>` +
                            `<td>${item["Department"] ?? ''}</td>` +
                            `<td></td>` +
                            `<td>${item["Req Type"] ?? ''}</td>` +
                            `<td>${item["Req No"] ?? ''}</td>` +
                            `<td>${item["DeptCode"] ?? ''}</td>` +
                            `<td>${item["MakeCode"] ?? ''}</td>` +
                            `<td>${item["UCode"] ?? ''}</td>` +
                            `<td>${item["TaxType"] ?? ''}</td>` +
                            '</tr>';

                        tbody.append(row);
                    });
                },
                error: function (xhr, status, error) {
                    toastr.error('Failed to load order details: ' + error);
                }
            });
        }
    });

    //order  type select drop down List End block
    $(document).on('change', '.item-name-dropdown', function () {
        const selectedCode = $(this).val();
        const row = $(this).closest('tr');
        getHSNCode(selectedCode, row);
    });

    $(document).on('change', 'input[name="BillQty"], input[name="Rate"]', function () {
        const row = $(this).closest('tr');
        const rate = parseFloat(row.find('input[name="Rate"]').val()) || 0;
        const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;

        const amount = rate * billQty;

        row.find('input[name="Amount"]').val(amount.toFixed(2));
    });

    // allData calculation this method onanimation start block
    $(document).on('change', '.TaxType-dropdown', function () {
        const selectedCode = $(this).val();
        const row = $(this).closest('tr');
        getTaxType(selectedCode, row);
    });

    $(document).on('input', '#tblPurchaseReceiptIR tbody input', function () {
        const row = $(this).closest('tr');
        recalculateRow(row);         
        calculateTotalRecQty();      
    });

    // allData calculation this method onanimation End block
    //Attachment start Block
    const $attachmentTbody = $('#tblAttachmentPRE tbody');

    // Function to add a new row
    function addAttachmentRow(data = {}) {
        const row = `
                    <tr class="no-border-input">
                        <td style="display:none;">${data.code || ''}</td>
                        <td>
                            <input type="text" class="form-control mb-1" value="${data.fileName || ''}" placeholder="Enter file name"/>
                        </td>
                        <td>
                            <input type="file" class="form-control file-upload" />
                        </td>
                        <td>
                            <i class="fa fa-plus btn-add-action text-success me-2" title="Add Row" style="cursor:pointer;"></i>
                            <i class="fa fa-edit btn-edit-action text-primary me-2" title="Edit Row" style="cursor:pointer;"></i>
                            <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
                        </td>
                    </tr>
                `;
        $attachmentTbody.append(row);
    }

    // Add initial row on page load
    addAttachmentRow();

    // Add new row on plus icon click or global Add Row icon
    $(document).on('click', '#tab3 .btn-add-row, #tab3 .btn-add-action', function () {
        addAttachmentRow();
    });

    // Delete row on trash icon click
    $(document).on('click', '.btn-delete-action', function () {
        $(this).closest('tr').remove();
    });

    //Attachment End Block
    $('#selectAllIOSM').on('change', function () {
        let checked = $(this).is(':checked');
        $('#tblindendorderstore tbody input[type="checkbox"]').prop('checked', checked).trigger('change');
    });

    $('#tblindendorderstore').on('change', 'input[type="checkbox"]', function () {
        let $row = $(this).closest('tr');

        // Extract required fields
        let itemCode = $row.find('td:eq(4)').text().trim();
        let reqNo = $row.find('td:eq(25)').text().trim();
        let vtype = $row.find('td:eq(2)').text().trim();
        let poNo = $row.find('td:eq(1)').text().trim();

        //Create a unique key combining multiple fields
        let uniqueKey = `${itemCode}_${reqNo}_${vtype}_${poNo}`;

        if (this.checked) {
            if ($('#tblPurchaseReceiptIR tbody tr[data-unique="' + uniqueKey + '"]').length > 0) return;

            let vdate = $row.find('td:eq(3)').text().trim();
            let itemName = $row.find('td:eq(5)').text().trim();
            let unit = $row.find('td:eq(6)').text().trim();
            let nos = $row.find('td:eq(7)').text().trim();
            let qty = $row.find('td:eq(8)').text().trim();
            let balqty = $row.find('td:eq(9)').text().trim();
            let rate = $row.find('td:eq(10)').text().trim();
            let taxtype = $row.find('td:eq(11)').text().trim();
            let pack = $row.find('td:eq(12)').text().trim();
            let disc = $row.find('td:eq(13)').text().trim();
            let cgst = $row.find('td:eq(14)').text().trim();
            let sgst = $row.find('td:eq(15)').text().trim();
            let igst = $row.find('td:eq(16)').text().trim();
            let cessPer = $row.find('td:eq(17)').text().trim();
            let cess = $row.find('td:eq(18)').text().trim();
            let vat = $row.find('td:eq(19)').text().trim();
            let othAmt = $row.find('td:eq(20)').text().trim();
            let make = $row.find('td:eq(21)').text().trim();
            let dept = $row.find('td:eq(22)').text().trim();
            let remarks = $row.find('td:eq(23)').text().trim();
            let reqType = $row.find('td:eq(24)').text().trim();
            let taxcode = $row.find('td:eq(29)').text().trim();

            let amount = (parseFloat(rate || 0) * parseFloat(qty || 0)).toFixed(2);

            let newRow = `
                    <tr data-unique="${uniqueKey}">
                        <td style="display:none;"><input type="text" class="form-control" name="Code" value="${itemCode}" /></td>
                        <td class="freeze-item"><select class="form-control item-name-dropdown" name="ItemName"></select></td>
                        <td><input type="text" class="form-control" name="HSNCode" /></td>
                        <td><input type="text" class="form-control" name="Unit" value="${unit}" /></td>
                        <td><input type="number" class="form-control" name="Nos" value="${nos}" /></td>
                        <td><input type="number" class="form-control" name="PlusMinusQty" /></td>
                        <td><input type="number" class="form-control" name="RecQty" value="${qty}" /></td>
                        <td><input type="number" class="form-control" name="BillQty" value="${qty}" /></td>
                        <td><input type="number" class="form-control" name="USDRate" /></td>
                        <td><input type="number" class="form-control" name="ExRate" /></td>
                        <td><input type="number" class="form-control" name="Rate" value="${rate}" /></td>
                        <td><input type="number" class="form-control" name="Amount" value="${amount}" /></td>
                        <td>
                            <select class="form-control EmptyYN-dropdown" name="EmptyYN">
                                <option value="">--Select--</option>
                                <option>Yes</option>
                                <option>No</option>
                            </select>
                        </td>
                        <td><input type="number" class="form-control" name="WBQty" /></td>
                        <td><select class="form-control TaxType-dropdown" name="TaxType" data-selected-value="${taxcode}"></select></td>
                        <td><input type="number" class="form-control" name="PackPer" value="${pack}" /></td>
                        <td><input type="number" class="form-control" name="PackAmt" /></td>
                        <td><input type="number" class="form-control" name="DiscPer" value="${disc}" /></td>
                        <td><input type="number" class="form-control" name="DiscAmt" /></td>
                        <td><input type="number" class="form-control" name="CGSTPer" value="${cgst}" /></td>
                        <td><input type="number" class="form-control" name="CGSTAmt" /></td>
                        <td><input type="number" class="form-control" name="SGSTPer" value="${sgst}" /></td>
                        <td><input type="number" class="form-control" name="SGSTAmt" /></td>
                        <td><input type="number" class="form-control" name="IGSTPer" value="${igst}" /></td>
                        <td><input type="number" class="form-control" name="IGSTAmt" /></td>
                        <td><input type="number" class="form-control" name="CESSPer" value="${cessPer}" /></td>
                        <td><input type="number" class="form-control" name="CESSAmt" value="${cess}" /></td>
                        <td><input type="number" class="form-control" name="VATPer" value="${vat}" /></td>
                        <td><input type="number" class="form-control" name="VATAmt" /></td>
                        <td><input type="number" class="form-control" name="OthAmt" value="${othAmt}" /></td>
                        <td><input type="number" class="form-control" name="NetAmt" /></td>
                        <td><input type="text" class="form-control" name="Make" value="${make}" /></td>
                        <td><input type="text" class="form-control" name="Department" value="${dept}" /></td>
                        <td><input type="text" class="form-control" name="Remarks" value="${remarks}" /></td>
                        <td><input type="number" class="form-control" name="LDRate" /></td>
                        <td><input type="number" class="form-control" name="LDAmt" /></td>
                        <td><input type="text" class="form-control" name="BinLocation" /></td>
                        <td><input type="text" class="form-control" name="POType" value="${vtype}" /></td>
                        <td><input type="text" class="form-control" name="PONo" value="${poNo}" /></td>
                        <td><input type="text" class="form-control" name="KantaType" /></td>
                        <td><input type="text" class="form-control" name="KantaNo" /></td>
                        <td><input type="text" class="form-control" name="ReqType" value="${reqType}" /></td>
                        <td><input type="text" class="form-control" name="ReqNo" value="${reqNo}" /></td>
                        <td><input type="text" class="form-control" name="GateType" /></td>
                        <td><input type="text" class="form-control" name="GateNo" /></td>
                        <td><button type="button" class="btn btn-danger btn-sm btn-delete-row">Delete</button></td>
                    </tr>
                `;
            
            $('#tblPurchaseReceiptIR tbody').append(newRow);

            let $lastRow = $('#tblPurchaseReceiptIR tbody tr').last();

            let $itemDropdown = $lastRow.find('select[name="ItemName"]');
            loadItemDropdown($itemDropdown, itemCode);

            let $taxDropdown = $lastRow.find('select[name="TaxType"]');
            loadTaxTypeDropdown($taxDropdown, taxtype);

        } else {
            // Remove row based on unique key
            $('#tblPurchaseReceiptIR tbody tr[data-unique="' + uniqueKey + '"]').remove();
        }
    });

    //==========Save and Update All Data=================
    $('#btn-save').on('click', function (e) {
        e.preventDefault(); 

        if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;
        if (!validateRequiredField('#DtDocDate', 'Doc Date')) return;
        if (!validateRequiredField('#ddlBillFrom', 'Bill From')) return;
        if (!validateRequiredField('#ddlShipFrom', 'Ship From')) return;
        if (!validateRequiredField('#txtBillNo', 'Bill No')) return;
        if (!validateRequiredField('#DtBillDate', 'Bill Date')) return;

        if (!validateRequiredField('#ddlReturnType', 'Return Type')) return;
        if (!validateRequiredField('#ddlDocStatus', 'Doc. Status')) return;
        //Header data
        var header = {
            DocType: $('#ddlDocType').val() || null,
            DocNo: $('#NumDocNo').val() || null,
            BillNo: $('#txtBillNo').val() || null,
            ChallanNo: $('#NumChallanNo').val() || null,
            WaybillNo: $('#TxtWaybillNo').val() || null,
            WaybillInvNo: $('#TxtWaybillInvNo').val() || 0,
            ReturnType: $('#ddlReturnType').val() || 0,
            DocStatus: $('#ddlDocStatus').val() || 0,
            DocDate: $('#DtDocDate').val() || 0,
            GateNo: $('#ddlGateNo').val() || null,
            BillDate: $('#DtBillDate').val() || null,
            ChallanDate: $('#DtChallanDate').val() || null,
            WaybillDate: $('#DtWaybillDate').val() || null,
            WaybillExpiry: $('#DtWaybillExpiry').val() || null,
            ExchangeRate: $('#txtExchangeRate').val() || null,
            NetAmount: $('#txtNetAmount').val() || null,
            BillFrom: $('#ddlBillFrom').val() || null,
            AddLine1: $('#txtAddLine1').val() || null,
            AddLine2: $('#txtAddLine2').val() || null,
            AddLine3: $('#txtAddLine3').val() || null,
            City: $('#ddlCity').val() || null,
            Pincode: $('#txtPincode').val() || null,
            State: $('#ddlState').val() || null,
            GST: $('#txtGST').val() || null,
            Remarks: $('#txtRemarks').val() || null,
            ShipFrom: $('#ddlShipFrom').val() || null,
            ShipAddLine1: $('#txtShipAddLine1').val() || null,
            ShipAddLine2: $('#txtShipAddLine2').val() || null,
            ShipAddLine3: $('#txtShipAddLine3').val() || null,
            ShipCity: $('#ddlShipCity').val() || null,
            ShipPincode: $('#NumShipPincode').val() || null,
            ShipState: $('#ddlShipState').val() || null,
            ShipGST: $('#txtShipGST').val() || null,
            TransportName: $('#ddlTransportName').val() || null,
            VehicleNo: $('#TxtVehicleNo').val() || null,
            ContainerNo: $('#TxtContainerNo').val() || null,
            FreightPay: $('#NumFreightPay').val() || null,
            FrtTax1: $('#NumFrtTax1').val() || null,
            FrtTax2: $('#NumFrtTax2').val() || null,
            FrtPayNarr: $('#txtFrtPayNarr').val() || null,
            GRNo: $('#NumGRNo').val() || null,
            GRDate: $('#DtGRDate').val() || null,
            NumReceivedQty: $('#NumReceivedQty').val() || null,
            NumBillQty: $('#NumBillQty').val() || null,
            NumAmount: $('#NumAmount').val() || null,
            NumPacking: $('#NumPacking').val() || null,
            NumDiscount: $('#NumDiscount').val() || null,
            NumCGST: $('#NumCGST').val() || null,
            NumSGST: $('#NumSGST').val() || null,
            NumIGST: $('#NumIGST').val() || null,
            NumCESS: $('#NumCESS').val() || null,
            NumVAT: $('#NumVAT').val() || null,
            NumOtherAmt: $('#NumOtherAmt').val() || null,
            NumTCSPer1: $('#NumTCSPer1').val() || null,
            NumTCSPer2: $('#NumTCSPer2').val() || null,
            NumSubTotal: $('#NumSubTotal').val() || null,
            NumRoundOff: $('#NumRoundOff').val() || null,
            NumFinalNetAmt: $('#NumFinalNetAmt').val() || null,
            ACTION: code > 0 ? "UPDATE" : "INSERT",
            code: code
        };

        //Items (from table rows)
        const allData = [];
        $('#tblPurchaseReceiptIR tbody tr').each(function () {
            const row = $(this);
            const rowData = {
                Code: row.find('input[name="Code"]').val() || null,
                ItemName: row.find('select[name="ItemName"]').val() || null,
                HSNCode: row.find('input[name="HSNCode"]').val() || null,
                Unit: row.find('input[name="Unit"]').val() || '',
                Nos: parseFloat(row.find('input[name="Nos"]').val()) || 0,
                PlusMinusQty: parseFloat(row.find('input[name="PlusMinusQty"]').val()) || 0,
                RecQty: parseFloat(row.find('input[name="RecQty"]').val()) || 0,
                BillQty: parseFloat(row.find('input[name="BillQty"]').val()) || 0,
                USDRate: parseFloat(row.find('input[name="USDRate"]').val()) || 0,
                ExRate: parseFloat(row.find('input[name="ExRate"]').val()) || 0,
                Rate: parseFloat(row.find('input[name="Rate"]').val()) || 0,
                Amount: parseFloat(row.find('input[name="Amount"]').val()) || 0,
                EmptyYN: row.find('select[name="EmptyYN"]').val() || null,
                WBQty: parseFloat(row.find('input[name="WBQty"]').val()) || 0,
                TaxType: row.find('select[name="TaxType"]').val() || null,
                PackPer: parseFloat(row.find('input[name="PackPer"]').val()) || 0,
                PackAmt: parseFloat(row.find('input[name="PackAmt"]').val()) || 0,
                DiscPer: parseFloat(row.find('input[name="DiscPer"]').val()) || 0,
                DiscAmt: parseFloat(row.find('input[name="DiscAmt"]').val()) || 0,
                CGSTPer: parseFloat(row.find('input[name="CGSTPer"]').val()) || 0,
                CGSTAmt: parseFloat(row.find('input[name="CGSTAmt"]').val()) || 0,
                SGSTPer: parseFloat(row.find('input[name="SGSTPer"]').val()) || 0,
                SGSTAmt: parseFloat(row.find('input[name="SGSTAmt"]').val()) || 0,
                IGSTPer: parseFloat(row.find('input[name="IGSTPer"]').val()) || 0,
                IGSTAmt: parseFloat(row.find('input[name="IGSTAmt"]').val()) || 0,
                CESSPer: parseFloat(row.find('input[name="CESSPer"]').val()) || 0,
                CESSAmt: parseFloat(row.find('input[name="CESSAmt"]').val()) || 0,
                VATPer: parseFloat(row.find('input[name="VATPer"]').val()) || 0,
                VATAmt: parseFloat(row.find('input[name="VATAmt"]').val()) || 0,
                OthAmt: parseFloat(row.find('input[name="OthAmt"]').val()) || 0,
                NetAmt: parseFloat(row.find('input[name="NetAmt"]').val()) || 0,
                Make: row.find('input[name="Make"]').val() || null,
                Department: row.find('input[name="Department"]').val() || null,
                Remarks: row.find('input[name="Remarks"]').val() || null,
                LDRate: parseFloat(row.find('input[name="LDRate"]').val()) || 0,
                LDAmt: parseFloat(row.find('input[name="LDAmt"]').val()) || 0,
                BinLocation: row.find('input[name="BinLocation"]').val() || null,
                POType: row.find('input[name="POType"]').val() || null,
                PONo: row.find('input[name="PONo"]').val() || null,
                KantaType: row.find('input[name="KantaType"]').val() || null,
                KantaNo: row.find('input[name="KantaNo"]').val() || null,
                ReqType: row.find('input[name="ReqType"]').val() || null,
                ReqNo: row.find('input[name="ReqNo"]').val() || null,
                GateType: row.find('input[name="GateType"]').val() || null,
                GateNo: row.find('input[name="GateNo"]').val() || ''
            };
            allData.push(rowData);
        });

        const formData = new FormData();
        formData.append("Header", JSON.stringify(header));
      
        allData.forEach((item, i) => {
            for (const key in item) {
                formData.append(`ItemDetails[${i}].${key}`, item[key]);
            }
        });

        //Attachments
        $('#tblAttachmentPRE tbody tr').each(function (index) {
            const fileName = $(this).find('input[type="text"]').val();
            const fileInput = $(this).find('input[type="file"]')[0];

            formData.append(`Attachments[${index}].FileName`, fileName || '');

            if (fileInput && fileInput.files.length > 0) {
                formData.append(`Attachments[${index}].File`, fileInput.files[0]);
            }
        });

        $('#tblAttachmentPRE tbody tr').each(function (index) {
            const fileName = $(this).find('input[type="text"]').val();
            const fileInput = $(this).find('input[type="file"]')[0];

            formData.append(`Attachments[${index}].FileName`, fileName || '');

            if (fileInput && fileInput.files.length > 0) {
                formData.append(`Attachments[${index}].File`, fileInput.files[0]);
            }
        });

        $.ajax({
            url: '/PurchaseReceiptEntry/SaveAllData',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            success: function (resp) {
                if (resp.status === "success") {
                    toastr.success(resp.message);
                    setTimeout(function () {
                        window.location.href = '/PurchaseReceiptEntryList';
                    }, 1500);
                } else {
                    toastr.error(resp.message || 'Something went wrong.');
                }
            },
            error: function (xhr, status, error) {
                toastr.error('Save failed: ' + error);
                console.error('AJAX Error:', status, error, xhr.responseText);
            }
        });

    });
});

function ddlDocType(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlDocType',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlDocType');
            ddl.empty().append('<option value="">-- Select Doc Type --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlDocType:", xhr.responseText);
        }
    });
}

function ddlDocStatus(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlDocStatus',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlDocStatus');
            ddl.empty().append('<option value="">-- Select Doc Status --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlDocStatus:", xhr.responseText);
        }
    });
}

function sendDocType(docType, docName) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetDocNo',
        type: 'POST',
        data: {
            docType: docType,
            docName: docName
        },
        success: function (response) {
            if (response.success) {

                const vNo = response.nextVNo;
                const vType = response.docType;

                $('#NumDocNo').val(vNo);
                ddlGateNo(vNo, vType);
            }
        },
        error: function (xhr) {
            console.error("Error:", xhr.responseText);
        }
    });
}

function ddlGateNo(vNo, vType1) {
    const ddl = $('#ddlGateNo');
    return $.ajax({
        url: '/PurchaseReceiptEntry/GetddlGateNo',
        type: 'GET',
        dataType: 'json',
        data: { VNo: vNo, Vtype: vType1 },
        success: function (data) {
            if (ddl.hasClass("select2-hidden-accessible")) {
                ddl.select2('destroy');
            }
            ddl.empty().append('<option value="" disabled selected hidden>-- Select Party --</option>');
            data.forEach(item => {
                ddl.append(new Option(item.text, item.value));
            });
            ddl.select2({
                placeholder: "-- Select Party --",
                allowClear: true,
                width: '100%'
            });
            $('.select2-selection').addClass('form-control');
        },
        error: function (xhr, status, error) {
            console.error("Error loading party list:", xhr.responseText);
        }
    });
}

function ddlBillFrom(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlBillFrom',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlBillFrom');
            ddl.empty().append('<option value="">-- Select Bill --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlBillFrom:", xhr.responseText);
        }
    });
}

function getBillFrom(code) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetBillDetails',
        type: 'POST',
        data: { code: code },
        success: function (response) {
            if (response) {
               
                $('#txtAddLine1').val(response.address1).prop('readonly', true);
                $('#txtAddLine2').val(response.address2).prop('readonly', true);
                $('#txtAddLine3').val(response.address3).prop('readonly', true);
                $('#txtPincode').val(response.pincode).prop('readonly', true);
                $('#txtGST').val(response.gstin).prop('readonly', true);

                $('#txtShipAddLine1').val(response.address1).prop('readonly', true);
                $('#txtShipAddLine2').val(response.address2).prop('readonly', true);
                $('#txtShipAddLine3').val(response.address3).prop('readonly', true);
                $('#NumShipPincode').val(response.pincode).prop('readonly', true);
                $('#txtShipGST').val(response.gstin).prop('readonly', true);

                GetddlCityBillDetails(function () {
                    $('#ddlCity').val(response.cityCode).prop('disabled', true);
                });

                GetddlstateBillDetails(function () {
                    $('#ddlState').val(response.stateCode).prop('disabled', true);
                });

                ddlShipDetails(function () {
                    $('#ddlShipFrom').val(code).prop('disabled', true);
                });

                GetddlCityShipDetails(function () {
                    $('#ddlShipCity').val(response.cityCode).prop('disabled', true);
                });

                GetddlstateShipDetails(function () {
                    $('#ddlShipState').val(response.stateCode).prop('disabled', true);
                });

            } else {
                console.warn("No supplier data found for code:", code);
            }
        },
        error: function (xhr) {
            console.error("Error:", xhr.responseText);
        }
    });

}

function getBillDetailsAddLine1(code, AddressID) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetBillDetailsAddLine1',
        type: 'POST',
        data: { code: code, AddressID: AddressID },
        success: function (response) {
            if (response) {
                $('#txtAddLine1').val(response.address1);
                $('#txtAddLine2').val(response.address2);
                $('#txtAddLine3').val(response.address3);
                GetddlCityBillDetails(function () {
                    $('#ddlCity').val(response.cityCode);
                })
                GetddlstateBillDetails(function () {
                    $('#ddlState').val(response.stateCode);
                })
                $('#txtPincode').val(response.pincode);
                $('#txtGST').val(response.gstin);
            } else {
                console.warn("No supplier data found for code:", code);
            }
        },
        error: function (xhr) {
            console.error("Error:", xhr.responseText);
        }
    });
}

function getShipDetailsAddLine1(code, AddressID) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetShipDetailsAddLine1',
        type: 'POST',
        data: { code: code, AddressID: AddressID },
        success: function (response) {
            if (response) {
                $('#txtShipAddLine1').val(response.address1);
                $('#txtShipAddLine2').val(response.address2);
                $('#txtShipAddLine3').val(response.address3);
                GetddlCityShipDetails(function () {
                    $('#ddlShipCity').val(response.cityCode);
                })
                GetddlstateShipDetails(function () {
                    $('#ddlShipState').val(response.stateCode);
                })
                $('#NumShipPincode').val(response.pincode);
                $('#txtShipGST').val(response.gstin);


            } else {
                console.warn("No supplier data found for code:", code);
            }
        },
        error: function (xhr) {
            console.error("Error:", xhr.responseText);
        }
    });
}

function BillDetailsddlAddLine1(code, callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetBillDetailsddlAddLine1',
        type: 'GET',
        dataType: 'json',
        data: { code: code },
        success: function (data) {
            const ddl = $('#ddladdressBD1');
            ddl.empty().append();
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddladdressBD1:", xhr.responseText);
        }
    });
}

function ShipDetailsddlAddLine1(code, callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetShipDetailsddlAddLine1',
        type: 'GET',
        dataType: 'json',
        data: { code: code },
        success: function (data) {
            const ddl = $('#ddladdressSD1');
            ddl.empty().append();
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddladdressSD1:", xhr.responseText);
        }
    });
}

function GetddlCityBillDetails(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlCityBillDetails',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlCity');
            ddl.empty().append('<option value="">-- Select City--</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlCity:", xhr.responseText);
        }
    });
}

function GetddlstateBillDetails(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlstateBillDetails',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlState');
            ddl.empty().append('<option value="">-- Select State --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlState:", xhr.responseText);
        }

    });
}

function GetddlCityShipDetails(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlCityShipDetails',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlShipCity');
            ddl.empty().append('<option value="">-- Select City--</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlShipCity:", xhr.responseText);
        }
    });
}

function GetddlstateShipDetails(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlstateShipDetails',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlShipState');
            ddl.empty().append('<option value="">-- Select State --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlShipState:", xhr.responseText);
        }
    });
}

function ddlShipDetails(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlShipDetails',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlShipFrom');
            ddl.empty().append('<option value="">-- Select Ship --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlShipFrom:", xhr.responseText);
        }
    });
}

//======Transport Name start ddl banding=================
function ddlTransportName(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlTransportName',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlTransportName');
            ddl.empty().append('<option value="">-- Transport Name --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlTransportName:", xhr.responseText);
        }
    });
}

//=========Transport Name End ddl banding============
function ddlOrdertype(callback) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetddlOrdertype',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const ddl = $('#ddlcopyFromDropdown');
            ddl.empty().append('<option value="">-- Order Type --</option>');
            $.each(data, function (index, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });
            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            console.error("Error loading ddlcopyFromDropdown:", xhr.responseText);
        }
    });
}

//=====Add Row in Table ===========
function addRow() {
    const tbody = $('#tblPurchaseReceiptIR tbody');
    const firstRow = tbody.find('tr:first');
    const newRow = firstRow.clone();
    newRow.find('input').val('');
    tbody.append(newRow);
    loadItemDropdown(newRow.find('.item-name-dropdown'));
    loadTaxTypeDropdown(newRow.find('.TaxType-dropdown'));
}

function loadItemDropdown(dropdown, selectedCode = "") {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetItemList',
        method: 'GET',
        success: function (data) {
            dropdown.empty();
            dropdown.append('<option value="">--Select Item--</option>');
            $.each(data, function (index, item) {
                let selected = item.code === selectedCode ? 'selected' : '';
                dropdown.append('<option value="' + item.code + '" ' + selected + '>' + item.name + '</option>');
            });
        },
        error: function () {
            toastr.error('Failed to load items.');
        }
    });
}

function getHSNCode(code, row) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetHSNCode',
        type: 'GET',
        data: { code: code },
        success: function (response) {

            row.find('input[name="HSNCode"]').val(response.hsnCode);
            row.find('input[name="Unit"]').val(response.unit);
        },
        error: function (xhr) {
            console.error("Error fetching HSNCode:", xhr.responseText);
        }
    });
}

function loadTaxTypeDropdown($dropdown, selectedCode = '') {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetTaxTypeList',
        method: 'GET',
        success: function (data) {
            $dropdown.empty();
            $dropdown.append('<option value="">--Select--</option>');
            $.each(data, function (index, item) {
                const selected = item.code === selectedCode ? ' selected' : '';
                $dropdown.append('<option value="' + item.code + '"' + selected + '>' + item.name + '</option>');
            });
        },
        error: function () {
            toastr.error('Failed to load Tax Types.');
        }
    });
}

//======Calulation Start ==========
function getTaxType(code, row) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetTaxTypeDetails',
        type: 'GET',
        data: { code: code },
        success: function (response) {
            const rate = parseFloat(row.find('input[name="Rate"]').val()) || 0;
            const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;
            const totalAmount = rate * billQty;

            row.find('input[name="Amount"]').val(totalAmount.toFixed(2));

            if (response) {
                const igstPer = parseFloat(response.igsT_PER) || 0;
                const cgstPer = parseFloat(response.cgsT_PER) || 0;
                const sgstPer = parseFloat(response.sgsT_PER) || 0;

                // Set tax percentages
                row.find('input[name="CGSTPer"]').val(cgstPer);
                row.find('input[name="SGSTPer"]').val(sgstPer);
                row.find('input[name="IGSTPer"]').val(igstPer);

                // Calculate tax amounts
                const CGSTAmt = (totalAmount * cgstPer) / 100;
                const SGSTAmt = (totalAmount * sgstPer) / 100;
                const IGSTAmt = (totalAmount * igstPer) / 100;

                row.find('input[name="CGSTAmt"]').val(CGSTAmt.toFixed(2));
                row.find('input[name="SGSTAmt"]').val(SGSTAmt.toFixed(2));
                row.find('input[name="IGSTAmt"]').val(IGSTAmt.toFixed(2));

                // Final calculation
                const NetAmt = totalAmount + CGSTAmt + SGSTAmt + IGSTAmt;
                const LDRate = billQty !== 0 ? NetAmt / billQty : 0;

                row.find('input[name="NetAmt"]').val(NetAmt.toFixed(2));
                row.find('input[name="LDAmt"]').val(NetAmt.toFixed(2));
                row.find('input[name="LDRate"]').val(LDRate.toFixed(2));

                // Recalculate totals
                recalculateRow(row);
                calculateTotalRecQty();
            }
        },
        error: function () {
            toastr.error("Failed to get item details.");
        }
    });
}

function recalculateRow(row) {

    const rate = parseFloat(row.find('input[name="Rate"]').val()) || 0;
    const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;

    const amount = rate * billQty;

    row.find('input[name="Amount"]').val(amount.toFixed(2));

    const packPer = parseFloat(row.find('input[name="PackPer"]').val()) || 0;
    const discPer = parseFloat(row.find('input[name="DiscPer"]').val()) || 0;

    const cgstPer = parseFloat(row.find('input[name="CGSTPer"]').val()) || 0;
    const sgstPer = parseFloat(row.find('input[name="SGSTPer"]').val()) || 0;
    const igstPer = parseFloat(row.find('input[name="IGSTPer"]').val()) || 0;
    const cessPer = parseFloat(row.find('input[name="CESSPer"]').val()) || 0;

    const otherAmt = parseFloat(row.find('input[name="OthAmt"]').val()) || 0;

    const packAmt = amount * packPer / 100;
    const discAmt = amount * discPer / 100;

    const taxable = amount + packAmt - discAmt;

    const cgstAmt = taxable * cgstPer / 100;
    const sgstAmt = taxable * sgstPer / 100;
    const igstAmt = taxable * igstPer / 100;
    const cessAmt = taxable * cessPer / 100;

    const vatAmt = parseFloat(row.find('input[name="VATAmt"]').val()) || 0;

    const ldAmt =
        amount +
        packAmt -
        discAmt +
        cessAmt +
        vatAmt +
        otherAmt;

    const netAmt =
        ldAmt +
        cgstAmt +
        sgstAmt +
        igstAmt;

    const ldRate = billQty > 0 ? netAmt / billQty : 0;

    row.find('input[name="PackAmt"]').val(packAmt.toFixed(2));
    row.find('input[name="DiscAmt"]').val(discAmt.toFixed(2));
    row.find('input[name="CGSTAmt"]').val(cgstAmt.toFixed(2));
    row.find('input[name="SGSTAmt"]').val(sgstAmt.toFixed(2));
    row.find('input[name="IGSTAmt"]').val(igstAmt.toFixed(2));
    row.find('input[name="CESSAmt"]').val(cessAmt.toFixed(2));

    row.find('input[name="LDAmt"]').val(ldAmt.toFixed(2));
    row.find('input[name="NetAmt"]').val(netAmt.toFixed(2));
    row.find('input[name="LDRate"]').val(ldRate.toFixed(2));
}

function calculateTotalRecQty() {
    let totalRecQty = 0;
    let totalBillQty = 0;
    let totalAmount = 0;
    let totalPacking = 0;
    let totalDiscount = 0;
    let totalCgst = 0;
    let totalSgst = 0;
    let totalIGST = 0;
    let totalCess = 0;
    let totalVAT = 0;
    let totalOtherAmt = 0;
    let SubTotal = 0;

    $('#tblPurchaseReceiptIR tbody tr').each(function () {
        const row = $(this);
        const recQty = parseFloat(row.find('input[name="RecQty"]').val()) || 0;
        const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;
        const totalAmountQty = parseFloat(row.find('input[name="Amount"]').val()) || 0;
        const PackingQty = parseFloat(row.find('input[name="PackAmt"]').val()) || 0;
        const DiscountQty = parseFloat(row.find('input[name="DiscAmt"]').val()) || 0;
        const CgstQty = parseFloat(row.find('input[name="CGSTAmt"]').val()) || 0;
        const SgstQty = parseFloat(row.find('input[name="SGSTAmt"]').val()) || 0;
        const IGSTQty = parseFloat(row.find('input[name="IGSTAmt"]').val()) || 0;
        const CESSQty = parseFloat(row.find('input[name="CESSAmt"]').val()) || 0;
        const VATQty = parseFloat(row.find('input[name="VATAmt"]').val()) || 0;
        const OtherAmtQty = parseFloat(row.find('input[name="OthAmt"]').val()) || 0;
        const SubTotalQty = parseFloat(row.find('input[name="NetAmt"]').val()) || 0;

        totalRecQty += recQty;
        totalBillQty += billQty;
        totalAmount += totalAmountQty;
        totalPacking += PackingQty;
        totalDiscount += DiscountQty;
        totalCgst += CgstQty;
        totalSgst += SgstQty;
        totalIGST += IGSTQty;
        totalCess += CESSQty;
        totalVAT += VATQty;
        totalOtherAmt += OtherAmtQty;
        SubTotal += SubTotalQty;
    });

    $('#NumReceivedQty').val(totalRecQty.toFixed(3));
    $('#NumBillQty').val(totalBillQty.toFixed(3));
    $('#NumAmount').val(totalAmount.toFixed(2));
    $('#NumPacking').val(totalPacking.toFixed(2));
    $('#NumDiscount').val(totalDiscount.toFixed(2));
    $('#NumCGST').val(totalCgst.toFixed(2));
    $('#NumSGST').val(totalSgst.toFixed(2));
    $('#NumIGST').val(totalIGST.toFixed(2));
    $('#NumCESS').val(totalCess.toFixed(2));
    $('#NumVAT').val(totalVAT.toFixed(2));
    $('#NumOtherAmt').val(totalOtherAmt.toFixed(2));
    // TCS Calculation
    const tcsPer = parseFloat($("#NumTCSPer1").val()) || 0;
    const tcsAmount = (SubTotal * tcsPer) / 100;

    $("#NumTCSPer2").val(tcsAmount.toFixed(2));

    // Sub Total = Net Total + TCS
    const finalAmount = SubTotal + tcsAmount;
    $("#NumSubTotal").val(finalAmount.toFixed(2));

    // Round Off
    const rounded = Math.round(finalAmount);
    $("#NumRoundOff").val((rounded - finalAmount).toFixed(2));

    // Final Net Amount
    $("#NumFinalNetAmt").val(rounded.toFixed(2));
  
}

//============Calculation End=================


//==========Bind All Data On change of Gate No=============
function GetalldatafetchGatonchange(response) {
    const header = response.header;

    ddlBillFrom(function () {

        ddlShipDetails(function () {

            $('#ddlBillFrom').val(header.PARTY_CODE);
            $('#ddlShipFrom').val(header.SHIP_PARTY);

            BillDetailsddlAddLine1(header.PARTY_CODE, function () {

                $('#ddladdressBD1').val(header.PARTY_ADDRESSID);
                $('#ddladdressBD1').trigger('change');

            });

            if (header.SHIP_PARTY && header.SHIP_PARTY != 0) {

                $('#ddlShipFrom').prop('disabled', false);
                $('#ddlShipFrom').val(header.SHIP_PARTY);

                ShipDetailsddlAddLine1(header.SHIP_PARTY, function () {

                    $('#ddladdressSD1').val(header.PARTY_ADDRESSID);
                    $('#ddladdressSD1').trigger('change');

                });

            } else {

                $('#ddlShipFrom').val(header.PARTY_CODE);
                $('#ddlShipFrom').prop('disabled', true);

                ShipDetailsddlAddLine1(header.PARTY_CODE, function () {

                    $('#ddladdressSD1').val(header.PARTY_ADDRESSID);
                    $('#ddladdressSD1').trigger('change');

                });

            }

        });

    });

    $('#txtAddLine1, #txtShipAddLine1').val(header.ADD1);
    $('#txtAddLine2, #txtShipAddLine2').val(header.ADD2);
    $('#txtAddLine3, #txtShipAddLine3').val(header.ADD3);

    GetddlCityBillDetails(() => {
        $('#ddlCity, #ddlShipCity').val(header.CITY_CODE);
    });

    GetddlstateBillDetails(() => {
        $('#ddlState, #ddlShipState').val(header.StateCode);
    });

    $('#txtPincode, #NumShipPincode').val(header.PARTY_PINCODE);
    $('#txtGST, #txtShipGST').val(header.GSTIN || '');

    //=========================
    // Documents Detail
    //=========================
    $('#txtBillNo').val(header.BILL_NO);
    $('#DtBillDate').val(header.BILL_DATE?.split('T')[0] || '');
    $('#NumChallanNo').val(header.CHALL_NO || '');
    $('#DtChallanDate').val(header.CHALL_DATE?.split('T')[0] || '');
    $('#TxtWaybillNo').val(header.WAYBILL_NO || header.EWB_INVNO || '');
    $('#TxtWaybillInvNo').val(header.EWB_INVNO || '');
    $('#DtWaybillExpiry').val(header.EWB_EXPDATE?.split('T')[0] || '');
    $('#DtWaybillDate').val(header.EWB_DATE?.split('T')[0] || '');
    //$('#txtExchangeRate').val(header.|| '');
    //$('#txtNetAmount').val( || '');

    //==========================
    // Transport Details
    //==========================
    $("#ddlTransportName").val(header.TRANSPORT_CODE).trigger("change");
    $("#TxtVehicleNo").val(header.TRUCK_NO || "");
    //$("#TxtContainerNo").val(header.CONTAINER_NO || "");
    $("#NumFreightPay").val(header.FREIGHT_PAY || 0);
    $("#NumFrtTax1").val(header.FRT_TAX_PER || 0);
    $("#NumFrtTax2").val(header.FRT_TAX_AMT || 0);
    $("#txtFrtPayNarr").val(header.FRT_PAY_NARR || "");
    $("#NumGRNo").val(header.GR_NO || "");
    $("#DtGRDate").val(header.GR_DATE?.split("T")[0] || "");
    if (header.ContainerList && header.ContainerList.length > 0) {
        $("#TxtContainerNo").val(header.ContainerList[0]);
    } else {
        $("#TxtContainerNo").val("");
    }

    //=================
    // FooterData
    //==================
    bindItems(response.items);


    //==========================
    // Payment & TCS
    //==========================
    if (response.items && response.items.length > 0) {

        $("#NumTCSPer1").val(response.items[0].TCS_PER || 0);

        $("#ddlPaymentIT").val(response.items[0].Payment).trigger("change");

        if (response.items[0].IsHold) {

            if (!$("#DtHoldDate").val()) {
                $("#DtHoldDate").val($("#DtDocDate").val());
            }

            $("#ddlPaymentIT").prop("disabled", true);
            $("#DtHoldDate").prop("disabled", true);
        }
        else {

            $("#ddlPaymentIT").prop("disabled", false);
            $("#DtHoldDate").prop("disabled", false);
        }
    }
}

//======Bind Items(On change of gate no)========
function bindItems(items) {
    recalculateRow($(this));

    const $tbody = $('#tblPurchaseReceiptIR tbody').empty();

    items.forEach(i => {
        const qty = i.QTY ?? 0;
        const recQty = i.RecQty ?? 0;
        const billQty = i.QTY ?? 0;
        const rate = i.RATE ?? 0;
        //const amount = qty * rate;
        const amount = (qty * rate).toFixed(2);

        const $tr = $(`
                <tr class="no-border-input">
                    <td class="freeze-item">
                        <select class="erppagetable-control erppagetabledynamic-table item-name-dropdown" name="ItemName"></select>
                    </td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="HSNCode" value="${i.HSN_CODE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Unit" value="${i.Unit || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Nos" value="${fmt(i.NOS ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="PlusMinusQty" value="${fmt(i.PlusMinusQty ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="RecQty" value="${fmt(recQty)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="BillQty" value="${fmt(billQty)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="USDRate" value="${fmt(i.USDRate ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="ExRate" value="${fmt(i.ExRate ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Rate" value="${fmt(rate)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Amount" value="${amount}" /></td>
                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table EmptyYN-dropdown" name="EmptyYN">
                            <option value="Yes" ${i.EMPTY === 'Yes' ? 'selected' : ''}>Yes</option>
                            <option value="No" ${i.EMPTY !== 'Yes' ? 'selected' : ''}>No</option>
                        </select>
                    </td>
                    <td><input name="WBQty"  class="erppagetable-control erppagetabledynamic-table" value="${fmt(i.WBQty ?? 0)}" /></td>
                    <td><select class="erppagetable-control erppagetabledynamic-table TaxType-dropdown" name="TaxType"></select></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="PackPer" value="${fmt(i.PACK_PER ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="PackAmt" value="${fmt(i.PACK_AMT ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="DiscPer" value="${fmt(i.DISC_PER ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="DiscAmt" value="${fmt(i.DISC_AMT ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="CGSTPer" value="${fmt(i.CGST_PER ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="CGSTAmt" value="${fmt(i.CGSTAmt ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="SGSTPer" value="${fmt(i.SGST_PER ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="SGSTAmt" value="${fmt(i.SGSTAmt ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="IGSTPer" value="${fmt(i.IGST_PER ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="IGSTAmt" value="${fmt(i.IGSTAmt ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="CESSPer" value="${fmt(i.CESS_PER ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="CESSAmt" value="${fmt(i.CESS_AMT ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="VATPer" value="${fmt(i.VATPer ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="VATAmt" value="${fmt(i.VATAmt ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="OthAmt" value="${fmt(i.OTH_AMT ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="NetAmt" value="${fmt(i.NetAmt ?? amount)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="Make" value="${i.Make || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="Department" value="${i.Department || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="Remarks" value="${i.Remarks || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="LDRate" value="${fmt(i.LDRate ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="LDAmt" value="${fmt(i.LDAmt ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="BinLocation" value="${i.BinLocation || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="POType" value="${i.REF_TYPE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="PONo" value="${i.REF_NO ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="KantaType" value="${i.KantaType || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="KantaNo" value="${i.KantaNo || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="ReqType" value="${i.REQUEST_TYPE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="ReqNo" value="${i.REQUEST_NO ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="GateType" value="${i.v_type || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="GateNo" value="${i.v_no ?? 0}" /></td>
                    <td class="action-col">
                        <div class="action-wrap">
                            <button class="act-btn edit btn-edit" title="Edit Row" style="cursor:pointer;" ><i class="fa fa-edit"></i></button>
                            <button class="act-btn delete btn-delete btn-delete-row" title="Delete Row" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                        </div>
              
                    </td>
                </tr>
            `);

        $tbody.append($tr);

        const $lastRow = $tbody.find('tr').last();
        const recQtyInput = $lastRow.find('input[name="RecQty"]');
        const plusMinusInput = $lastRow.find('input[name="PlusMinusQty"]');

        if (i.WB_YN === "Yes") {

            recQtyInput.prop("readonly", true);
            plusMinusInput.prop("readonly", false);

        }
        else {

            recQtyInput.prop("readonly", false);
            plusMinusInput.prop("readonly", true);

        }

        const $itemDropdown = $lastRow.find('select[name="ItemName"]');
        const $taxDropdown = $lastRow.find('select[name="TaxType"]');


        loadItemDropdowngat($itemDropdown, i.ITEM_CODE || i.ITEM_NAME);
        loadTaxTypeDropdowngat($taxDropdown, i.TaxType);
        recalculateRow($lastRow);
    });

    //Delete row handler
    $tbody.off('click', '.btn-delete').on('click', '.btn-delete', function () {
        $(this).closest('tr').remove();
        calculateTotalRecQty();
    });
    calculateTotalRecQty();
}

function loadItemDropdowngat($dropdown, selectedItem = "") {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetItemList',
        method: 'GET',
        success: function (data) {
            $dropdown.empty();
            $dropdown.append('<option value="">--Select Item--</option>');

            const selectedValue = String(selectedItem).trim().toLowerCase();

            $.each(data, function (index, item) {
                const itemCode = String(item.code).trim().toLowerCase();
                const itemName = String(item.name).trim().toLowerCase();

                const isSelected = (selectedValue === itemCode || selectedValue === itemName) ? 'selected' : '';
                $dropdown.append(`<option value="${item.code}" ${isSelected}>${item.name}</option>`);
            });
        },
        error: function () {
            toastr.error('Failed to load items.');
        }
    });
}

//=======Tax Tpe Dropdown Load Function=======
function loadTaxTypeDropdowngat($dropdown, selectedType = "") {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetTaxTypeList',
        method: 'GET',
        success: function (data) {
            $dropdown.empty();
            $dropdown.append('<option value="">--Select Tax Type--</option>');
            $.each(data, function (index, tax) {
                let selected = tax.name == selectedType ? 'selected' : '';
                $dropdown.append(`<option value="${tax.name}" ${selected}>${tax.name}</option>`);
            });
        },
        error: function () {
            toastr.error('Failed to load tax types.');
        }
    });
}

function GetallDatapurchase1purchase2purchase3(data) {
    const purchase1 = data.purchase1;
    const purchase2 = data.purchase2;
    const purchase3 = data.purchase3;

    // ===== Purchase1 Header Bind ======
    if (purchase1 && purchase1.length > 0) {
        const p1 = purchase1[0];

        ddlDocType(() => {
            $('#ddlDocType').val(p1.v_TYPE);
        });

        $('#NumDocNo').val(p1.doC_ID);
        $('#DtDocDate').val(formatDateForInput(p1.v_DATE));

        ddlGateNo(p1.gatE_NO);
        ddlBillFrom(() => {
            $('#ddlBillFrom').val(p1.partY_CODE);
            $('#ddlShipFrom').val(p1.shiP_CODE);
        });

        $('#txtAddLine1').val(p1.bilL_ADD1);
        $('#txtAddLine2').val(p1.bilL_ADD2);
        $('#txtAddLine3').val(p1.bilL_ADD3);

        $('#txtShipAddLine1').val(p1.shiP_ADD1);
        $('#txtShipAddLine2').val(p1.shiP_ADD2);
        $('#txtShipAddLine3').val(p1.shiP_ADD3);

        GetddlCityBillDetails(() => {
            $('#ddlCity').val(p1.bilL_CITY);
            $('#ddlShipCity').val(p1.shiP_CITY);
        });

        GetddlstateBillDetails(() => {
            $('#ddlState').val(p1.bilL_ADDRESSID);
        });

        GetddlstateShipDetails(() => {
            $('#ddlShipState').val(p1.shiP_ADDRESSID);
        });

        $('#txtPincode').val(p1.bilL_PINCODE);
        $('#NumShipPincode').val(p1.shiP_PINCODE);

        $('#txtGST').val(p1.bilL_GST);
        $('#txtShipGST').val(p1.shiP_GST);

        $('#txtBillNo').val(p1.bilL_NO);
        $('#DtBillDate').val(formatDateForInput(p1.bilL_DATE));
        $('#NumChallanNo').val(p1.chalL_NO);
        $('#DtChallanDate').val(formatDateForInput(p1.chalL_DATE));
        $('#TxtWaybillNo').val(p1.waybilL_NO);

        $('#txtExchangeRate').val(p1.excH_RATE);
        $('#txtNetAmount').val(p1.namount);
        //=====Item Total======
        $('#NumReceivedQty').val(p1.recD_QTY);
        $('#NumBillQty').val(p1.bilL_QTY);
        $('#NumAmount').val(p1.amount);
        $('#NumPacking').val(p1.pacK_AMT);
        $('#NumDiscount').val(p1.disC_AMT);
        $('#NumCGST').val(p1.cgsT_AMT);
        $('#NumSGST').val(p1.sgsT_AMT);
        $('#NumIGST').val(p1.igsT_AMT);
        $('#NumCESS').val(p1.cesS_AMT);
        $('#NumVAT').val(p1.vaT_AMT);
        $('#NumOtherAmt').val(p1.otH_AMT);
        $('#NumSubTotal').val(p1.amount);
        $('#NumRoundOff').val(p1.rounD_OFF);
        $('#NumFinalNetAmt').val(p1.namount);

        $('#TxtVehicleNo').val(p1.trucK_NO);
        $('#TxtContainerNo').val(p1.containeR_NO);;
        $('#NumFreightPay').val(p1.frtpaY_AMT);
        $('#NumFrtTax1').val(p1.frtpaY_TAXPER);
        $('#NumFrtTax2').val(p1.frtpaY_TAX);
        $('#txtFrtPayNarr').val(p1.frtpaY_NAR);
        $('#NumGRNo').val(p1.gR_NO);

        $('#txtremarks').val(p1.remarks);

        $('#DtGRDate').val(formatDateForInput(p1.gR_DATE));

        ddlTransportName(() => {
            $('#ddlTransportName').val(p1.transporT_NAME);
        });

        ddlTransportName(() => {
            $('#ddlTransportName').val(p1.transporT_NAME);
        });

        ddlDocStatus(() => {
            $('#ddlDocStatus').val(p1.status);
        });

        BillDetailsddlAddLine1(p1.partY_CODE);
        ShipDetailsddlAddLine1(p1.shiP_CODE);

    }
    // ====== Purchase2 Item Rows Bind ======
    bindItemsdata(purchase2);
    getdataImage(purchase3)
}

//=======Helper for Date=======
function formatDateForInput(dateString) {
    if (!dateString) return "";
    const dateObj = new Date(dateString);
    if (isNaN(dateObj)) return "";
    const year = dateObj.getFullYear();
    const month = String(dateObj.getMonth() + 1).padStart(2, '0');
    const day = String(dateObj.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

//=======Bind Purchase2 Item Rows=======
function bindItemsdata(items) {

    const $tbody = $('#tblPurchaseReceiptIR tbody').empty();
    items.forEach(i => {
        const qty = i.recD_QTY ?? 0;
        const rate = i.rate ?? 0;
        const amount = qty * rate;

        const $tr = $(`
                <tr>
                    <td>
                        <select class="form-control item-name-dropdown" name="ItemName" style="width:200px;"></select>
                    </td>
                    <td><input class="form-control" name="HSNCode" value="${i.hsN_CODE || ''}" /></td>
                    <td><input class="form-control" name="Unit" value="${i.uoM_NAME || ''}" /></td>
                    <td><input class="form-control" name="Nos" value="${i.nos ?? 0}" /></td>
                    <td><input class="form-control" name="PlusMinusQty" value="${i.pluS_MINUSQTY ?? 0}" /></td>
                    <td><input class="form-control" name="RecQty" value="${i.recD_QTY ?? 0}" /></td>
                    <td><input class="form-control" name="BillQty" value="${i.bilL_QTY ?? 0}" /></td>
                    <td><input class="form-control" name="USDRate" value="${i.usD_RATE ?? 0}" /></td>
                    <td><input class="form-control" name="ExRate" value="${i.excH_RATE ?? 0}" /></td>
                    <td><input class="form-control" name="Rate" value="${i.rate ?? 0}" /></td>
                    <td><input class="form-control" name="Amount" value="${amount}" /></td>
                    <td>
                        <select class="form-control EmptyYN-dropdown" name="EmptyYN">
                            <option value="">--Select--</option>
                            <option value="Yes" ${i.emptY_YN === 'Yes' ? 'selected' : ''}>Yes</option>
                            <option value="No" ${i.emptY_YN === 'No' ? 'selected' : ''}>No</option>
                        </select>
                    </td>
                    <td><input name="WBQty" class="form-control" style="width: 100PX;" value="${i.wB_QTY ?? 0}" /></td>

                    <td><select class="form-control TaxType-dropdown" name="TaxType"></select></td>

                    <td><input class="form-control" name="PackPer" value="${i.pacK_PER ?? 0}" /></td>
                    <td><input class="form-control" name="PackAmt" value="${i.pacK_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="DiscPer" value="${i.disC_PER ?? 0}" /></td>
                    <td><input class="form-control" name="DiscAmt" value="${i.disC_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="CGSTPer" value="${i.cgsT_PER ?? 0}" /></td>
                    <td><input class="form-control" name="CGSTAmt" value="${i.cgsT_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="SGSTPer" value="${i.sgsT_PER ?? 0}" /></td>
                    <td><input class="form-control" name="SGSTAmt" value="${i.sgsT_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="IGSTPer" value="${i.igsT_PER ?? 0}" /></td>
                    <td><input class="form-control" name="IGSTAmt" value="${i.igsT_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="CESSPer" value="${i.cesS_PER ?? 0}" /></td>
                    <td><input class="form-control" name="CESSAmt" value="${i.cesS_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="VATPer" value="${i.vaT_PER ?? 0}" /></td>
                    <td><input class="form-control" name="VATAmt" value="${i.vaT_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="OthAmt" value="${i.otH_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="NetAmt" value="${i.neT_AMT ?? amount}" /></td>
                    <td><input class="form-control" name="Make" value="${i.makE_CODE ?? ''}" /></td>
                    <td><input class="form-control" name="Department" value="${i.depT_CODE ?? ''}" /></td>
                    <td><input class="form-control" name="Remarks" value="${i.remarks || ''}" /></td>
                    <td><input class="form-control" name="LDRate" value="${i.lanD_RATE ?? 0}" /></td>
                    <td><input class="form-control" name="LDAmt" value="${i.lanD_AMT ?? 0}" /></td>
                    <td><input class="form-control" name="BinLocation" value="${i.biN_LOCATION || ''}" /></td>
                    <td><input class="form-control" name="POType" value="${i.pO_TYPE || ''}" /></td>
                    <td><input class="form-control" name="PONo" value="${i.pO_NO || ''}" /></td>
                    <td><input class="form-control" name="KantaType" value="${i.kantA_TYPE || ''}" /></td>
                    <td><input class="form-control" name="KantaNo" value="${i.kantA_NO || ''}" /></td>
                    <td><input class="form-control" name="ReqType" value="${i.reQ_TYPE || ''}" /></td>
                    <td><input class="form-control" name="ReqNo" value="${i.reQ_NO || ''}" /></td>
                    <td><input class="form-control" name="GateType" value="${i.gatE_TYPE || ''}" /></td>
                    <td><input class="form-control" name="GateNo" value="${i.doC_ID || ''}" /></td>
                    <td>
                        <i class="fa fa-edit btn-edit"></i>
                        <i class="fas fa-trash btn-delete btn-delete-row"></i>
                    </td>
                </tr>
            `);
        $tbody.append($tr);
        let $lastRow = $tbody.find('tr').last();
        let $itemDropdown = $lastRow.find('select[name="ItemName"]');
        let $taxDropdown = $lastRow.find('select[name="TaxType"]');

        loadItemDropdowngat($itemDropdown, i.iteM_CODE);
        loadTaxTypeDropdowngatCode($taxDropdown, i.taX_CODE);
    });

    $tbody.off('click', '.btn-delete').on('click', '.btn-delete', function () {
        $(this).closest('tr').remove();
    });
}

function loadTaxTypeDropdowngatCode($dropdown, selectedType = "") {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetTaxTypeList',
        method: 'GET',
        success: function (data) {
            $dropdown.empty();
            $dropdown.append('<option value="">--Select Tax Type--</option>');
            $.each(data, function (index, tax) {
                let selected = String(tax.code) === String(selectedType) ? 'selected' : '';
                $dropdown.append(`<option value="${tax.code}" ${selected}>${tax.name}</option>`);
            });
        },
        error: function () {
            toastr.error('Failed to load tax types.');
        }
    });
}

//================= Attachment Data Bind =====================
function getdataImage(Imagedata) {
    const $attachmentTbody = $('#tblAttachmentPRE tbody');
    $attachmentTbody.empty();

    if (!Array.isArray(Imagedata) || Imagedata.length === 0) {
        console.warn("⚠️ No attachment data to display");
        return;
    }

    Imagedata.forEach(item => {
        const fullPath = item.attachment; 
        if (!fullPath) return;

        const fileName = fullPath.split('/').pop();

        let previewHtml = '';
        if (/\.(jpg|jpeg|png|gif|bmp)$/i.test(fileName)) {
            previewHtml = `
                    <img src="${fullPath}" alt="${fileName}"
                         style="max-width:80px; max-height:80px; border:1px solid #ccc; border-radius:4px;" />`;
        } else {
            previewHtml = `<a href="${fullPath}" target="_blank" class="text-info">View File</a>`;
        }

        const row = `
                <tr>
                    <td >${item.doC_ID || ''}</td>
                    <td style="display:none;"><label>${fileName}</label></td>
                    <td><input type="file" class="form-control file-upload" /></td>
                    <td>${previewHtml}</td>
                    <td>
                        <i class="fa fa-plus btn-add-action text-success me-2" title="Add Row" style="cursor:pointer;"></i>
                        <i class="fa fa-edit btn-edit-action text-primary me-2" title="Edit Row" style="cursor:pointer;"></i>
                        <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
                    </td>
                </tr>
            `;
        $attachmentTbody.append(row);
    });
}

//=========ReadOnly Mode=============
function setFormReadOnly() {
    $('#PurchaseReceiptEntryForm input').prop('readonly', true);
    $('#PurchaseReceiptEntryForm select').prop('disabled', true);
    $('#customToggle').css('pointer-events', 'none');
    $('#btn-save').hide();

    $('#tblPurchaseReceiptIR')
        .find('input, select, textarea')
        .each(function () {
            if ($(this).is('input, textarea')) {
                $(this).prop('readonly', true);
            } else if ($(this).is('select')) {
                $(this).prop('disabled', true);
            }
        });
    $('#tblPurchaseReceiptIR .btn-edit, #tblPurchaseReceiptIR .btn-delete, #tblPurchaseReceiptIR .btn-delete-row').hide();
    $('#tblPurchaseReceiptIR').closest('.card').find('.btn-add-row').hide();
    $('#tblPurchaseReceiptIR')
        .find('*')
        .css('pointer-events', 'none');
}

//=====helper function for decimal ============
function fmt(val) {
    const num = parseFloat(val);
    return isNaN(num) ? "0.00" : num.toFixed(2);
}