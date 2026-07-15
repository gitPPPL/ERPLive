function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}

const code = getQueryParam('vNo');
const vType = getQueryParam('vType');
let isReadOnly = getQueryParam('readOnly') === 'true';
let rowsAttachment = [];

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
    /* ddlTransportName();*/
    bindDropdownNew('PurchaseReceiptEntry', 'TransportName', '#ddlTransportName', '-- Select Tran --');
    $('#btnCreateIntimation').hide();
    ddlOrdertype();
    GetDocTypeCopyFrom();
    const today = new Date().toISOString().split('T')[0];
    
    document.getElementById('DtDocDate').value = today;
    const billDate = document.getElementById('DtBillDate');
    if (billDate) billDate.min = today;
    const challanDate = document.getElementById('DtChallanDate');
    if (challanDate) challanDate.min = today;

    ddlDocStatus(() => {
        $('#ddlDocStatus').val(1);
    });

    $('#ddlDocType').on('blur', function () {
        $(this).prop('disabled', true);
    });
    
    if (isReadOnly) {
        setFormReadOnly();
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
    
    //=========Fill Data on Change Gate No==============
    //$('#ddlGateNo').on('change', function () {
    //    var selectedNo = $(this).val(); 
    //    const selectedText = $(this).find('option:selected').text();
    //    $('#NumDocNo').prop('readonly', true);

    //    $('#txtNetAmount').prop('readonly', true);
    //    $('#ddlBillFrom').prop('disabled', true);
    //    $('#txtAddLine1').prop('readonly', true);
    //    $('#txtAddLine2').prop('readonly', true);
    //    $('#txtAddLine3').prop('readonly', true);
    //    $('#ddlCity').prop('disabled', true);
    //    $('#txtPincode').prop('readonly', true);
    //    $('#ddlState').prop('disabled', true);
    //    $('#txtGST').prop('readonly', true);

    //    $('#txtShipAddLine1').prop('readonly', true);
    //    $('#txtShipAddLine2').prop('readonly', true);
    //    $('#txtShipAddLine3').prop('readonly', true);
    //    $('#ddlShipCity').prop('disabled', true);
    //    $('#ddlShipState').prop('disabled', true);
    //    $('#NumShipPincode').prop('readonly', true);
    //    $('#txtShipGST').prop('readonly', true);
        
    //    if (selectedNo !== "0" && selectedNo !== "") {
    //        $.ajax({
    //            url: '/PurchaseReceiptEntry/GetGatDetailsList',
    //            type: 'POST',
    //            data: { StrVNo: selectedNo, StrV_type: selectedText },
    //            success: function (response) {
    //                console.log("Full Data", response);
    //                GetalldatafetchGatonchange(response);
    //            },
    //            error: function (xhr) {
    //                console.error("Error:", xhr.responseText);
    //            }
    //        });
    //    }
    //});

    $('#ddlGateNo').on('change', async function () {

        console.log("Selected Text :", $(this).find('option:selected').text());
        console.log("Selected Value:", $(this).val());

        var selectedNo = $(this).val();
        var selectedType = $(this).find('option:selected').text();

        if (selectedNo === "0" || selectedNo === "")
            return;

        var currentVNo = $('#NumDocNo').val();
        var docType = $('#ddlDocType').val();

        var gateType = "";

        switch (docType) {
            case "RCPT":
            case "RCPI":
                gateType = "INRM";
                break;

            case "SRPU":
            case "SRJW":
                gateType = "INST";
                break;

            case "BFRC":
                gateType = "INFU";
                break;
        }

        $.ajax({
            url: '/PurchaseReceiptEntry/ValidateGate',
            type: 'POST',
            data: {
                gateType: gateType, 
                gateNo: selectedNo,
                docType: docType,
                currentVNo: currentVNo
            },
            success: async function (result) {

                if (!result.status) {
                    showToast(result.message, { type: "warning" });

                    $('#ddlGateNo').val('').trigger('change.select2');
                    return;
                }
                
                //==============================
                // Confirmation (VB MessageBox Yes/No)
                //==============================
                //if (!confirm("All data will be refreshed. Do you want to import data from Gate?")) {

                //    $('#ddlGateNo').val('').trigger('change.select2');
                //    return;
                //}

                const swalResult = await Swal.fire({
                    title: 'Import Gate Data?',
                    text: 'All data will be refreshed. Do you want to import data from Gate?',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Yes',
                    cancelButtonText: 'No',
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                    width: '420px'
                });

                if (!swalResult.isConfirmed) {
                    $('#ddlGateNo').val('').trigger('change.select2');
                    return;
                }

                //==============================
                // Disable Controls
                //==============================
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

                //==============================
                // Load Gate Data
                //==============================
                $.ajax({
                    url: '/PurchaseReceiptEntry/GetGatDetailsList',
                    type: 'POST',
                    data: {
                        StrVNo: selectedNo,
                        StrV_type: gateType
                    },
                    success: function (response) {

                        console.log(response);
                        GetalldatafetchGatonchange(response);
                    },
                    error: function (xhr) {
                        console.error(xhr.responseText);
                        showToast("Error loading Gate Details.", { type: "error" });
                    }
                });

            },
            error: function (xhr) {
                console.error(xhr.responseText);
                showToast("Validation failed.", { type: "error" });
            }
        });

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

        $('#tblPurchaseReceiptIR tbody tr').each(function () {
            recalculateRow($(this));
        });

        calculateTotalRecQty();
    });

    //=========USD Rate and Ex rate Calculation=========
    $(document).on('change', 'input[name="USDRate"], input[name="ExRate"]', function () {
        const row = $(this).closest('tr');
        console.log("USD =", row.find('input[name="USDRate"]').val());
        console.log("EX =", row.find('input[name="ExRate"]').val());
        recalculateRow(row);
        calculateTotalRecQty();
    });

    //==========Add new row=================
    $('#tblPurchaseReceiptIR tbody tr').each(function () {
        loadItemDropdown($(this).find('.item-name-dropdown'));
        loadTaxTypeDropdown($(this).find('.TaxType-dropdown'));
        loadDepartmentDropdown($(this).find('.Department-dropdown'));
        loadBinDropdown($(this).find('.bin-dropdown'));
        loadUnitDropdown($(this).find('.Unit-dropdown'));
        loadMakeDropdown($(this).find('.Make-dropdown'));
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

    //============For Attachment============
    $("#browseBtn").on("click", function () {
        $("#fileInput").click();
    });

    $('#fileInput').on('change', function (e) {
        console.log("Before Adding New", JSON.stringify(rowsAttachment));
        console.log(rowsAttachment);
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

    //=========For Preview Image=========
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
                        showToast("No order details found for the selected ID", { type: "warning" });
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
                    showToast("Failed To Load Order Details: " + error, { type: "error" });
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

    //================================================================
    //                  Show Production Batch
    //================================================================

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

            url: "/PurchaseReceiptEntry/GetProductionBatch",
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
              
                var modal = new bootstrap.Modal(document.getElementById("showproductionbatchmodal"));
                modal.show();
            },

            error: function (xhr) {

                console.log(xhr.responseText);

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

    //================================================================
    //                  Copy From Tables
    //================================================================

    $(document).on('click', '.copy-from-item', function (e) {

        e.preventDefault();

        const vType = $(this).data('vtype');
        const docName = $(this).text().trim();
        const receiptType = $('#ddlDocType').val();
        const partyCode = $('#ddlBillFrom').val();

        if (!partyCode) {
            showToast("Please select Bill From.", { type: "warning" });
            $('#ddlBillFrom').focus();
            return;
        }
        console.log(vType);
        console.log(docName);   

        $('#purchaseRequestLabel').text(`Copy From - ${docName}`);
        loadCopyFrom(vType, receiptType, partyCode);
         
    });
   
    //================================================================
    //                     Create Intimation
    //================================================================
    $("#btnCreateIntimation").click(function (e) {

        e.preventDefault();

        const formData = buildPurchaseReceiptFormData();

        $.ajax({
            url: '/PurchaseReceiptEntry/CreateIntimation',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            success: function (res) {

                if (res.success) {
                    toastr.success(res.message);
                }
                else {
                    toastr.warning(res.message);
                }
            },
            error: function () {
                toastr.error("Error creating intimation.");
            }
        });

    });

    //================================================================
    //             Bind Table Data on Change
    //================================================================

    $('#tblindendorderstore').on('change', 'input[type="checkbox"]', function () {
        let $row = $(this).closest('tr');

        let itemCode = $row.find('td:eq(4)').text().trim();
        let reqNo = $row.find('td:eq(25)').text().trim();
        let vtype = $row.find('td:eq(2)').text().trim();
        let poNo = $row.find('td:eq(1)').text().trim();

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
                       <td style="display:none;">
                         <input type="hidden" name="PackOnBasic" value="${i.PACK_ONBASIC ?? 0}" />
                     </td>
                        <td style="display:none;"><input type="text" class="form-control" name="Code" value="${itemCode}" /></td>
                        <td class="freeze-item"><select class="form-control item-name-dropdown" name="ItemName"></select></td>
                        <td><input type="text" class="form-control" name="HSNCode" /></td>
                        <td>
                            <select class="form-control Unit-dropdown"
                                    name="Unit"
                                    data-selected-value="${unit}">
                            </select>
                        </td
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

                        <td>
                            <select class="form-control Make-dropdown"
                                    name="Make"
                                    data-selected-value="${make}">
                            </select>
                        </td>

                        <td>
                            <select class="form-control Department-dropdown"
                                    name="Department"
                                    data-selected-value="${dept}">
                            </select>
                        </td>

                        <td><input type="text" class="form-control" name="Remarks" value="${remarks}" /></td>
                        <td><input type="number" class="form-control" name="LDRate" /></td>
                        <td><input type="number" class="form-control" name="LDAmt" /></td>
                        <td>
                            <select class="form-control bin-dropdown"
                                    name="BinLocation"
                                    data-selected-value="${bin}">
                            </select>
                        </td>
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

            let $deptDropdown = $lastRow.find('select[name="Department"]');
            loadDepartmentDropdown($deptDropdown, dept);

            let $BinDropdown = $lastRow.find('select[name="BinLocation"]');
            loadBinDropdown($BinDropdown, binlocation);
                
            let $UnitDropdown = $lastRow.find('select[name="Unit"]');
            loadUnitDropdown($UnitDropdown, unit);

            let $MakeDropdown = $lastRow.find('select[name="Make"]');
            loadMakeDropdown($MakeDropdown, make);

        } else {
            $('#tblPurchaseReceiptIR tbody tr[data-unique="' + uniqueKey + '"]').remove();
        }
    });

    //================================================================
    //                Save And Update
    //================================================================

    $('#btn-save').on('click', async function (e) {
        e.preventDefault(); 

        const gateText = $('#ddlGateNo option:selected').text();

        if (!validateDataForPRMRN()) {
            return;
        }

        const isValidDate = await checkValidDate();
        if (!isValidDate) {
            return;
        }

        var header = {
            DocType: $('#ddlDocType').val() || null,
            DocNo: $('#NumDocNo').val() || null,
            BillNo: $('#txtBillNo').val() || null,
            ChallanNo: $('#NumChallanNo').val() || null,
            WaybillNo: $('#TxtWaybillNo').val() || null,
            EWB_INVNO: $('#TxtWaybillInvNo').val() || 0,
            ReturnType: $('#ddlReturnType').val() || 0,
            DocStatus: $('#ddlDocStatus').val() || 0,
            DocDate: $('#DtDocDate').val() || 0,
            GateNo: $('#ddlGateNo').val() || null,
            GATE_TYPE: gateText ? gateText.match(/^[A-Za-z]+/)?.[0] : null,
            BillDate: $('#DtBillDate').val() || null,
            ChallanDate: $('#DtChallanDate').val() || null,
            EWB_DATE: $('#DtWaybillDate').val() || null,
            EWB_EXPDATE: $('#DtWaybillExpiry').val() || null,
            ExchangeRate: $('#txtExchangeRate').val() || null,
            NetAmount: $('#txtNetAmount').val() || null,
            BillFrom: $('#ddlBillFrom').val() || null,
            AddLine1: $('#txtAddLine1').val() || null,
            AddLine2: $('#txtAddLine2').val() || null,
            AddLine3: $('#txtAddLine3').val() || null,
            City: $('#ddlCity').val() || null,
            Pincode: $('#txtPincode').val() || null,
            BILL_ADDRESSID: $('#ddladdressBD1').val() || 0,
            SHIP_ADDRESSID: $('#ddladdressSD1').val() || 0,
            GST: $('#txtGST').val() || null,
            Remarks: $('#txtRemarks').val() || null,
            ShipFrom: $('#ddlShipFrom').val() || null,
            ShipAddLine1: $('#txtShipAddLine1').val() || null,
            ShipAddLine2: $('#txtShipAddLine2').val() || null,
            ShipAddLine3: $('#txtShipAddLine3').val() || null,
            ShipCity: $('#ddlShipCity').val() || null,
            ShipPincode: $('#NumShipPincode').val(),
            ShipState: $('#ddlShipState').val(),
            ShipGST: $('#txtShipGST').val(),
            //TransportName: $('#ddlTransportName option:selected').text(),
            //TRANSPORT_CODE: $('#ddlTransportName').val() || 0,
            TransportName: $('#ddlTransportName').val().trim(),
            TRANSPORT_CODE: $('#hdnTransport').val() || 0,
            VehicleNo: $('#TxtVehicleNo').val(),
            ContainerNo: $('#TxtContainerNo').val() || 0, 
            FreightPay: $('#NumFreightPay').val() ,
            FrtTax1: $('#NumFrtTax1').val() || 0,
            FrtTax2: $('#NumFrtTax2').val() || 0,
            FrtPayNarr: $('#txtFrtPayNarr').val() || 0,
            GRNo: $('#NumGRNo').val() || 0,
            GRDate: $('#DtGRDate').val() || null,
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
            NumSubTotal: $('#NumSubTotal').val() || 0,
            NumRoundOff: $('#NumRoundOff').val() || 0,
            NumFinalNetAmt: $('#NumFinalNetAmt').val() || 0,
            HOLD_PAY: $('#ddlPaymentIT option:selected').text() || 0,
            HOLD_REASON: $('#txtReason').val(), 
            HOLD_DATE: $('#DtHoldDate').val(),
            ACTION: code > 0 ? "UPDATE" : "INSERT",
            code: code
        };

        //Items (from table rows)
        const allData = [];
        $('#tblPurchaseReceiptIR tbody tr').each(function () {

            const row = $(this);
            const itemSelect = row.find('select[name="ItemName"]');
            const rowData = {
                Code: row.find('input[name="Code"]').val() || null,
                ItemCode: itemSelect.val() || null,
                ItemName: itemSelect.find("option:selected").text(),
                HSNCode: row.find('input[name="HSNCode"]').val() || 0,
                UOMCode: row.find('select[name="Unit"]').val() || 0,
                UOMName: row.find('select[name="Unit"] option:selected').text() || '',
                Nos: parseFloat(row.find('input[name="Nos"]').val()) || 0,
                PlusMinusQty: parseFloat(row.find('input[name="PlusMinusQty"]').val()) || 0,
                RecQty: parseFloat(row.find('input[name="RecQty"]').val()) || 0,
                BillQty: parseFloat(row.find('input[name="BillQty"]').val()) || 0,
                USDRate: parseFloat(row.find('input[name="USDRate"]').val()) || 0,
                ExRate: parseFloat(row.find('input[name="ExRate"]').val()) || 0,
                Rate: parseFloat(row.find('input[name="Rate"]').val()) || 0,
                Amount: parseFloat(row.find('input[name="Amount"]').val()) || 0,
                EmptyYN: row.find('select[name="EmptyYN"]').val(),
                WBQty: parseFloat(row.find('input[name="WBQty"]').val()) || 0,
                TaxCode: row.find('select[name="TaxType"]').val() || 0,
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
                MakeCode: row.find('select[name="Make"]').val() || 0,
                DeptCode: row.find('select[name="Department"]').val() || 0,
                Remarks: row.find('input[name="Remarks"]').val(),
                LDRate: parseFloat(row.find('input[name="LDRate"]').val()) || 0,
                LDAmt: parseFloat(row.find('input[name="LDAmt"]').val()) || 0,
                BinLocation: row.find('select[name="BinLocation"]').val() || "",
                POType: row.find('input[name="POType"]').val(),
                PONo: row.find('input[name="PONo"]').val() || 0,
                KantaType: row.find('input[name="KantaType"]').val(),
                KantaNo: row.find('input[name="KantaNo"]').val() || 0,
                ReqType: row.find('input[name="ReqType"]').val(),
                ReqNo: row.find('input[name="ReqNo"]').val() || 0,
                GateType: row.find('input[name="GateType"]').val(),
                GateNo: row.find('input[name="GateNo"]').val() || 0
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

        rowsAttachment.forEach((attachment, index) => {
            
            formData.append(`Attachments[${index}].FileName`, attachment.FileName);

            if (attachment.File) {
                console.log("NEW FILE", attachment.File.name);
                formData.append(`Attachments[${index}].File`, attachment.File);
            }
            else {
                formData.append(`Attachments[${index}].IMG_FILE`, attachment.IMG_FILE);
                formData.append(`Attachments[${index}].FILE_NAME`, attachment.FILE_NAME);
                formData.append(`Attachments[${index}].FILE_TYPE`, attachment.FILE_TYPE);
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

                    isReadOnly = true;
                    setFormReadOnly();
                    $('#btnCreateIntimation').show();

                } else {
                    showToast("Data Saved Failed: " + resp.message, { type: "error" });
                }
            },
            error: function (xhr, status, error) {

                let message = "Data Saved Failed.";

                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                }
                else if (xhr.responseText) {
                    try {
                        const resp = JSON.parse(xhr.responseText);
                        message = resp.message || message;
                    } catch (e) {
                        message = xhr.responseText;
                    }
                }

                showToast(message, { type: "error" });
               
                console.error("AJAX Error:", status, error, xhr.responseText);
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
            showToast("Error loading ddlDocType: " + xhr.responseText, { type: "error" });
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
            showToast("Error loading ddlDocStatus: " + xhr.responseText, { type: "error" });
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
            showToast("Error: " + xhr.responseText, { type: "error" });
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
                placeholder: "-- Select GateNo --",
                allowClear: true,
                width: '100%'
            });
            $('.select2-selection').addClass('form-control');
        },
        error: function (xhr, status, error) {
            showToast("Error loading Gate Number: " + error, { type: "error" });
            console.error("Error loading Gate Number:", xhr.responseText);
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
            showToast("Error loading Bill From", { type: "error" });
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
                showToast("No supplier data found for Bill from Dropdown", { type: "warning" });
            }
        },
        error: function (xhr) {
            showToast("Error : No supplier data found for bill From Dropdown" + xhr.responseText, { type: "error" });
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
                showToast("Address Line1 not found for Bill From Dropdown", { type: "warning" });
                console.warn("No supplier data found for code:", code);
            }
        },
        error: function (xhr) {
            showToast("Error: " + xhr.responseText, { type: "error" });
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
                });
                GetddlstateShipDetails(function () {
                    $('#ddlShipState').val(response.stateCode);
                });
                $('#NumShipPincode').val(response.pincode);
                $('#txtShipGST').val(response.gstin);

            } else {
                showToast("Address Line1 not found for Ship From Dropdown", { type: "warning" });
            }
        },
        error: function (xhr) {
            showToast("Error: " + xhr.responseText, { type: "error" });
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

//function ddlTransportName(callback) {
//    $.ajax({
//        url: '/PurchaseReceiptEntry/GetddlTransportName',
//        type: 'GET',
//        dataType: 'json',
//        success: function (data) {
//            const ddl = $('#ddlTransportName');
//            ddl.empty().append('<option value="">-- Transport Name --</option>');
//            $.each(data, function (index, item) {
//                ddl.append(`<option value="${item.value}">${item.text}</option>`);
//            });
//            if (typeof callback === "function") callback();
//        },
//        error: function (xhr) {
//            console.error("Error loading ddlTransportName:", xhr.responseText);
//        }
//    });
//}

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

function GetStateByCity(cityCode, stateDropdown, callback) {

    if (!cityCode) return;

    $.ajax({
        url: '/PurchaseReceiptEntry/GetStateByCity',
        type: 'GET',
        data: { cityCode: cityCode },
        success: function (res) {

            if (res) {
                $(stateDropdown).val(res.stateCode);

                if (callback)
                    callback(res);
            }
        },
        error: function () {
            showToast("State Binding Failed", { type: "error" });
        }
    });
}

//================================================================
//            Add new Row in Table 
//================================================================
function addRow() {
    const tbody = $('#tblPurchaseReceiptIR tbody');
    const firstRow = tbody.find('tr:first');
    const newRow = firstRow.clone();
    newRow.find('input').val('');
    tbody.append(newRow);
    loadItemDropdown(newRow.find('.item-name-dropdown'));
    loadTaxTypeDropdown(newRow.find('.TaxType-dropdown'));
    loadDepartmentDropdown(newRow.find('.Department-dropdown'));
    loadBinDropdown(newRow.find('.bin-dropdown'));
    loadUnitDropdown(newRow.find('.Unit-dropdown'));
    loadMakeDropdown(newRow.find('.Make-dropdown'));
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
            showToast("Failed To load Items", { type: "error" });
        }
    });
}

