async function LoadDropDown() {
    try {
        await Promise.all([
            DDLShipFrom(),
            DDLPurchaseThrough(),
            DDLTaxRate(),
            DDLPaymentTerm(),
            DDLBrokerName(),
            DDLDispatchForm(),   
            DDLPartyMast(),
            DDLItemMaster(),
            ddlDeliveryFrom(),
            DDLCityMast(),
            DDLstatus()
        ]);
    } catch (error) {
        console.error("Error loading dropdowns:", error);
    }
}
function formatDate(dateStr) {
    if (!dateStr) return null;

    const date = new Date(dateStr);
    if (isNaN(date)) return null;

    // Extract local date parts
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0'); // Months 0-11
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
}

function collectPurchaseDocumentsData() {

    const documents = [];
    let errorMessages = [];

    // =================================================
    // 1. GLOBAL VARIABLE (DB LOADED DATA)
    // =================================================
    if (Array.isArray(globalAttachments)) {
        globalAttachments.forEach(att => {

            if (!att) return;

            documents.push({
                FileName: att.FileName || att.fileName || "",
                FilePath: att.FilePath || att.filePath || null,
                IsNew: false
            });
        });
    }

    // =================================================
    // 2. UI - NEW ERP STYLE (.erp-file-row)
    // =================================================
    $("#fileList .erp-file-row").each(function () {

        const fileName = $(this).data("filename");
        const filePath = $(this).data("filepath");

        if (fileName && !documents.some(d => d.FileName === fileName)) {
            documents.push({
                FileName: fileName,
                FilePath: filePath || null,
                IsNew: false
            });
        }
    });

    // =================================================
    // 3. UI - OLD STYLE (.erppageattachmentsectionfileitem)
    // =================================================
    $("#fileList .erppageattachmentsectionfileitem").each(function () {

        const fileName = $(this)
            .find(".erppageattachmentsectionfilename")
            .text()
            .trim();

        if (fileName && !documents.some(d => d.FileName === fileName)) {
            documents.push({
                FileName: fileName,
                FilePath: null,
                IsNew: false
            });
        }
    });

    // =================================================
    // 4. NEW FILES FROM INPUT
    // =================================================
    const fileInput = document.getElementById("fileInput");

    if (fileInput && fileInput.files.length > 0) {

        Array.from(fileInput.files).forEach(file => {

            if (!file || !file.name) {
                errorMessages.push("Invalid file detected.");
                return;
            }

            documents.push({
                FileName: file.name,
                FilePath: "/attachments/pan/" + file.name,
                FileObject: file,
                IsNew: true
            });
        });
    }

    // =================================================
    // 5. ERROR HANDLING
    // =================================================
    if (errorMessages.length > 0) {
        toastr.error(errorMessages.join(" "));
        return [];
    }

    return documents;
}
function addAttachmentRow(data = {}) {
    const index = data.index || attachmentIndex++;
    const row = `
        <tr>
          <td style="display:none;">
            <input type="hidden" class="attachment-code" value="${index}" />
          </td>
          <td>
            <input type="text" class="form-control mb-1" id="fileName_${index}" value="${data.fileName || ''}" placeholder="Enter file name" disabled />
          </td>
          <td>
            <input type="file" class="form-control file-upload" id="fileInput_${index}" />
          </td>
          <td>
            <i class="fa fa-plus btn-add-action" title="Add Row" style="cursor:pointer; margin-right:8px;"></i>
            <i class="fa fa-edit btn-edit-action" title="Edit Row" style="cursor:pointer; margin-right:8px;"></i>
            <i class="fa fa-trash btn-delete-action" title="Delete Row" style="cursor:pointer;"></i>
          </td>
        </tr>
      `;
    $attachmentTbody.append(row);
}
function recalculateNetRate() {
    const baseRate = parseFloat($('#numRate').val()) || 0;
    const discountPercent = parseFloat($('#numDiscount').val()) || 0;
    let taxRate = 0;
    const selectedText = $('#ddlTaxRate').find('option:selected').text();
    const taxMatch = selectedText.match(/\d+(\.\d+)?/);
    if (taxMatch) {
        taxRate = parseFloat(taxMatch[0]);
    }
    const discountAmount = baseRate * discountPercent / 100;
    const rateAfterDiscount = baseRate - discountAmount;
    const taxAmount = rateAfterDiscount * taxRate / 100;
    const roundedTax = Math.round(taxAmount);
    const finalNetRate = rateAfterDiscount + taxAmount;
    $('#numNetRate').val(finalNetRate.toFixed(2));
    $('#numTaxRate').val(taxRate.toFixed(2));
}

