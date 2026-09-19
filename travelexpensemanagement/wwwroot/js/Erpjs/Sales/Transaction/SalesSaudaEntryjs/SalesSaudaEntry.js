const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('docId');
const vtype = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(async function() {
        
    await GetVNo();
    await BindAllHeaderDropdown();
    await BindCustomerAgentList();
    await BindItemList();
    await BindPIList();
    await wireEvent();

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

    // ======================================================
    // PI CHANGE → LOAD PI DETAILS
    // ======================================================
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

        console.log("Credit Limit:", data.creditLimit);
        console.log("Outstanding:", data.outstanding);
        console.log("Available Limit:", data.availableLimit);

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

        const defectiveGoods = $('#ChkDefectiveGoods').is(':checked') ? 1 : 0;

        const selectedItem = $('#ddlItemShortname').select2('data')[0];

        const itemCode = selectedItem ? selectedItem.id : null;
        const itemType = $('#txtItemType').val() || null;

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

            GRADE: null,
            WASTE_PER: null,

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

//async function SaveData() {

//    try {

//        const defectiveGoods = $('#ChkDefectiveGoods').is(':checked') ? 1 : 0;

//        const selectedItem = $('#ddlItemShortname').select2('data')[0];

//        const itemCode = selectedItem ? selectedItem.id : null;
//        const itemType = $('#txtItemType').val() || null;

//        const model = {

//            V_NO: parseInt($('#NumDocno').val()) || null,
//            V_DATE: $('#DtDate').val() || null,

//            STATUS: $('#ddlstatus').val() || null,
//            PARTY_CODE: $('#ddlCustomerName').val() || null,

//            ADD1: $('#txtAddressLine1').val() || null,
//            ADD2: $('#txtAddressLine2').val() || null,
//            ADD3: $('#txtAddressLine3').val() || null,

//            CITY_CODE: $('#ddlCity').val() || null,
//            PHONE: $('#txtContactNo').val() || null,

//            ITEM_TYPE: itemType || null,
//            ITEM_CODE: itemCode || null,

//            TENACITY_GRPCODE: $('#ddlTenacity').val() || null,
//            TENACITY_GRP: $('#ddlTenacity option:selected').text() || null,

//            TRUCK_NO: $('#NumTrucksCont').val() || null,

//            QTY: $('#NumTotalWeight').val() || null,
//            NOS: $('#NumNosPcs').val() || null,

//            RATE: $('#NumBasicRate').val() || null,
//            CURRENCY: $('#ddlBasicRateCurrency').val() || null,

//            DISC_PER: $('#NumTradeDiscPerKgs').val() || null,
//            CDISC_PER: $('#NumCashDiscPercent').val() || null,
//            DISC_TYPE: $('#ddlTradeDisc').val() || null,

//            FRT_TERM: $('#ddlFreightTerm').val() || null,
//            FRT_RATE: $('#NumFreightRate').val() || null,
            
//            NET_RATE: $('#NumNetRate').val() || null,

//            PAYTERM_CODE: $('#ddlPaymentTerm').val() || null,
//            CD_DAYS: $('#NumCDInDays').val() || null,

//            DEFECTIVE_GOODS: defectiveGoods,
//            ITEM_REMARKS: $('#txtItemNameAsPerPO').val() || null,

//            DEL_TERM: $('#ddlDeliveryTerm').val() || null,
//            DELIVERY_DAYS: $('#ddlDelDays').val() || null,

//            REMARK: $('#txtRemarks').val() || null,

//            DEAL_THROUGH: $('#ddlSoldBy').val() || null,

//            PINO: $('#ddlPINO').val() || null,
//            OFFERNO: $('#txtOfferPONo').val() || null,
           
//            FLAKES_SIZE: $('#NumSizemm').val() || null,
//            FLAKES_SIZEMAX: null,
//            FLAKES_PVCPPM: $('#NumPVCPPM').val() || null,
//            FLAKES_PPMALL: $('#NumPPMALL').val() || null,

//            GRADE: null,
//            WASTE_PER: null,

//            FLAKES_USETYPE: $('#ddlusetype').val() || null,
//            REF_ASTYPE: null,
//            DEL_PORT: $('#txtPortCodeName').val() || null,

//            ATTACHMENT_PATH: null,

//            REF_ASNO: $('#ddlRefNo').val() || null,

//            STUFFING_WT: $('#NumStuffWtCont').val() || null,

//            FAPROV_STATUS: null,
//            FAPROV_REMARKS: null,
//            SHIP_TYPE: $('#ddlSupplyFor').val() || null

//        };

//        console.log("Sales Sauda Save Model:", model);

//        // ==========================================
//        // SAVE API
//        // ==========================================
//        const response = await fetch('/SalesSaudaEntry/SaveAndUpdateData', {
//            method: 'POST',
//            headers: {
//                'Content-Type': 'application/json'
//            },
//            body: JSON.stringify(model)
//        });

//        if (!response.ok) {
//            throw new Error("Failed to save Sales Sauda");
//        }

//        const result = await response.json();
//        console.log("Save Response:", result);

//        if (result.success) {
//            showToast(result.message || "Data saved successfully.", { type: "success" });
//        }
//        else {
//            showToast(result.message || "Failed to save data.", { type: "error" });
//        }
//    }
//    catch (error) {
//        console.error("Save Sales Sauda Error:", error);
//        showToast(error.message || "Something went wrong while saving data.", { type: "error" });
//    }

//}