var ddlGateNo = "";
let itemList = [];
let taxList = [];
let makeList = [];
let deptList = [];
let readOnly = false;
let isEditMode = false;
let rowsAttachment = [];
function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}
const code = getQueryParam('vNo');
const vType = getQueryParam('vType');
const mode = getQueryParam("mode");
$(document).ready(function () {
    ddlDocType();
    ddlRefType();
    ddlDocStatus();
    ddlReturnTo();
    GetddlCityBillDetails();
    GetddlstateBillDetails();
    GetddlCityShipDetails();
    GetddlstateShipDetails();
    ddlShipDetails();
    //ddlTransportName();
    bindDropdownNew('ImportExportExpensesEntry', 'TransportName', '#ddlTransportName', '-- Select Tran --');
    ddlTransportAC()
    ddlCreditAC();
    ddlDebitAC();
    ddlFreightCreditAC()
    ddlFreightDebitAC()
    ddlDocStatus(() => {
        $('#ddlDocStatus').val(1).prop('disabled', true);
    });

    const today = new Date().toISOString().split('T')[0];
    document.getElementById('DtDocDate').value = today;
    checkApprovalStatus(vType, code, 'PURCHASE1');
    readOnly = (mode === 'view');
    if (code) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetAllDatadetails',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ VNO: code, vType: vType }),
            success: function (response) {
                isEditMode = true;
                GetallDatapurchase1purchase2purchase3(response);
                if (readOnly) {
                    setTimeout(function () {
                        makePageReadOnly();
                    }, 100);
                }
            },
            error: function (xhr) {
                toastr.error('Error: ' + xhr.responseText);
            }
        });
    }
    // When document type changes
    $('#ddlDocType').on('change', function () {
        const selectedValue = $(this).val();
        const selectedText = $(this).find("option:selected").text();
        $('#NumDocNo').prop('readonly', true);
        if (selectedValue !== "") {
            sendDocType(selectedValue, selectedText);
        }
    });
    // Ref Type dropdown list banding Start
    $('#ddlRefType').on('change', function () {
        const selectedValue = $(this).val();
        const selectedText = $(this).find("option:selected").text();
        ddlRefNo(selectedValue);
    });
    // Ref Type dropdown list banding End
    // How to get data on change start Block
    $('#ddlRefNo').on('change', function () {
        var selectedNo = $(this).val();
        var selectedText = $(this).find("option:selected").text();
        if (selectedNo == "0" || selectedNo == "")
            return;
        $.ajax({
            url: '/ImportExportExpensesEntry/GetRefNoList',
            type: 'POST',
            data: {
                StrVNo: selectedNo,
                StrV_type: selectedText.substring(0, 4)
            },
            success: function (res) {
                if (!res.success) {
                    alert(res.message);
                    return;
                }
                GetalldatafetchGatonchange(res.data);
            },
            error: function (xhr) {
                alert("Server Error");
            }
        });
    });
    $('#ddlBillFrom').on('change', function () {
        const selectedBillFrom = $(this).val();
        if (selectedBillFrom !== "") {
            getBillFrom(selectedBillFrom);
        }
    });
    // Add new row
    $('#tblItemRecordPO tbody tr').each(function () {
        loadItemDropdown($(this).find('.item-name-dropdown'));
        loadTaxTypeDropdown($(this).find('.TaxType-dropdown'));
        loadMakeList($(this).find('.Make-dropdown'));
        loadDeptList($(this).find('.Department-dropdown'));

    });
    $('#btnAddRow').on('click', function () {
        addRow();
    });
    // Delete row
    $(document).on('click', '.btn-delete-row', function () {
        if ($('#tblItemRecordPO tbody tr').length > 1) {
            $(this).closest('tr').remove();
        } else {
            toastr.warning('At least one row is required.');
        }
    });

    $(document).on('change', '.item-name-dropdown', function () {
        const selectedCode = $(this).val();
        const row = $(this).closest('tr');
        getHSNCode(selectedCode, row);
        getItemMakeList(selectedCode);
    })

    $(document).on('change', 'input[name="BillQty"], input[name="Rate"]', function () {
        const row = $(this).closest('tr');
        const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;
        const rate = parseFloat(row.find('input[name="Rate"]').val()) || 0;
        const amount = billQty * rate;
        row.find('input[name="Amount"]').val(amount.toFixed(2));
    });
    // allData calculation this method onanimation start block
    $(document).on('change', '.TaxType-dropdown', function () {
        const selectedCode = $(this).val();
        const row = $(this).closest('tr');
        getTaxType(selectedCode, row);
    })
    // checked last time when page is complate
    $(document).on('input', '#tblItemRecordPO tbody input', function () {
        const row = $(this).closest('tr');
        calculateTotalRecQty();
        recalculateRow(row);
    });
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

    $(document).on('click', '.btn-add-row', function () {
        const currentRow = $(this).closest('tr');
        const newRow = currentRow.clone();
        // Clear values
        newRow.find('input').val('');
        newRow.find('select').prop('selectedIndex', 0);
        // New row editable
        newRow.find('input').prop('readonly', false);
        newRow.find('select').prop('disabled', false);

        // Reload dropdowns
        loadItemDropdown(newRow.find('.item-name-dropdown'));
        loadTaxTypeDropdown(newRow.find('.TaxType-dropdown'));
        loadMakeList(newRow.find('.make-dropdown'));
        loadDeptList(newRow.find('select[name="Department"]'));

        currentRow.after(newRow);
    });

    $(document).on('click', '.btn-delete', function () {
        $(this).closest('tr').remove();
    });
    $(document).on('click', '.btn-edit', function () {
        const row = $(this).closest('tr');
        row.find('input').prop('readonly', false);
        row.find('select').prop('disabled', false);
    });
    // Delete row on trash icon click
    $('#selectAllIOSM').on('change', function () {
        let checked = $(this).is(':checked');
        $('#tblindendorderstore tbody input[type="checkbox"]').prop('checked', checked).trigger('change');
    });
    // checked box checked multiple row insert onanimationstart Block
    $('#tblindendorderstore').on('change', 'input[type="checkbox"]', function () {
        let $row = $(this).closest('tr');

        // Extract required fields
        let itemCode = $row.find('td:eq(4)').text().trim();
        let reqNo = $row.find('td:eq(25)').text().trim();
        let vtype = $row.find('td:eq(2)').text().trim();
        let poNo = $row.find('td:eq(1)').text().trim();
        let uniqueKey = `${itemCode}_${reqNo}_${vtype}_${poNo}`;

        if (this.checked) {
            dubgger;
            // ✅ Check if row with this unique key already exists
            if ($('#tblItemRecordPO tbody tr[data-unique="' + uniqueKey + '"]').length > 0) return;

            // Continue extracting values
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
            let amount = (parseFloat(rate || 0) * parseFloat(qty || 0)).toFixed(2);
            let newRow = `
                <tr data-unique="${uniqueKey}">
                    <td style="display:none;"><input type="text" class="form-control" name="Code" value="${itemCode}" /></td>
                    <td><select class="form-control item-name-dropdown" name="ItemName"></select></td>
                    <td><input type="text" class="form-control" name="HSNCode" value="1402" /></td>
                    <td><input type="text" class="form-control" name="Unit" value="${unit}" /></td>

                    <td><input type="number" class="form-control" name="PackPer" value="${pack}" /></td>
                    <td><input type="number" class="form-control" name="PackAmt" /></td>
                    <td><input type="number" class="form-control" name="DiscPer" value="${disc}" /></td>
                    <td><input type="number" class="form-control" name="DiscAmt" /></td>
                    <td><input type="number" class="form-control" name="WBQty" /></td>
                    <td><input type="number" class="form-control" name="CGSTPer" value="${cgst}" /></td>
                    <td><input type="number" class="form-control" name="CGSTAmt" /></td>
                    <td><input type="number" class="form-control" name="SGSTPer" value="${sgst}" /></td>
                    <td><input type="number" class="form-control" name="SGSTAmt" /></td>
                    <td><input type="number" class="form-control" name="IGSTPer" value="${igst}" /></td>
                    <td><input type="number" class="form-control" name="IGSTAmt" /></td>
                    <td><input type="number" class="form-control" name="CESSPer" value="${cessPer}" /></td>
                    <td><input type="number" class="form-control" name="CESSAmt" value="${cess}" /></td>
                    <td><input type="number" class="form-control" name="OthAmt" value="${othAmt}" /></td>
                    <td><input type="number" class="form-control" name="NetAmt" /></td>
                    <td>
                        <select class="form-control" style="width: 100PX;" name="Make" value="${make}"></select>
                    </td>
                    <td>
                        <select class="form-control" style="width: 100PX;" name="Department" value="${dept}"></select>
                    </td>
                    <td><input type="text" class="form-control" name="Remarks" value="${remarks}" /></td>
                    <td><input type="number" class="form-control" name="LDRate" /></td>
                    <td><input type="number" class="form-control" name="LDAmt" /></td>
                    <td><input type="number" class="form-control" name="VATPer" value="${vat}" /></td>
                    <td><input type="number" class="form-control" name="VATAmt" /></td>
                    <td><input type="number" class="form-control" name="PlusMinusQty" /></td>
                    <td><input type="number" class="form-control" name="USDRate" /></td>
                    <td><input type="number" class="form-control" name="ExRate" /></td>
                    <td>
                        <select class="form-control EmptyYN-dropdown" name="EmptyYN">
                            <option value="">--Select--</option>
                            <option>Yes</option>
                            <option>No</option>
                        </select>
                    </td>
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

            $('#tblItemRecordPO tbody').append(newRow);

            // Bind dropdowns after row is added
            let $lastRow = $('#tblItemRecordPO tbody tr').last();

            let $itemDropdown = $lastRow.find('select[name="ItemName"]');
            loadItemDropdown($itemDropdown, itemCode);

            let $taxDropdown = $lastRow.find('select[name="TaxType"]');
            loadTaxTypeDropdown($taxDropdown, taxtype);

            let $MakeDropdown = $lastRow.find('select[name="Make"]');
            loadMakeList($MakeDropdown, make);

            let $DeptDropdown = $lastRow.find('select[name="Department"]');
            loadDeptList($DeptDropdown, dept);



        } else {

            $('#tblItemRecordPO tbody tr[data-unique="' + uniqueKey + '"]').remove();
        }
    });
    // checked box checked multiple row insert onanimationstart Block
    $('#btn-save').on('click', function (e) {
        e.preventDefault();
        if (!validateSave())
            return;
        // 1. Header data

        var header = {
            // Document Header
            Vno: $('#NumDocNo').val() || null,
            Vtype: $('#ddlDocType').val() || null,
            DocType: $('#ddlDocType').val() || null,
            DocNo: $('#NumDocNo').val() || null,
            DocDate: $('#DtDocDate').val() || null,
            WbNo: $('#WBNo').val() || null,
            RefType: $('#ddlRefType').val() || null,
            RefNo: (function () {
                const val = $('#ddlRefNo option:selected').text();
                const match = val.match(/\d+$/);
                return match ? parseInt(match[0]) : null;
            })(),

            // Return To
            ReturnTo: $('#ddlReturnTo').val() || null,
            ReturnAddLine1: $('#txtAddLine1').val() || null,
            ReturnAddLine2: $('#txtAddLine2').val() || null,
            ReturnAddLine3: $('#txtAddLine3').val() || null,
            ReturnCity: $('#ddlCity').val() || null,
            ReturnGST: $('#txtGST').val() || null,

            // Ship To
            ShipTo: $('#ddlShipFrom').val() || null,
            ShipAddLine1: $('#txtShipAddLine1').val() || null,
            ShipAddLine2: $('#txtShipAddLine2').val() || null,
            ShipAddLine3: $('#txtShipAddLine3').val() || null,
            ShipCity: $('#ddlShipCity').val() || null,
            ShipGST: $('#txtShipGST').val() || null,

            // Accounting
            CreditAC: $('#ddlcreditAC').val() || null,
            DebitAC: $('#ddldebitAC').val() || null,

            // Document Details
            BillNo: $('#txtBillNo').val() || null,
            BillDate: $('#DtBillDate').val() || null,
            BLNo: $('#TxtBLNo').val() || null,
            BLDate: $('#DtBLDate').val() || null,
            WaybillNo: $('#TxtWaybillNo').val() || null,
            TransitNo: $('#txtTransitNo').val() || null,
            InputType: $('#ddlInputType').val() || null,
            ExpensesType: $('#ddlExpensesType').val() || null,
            NetAmount: $('#txtNetAmount').val() || null,
            Status: $('#ddlDocStatus').val() || null,

            // Optional
            Remarks: $('#txtRemarks').val() || null,
            GateNo: $('#ddlGateNo').val() || null,
            ChallanNo: $('#NumChallanNo').val() || null,
            ChallanDate: $('#DtChallanDate').val() || null,
            WaybillDate: $('#DtWaybillDate').val() || null,
            WaybillExpiry: $('#DtWaybillExpiry').val() || null,
            ExchangeRate: $('#txtExchangeRate').val() || null,
            BillFrom: $('#ddlBillFrom').val() || null,
            ReturnType: $('#ddlReturnType').val() || null,

            // Transport
            TransportName: $('#ddlTransportName').val() || null,
            TransportCode: $('#hdnTransport').val() || null,
            VehicleNo: $('#txtVehicleNo').val() || null,
            ContainerNo: $('#txtContainerNo').val() || null,
            GRNo: $('#txtGRNo').val() || null,
            GRDate: $('#DtGRDate').val() || null,
            TransportAC: $('#ddlTransportAC').val() || null,
            FreightDebit: $('#ddlFreightDebitAC').val() || null,
            FreightCredit: $('#ddlFreightCreditAC').val() || null,
            TDSonFreight: $('#NumTDSFreight1').val() || null,
            TDSonFreight: $('#NumTDSFreight2').val() || null,

            //Freight Details
            FreightPay: $('#NumFreightPay').val() || null,
            FrtTax1: $('#NumFrtTax1').val() || null,
            FrtTax2: $('#NumFrtTax2').val() || null,
            FrtPayNarr: $('#TxtFreightPayNarration').val() || null,

            // Amount Breakdown
            NumReceivedQty: $('#NumReceivedQty').val() || 0,
            NumBillQty: $('#NumBillQty').val() || 0,
            NumAmount: $('#NumAmount').val() || 0,
            NumPacking: $('#NumPacking').val() || 0,
            NumDiscount: $('#NumDiscount').val() || 0,
            NumCGST: $('#NumCGST').val() || 0,
            NumSGST: $('#NumSGST').val() || 0,
            NumIGST: $('#NumIGST').val() || 0,
            NumCESS: $('#NumCESS').val() || 0,
            NumVAT: $('#NumVAT').val() || 0,
            NumOtherAmt: $('#NumOtherAmt').val() || 0,
            NumTCSPer1: $('#NumTCSPer1').val() || 0,
            NumTCSPer2: $('#NumTCSPer2').val() || 0,
            NumRoundOff: $('#NumRoundOff').val() || 0,
            NumFinalNetAmt: $('#NumFinalNetAmt').val() || 0,

            ACTION: code > 0 ? "UPDATE" : "INSERT"
        };
        // 2. Items (from table rows)
        const allData = [];
        $('#tblItemRecordPO tbody tr').each(function () {
            const row = $(this);
            const getIntOrNull = val => {
                const parsed = parseInt(val);
                return isNaN(parsed) ? null : parsed;
            };

            const getDecimalOrZero = val => {
                const parsed = parseFloat(val);
                return isNaN(parsed) ? 0 : parsed;
            };

            const getIntOrZero = val => {
                const parsed = parseInt(val);
                return isNaN(parsed) ? 0 : parsed;
            };
            const rowData = {
                Code: getIntOrNull(row.find('input[name="Code"]').val()),
                ItemCode: parseInt(row.find('select[name="ItemName"]').val()) || null,
                HSNCode: row.find('input[name="HSNCode"]').val() || null,
                Unit: row.find('input[name="Unit"]').val() || '',
                Nos: getIntOrNull(row.find('input[name="Nos"]').val()),
                ReturnQty: getDecimalOrZero(row.find('input[name="ReturnQty"]').val()),
                BillQty: getDecimalOrZero(row.find('input[name="BillQty"]').val()),
                Rate: getDecimalOrZero(row.find('input[name="Rate"]').val()),
                Amount: getDecimalOrZero(row.find('input[name="Amount"]').val()),
                RCMYN: row.find('select[name="RCMYN"]').val() || null,
                InputYN: row.find('select[name="InputYN"]').val() || null,
                TaxType: row.find('select[name="TaxType"]').val() || null,
                PackPer: getDecimalOrZero(row.find('input[name="PackPer"]').val()),
                PackAmt: getDecimalOrZero(row.find('input[name="PackAmt"]').val()),
                DiscPer: getDecimalOrZero(row.find('input[name="DiscPer"]').val()),
                DiscAmt: getDecimalOrZero(row.find('input[name="DiscAmt"]').val()),
                WBQty: getDecimalOrZero(row.find('input[name="WBQty"]').val()),
                CGSTPer: getDecimalOrZero(row.find('input[name="CGSTPer"]').val()),
                CGSTAmt: getDecimalOrZero(row.find('input[name="CGSTAmt"]').val()),
                SGSTPer: getDecimalOrZero(row.find('input[name="SGSTPer"]').val()),
                SGSTAmt: getDecimalOrZero(row.find('input[name="SGSTAmt"]').val()),
                IGSTPer: getDecimalOrZero(row.find('input[name="IGSTPer"]').val()),
                IGSTAmt: getDecimalOrZero(row.find('input[name="IGSTAmt"]').val()),
                CESSPer: getDecimalOrZero(row.find('input[name="CessPer"]').val()),
                CESSAmt: getDecimalOrZero(row.find('input[name="CessAmt"]').val()),
                OthAmt: getDecimalOrZero(row.find('input[name="OthAmt"]').val()),
                NetAmt: getDecimalOrZero(row.find('input[name="NetAmt"]').val()),
                Make: parseInt(row.find('select[name="Make"]').val()) || null,
                Department: parseInt(row.find('select[name="Department"]').val()) || null,
                Remarks: row.find('input[name="Remarks"]').val() || null,
                LDRate: getDecimalOrZero(row.find('input[name="LDRate"]').val()),
                LDAmt: getDecimalOrZero(row.find('input[name="LDAmt"]').val()),
                WBType: row.find('input[name="WBType"]').val() || null,
                WBNo: row.find('input[name="WBNo"]').val() || null,
                RefType: row.find('input[name="RefType"]').val() || null,
                RefNo: row.find('input[name="RefNo"]').val() || null,
                RefBatchNo: row.find('input[name="RefBatchNo"]').val() || null,
                RefBagNo: row.find('input[name="RefBagNo"]').val() || null
            };
            allData.push(rowData);
        });
        // 3. FormData
        const formData = new FormData();
        formData.append("Header", JSON.stringify(header));
        // formData.append("ItemDetails", JSON.stringify(allData));
        allData.forEach((item, i) => {
            for (const key in item) {
                formData.append(`ItemDetails[${i}].${key}`, item[key]);
            }
        });

        // 4. Attachments
        rowsAttachment.forEach((attachment, index) => {
            formData.append(`Attachments[${index}].FileName`, attachment.FileName);
            formData.append(`Attachments[${index}].File`, attachment.File);

        });
        console.log('kks')

        // 5. Add Attachments
        $.ajax({
            url: '/ImportExportExpensesEntry/SaveAllData',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            success: function (resp) {
                if (resp.success) {

                    if (resp.action === "INSERT") {
                        toastr.success("Data saved successfully.");
                    }
                    else if (resp.action === "UPDATE") {
                        toastr.success("Data updated successfully.");
                    }
                    else {
                        toastr.success(resp.message);
                    }

                    setTimeout(function () {
                        window.location.href = "/ImportExportExpensesEntryList/Index";
                    }, 500);
                }
                else {
                    toastr.error(resp.message);
                }
            },
            error: function (xhr, status, error) {

                console.log("XHR:", xhr);
                console.log("Status:", status);
                console.log("Error:", error);
                console.log("Response:", xhr.responseText);

                toastr.error(xhr.responseText);
            }
        });
    });

    function validateSave() {

        // Header Validation
        if (!validateRequiredField('#ddlDocType', 'Voucher Type'))
            return false;

        if ($('#NumDocNo').val() == "" || parseInt($('#NumDocNo').val()) == 0) {
            toastr.error("Invalid Voucher No.");
            $('#NumDocNo').focus();
            return false;
        }

        if (!validateRequiredField('#DtDocDate', 'Document Date'))
            return false;

        if (!validateRequiredField('#ddlRefType', 'Reference Type'))
            return false;

        if (!validateRequiredField('#ddlInputType', 'Input Type'))
            return false;

        if (!validateRequiredField('#ddlRefNo', 'Reference No'))
            return false;

        //if ($('#txtRemarks').val().trim().length <= 5) {
        //    toastr.error("Reason for Return must be mentioned in Remarks.");
        //    $('#txtRemarks').focus();
        //    return false;
        //}

        if (!validateRequiredField('#ddlReturnTo', 'Party'))
            return false;

        if (!validateRequiredField('#ddlShipFrom', 'Ship To'))
            return false;

        if (!validateRequiredField('#ddlcreditAC', 'Debit Account'))
            return false;

        if (!validateRequiredField('#ddldebitAC', 'Credit Account'))
            return false;

        // Freight Validation

        let freight = parseFloat($('#NumFreightPay').val()) || 0;

        if (freight > 0 && !$('#ddlFreightDebitAC').val()) {
            toastr.error("Freight Debit A/C not selected.");
            $('#ddlFreightDebitAC').focus();
            return false;
        }

        if (freight > 0 && !$('#ddlFreightCreditAC').val()) {
            toastr.error("Freight Credit A/C not selected.");
            $('#ddlFreightCreditAC').focus();
            return false;
        }

        if (freight > 0 &&
            $('#ddlFreightDebitAC').val() == $('#ddlFreightCreditAC').val()) {

            toastr.error("Freight Debit A/C and Credit A/C must be different.");
            $('#ddlFreightCreditAC').focus();
            return false;
        }

        let freightTax =
            (parseFloat($('#NumFrtTax1').val()) || 0) +
            (parseFloat($('#NumFrtTax2').val()) || 0);

        if (freight == 0 && freightTax > 0) {
            toastr.error("Freight Tax not applicable when Freight Amount is 0.");
            return false;
        }

        let tds =
            (parseFloat($('#NumTDSFreight1').val()) || 0) +
            (parseFloat($('#NumTDSFreight2').val()) || 0);

        if (tds > 0 && freight == 0) {
            toastr.error("Freight TDS not applicable when Freight Amount is 0.");
            return false;
        }

        if (freight > 0 && !$('#ddlTransportName').val()) {
            toastr.error("Transport Name is required.");
            $('#ddlTransportName').focus();
            return false;
        }
        // Item Grid Validation
        let itemCount = 0;
        let isValid = true;

        $('#tblItemRecordPO tbody tr').each(function () {

            let row = $(this);
            let item = row.find('select[name="ItemName"]').val();

            if (!item) {
                toastr.error("Item not selected.");
                row.find('select[name="ItemName"]').focus();
                isValid = false;
                return false;
            }
            itemCount++;
            let returnQty = row.find('input[name="ReturnQty"]');

            if (returnQty == 0) {
                toastr.error("Received Qty is 0.");
                row.find('input[name="ReturnQty"]').focus();
                isValid = false;
                return false;
            }
            let amount = parseFloat(row.find('input[name="Amount"]').val()) || 0;
            if (amount == 0) {
                toastr.error("Amount must not be 0.");
                row.find('input[name="Amount"]').focus();
                isValid = false;
                return false;
            }
            let taxType = row.find('select[name="TaxType"]').val();
            if (!taxType) {
                toastr.error("Tax Type not selected.");
                row.find('select[name="TaxType"]').focus();
                isValid = false;
                return false;
            }
            let make = row.find('select[name="Make"]').val();
            if (!make) {
                toastr.error("Make is required.");
                row.find('select[name="Make"]').focus();
                isValid = false;
                return false;
            }
            let refType = row.find('input[name="RefType"]').val();
            let refNo = row.find('input[name="RefNo"]').val();
            if ((!refType || refType == "") &&
                (!refNo || refNo == "")) {

                toastr.error("Reference Type and Reference No required.");
                isValid = false;
                return false;
            }

        });

        if (!isValid)
            return false;

        if (itemCount == 0) {
            toastr.error("No Record in grid to save.");
            return false;
        }

        return true;
    }

    $("#browseBtn").on("click", function () {
        $("#fileInput").click();
    });
    //----------------------------------------------Image----------------------------------------
    $('#fileInput').on('change', function (e) {

        const files = e.target.files;

        Array.from(files).forEach(file => {

            const reader = new FileReader();

            reader.onload = function (ev) {

                const mime = file.type || 'application/octet-stream';

                // Store selected file
                rowsAttachment.push({
                    FileName: file.name,
                    File: file
                });

                // UI
                const card = `
            <div class="erp-file-row" data-filename="${file.name}">

                <div class="erp-file-preview">
                    ${mime.startsWith('image/')
                        ? `<img src="${ev.target.result}" class="erp-file-thumbnail">`
                        : mime === 'application/pdf'
                            ? `<i class="fa fa-file-pdf-o" style="font-size:40px;color:red;"></i>`
                            : `<i class="fa fa-file" style="font-size:40px;"></i>`
                    }
                </div>

                <div class="erp-file-info">
                    <div class="erp-file-name">${file.name}</div>
                    <div class="erp-file-type">${mime}</div>
                </div>

                <div class="erp-file-actions">

                    <button type="button"
                            class="erp-btn view btn-view-attachment"
                            data-src="${ev.target.result}"
                            data-type="${mime}">
                        <i class="fa fa-eye"></i>
                    </button>

                    <button type="button"
                            class="erp-btn delete btn-delete-attachment"
                            data-filename="${file.name}">
                        <i class="fa fa-trash"></i>
                    </button>

                </div>

            </div>
        `;
                $('#fileList').append(card);
            };

            reader.readAsDataURL(file);
        });

        $(this).val('');
    });

    $(document).on("click", ".btn-view-attachment", function () {

        const src = $(this).data("src");
        const type = $(this).data("type");

        $("#previewImage").hide();
        $("#previewPdf").hide();

        if (type.startsWith("image/")) {

            $("#previewImage").attr("src", src).show();
        }
        else if (type === "application/pdf") {

            $("#previewPdf").attr("src", src).show();
        }
        else {

            alert("Preview not available for this file type.");
            return;

        }

        $("#imagePreviewModal").modal("show");
    });

    //=======For Image Delete========
    $(document).on("click", ".btn-delete-attachment", function () {

        const fileName = $(this).data("filename");
        rowsAttachment = rowsAttachment.filter(x => x.FileName !== fileName);
        $(this).closest(".erp-file-row").remove();

    });
    //----------------------------------------------Image----------------------------------------
    function GetallDatapurchase1purchase2purchase3(data) {
        const purchase1 = data.purchase1;
        const purchase2 = data.purchase2;
        const purchase3 = data.purchase3;
        // ====== Purchase1 Header Bind ======
        if (purchase1 && purchase1.length > 0) {
            const p1 = purchase1[0];
            console.log('Header List', p1)

            ddlDocType(() => {
                $('#ddlDocType').val(p1.v_TYPE);
            });

            $('#NumDocNo').val(p1.v_NO);
            $('#DtDocDate').val(formatDateForInput(p1.v_DATE));

            $('#ddlRefNo').val(p1.reF_NO);
            if (p1.reF_TYPE && p1.reF_NO) {
                ddlRefNo(p1.reF_NO, p1.reF_TYPE, p1.reF_NO);
            } else {
                ddlRefNo('', p1.reF_TYPE);
            }
            ddlGateNo = p1.gatE_NO;
            ddlRefType(() => {
                $('#ddlRefType').val(p1.reF_TYPE);
            });
            ddlReturnTo(function () {
                $('#ddlReturnTo').val(String(p1.partY_CODE)).trigger('change.select2');
            });
            ddlShipDetails(function () {
                $('#ddlShipFrom').val(String(p1.shiP_CODE)).trigger('change.select2');
            });
            $('#txtAddLine1').val(p1.bilL_ADD1);
            $('#txtAddLine2').val(p1.bilL_ADD2);
            $('#txtAddLine3').val(p1.bilL_ADD3);

            $('#txtShipAddLine1').val(p1.shiP_ADD1);
            $('#txtShipAddLine2').val(p1.shiP_ADD2);
            $('#txtShipAddLine3').val(p1.shiP_ADD3);

            $('#ddlCity').val(p1.bilL_CITY).trigger('change');
            $('#ddlShipCity').val(p1.shiP_CITY).trigger('change');

            $('#ddlReturnTo').val(p1.partY_CODE).trigger('change');
            $('#ddlShipFrom').val(p1.shiP_CODE).trigger('change');


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
            ddlDebitAC(() => {
                $('#ddldebitAC').val(p1.frtpaY_DRAC);
            });
            ddlCreditAC(() => {
                $('#ddlcreditAC').val(p1.frtpaY_CRAC);
            });

            //Item Total
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

            $('#txtVehicleNo').val(p1.trucK_NO);
            $('#txtContainerNo').val(p1.containeR_NO);;
            $('#NumFreightPay').val(p1.frtpaY_AMT);
            $('#NumFrtTax1').val(p1.frtpaY_TAXPER);
            $('#NumFrtTax2').val(p1.frtpaY_TAX);
            $('#TxtFreightPayNarration').val(p1.frtpaY_NAR);
            $('#NumTDSFreight1').val(p1.frtpaY_TAXPER);
            $('#txtGRNo').val(p1.gR_NO);
            // $('#DtGRDate').val(p1.gR_DATE);
            if (p1.gR_DATE) {
                const grDate = new Date(p1.gR_DATE).toISOString().split('T')[0];
                $('#DtGRDate').val(grDate);
            }

            $('#NumGRNo').val(p1.gR_NO);

            $('#txtremarks').val(p1.remarks);

            $('#DtGRDate').val(formatDateForInput(p1.gR_DATE));
            $('#DtBLDate').val(formatDateForInput(p1.bilL_DATE));
            bindDropdownNew('ImportExportExpensesEntry', 'TransportName', '#ddlTransportName', '-- Select Transport --', p1.transporT_NAME);
            ddlTransportAC(() => {
                $('#ddlTransportAC').val(p1.transporT_AC);
            });
            ddlDocStatus(() => {
                $('#ddlDocStatus').val(p1.status);
            });

            ddlFreightCreditAC(() => {
                $('#ddlFreightCreditAC').val(p1.crediT_AC);
            });
            ddlFreightDebitAC(() => {
                $('#ddlFreightDebitAC').val(p1.debiT_AC);
            });
            $('#TxtWaybillNo').val(p1.waybilL_NO);
            $('#TxtBLNo').val(p1.bL_NO);
            if (p1.inpuT_TYPE) {
                $('#ddlInputType').val(p1.inpuT_TYPE);
            }
            if (p1.inpuT_TYPE) {
                $('#ddlExpensesType').val(p1.expS_TYPE);
            }
            // khushahal

        }
        // ====== Purchase2 Item Rows Bind ======
        bindItemsdata(purchase2);
        getdataImage(purchase3);
    }
    // LOAD BY V_NO ==========================
    function bindItemsdata(items) {
        console.log('khushahal', items)
        const $tbody = $('#tblItemRecordPO tbody').empty();

        items.forEach(i => {
            const qty = i.recD_QTY ?? 0;
            const rate = i.rate ?? 0;
            const amount = qty * rate;
            const inputYN = (i.inpuT_YN || '').trim().toUpperCase();
            const rcmYN = (i.rcM_YN || '').trim().toUpperCase();
            debugger;
            const $tr = $(`
                <tr>
                    <td>
                        <select class="form-control item-name-dropdown" name="ItemName" style="width:200px;"></select>
                    </td>
                    <td><input class="form-control" style="width: 100PX;" name="HSNCode" value="${i.hsN_CODE ?? 0}"  /></td>
                    <td><input class="form-control" style="width: 100PX;" name="Unit" value="${i.uoM_NAME || ''}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="Nos" value="${i.nos ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="RecQty" value="${i.recD_QTY ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="BillQty" value="${i.bilL_QTY ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="Rate" value="${i.rate ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="Amount" value="${i.amount}" /></td>
                    <td>
                        <select class="form-control" name="RCMYN">
                              <option value="">--Select--</option>
                              <option value="Yes" ${(rcmYN === 'Y' || rcmYN === 'YES') ? 'selected' : ''}>Yes</option>
                              <option value="No" ${(rcmYN === 'N' || rcmYN === 'NO') ? 'selected' : ''}>No</option>
                        </select>
                    </td>
                    <td>
                        <select class="form-control" name="InputYN">
                             <option value="">--Select--</option>
                             <option value="Yes" ${(inputYN === 'Y' || inputYN === 'YES') ? 'selected' : ''}>Yes</option>
                             <option value="No" ${(inputYN === 'N' || inputYN === 'NO') ? 'selected' : ''}>No</option>
                        </select>
                    </td>
                    <td><select class="form-control TaxType-dropdown" name="TaxType"></select></td>
                    <td><input class="form-control" style="width: 100PX;" name="PackPer" value="${i.pacK_PER ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="PackAmt" value="${i.pacK_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="DiscPer" value="${i.disC_PER ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="DiscAmt" value="${i.disC_AMT ?? 0}" /></td>
                    <td><input name="WBQty" class="form-control" style="width: 100PX;" value="${i.wB_QTY ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="CGSTPer" value="${i.cgsT_PER ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="CGSTAmt" value="${i.cgsT_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="SGSTPer" value="${i.sgsT_PER ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="SGSTAmt" value="${i.sgsT_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="IGSTPer" value="${i.igsT_PER ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="IGSTAmt" value="${i.igsT_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="CESSPer" value="${i.cesS_PER ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="CESSAmt" value="${i.cesS_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="OthAmt" value="${i.otH_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="NetAmt" value="${i.neT_AMT ?? amount}" /></td>
                     <td>
                        <select class="form-control make-dropdown" style="width: 100PX;" name="Make"}"></select>
                    </td>
                    <td>
                        <select class="form-control" style="width: 100PX;" name="Department"}"></select>
                    </td>
                    <td><input class="form-control" style="width: 100PX;" name="Remarks" value="${i.remarks || ''}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="LDRate" value="${i.lanD_RATE ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="LDAmt" value="${i.lanD_AMT ?? 0}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="WBType" value="${i.kantA_TYPE || ''}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="WBNo" value="${i.kantA_NO || ''}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="RefType" value="${i.reF_TYPE || ''}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="RefNo" value="${i.reF_NO || ''}" /></td>

                    <td><input class="form-control" style="width: 100PX;" name="RefBatchNo" value="${i.batcH_NO || ''}" /></td>
                    <td><input class="form-control" style="width: 100PX;" name="RefBagNo" value="${i.baG_NO || ''}" /></td>

                    <td class="action-col">
                    <div class="action-wrap">
                        <button type="button" class="act-btn add btn-add-row"  title="Add" style="cursor:pointer;"><i class="fa fa-plus-circle"></i></button>
                        <button type="button" class="act-btn edit btn-edit"  title="edit" style="cursor:pointer;"><i class="fa fa-edit"></i></button>
                        <button type="button" class="act-btn delete btn-delete"  title="delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                     </div>
                    </td>
                </tr>
            `);

            $tbody.append($tr);
            const $lastRow = $tbody.find('tr').last();
            const $itemDropdown = $lastRow.find('select[name="ItemName"]');
            const $taxDropdown = $lastRow.find('select[name="TaxType"]');
            const $makeDropdown = $lastRow.find('select[name="Make"]');
            const $deptDropdown = $lastRow.find('select[name="Department"]');
            // Load dropdowns
            loadItemDropdowngat($itemDropdown, i.iteM_CODE);
            loadTaxTypeDropdowngatCode($taxDropdown, i.taX_CODE);
            loadMakeList($makeDropdown, i.makE_CODE);
            loadDeptList($deptDropdown, i.depT_CODE);
            // Default ReadOnly
            $lastRow.find('input').prop('readonly', true);
            $lastRow.find('select').prop('disabled', true);
            // Action buttons enabled rahenge
            $lastRow.find('.btn-add-row, .btn-edit, .btn-delete').prop('disabled', false);
        });

        // delete row event
        $tbody.off('click', '.btn-delete').on('click', '.btn-delete', function () {
            $(this).closest('tr').remove();
        });
    }
    function loadTaxTypeDropdowngatCode($dropdown, selectedType = "") {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetTaxTypeList',
            method: 'GET',
            success: function (data) {
                $dropdown.empty();
                $dropdown.append('<option value="">--Select Tax Type--</option>');
                $.each(data, function (index, tax) {
                    // ✅ use selectedType instead of selectedCode
                    let selected = String(tax.code) === String(selectedType) ? 'selected' : '';
                    $dropdown.append(`<option value="${tax.code}" ${selected}>${tax.name}</option>`);
                });
            },
            error: function () {
                toastr.error('Failed to load tax types.');
            }
        });
    }
    //function getdataImage(Imagedata) {
    //    const $attachmentTbody = $('#tblAttachmentPRE tbody');
    //    $attachmentTbody.empty();

    //    if (!Array.isArray(Imagedata) || Imagedata.length === 0) {
    //        console.warn("⚠️ No attachment data to display");
    //        return;
    //    }

    //    Imagedata.forEach(item => {
    //        const fullPath = item.attachment; // "/attachments/Purchase/sdfsf.PNG"
    //        if (!fullPath) return;

    //        const fileName = fullPath.split('/').pop();

    //        // Check if it's an image
    //        let previewHtml = '';
    //        if (/\.(jpg|jpeg|png|gif|bmp)$/i.test(fileName)) {
    //            previewHtml = `
    //                <img src="${fullPath}" alt="${fileName}"
    //                     style="max-width:80px; max-height:80px; border:1px solid #ccc; border-radius:4px;" />`;
    //        } else {
    //            previewHtml = `<a href="${fullPath}" target="_blank" class="text-info">View File</a>`;
    //        }

    //        const row = `
    //            <tr>
    //                <td >${item.doC_ID || ''}</td>
    //                <td style="display:none;"><label>${fileName}</label></td>
    //                <td><input type="file" class="form-control file-upload" /></td>
    //                <td>${previewHtml}</td>
    //                <td>
    //                    <i class="fa fa-plus btn-add-action text-success me-2" title="Add Row" style="cursor:pointer;"></i>
    //                    <i class="fa fa-edit btn-edit-action text-primary me-2" title="Edit Row" style="cursor:pointer;"></i>
    //                    <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
    //                </td>
    //            </tr>
    //        `;
    //        $attachmentTbody.append(row);
    //    });
    //}
    function ddlDocType(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlDocType',
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
    function ddlRefType(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlRefType',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddl = $('#ddlRefType');
                ddl.empty().append('<option value="">-- Select Doc Type --</option>');
                $.each(data, function (index, item) {
                    ddl.append(`<option value="${item.value}">${item.text}</option>`);
                });
                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlRefType:", xhr.responseText);
            }
        });
    }
    function ddlDocStatus(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlDocStatus',
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
            url: '/ImportExportExpensesEntry/GetDocNo',
            type: 'POST',
            data: {
                docType: docType,
                docName: docName
            },
            success: function (response) {
                if (response.success) {
                    // Update value in a textbox
                    $('#NumDocNo').val(response.nextVNo);
                }
            },
            error: function (xhr) {
                console.error("Error:", xhr.responseText);
            }
        });
    }
    function ddlRefNo(selectedValue = '', vType = '', vNo = '') {

        const ddl = $('#ddlRefNo');

        // If parameters are not passed, use current control values
        vType = vType || $('#ddlRefType').val();
        vNo = vNo || $('#NumDocNo').val();

        if (!vType) {
            return;
        }

        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlRefNo',
            type: 'GET',
            dataType: 'json',
            data: { VNo: vNo, Vtype: vType },
            success: function (data) {

                if (ddl.hasClass("select2-hidden-accessible")) {
                    ddl.select2('destroy');
                }

                ddl.empty().append('<option value="">-- Select Reference No --</option>');

                $.each(data, function (i, item) {
                    ddl.append(new Option(item.text, item.value));
                });

                ddl.select2({
                    placeholder: "-- Select Reference No --",
                    allowClear: true,
                    width: '100%'
                });

                if (selectedValue) {
                    // ddl.val(selectedValue).trigger('change');
                    ddl.val(selectedValue).trigger('change.select2');
                }
            }
        });
    }
    function ddlReturnTo(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlReturnTo',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddl = $('#ddlReturnTo');
                ddl.empty().append('<option value="">-- Select Bill --</option>');
                $.each(data, function (index, item) {
                    ddl.append(`<option value="${item.value}">${item.text}</option>`);
                });
                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlReturnTo:", xhr.responseText);
            }
        });
    }
    function getBillFrom(code) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetBillDetails',
            type: 'POST',
            data: { code: code },
            success: function (response) {
                if (response) {
                    $('#txtAddLine1').val(response.address1);
                    $('#txtAddLine2').val(response.address2);
                    $('#txtAddLine3').val(response.address3);
                    GetddlCityBillDetails(function () {
                        $('#ddlCity').val(response.CITY_CODE);
                    })
                    GetddlstateBillDetails(function () {
                        $('#ddlShipFrom').val(code);
                    })
                    $('#txtPincode').val(response.pincode);
                    $('#txtGST').val(response.gstin);
                    // Ship Details banding dropdown List
                    ddlShipDetails(function () {
                        $('#ddlShipFrom').val(code);
                    })

                    $('#txtShipAddLine1').val(response.address1);
                    $('#txtShipAddLine2').val(response.address2);
                    $('#txtShipAddLine3').val(response.address3);
                    GetddlCityShipDetails(function () {
                        $('#ddlShipCity').val(response.CITY_CODE);
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
    function GetddlCityBillDetails(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlCityBillDetails',
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
            url: '/ImportExportExpensesEntry/GetddlstateBillDetails',
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
            url: '/ImportExportExpensesEntry/GetddlCityShipDetails',
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
            url: '/ImportExportExpensesEntry/GetddlstateShipDetails',
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
            url: '/ImportExportExpensesEntry/GetddlShipDetails',
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
    function ddlCreditAC(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlCreditAC',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddlCredit = $('#ddlcreditAC');
                ddlCredit.empty().append('<option value="">-- Select Bill --</option>');

                $.each(data, function (index, item) {
                    ddlCredit.append(`<option value="${item.value}">${item.text}</option>`);
                });

                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlCreditAC:", xhr.responseText);
            }
        });
    }
    function ddlDebitAC(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlDebitAC',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddlDebit = $('#ddldebitAC');
                ddlDebit.empty().append('<option value="">-- Select Bill --</option>');

                $.each(data, function (index, item) {
                    ddlDebit.append(`<option value="${item.value}">${item.text}</option>`);
                });

                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlDebitAC:", xhr.responseText);
            }
        });
    }

    function ddlFreightCreditAC(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlFreightCreditAC',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddlFreight = $('#ddlFreightCreditAC');
                ddlFreight.empty().append('<option value="">-- Select Bill --</option>');
                $.each(data, function (index, item) {
                    ddlFreight.append(`<option value="${item.value}">${item.text}</option>`);
                });
                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlCreditAC:", xhr.responseText);
            }
        });
    }
    function ddlFreightDebitAC(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlFreightDebitAC',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddlFreight = $('#ddlFreightDebitAC');
                ddlFreight.empty().append('<option value="">-- Select Bill --</option>');
                $.each(data, function (index, item) {
                    ddlFreight.append(`<option value="${item.value}">${item.text}</option>`);
                });
                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlDebitAC:", xhr.responseText);
            }
        });
    }
    function ddlTransportAC(callback) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetddlTransportAc',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddl = $('#ddlTransportAC');
                ddl.empty().append('<option value="">-- Transport Name --</option>');
                $.each(data, function (index, item) {
                    ddl.append(`<option value="${item.value}">${item.text}</option>`);
                });
                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlTransportAC:", xhr.responseText);
            }
        });
    }
    //Transport Name End ddl banding
    function addRow() {
        const tbody = $('#tblItemRecordPO tbody');
        const firstRow = tbody.find('tr:first');
        const newRow = firstRow.clone();
        newRow.find('input').val('');
        tbody.append(newRow);
        loadItemDropdown(newRow.find('.item-name-dropdown'));
        loadTaxTypeDropdown(newRow.find('.TaxType-dropdown'));

        loadMakeList(newRow.find('.Make-dropdown'));
        loadDeptList(newRow.find('.Department-dropdown'));


    }
    function loadItemDropdown(dropdown, selectedCode = "") {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetItemList',
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
            url: '/ImportExportExpensesEntry/GetHSNCode',
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
            url: '/ImportExportExpensesEntry/GetTaxTypeList',
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
    function getTaxType(code, row) {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetTaxTypeDetails',
            type: 'GET',
            data: { code: code },
            success: function (response) {
                const rate = parseFloat(row.find('input[name="Rate"]').val()) || 0;
                const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;
                const totalAmount = rate * billQty;

                if (response) {

                    const igstPer = parseFloat(response.igsT_PER) || 0;
                    const cgstPer = parseFloat(response.cgsT_PER) || 0;
                    const sgstPer = parseFloat(response.sgsT_PER) || 0;

                    // CGST
                    row.find('input[name="CGSTPer"]').val(cgstPer);
                    let CGSTAmt = 0;
                    if (cgstPer === 0) {
                        row.find('input[name="CGSTAmt"]').val(0);
                    } else {
                        CGSTAmt = (totalAmount * cgstPer) / 100;
                        row.find('input[name="CGSTAmt"]').val(CGSTAmt.toFixed(2));
                    }

                    // SGST
                    row.find('input[name="SGSTPer"]').val(sgstPer);
                    let SGSTAmt = 0;
                    if (sgstPer === 0) {
                        row.find('input[name="SGSTAmt"]').val(0);
                    } else {
                        SGSTAmt = (totalAmount * sgstPer) / 100;
                        row.find('input[name="SGSTAmt"]').val(SGSTAmt.toFixed(2));
                    }

                    // IGST
                    row.find('input[name="IGSTPer"]').val(igstPer);
                    let IGSTAmt = 0;
                    if (igstPer === 0) {
                        row.find('input[name="IGSTAmt"]').val(0);
                    } else {
                        IGSTAmt = (totalAmount * igstPer) / 100;
                        row.find('input[name="IGSTAmt"]').val(IGSTAmt.toFixed(2));
                    }
                    const NetAmt = totalAmount + CGSTAmt + SGSTAmt + IGSTAmt;
                    const LDRate = NetAmt / billQty;
                    row.find('input[name="NetAmt"]').val(NetAmt.toFixed(2));
                    row.find('input[name="LDAmt"]').val(NetAmt.toFixed(2));
                    row.find('input[name="LDRate"]').val(LDRate.toFixed(2));

                    calculateTotalRecQty();
                }
            },
            error: function () {
                toastr.error("Failed to get item details.");
            }
        });
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
        $('#tblItemRecordPO tbody tr').each(function () {
            const row = $(this);
            const recQty = parseFloat(row.find('input[name="RecQty"]').val()) || 0;
            const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;
            const totalAmountQty = parseFloat(row.find('input[name="Amount"]').val()) || 0;
            const PackingQty = parseFloat(row.find('input[name="PackPer"]').val()) || 0;
            const DiscountQty = parseFloat(row.find('input[name="DiscPer"]').val()) || 0;
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

        // Display in your input fields
        $('#NumReceivedQty').val(totalRecQty);
        $('#NumBillQty').val(totalBillQty);
        $('#NumAmount').val(totalAmount.toFixed(2));
        $('#NumPacking').val(totalPacking.toFixed(2));
        $('#NumDiscount').val(totalDiscount.toFixed(2));
        $('#NumCGST').val(totalCgst.toFixed(2));
        $('#NumSGST').val(totalSgst.toFixed(2));
        $('#NumIGST').val(totalIGST.toFixed(2));
        $('#NumCESS').val(totalCess.toFixed(2));
        $('#NumVAT').val(totalVAT.toFixed(2));
        $('#NumOtherAmt').val(totalOtherAmt.toFixed(2));
        $('#NumSubTotal').val(SubTotal.toFixed(2));


        // Optional: Calculate Round Off
        let rounded = Math.round(SubTotal);
        let roundOff = (rounded - SubTotal).toFixed(2);

        $('#NumRoundOff').val(roundOff);
        $('#NumFinalNetAmt').val(rounded.toFixed(2));
        $('#txtNetAmount').val(rounded.toFixed(2));
    }
    function recalculateRow(row) {
        const rateInput = row.find('input[name="Rate"]');
        const rate = parseFloat(rateInput.val()) || 0;
        const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;

        const totalAmount = rate * billQty;

        // Don't overwrite user typing
        if (!rateInput.is(':focus')) {
            rateInput.val(rate.toFixed(2));
        }

        row.find('input[name="Amount"]').val(totalAmount.toFixed(2));

        const cgstPer = parseFloat(row.find('input[name="CGSTPer"]').val()) || 0;
        const sgstPer = parseFloat(row.find('input[name="SGSTPer"]').val()) || 0;
        const igstPer = parseFloat(row.find('input[name="IGSTPer"]').val()) || 0;

        const CGSTAmt = (totalAmount * cgstPer) / 100;
        const SGSTAmt = (totalAmount * sgstPer) / 100;
        const IGSTAmt = (totalAmount * igstPer) / 100;

        row.find('input[name="CGSTAmt"]').val(CGSTAmt.toFixed(2));
        row.find('input[name="SGSTAmt"]').val(SGSTAmt.toFixed(2));
        row.find('input[name="IGSTAmt"]').val(IGSTAmt.toFixed(2));

        const NetAmt = totalAmount + CGSTAmt + SGSTAmt + IGSTAmt;
        const LDRate = billQty > 0 ? NetAmt / billQty : 0;

        row.find('input[name="NetAmt"]').val(NetAmt.toFixed(2));
        row.find('input[name="LDAmt"]').val(NetAmt.toFixed(2));
        row.find('input[name="LDRate"]').val(LDRate.toFixed(2));
    }
    //-------------------------------------GetRefNoList----------------------------------
    function GetalldatafetchGatonchange(response) {

        if (!response || !response.header || response.header.length === 0)
            return;
        const header = response.header[0];
        // Party
        $('#TxtWaybillNo').val(header.WAYBILL_NO || '');

        ddlReturnTo(function () {
            $('#ddlReturnTo').val(String(header.PARTY_CODE)).trigger('change.select2');
        });
        ddlShipDetails(function () {
            $('#ddlShipFrom').val(String(header.SHIP_CODE)).trigger('change.select2');
        });
        // Address
        $('#txtAddLine1').val(header.ADD1 || '');
        $('#txtAddLine2').val(header.ADD2 || '');
        $('#txtAddLine3').val(header.ADD3 || '');

        $('#txtShipAddLine1').val(header.SHIP_ADD1 || '');
        $('#txtShipAddLine2').val(header.SHIP_ADD2 || '');
        $('#txtShipAddLine3').val(header.ADD3 || '');

        // City
        GetddlCityBillDetails(function () {
            $('#ddlCity').val(header.CITY_CODE);

        });

        GetddlCityShipDetails(function () {
            $('#ddlShipCity').val(header.SHIP_CITY);
        })
        // State (Only if available in API)
        GetddlstateBillDetails(function () {
            if (header.StateCode)
                $('#ddlState').val(header.StateCode);

            if (header.SHIP_STATE)
                $('#ddlShipState').val(header.SHIP_STATE);
        });

        // GST
        $('#txtGST').val(header.GSTIN || '');
        $('#txtShipGST').val(header.SHIP_GST || '');

        // Pincode
        $('#txtPincode').val(header.PARTY_PINCODE || '');
        $('#NumShipPincode').val(header.SHIP_PINCODE || '');

        // Bill Details
        $('#txtBillNo').val(header.BILL_NO || '');
        $('#DtBillDate').val(header.BILL_DATE ? header.BILL_DATE.split('T')[0] : '');

        $('#NumChallanNo').val(header.CHALL_NO || '');
        $('#DtChallanDate').val(header.CHALL_DATE ? header.CHALL_DATE.split('T')[0] : '');

        // E-Way Bill
        $('#TxtWaybillInvNo').val(header.WAYBILL_NO || '');
        $('#DtWaybillExpiry').val(header.EWB_EXPDATE ? header.EWB_EXPDATE.split('T')[0] : '');

        // Transport
        $('#ddlTransport').val(header.TRANSPORT_CODE || '');
        $('#txtVehicleNo').val(header.TRUCK_NO || '');
        $('#txtContainerNo').val(header.CONTAINER_NO || '');
        // GR Details
        const today = new Date().toISOString().split('T')[0];
        $('#DtGRDate').val(header.GR_DATE ? header.GR_DATE.split('T')[0] : ($('#DtDocDate').val() || today));
        // Freight
        $('#txtFreightAmount').val(header.FRTPAY_AMT || 0);
        $('#txtFreightTaxPer').val(header.FRTPAY_TAXPER || 0);
        $('#txtFreightTax').val(header.FRTPAY_TAX || 0);
        $('#txtFreightNarration').val(header.FRTPAY_NAR || '');

        // Remarks
        $('#txtRemarks').val(header.REMARKS || '');

        // Items
        if (response.items && response.items.length > 0) {
            bindItems(response.items);
        }
    }
    function bindItems(items) {

        const $tbody = $("#tblItemRecordPO tbody");
        $tbody.empty();
        console.log("kks", items);
        $.each(items, function (index, i) {

            let qty = parseFloat(i.RECD_QTY || 0);
            let rate = parseFloat(i.RATE || 0);
            let amount = qty * rate;

            let row = `
            <tr>
                <td style="display:none">
                    <input type="hidden" name="Code" value="${i.ITEM_CODE || ''}">
                </td>

                <td>
                    <select class="form-control item-name-dropdown" name="ItemName"></select>
                </td>

                <td>
                    <input type="text" class="form-control" name="HSNCode"
                           value="${i.HSN_CODE || ''}">
                </td>

                <td>
                    <input type="text" class="form-control" name="Unit"
                           value="${i.Unit || ''}">
                </td>

                <td>
                    <input type="number" class="form-control" name="Nos"
                           value="${i.NOS || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="ReturnQty"
                           value="${i.RECD_QTY || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="BillQty"
                           value="${i.BILL_QTY || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="Rate"
                           value="${i.RATE || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="Amount"
                           value="${amount.toFixed(2)}">
                </td>

                <td>
                     <select class="form-control" name="RCMYN">
                        <option value="">--Select--</option>
                        <option value="Y" ${i.rcM_YN === 'Y' ? 'selected' : ''}>Yes</option>
                        <option value="N" ${i.rcM_YN === 'N' ? 'selected' : ''}>No</option>
                    </select>
                </td>

                <td>
                  <select class="form-control" name="InputYN">
                    <option value="">--Select--</option>
                    <option value="Y" ${i.inpuT_YN === 'Y' ? 'selected' : ''}>Yes</option>
                    <option value="N" ${i.inpuT_YN === 'N' ? 'selected' : ''}>No</option>
                </select>
                </td>

                <td>
                    <select class="form-control TaxType-dropdown" name="TaxType"></select>
                </td>

                <td>
                    <input type="number" class="form-control" name="PackPer"
                           value="${i.PACK_PER || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="PackAmt"
                           value="${i.PACK_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="DiscPer"
                           value="${i.DISC_PER || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="DiscAmt"
                           value="${i.DISC_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="WBQty"
                           value="${i.WB_QTY || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="CGSTPer"
                           value="${i.CGST_PER || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="CGSTAmt"
                           value="${i.CGST_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="SGSTPer"
                           value="${i.SGST_PER || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="SGSTAmt"
                           value="${i.SGST_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="IGSTPer"
                           value="${i.IGST_PER || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="IGSTAmt"
                           value="${i.IGST_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="CessPer"
                           value="${i.CESS_PER || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="CessAmt"
                           value="${i.CESS_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="OthAmt"
                           value="${i.OTH_AMT || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="NetAmt"
                           value="${i.NET_AMT || amount}">
                </td>

                <td>
                    <select class="form-control make-dropdown" name="Make"></select>
                </td>

                <td>
                    <select class="form-control department-dropdown" name="Department"></select>
                </td>

                <td>
                    <input type="text" class="form-control" name="Remarks"
                           value="${i.REMARKS || ''}">
                </td>

                <td>
                    <input type="number" class="form-control" name="LDRate"
                           value="${i.POLAND_RATE || 0}">
                </td>

                <td>
                    <input type="number" class="form-control" name="LDAmt"
                           value="${i.LAND_AMT || 0}">
                </td>

                <td>
                    <input type="text" class="form-control" name="WBType"
                           value="${i.KANTA_TYPE || ''}">
                </td>

                <td>
                    <input type="text" class="form-control" name="WBNo"
                           value="${i.KANTA_NO || ''}">
                </td>

                <td>
                    <input type="text" class="form-control" name="RefType"
                           value="${i.PO_TYPE || ''}">
                </td>

                <td>
                    <input type="text" class="form-control" name="RefNo"
                           value="${i.PO_NO || ''}">
                </td>

                <td><input class="form-control" style="width: 100PX;" name="RefBatchNo" value="${i.BATCH_NO || ''}" /></td>
                <td><input class="form-control" style="width: 100PX;" name="RefBagNo" value="${i.BAG_NO || ''}" /></td>


                 <td class="action-col">
                    <div class="action-wrap">
                        <button type="button" class="act-btn add btn-add-row"  title="Add" style="cursor:pointer;"><i class="fa fa-plus-circle"></i></button>
                        <button type="button" class="act-btn edit btn-edit"  title="edit" style="cursor:pointer;"><i class="fa fa-edit"></i></button>
                        <button type="button" class="act-btn delete btn-delete"  title="delete" style="cursor:pointer;"><i class="fa fa-trash"></i></button>
                     </div>
                 </td>

            </tr>`;

            $tbody.append(row);
            let $lastRow = $tbody.find("tr:last");
            $lastRow.find('[name="RCMYN"]').val(i.rcM_YN);
            $lastRow.find('[name="InputYN"]').val(i.inpuT_YN);
            loadItemDropdowngat($lastRow.find(".item-name-dropdown"), i.ITEM_CODE);
            loadTaxTypeDropdowngat($lastRow.find(".TaxType-dropdown"), i.TAX_CODE);
            loadMakeList($lastRow.find(".make-dropdown"), i.MAKE_CODE);
            loadDeptList($lastRow.find(".department-dropdown"), i.DEPT_CODE);

        });

        $("#tblItemRecordPO tbody tr").each(function () {
            recalculateRow($(this));
        });
        calculateTotalRecQty();
        $tbody.off("click", ".btn-delete").on("click", ".btn-delete", function () {
            $(this).closest("tr").remove();
            calculateTotalRecQty();
        });
    }
    function loadTaxTypeDropdowngat($dropdown, selectedCode) {

        $.get('/ImportExportExpensesEntry/GetTaxTypeList', function (data) {

            $dropdown.empty();

            $dropdown.append('<option value="">--Select--</option>');

            $.each(data, function (i, item) {

                $dropdown.append(
                    `<option value="${item.code}" ${item.code == selectedCode ? "selected" : ""}>
                        ${item.name}
                     </option>`
                );

            });

        });

    }
    function loadItemDropdowngat($dropdown, selectedCode = "") {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetItemList',
            method: 'GET',
            success: function (data) {
                $dropdown.empty();
                $dropdown.append('<option value="">--Select Item--</option>');
                $.each(data, function (index, item) {
                    let selected = String(item.code) === String(selectedCode) ? 'selected' : '';
                    $dropdown.append(`<option value="${item.code}" ${selected}>${item.name}</option>`);
                });
            },
            error: function () {
                toastr.error('Failed to load items.');
            }
        });
    }
    function loadMakeList($dropdown, selectedCode = "") {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetMakeListByItem',
            method: 'GET',
            success: function (data) {
                $dropdown.empty().append('<option value="">--Select Make--</option>');
                $.each(data, function (index, item) {
                    const option = $('<option></option>').val(item.value).text(item.text);
                    if (String(item.value) === String(selectedCode)) {
                        option.prop('selected', true);
                    }
                    $dropdown.append(option);
                });
            },
            error: function () {
                toastr.error('Failed to load makes.');
            }
        });
    }
    function loadDeptList($dropdown, selectedCode = "") {
        $.ajax({
            url: '/ImportExportExpensesEntry/GetDepartmentList',
            method: 'GET',
            success: function (data) {
                $dropdown.empty().append('<option value="">--Select Department--</option>');
                $.each(data, function (index, item) {
                    const $option = $('<option></option>').val(item.value).text(item.text);
                    if (String(item.value) === String(selectedCode)) {
                        $option.prop('selected', true);
                    }
                    $dropdown.append($option);
                });
            },
            error: function () {
                toastr.error('Failed to load departments.');
            }
        });
    }
    //-------------------------------------GetRefNoList----------------------------------
    function formatDateForInput(dateString) {
        if (!dateString) return "";
        const dateObj = new Date(dateString);
        if (isNaN(dateObj)) return "";
        const year = dateObj.getFullYear();
        const month = String(dateObj.getMonth() + 1).padStart(2, '0');
        const day = String(dateObj.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    $(document).on('click', '#btn_Sendapproval', function () {
        var FromName = window.location.pathname.split('/')[1];
        $.ajax({
            url: '/Approval/CheckPendingUser',
            type: 'POST',
            data: {
                vNo: code,
                vType: vType
            },
            success: function (response) {
                console.log('Response:', response);
                // Pending with another user
                if (response.success === false) {
                    showToast(`Pending With Another User : ${response.fullName} (${response.userCode})`,
                        { type: "warning" });
                    return;
                }
                // Approval_Code = 5
                if (response.approvalCode8 === true) {
                    OpenApprovalModal({
                        DocType: vType,
                        DocNo: code,
                        TableName: 'PURCHASE1'
                    });
                    return;
                }
                // Approval_Code != 8
                OpenSendForApprovalModal({
                    DocType: vType,
                    DocNo: code,
                    UserCode: null,
                    UserName: null,
                    DocDate: null,
                    TableName: 'PURCHASE1',
                    FromName, FromName
                });

            },
            error: function (xhr, status, error) {
                console.log(error);
                alert('Error while checking approval status.');
            }
        });

    });
    $(document).on('click', '#btn_Approved', function () {
        OpenApprovalModal({
            DocType: vType,
            DocNo: code,
            TableName: 'PURCHASE1'
        });
    });

    //------------------------------------------ProductionBatch---------------------------------

    $("#btnShowProductionBatch").click(function () {

        $("#hdnItemCode").val($("#txtItemCode").val());
        $("#hdnFromDept").val($("#ddlFromDept").val());
        $("#hdnToDept").val($("#ddlToDept").val());
        var model = {
            Vno: parseInt($("#NumDocNo").val()) || 0,
            Vtype: $("#ddlDocType").val()
        };

        console.log(model);

        $.ajax({

            url: "/ImportExportExpensesEntry/GetProductionBatch",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(model),

            success: function (data) {

                var tbody = $("#tblshowproductionbatchModal tbody");
                tbody.empty();

                var totalApproxWeight = 0;
                var totalActualWeight = 0;

                $.each(data, function (i, row) {

                    totalApproxWeight += parseFloat(row.approxWeight || 0);
                    totalActualWeight += parseFloat(row.actualWeight || 0);

                    tbody.append(`
            <tr>
                <td><input type="checkbox"></td>
                <td>${row.refType}</td>
                <td>${row.refNo}</td>
                <td>${row.itemCode}</td>
                <td>${row.itemName}</td>
                <td>${row.barcodeNo}</td>
                <td>${row.batchNo}</td>
                <td>${row.approxWeight}</td>
                <td>${row.actualWeight}</td>
            </tr>
        `);
                });

                // Show totals
                var modal = new bootstrap.Modal(document.getElementById("showproductionbatchmodal"));
                modal.show();
            },

            error: function (xhr) {

                console.log(xhr.responseText);

                alert("Something went wrong.");

            }

        });

    });
    $(document).on("change", "#tblshowproductionbatchModal tbody input[type='checkbox']", function () {

        var totalApprox = 0;
        var totalActual = 0;

        $("#tblshowproductionbatchModal tbody tr").each(function () {

            var chk = $(this).find("input[type='checkbox']");

            if (chk.is(":checked")) {

                totalApprox += parseFloat($(this).find("td:eq(7)").text()) || 0;
                totalActual += parseFloat($(this).find("td:eq(8)").text()) || 0;

            }

        });

        $("#NumApproxWeight").val(totalApprox.toFixed(2));
        $("#NumActualWeight").val(totalActual.toFixed(2));

    });
    $("#btn_modalsubmit").click(function () {

        var rows = [];

        $("#tblshowproductionbatchModal tbody tr").each(function (index) {

            if ($(this).find("input[type='checkbox']").is(":checked")) {

                rows.push({
                    RefType: $(this).find("td:eq(1)").text(),
                    RefNo: parseInt($(this).find("td:eq(2)").text()),
                    ItemCode: parseInt($(this).find("td:eq(3)").text()),
                    BarcodeNo: $(this).find("td:eq(5)").text(),
                    BatchNo: $(this).find("td:eq(6)").text(),
                    ApproxWeight: parseFloat($(this).find("td:eq(7)").text()),
                    ActualWeight: parseFloat($(this).find("td:eq(8)").text()),
                    Sno: index + 1
                });
            }

        });

        var model = {
            VNo: $("#NumDocNo").val(),
            VType: $("#ddlDocType").val(),
            VDate: $("#DtDocDate").val(),
            ItemCode: $("#hdnItemCode").val(),
            FromDeptCode: $("#hdnFromDept").val(),
            ToDeptCode: $("#hdnToDept").val(),
            Rows: rows
        };
        console.log('Production Batch data', model)

        $.ajax({
            url: "/ImportExportExpensesEntry/SaveProductionBatch",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(model),
            success: function (res) {
                alert(res.message);
            }
        });

    });

    //------------------------------------------ProductionBatch---------------------------------
});
function PrintPurchaseReturnEntryReport() {
    var model = {
        VType: $('#ddlDocType').val(),
        VNo: code,
        Amount: $("#txtNetAmount").val()
    };
    $.ajax({
        url: '/ImportExportExpensesEntry/PrintPurchaseReturnEntryReport',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(model),

        success: function (response) {
            var requestData = response.report;
            $.ajax({
                url: "http://localhost:34087/Report/PendingQCReport",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(requestData),
                xhrFields: {
                    responseType: "blob"
                },
                success: function (pdf) {
                    var file = new Blob([pdf], {
                        type: "application/pdf"
                    });
                    var url = window.URL.createObjectURL(file);
                    window.open(url, "_blank");
                },
                error: function (xhr) {
                    var reader = new FileReader();
                    reader.onload = function () {
                        alert(reader.result);
                    };
                    reader.readAsText(xhr.response);
                }
            });
        },
        error: function (xhr) {
            alert("Error occurred.");
        }
    });
}
function makePageReadOnly() {
    // Textbox
    $("input[type='text']").prop("readonly", true);
    $("input[type='number']").prop("readonly", true);
    $("input[type='date']").prop("readonly", true);
    $("input[type='email']").prop("readonly", true);
    // Textarea
    $("textarea").prop("readonly", true);
    // Dropdown
    $("select").prop("disabled", true);
    // Checkbox & Radio
    $("input[type='checkbox']").prop("disabled", true);
    $("input[type='radio']").prop("disabled", true);
    // Select2
    $(".select2").prop("disabled", true).trigger("change");

    $(".select2-selection").css({
        "pointer-events": "none",
        "background": "#f5f5f5"
    });
    // Datepicker
    $(".datepicker").datepicker("disable");
    // Item Grid
    $("#tblItemRecordPO input").prop("readonly", true);
    $("#tblItemRecordPO select").prop("disabled", true);
    $("#tblItemRecordPO button").hide();
    // Tax Grid
    $("#tblTaxRecord input").prop("readonly", true);
    $("#tblTaxRecord select").prop("disabled", true);
    $("#tblTaxRecord button").hide();
    // Attachment Grid
    $("#tblAttachmentPRE input").prop("readonly", true);
    $("#tblAttachmentPRE select").prop("disabled", true);
    $("#tblAttachmentPRE button").hide();
    // Hide Buttons
    $("#btnSave").hide();
    $("#btnDelete").hide();
    $("#btnSubmit").hide();
    $("#btnAdd").hide();

    $(".btn-save").hide();
    $(".btn-delete").hide();
    $(".btn-edit").hide();
    $(".btn-add").hide();
    $(".btn-remove").hide();
    // Remove onclick events
    $("#btnSave").removeAttr("onclick");
    $("#btnDelete").removeAttr("onclick");
    $("#btnAdd").removeAttr("onclick");
    $("#btnSubmit").removeAttr("onclick");

    // Grid buttons
    $("#tblItemRecordPO .btn-delete").removeAttr("onclick");
    $("#tblItemRecordPO .btn-edit").removeAttr("onclick");
    $("#tblItemRecordPO .btn-add").removeAttr("onclick");

}

