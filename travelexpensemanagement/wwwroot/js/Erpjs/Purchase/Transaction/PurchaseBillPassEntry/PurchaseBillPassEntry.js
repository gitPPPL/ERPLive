const rowsData = [];
let rowsAttachment = [];
let rowIndex = 0;

const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const isReadOnly = urlParams.get('readOnly') === 'true';

const compCode = window.compCode;
const branchCode = window.branchCode;
const yearCode = window.yearCode;

$(document).ready(function () {
    //Added by Sumesh
    InitializeERPSingleDropdown({
        id: "TxtMRNNo2",
        placeholder: "Select MRN",
        data: []
    });

    toggleDate();

    if (!isNaN(rowId) && rowId > 0) {
        loadFullQuotationByVno(rowId);
    }
    else {
        addNewRowBelow();
    }

    loadDocTypeList();
    loadPartyListNatureSupplier('#ddlBillFrom');
    loadPartyListNatureSupplier('#ddlShipFrom1', true);
    loadCityList('#ddlDispCity');
    loadCityList('#ddlCityPD');
    loadCityList('#ddlCitySF');
    loadTransportList();
    loadStateList('#ddlStatePD');
    loadStateList('#ddlStateSF');
    loadPartyDrCrAcList('#ddlCreditAC');
    loadPartyDrCrAcList('#ddlFreightDebitAC');
    loadPartyDrCrAcList('#ddlFreightCreditAC');
    loadPartyDrCrAcList('#ddlWBDebitAC');
    loadPartyDrCrAcList('#ddlWBCreditAC');
    loadPartyDrCrAcList('#ddlUnloadDebitAC');
    loadPartyDrCrAcList('#ddlUnloadCreditAC');
    loadPartyDrCrAcList('#ddlTdsAccount');
    loadCurrencyList();
    let frtCrAcCode = $('#ddlFreightCreditAC').val();
    loadTranGSTByFrtCrAc(frtCrAcCode);
    const currentDate = getCurrentDateYMD();
    $('#DtDocDate, #DtBillDate, #DtGRDate, #DtBillDateLD, #DtHoldDateCRDRNote, #DtBLDate, #Dtsysdate, #DtChDate, #DtPlDate').val(currentDate);
    wireEvents();
});
//=====================EVENTS=============================
function wireEvents() {
    $('#ddlDocType').on('change', function () {
        const vType = $(this).val();
        console.log("vType: ", vType);
        GetVNo(vType);
        loadMRNList(vType)
        $('#TxtMRNNo1').val('');
        loadDrAcListByVtype('#ddlDebitAC', vType);
    })

    $('#TxtMRNNo2').on('change', function () {
        const mrnType = $(this).find("option:selected").data("vtype");
        console.log("mrnType: ", mrnType);
        $('#TxtMRNNo1').val(mrnType);
    })

    $('#ddlFreightCreditAC').on('change', function () {
        const frtCrAcCode = $(this).val();
        loadTranGSTByFrtCrAc(frtCrAcCode);
    })

    $('#ddlShipFrom1').on('change', function () {
        const shipFromCode = $(this).val();
        loadAddList(shipFromCode);
    })

    $('#ddlBillFrom').on('change', function () {
        const billFromCode = $(this).val();
        loadAddList(billFromCode);
    })

    $('#TxtAdd1PD').change(function () {
        var selectedVal = $(this).val();
        var selectedtxt = $(this).text();
        var PCode = $(this).data('pcode');

        $.ajax({
            url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
            type: 'GET',
            data: { cCode: compCode, pCode: PCode, addressId: selectedVal },
            success: function (response) {
                var res = response.addressDetails;
                $('#TxtAdd2PD').val(res.add2);
                $('#TxtAdd3PD').val(res.add3);
                $('#TxtGSTNo').val(res.gstin);
                if ($('#ddlCityPD option[value="' + res.cityCode + '"]').length === 0) {
                    $('#ddlCityPD').append($('<option>', {
                        value: res.cityCode,
                        text: res.cityName
                    }));
                }
                $('#ddlCityPD').val(res.cityCode);

                // shipto address
                $('#TxtAdd1SF').val(res.add1);
                $('#TxtAdd2SF').val(res.add2);
                $('#TxtAdd3SF').val(res.add3);
                $('#TxtGSTNoSF').val(res.gstin);
                if ($('#ddlCitySF option[value="' + res.cityCode + '"]').length === 0) {
                    $('#ddlCitySF').append($('<option>', {
                        value: res.cityCode,
                        text: res.cityName
                    }));
                }
                $('#ddlCitySF').val(res.cityCode);
            },
            error: function (xhr, status, error) {
                toastr.error('Error loading Item make: ' + error);
            }
        });
    })

    $('#ddlShipFromAddress').change(function () {
        var selectedVal = $(this).val();
        var selectedtxt = $(this).text();
        var PCode = $(this).data('pcode');

        $.ajax({
            url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
            type: 'GET',
            data: { cCode: compCode, pCode: PCode, addressId: selectedVal },
            success: function (response) {
                var res = response.addressDetails;
                // shipto address
                $('#TxtAdd1SF').val(res.add1);
                $('#TxtAdd2SF').val(res.add2);
                $('#TxtAdd3SF').val(res.add3);
                $('#TxtGSTNoSF').val(res.gstin);
            },
            error: function (xhr, status, error) {
                toastr.error('Error loading Item make: ' + error);
            }
        });
    })

    $('#btn-save').click(function (e) {
        e.preventDefault();

        // Validate Doc Type
        if (!$('#ddlDocType').val()) {
            toastr.warning("Please select a Document Type.");
            return;
        }

        // Validate Doc Date
        const docDate = $('#DtDocDate').val();
        if (!docDate) {
            toastr.warning("Please select a Document Date.");
            return;
        }
        // Optional: check valid date format (basic)
        if (isNaN(new Date(docDate).getTime())) {
            toastr.warning("Please enter a valid Document Date.");
            return;
        }

        // Validate Bill From (assuming dropdown with numeric value)
        if (!$('#ddlBillFrom').val()) {
            toastr.warning("Please select Bill From.");
            return;
        }

        // validation for quotation3/attachments
        if (!rowsAttachment || rowsAttachment.length === 0) {
            toastr.warning("please attach at least one file before saving.");
            return;
        }

        // 1. Clear previous data if any
        let rowsData = [];

        const headerData = {
            V_TYPE: $('#ddlDocType').val(),
            V_DATE: parseNullableDate($('#DtDocDate').val()),
            V_NO: parseInt($('#NumDocNo').val()) || null,
            PARTY_CODE: parseInt($('#ddlBillFrom').val()) || null,
            BILL_ADD1: $('#TxtAdd1PD').val(),
            BILL_ADD2: $('#TxtAdd2PD').val(),
            BILL_ADD3: $('#TxtAdd3PD').val(),
            BILL_CITY: parseInt($('#ddlCityPD').val()) || null,
            BILL_GST: $('#TxtGSTNo').val(),
            DISP_ADDRESS: $('#TxtDispFromAdd').val(),
            DISP_CITY: parseInt($('#ddlDispCity').val()) || null,
            SHIP_ADD1: $('#ddlShipFromAddress').val(),
            SHIP_ADD2: $('#TxtAdd2SF').val(),
            SHIP_ADD3: $('#TxtAdd3SF').val(),
            SHIP_CITY: parseInt($('#ddlCitySF').val()) || null,
            SHIP_GST: $('#TxtGSTNoSF').val(),
            BILL_NO: $('#TxtBillNoLD').val(),
            BILL_DATE: parseNullableDate($('#DtBillDate').val()),
            CHALL_NO: $('#TxtChallanNo').val(),
            CHALL_DATE: parseNullableDate($('#DtChDate').val()),
            WAYBILL_NO: $('#TxtWaybillNo').val(),
            GR_DATE: parseNullableDate($('#DtGRDate').val()),
            EWB_INVNO: $('#TxtWayBillInvNo').val(),
            EWB_EXPDATE: parseNullableDate($('#DtWaybillExpiry').val()),
            EXCH_RATE: parseFloat($('#NumExRate').val()) || null,
            INPUT_TYPE: $('#ddlInputType').val(),
            DEBIT_AC: parseInt($('#ddlDebitAC').val()) || null,
            CREDIT_AC: parseInt($('#ddlCreditAC').val()) || null,
            REMARKS: $('#txtRemarks').val(),
            NAMOUNT: parseFloat($('#TxtNetAmount').val()) || null,
            STATUS: parseInt($('#ddlStatus').val()) || null,

            // 🚚 Transport Information
            TRANSPORT_NAME: $('#ddlTransportName').val(),
            TRUCK_NO: $('#txtVehicleNo').val(),
            CONTAINER_NO: $('#txtContainerNo').val(),
            GR_NO: $('#txtGRNo').val(),
            SEALED_VEHICLE: $('#ChkSealedVehicle').is(':checked') ? 1 : 0,

            // 🚛 Freight Details
            FRTPAY_AMT: parseFloat($('#NumFreightPay').val()) || 0,
            FRTPAY_TAXPER: parseFloat($('#NumFrtTax1').val()) || 0,
            FRTPAY_TAX: parseFloat($('#NumFrtTax2').val()) || 0,
            FRTPAY_DRAC: parseInt($('#ddlFreightDebitAC').val()) || null,
            FRTPAY_CRAC: parseInt($('#ddlFreightCreditAC').val()) || null,
            FRTPAY_NAR: $('#TxtFrtPayNarration').val(),
            FRT_TDSPER: parseFloat($('#NumTDSonFRT1').val()) || 0,
            FRT_TDS: parseFloat($('#NumTDSonFRT2').val()) || 0,

            // 🧾 Billing Info
            TRP_GSTNO: $('#ddlTransportGSTNo').val(),
            TRP_TAXTYPE: $('#ddlTaxType').val(),

            // 📦 WB Details
            WB_AMT: parseFloat($('#NumWBAmount').val()) || 0,
            WB_TDSPER: parseFloat($('#NumWBTDS1').val()) || 0,
            WB_TDS: parseFloat($('#NumWBTDS2').val()) || 0,
            WB_DRACT: parseInt($('#ddlWBDebitAC').val()) || null,
            WB_CRACT: parseInt($('#ddlWBCreditAC').val()) || null,
            WB_NARR: $('#TxtWBNarration').val(),

            // 🏗️ Unloading Details
            UL_AMT: parseFloat($('#NumUnloadAmt').val()) || 0,
            UL_TDSPER: parseFloat($('#NumUnloadTDS1').val()) || 0,
            UL_TDS: parseFloat($('#NumUnloadTDS2').val()) || 0,
            UL_DRACT: parseInt($('#ddlUnloadDebitAC').val()) || null,
            UL_CRACT: parseInt($('#ddlUnloadCreditAC').val()) || null,
            UL_NARR: $('#TxtUnloadNarration').val()
        };

        $('#tblItemRecordPBPE tbody tr').each(function () {
            const row = $(this);
            const index = row.index();
            const rowData = {
                ITEM_NAME: row.find(`[id^="ITEM_NAME_"]`).val(),
                HSN_CODE: row.find(`[id^="HSN_CODE_"]`).val(),
                UOM_NAME: row.find(`[id^="UOM_NAME_"]`).val(),
                NOS: parseInt(row.find(`[id^="NOS_"]`).val()) || 0,
                RECD_QTY: parseFloat(row.find(`[id^="RECD_QTY_"]`).val()) || 0,
                BILL_QTY: parseFloat(row.find(`[id^="BILL_QTY_"]`).val()) || 0,
                USD_RATE: parseFloat(row.find(`[id^="USD_RATE_"]`).val()) || 0,
                EXCH_RATE: parseFloat(row.find(`[id^="EXCH_RATE_"]`).val()) || 0,
                RATE: parseFloat(row.find(`[id^="RATE_"]`).val()) || 0,
                AMOUNT: parseFloat(row.find(`[id^="AMOUNT_"]`).val()) || 0,
                RCM_YN: row.find(`[id^="RCM_YN_"]`).val(),
                INPUT_YN: row.find(`[id^="INPUT_YN_"]`).val(),
                TAX_CODE: parseInt(row.find(`[id^="TAX_CODE_"]`).val()) || null,
                PACK_PER: parseFloat(row.find(`[id^="PACK_PER_"]`).val()) || 0,
                PACK_AMT: parseFloat(row.find(`[id^="PACK_AMT_"]`).val()) || 0,
                DISC_PER: parseFloat(row.find(`[id^="DISC_PER_"]`).val()) || 0,
                DISC_AMT: parseFloat(row.find(`[id^="DISC_AMT_"]`).val()) || 0,
                CGST_PER: parseFloat(row.find(`[id^="CGST_PER_"]`).val()) || 0,
                CGST_AMT: parseFloat(row.find(`[id^="CGST_AMT_"]`).val()) || 0,
                SGST_PER: parseFloat(row.find(`[id^="SGST_PER_"]`).val()) || 0,
                SGST_AMT: parseFloat(row.find(`[id^="SGST_AMT_"]`).val()) || 0,
                IGST_PER: parseFloat(row.find(`[id^="IGST_PER_"]`).val()) || 0,
                IGST_AMT: parseFloat(row.find(`[id^="IGST_AMT_"]`).val()) || 0,
                CESS_PER: parseFloat(row.find(`[id^="CESS_PER_"]`).val()) || 0,
                CESS_AMT: parseFloat(row.find(`[id^="CESS_AMT_"]`).val()) || 0,
                VAT_PER: parseFloat(row.find(`[id^="VAT_PER_"]`).val()) || 0,
                VAT_AMT: parseFloat(row.find(`[id^="VAT_AMT_"]`).val()) || 0,
                OTH_AMT: parseFloat(row.find(`[id^="OTH_AMT_"]`).val()) || 0,
                NET_AMT: parseFloat(row.find(`[id^="NET_AMT_"]`).val()) || 0,
                MAKE_CODE: parseInt(row.find(`[id^="MAKE_CODE_"]`).val()) || null,
                DEPT_CODE: parseInt(row.find(`[id^="DEPT_CODE_"]`).val()) || null,
                REMARKS: row.find(`[id^="REMARKS_"]`).val(),
                LAND_RATE: parseFloat(row.find(`[id^="LAND_RATE_"]`).val()) || 0,
                LAND_AMT: parseFloat(row.find(`[id^="LAND_AMT_"]`).val()) || 0,
                POLAND_RATE: parseFloat(row.find(`[id^="POLAND_RATE_"]`).val()) || 0,
                PO_RATE: parseFloat(row.find(`[id^="PO_RATE_"]`).val()) || 0,
                PO_TYPE: row.find(`[id^="PO_TYPE_"]`).val(),
                PO_NO: parseInt(row.find(`[id^="PO_NO_"]`).val()) || null,
                KANTA_TYPE: row.find(`[id^="KANTA_TYPE_"]`).val(),
                KANTA_NO: parseInt(row.find(`[id^="KANTA_NO_"]`).val()) || 0,
                REQ_TYPE: row.find(`[id^="REQ_TYPE_"]`).val(),
                REQ_NO: parseInt(row.find(`[id^="REQ_NO_"]`).val()) || null,
                REF_TYPE: row.find(`[id^="REF_TYPE_"]`).val(),
                REF_NO: parseInt(row.find(`[id^="REF_NO_"]`).val()) || null
            };
            rowsData.push(rowData);
        });

        if (rowsData.length === 0) {
            toastr.warning("Please add at least one row before saving.");
            return;
        }

        const data = {
            header: headerData,
            lineRows: rowsData,
            Attachement: rowsAttachment
        };
        console.log(data);

        //3. AJAX Save
        $.ajax({
            url: '/PurchaseBillPassEntry/SavePurchaseBillPassEntry',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function (response) {
                if (response.success) {
                    toastr.success('Saved successfully!');
                    setTimeout(() => {
                        window.location.href = '/PurchaseBillPassEntryList/Index';
                    }, 1000);
                } else {
                    toastr.error('Error: ' + response.message);
                }
            },
            error: function (xhr, status, error) {
                toastr.error('AJAX error: ' + error);
            }
        });
    });

    $('#btnAddAttachment').on('click', function () {
        let fileName = $('#txtFileName').val().trim();
        const fileInput = $('#fileUpload')[0].files[0];

        if (!fileName || !fileInput) {
            toastr.error("Please provide both file name and file.");
            return;
        }

        const extension = fileInput.type.split('/')[1];

        if (!fileName.toLowerCase().endsWith(`.${extension}`)) {
            fileName = `${fileName}.${extension}`;
        }

        const reader = new FileReader();
        reader.onload = function (e) {
            let fullBase64 = e.target.result;
            let base64File = fullBase64.split(',')[1];

            const attachment = {
                ATTACHMENT: fileName,
                FILE_NAME: base64File,
                MIME_TYPE: fileInput.type
            };
            rowsAttachment.push(attachment);

            const isImage = fileInput.type.startsWith('image/');
            let previewHtml = '';
            let tdStyle = '';

            if (isImage) {
                tdStyle = 'style="width: 100px; height: 100px; border-radius: 50%; border: 2px solid #ccc; overflow: hidden; text-align: center; vertical-align: middle;"';
                previewHtml = `<img src="${fullBase64}" alt="${fileName}" style="height: 100%; width: 100%; border-radius: 50%; object-fit: cover;" />`;
            } else {
                previewHtml = `<a href="${fullBase64}" target="_blank">Preview</a>`;
            }

            const row = `
                <tr data-filename="${fileName}" style="height: 100px;">
                    <td style="vertical-align: middle;">${fileName}</td>
                    <td ${tdStyle}>${previewHtml}</td>
                    <td style="vertical-align: middle;">
                        <i class="fa fa-trash text-danger cursor-pointer btn-delete-attachment"></i>
                    </td>
                </tr>`;

            $('#tblAttachmentPBPE tbody').append(row);

            // Clear inputs
            $('#txtFileName').val('');
            $('#fileUpload').val('');
        };

        reader.readAsDataURL(fileInput);
    });

    $('#tblAttachmentPBPE').on('click', '.btn-delete-attachment', function () {
        const row = $(this).closest('tr');
        const fileName = row.data('filename');

        rowsAttachment = rowsAttachment.filter(item => item.ATTACHMENT !== fileName);

        row.remove();
    });

    $('#tblAttachmentPBPE').on('click', '.btn-delete-attachment', function () {
        const row = $(this).closest('tr');
        const fileName = row.data('filename');

        rowsAttachment = rowsAttachment.filter(item => item.FileName !== fileName);

        row.remove();
    });
}