async function GetVNo() {
    try {
        const res = await fetch('/PurchaseSauda/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#txtDocNo').val(data.v_NO);
        $('#TxtDispatchDocNo').val(data.v_NO);

    } catch (e) {
        console.error('GetVNo failed:', e);
        toastr.error('Error loading Document Number: ' + e.message);
    }
}

async function LoadFormByID(id) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSaudaList/GetDataByCode',
            method: 'GET',
            data: { code: id }
        });

        if (res.success) {
            const header = res.data.header;
            const DispatchDetails = res.data.details;
            const attachments = res.data.attachment || [];

            console.log("attachments", attachments);

            $('#TxtCode').val(header.v_NO || '');
            $('#txtDocNo').val(header.v_NO || '');
            $('#TxtDispatchDocNo').val(header.v_NO || '');
            $('#dtDocDate').val(formatDate(header.v_DATE));
            $('#DispatchDocDate').val(formatDate(header.v_DATE));          
            $('#ddlSupplyFrom').val(header.shiP_TYPE || '');
            $('#ddlPartyName').val(header.partY_CODE || '').trigger('change');
            $('#ddlShipFrom').val(header.shiP_CODE || '').trigger('change');
            $('#ddlItemName').val(header.iteM_CODE || '').trigger('change');
            $('#txtAddress1').val(header.adD1 || '');
            $('#txtAddress2').val(header.adD2 || '');
            $('#txtAddress3').val(header.adD3 || '');
            $('#txtStation').val( header.citY_CODE || 0);
            $('#numTrucks').val(header.trucK_NO || '');
            $('#txtExRate').val(header.exrate || '');
            $('#numDiscount').val(header.disC_PER || '');
            $('#txtRemarks').val(header.remark || '');
            $('#NumPINO').val(header.pino || '');
            $('#DtPIDate').val(formatDate(header.pidate));
            $('#NumOfferNo').val(header.offerno || '');
            $('#NumBrokerage').val(header.brokeR_RATE || '');
            $('#ddlBrokerName').val(header.broker || '');
            $('#ddlPackingType').val(header.pacK_TYPE || '');
            $('#ddlDispatchFrom').val(header.dispatcH_FROM || '');
            $('#ddlPaymentType').val(header.paymenT_STATUS || '');
            $('#ddlRate').val(header.currency || '');
            $('#DtSBLCDue').val(formatDate(header.sblC_DUEDATE));
            $('#ddlGrade').val(header.grade || '');
            $('#TxtItemRemarks').val(header.iteM_REMARKS || '');
            $('#numWaste').val(header.wastE_PER || 0);
            $('#numRate').val(header.rate || 0);
            $('#numTaxRate').val(header.taX_RATE || 0);
            $('#ddlTaxRate').val(header.taX_CODE || 0);
            $('#chkNatural').prop('checked', header.onlY_NATURAL == 1);
            $('#ddlItemType').val(header.iteM_TYPE || '');
            $('#numWeight').val(header.qty || 0);
            $('#ddlFreightTerm').val(header.frT_TERM || '');
            $('#numNetRate').val(header.neT_RATE || 0);
            $('#ddlPaymentTerm').val(header.payterM_CODE || 0);
            $('#txtDeliveryTerm').val(header.deL_TERM || '');
            $('#ddlDocStatus').val(header.status);
            $('#DtLCDue').val(formatDate(header.lC_DUEDATE));
            $('#ddlPurchaseThrough').val(header.deaL_THROUGH || 0);
            $('#txtCountry').val(header.countrY_CODE || 0);
            $('#txtCountry').val(header.country || '');
            $('#txtContactNo').val(header.phone || '');
            $('#txtDeliveryFrom').val(header.partY_TO || '');
            $('#numFreightRate').val(header.frT_RATE || '');
            $('#NumOfferRate').val(header.offerRate || '');

            const attachBody = $('#fileList');
            attachBody.empty();

            if (!attachments || attachments.length === 0) {
                attachBody.html(`
                    <div class="erp-empty-state text-center text-muted">
                        No attachments found.
                    </div>
                `);
            } else {

                globalAttachments = res.data.attachment || [];
                const attachments = globalAttachments;

                console.log("attachments", globalAttachments);


                attachments.forEach((att, idx) => {
                    const fileName = att.FileName ?? att.fileName ?? `File_${idx + 1}`;
                    const filePath = att.FilePath ?? att.filePath ?? '';

                    const extension = fileName.split('.').pop()?.toLowerCase() || '';

                    let filePreview = '';

                    if (filePath && ['png', 'jpg', 'jpeg', 'gif', 'webp'].includes(extension)) {
                        filePreview = `<span>${extension.toUpperCase()}</span>`;
                    }
                    else if (extension === 'pdf') {
                        filePreview = `<i class="fa fa-file-pdf-o" style="color:#e53935;"></i>`;
                    }
                    else {
                        filePreview = `<i class="fa fa-file-o"></i>`;
                    }

                    const card = `
                        <div class="file-item erp-file-row"
                             data-filename="${fileName}"
                             data-filepath="${filePath}">

                            <div class="erp-file-preview">
                                ${filePreview}
                            </div>

                            <div class="erp-file-info">
                                <div class="erp-file-name">${fileName}</div>
                                <div class="erp-file-type">${extension.toUpperCase() || 'FILE'}</div>
                            </div>

                            <div class="erp-file-actions">
                                <a href="${filePath}" target="_blank" class="erp-view-btn">View</a>

                                <button type="button"
                                        class="erp-delete-btn btn-delete-attachment">
                                    Delete
                                </button>
                            </div>

                        </div>
                        `;

                    attachBody.append(card);
                });
            }

        } else {
            toastr.error(res.message || "Failed to load data.");
            console.error("Error from server:", res.message);
        }
    } catch (err) {
        console.error("Failed to load data", err);
        toastr.error("Something went wrong while loading the form.");
    }
}