function loadDepartmentDropdown($ddl, selectedValue = "") {
    $.get('/PurchaseReceiptEntry/GetDepartment', function (data) {
        $ddl.empty();
        $ddl.append('<option value="">Select</option>');

        $.each(data, function (i, item) {
            $ddl.append(
                `<option value="${item.text}">${item.value}</option>`
            );
        });

        if (selectedValue) {
            $ddl.val(selectedValue);
        }
    });
}

function loadBinDropdown($ddl, selectedValue = "") {
   
    $.get('/PurchaseReceiptEntry/GetBINMAST', function (data) {
        $ddl.empty();
        $ddl.append('<option value="">Select</option>');

        $.each(data, function (i, item) {
            $ddl.append(
                `<option value="${item.text}">${item.value}</option>`
            );
        });

        if (selectedValue) {
            $ddl.val(selectedValue);
        }
    });
}

function loadUnitDropdown($ddl, selectedValue = "") {
   
    $.get('/PurchaseReceiptEntry/GetUnitMast', function (data) {
        $ddl.empty();
        $ddl.append('<option value="">Select</option>');

        $.each(data, function (i, item) {
            $ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

        if (selectedValue) {
            $ddl.val(selectedValue);
        }
    });
}

function loadMakeDropdown($ddl, selectedValue = "") {
   
    $.get('/PurchaseReceiptEntry/GetMakeMast', function (data) {
        $ddl.empty();
        $ddl.append('<option value="">Select</option>');

        $.each(data, function (i, item) {
            $ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

        if (selectedValue) {
            $ddl.val(selectedValue);
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
            showToast("Error while fetching HSNCode", { type: "error" });
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
            showToast("Failed To load tax Drodpown", { type: "error" });
        }
    });
}

//================================================================
//         Calculation Block Start
//================================================================
function getTaxType(code, row) {
    $.ajax({
        url: '/PurchaseReceiptEntry/GetTaxTypeDetails',
        type: 'GET',
        data: { code: code },
        success: function (response) {
            console.log(response);
            console.log(row);
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
                row.find('input[name="PackOnBasic"]').val(
                    parseInt(response.pacK_ONBASIC ?? response.PACK_ONBASIC ?? 0)
                );
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
            showToast("Failed to get tax type details", { type: "error" });
        }
    });
}

function recalculateRow(row) {

    const usdRate = parseFloat(row.find('input[name="USDRate"]').val()) || 0;
    const exchRate = parseFloat(row.find('input[name="ExRate"]').val()) || 0;
    let rate = parseFloat(row.find('input[name="Rate"]').val()) || 0;
    const billQty = parseFloat(row.find('input[name="BillQty"]').val()) || 0;

    if (exchRate > 0) {
        rate = usdRate * exchRate;
        row.find('input[name="Rate"]').val(rate.toFixed(2));
    }

    const amount = rate * billQty;

    row.find('input[name="Amount"]').val(amount.toFixed(2));

    const packPer = parseFloat(row.find('input[name="PackPer"]').val()) || 0;
    const discPer = parseFloat(row.find('input[name="DiscPer"]').val()) || 0;

    const cgstPer = parseFloat(row.find('input[name="CGSTPer"]').val()) || 0;
    const sgstPer = parseFloat(row.find('input[name="SGSTPer"]').val()) || 0;
    const igstPer = parseFloat(row.find('input[name="IGSTPer"]').val()) || 0;
    const cessPer = parseFloat(row.find('input[name="CESSPer"]').val()) || 0;

    const otherAmt = parseFloat(row.find('input[name="OthAmt"]').val()) || 0;
    const vatAmt = parseFloat(row.find('input[name="VATAmt"]').val()) || 0;

    //================ PACK_ONBASIC Logic =================
    const discAmt = amount * discPer / 100;

    const packOnBasic = parseInt(row.find('input[name="PackOnBasic"]').val()) || 0;

    let packAmt = 0;

    if (packOnBasic === 1) {
        packAmt = amount * packPer / 100;
    } else {
        packAmt = (amount - discAmt) * packPer / 100;
    }

    const taxable = amount + packAmt - discAmt;

    const cgstAmt = taxable * cgstPer / 100;
    const sgstAmt = taxable * sgstPer / 100;
    const igstAmt = taxable * igstPer / 100;
    const cessAmt = taxable * cessPer / 100;

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
    const netAmount = rounded.toFixed(2);

    $("#NumFinalNetAmt").val(netAmount);
    $("#txtNetAmount").val(netAmount);
}
//================================================================
//         Calculation Block End
//================================================================

//================================================================
//        Bind All Data On Change of Gate No 
//================================================================
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
                    <td style="display:none;">
                         <input type="hidden" name="PackOnBasic" value="${i.PACK_ONBASIC ?? 0}" />
                    </td>
                    <td class="freeze-item">
                        <select class="erppagetable-control erppagetabledynamic-table item-name-dropdown" name="ItemName"></select>
                    </td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="HSNCode" value="${i.HSN_CODE || ''}" /></td>
                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table Unit-dropdown"
                                name="Unit"
                                data-selected-value="${i.Unit || i.UOM_CODE || ''}">
                        </select>
                    </td>

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

                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table Make-dropdown"
                                name="Make"
                                data-selected-value="${i.Make || i.MAKE_CODE || ''}">
                        </select>
                    </td>

                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table Department-dropdown"
                                name="Department"
                                data-selected-value="${i.Department || i.DEPT_CODE || ''}">
                        </select>
                    </td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="Remarks" value="${i.Remarks || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="LDRate" value="${fmt(i.LDRate ?? 0)}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table"  name="LDAmt" value="${fmt(i.LDAmt ?? 0)}" /></td>
                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table bin-dropdown"
                                name="BinLocation"
                                data-selected-value="${i.BinLocation || i.BIN_CODE || ''}">
                        </select>
                    </td>
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
        const $departmentDropdown = $lastRow.find('.Department-dropdown');
        const $BinDropdown = $lastRow.find('.bin-dropdown');
        const $UnitDropdown = $lastRow.find('.Unit-dropdown');
        const $MakeDropdown = $lastRow.find('.Make-dropdown');

        loadItemDropdowngat($itemDropdown, i.ITEM_CODE || i.ITEM_NAME);
        loadTaxTypeDropdowngat($taxDropdown, i.TaxType);
        loadDepartmentDropdown($departmentDropdown, i.DEPT_CODE);
        loadBinDropdown($BinDropdown, i.BinLocation);
        loadUnitDropdown($UnitDropdown, i.UOM_CODE);
        loadMakeDropdown($MakeDropdown, i.MAKE_CODE);
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
            showToast("Failed To load items", { type: "error" });
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
                $dropdown.append(`<option value="${tax.code}" ${selected}>${tax.name}</option>`);
            });
        },
        error: function () {
            showToast("Failed to load tax types", { type: "error" });
        }
    });
}

