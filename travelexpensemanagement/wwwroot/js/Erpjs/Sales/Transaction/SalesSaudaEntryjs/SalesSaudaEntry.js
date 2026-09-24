const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('id');
const vtype = urlParams.get('vType');
let isReadOnly = urlParams.get('readOnly') === 'true';
const DBTableName = 'SAUDA';
                              
$(document).ready(async function() {

    await GetVNo();
    await BindAllHeaderDropdown();
    await BindCustomerAgentList();
    await BindItemList();
    await BindPIList();
    await wireEvent();

    if (id) {
        await LoadEditData();
        checkApprovalStatus(vtype, $("#NumDocno").val(), DBTableName);
    }
    else {
        await GetVNo();
    }

    $('#btn_save').on('click', async function (e) {
        e.preventDefault();

        await SaveData();

    });
         
});

async function wireEvent() {

    SetCurrentDate();

    // ======================================================
    // CUSTOMER CHANGE → AUTOFILL DETAILS
    // ======================================================
    $(document).on('change', '#ddlCustomerName', async function () {

        const customerCode = $(this).val();

        if (!customerCode) {
            ClearCustomerDetails();
            ClearCreditLimitDetails();
            return;
        }

        const customer = $(this).find('option:selected').data('customer');

        if (!customer) {
            return;
        }

        // ==========================================
        // COUNTRY → SUPPLY FOR
        // ==========================================
        if (customer.country &&
            String(customer.country).trim().toUpperCase() !== 'INDIA') {
            
            $('#ddlSupplyFor').val('EXPORT').trigger('change');
        }

        // ==========================================
        // CUSTOMER CREDIT LIMIT
        // ==========================================
        await GetCustomerCreditLimit(customerCode);

        $('#txtAddressLine1').val(customer.add1 || '');
        $('#txtAddressLine2').val(customer.add2 || '');
        $('#txtAddressLine3').val(customer.add3 || '');
        $('#txtContactNo').val(customer.mobile || '');

        if (customer.cityCode !== null && customer.cityCode !== undefined && customer.cityCode !== '') {
            $('#ddlCity').val(customer.cityCode).trigger('change');
        }
        else {
            $('#ddlCity').val('').trigger('change');
        }

        if (customer.country) {

            const countryName = String(customer.country).trim().toLowerCase();

            const countryOption = $('#ddlCountry option').filter(function () {
                return $(this).text().trim().toLowerCase() === countryName;
            }).first();

            if (countryOption.length) {
                const countryValue = countryOption.val();

                $('#ddlCountry').val(countryValue).trigger('change');
            }
            else {
                console.log('Country not found:', customer.country);
                $('#ddlCountry').val('').trigger('change');
            }

        }
        else {
            $('#ddlCountry').val('').trigger('change');
        }

    });

    // ======================================================
    // ITEM FULL NAME CHANGE → AUTOFILL ITEM DETAILS
    // ======================================================
    $(document).on('change', '#ddlItemShortname', function () {

        const selectedData = $(this).select2('data');

        if (!selectedData || selectedData.length === 0) {

            $('#txtItemFullName').val('').trigger('change');
            $('#txtItemType').val('').trigger('change');
            return;
        }

        const item = selectedData[0];

        $('#txtItemFullName').val(item.shortName || '').trigger('change');
        $('#txtItemType').val(item.mgroupType || '').trigger('change');

    });

    // ================================
    // PI CHANGE → LOAD PI DETAILS
    // ================================
    $(document).on('change', '#ddlPINO', async function () {

        const docNo = $(this).val();

        if (!docNo) {
            return;
        }

        try {

            const response = await fetch(`/SalesSaudaEntry/GetPIDetails?docNo=${encodeURIComponent(docNo)}`);

            if (!response.ok) {
                throw new Error('Failed to load PI details');
            }

            const data = await response.json();

            console.log("PI Details:", data);

            $('#ddlCustomerName').val(data.billCode).trigger('change');
            $('#txtAddressLine1').val(data.add1 || '');
            $('#txtAddressLine2').val(data.add2 || '');
            $('#txtAddressLine3').val(data.add3 || '');
            $('#ddlCity').val(data.cityCode).trigger('change');

            if (data.country) {

                const countryName = String(data.country).trim().toLowerCase();

                const countryOption = $('#ddlCountry option').filter(function () {

                  return $(this).text().trim().toLowerCase() === countryName;

                }).first();

                if (countryOption.length) {

                    $('#ddlCountry').val(countryOption.val()).trigger('change');

                }

            }

            if (data.itemCode) {

                const itemOption = new Option(data.itemShortName,  data.itemCode, true, true);

                $(itemOption).data({

                    itemName: data.itemName,
                    shortName: data.itemShortName,
                    mgroupType: data.itemType

                });

                $('#ddlItemShortname').append(itemOption).trigger('change');

            }

            $('#txtQuantity').val(data.quantity || '');
            $('#txtBasicRate').val(data.rate || '');
            $('#ddlPaymentTerm').val(data.payTerm).trigger('change');
            $('#txtIncoterm').val(data.incoterm || '');
            $('#txtFinalDestination').val(data.finalDestCountry || '');
            $('#ddlSoldBy').val(data.soldBy).trigger('change');
            $('#txtOfferNo').val(data.buyerOrderNo || '');
            $('#ddlBasicRateCurrency').val(data.currency).trigger('change');
            $('#txtItemType').val(data.saleGroup || data.itemType || '').trigger('change');
        }
        catch (error) {
            console.error('Error loading PI details:', error);
            showToast("Error Loading PI Details",{ type: "error" });
        }

    });

    //==============================
    // PREVIEW ATTACHMENT
    //==============================
    $(document).on('click', '#btnPreviewImage', function () {

        const fileUrl = $('#btnPreviewImage').data('file-url');
        const fileType = $('#btnPreviewImage').data('file-type');

        if (!fileUrl || !fileType) {
            return;
        }

        $('#previewImage').hide().attr('src', '');
        $('#previewPdf').hide().attr('src', '');

        if (fileType.startsWith('image/')) {

            $('#previewImage').attr('src', fileUrl).show();

        }
        else if (fileType === 'application/pdf') {

            $('#previewPdf').attr('src', fileUrl).show();

        }

        const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));

        modal.show();
    });

    //==============================
    // CREATE SALES ORDER
    //==============================
    $(document).on('click', '#btn_createsalesorder', async function () {

        await CreateSalesOrder();

    });

    $(document).on('input change','#NumBasicRate, #NumTradeDiscPerKgs, #ddlTradeDisc, #NumCashDiscPercent, #ddlFreightTerm ,#NumFreightRate',
        function () {

            RateCalculator();

        }
    );

}