async function fetchDDlParty(PartyId) {
    try {
        const response = await fetch(`/PurchaseSauda/GetDataByPartyCode?PartyId=${PartyId}`);
        const data = await response.json();
        console.log("data", data);
        $('#txtAddress1').val(data?.supplier?.address1 || '');
        $('#txtAddress2').val(data?.supplier?.address2 || '');
        $('#txtAddress3').val(data?.supplier?.address3 || '');
        $('#txtContactNo').val(data?.supplier?.mobile || '');
        $('#txtStation').val(data?.supplier?.cityCode || '');
        $('#txtCountry').val(data?.supplier?.country || '');
        $('#txtDeliveryFrom').val(data?.supplier?.name || '');
        $('#ddlShipFrom').val(data?.supplier?.code || '').trigger('change');
 
        $('#ddlPaymentTerm').val(data?.partermcode || '');
        $('#ddlFreightTerm').val(data?.sauda?.frtTerm || '');
        $('#txtDeliveryTerm').val(data?.sauda?.delTerm || '');
        $('#ddlItemName').val(data?.sauda?.itemCode || '');
        $('#ddlItemType').val(data?.sauda?.itemType || '');

    } catch (error) {
        console.error('Failed to fetch party data:', error);
    }
}

async function DDLPartyMast() {
    const res = await fetch("/PurchaseSauda/DDLPartyMast");
    const list = await res.json();
    const ddl = $("#ddlPartyName");

    ddl.empty().append('<option value="">Select Party Name</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Party Name --",
        allowClear: true,
        width: '100%'
    });

}