//=====================GENERATE VNO=============================
async function GetVNo(vType) {
    try {
        const res = await fetch(`/PurchaseBillPassEntry/GetVNo?vType=${encodeURIComponent(vType)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();

        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=====================DROPDOWNS=============================
//Commmented By Sumesh
function loadDocTypeList() {
    docTypeMap = {};
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetDocTypeList',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var ddl = $('#ddlDocType');
            ddl.empty();
            ddl.append('<option value="">-- Select Doc Type --</option>');

            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.value + '">' + item.text + '</option>');
                docTypeMap[item.value] = item.text;
            });
        },
        error: function (xhr, status, error) {
            toastr.error('Error loading DocType list: ' + error);
        }
    });
}

function loadMRNList(vType) {

    // If no Doc Type selected, initialize empty dropdown
    if (!vType) {

        InitializeERPSingleDropdown({
            id: "TxtMRNNo2",
            placeholder: "Select MRN",
            data: []
        });

        $('#TxtMRNNo1').val('');
        return;
    }

    $.ajax({
        url: '/PurchaseBillPassEntry/GetMrnNoList',
        type: 'GET',
        dataType: 'json',
        data: { vType: vType },

        success: function (res) {

            if (!res.success)
                return;

            const dropdownData = res.data.map(item => ({
                id: item.Value,
                text: item.Text,
                vType: item.vType
            }));

            InitializeERPSingleDropdown({

                id: "TxtMRNNo2",

                placeholder: "Select MRN",

                data: dropdownData,

                onChange: function (selectedItem) {

                    $('#TxtMRNNo1').val(selectedItem.vType);

                }

            });

        },

        error: function (xhr, status, error) {

            toastr.error("Error loading MRN List : " + error);

        }

    });

}

function loadPartyListNatureSupplier(dropdownId, isInitSelect2 = false) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetPartyListNatureSupplier',
        type: 'GET',
        dataType: 'json',
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        res.data.forEach(item => {
            ddl.append(`<option value="${item.Value}">${item.Text}</option>`);
        });
        if (isInitSelect2) {
            initSelect2(ddl);
        }
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Party list: ' + error);
        return [];
    });
}

function loadDrAcListByVtype(dropdownId, vType) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetDrAcListByVtype',
        type: 'GET',
        dataType: 'json',
        data: { vType: vType }
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        res.data.forEach(item => {
            ddl.append(`<option value="${item.Value}">${item.Text}</option>`);
        });
        initSelect2(ddl);
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Dr Ac list: ' + error);
        return [];
    });
}

function loadPartyDrCrAcList(dropdownId) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetPartyDrCrAcList',
        type: 'GET',
        dataType: 'json',
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        res.data.forEach(item => {
            ddl.append(`<option value="${item.Value}">${item.Text}</option>`);
        });
        initSelect2(ddl);
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Cr Ac list: ' + error);
        return [];
    });
}

function loadTranGSTByFrtCrAc(frtCrAcCode) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetTranGSTByFrtCrAc',
        type: 'GET',
        dataType: 'json',
        data: { frtCrAcCode: frtCrAcCode }
    }).then(function (res) {
        const ddl = $('#ddlTransportGSTNo');
        ddl.empty();
        ddl.append('<option value="">-- Select GST--</option>');
        res.data.forEach(item => {
            ddl.append(`<option value="${item.Value}">${item.Text}</option>`);
        });
        initSelect2(ddl);
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading GST: ' + error);
        return [];
    });
}

function loadItemList(dropdownId) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetItemList',
        type: 'GET',
        dataType: 'json',
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        res.data.forEach(item => {
            ddl.append(`<option value="${item.Value}">${item.Text}</option>`);
        });
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Item list: ' + error);
        return [];
    });
}

function loadAddList(shipFromCode) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetAddList',
        type: 'GET',
        dataType: 'json',
        data: { shipFromCode: shipFromCode },
        success: function (res) {
            const data = res.data;
            var ddl = $('#ddlShipFromAddress');
            ddl.empty();
            ddl.append('<option value="">-- Select Address --</option>');

            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.Value + '">' + item.Text + '</option>');
            });
        },
        error: function (xhr, status, error) {
            toastr.error('Error loading Address list: ' + error);
        }
    });
}

function loadCityList(dropdownId) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetCityList',
        type: 'GET',
        success: function (res) {
            const data = res.data;
            var ddl = $(dropdownId);
            ddl.empty();
            ddl.append('<option value="">-- Select City --</option>');
            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.Value + '">' + item.Text + '</option>');
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading city list: ' + error);
        }
    });
}

function loadStateList(dropdownId) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetStateList',
        type: 'GET',
        success: function (res) {
            const data = res.data;
            var ddl = $(dropdownId);
            ddl.empty();
            ddl.append('<option value="">-- Select State --</option>');
            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.Value + '">' + item.Text + '</option>');
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading state list: ' + error);
        }
    });
}

function loadCurrencyList() {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetCurrencyList',
        type: 'GET',
        success: function (res) {
            const data = res.data;
            var ddl = $('#ddlCurrency');
            ddl.empty();
            ddl.append('<option value="">-- Select Currency --</option>');
            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.Value + '">' + item.Text + '</option>');
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading currency list: ' + error);
        }
    });
}

function loadTaxTypeList(dropdownId) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetTaxList',
        type: 'GET',
        dataType: 'json',
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        console.log("Tax list: ", res.data);
        res.data.forEach(item => {
            //const text = `${item.Text} | ${item.CGST_PER} | ${item.SGST_PER} | ${item.IGST_PER} | ${item.VAT_PER} | ${item.TDS_PER} | ${item.TCS_PER} | ${item.OTH_PER} | ${item.OTH_PER2}`;
            const f = v => Number(v || 0).toFixed(4);

            const text = `${item.Text} | ${f(item.CGST_PER)} | ${f(item.SGST_PER)} | ${f(item.IGST_PER)} | ${f(item.VAT_PER)} | ${f(item.TDS_PER)} | ${f(item.TCS_PER)} | ${f(item.OTH_PER)} | ${f(item.OTH_PER2)}`;

            ddl.append(`
                <option
                    value="${item.Value}"
                    data-cgst="${item.CGST_PER}"
                    data-sgst="${item.SGST_PER}"
                    data-igst="${item.IGST_PER}"
                    data-vat="${item.VAT_PER}"
                    data-tds="${item.TDS_PER}"
                    data-tcs="${item.TCS_PER}"
                    data-oth="${item.OTH_PER}"
                    data-oth2="${item.OTH_PER2}">
                    ${text}
                </option>
            `);
        });
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading tax list: ' + error);
        return [];
    });
}

function loadStatusList() {
    statusMap = {};
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetStatusList',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var ddl = $('#ddlStatus');
            ddl.empty();
            ddl.append('<option value="">-- Select Status --</option>');

            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.value + '">' + item.text + '</option>');
                statusMap[item.value] = item.text;
            });
        },
        error: function (xhr, status, error) {
            toastr.error('Error loading Status list: ' + error);
        }
    });
}

function loadTransportList() {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetTransportList',
        type: 'GET',
        success: function (res) {
            var ddl = $('#ddlTransportName');
            ddl.empty();
            ddl.append('<option value="">-- Select City --</option>');
            $.each(res, function (index, item) {
                ddl.append('<option value="' + item.value + '">' + item.text + '</option>');
            });
            initSelect2(ddl);
        },
        error: function (xhr, status, error) {
            console.error('Error loading city list: ' + error);
        }
    });
}

function convertToDateInputFormat(dateTimeStr) {
    if (!dateTimeStr) return '';

    var datePart = dateTimeStr.split(' ')[0];
    var parts = datePart.split('/');

    if (parts.length !== 3) return '';

    var day = parts[0].padStart(2, '0');
    var month = parts[1].padStart(2, '0');
    var year = parts[2];

    return `${year}-${month}-${day}`;
}

//function to LOAD DATA from QUOTATION 1,2,3 Table
function loadFullQuotationByVno(VNo) {
    $.ajax({
        url: '/PurchaseBillPassEntry/GetFullQuotationByVno',
        type: 'GET',
        data: { vNo: VNo },
        success: function (res) {
            console.log(res);
            if (!res.success || !res.header) {
                toastr.warning("Quotation not found.");
                return;
            }
            const jsonData = res.header;
            const items = res.items || [];
            const attachments = res.attachments || [];

            // Loading for QUOTATION1/Header
            //Commented By Sumesh
            //$('#ddlDocType').val(jsonData.v_TYPE);
            //Added By Sumesh
            ERPSingleDropdownManager["ddlDocType"]?.SetValue(jsonData.v_TYPE);
            $('#DtDocDate').val(jsonData.v_DATE ? jsonData.v_DATE.split('T')[0] : '');
            $('#NumDocNo').val(jsonData.v_NO);
            $('#TxtMRNNo1').val(jsonData.v_TYPE);
            //Commented By Sumesh
            //$('#TxtMRNNo2').val(jsonData.v_NO);
            //Added By Sumesh
            loadMRNList(jsonData.v_TYPE).then(function () {

                ERPSingleDropdownManager["TxtMRNNo2"]?.SetValue(jsonData.v_NO);

            });
            //ERPSingleDropdownManager["TxtMRNNo2"]?.SetValue(jsonData.v_NO);

            $('#ddlBillFrom').val(jsonData.partY_CODE);
            $('#TxtAdd1PD').val(jsonData.bilL_ADD1);
            $('#TxtAdd2PD').val(jsonData.bilL_ADD2);
            $('#TxtAdd3PD').val(jsonData.bilL_ADD3);
            $('#ddlCityPD').val(jsonData.bilL_CITY);
            $('#TxtGSTNo').val(jsonData.bilL_GST);

            $('#TxtDispFromAdd').val(jsonData.disP_ADDRESS);
            $('#ddlDispCity').val(jsonData.disP_CITY);

            $('#ddlShipFrom1').val(jsonData.partY_CODE);
            $('#ddlShipFromAddress').val(jsonData.shiP_ADD1);
            $('#TxtAdd1SF').val(jsonData.shiP_ADD1);
            $('#TxtAdd2SF').val(jsonData.shiP_ADD2);
            $('#TxtAdd3SF').val(jsonData.shiP_ADD3);
            $('#ddlCitySF').val(jsonData.shiP_CITY);
            $('#TxtGSTNoSF').val(jsonData.shiP_GST);

            $('#TxtBillNo').val(jsonData.bilL_NO);
            $('#DtBillDate').val(jsonData.bilL_DATE ? jsonData.bilL_DATE.split('T')[0] : '');
            $('#TxtChallanNo').val(jsonData.chalL_NO);
            $('#DtChDate').val(jsonData.chalL_DATE ? jsonData.chalL_DATE.split('T')[0] : '');
            $('#TxtWaybillNo').val(jsonData.waybilL_NO);
            $('#DtWaybillDate').val(jsonData.gR_DATE ? jsonData.gR_DATE.split('T')[0] : '');
            $('#TxtWayBillInvNo').val(jsonData.ewB_INVNO);
            $('#DtWaybillExpiry').val(jsonData.ewB_EXPDATE ? jsonData.ewB_EXPDATE.split('T')[0] : '');
            $('#NumExRate').val(jsonData.excH_RATE);
            $('#ddlInputType').val(jsonData.inpuT_TYPE);

            $('#ddlDebitAC').val(jsonData.debiT_AC);
            $('#ddlCreditAC').val(jsonData.crediT_AC);

            $('#txtRemarks').val(jsonData.remarks);
            $('#TxtNetAmount').val(jsonData.namount);

            // Optional - set status if your dropdown supports values
            $('#ddlStatus').val(jsonData.status);


            // // Loading for Purchase1/items,
            // if (!Array.isArray(items) || items.length === 0) {
            //     addNewRowBelow();
            // } else {
            //     Promise.all([
            //         loadItemList(compCode, '#ddlItemList'),
            //         loadUOMList('#ddlUOMList')
            //     ]).then(([itemMap, itemMakeMap, taxCodeMap,uomMap]) => {
            //         items.forEach((row, i) => {
            //             rowIndex++;
            //             const isLastRow = (i === items.length - 1);
            //             const rowHtml = generateRowHtml(row, i, itemMap, itemMakeMap, taxCodeMap,uomMap,isLastRow);
            //             $('#tblItemRecordPBPE tbody').append(rowHtml);
            //         });
            //     }).catch(err => {
            //         toastr.error("Failed to load item metadata: " + err);
            //     });
            // }

            // Load dropdowns before populating items
            if (!Array.isArray(items) || items.length === 0) {
                addNewRowBelow();
            } else {
                Promise.all([
                    loadItemList('#ddlItemList'),
                    //loadUOMList('#ddlUOMList')
                ]).then(([itemListRes, itemMakeMap, taxCodeMap, uomListRes]) => {
                    items.forEach((row, i) => {
                        rowIndex++;
                        const isLastRow = (i === items.length - 1);
                        const rowHtml = generateRowHtml(
                            row,
                            i,
                            itemListRes,
                            itemMakeMap,
                            taxCodeMap,
                            uomListRes,
                            isLastRow
                        );
                        $('#tblItemRecordPBPE tbody').append(rowHtml);
                    });
                }).catch(err => {
                    toastr.error("Failed to load item metadata: " + err);
                });
            }


            // Loading for PURCHASE3/Attachment
            const attachBody = $('#tblAttachmentPBPE tbody');
            attachBody.empty();

            if (attachments.length === 0) {
                attachBody.append('<tr><td colspan="3" class="text-center text-muted">No attachments found.</td></tr>');
            } else {
                attachments.forEach((att, idx) => {
                    const fileName = att.attachment?.split('/').pop() || `Attachment_${idx + 1}`;
                    const base64File = (att.FILE_NAME || att.filE_NAME || '').trim(); // May be base64

                    if (!base64File) {
                        console.warn(`Empty base64 for file: ${fileName}`);
                        return;
                    }

                    // Determine file extension
                    const extension = fileName.split('.').pop()?.toLowerCase() || '';
                    let guessedMime = 'application/octet-stream';

                    if (['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'].includes(extension)) {
                        guessedMime = `image/${extension === 'jpg' ? 'jpeg' : extension}`;
                    } else if (extension === 'pdf') {
                        guessedMime = 'application/pdf';
                    }

                    const fullBase64 = `data:${guessedMime};base64,${base64File}`;

                    // DEBUG: log the final base64 string and MIME
                    console.log(`Attachment #${idx + 1}:`, {
                        fileName,
                        mime: guessedMime,
                        base64Preview: fullBase64.substring(0, 50) + '...' // show only start for preview
                    });

                    let filePreview = '';
                    let tdStyle = 'style="text-align: center; vertical-align: middle;"';

                    // Render based on MIME type
                    if (guessedMime.startsWith('image/')) {
                        filePreview = `
                                <img src="${fullBase64}"
                                     alt="${fileName}"
                                     style="max-width: 100px; max-height: 100px; border-radius: 4px; object-fit: contain; border: 1px solid #ccc;" />`;
                    } else if (guessedMime === 'application/pdf') {
                        filePreview = `<a href="${fullBase64}" target="_blank" class="btn btn-sm btn-primary">View PDF</a>`;
                    } else {
                        filePreview = `<a href="${fullBase64}" download="${fileName}" class="btn btn-sm btn-secondary">Download File</a>`;
                    }

                    const row = `
                            <tr data-filename="${fileName}" style="height: 100px;">
                                <td style="vertical-align: middle;">${fileName}</td>
                                <td ${tdStyle}>${filePreview}</td>
                                <td style="vertical-align: middle;">
                                    <i class="fa fa-trash text-danger cursor-pointer btn-delete-attachment" title="Delete"></i>
                                </td>
                            </tr>
                        `;

                    attachBody.append(row);
                });
            }


        },
        error: function (xhr) {
            toastr.error("Failed to load quotation: " + xhr.responseText);
        }
    });
}

