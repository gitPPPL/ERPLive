function collectPurchaseDocumentsData() {
    const documents = [];
    if (Array.isArray(globalAttachments)) {
        globalAttachments.forEach(att => {
            const fileName = att.FileName ?? att.fileName;
            const filePath = att.FilePath ?? att.filePath;

            if (!fileName) return;

            documents.push({
                FileName: fileName,
                FilePath: filePath,
                IsNew: false
            });
        });
    }

    if (Array.isArray(selectedFiles)) {

        selectedFiles.forEach(file => {

            if (!file?.name) return;

            documents.push({
                FileName: file.name,
                FilePath: null,
                FileObject: file,
                IsNew: true
            });
        });
    }

    return documents;
}
function addAttachmentRow(data = {}) {

    const index = data.index ?? attachmentIndex++;

    const row = `
        <tr class="attachment-row" data-index="${index}">

            <td style="display:none;">
                <input type="hidden"
                       class="attachment-code"
                       value="${index}" />
            </td>

            <td>
                <input type="text"
                       class="form-control file-name"
                       id="fileName_${index}"
                       value="${data.fileName || ''}"
                       placeholder="Enter file name"
                       disabled />
            </td>

            <td>
                <input type="file"
                       class="form-control file-upload"
                       id="fileInput_${index}"
                       data-index="${index}" />
            </td>

            <td>
                <i class="fa fa-plus btn-add-action"
                   title="Add Row"
                   data-index="${index}"
                   style="cursor:pointer; margin-right:8px;"></i>

                <i class="fa fa-edit btn-edit-action"
                   title="Edit Row"
                   data-index="${index}"
                   style="cursor:pointer; margin-right:8px;"></i>

                <i class="fa fa-trash btn-delete-action"
                   title="Delete Row"
                   data-index="${index}"
                   style="cursor:pointer;"></i>
            </td>

        </tr>
    `;

    $attachmentTbody.append(row);
}