async function DDLCityMast() {
    try {
        const res = await fetch('/PurchaseSauda/DDLCityMast');
        const data = await res.json();
        const ddl = $('#txtStation');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function DDLstatus() {
    try {
        const res = await fetch('/PurchaseSauda/DDLstatus');
        const data = await res.json();
        const ddl = $('#ddlDocStatus');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Status:", error);
    }
}

async function DDLShipFrom() {
    const res = await fetch("/PurchaseSauda/DDLShipFrom");
    const list = await res.json();
    const ddl = $("#ddlShipFrom");

    ddl.empty().append('<option value="">Select Ship From</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Ship From--",
        allowClear: true,
        width: '100%'
    });

}

async function ddlDeliveryFrom() {
    try {
        const res = await fetch('/PurchaseSauda/DDLShipFrom');
        const data = await res.json();
        const ddl = $('#ddlDeliveryFrom');
        ddl.empty().append('<option value="">-- Select Delivery From --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Delivery From:", error);
    }
}

async function DDLPurchaseThrough() {
    try {
        const res = await fetch('/PurchaseSauda/DDLPurchaseThrough');
        const data = await res.json();

        const ddl = $('#ddlPurchaseThrough');
        ddl.empty().append('<option value="">-- Select Purchase Through --</option>');

        if (Array.isArray(data)) {
            data.forEach(item => {
                ddl.append(`<option value="${item.value}">${item.value} - ${item.text}</option>`);
            });
        } else {
            console.warn('Unexpected data format:', data);
        }

    } catch (error) {
        console.error("Error loading Purchase Through:", error);
        toastr.error("Failed to load Purchase Through options.");
    }
}

async function DDLItemMaster() {
    const res = await fetch("/PurchaseSauda/DDLItemMaster");
    const list = await res.json();
    const ddl = $("#ddlItemName");

    ddl.empty().append('<option value="">Select Item Name</option>');
    list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

    ddl.select2({
        placeholder: "-- Select Item Name --",
        allowClear: true,
        width: '100%'
    });




}

async function DDLTaxRate() {
    try {
        const res = await fetch('/PurchaseSauda/DDLTaxRate');
        const data = await res.json();
        const ddl = $('#ddlTaxRate');
        ddl.empty().append('<option value="">-- Select Tax Rate --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Tax Rate:", error);
    }
}

async function DDLPaymentTerm() {
    try {
        const res = await fetch('/PurchaseSauda/DDLPaymentTerm');
        const data = await res.json();
        const ddl = $('#ddlPaymentTerm');
        ddl.empty().append('<option value="">-- Select Payment Term --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Payment Term:", error);
    }
}

async function DDLBrokerName() {
    try {
        const res = await fetch('/PurchaseSauda/DDLBrokerName');
        const data = await res.json();
        const ddl = $('#ddlBrokerName');
        ddl.empty().append('<option value="">-- Select Broker Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Broker Name:", error);
    }
}

async function DDLDispatchForm() {
    try {
        const res = await fetch('/PurchaseSauda/DDLDispatchForm');
        const data = await res.json();
        const ddl = $('#ddlDispatchFrom');
        ddl.empty().append('<option value="">-- Select Dispatch From --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Dispatch Form:", error);
    }
}
function setFormReadOnly() {
    const form = $('#PurchaseSaudaForm');
    form.find('input, select, textarea, button').prop('disabled', true);
    form.find('textarea').css('background-color', '#f0f0f0');
    form.find('table tbody tr').each(function () {
        $(this).find('input, select, textarea').prop('disabled', true);
        $(this).css('background-color', '#f9f9f9');
    });
    form.find('.btn-save').hide();
}

async function checkValidDate() {
    const data = {
        vdate: $("#dtDocDate").val(),
        vtype: 'PAUD',
        vno: $("#txtDocNo").val()
    };
    try {
        const response = await fetch('/PurchaseSauda/CheckValidDate', {
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
        showToast(result.message, { type: "warning" });
        return false;
    }
}
function generateSelect(data, selectedValue = '') {
    let options = `<option value="">-- Select Item --</option>`;
    for (let code in data) {
        const name = data[code];
        const selected = code === selectedValue ? 'selected' : '';
        options += `<option value="${code}" data-code="${code}">${name}</option>`;
    }
    return options;
}
function addPurchaseQuotationRow(data = {}) {

    data.index = data.index !== undefined ? data.index : rowIndex++;
    const row = `
            <tr class="no-border-input" data-row="${data.index}">
                <td><input type="text" class="form-control icode" id="icode_${data.index}" value="${data.icode || ''}" readonly /></td>
                <td><select class="form-control itemName">${generateSelect(itemMap, data.itemName)}</select></td>
                <td><input type="date" class="form-control" id="delDate_${data.index}" value="${data.delDate || ''}" /></td>

            <td>
              <input type="number"
                     class="form-control"
                     id="quantity_${data.index}"
                     value="${data.quantity || ''}"
                        oninput="
                        this.value = this.value.replace(/[^0-9.]/g,'');
                        if (this.value.length > 16) this.value = this.value.slice(0,16);
                        " />
            </td>

            <td>
              <input type="text"
                     class="form-control"
                     id="remarks_${data.index}"
                     value="${data.remarks || ''}"
                     maxlength="100" />
            </td>

                <td class="action-col">
                     <button class="act-btn add btn-add-actions" title="Add Row" style="cursor:pointer;">
                      <i class="fa fa-plus-circle"></i>
                    </button> 
                     <button class="act-btn delete btn-delete-actions" title="Delete Row" style="cursor:pointer;">
                      <i class="fa fa-trash"></i>
                    </button>
                </td>
            </tr>
        `;

    $tbody.append(row);
}
function Getitem() {
    return $.ajax({
        url: '/PurchaseSauda/DDLDispatchItemMaster',
        type: 'GET',
        success: function (response) {
            if (response.length > 0) {
                itemMap = {};
                reverseItemMap = {};

                response.forEach(item => {
                    itemMap[item.value] = item.text;
                    reverseItemMap[item.text] = item.value;
                });


                const existingData = [];
                $tbody.find('tr').each(function () {
                    const $tr = $(this);
                    const data = {
                        index: $tr.data('row'),
                        icode: $tr.find('.icode').val(),
                        itemName: $tr.find('.itemName').val(),
                        delDate: $tr.find(`#delDate_${$tr.data('row')}`).val(),
                        quantity: $tr.find(`#quantity_${$tr.data('row')}`).val(),
                        remarks: $tr.find(`#remarks_${$tr.data('row')}`).val()
                    };
                    existingData.push(data);
                });

                $tbody.empty();
                existingData.forEach(rowData => addPurchaseQuotationRow(rowData));
            } else {
                console.warn("⚠️ No Item data returned.");
            }
        },
        error: function (xhr) {
            console.error("❌ Error fetching item list:", xhr);
        }
    });
}
function collectPurchaseQuotationData() {
    const data = [];
    $tbody.find('tr').each(function () {
        const $tr = $(this);
        const itemCode = parseInt($tr.find('.icode').val()?.trim()) || 0;
        const itemName = $tr.find('.itemName option:selected').text().trim() || '';
        const delDate = $tr.find('input[type="date"]').val() || '';
        const formattedDelDate = delDate ? new Date(delDate).toISOString() : null;
        const quantity = parseFloat($tr.find('input[type="number"]').val()) || null;
        const remarks = $tr.find('input[type="text"]').last().val()?.trim() || '';
        const v_no = document.getElementById('TxtDispatchDocNo').value;
        const V_DATE = document.getElementById('DispatchDocDate').value;

        data.push({
            ItemCode: itemCode,
            ItemName: itemName,
            DeliveryDate: formattedDelDate,
            Qty: quantity,
            Remarks: remarks,
            v_no: v_no,
            V_DATE: V_DATE
        });
    });

    return data;
}
function SetFYDate(inputId, loginDate) {
    var $input = $('#' + inputId);
    var d = new Date(loginDate);
    var fyStartYear = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
    var minDate = fyStartYear + '-04-01';  
    var maxDate = loginDate;               
    $input.attr('min', minDate).attr('max', maxDate).val(maxDate);
    $input.on('change', function () {
        var selectedDate = new Date(this.value);
        var min = new Date(minDate);
        var max = new Date(maxDate);

        if (selectedDate < min || selectedDate > max) {
            toastr.info('Please select a date within the Financial Year and not greater than Login Date.');
            this.value = maxDate;
        }
    });
}

async function CheckOutherrised(partycode) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/CheckOutherrised',
            method: 'GET',
            data: { partycode: partycode }
        });

        console.log(res); 
        var indrate = 0;
        var imprate = 0;

        if (res.success == true) {
            toastr.warning(res.message);

            if (res.statetype != 'IMPORT') {
                var indrate = $('#numRate').val();
                var imprate = 0;
            }
            else {
                var txtrate = parseFloat($('#txtrate').val()) || 0;
                var exchangeRate = parseFloat($('#txtExchangeRate').val()) || 0;
                var indrate = Math.round((txtrate * exchangeRate * 0.001) * 1000) / 1000;
                var imprate = $('#numRate').val();
            }
            var exchangeRate = parseFloat($('#txtExchangeRate').val()) || 0;
            if (res.statetype === 'IMPORT' && exchangeRate === 0) {
                toastr.warning("Please enter exchange rate, in case of Import material. PO not generated.");           
                return;
            }
            setTimeout(() => {
                Swal.fire({
                    title: 'Question',
                    text: 'Do you want to create Purchase Order?',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Yes',
                    cancelButtonText: 'No'
                }).then((result) => {
                    if (result.isConfirmed) {
                        console.log("User selected YES");


                        createPurchaseOrder(indrate, imprate);


                    } else {
                        return;
                    }
                });
            }, 1200); 

        }
        else {
            toastr.warning(res.message);
            return;
        }        


    } catch (error) {
        console.error(error);
        toastr.error("Error");
    }
}

async function createPurchaseOrder(indrate, imprate) {
    try { 


        // ---------------- PARTY ----------------
        const PartyCode = parseInt($('#ddlPartyName').val()) || 0;

        const ITEM_CODE = parseInt($('#ddlItemName').val()) || 0;
        const ITEM_NAME = $('#ddlItemName option:selected').text();
        // ---------------- BILLING ----------------
        const BillAdd1 = $.trim($('#txtAddress1').val()) || "";
        const BillAdd2 = $.trim($('#txtAddress2').val()) || "";
        const BillAdd3 = $.trim($('#txtAddress3').val()) || "";
        const BillCity = parseInt($('#txtStation').val()) || 0;
        const BillPincode = $.trim($('#txtPin').val()) || "";
        const BillGst = $.trim($('#txtGST').val()) || "";

        // ---------------- SHIPPING ----------------
        const ShipFrom = parseInt($('#ddlShipFrom').val()) || 0;
        const ShipAdd1 = $.trim($('#txtShipAdd1').val()) || "";
        const ShipAdd2 = $.trim($('#txtShipAdd2').val()) || "";
        const ShipAdd3 = $.trim($('#txtShipAdd3').val()) || "";
        const ShipCity = parseInt($('#txtShipCity').val()) || 0;
        const ShipPincode = $.trim($('#txtShipPin').val()) || "";
        const ShipGst = $.trim($('#txtShipGST').val()) || "";

        // ---------------- SAUDA ----------------
        const SaudaNo = parseInt($('#txtDocNo').val()) || 0;
        const PlaceCode = parseInt($('#ddlPlace').val()) || 0;

        // ---------------- PRICING ----------------
        const PriceTypeRaw = $.trim($('#ddlFreightTerm').val()) || "";
        const PriceType =  PriceTypeRaw === "FOR" ? "F.O.R. - at our Plant" : PriceTypeRaw === "EX" ? "Ex - Work" : PriceTypeRaw;

        const Currency = $('#ddlRate').val() || "";
        const taxrate = $('#ddlTaxRate').val() || "";

        // ---------------- QTY ----------------
        const Nos = parseFloat($('#numTrucks').val()) || 0;
        const Qty = parseFloat($('#numWeight').val()) || 0;

        indrate = parseFloat(indrate) || 0;
        imprate = parseFloat(imprate) || 0;

        const PackAmt = 0;
        const DiscAmt = parseFloat($('#txtDiscount').val()) || 0;
        // ---------------- TAX ----------------
        const CgstAmt = parseFloat($('#txtCGST').val()) || 0;
        const SgstAmt = parseFloat($('#txtSGST').val()) || 0;
        const IgstAmt = parseFloat($('#txtIGST').val()) || 0;
        const TcsPer = parseFloat($('#txtTCSPer').val()) || 0;
        const TcsAmt = parseFloat($('#txtTCSAmt').val()) || 0;
        const OtherAmt = parseFloat($('#txtOtherAmt').val()) || 0;

        // ---------------- OTHER ----------------
        const DeliveryTerm = $.trim($('#txtDeliveryTerm').val()) || "";
        const PartyRef = $.trim($('#txtPartyRef').val()) || "";
        const PayTermCode = parseInt($('#ddlPaymentTerm').val()) || 0;
        const Remarks = $.trim($('#txtRemarks').val()) || "";


        // ---------------- DTO ----------------
        const Data = {
            indrate,
            imprate,
            PartyCode,
            BillAdd1,
            BillAdd2,
            BillAdd3,
            BillCity,
            BillPincode,
            BillGst,
            ShipFrom,
            ShipAdd1,
            ShipAdd2,
            ShipAdd3,
            ShipCity,
            ShipPincode,
            ShipGst,
            SaudaNo,
            PlaceCode,
            PriceType,
            Currency,
            Nos,
            Qty,    
            PackAmt,
            DiscAmt,
            CgstAmt,
            SgstAmt,
            IgstAmt,
            TcsPer,
            TcsAmt,
            OtherAmt,      
            DeliveryTerm,
            PartyRef,
            PayTermCode,
            Remarks,
            taxrate,
            ITEM_CODE,
            ITEM_NAME

        };

        console.log("Final PO Data:", Data);

        // ---------------- API CALL ----------------
        const response = await fetch("/PurchaseSauda/CreatePurchaseOrder", {
                method: "POST",
                headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(Data)
        });

        const result = await response.json();
        console.log("Result", result);



        if (result.validation == true) {
            toastr.warning(result.message);
            return;
        }

        if (result.status) {
            toastr.success(result.message || "Purchase Order created successfully");
        } else {
            toastr.error(result.message || "Failed to create Purchase Order");
        }

    } catch (error) {
        console.error(error);
        toastr.error("Error while creating Purchase Order");
    }
}

function enforceDecimal(el, maxInt = 10, maxDec = 2) {
    let val = el.value;

    // remove invalid characters (keep only digits and dot)
    val = val.replace(/[^0-9.]/g, '');

    // allow only one dot
    let dotIndex = val.indexOf('.');
    if (dotIndex !== -1) {
        val =
            val.substring(0, dotIndex + 1) +
            val.substring(dotIndex + 1).replace(/\./g, '');
    }

    let parts = val.split('.');

    let intPart = parts[0] || '';
    let decPart = parts[1] || '';

    // limit integer part
    if (intPart.length > maxInt) {
        intPart = intPart.substring(0, maxInt);
    }

    // limit decimal part
    if (decPart.length > maxDec) {
        decPart = decPart.substring(0, maxDec);
    }

    el.value = decPart ? `${intPart}.${decPart}` : intPart;
}

async function loadPurchaseHistory() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseSaudaList/GetDataByPurchaseHistory',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];               

            if (!data || data.length === 0) {
                toastr.info("Purchase History Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseHistory(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}
function renderPurchaseHistory(data) {

    const tbody = $('#tblshowpurchasehistoryList tbody');
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`
            <tr>
                <td colspan="4" class="text-center">No data found</td>
            </tr>
        `);
    } else {
        data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.doc_id ?? ''}</td>
                    <td>${item.vdate ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.party_name ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('showpurchasehistoryModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}
function TransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "SAUDA_PURCH";

    var v_no = $('#TxtCode').val();
    var v_type = "PAUD";

    var formula =
        "{SAUDA.COMP_CODE} = " + globalVars.CompCode +
        " and {SAUDA.YEAR_CODE} = " + globalVars.FYearCode +
        " and {SAUDA.BRANCH_CODE} = " + globalVars.BranchCode +
        " and {SAUDA.V_NO} = " + v_no +
        " and {SAUDA.V_TYPE} = '" + v_type + "'";

    // Prepare the payload for the API
    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",
            COMP_GST: globalVars.GST || "",
            COMP_PAN: globalVars.PAN || "",
            COMP_CIN: globalVars.COMP_CIN || "",
            COMP_EMAIL: globalVars.Email || "",
            comp_phone: globalVars.Phone || "",     
            RPTNAME: "PURCHASE CONTRACT/ORDER"
        }
    };

    // Timestamp for file name
    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');

    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport', // check port
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' }, // Important for PDF

        success: function (response) {
            // Convert response to a Blob
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            // Trigger download
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

async function paymentterm(partyCode, payTermCode) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/Paymentterm',
            type: 'GET',
            data: { partyCode: partyCode },
            dataType: 'json'
        });

        console.log("dd", res);

        if (String(res).trim() !== String(payTermCode ?? '').trim()) {
            toastr.info("Payment Term is not matching with Party Master, please check it");
        }

        return res;

    } catch (error) {
        console.error("Error fetching payment term:", error);
    }
}

async function GetTaxRate( taxrate) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/GetTaxRate',
            type: 'GET',
            data: { taxrate: taxrate },
            dataType: 'json'
        });

        console.log("dd", res);


        $('#numTaxRate').val(res);


        return res;

    } catch (error) {
        console.error("Error fetching payment term:", error);
    }
}