function SetCurrentDate() {
    const today = new Date();

    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');

    $('#DtDate').val(`${year}-${month}-${day}`);
}

function ClearCustomerDetails() {

    $('#txtAddressLine1').val('');
    $('#txtAddressLine2').val('');
    $('#txtAddressLine3').val('');
    $('#txtContactNo').val('');
    $('#ddlCity').val('').trigger('change');
    $('#ddlCountry').val('').trigger('change');

}

async function BindAllHeaderDropdown() {

    await Promise.all([

        bindDropdown('SalesSaudaEntry', 'PaymentTerm', '#ddlPaymentTerm', 'Select PaymentTerm', null, null, false, null, false),
        bindDropdown('SalesSaudaEntry', 'DocStatus', '#ddlstatus', 'Select Status', null, null, false, null, false),
        bindDropdown('SalesSaudaEntry', 'CurrencyMast', '#ddlBasicRateCurrency', 'Select Currency', null, null, false, null, false),
        bindDropdown('SalesSaudaEntry', 'CountryMast', '#ddlCountry', 'Select Country', null, null, false, null, false),
        bindDropdown('SalesSaudaEntry', 'TenacityGroup', '#ddlTenacity', 'Select Tenacity', null, null, false, null, false),
        bindDropdown('SalesSaudaEntry', 'CityMast', '#ddlCity', 'Select City', null, null, false, null, false),
        bindDropdown('SalesSaudaEntry', 'SoldBy', '#ddlSoldBy', 'Select SoldBy', null, null, false, null, true),

    ]);
}