function addNewRowBelow() {

    let lastRow = $('#tblItemRecordPBPE tbody tr:last');
    if (lastRow.length > 0) {
        let lastRowId = lastRow.find('select[id^="ITEM_NAME"]').attr('id');
        if (lastRowId) {
            let lastIndex = lastRowId.split('_')[1];

            let itemNameVal = $(`#ITEM_NAME_${lastIndex}`).val();
            let itemCodeVal = $(`#HSN_CODE_${lastIndex}`).val();

            if (!itemNameVal) {
                toastr.warning('Please select an Item Name before adding a new row.');
                $(`#ITEM_NAME_${lastIndex}`).focus();
                return;
            }
            if (!itemCodeVal || itemCodeVal.trim() === '') {
                toastr.warning('Please enter an Item Code before adding a new row.');
                $(`#HSN_CODE_${lastIndex}`).focus();
                return;
            }
        }
    }

    rowIndex++;

    const newRowData = {
        ITEM_NAME: '',
        HSN_CODE: '',
        UOM_NAME: '',
        NOS: '',
        RECD_QTY: '',
        BILL_QTY: '',
        USD_RATE: '',
        EXCH_RATE: '',
        RATE: '',
        AMOUNT: '',
        RCM_YN: '',
        INPUT_YN: '',
        TAX_CODE: '',
        PACK_PER: '',
        PACK_AMT: '',
        DISC_PER: '',
        DISC_AMT: '',
        CGST_PER: '',
        CGST_AMT: '',
        SGST_PER: '',
        SGST_AMT: '',
        IGST_PER: '',
        IGST_AMT: '',
        CESS_PER: '',
        CESS_AMT: '',
        VAT_PER: '',
        VAT_AMT: '',
        OTH_AMT: '',
        NET_AMT: '',
        MAKE_CODE: '',
        DEPT_CODE: '',
        REMARKS: '',
        LAND_RATE: '',
        LAND_AMT: '',
        POLAND_RATE: '',
        PO_RATE: '',
        PO_TYPE: '',
        PO_NO: '',
        KANTA_TYPE: '',
        KANTA_NO: '',
        REQ_TYPE: '',
        REQ_NO: '',
        REF_TYPE: '',
        REF_NO: ''
    };

    rowsData.push(newRowData);

    const rowHtml = `
        <tr>
            <td class="freeze-item">
                <select style="min-width: 150px; max-width: 300px;"  class="form-control" id="ITEM_NAME_${rowIndex}" name="ITEM_NAME[${rowIndex}]" class="form-control-sm">
                    <option value="">-- Select Item --</option>
                </select>
            </td>   
            <td><input style="min-width: 100px; max-width: 200px;" class="form-control" id="HSN_CODE_${rowIndex}" name="HSN_CODE[${rowIndex}]" type="text" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="UOM_NAME_${rowIndex}" name="UOM_NAME[${rowIndex}]" type="number" class="form-control-sm" disabled/></td>            
            <td><input style="min-width: 100px; max-width: 200px;"  class="form-control" id="NOS_${rowIndex}" name="NOS[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="RECD_QTY_${rowIndex}" name="RECD_QTY[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="BILL_QTY_${rowIndex}" name="BILL_QTY[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="USD_RATE_${rowIndex}" name="USD_RATE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="EXCH_RATE_${rowIndex}" name="EXCH_RATE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="RATE_${rowIndex}" name="RATE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="AMOUNT_${rowIndex}" name="AMOUNT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td>
                <select style="min-width: 100px; max-width: 200px;" class="form-control" id="RCM_YN_${rowIndex}" name="RCM_YN[${rowIndex}]" class="form-control-sm">
                    <option value="">-- Select --</option>
                    <option value="YES">YES</option>
                    <option value="NO">NO</option>
                </select>
            </td>
            <td>
                <select style="min-width: 100px; max-width: 200px;" class="form-control" id="INPUT_YN_${rowIndex}" name="INPUT_YN[${rowIndex}]" class="form-control-sm">
                    <option value="">-- Select --</option>
                    <option value="YES">YES</option>
                    <option value="NO">NO</option>
                </select>

            </td>
            <td>
                 <select style="min-width: 100px; max-width: 200px;" class="form-control" id="TAX_CODE_${rowIndex}" name="TAX_CODE[${rowIndex}]" class="form-control-sm">
                    <option value="">-- Select Tax Type --</option>
                </select>
            </td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="PACK_PER_${rowIndex}" name="PACK_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="PACK_AMT_${rowIndex}" name="PACK_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="DISC_PER_${rowIndex}" name="DISC_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="DISC_AMT_${rowIndex}" name="DISC_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="CGST_PER_${rowIndex}" name="CGST_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="CGST_AMT_${rowIndex}" name="CGST_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="SGST_PER_${rowIndex}" name="SGST_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="SGST_AMT_${rowIndex}" name="SGST_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="IGST_PER_${rowIndex}" name="IGST_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="IGST_AMT_${rowIndex}" name="IGST_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="CESS_PER_${rowIndex}" name="CESS_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="CESS_AMT_${rowIndex}" name="CESS_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="VAT_PER_${rowIndex}" name="VAT_PER[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="VAT_AMT_${rowIndex}" name="VAT_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="OTH_AMT_${rowIndex}" name="OTH_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="NET_AMT_${rowIndex}" name="NET_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="MAKE_CODE_${rowIndex}" name="MAKE_CODE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="DEPT_CODE_${rowIndex}" name="DEPT_CODE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 100px; max-width: 200px;"  class="form-control" id="REMARKS_${rowIndex}" name="REMARKS[${rowIndex}]" type="text" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="LAND_RATE_${rowIndex}" name="LAND_RATE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="LAND_AMT_${rowIndex}" name="LAND_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="POLAND_RATE_${rowIndex}" name="POLAND_RATE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="PO_RATE_${rowIndex}" name="PO_RATE[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 100px; max-width: 200px;"  class="form-control" id="PO_TYPE_${rowIndex}" name="PO_TYPE[${rowIndex}]" type="text" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="PO_NO_${rowIndex}" name="PO_NO[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 100px; max-width: 200px;"  class="form-control" id="KANTA_TYPE_${rowIndex}" name="KANTA_TYPE[${rowIndex}]" type="text" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="KANTA_NO_${rowIndex}" name="KANTA_NO[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 100px; max-width: 200px;"  class="form-control" id="REQ_TYPE_${rowIndex}" name="REQ_TYPE[${rowIndex}]" type="text" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="REQ_NO_${rowIndex}" name="REQ_NO[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 100px; max-width: 200px;"  class="form-control" id="REF_TYPE_${rowIndex}" name="REF_TYPE[${rowIndex}]" type="text" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="REF_NO_${rowIndex}" name="REF_NO[${rowIndex}]" type="number" class="form-control-sm" /></td>


            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="DR_NOTE_AMT_${rowIndex}" name="DR_NOTE_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="CR_NOTE_AMT_${rowIndex}" name="CR_NOTE_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="QLTY_DIFF_DR_AMT_${rowIndex}" name="QLTY_DIFF_DR_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="RATE_DIFF_DR_AMT_${rowIndex}" name="RATE_DIFF_DR_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="QC_DIFF_DR_AMT_${rowIndex}" name="QC_DIFF_DR_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="QTY_DIFF_DR_AMT_${rowIndex}" name="QTY_DIFF_DR_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td><input style="min-width: 50px; max-width: 200px;"  class="form-control" id="OTHER_DR_AMT_${rowIndex}" name="OTHER_DR_AMT[${rowIndex}]" type="number" class="form-control-sm" /></td>
            <td class="action-col">
                <i class="fas fa-trash ms-2 text-danger" onclick="deleteRow(this)" style="cursor:pointer;"></i>
                <i class="fas fa-plus-circle ms-2 text-success add-row-icon" onclick="addNewRowBelow()" style="cursor:pointer;"></i>
            </td>
        </tr>`;
    // Remove existing add icons from all rows (but leave delete icons untouched)
    $('#tblItemRecordPBPE tbody tr .add-row-icon').remove();
    $('#tblItemRecordPBPE tbody').append(rowHtml);
    // Load dropdowns for new row
    loadItemList(`#ITEM_NAME_${rowIndex}`);
    //loadUOMList(`#UOM_NAME_${rowIndex}`);
    loadTaxTypeList(`#TAX_CODE_${rowIndex}`);
}