async function loadModificationdata() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseSaudaList/GetModificationData',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];

            if (!data || data.length === 0) {
                toastr.info("Purchase Modification Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseModification(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}
function renderPurchaseModification(data) {
    console.log("Rendering Purchase Modification Data:", data);
    const tbody = $('#modificationList tbody');
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`
            <tr>
                <td colspan="4" class="text-center">No data found</td>
            </tr>
        `);
    } else {
        data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.saudaNo ?? ''}</td>
                    <td>${item.party ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.rate ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.modifyDate ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('modificationModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}

async function GetFinalUser(v_no) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSauda/FinalUser',
            type: 'GET',
            data: { v_no: v_no },
            dataType: 'json'
        });

        console.log("dd", res);

        if (mode === "view") {
            $("#btn_ModificationOrder").show();
            $("#modification_count").show().text("Modification(" + (res.modificationcount || 0) + ")");
        }        

        if (res.finalUser && res.finalUser.toUpperCase() === "FINAL") {    

                $("#btn_ModificationOrder").show();
                $("#modification_count").show().text("Modification(" + (res.modificationcount || 0) + ")");

        } else {
            $("#btn_ModificationOrder").hide();
            $("#modification_count").hide().text("");
        }

    } catch (error) {
        console.error("Error fetching payment term:", error);
    }
}


