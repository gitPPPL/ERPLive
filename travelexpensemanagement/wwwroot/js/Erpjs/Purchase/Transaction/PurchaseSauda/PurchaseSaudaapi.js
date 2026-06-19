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
            DDLCityMast()
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

    const fileInput = document.getElementById("fileInput");

    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        toastr.error("Please upload at least one file.");
        return [];
    }

    Array.from(fileInput.files).forEach((file) => {
        if (!file.name) {
            errorMessages.push("Invalid file detected.");
            return;
        }

        documents.push({
            FileName: file.name,
            FilePath: "/attachments/pan/" + file.name
        });
    });

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

            $('#TxtCode').val(header.doC_ID || '');
            $('#txtDocNo').val(header.v_NO || '');
            $('#TxtDispatchDocNo').val(header.v_NO || '');
            $('#dtDocDate').val(formatDate(header.v_DATE));
            $('#DispatchDocDate').val(formatDate(header.v_DATE));
            $('#ddlShipFrom').val(header.shiP_CODE || '');
            $('#ddlSupplyFrom').val(header.shiP_TYPE || '');
            $('#ddlPartyName').val(header.partY_CODE || '');
            $('#txtAddress1').val(header.adD1 || '');
            $('#txtAddress2').val(header.adD2 || '');
            $('#txtAddress3').val(header.adD3 || '');
            $('#txtStation').val( header.citY_CODE || 0);
            $('#ddlItemName').val(header.iteM_CODE || '');
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
            $('#ddlDocStatus').val(header.status === 1 ? 'open' : 'closed');
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

                attachments.forEach((att, idx) => {

                    const fileName = att.fileName || `File_${idx + 1}`;
                    const filePath = att.filePath || '';

                    const extension = fileName.split('.').pop()?.toLowerCase() || '';

                    let filePreview = '';

                    // IMAGE PREVIEW
                    if (filePath && ['png', 'jpg', 'jpeg', 'gif', 'webp'].includes(extension)) {
                        filePreview = `
                <img src="${filePath}" 
                     alt="${fileName}" 
                     class="erp-file-thumbnail" />
            `;
                    }

                    // PDF
                    else if (extension === 'pdf') {
                        filePreview = `
                <i class="fa fa-file-pdf-o" style="font-size:40px;color:#e53935;"></i>
            `;
                    }

                    // OTHER FILES
                    else {
                        filePreview = `
                <i class="fa fa-file-o" style="font-size:40px;color:#555;"></i>
            `;
                    }

                    const card = `
            <div class="erp-file-row" data-filename="${fileName}">

                <div class="erp-file-preview">
                    ${filePreview}
                </div>

                <div class="erp-file-info">
                    <div class="erp-file-name" title="${fileName}">
                        ${fileName}
                    </div>

                    <div class="erp-file-type">
                        ${extension.toUpperCase() || 'FILE'}
                    </div>
                </div>

                <div class="erp-file-actions">

                    <!-- VIEW BUTTON (NEW) -->
                    <a href="${filePath}" target="_blank" class="erp-view-btn">
                        View
                    </a>

                    <!-- DELETE BUTTON -->
                    <button type="button" class="erp-delete-btn btn-delete-attachment">
                        <i class="fa fa-trash"></i>
                    </button>

                </div>

            </div>
        `;

                    attachBody.append(card);
                });
            }



            $dispatchtableTbody.empty();
            if (DispatchDetails.length === 0) {
                addPurchaseQuotationRow();
            } else {
                for (const Dispatch of DispatchDetails) {
                    addPurchaseQuotationRow({
                        icode: Dispatch.itemCode || '',
                        itemName: Dispatch.itemName || '',
                        delDate: Dispatch.deliveryDate ? formatDate(Dispatch.deliveryDate) : '',
                        quantity: Dispatch.qty || '',
                        remarks: Dispatch.remarks || ''
                    });
                }
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
        $('#ddlShipFrom').val(data?.supplier?.code || '');
        $('#ddlFreightTerm').val(data?.sauda?.frtTerm || '');
        $('#txtDeliveryTerm').val(data?.sauda?.delTerm || '');
        $('#ddlItemName').val(data?.sauda?.itemCode || '');
        $('#ddlItemType').val(data?.sauda?.itemType || '');

    } catch (error) {
        console.error('Failed to fetch party data:', error);
    }
}