async function LoadFormByID(id) {
    try {
        const res = await $.ajax({
            url: '/PurchaseSaudaList/GetDataByCode',
            method: 'GET',
            data: { code: id }
        });

        if (!res.success) {
            toastr.error(res.message || "Failed to load data.");
            return;
        }

        const header = res.data.header;

        // =========================
        // HEADER FIELDS
        // =========================
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
        $('#txtStation').val(header.citY_CODE || 0);

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

        $('#txtCountry').val(header.country || '');
        $('#txtContactNo').val(header.phone || '');
        $('#txtDeliveryFrom').val(header.partY_TO || '');

        $('#numFreightRate').val(header.frT_RATE || '');
        $('#NumOfferRate').val(header.offerRate || '');

        // =========================
        // ATTACHMENTS (INLINE RENDER)
        // =========================
        globalAttachments = res.data.attachment || [];
        selectedFiles = [];

        const attachBody = $("#fileList");
        attachBody.empty();

        if (!globalAttachments || globalAttachments.length === 0) {
            attachBody.html(`
                <div class="erp-empty-state text-center text-muted">
                    No attachments found.
                </div>
            `);
            return;
        }

        globalAttachments.forEach((att, idx) => {

            const fileName = att.FileName ?? att.fileName ?? `File_${idx + 1}`;
            let filePath = att.FilePath ?? att.filePath ?? "";

            // FIX: safe URL
            if (filePath && !filePath.startsWith("http") && !filePath.startsWith("/")) {
                filePath = "/" + filePath;
            }

            const ext = (fileName.split('.').pop() || "").toLowerCase();

            let preview = "";

            if (["png", "jpg", "jpeg", "gif", "webp", "bmp"].includes(ext)) {
                preview = `
                    <img src="${filePath}"
                         style="width:40px;height:40px;object-fit:cover;border-radius:5px;">
                `;
            }
            else if (ext === "pdf") {
                preview = `<i class="fa fa-file-pdf-o" style="color:red;font-size:20px;"></i>`;
            }
            else {
                preview = `<i class="fa fa-file-o" style="font-size:20px;"></i>`;
            }

            attachBody.append(`
                <div class="file-item erp-file-row"
                     data-id="${att.Id || idx}"
                     data-type="existing">

                    <div class="erp-file-preview">
                        ${preview}
                    </div>

                    <div class="erp-file-info">
                        <div class="erp-file-name">${fileName}</div>
                        <div class="erp-file-type">${ext.toUpperCase() || 'FILE'}</div>
                    </div>

                    <div class="erp-file-actions">
                        <a href="${filePath}" target="_blank" class="erp-view-btn">
                            View
                        </a>

                        <button type="button"
                                class="erp-delete-db-btn"
                                data-id="${att.Id || idx}">
                            Delete
                        </button>
                    </div>

                </div>
            `);
        });

    } catch (err) {
        console.error("Failed to load data", err);
        toastr.error("Something went wrong while loading the form.");
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

async function CheckOutherrised(partycode) {
    try {

        if (!rowId) {
            showToast(`Please save the data before Create Purchase Order.`, { type: "info" });
            return;
        }

        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;


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
function isDuplicateFile(file) {

    const fileName = file.name;

    // 1. check in newly selected files
    const inNew = selectedFiles.some(f =>
        f.name === file.name &&
        f.size === file.size &&
        f.lastModified === file.lastModified
    );

    // 2. check in existing DB attachments
    const inExisting = globalAttachments.some(att => {
        const name = att.FileName ?? att.fileName;
        return name.toLowerCase() === file.name.toLowerCase();
    });

    if (inNew || inExisting) {
        toastr.warning(`File "${fileName}" already exists in attachment list.`);
        return true;
    }

    return false;
}
function renderFileList() {

    const attachBody = $("#fileList");
    attachBody.empty();

    // ===== EXISTING ATTACHMENTS =====
    if (globalAttachments && globalAttachments.length > 0) {

        globalAttachments.forEach((att, index) => {

            const fileName = att.FileName ?? att.fileName;
            const filePath = att.FilePath ?? att.filePath;

            const extension = fileName.split('.').pop().toLowerCase();

            let previewHtml = "";

            if (["png", "jpg", "jpeg", "gif", "webp", "bmp"].includes(extension)) {
                previewHtml = `<img src="${filePath}" class="erp-file-thumb">`;
            }
            else if (extension === "pdf") {
                previewHtml = `<i class="fa fa-file-pdf-o erp-file-icon text-danger"></i>`;
            }
            else {
                previewHtml = `<i class="fa fa-file-o erp-file-icon"></i>`;
            }

            attachBody.append(`
                <div class="file-item erp-file-row">

                    <!-- LEFT: ICON / IMAGE -->
                    <div class="erp-file-preview">
                        ${previewHtml}
                    </div>

                    <!-- MIDDLE: NAME -->
                    <div class="erp-file-info">
                        <div class="erp-file-name">${fileName}</div>
                    </div>

                    <!-- RIGHT: ACTIONS -->
                    <div class="erp-file-actions">
                        <a href="${filePath}" target="_blank" class="erp-view-btn">
                            View
                        </a>

                        <button type="button"
                                class="erp-delete-db-btn"
                                data-index="${index}">
                            Delete
                        </button>
                    </div>

                </div>
            `);
        });
    }

    // ===== NEWLY SELECTED FILES =====
    selectedFiles.forEach((file, index) => {

        const extension = file.name.split('.').pop().toLowerCase();
        const fileUrl = URL.createObjectURL(file);

        let previewHtml = "";

        if (["png", "jpg", "jpeg", "gif", "webp", "bmp"].includes(extension)) {
            previewHtml = `<img src="${fileUrl}" class="erp-file-thumb">`;
        }
        else if (extension === "pdf") {
            previewHtml = `<i class="fa fa-file-pdf-o erp-file-icon text-danger"></i>`;
        }
        else {
            previewHtml = `<i class="fa fa-file-o erp-file-icon"></i>`;
        }

        attachBody.append(`
            <div class="file-item erp-file-row">

                <!-- LEFT: ICON / IMAGE -->
                <div class="erp-file-preview">
                    ${previewHtml}
                </div>

                <!-- MIDDLE: NAME -->
                <div class="erp-file-info">
                    <div class="erp-file-name">${file.name}</div>
                </div>

                <!-- RIGHT: ACTIONS -->
                <div class="erp-file-actions">
                    <a href="${fileUrl}" target="_blank" class="erp-view-btn">
                        View
                    </a>

                    <button type="button"
                            class="erp-delete-file-btn"
                            data-index="${index}">
                        Delete
                    </button>
                </div>

            </div>
        `);
    });

    if (globalAttachments.length === 0 && selectedFiles.length === 0) {
        attachBody.html(`
            <div class="erp-empty-state text-center text-muted">
                No attachments found.
            </div>
        `);
    }
}