//------------------------------------------------Images Start Block-----------------------------------
function getdataImage(Imagedata) {

    const $list = $('#fileList');
    $list.empty();

    rowsAttachment = [];

    if (!Array.isArray(Imagedata) || Imagedata.length === 0)
        return;

    Imagedata.forEach(item => {

        const fileName = item.filE_NAME || "File";
        const base64 = item.imG_FILE;
        const mimeType = getMimeType(fileName);

        if (!base64) return;

        // Save for Update
        rowsAttachment.push({
            FileName: fileName,
            FileContentBase64: base64,
            FileType: mimeType
        });

        const dataUrl = `data:${mimeType};base64,${base64}`;

        const html = `
            <div class="erp-file-row" data-filename="${fileName}">

                <div class="erp-file-preview">
                    ${mimeType.startsWith("image/")
                ? `<img src="${dataUrl}" class="erp-file-thumbnail">`
                : mimeType === "application/pdf"
                    ? `<i class="fa fa-file-pdf-o" style="font-size:40px;color:red;"></i>`
                    : `<i class="fa fa-file"></i>`
            }
                </div>

                <div class="erp-file-info">
                    <div class="erp-file-name">${fileName}</div>
                    <div class="erp-file-type">${mimeType}</div>
                </div>

                <div class="erp-file-actions">

                    <button type="button"
                            class="erp-btn view btn-view-attachment"
                            data-src="${dataUrl}"
                            data-type="${mimeType}">
                        <i class="fa fa-eye"></i>
                    </button>

                    <button type="button"
                            class="erp-btn delete btn-delete-attachment"
                            data-filename="${fileName}">
                        <i class="fa fa-trash"></i>
                    </button>

                </div>

            </div>
        `;

        $list.append(html);

    });
}
function getMimeType(fileName) {

    const ext = fileName?.split('.').pop()?.toLowerCase();

    switch (ext) {

        case 'jpg':
        case 'jpeg':
            return 'image/jpeg';

        case 'png':
            return 'image/png';

        case 'gif':
            return 'image/gif';

        case 'bmp':
            return 'image/bmp';

        case 'webp':
            return 'image/webp';

        case 'pdf':
            return 'application/pdf';

        default:
            return 'application/octet-stream';
    }
}

//------------------------------------------------Images End Block-----------------------------------