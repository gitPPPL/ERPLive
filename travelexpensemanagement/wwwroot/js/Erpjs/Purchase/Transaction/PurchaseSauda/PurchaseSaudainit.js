let $tbody;
let rowIndex = 0;
let itemMap = {};
let reverseItemMap = {};
const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = (mode === 'view');
var globalVars = window.globalVariables || {};
var database = window.database || "";
let PubUserLevel = globalVars.UserLevel;
let CompCode = globalVars.CompCode;
let LoginDate = globalVars.LoginDate;
let globalAttachments = [];
const $attachmentTbody = $('#tblAttachmentPS tbody');
const $dispatchtableTbody = $('#tblPurchaseQuotationList tbody');
let attachmentIndex = Date.now();
const browseBtn = document.getElementById("browseBtn");
const fileInput = document.getElementById("fileInput");
const dropZone = document.getElementById("dropZone");

let selectedFiles = [];

$(document).ready(async function () {
    $tbody = $('#tblPurchaseQuotationList tbody');
    $tbody.empty();

    Getitem().then(() => {
        addPurchaseQuotationRow();
    });

    SetFYDate('dtDocDate', LoginDate);

    if (!rowId) {
        await GetVNo();
        document.getElementById('DispatchDocDate').valueAsDate = new Date();
    }
    else {     

        if (mode === "view") {
            setFormReadOnly();
            $('#PurchaseRequestForm').after( '<span class="badge bg-secondary ms-2">Read‑Only Mode</span>' );
        }
    }

    LoadDropDown() .then(() => {
            if (rowId) {
                return LoadFormByID(rowId).then(() => {

                    checkApprovalStatus("paud", rowId, 'ORDER1');


                   const v_no = $('#txtDocNo').val(); 

                    GetFinalUser(v_no);


                    const selectedValue = $('#ddlSupplyFrom').val();

                    if (selectedValue === "IMPORT") {
                        $('#GradeID').html('Grade <span class="required">*</span>');
                    } else {
                        $('#GradeID').html('Grade');
                    }
                    const DtSBLCDue = $("#DtSBLCDue").val();
                    if (DtSBLCDue) {
                        $("#chkSBLCDue").prop("checked", true);
                    } else {
                        $("#chkSBLCDue").prop("checked", false);
                    }
                    const DtLCDue = $("#DtLCDue").val();
                    if (DtLCDue) {
                        $("#chkDtLCDue").prop("checked", true);
                    } else {
                        $("#chkDtLCDue").prop("checked", false);
                    }

                });
            }
        })
        .catch(error => {
            console.error("An error occurred:", error);
        });


    $('#ddlPartyName').on('change', function () {

        if (!rowId) {
            const selectedValue = this.value;
            fetchDDlParty(selectedValue);
            var CountryName = $('#txtCountry').val();
            if (CountryName != 'INDIA') {
                $('#ddlSupplyFrom').val('LOCAL');
            }
        }
              
    });

    $('#ddlSupplyFrom').on('change', function () {
        const selectedValue = this.value;
        if (selectedValue === "IMPORT") {
            $('#GradeID').html('Grade <span class="required">*</span>');
        } else {
            $('#GradeID').html('Grade');
        }
    });

    $('#numRate, #numDiscount,  #ddlTaxRate').on('change', recalculateNetRate);

    $(document).on('click', '.btn-delete-action', function () {
        if (confirm('Are you sure you want to delete this attachment?')) {
            $(this).closest('tr').remove();
        }
    });

    $(document).on('click', '.btn-edit-action', function () {
        const $btn = $(this);
        const $row = $btn.closest('tr');
        const $input = $row.find('input[type="text"]');

        $btn.toggleClass('editing');
        const isEditing = $btn.hasClass('editing');

        $input.prop('disabled', !isEditing);
        $input.focus();
        $input.css('background-color', isEditing ? '#f0f8ff' : 'white');
    });

    $(document).on('change', 'input[type="file"]', function () {
        const fileInput = this;
        const fileName = fileInput.files.length ? fileInput.files[0].name : '';
        const $row = $(this).closest('tr');
        $row.find('input[type="text"]').val(fileName);
    });

    $("#btn-save").click(async function (e) {
        e.preventDefault();

        if (!validateRequiredField('#dtDocDate', 'Please select a Voucher Date.')) return;
        if (!validateRequiredField('#ddlSupplyFrom', 'Please select a Supply From.')) return;
        if (!validateRequiredField('#ddlPartyName', 'Please select a Party Name.')) return;
        if (!validateRequiredField('#ddlItemName', 'Please select a Item Name.')) return;

        if (!validateRequiredField('#ddlFreightTerm', 'Please Select Freight Term')) return;
        if (!validateRequiredField('#numWeight', 'Please Fill Weight (Kgs) .')) return;
        if (!validateRequiredField('#numRate', 'Please Fill Rate .')) return;
        if (!validateRequiredField('#ddlPurchaseThrough', 'Please select a Purchase Through.')) return;
        if (!validateRequiredField('#ddlTaxRate', 'Please Select Tax Rate')) return;
        if (!validateRequiredField('#ddlPaymentTerm', 'Please Select Payment Term')) return;

        const DOC_ID = $.trim($('#TxtCode').val()) || null;

        const V_NO = $.trim($('#txtDocNo').val()) ? parseFloat($.trim($('#txtDocNo').val())) : null;
  
        const V_DATE = formatDate($("#dtDocDate").val()) || null;
        const SHIP_CODE = parseInt($('#ddlShipFrom').val()) || 0;
        const Delivery_From = $('#txtDeliveryFrom').val() || "";
        const SHIP_TYPE = $.trim($('#ddlSupplyFrom').val()) || "";
        const PARTY_CODE = parseInt($('#ddlPartyName').val()) || 0;
        const ADD1 = $.trim($('#txtAddress1').val()) || "";
        const ADD2 = $.trim($('#txtAddress2').val()) || "";
        const ADD3 = $.trim($('#txtAddress3').val()) || "";
        const CITY_CODE = $('#txtStation').val() || 0;

        let CityName = '';

        if (CITY_CODE) {
             CityName = $.trim($('#txtStation option:selected').text()) || "";
        }    

        const PHONE = $.trim($('#txtContactNo').val()) || "";
        const ITEM_CODE = parseInt($('#ddlItemName').val()) || 0;
        const TRUCK_NO = parseInt($('#numTrucks').val()) || 0;
        const EXRATE = parseFloat($('#txtExRate').val()) || 0;
        const DISC_PER = parseFloat($('#numDiscount').val()) || 0;
        const REMARK = $.trim($('#txtRemarks').val()) || "";
        const PINO = $.trim($('#NumPINO').val()) || "";
        const PIDATE = formatDate($("#DtPIDate").val()) || null;
        const OFFERNO = $.trim($('#NumOfferNo').val()) || "";
        const BROKER_RATE = parseFloat($('#NumBrokerage').val()) || 0;
        const BROKER = parseInt($('#ddlBrokerName').val()) || 0;
        const PACK_TYPE = $.trim($('#ddlPackingType').val()) || "";
        const DISPATCH_FROM = parseInt($('#ddlDispatchFrom').val()) || 0;
        const PAYMENT_STATUS = $.trim($('#ddlPaymentType').val()) || "";
        const CURRENCY = $.trim($('#ddlRate').val()) || "";

     
        let SBLC_DUEDATE = null;
        if ($("#chkSBLCDue").is(":checked")) {
            const dateVal = $("#DtSBLCDue").val();
            SBLC_DUEDATE = dateVal ? dateVal : null;
        } else {
            SBLC_DUEDATE = null;
        }

        const GRADE = $.trim($('#ddlGrade').val()) || "";
        const ITEM_REMARKS = $.trim($('#TxtItemRemarks').val()) || "";
        const WASTE_PER = parseInt($('#numWaste').val()) || 0;
        const RATE = parseFloat($('#numRate').val()) || 0;
        const TAX_CODE = parseFloat($('#ddlTaxRate').val()) || 0;
        const TAX_RATE = parseFloat($('#numTaxRate').val()) || 0;
        const ONLY_NATURAL = $('#chkNatural').prop('checked') ? 1 : 0;
        const ITEM_TYPE = $.trim($('#ddlItemType').val()) || "";
        const PartyName = $.trim($('#ddlPartyName option:selected').text()) || "";
        const QTY = parseInt($('#numWeight').val()) || 0;
        const FRT_TERM = $.trim($('#ddlFreightTerm option:selected').text()) || "";
        const NET_RATE = parseFloat($('#numNetRate').val()) || 0;
        const FRT_RATE = parseFloat($('#numFreightRate').val()) || 0;
        const OfferRate = parseFloat($('#NumOfferRate').val()) || 0;
        const PAYTERM_CODE = parseInt($('#ddlPaymentTerm').val()) || 0;
        const STATUS = parseInt($('#ddlDocStatus').val()) || 0;
        const DEL_TERM = $.trim($('#txtDeliveryTerm').val()) || "";
            let LC_DUEDATE = null;
        if ($("#chkDtLCDue").is(":checked")) {
            const dateVal = $("#DtLCDue").val();
            LC_DUEDATE = dateVal ? dateVal : null;
        } else {
            LC_DUEDATE = null;
        }
        const DEAL_THROUGH = parseInt($('#ddlPurchaseThrough').val()) || 0;
        const ACTION = !DOC_ID ? 'INSERT' : 'UPDATE';
        const TAX_TERM = $.trim($('#ddlPackingType').val()) || "";


        if ($("#chkSBLCDue").is(":checked")) {
            if (!validateRequiredField('#DtSBLCDue', 'Please Select SBLC Due Date')) return;
        }

        if ($("#chkDtLCDue").is(":checked")) {
            if (!validateRequiredField('#DtLCDue', 'Please Select LC Due Dt')) return;
        }

        const checkdate = await checkValidDate();
        if (!checkdate) {
            return;
        }

        const Header = {
            DOC_ID: DOC_ID,
            V_NO: V_NO,
            V_DATE: V_DATE,
            SHIP_CODE: SHIP_CODE,
            SHIP_TYPE: SHIP_TYPE,
            PARTY_CODE: PARTY_CODE,
            ADD1: ADD1,
            ADD2: ADD2,
            ADD3: ADD3,
            CITY_CODE: CITY_CODE,
            CityName: CityName,
            PHONE: PHONE,
            ITEM_CODE: ITEM_CODE,
            TRUCK_NO: TRUCK_NO,
            EXRATE: EXRATE,
            DISC_PER: DISC_PER,
            REMARK: REMARK,
            PINO: PINO,
            PIDATE: PIDATE,
            OFFERNO: OFFERNO,
            BROKER_RATE: BROKER_RATE,
            BROKER: BROKER,
            PACK_TYPE: PACK_TYPE,
            DISPATCH_FROM: DISPATCH_FROM,
            PAYMENT_STATUS: PAYMENT_STATUS,
            CURRENCY: CURRENCY,
            SBLC_DUEDATE: SBLC_DUEDATE,
            GRADE: GRADE,
            ITEM_REMARKS: ITEM_REMARKS,
            WASTE_PER: WASTE_PER,
            RATE: RATE,
            TAX_CODE: TAX_CODE,
            ONLY_NATURAL: ONLY_NATURAL,
            ITEM_TYPE: ITEM_TYPE,
            QTY: QTY,
            FRT_TERM: FRT_TERM,
            NET_RATE: NET_RATE,
            PAYTERM_CODE: PAYTERM_CODE,
            DEL_TERM: DEL_TERM,
            STATUS: STATUS,
            LC_DUEDATE: LC_DUEDATE,
            DEAL_THROUGH: DEAL_THROUGH,
            ACTION: ACTION,
            TAX_TERM: TAX_TERM,
            TAX_RATE: TAX_RATE,
            PartyName: PartyName,
            Delivery_From: Delivery_From,
            FRT_RATE: FRT_RATE,
            OfferRate: OfferRate
        };

        const documentData = collectPurchaseDocumentsData();

        console.log("documentData", documentData);





        if (SHIP_TYPE === "IMPORT") {
            if (!GRADE) {
                toastr.warning("Please Select Grade.");
                return;
            }

            if ((!documentData || documentData.length === 0) &&
                (!globalAttachments || globalAttachments.length === 0)) {

                toastr.warning("Please Fill Atleast One Row Document Details.");
                return;
            }

            const hasValidDocument =
                documentData?.some(x => x.FileName && x.FileName.trim() !== "") || false;

            const hasValidAttachment =
                globalAttachments?.some(x => x.FileName && x.FileName.trim() !== "") || false;

            if (!hasValidDocument && !hasValidAttachment) {
                toastr.warning("Please Fill Atleast One Row Document Details.");
                return;
            }

        }

        const payload = {
            Header: Header,
            Document: documentData
        };

        $("#btn-save").prop("disabled", true);
        $.ajax({
            url: '/PurchaseSauda/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {

                console.log("response", response);


                if (response.success) {
                    toastr.success("Saved successfully!");


                    setTimeout(function () {
                        window.location.href = '/PurchaseSauda/Index?id=' + V_NO + '&VType=' + encodeURIComponent('Paud') +
                            '&mode=view';
                            //+
                            //'&mode=view';
                    }, 3000);

                } else {
                    toastr.error(response.message || "Save failed.");
                }
            },
            error: function (xhr, status, error) {
                let errorMessage = "Something went wrong.";
                if (xhr.status === 400) {
                    errorMessage = "Bad Request: " + xhr.responseText;
                } else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                } else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }
                console.error("Error: ", errorMessage);
                toastr.error(errorMessage);
            },
            complete: function () {

                $("#btn-save").prop("disabled", false);
            }
        });
    });

    $("#DispatchSave").click(function (e) {
        e.preventDefault();
        const rowsData = collectPurchaseQuotationData();
        const payload = {
            DispatchDelivery: rowsData
        };

        if (
            !payload.DispatchDelivery ||
            (Array.isArray(payload.DispatchDelivery) && payload.DispatchDelivery.length === 0) ||
            (typeof payload.DispatchDelivery === 'object' && !Array.isArray(payload.DispatchDelivery) && Object.keys(payload.DispatchDelivery).length === 0)
        ) {
            toastr.warning('Please Fill Atleast One Row in Dispatch Delivery Planning.');
            return;
        }


        $("#DispatchSave").prop("disabled", true);
        $.ajax({
            url: '/PurchaseSauda/SaveDispatchDetails',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    toastr.success("Saved successfully!");
                    var deliveryPlanModal = document.getElementById('deliveryPlanModal');
                    var modal = bootstrap.Modal.getInstance(deliveryPlanModal);
                    modal.hide();
                } else {
                    toastr.error(response.message || "Save failed.");
                }
            },
            error: function (xhr, status, error) {
                let errorMessage = "Something went wrong.";
                if (xhr.status === 400) {
                    errorMessage = "Bad Request: " + xhr.responseText;
                } else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                } else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }

                console.error("Error: ", errorMessage);
                toastr.error(errorMessage);
            },
            complete: function () {
                $("#DispatchSave").prop("disabled", false);
            }
        });
    });

    $(document).on('click', '.btn-add-actions', function () {
        const lastRow = $tbody.find('tr').last();
        if (lastRow.length) {
            const lastItemName = lastRow.find('select.itemName').val();
            if (!lastItemName) {
                toastr.warning("Please Select Data in  Previous Row .");
                return;
            }
        }
        addPurchaseQuotationRow();
    });

    $(document).on('click', '.btn-delete-actions', function () {
        const $row = $(this).closest('tr');

        $row.remove();

    });

    $(document).on('change', '.itemName', function () {
        const $select = $(this);
        const selectedCode = $select.val();
        const $row = $select.closest('tr');
        $row.find('.icode').val(selectedCode);
    });

    $('#btn_CreatePurchaseOrder').on('click', function () {  
        var partycode = $('#ddlPartyName').val();
        CheckOutherrised(partycode);
    });

    $('#btn_ShowPuchasehistory').on('click', function () {
        loadPurchaseHistory();
    });

    $('#ddlPaymentTerm').on('change', function () {
   
        var PAYTERM_CODE  = $('#ddlPaymentTerm').val();
        var partyCode = $('#ddlPartyName').val();
        if (partyCode) {
            paymentterm(partyCode, PAYTERM_CODE );
        }
    });

    $('#ddlTaxRate').on('change', function () {
        var taxrate = $('#ddlTaxRate').val();
        if (taxrate) {
            GetTaxRate(taxrate);
        }
    });

    $('#btn_ModificationOrder').on('click', function () {
        loadModificationdata();
    });


    $('#btnMail').on('click', function () {
        CheackSendMail();
    });

   // Attachment code

    browseBtn.addEventListener("click", function () {
        fileInput.click();
    });

    fileInput.addEventListener("change", function () {

        Array.from(this.files).forEach(file => {

            if (!isDuplicateFile(file)) {
                selectedFiles.push(file);
            }
        });

        renderFileList();
        this.value = "";
    });

    $(document).on("click", ".erp-delete-file-btn", function () {

        const index = $(this).data("index");

        selectedFiles.splice(index, 1);

        renderFileList();
    });

    $(document).on("click", ".erp-delete-db-btn", function () {

        const index = $(this).data("index");

        globalAttachments.splice(index, 1);

        renderFileList();
    });

    dropZone.addEventListener("dragover", function (e) {
        e.preventDefault();
        dropZone.classList.add("dragover");
    });

    dropZone.addEventListener("dragleave", function () {
        dropZone.classList.remove("dragover");
    });

    dropZone.addEventListener("drop", function (e) {
        e.preventDefault();
        dropZone.classList.remove("dragover");

        const files = e.dataTransfer.files;

        Array.from(files).forEach(file => {

            if (!isDuplicateFile(file)) {
                selectedFiles.push(file);
            }
        });

        renderFileList();
    });


    $(document).on('click', '#btn_Sendapproval', function () {
        var FromName = window.location.pathname.split('/')[1];
        $.ajax({
            url: '/Approval/CheckPendingUser',
            type: 'POST',
            data: { vNo: rowId, vType: "Paud" },
            success: function (response) {
                console.log('Response:', response);
                // Pending with another user
                if (response.success === false) {
                    showToast(`Pending With Another User (${response.userCode})`,
                        { type: "warning" });
                    return;
                }
                // Approval_Code = 5
                if (response.approvalCode8 === true) {
                    OpenApprovalModal({ DocType: "Paud", DocNo: rowId, TableName: 'SAUDA' });
                    return;
                }
                // Approval_Code != 8
                OpenSendForApprovalModal({ DocType: "Paud", DocNo: rowId, UserCode: null, UserName: null, DocDate: null, TableName: 'SAUDA',  FromName, FromName });

            },
            error: function (xhr, status, error) {
                console.log(error);
                alert('Error while checking approval status.');
            }
        });

    });

    $(document).on('click', '#btn_Approved', function () {
        OpenApprovalModal({ DocType: "Paud", DocNo: rowId, TableName: 'SAUDA' });
    });

});