async function CheackSendMail() {
    const v_no = parseInt($('#txtDocNo').val()) || 0;
    const res = await $.ajax({
        url: '/PurchaseSauda/CheackMail',
        type: 'GET',
        data: { v_no: v_no },
        dataType: 'json'
    });

    if (res.status == false) {
        toastr.warning(res.message);
        return;
    }

    Swal.fire({
        title: "Do you want to send mail?",
        icon: "question",
        showCancelButton: true,
        confirmButtonText: "Yes",
        cancelButtonText: "No"
    }).then((result) => {

        if (result.isConfirmed) {
            SendMail();
        }        
    });
}

async function SendMail() {
    try {

        let PartyCode = $('#ddlPartyName').val();
        const vno = parseInt($('#txtDocNo').val()) || 0;

        // 🔥 STEP 1: GET REPORT FILE
        const report = await GetTransitReportFile();

        // STEP 2: SEND TO CONTROLLER
        let formData = new FormData();
        formData.append("PartyCode", PartyCode);
        formData.append("vno", vno);

        formData.append("file", report.file, report.fileName);

        const res = await $.ajax({
            url: '/PurchaseSauda/SendMail',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        });

        console.log("Mail response:", res);
        return res;

    } catch (error) {
        console.error("Error:", error);
    }
}