function deleteRow(el) {
    // Remove row
    $(el).closest('tr').remove();

    // Remove existing add buttons from all rows
    $('#tblItemRecordPBPE tbody tr .add-row-icon').remove();

    // Add add-icon to the last row only (if any rows left)
    const lastRow = $('#tblItemRecordPBPE tbody tr:last');
    if (lastRow.length) {
        const actionCell = lastRow.find('td:last');
        actionCell.append(`
                <i class="fas fa-plus-circle ms-2 text-success add-row-icon" onclick="addNewRowBelow()" style="cursor:pointer;"></i>
            `);
    }
}

function parseNullableDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    return isNaN(date.getTime()) ? null : date.toISOString();
}

//==============================================Helper===============================
function initSelect2($ddl) {
    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true
    });
    $ddl.on('select2:open', function () {
        setTimeout(function () {
            let searchBox = document.querySelector('.select2-container--open .select2-search__field');

            if (searchBox) {
                searchBox.focus();
            }
        }, 0);
    });
}

function toggleDate() {

    $('.erppage-checkbox-input').each(function () {

        const chk = $(this);
        const dateInput = chk.closest('.erppage-datebox').find('input[type="date"]');

        if (!dateInput.length) return;

        // Initial state
        dateInput.prop('disabled', !chk.is(':checked'));

        // Toggle on change
        chk.on('change', function () {
            dateInput.prop('disabled', !this.checked);
        });

    });

}