async function GetVNo() {
    try {
        const response = await fetch('/SalesSaudaEntry/GenerateVNo');

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            return;
        }
        $('#NumDocno').val(data.v_NO);

        const docId = 'SAUD' + $('#NumDocno').val();
        console.log("docId", docId);
        
    } catch (error) {
        console.error('Error generating VNo:', error);
    }
}

async function GetDSAUVNo() {
    try {
        const response = await fetch('/SalesSaudaEntry/GenerateVNoForDSAU');

        if (!response.ok) {
            throw new Error('Failed to generate DSAU VNo');
        }

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            showToast(data.error, { type: "error" });
            return null;
        }

        console.log("DSAU VNo:", data.v_NO);
        console.log("DSAU VType:", data.v_TYPE);

        return data.v_NO;
    }
    catch (error) {
        console.error("Error generating DSAU VNo:", error);
        showToast("Error generating DSAU VNo", { type: "error" });
        return null;
    }
}

async function BindCustomerAgentList() {

    try {

        const response = await fetch('/SalesSaudaEntry/GetCustomerAgentList');

        if (!response.ok) {
            throw new Error('Failed to load Customer list');
        }

        const data = await response.json();

        console.log("Customer Name", data);

        const ddl = $('#ddlCustomerName');

        ddl.empty();

        ddl.append('<option value="">Select Customer Name</option>');
        
        data.forEach(item => {
            ddl.append($('<option>', {value: item.value, text: item.text}) );
        });

        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.trigger('change');
        }
        else {

            ddl.select2({
                width: '100%',
                placeholder: 'Select Customer Name',
                allowClear: true
            });

        }

        ddl.off('select2:open').on('select2:open', function () {

            setTimeout(function () {

                const searchBox = document.querySelector(
                    '.select2-container--open .select2-search__field'
                );

                if (searchBox) {
                    searchBox.focus();
                }

            }, 0);

        });

        data.forEach(item => {

            const option = ddl.find(`option[value="${item.value}"]`);
            option.data('customer', item);

        });

    }
    catch (error) {
        console.error('Error loading Customer list:',error);

        showToast("Error Loading Customer List", { type: "error" });
       
    }

}

async function BindItemList() {

    const ddl = $('#ddlItemShortname');

    if (ddl.hasClass('select2-hidden-accessible')) {
        ddl.select2('destroy');
    }

    ddl.empty();

    ddl.append($('<option>', { value: '', text: 'Select Item Short Name'}));

    ddl.select2({

        width: '100%',
        placeholder: 'Select Item Short Name',
        allowClear: true,

        minimumInputLength: 0,

        ajax: {

            url: '/SalesSaudaEntry/GetItemList',
            dataType: 'json',

            delay: 300,

            data: function (params) {

                return {

                    pageNo: params.page || 1,

                    pageSize: 500,

                    searchTerm: params.term || ''

                };

            },

            processResults: function (result, params) {

                params.page = params.page || 1;

                return {

                    results: result.data.map(function (item) {

                        return {

                            id: item.value,
                            text: item.shortName,
                            itemName: item.text,
                            shortName: item.shortName,
                            mgroupType: item.mgroupType

                        };

                    }),

                    pagination: {

                        more: result.hasMore

                    }

                };

            },

            cache: true

        }

    });

    ddl.off('select2:open').on('select2:open', function () {

        setTimeout(function () {

            const searchBox = document.querySelector(
                '.select2-container--open .select2-search__field'
            );

            if (searchBox) {

                searchBox.focus();

            }

        }, 0);

    });

}

async function BindPIList() {

    const ddl = $('#ddlPINO');

    if (ddl.hasClass('select2-hidden-accessible')) {
        ddl.select2('destroy');
    }

    ddl.empty();

    ddl.append(
        $('<option>', {
            value: '',
            text: 'Select PI'
        })
    );

    ddl.select2({

        width: '100%',
        placeholder: 'Select PI',
        allowClear: true,

        minimumInputLength: 0,

        ajax: {

            url: '/SalesSaudaEntry/GetPIList',
            dataType: 'json',

            delay: 300,

            data: function (params) {

                return {
                    searchTerm: params.term || ''
                };

            },

            processResults: function (data) {

                return {

                    results: data.map(function (item) {

                        return {

                            id: item.docNo,
                            text: item.docNo + ' - ' + item.customerName

                        };

                    })

                };

            },

            cache: true
        }

    });

}