//======Load Data On Edit===========
function GetallDatapurchase1purchase2purchase3(data) {

    const purchase1 = data.purchase1;
    const purchase2 = data.purchase2;
    const purchase3 = data.purchase3;

    if (purchase1 && purchase1.length > 0) {

        const p1 = purchase1[0];

        //==========================
        // Simple Controls
        //==========================

        ddlDocType(function () {
            $('#ddlDocType').val(p1.v_TYPE);
        });

        $('#NumDocNo').val(p1.v_NO);
        $('#DtDocDate').val(formatDateForInput(p1.v_DATE));

        ddlGateNo(p1.gatE_NO, p1.v_TYPE).done(function () {
            $('#ddlGateNo').val(p1.gatE_NO).trigger('change.select2');
        });

        $('#txtAddLine1').val(p1.bilL_ADD1);
        $('#txtAddLine2').val(p1.bilL_ADD2);
        $('#txtAddLine3').val(p1.bilL_ADD3);

        $('#txtShipAddLine1').val(p1.shiP_ADD1);
        $('#txtShipAddLine2').val(p1.shiP_ADD2);
        $('#txtShipAddLine3').val(p1.shiP_ADD3);

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

        //==========================
        // Totals
        //==========================

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
        
        //==========================
        // Transport
        //==========================
        $('#ddlTransportName').val(p1.transporT_NAME);
        $('#hdnTransport').val(p1.transporT_CODE);
        $('#TxtVehicleNo').val(p1.trucK_NO);
        $('#TxtContainerNo').val(p1.containeR_NO);
        $('#NumFreightPay').val(p1.frtpaY_AMT);
        $('#NumFrtTax1').val(p1.frtpaY_TAXPER);
        $('#NumFrtTax2').val(p1.frtpaY_TAX);
        $('#txtFrtPayNarr').val(p1.frtpaY_NAR);
        $('#NumGRNo').val(p1.gR_NO);
        $('#DtGRDate').val(formatDateForInput(p1.gR_DATE));

        $('#txtremarks').val(p1.remarks);

        //ddlTransportName(function () {
        //    $('#ddlTransportName').val(p1.transporT_CODE);
        //});

        ddlDocStatus(function () {
            $('#ddlDocStatus').val(p1.status);
        });

        //==========================================================
        // Sequential Loading (Race Condition Fixed)
        //==========================================================

        ddlBillFrom(function () {

            $('#ddlBillFrom').val(p1.partY_CODE);

            ddlShipDetails(function () {

                $('#ddlShipFrom').val(p1.shiP_CODE);

                BillDetailsddlAddLine1(p1.partY_CODE, function () {

                    // Agar address dropdown ki value save hai to uncomment karo
                    // $('#ddladdressBD1').val(p1.PARTY_ADDRESSID);

                    ShipDetailsddlAddLine1(p1.shiP_CODE, function () {

                        // Agar ship address dropdown ki value save hai to uncomment karo
                        // $('#ddladdressSD1').val(p1.SHIP_ADDRESSID);

                        GetddlCityBillDetails(function () {

                            $('#ddlCity').val(p1.bilL_CITY);

                            GetStateByCity(p1.bilL_CITY, '#ddlState', function () {

                                $('#ddlShipCity').val(p1.shiP_CITY);

                                GetStateByCity(p1.shiP_CITY, '#ddlShipState');
                            });
                        });
                    });
                });
            });
        });
    }

    //==========================
    // Items & Attachments
    //==========================

    bindItemsdata(purchase2);
    getdataImage(purchase3);
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
                   <td style="display:none;">
                        <input type="hidden" name="PackOnBasic" value="${i.PACK_ONBASIC ?? 0}" />
                    </td>
                    <td class="freeze-item">
                        <select class="erppagetable-control erppagetabledynamic-table item-name-dropdown" name="ItemName"></select>
                    </td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="HSNCode" value="${i.hsN_CODE || ''}" /></td>
                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table Unit-dropdown"
                                name="Unit"
                                data-selected-value="${i.uoM_CODE || i.uoM_NAME}">
                        </select>
                    </td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Nos" value="${i.nos ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="PlusMinusQty" value="${i.pluS_MINUSQTY ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="RecQty" value="${i.recD_QTY ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="BillQty" value="${i.bilL_QTY ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="USDRate" value="${i.usD_RATE ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="ExRate" value="${i.excH_RATE ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Rate" value="${i.rate ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Amount" value="${amount}" /></td>
                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table EmptyYN-dropdown" name="EmptyYN">
                            <option value="">--Select--</option>
                            <option value="Yes" ${i.emptY_YN === 'Yes' ? 'selected' : ''}>Yes</option>
                            <option value="No" ${i.emptY_YN === 'No' ? 'selected' : ''}>No</option>
                        </select>
                    </td>
                    <td><input name="WBQty" class="erppagetable-control erppagetabledynamic-table" style="width: 100PX;" value="${i.wB_QTY ?? 0}" /></td>

                    <td><select class="erppagetable-control erppagetabledynamic-table TaxType-dropdown" name="TaxType"></select></td>

                    <td><input class="erppagetable-control erppagetabledynamic-table" name="PackPer" value="${i.pacK_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="PackAmt" value="${i.pacK_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="DiscPer" value="${i.disC_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="DiscAmt" value="${i.disC_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="CGSTPer" value="${i.cgsT_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="CGSTAmt" value="${i.cgsT_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="SGSTPer" value="${i.sgsT_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="SGSTAmt" value="${i.sgsT_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="IGSTPer" value="${i.igsT_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="IGSTAmt" value="${i.igsT_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="CESSPer" value="${i.cesS_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="CESSAmt" value="${i.cesS_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="VATPer" value="${i.vaT_PER ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="VATAmt" value="${i.vaT_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="OthAmt" value="${i.otH_AMT ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="NetAmt" value="${i.neT_AMT ?? amount}" /></td>

                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table Make-dropdown"
                                name="Make"
                                data-selected-value="${i.makE_CODE ?? ''}">
                        </select>
                    </td>

                    <td>
                        <select class="erppagetable-control erppagetabledynamic-table Department-dropdown"
                                name="Department"
                                data-selected-value="${i.depT_CODE ?? ''}">
                        </select>
                    </td>
                    
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="Remarks" value="${i.remarks || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="LDRate" value="${i.lanD_RATE ?? 0}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="LDAmt" value="${i.lanD_AMT ?? 0}" /></td>
                     <td>
                        <select class="erppagetable-control erppagetabledynamic-table bin-dropdown"
                                name="BinLocation"
                                data-selected-value="${i.biN_LOCATION ?? ''}">
                        </select>
                    </td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="POType" value="${i.pO_TYPE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="PONo" value="${i.pO_NO || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="KantaType" value="${i.kantA_TYPE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="KantaNo" value="${i.kantA_NO || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="ReqType" value="${i.reQ_TYPE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="ReqNo" value="${i.reQ_NO || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="GateType" value="${i.gatE_TYPE || ''}" /></td>
                    <td><input class="erppagetable-control erppagetabledynamic-table" name="GateNo" value="${i.doC_ID || ''}" /></td>
                    <td class="action-col">
                        <div class="action-wrap">
                            <button type="button" class="act-btn edit btn-edit"><i class="fa fa-edit"></i></button>
                            <button type="button" class="act-btn delete btn-delete btn-delete-row"><i class="fa fa-trash"></i></button>
                        </div>
                    </td>
                </tr>
        `);
        $tbody.append($tr);
        let $lastRow = $tbody.find('tr').last();
        let $itemDropdown = $lastRow.find('select[name="ItemName"]');
        let $taxDropdown = $lastRow.find('select[name="TaxType"]');
        let $departmentDropdown = $lastRow.find('.Department-dropdown');
        let $BinDropdown = $lastRow.find('.bin-dropdown');
        let $UnitDropdown = $lastRow.find('.Unit-dropdown');
        let $MakeDropdown = $lastRow.find('.Make-dropdown');

        loadItemDropdowngat($itemDropdown, i.iteM_CODE);
        loadTaxTypeDropdowngatCode($taxDropdown, i.taX_CODE);
        loadDepartmentDropdown($departmentDropdown, i.depT_CODE);
        loadBinDropdown($BinDropdown, i.biN_LOCATION);
        loadUnitDropdown($UnitDropdown, i.uoM_CODE);
        loadMakeDropdown($MakeDropdown, i.makE_CODE);
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
            console.error('Failed to load tax types.');
        }
    });
}

//================= Attachment Data Bind =====================
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

        rowsAttachment.push({
            FileName: fileName,
            IMG_FILE: base64,
            FILE_NAME: fileName,
            FILE_TYPE: mimeType
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

//================================================================
//        ReadOnly Mode
//================================================================
function setFormReadOnly() {

    $('#PurchaseReceiptEntryForm').find('input:not([type=button]):not([type=submit]):not([type=hidden])').prop('readonly', true);

    $('#PurchaseReceiptEntryForm').find('select, textarea').prop('disabled', true);

    $('#customToggle').css('pointer-events', 'none');

    $('#btn-save').hide();
    $('#btnShowProductionBatch').hide();

    $('#tblPurchaseReceiptIR tbody').find('input:not([type=button])').prop('readonly', true);

    $('#tblPurchaseReceiptIR tbody').find('select, textarea').prop('disabled', true);

    $('#tblPurchaseReceiptIR .btn-edit').hide();
    $('#tblPurchaseReceiptIR .btn-delete').hide();
    $('#tblPurchaseReceiptIR .btn-delete-row').hide();

    $('.btn-add-row').hide();
    $('#btnCreateIntimation').hide();
    $('#tblPurchaseReceiptIR tbody').css('pointer-events', 'none');

    $('.erppage-btn-print').show();       
    $('.erppage-btn-common').show();      
    $('.erppage-header-back').show();     
    $('#btnCreateIntimation').show();     
}

//=====helper function for decimal ============
function fmt(val) {
    const num = parseFloat(val);
    return isNaN(num) ? "0.00" : num.toFixed(2);
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

//================================================================
//        Foam Validation
//================================================================

function validateDataForPRMRN() {

    const docNo = parseInt($('#NumDocNo').val()) || 0;
    const docType = $('#ddlDocType').val();
    const returnType = $('#ddlReturnType').val();

    const billNo = $('#txtBillNo').val().trim();
    const billDate = $('#DtBillDate').val().trim();
    const docDate = $('#DtDocDate').val();

    const challanNo = $('#NumChallanNo').val().trim();
    const challanDate = $('#DtChallanDate').val().trim();

    //==================== Required Fields ====================

    if (!validateRequiredField('#ddlDocType', 'Doc Type')) return false;
    if (!validateRequiredField('#ddlBillFrom', 'Bill From')) return false;
    if (!validateRequiredField('#txtBillNo', 'Bill No')) return false;
    if (!validateRequiredField('#ddlCity', 'Bill City')) return false;
    if (!validateRequiredField('#ddlShipCity', 'Ship City')) return false;

    //==================== Header Validation ====================

    if (docNo <= 0) {
        setInvalid($('#NumDocNo'), "Invalid Voucher No. Record not saved.");
        return false;
    }

    if (billNo !== "" && billDate === "") {
        setInvalid($('#dtpBillDate'), "Bill date not entered.");
        return false;
    }

    if (billDate !== "" && new Date(billDate) > new Date(docDate)) {
        setInvalid($('#dtpBillDate'), "Bill date can not be greater than Voucher date.");
        return false;
    }

    if (challanNo !== "" && challanDate === "") {
        setInvalid($('#dtpChallanDate'), "Challan date not entered.");
        return false;
    }

    if (challanDate !== "" && new Date(challanDate) > new Date(docDate)) {
        setInvalid($('#dtpChallanDate'), "Challan date can not be greater than Voucher date.");
        return false;
    }

    //==================== RCPT WB Qty Validation ====================

    if (docType === "RCPT") {

        let totalWBQty = 0;
        let totalRecQty = 0;

        $('#tblPurchaseReceiptIR tbody tr').each(function () {

            const itemCode = $(this).find('.item-name-dropdown').val();
            if (!itemCode) return;

            const wbQty = parseFloat($(this).find('.WBQty').val()) || 0;
            const recQty = parseFloat($(this).find('.RecQty').val()) || 0;

            totalWBQty += wbQty;
            totalRecQty += recQty;
        });

        if (totalWBQty !== totalRecQty) {
            showToast("Please check, Received Qty and WB Qty Not Matched in totality", { type: "error" });
            return false;
        }
    }

    //==================== Row Validation ====================

    const rows = $('#tblPurchaseReceiptIR tbody tr');

    for (let i = 0; i < rows.length; i++) {

        const row = $(rows[i]);

        const recQty = parseFloat(row.find('input[name="RecQty"]').val()) || 0;
        const plusMinusQty = parseFloat(row.find('input[name="PlusMinusQty"]').val()) || 0;
        const wbQty = parseFloat(row.find('input[name="WBQty"]').val()) || 0;
        const amount = parseFloat(row.find('input[name="Amount"]').val()) || 0;

        const itemCode = parseInt(row.find('select[name="ItemName"]').val()) || 0;
        const itemName = row.find('select[name="ItemName"] option:selected').text().trim();

        const taxCode = row.find('.TaxType-dropdown').val();
        const poType = row.find('input[name="POType"]').val();

        // Item Validation
        if (itemName !== "Select" && itemName !== "" && itemCode === 0) {
            showToast("Item code not valid.", { type: "error" });
            row.find('select[name="ItemName"]').focus();
            return false;
        }

        // Received Qty
        if (recQty === 0) {
            showToast("Received Qty is 0.", { type: "warning" });
            row.find('input[name="RecQty"]').focus();
            return false;
        }

        // WB Qty
        if (wbQty === 0 && plusMinusQty !== 0 && returnType !== "Return") {
            showToast("+/- Qty is not valid if WB Qty is 0.", { type: "error" });
            row.find('input[name="WBQty"]').focus();
            return false;
        }

        // Amount
        if (amount === 0) {
            showToast("Amount must not be 0.", { type: "error" });
            row.find('input[name="Amount"]').focus();
            return false;
        }

        // PO Type
        if (poType === "PAUD") {
            showToast(`Kindly make PO of Item => ${itemName}`, { type: "error" });
            row.find('input[name="POType"]').focus();
            return false;
        }

        // Tax Type
        if (!taxCode || taxCode === "0") {
            showToast("Tax Type not selected.", { type: "error" });
            row.find('.TaxType-dropdown').focus();
            return false;
        }

        // SRPU Validation
        if (docType === "SRPU" && poType !== "PORD") {
            showToast("Please Select correct Order/Gate No.", { type: "error" });
            row.find('input[name="POType"]').focus();
            return false;
        }

        // SRJW Validation
        if (docType === "SRJW" && poType !== "JORD") {
            showToast("Please Select correct Order/Gate No.", { type: "error" });
            row.find('input[name="POType"]').focus();
            return false;
        }
    }

    return true;
}

//function validateDataForPRMRN() {

//    const docNo = parseInt($('#NumDocNo').val()) || 0;
//    const billNo = $('#txtBillNo').val().trim();
//    const billDate = $('#DtBillDate').val().trim();
//    const DocDate = $('#DtDocDate').val();
//    const challanNo = $('#NumChallanNo').val().trim();
//    const challanDate = $('#DtChallanDate').val().trim();
//    //const recQty = parseFloat(row.find('input[name="RecQty"]').val()) || 0;
//    //const plusMinusQty = parseFloat(row.find('input[name="PlusMinusQty"]').val()) || 0;
//    //const wbQty = parseFloat(row.find('input[name="WBQty"]').val()) || 0;
//    const returnType = $('#ddlReturnType').val(); 
//    //const amount = parseFloat(row.find('input[name="Amount"]').val()) || 0;
//    //const itemName = row.find('select[name="ItemName"] option:selected').text();
//    //const taxCode = row.find('.TaxType-dropdown').val();
//    //const poType = row.find('input[name="POType"]').val();

//    if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;
//    if (!validateRequiredField('#ddlBillFrom', 'Bill From')) return;
//    if (!validateRequiredField('#txtBillNo', 'Bill No')) return;
//    if (!validateRequiredField('#ddlCity', 'Bill City')) return;
//    if (!validateRequiredField('#ddlShipCity', 'Ship City')) return;
    
//    if (docNo <= 0) {
//        setInvalid($('#NumDocNo'), "Invalid Voucher No. Record not saved.");
//        return false;
//    }

//    if (billNo !== "" && billDate === "") {
//        setInvalid($('#dtpBillDate'), "Bill date not entered.");
//        return false;
//    }

//    if (billDate !== "" && new Date(billDate) > new Date(DocDate)) {
//        setInvalid($('#dtpBillDate'), "Bill date can not be greater than Voucher date.");
//        return false;
//    }

//    if (challanNo !== "" && challanDate === "") {
//        setInvalid($('#dtpChallanDate'), "Challan date not entered.");
//        return false;
//    }

//    if (challanDate !== "" && new Date(challanDate) > new Date(DocDate)) {
//        setInvalid($('#dtpChallanDate'), "Challan date can not be greater than Voucher date.");
//        return false;
//    }

//    // ============================
//    // RCPT WB Qty vs Received Qty Validation
//    // ============================
//    if ($('#ddlDocType').val() === "RCPT") {

//        let totalWBQty = 0;
//        let totalRecQty = 0;

//        $('#tblPurchaseReceiptIR tbody tr').each(function () {

//            const itemCode = $(this).find('.item-name-dropdown').val();
//            if (!itemCode) return;

//            const wbQty = parseFloat($(this).find('.WBQty').val()) || 0;
//            const recQty = parseFloat($(this).find('.RecQty').val()) || 0;

//            totalWBQty += wbQty;
//            totalRecQty += recQty;
//        });

//        if (totalWBQty !== totalRecQty) {
//            showToast("Please check, Received Qty and WB Qty Not Matched in totality", { type: "error" });
//            return false;
//        }
//    }

//    const rows = $('#tblPurchaseReceiptIR tbody tr');

//    for (let i = 0; i < rows.length; i++) {

//        const row = $(rows[i]);
//        const recQty = parseFloat(row.find('input[name="RecQty"]').val()) || 0;
//        const plusMinusQty = parseFloat(row.find('input[name="PlusMinusQty"]').val()) || 0;
//        const wbQty = parseFloat(row.find('input[name="WBQty"]').val()) || 0;
//        const amount = parseFloat(row.find('input[name="Amount"]').val()) || 0;
//        const itemName = row.find('select[name="ItemName"] option:selected').text().trim();
//        const taxCode = row.find('.TaxType-dropdown').val();
//        const poType = row.find('input[name="POType"]').val();
        
//        const itemCode = parseInt(row.find('select[name="ItemName"]').val()) || 0;

//        if (itemName !== "Select" && itemName !== "" && itemCode === 0) {
//            showToast("Item code not valid.", { type: "error" });
//            row.find('select[name="ItemName"]').focus();
//            return false;
//        }
//    }

//    if (recQty === 0) {
//        showToast("Received Qty is 0.", { type: "warning" });
//        row.find('input[name="RecQty"]').focus();
//    }

//    if (wbQty === 0 && plusMinusQty !== 0 && returnType !== "Return") {
//        showToast("+/- Qty is not valid if WB Qty is 0.", { type: "error" });
//        row.find('input[name="WBQty"]').focus();
//        return false;
//    }

//    if (amount === 0) {
//        showToast("Amount must not be 0.", { type: "error" });
//        row.find('input[name="Amount"]').focus();
//        return false;
//    }

//    if (poType === "PAUD") {
//        showToast(`Kindly make PO of Item => ${itemName}`, { type: "error" });
//        row.find('input[name="POType"]').focus();
//        return false;
//    }

//    if (!taxCode || taxCode === "0") {
//        showToast("Tax Type not selected.", { type: "error" });
//        return false;
//    }

//    if (docType === "SRPU" && poType !== "PORD") {
//        showToast("Please Select correct Order/Gate No.", { type: "error" });
//        row.find('input[name="POType"]').focus();
//        return false;
//    }
    
//    if (docType === "SRJW" && poType !== "JORD") {
//        showToast("Please Select correct Order/Gate No.", { type: "error" });
//        row.find('input[name="POType"]').focus();
//        return false;
//    }

//}

//============Copy From Code==============
function GetDocTypeCopyFrom() {

    $.ajax({
        url: '/PurchaseReceiptEntry/GetDocTypeCopyFrom',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            console.log("Copy Data", data);
            const $menu = $('#copyFromMenu');
            $menu.empty();

            $.each(data, function (index, item) {

                $menu.append(`
                    <li>
                        <a class="dropdown-item erppagedropdown-item copy-from-item"
                           href="#"
                           data-vtype="${item.value}">
                            ${item.text}
                        </a>
                    </li>
                `);

            });

        },
        error: function () {
            showToast("Unable To Load Copy From Documents", { type: "error" });
        }
    });
}

function loadCopyFrom(vType, receiptType, partyCode) {

    const currentVNo = $('#NumDocNo').val(); 
    
    $.ajax({

        url: '/PurchaseReceiptEntry/GetCopyFromData',
        type: 'Get',
        data: {
            vType: vType, receiptType: receiptType, partyCode: partyCode, currentVNo: currentVNo
        },
        success: function (data) {

            const tbody = $('#tblPurchaseRequest tbody');
            tbody.empty();

            $.each(data, function (index, item) {

                tbody.append(`
                    <tr>
                        <td><input type="checkbox" class="copyRow"></td>

                        <td>${item.vNo}</td>
                        <td>${item.vType}</td>
                        <td>${item.vDate}</td>
                        <td>${item.itemCode}</td>
                        <td>${item.itemName}</td>     
                        <td>${item.unit}</td>
                        <td>${item.nos}</td>
                        <td>${item.qty}</td>
                        <td>${item.balQty}</td>
                        <td>${item.rate}</td>
                        <td>${item.taxType}</td>
                        <td>${item.packPer}</td>
                        <td>${item.discPer}</td>
                        <td>${item.cgstPer}</td>
                        <td>${item.sgstPer}</td>
                        <td>${item.igstPer}</td>
                        <td>${item.cessPer}</td>
                        <td>${item.cessAmt}</td>
                        <td>${item.vatPer}</td>
                        <td>${item.othAmt}</td>
                        <td>${item.make}</td>
                        <td>${item.department}</td>
                        <td>${item.remarks}</td>
                        <td>${item.reqType}</td>
                        <td>${item.reqNo}</td>

                        <td class="hidden-col">${item.uCode}</td>
                        <td class="hidden-col">${item.makeCode}</td>
                        <td class="hidden-col">${item.taxCode}</td>
                        <td class="hidden-col">${item.deptCode}</td>

                        <td class="hidden-col"></td>
                    </tr>
                `)

            });

            $('#copyFromModal').modal('show');
        },
        error: function () {
            showToast("Unable to load Copy From data.", { type: "error" });
        }

    }); 
}

//========================
// Reports 
//========================
function PendingReport() {

    var reportName = "m_r_note";
     
    var SelForMul =
        "{PURCHASE1.V_TYPE}='" + $("#ddlDocType").val() + "'" +
        " AND {PURCHASE1.V_NO}= " + $("#NumDocNo").val() +
        " AND {PURCHASE1.COMP_CODE}= " + window.globalVariables.compCode +
        " AND {PURCHASE1.BRANCH_CODE}= " + window.globalVariables.branchCode +
        " AND {PURCHASE1.YEAR_CODE}= " + window.globalVariables.yearCode;
    var formulaFields = {
        Reportname: reportName,
        selectionFormula: SelForMul,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "MATERIAL RECEIPT NOTE"
        }
    };

    var now = new Date();
    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);
    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');
    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
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
            console.error('Error generating report:', error);
        }
    });
}

async function checkIntimation() {
    try {
        const mrnType = $('#ddlDocType').val();
        const mrnNo = $('#NumDocNo').val();

        const url = `/PurchaseReceiptEntry/CheckIntimation?mrnType=${encodeURIComponent(mrnType)}&mrnNo=${encodeURIComponent(mrnNo)}`;

        const response = await fetch(url);

        const result = await response.json();

        if (!result.status) {
            showToast(result.message, { type: "warning" });
            return false;
        }

        return true;

    } catch (error) {
        console.error(error);
        showToast("Unable to verify Intimation.", { type: "error" });
        return false;
    }
}

async function PrintIntimation() {

    const isValid = await checkIntimation();
    if (!isValid) return;

    var reportName = "INTIMATION";

    var SelForMul =
        "{INTIMATION.MRN_TYPE}='" + $("#ddlDocType").val() + "'" +
        " AND {INTIMATION.V_TYPE}='INTI'" +
        " AND {INTIMATION.MRN_NO}= " + $("#NumDocNo").val() +
        " AND {INTIMATION.COMP_CODE}= " + window.globalVariables.compCode +
        " AND {INTIMATION.BRANCH_CODE}= " + window.globalVariables.branchCode +
        " AND {INTIMATION.YEAR_CODE}= " + window.globalVariables.yearCode;
    var formulaFields = {
        Reportname: reportName,
        selectionFormula: SelForMul,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "MATERIAL INTIMATION NOTE"
        }
    };

    var now = new Date();
    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);
    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');
    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
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
            console.error('Error generating report:', error);
        }
    });
}

async function updatePendingGateIn() {

    try {

        const response = await fetch('/PurchaseReceiptEntry/UpdatePendingGateIn', {
            method: 'POST'
        });

        const result = await response.json();

        if (!result.status) {
            showToast(result.message, { type: "error" });
            return false;
        }

        return true;

    } catch (error) {
        console.error(error);
        showToast("Unable to update Pending Gate In.", { type: "error" });
        return false;
    }
}

async function PendingGateReport() {

    const updated = await updatePendingGateIn();
    if (!updated) return;

    var reportName = "gatepass2";

    var SelForMul =
        "{GATE1.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {GATE1.BRANCH_CODE}=" + window.globalVariables.branchCode +
        " AND {GATE1.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {GATE2.MRN_NO}=0" +
        " AND {DOCTYPE_MAST.DOCTYPE}='GateInward'" +
        " AND ({GATE1.V_TYPE}='INRM' OR {GATE1.V_TYPE}='INST' OR {GATE1.V_TYPE}='INFU')";
    
    var formulaFields = {
        Reportname: reportName,
        selectionFormula: SelForMul,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "Pending Gate Inward for MRN"
        }
    };

    var now = new Date();
    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);
    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');
    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
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
            console.error('Error generating report:', error);
        }
    });
}

//===============================
//  Check validate Date
//===============================
async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/PurchaseReceiptEntry/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();

        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;

    } catch (error) {
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