//============================================ MRN No Change=============================
$('#TxtMRNNo2').on('change', function () {

    var mrnNo = $("#TxtMRNNo2").val().trim();

    if (mrnNo === "")
        return;

    $.ajax({
        url: "/PurchaseBillPassEntry/ValidateMRN",
        type: "POST",
        data: {
            mrnNo: mrnNo,
            vType: $("#ddlDocType").val(),
            vNo: $("#NumDocNo").val()
        },
        dataType:'json',
        success: function (response) {

            if (!response.success) {

                showToast(response.message, {type:"warning"});

                //clearForm();

                $("#TxtMRNNo2").focus();
                return;
            }

            //Fill Correct MRN
            $("#TxtMRNNo2").val(response.mrnNo);
            //$("#TxtRefType").val(response.refType);

            //Load MRN Data
            //loadMRNData(response.refType, response.mrnNo);

        },
        error: function () {
            alert("Error occurred.");
        }

    });

});
function clearForm() {

    $("#tblItemRecordPBPE tbody").empty();

    $("#ddlBillFrom").val("");
    $("#TxtBillNo").val("");
    $("#DtBillDate").val("");

    $("#TxtChallanNo").val("");
    $("#DtChDate").val("");
    $("#TxtWayBillNo").val("");


    $("#TxtBLNo").val("");



    $("#ddlBillFromAddress").val("");
    $("#TxtAdd1PD").val("");
    $("#TxtAdd2PD").val("");
    $("#TxtAdd3PD").val("");
    $("#ddlCityPD").val("");
    $("#ddlStatePD").val("");
    $("#NumPincodeBL").val("");
    $("#TxtGSTNo").val("");

    $("#TxtShipTo").val("");
    $("#TxtShipToAdd1").val("");
    $("#TxtShipToAdd2").val("");
    $("#TxtShipToAdd3").val("");

    $("#TxtRemarks").val("");

    $("#TxtVehicleNo").val("");
    $("#TxtContainerNo").val("");
    $("#TxtGRNo").val("");

    $("#TxtRefType").val("");
    $("#TxtMRNNo").val("");

    $("#TxtReason").val("");

}