async function GetCustomerCreditLimit(customerCode) {

    try {

        const response = await fetch(
            `/SalesSaudaEntry/GetCustomerCreditLimit?customerCode=${encodeURIComponent(customerCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load credit limit details');
        }

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            return;
        }

        // ==========================================
        // CREDIT LIMIT MASTER NOT FOUND
        // ==========================================
        if (!data.exists) {

            $('#Numcreditlimit').text('0');
            $('#Numoutstanding').text('0');
            $('#Numavailablelimit').text('0');

            showToast(data.message || 'Please First Create, Credit Limit Master for This Customer', { type: "warning" });

            return;
        }

        // ==========================================
        // SET VALUES
        // ==========================================
        $('#Numcreditlimit').text(formatAmount(data.creditLimit));
        $('#Numoutstanding').text(formatAmount(data.outstanding));
        $('#Numavailablelimit').text(formatAmount(data.availableLimit));

    }
    catch (error) {
        console.error('Error loading credit limit:', error);
        showToast( "Error Loading Credit Limit",{ type: "error" });
    }
}

function formatAmount(amount) {
    return Number(amount || 0).toLocaleString('en-IN', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function ClearCreditLimitDetails() {

    $('#Numcreditlimit').text('0.00');
    $('#Numoutstanding').text('0.00');
    $('#Numavailablelimit').text('0.00');

}

//==============================
// Save And Update Data
//==============================
async function SaveData() {

    try {

        const isValidDate = await checkValidDate();
        if (!isValidDate) return;

        if (!await validateData()) {
            return;
        }

        const defectiveGoods = $('#ChkDefectiveGoods').is(':checked') ? 1 : 0;

        const selectedItem = $('#ddlItemShortname').select2('data')[0];

        const itemCode = selectedItem ? selectedItem.id : null;
        const itemType = $('#txtItemType').val() || null;

        let dsauVNo = null;

        if (id) {
            dsauVNo = await GetDSAUVNo();

            if (!dsauVNo) {
                return;
            }

            console.log("Generated DSAU VNo:", dsauVNo);
            console.log("DSAU DOC_ID:", "DSAU" + dsauVNo);
        }

        const model = {

            V_NO: parseInt($('#NumDocno').val()) || null,
            V_DATE: $('#DtDate').val() || null,

            STATUS: $('#ddlstatus').val() || null,
            PARTY_CODE: $('#ddlCustomerName').val() || null,

            ADD1: $('#txtAddressLine1').val() || null,
            ADD2: $('#txtAddressLine2').val() || null,
            ADD3: $('#txtAddressLine3').val() || null,

            CITY_CODE: $('#ddlCity').val() || null,
            PHONE: $('#txtContactNo').val() || null,

            ITEM_TYPE: itemType || null,
            ITEM_CODE: itemCode || null,

            TENACITY_GRPCODE: $('#ddlTenacity').val() || null,
            TENACITY_GRP: $('#ddlTenacity option:selected').text() || null,

            TRUCK_NO: $('#NumTrucksCont').val() || null,

            QTY: $('#NumTotalWeight').val() || null,
            NOS: $('#NumNosPcs').val() || null,

            RATE: $('#NumBasicRate').val() || null,
            CURRENCY: $('#ddlBasicRateCurrency').val() || null,

            DISC_PER: $('#NumTradeDiscPerKgs').val() || null,
            CDISC_PER: $('#NumCashDiscPercent').val() || null,
            DISC_TYPE: $('#ddlTradeDisc').val() || null,

            FRT_TERM: $('#ddlFreightTerm').val() || null,
            FRT_RATE: $('#NumFreightRate').val() || null,

            NET_RATE: $('#NumNetRate').val() || null,

            PAYTERM_CODE: $('#ddlPaymentTerm').val() || null,
            CD_DAYS: $('#NumCDInDays').val() || null,

            DEFECTIVE_GOODS: defectiveGoods,
            ITEM_REMARKS: $('#txtItemNameAsPerPO').val() || null,

            DEL_TERM: $('#ddlDeliveryTerm').val() || null,
            DELIVERY_DAYS: $('#ddlDelDays').val() || null,

            REMARK: $('#txtRemarks').val() || null,

            DEAL_THROUGH: $('#ddlSoldBy').val() || null,

            PINO: $('#ddlPINO').val() || null,
            OFFERNO: $('#txtOfferPONo').val() || null,

            FLAKES_SIZE: $('#NumSizemm').val() || null,
            FLAKES_SIZEMAX: null,
            FLAKES_PVCPPM: $('#NumPVCPPM').val() || null,
            FLAKES_PPMALL: $('#NumPPMALL').val() || null,

            GRADE: $('#ddlGRSApplicable').val(),
            WASTE_PER: $('#NumGRSRate').val() ,

            FLAKES_USETYPE: $('#ddlusetype').val() || null,
            REF_ASTYPE: null,
            DEL_PORT: $('#txtPortCodeName').val() || null,

            ATTACHMENT_PATH: null,

            REF_ASNO: $('#ddlRefNo').val() || null,

            STUFFING_WT: $('#NumStuffWtCont').val() || null,

            FAPROV_STATUS: null,
            FAPROV_REMARKS: null,

            SHIP_TYPE: $('#ddlSupplyFor').val() || null
        };

        const formData = new FormData();

        Object.keys(model).forEach(key => {

            if (model[key] !== null && model[key] !== undefined) {
                formData.append(key, model[key]);
            }

        });

        if (dsauVNo) {
            formData.append('DSAU_V_NO', dsauVNo);
        }
         
        const imageFile = $('#TxtAttachment')[0].files[0];

        if (imageFile) {

            formData.append('Attachment', imageFile);

            console.log("Attachment:", imageFile.name);

        }

        const response = await fetch('/SalesSaudaEntry/SaveAndUpdateData', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error("Failed to save Sales Sauda");
        }

        const result = await response.json();

        console.log("Save Response:", result);

        if (result.success) {
            showToast(result.message || "Data saved successfully.", { type: "success" });

            isReadOnly = true;
            setFormReadOnly();

        }
        else {
            showToast(result.message || "Failed to save data.", { type: "error" });
        }

    }
    catch (error) {
        console.error("Save Sales Sauda Error:", error);
        showToast(error.message || "Something went wrong while saving data.",{ type: "error" });
    }

}

//==============================
// Load Edit Data
//==============================
async function LoadEditData() {

    try {

        const response = await fetch(
            `/SalesSaudaEntry/LoadEditData?vNo=${encodeURIComponent(id)}&vType=${encodeURIComponent(vtype)}`
        );

        if (!response.ok) {
            throw new Error("Failed to load edit data.");
        }

        const result = await response.json();

        console.log("Load Edit Data:", result);

        if (!result.success) {
            showToast(result.message || "Data not found.", {type: "error"});
            return;
        }

        const data = result.data;
        const images = result.images || [];

        $('#NumDocno').val(data.V_NO ?? '');

        if (data.V_DATE) {
            $('#DtDate').val(String(data.V_DATE).substring(0, 10));
        }

        $('#ddlstatus').val(data.STATUS ?? '').trigger('change');
        $('#ddlCustomerName').val(data.PARTY_CODE ?? '').trigger('change');
        $('#txtAddressLine1').val(data.ADD1 ?? '');
        $('#txtAddressLine2').val(data.ADD2 ?? '');
        $('#txtAddressLine3').val(data.ADD3 ?? '');
        $('#txtContactNo').val(data.PHONE ?? '');
            
        $('#ddlCity').val(data.CITY_CODE ?? '').trigger('change');

        $('#ddlSupplyFor').val(data.SHIP_TYPE ?? '').trigger('change');

        if (data.ITEM_CODE) {

            const itemOption = new Option(data.ITEM_SHORTNAME || data.ITEM_NAME || data.ITEM_CODE, data.ITEM_CODE, true, true);

            $(itemOption).data({
                itemName: data.ITEM_NAME,
                shortName: data.ITEM_SHORTNAME,
                mgroupType: data.ITEM_TYPE
            });

            $('#ddlItemShortname').append(itemOption).trigger('change');
        }

        $('#txtItemFullName').val(data.ITEM_NAME ?? '');
        $('#txtItemType').val(data.ITEM_TYPE ?? '');

        $('#ddlTenacity').val(data.TENACITY_GRPCODE ?? '').trigger('change');

        $('#NumTrucksCont').val(data.TRUCK_NO ?? '');

        $('#NumTotalWeight').val(data.QTY ?? '');

        $('#NumNosPcs').val(data.NOS ?? '');

        $('#NumBasicRate').val(data.RATE ?? '');
        
        $('#ddlBasicRateCurrency').val(data.CURRENCY ?? '').trigger('change');

        $('#NumTradeDiscPerKgs').val(data.DISC_PER ?? '');

        $('#NumCashDiscPercent').val(data.CDISC_PER ?? '');

        $('#ddlTradeDisc').val(data.DISC_TYPE ?? '').trigger('change');

        $('#ddlFreightTerm').val(data.FRT_TERM ?? '').trigger('change');

        $('#NumFreightRate').val(data.FRT_RATE ?? '');

        $('#NumNetRate').val(data.NET_RATE ?? '');

        $('#ddlPaymentTerm').val(data.PAYTERM_CODE ?? '').trigger('change');

        $('#NumCDInDays').val(data.CD_DAYS ?? '');

        $('#ChkDefectiveGoods').prop('checked',Number(data.DEFECTIVE_GOODS) === 1);

        $('#txtItemNameAsPerPO').val(data.ITEM_REMARKS ?? '');

        $('#ddlDeliveryTerm').val(data.DEL_TERM ?? '').trigger('change');
        $('#ddlDelDays').val(data.DELIVERY_DAYS ?? '').trigger('change');
        $('#txtRemarks').val(data.REMARK ?? '');
        $('#ddlSoldBy').val(data.DEAL_THROUGH ?? '').trigger('change');
        $('#txtOfferPONo').val(data.OFFERNO ?? '');
        $('#NumSizemm').val(data.FLAKES_SIZE ?? '');
        $('#NumPVCPPM').val(data.FLAKES_PVCPPM ?? '');
        $('#NumPPMALL').val(data.FLAKES_PPMALL ?? '');
        $('#ddlusetype').val(data.FLAKES_USETYPE ?? '').trigger('change');
        $('#txtPortCodeName').val(data.DEL_PORT ?? '');
        $('#ddlRefNo').val(data.REF_ASNO ?? '').trigger('change');
        $('#NumStuffWtCont').val(data.STUFFING_WT ?? '');
        $('#ddlGRSApplicable').val(data.GRADE ?? '');
        $('#NumGRSRate').val(data.WASTE_PER ?? '');

        if (data.PINO) {
            const piOption = new Option(data.PINO, data.PINO, true, true );
            $('#ddlPINO').append(piOption).trigger('change');
        }

        // ==========================================
        // LOAD ATTACHMENT
        // ==========================================

        if (images.length > 0) {

            const attachment = images[0];

            console.log("Attachment Data:", attachment);

            const base64 = attachment.IMG_FILE;
            const fileName = attachment.FILE_NAME;

            if (base64 && fileName) {

                const fileType = getMimeType(fileName);

                const fileUrl = `data:${fileType};base64,${base64}`;

                $('#TxtAttachment').hide();

                $('#attachmentFileName').text(fileName);
               
                $('#previewContainer').css('display', 'flex');

                $('#btnPreviewImage').data('file-url', fileUrl).data('file-type', fileType).show();

                console.log("File Name:", fileName);
                console.log("File Type:", fileType);
            }
        }

        await GetBalanceDispatch();

        if (isReadOnly) {
            setFormReadOnly();
        }

    }
    catch (error) {
        console.error("Load Sales Sauda Edit Error:", error);
        showToast("Error loading Sales Sauda data.", { type: "error" });
    }
}

//=====================
// Balance Dispatch
//=====================
async function GetBalanceDispatch() {

    try {

        const saudaNo = parseInt($('#NumDocno').val());
        const saudaQty = Number($('#NumTotalWeight').val()) || 0;

        if (!saudaNo) {
            $('#Numbalancedispatch').text('0.00');
            return;
        }

        const response = await fetch(
            `/SalesSaudaEntry/GetBalanceDispatch?saudaNo=${encodeURIComponent(saudaNo)}&saudaType=SAUD`
        );

        if (!response.ok) {
            throw new Error('Failed to load Balance Dispatch');
        }

        const data = await response.json();

        if (!data.success) {
            $('#Numbalancedispatch').text('0.00');
            return;
        }

        const balanceDispatch = saudaQty - Number(data.dispatchedQty || 0);

        $('#Numbalancedispatch').text(formatAmount(balanceDispatch));

    }
    catch (error) {

        console.error('Error loading Balance Dispatch:', error);

        $('#Numbalancedispatch').text('0.00');
    }
}

//=======================
// Validate Data
//=======================
async function validateData() {

    if (!validateRequiredField('#DtDate', 'Date')) {
        return false;
    }

    if(!validateRequiredField('#ddlCustomerName', 'Customer Name')) {
        return false;
    }

    if (!validateRequiredField('#ddlItemShortname', 'Item Name')) {
        return false;
    }

    if (!validateRequiredField('#txtItemType', 'Type')) {
        return false;
    }

    if (!validateRequiredField('#NumTotalWeight', 'Weight')) {
        return false;
    }

    if (!validateRequiredField('#ddlSoldBy', 'Sold By person')) {
        return false;
    }

    if (!validateRequiredField('#NumBasicRate', 'Rate')) {
        return false;
    }

    if (!validateRequiredField('#ddlDelDays', 'Delivery Days')) {
        return false;
    }

    if (!validateRequiredField('#ddlFreightTerm', 'Freight Term')) {
        return false;
    }

    // ==========================================
    // FREIGHT RATE
    // FOR / FOR-TPT
    // ==========================================
    const freightTerm = $('#ddlFreightTerm').val();

    if (freightTerm === 'FOR' || freightTerm === 'FOR-TPT') {

        if (!validateRequiredField('#NumFreightRate', 'Freight Rate')) {
            return false;
        }
    }

    const supplyFor = $('#ddlSupplyFor').val();

    if (supplyFor !== 'LOCAL') {

        if (!validateRequiredField('#ddlPINO', 'Proforma Invoice No.')) {
            return false;
        }
    }

    if ($('#ddlGRSApplicable').val() === 'Yes') {
        if (!validateRequiredField('#NumGRSRate', 'GRS Rate')) {
            return false;
        }
    }

    return true;
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

function setFormReadOnly() {
    const page = $('#SalesSaudaform');

    page.find('input, select, textarea').prop('disabled', true);
    page.find('button').not('#btnPreviewImage').prop('disabled', true);

    $('#btn_save').hide();
    $('#btnPreviewImage').prop('disabled', false);
}

//===========================
// CREATE SALES ORDER
//===========================
async function CreateSalesOrder() {

    try {

        const saudaVNo = parseInt($('#NumDocno').val());

        if (!saudaVNo) {
            showToast("Sales Sauda Number not found.", { type: "error" });
            return;
        }

        const confirmResult = await Swal.fire({
            title: 'Create Sales Order?',
            text: 'Do you want to create Sales Order?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes',
            cancelButtonText: 'No'
        });

        if (!confirmResult.isConfirmed) {
            return;
        }

        $('#btn_createsalesorder').prop('disabled', true);

        const response = await fetch('/SalesSaudaEntry/CreateSalesOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(saudaVNo)
        });

        if (!response.ok) {
            throw new Error("Failed to create Sales Order.");
        }

        const result = await response.json();

        console.log("Create Sales Order Response:", result);

        if (result.success) {
            showToast(result.message || "Sales Order generated successfully.",{ type: "success" });
        }
        else {
            showToast(result.message || "Sales Order could not be created.", { type: "error" });
        }

    }
    catch (error) {
        console.error("Create Sales Order Error:", error);
        showToast(error.message || "Something went wrong while creating Sales Order.",{ type: "error" });
    }
    finally {
        $('#btn_createsalesorder').prop('disabled', false);
    }
}

//===========================
// RATE CALCULATOR
//===========================
function RateCalculator() {

    try {

        let tdiscAmt = 0;
        let res1 = 0;
        let res2 = 0;

        const rate = Number($('#NumBasicRate').val()) || 0;
        const tradeDisc = Number($('#NumTradeDiscPerKgs').val()) || 0;
        const discType = $('#ddlTradeDisc').val();
        const cashDisc = Number($('#NumCashDiscPercent').val()) || 0;
        const freightTerm = $('#ddlFreightTerm').val();
        const freightRate = Number($('#NumFreightRate').val()) || 0;

        // ==========================================
        // TRADE DISCOUNT
        // ==========================================
        if (discType === '%') {

            tdiscAmt = (rate * tradeDisc / 100);

        }
        else {

            tdiscAmt = tradeDisc;

        }

        // ==========================================
        // RATE AFTER TRADE + CASH DISCOUNT
        // ==========================================
        res1 = rate - tdiscAmt - (rate * cashDisc / 100);

        // ==========================================
        // FREIGHT
        // FOR / FOR-TPT
        // ==========================================
        if (freightTerm === 'FOR' || freightTerm === 'FOR-TPT') {

            res2 = freightRate;

            $('#NumNetRate').val(formatAmount(res1 - res2));

        }
        else {

            $('#NumNetRate').val(formatAmount(res1));

        }

    }
    catch (error) {

        console.error('Rate Calculator Error:', error);

        showToast(
            error.message || 'Error calculating Net Rate',
            { type: "error" }
        );

    }
}

//===============Approval===============
$(document).on('click', '#btn_Sendapproval', function () {
    var FromName = window.location.pathname.split('/')[1];
    $.ajax({
        url: '/Approval/CheckPendingUser',
        type: 'POST',
        data: {
            vNo: $('#NumDocno').val(),
            vType: vtype
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
                    DocType: vtype,
                    DocNo: $('#NumDocno').val(),
                    TableName: DBTableName
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: vtype,
                DocNo: $('#NumDocno').val(),
                UserCode: null,
                UserName: null,
                DocDate: null,
                TableName: DBTableName,
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
        DocType: vtype,
        DocNo: $('#NumDocno').val(),
        TableName: DBTableName
    });
});

async function checkValidDate() {

    const data = {
        vdate: $("#DtDate").val(),
        vtype: "SAUD",
        vno: $("#NumDocno").val()
    };

    try {

        const response = await fetch('/SalesSaudaEntry/CheckValidDate', {
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

//============================
// Print Report
//============================
async function SalesSaudaEntry() {

    var reportName = "SAUDA_FORMAT";

    var vType = "SAUD";
    var vNo = $('#NumDocno').val();
    var rptName = "Sale Sauda Note";

    if (!vType || !vNo) {
        showToast("V Type and V No are required.", { type: "error" });
        return;
    }

    var SelForMul =
        "{SAUDA.V_TYPE}='" + vType + "'" +
        " AND {SAUDA.V_NO}=" + vNo +
        " AND {SAUDA.COMP_CODE}=" + window.globalVariables.compCode +
        " AND {SAUDA.YEAR_CODE}=" + window.globalVariables.yearCode +
        " AND {SAUDA.BRANCH_CODE}=" + window.globalVariables.branchCode;

    // =========================
    // REPORT DATA
    // =========================

    var formulaFields = {

        Reportname: reportName,

        selectionFormula: SelForMul,

        Database: window.database.db,

        Parameters: {

            RPTNAME: rptName,

            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,

            GST: "GSTIN: " + window.globalVariables.gstin,
            PAN: "PAN : " + window.globalVariables.pan,
            website: "Website: " + window.globalVariables.website,
            email: "Email: " + window.globalVariables.email,
            comp_phone: "Phone: " + window.globalVariables.compPhone
        }
    };

    console.log("Sale Sauda Entry Report Data:", formulaFields);
    console.log("Selection Formula:", SelForMul);

    // =========================
    // TIMESTAMP
    // =========================

    var now = new Date();

    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);

    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');

    var timestamp =`${day}${month}${year}_${hours}${minutes}${seconds}`;

    // =========================
    // GENERATE REPORT
    // =========================

    $.ajax({

        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {

            var file = new Blob(
                [response],
                { type: 'application/pdf' }
            );

            var fileName =
                `${reportName}_${timestamp}.pdf`;

            var link =
                document.createElement('a');

            link.href =
                URL.createObjectURL(file);

            link.download =
                fileName;

            document.body.appendChild(link);

            link.click();

            document.body.removeChild(link);

        },

        error: function (xhr, status, error) {

            console.error(
                'Error generating report:',
                error
            );

        }

    });
}