async function DDLPartyMast() {
    try {
        const res = await fetch('/PurchaseSauda/DDLPartyMast');
        const data = await res.json();
        const ddl = $('#ddlPartyName');
        ddl.empty().append('<option value="">-- Select Party Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Party:", error);
    }
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

async function DDLShipFrom() {
    try {
        const res = await fetch('/PurchaseSauda/DDLShipFrom');
        const data = await res.json();
        const ddl = $('#ddlShipFrom');
        ddl.empty().append('<option value="">-- Select Ship From --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Ship From:", error);
    }
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
    try {
        const res = await fetch('/PurchaseSauda/DDLItemMaster');
        const data = await res.json();
        const ddl = $('#ddlItemName');
        ddl.empty().append('<option value="">-- Select Item Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item Master:", error);
    }
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
                     oninput="if (this.value.length > 16) this.value = this.value.slice(0, 16)" />
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

    // Determine the financial year start year
    var fyStartYear = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;

    var minDate = fyStartYear + '-04-01';  // FY start
    var maxDate = loginDate;               // Cannot select beyond login date

    // Set attributes and default value
    $input.attr('min', minDate)
        .attr('max', maxDate)
        .val(maxDate);

    // Validate user input
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

        // ---------------- MASTER IDS ----------------
        const CompCode = parseInt($('#ddlCompany').val()) || 0;
        const BranchCode = parseInt($('#ddlBranch').val()) || 0;
        const YearCode = parseInt($('#ddlYear').val()) || 0;
        const VType = "RORD";
        const VNo = parseInt($('#txtVNo').val()) || 0;
        const DocId = $.trim($('#txtDocId').val()) || "";

        // ---------------- PARTY ----------------
        const PartyCode = parseInt($('#ddlPartyName').val()) || 0;

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
        const PriceType =
            PriceTypeRaw === "FOR"
                ? "F.O.R. - at our Plant"
                : PriceTypeRaw === "EX"
                    ? "Ex - Work"
                    : PriceTypeRaw;

        const Currency = $('#ddlRate').val() || "";

        // ---------------- QTY ----------------
        const Nos = parseFloat($('#numTrucks').val()) || 0;
        const Qty = parseFloat($('#numWeight').val()) || 0;

        // ✅ USING FUNCTION PARAMETER (NOT DOM RE-READ)
        indrate = parseFloat(indrate) || 0;
        imprate = parseFloat(imprate) || 0;

        const basicAmount = Qty * indrate;

        const PackAmt = 0;
        const DiscAmt = parseFloat($('#txtDiscount').val()) || 0;

        // ---------------- TAX ----------------
        const CgstAmt = parseFloat($('#txtCGST').val()) || 0;
        const SgstAmt = parseFloat($('#txtSGST').val()) || 0;
        const IgstAmt = parseFloat($('#txtIGST').val()) || 0;

        const TcsPer = parseFloat($('#txtTCSPer').val()) || 0;
        const TcsAmt = parseFloat($('#txtTCSAmt').val()) || 0;

        const OtherAmt = parseFloat($('#txtOtherAmt').val()) || 0;

        // ---------------- NET AMOUNT ----------------
        const NetAmt =
            basicAmount +
            PackAmt -
            DiscAmt +
            CgstAmt +
            SgstAmt +
            IgstAmt +
            TcsAmt +
            OtherAmt;

        // ---------------- OTHER ----------------
        const DeliveryTerm = $.trim($('#txtDeliveryTerm').val()) || "";
        const PartyRef = $.trim($('#txtPartyRef').val()) || "";
        const PayTermCode = parseInt($('#ddlPaymentTerm').val()) || 0;
        const Remarks = $.trim($('#txtRemarks').val()) || "";

        // ---------------- DTO ----------------
        const Data = {
            CompCode,
            BranchCode,
            YearCode,
            VType,
            VNo,
            DocId,

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
            Amount: basicAmount,
            PackAmt,
            DiscAmt,
            CgstAmt,
            SgstAmt,
            IgstAmt,
            TcsPer,
            TcsAmt,
            OtherAmt,
            NetAmt,

            DeliveryTerm,
            PartyRef,
            PayTermCode,
            Remarks,

            CDiscAmt: 0
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