function GetTransitReportFile() {

    return new Promise((resolve, reject) => {

        if (!rowId) {
            showToast(`Please save the data before printing the report.`, { type: "info" });
            reject("No rowId");
            return;
        }

        var reportName = "SAUDA_PURCH";

        var v_no = $('#TxtCode').val();
        var v_type = "PAUD";

        var formula =
            "{SAUDA.COMP_CODE} = " + globalVars.CompCode +
            " and {SAUDA.YEAR_CODE} = " + globalVars.FYearCode +
            " and {SAUDA.BRANCH_CODE} = " + globalVars.BranchCode +
            " and {SAUDA.V_NO} = " + v_no +
            " and {SAUDA.V_TYPE} = '" + v_type + "'";

        var payload = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: database,
            Parameters: {
                comp_name: globalVars.CompanyName || "",
                comp_add1: globalVars.Address1 || "",
                comp_phone: globalVars.Phone || "",
                RPTNAME: "PURCHASE CONTRACT/ORDER"
            }
        };

        $.ajax({
            url: 'http://localhost:24085/Report/PendingQCReport',
            type: 'POST',
            data: JSON.stringify(payload),
            contentType: "application/json",
            xhrFields: { responseType: 'blob' },

            success: function (response) {

                // 🔥 return file instead of downloading
                const file = new Blob([response], { type: 'application/pdf' });

                const v_no = $('#TxtCode').val();
                const now = new Date();

                const timestamp =
                    String(now.getDate()).padStart(2, '0') +
                    String(now.getMonth() + 1).padStart(2, '0') +
                    String(now.getFullYear()).slice(-2) + "_" +
                    String(now.getHours()).padStart(2, '0') +
                    String(now.getMinutes()).padStart(2, '0') +
                    String(now.getSeconds()).padStart(2, '0');

                const fileName = `SAUDA_PURCH_${v_no}_${timestamp}.pdf`;

                resolve({ file, fileName });
            },

            error: function (xhr) {
                reject(xhr);
            }
        });
    });
}


