
let pubDefPOInMRN = null;
let compCode = null;

const rowsData = [];
let rowsAttachment = [];
let rowIndex = 0;

const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const isReadOnly = urlParams.get('readOnly') === 'true';

//const compCode = window.compCode;
const branchCode = window.branchCode;
const yearCode = window.yearCode;

$(document).ready(function () {
    $('#ddlDocType').focus();
    //Added by Sumesh
    //InitializeERPSingleDropdown({
    //    id: "TxtMRNNo2",
    //    placeholder: "Select MRN",
    //    data: []
    //});

    toggleDate();
    //getGlobalDetails();

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
    $('#DtDocDate, #DtBillDate, #DtGRDate, #DtBillDateLD, #DtHoldDateCRDRNote, #DtBLDate, #Dtsysdate, #DtChDate, #DtPlDate, #DtHoldDate').val(currentDate);
    wireEvents();
});
//=====================EVENTS=============================
function wireEvents() {
    //------------ VType Change --------
    $('#ddlDocType').on('change', function () {
        const vType = $(this).val();
        console.log("vType: ", vType);
        GetVNo(vType);
        loadMRNList(vType)
        $('#TxtMRNNo1').val('');
        loadDrAcListByVtype('#ddlDebitAC', vType);
    })

    //--------------- MRN Change ------------
    $('#TxtMRNNo2').on('change', function () {
        var mrnTypeNo = $(this).find("option:selected").text().trim();

        var mrnType = $(this).find("option:selected").data("vtype");
        if (mrnTypeNo === "")
            return;

        $.ajax({
            url: "/PurchaseBillPassEntry/ValidateMRN",
            type: "POST",
            data: {
                mrnTypeNo: mrnTypeNo,
                vType: $("#ddlDocType").val(),
                vNo: $("#NumDocNo").val()
            },
            dataType: 'json',
            success: function (response) {

                if (!response.success) {

                    showToast(response.message, { type: "warning" });

                    //clearForm();

                    $("#TxtMRNNo2").focus();
                    return;
                }

                //Fill Correct MRN
                $("#TxtMRNNo2").val(response.mrnNo);
                $('#TxtMRNNo1').val(mrnType);

                //Load MRN Data
                LoadMRNData(mrnType, response.mrnNo);

            },
            error: function () {
                alert("Error occurred.");
            }

        });

    });

    //---------- Frieght Cr Ac Change --------
    $('#ddlFreightCreditAC').on('change', function () {
        const frtCrAcCode = $(this).val();
        loadTranGSTByFrtCrAc(frtCrAcCode);
    })

    //---------- Ship List Change ------
    $('#ddlShipFrom1').on('change', function () {
        const shipFromCode = $(this).val();
        loadAddList(shipFromCode, '#ddlShipFromAddress');
    })

    //--------- Bill From List Change --------
    $('#ddlBillFrom').on('change', function () {
        const billFromCode = $(this).val();
        loadAddList(billFromCode, '#ddlBillFromAddress');
    })

    //$('#TxtAdd1PD').change(function () {
    //    var selectedVal = $(this).val();
    //    var selectedtxt = $(this).text();
    //    var PCode = $(this).data('pcode');

    //    $.ajax({
    //        url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
    //        type: 'GET',
    //        data: { cCode: compCode, pCode: PCode, addressId: selectedVal },
    //        success: function (response) {
    //            var res = response.addressDetails;
    //            $('#TxtAdd2PD').val(res.add2);
    //            $('#TxtAdd3PD').val(res.add3);
    //            $('#TxtGSTNo').val(res.gstin);
    //            if ($('#ddlCityPD option[value="' + res.cityCode + '"]').length === 0) {
    //                $('#ddlCityPD').append($('<option>', {
    //                    value: res.cityCode,
    //                    text: res.cityName
    //                }));
    //            }
    //            $('#ddlCityPD').val(res.cityCode);

    //            // shipto address
    //            $('#TxtAdd1SF').val(res.add1);
    //            $('#TxtAdd2SF').val(res.add2);
    //            $('#TxtAdd3SF').val(res.add3);
    //            $('#TxtGSTNoSF').val(res.gstin);
    //            if ($('#ddlCitySF option[value="' + res.cityCode + '"]').length === 0) {
    //                $('#ddlCitySF').append($('<option>', {
    //                    value: res.cityCode,
    //                    text: res.cityName
    //                }));
    //            }
    //            $('#ddlCitySF').val(res.cityCode);
    //        },
    //        error: function (xhr, status, error) {
    //            toastr.error('Error loading Item make: ' + error);
    //        }
    //    });
    //})

    //---------- Ship Address Change -----------
    $('#ddlShipFromAddress').change(function () {
        var selectedVal = $(this).val();
        var code = $('#ddlShipFrom1').val();

        $.ajax({
            url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
            type: 'GET',
            data: { code: code, addressId: selectedVal },
            success: function (response) {
                var res = response.addressDetails;
                // shipto address
                $('#TxtAdd1SF').val(res.add1);
                $('#TxtAdd2SF').val(res.add2);
                $('#TxtAdd3SF').val(res.add3);
                $('#TxtGSTNoSF').val(res.gstin);
                $('#TxtPincodeSF').val(res.pincode);
                loadCityList('#ddlCitySF', res.cityCode)
            },
            error: function (xhr, status, error) {
                toastr.error('Error loading ship address: ' + error);
            }
        });
    })

    //---------- Bill Address Change ----------
    $('#ddlBillFromAddress').change(function () {
        var selectedVal = $(this).val();
        var code = $('#ddlBillFrom').val();

        $.ajax({
            url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
            type: 'GET',
            data: { code: code, addressId: selectedVal },
            success: function (response) {
                var res = response.addressDetails;
                // shipto address
                $('#TxtAdd1PD').val(res.add1);
                $('#TxtAdd2PD').val(res.add2);
                $('#TxtAdd3PD').val(res.add3);
                $('#TxtAdd3PD').val(res.gstin);
                $('#NumPincodeBL').val(res.pincode);
                loadCityList('#ddlCityPD', res.cityCode)
            },
            error: function (xhr, status, error) {
                toastr.error('Error loading bill address: ' + error);
            }
        });
    })

    //--------- Ship City Change ----------
    $('#ddlCitySF').on('change', function () {
        const cCode = parseInt($(this).val()) || 0;
        loadStateList('#ddlStateSF', cCode);
    })

    //-------- Bill City Change ----------
    $('#ddlCityPD').on('change', function () {
        const cCode = parseInt($(this).val()) || 0;
        loadStateList('#ddlStatePD', cCode);
    })

    //--------- Save Click --------
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

    //---------- Attachment handler --------
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

    ////--------- Item Change ---------
    $(document).on("change", ".item-name", function () {

        const $row = $(this).closest("tr");

        const selectedOption = $(this).find("option:selected");

        const uomCode = selectedOption.data("ucode");
        const uomName = selectedOption.data("unit");

        $row.find(".uom-code").val(uomCode || "");
        $row.find(".uom-name").val(uomName || "");
    });

    //--------- Calculation on row values change change ------------
    $(document).on("change", ".usd-rate,.exch-rate,.rate,.bill-qty, .recd-qty, .pack-per, .disc-per, .cess-per, .oth-amt, .pack-amt, .disc-amt, .cgst-amt, " +
        ".sgst-amt, .igst-amt, .cess-amt, .vat-per, .vat-amt", async function () {

        await processRow($(this).closest("tr"), {
            calculateAmount: true,
            calculateTaxes: true
        });
    });

    //--------- Calculation on amount change ------------
    $(document).on("change", ".amount", async function () {

        const $row = $(this).closest("tr");

        const qty = parseFloat($row.find(".bill-qty").val()) || 0;
        const amount = parseFloat($row.find(".amount").val()) || 0;

        const rate = qty > 0
            ? amount / qty
            : 0;

        $row.find(".rate").val(rate.toFixed(6));

        await processRow($(this).closest("tr"), {
            calculateAmount: true,
            calculateTaxes: true
        });
    });

    //--------- Calculation on cgst%, sgst%, igst% change ------------
    $(document).on("change", ".cgst-per,.sgst-per,.igst-per", async function () {
        await processRow($(this).closest("tr"), {
            calculateTaxes: true
        });
    });
}

async function processRow($row, {calculateAmount = false, calculateTaxes = false} = {}) {

    const itemCode = Number($row.find(".item-name").val()) || 0;

    if (calculateAmount) {
        await calculateAmt($row, itemCode);
    }

    if (calculateTaxes) {
        calculateTax($row, itemCode);
    }

    calculateItemTotals();
    calculateLandAmount($row, itemCode);
    await CalcDrCrNote();
}
//------------- GENERATE VNO -----------------
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
//--------- DOCTYPE -----------
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

//--------- MRN -----------
function loadMRNList(vType) {
    $.ajax({
        url: '/PurchaseBillPassEntry/GetMrnNoList',
        type: 'GET',
        dataType: 'json',
        data: { vType: vType },
        success: function (res) {
            if (res.success) {
                console.log("mrnList: ", res.data);
                var ddl = $('#TxtMRNNo2');
                ddl.empty();
                ddl.append('<option value="">--Select MRN No--</option>');

                $.each(res.data, function (index, item) {
                    ddl.append(`<option data-vtype="${item.vType}" value="${item.Value}">${item.Text}</option>`);
                });
                //initSelect2(ddl);
            }
        },
        error: function (xhr, status, error) {
            toastr.error('Error loading MRN list: ' + error);
        }
    });
}

//--------- SUPPLIER PARTY -----------
function loadPartyListNatureSupplier(dropdownId, isInitSelect2 = false, selectedValue = null) {
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
        // Set selected value if provided
        if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
            ddl.val(selectedValue).trigger('change');
        }
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Party list: ' + error);
        return [];
    });
}

//--------- DR AC BY VTYPE-----------
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

//--------- PARTY DR CR -----------
function loadPartyDrCrAcList(dropdownId, selectedValue = null) {
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
        // Set selected value if provided
        if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
            ddl.val(selectedValue).trigger('change');
        }
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Cr Ac list: ' + error);
        return [];
    });
}

//--------- TRANSPORT GST -----------
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

//--------- ITEM -----------
function loadItemList(dropdownId, selectedValue = null) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetItemList',
        type: 'GET',
        dataType: 'json',
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        res.data.forEach(item => {
            ddl.append(`<option value="${item.Value}" data-unit="${item.unit}" data-ucode="${item.ucode}">${item.Text}</option>`);
        });
        // Set selected value if provided
        if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
            ddl.val(selectedValue).trigger('change');
        }
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading Item list: ' + error);
        return [];
    });
}

//--------- ADDRESS -----------
function loadAddList(shipFromCode, selector) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetAddList',
        type: 'GET',
        dataType: 'json',
        data: { shipFromCode: shipFromCode },
        success: function (res) {
            const data = res.data;
            var ddl = $(selector);
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

//--------- CITY -----------
function loadCityList(dropdownId, selectedValue = null) {
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
            if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
                ddl.val(selectedValue).trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading city list: ' + error);
        }
    });
}

//--------- STATE -----------
function loadStateList(dropdownId, cCode, selectedValue = null) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetStateList',
        type: 'GET',
        data: { cCode: cCode },
        dataType: 'json',
        success: function (res) {
            const data = res.data;
            var ddl = $(dropdownId);
            ddl.empty();
            //ddl.append('<option value="">-- Select State --</option>');
            $.each(data, function (index, item) {
                ddl.append('<option value="' + item.Value + '" selected>' + item.Text + '</option>');
            });
            if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
                ddl.val(selectedValue).trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading state list: ' + error);
        }
    });
}

//--------- CURRENCY -----------
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

//--------- TAX -----------
function loadTaxTypeList(dropdownId, selectedValue = null) {
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
        // Set selected value if provided
        if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
            ddl.val(selectedValue).trigger('change');
        }
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading tax list: ' + error);
        return [];
    });
}

//--------- STATUS -----------
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

//--------- TRANSPORT -----------
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

//--------- DEPARTMENT -----------
function loadDepartmentList(dropdownId, selectedValue = null) {
    return $.ajax({
        url: '/PurchaseBillPassEntry/GetDepartmentList',
        type: 'GET',
        dataType: 'json',
    }).then(function (res) {
        const ddl = $(dropdownId);
        ddl.empty();
        ddl.append('<option value="">-- Select --</option>');
        console.log("Tax list: ", res.data);
        res.data.forEach(item => {
            ddl.append(`
                <option value="${item.Value}">${item.Text}</option>
            `);
        });
        // Set selected value if provided
        if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
            ddl.val(selectedValue).trigger('change');
        }
        return res;
    }).catch(function (xhr, status, error) {
        toastr.error('Error loading department list: ' + error);
        return [];
    });
}
//=========== DROPDOWN END ============
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

function parseNullableDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    return isNaN(date.getTime()) ? null : date.toISOString();
}

//------------------ ADD ROW ---------------
function createRowHtml(data = {}) {

    return `
        <tr>

            <td class="freeze-item"><select class="form-control form-control-sm item-name" disabled></select></td>

            <td><input class="form-control form-control-sm hsn-code" type="text" value="${data.hsN_CODE || ''}"/></td>
            <td>
                <input class="form-control form-control-sm uom-code" type="hidden" value="${data.uoM_CODE || ''}" disabled/>
                <input class="form-control form-control-sm uom-name" type="text" value="${data.unit || ''}" disabled/>
            </td>

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || ''}"/></td>
            <td><input class="form-control form-control-sm recd-qty" type="number" value="${data.recD_QTY || ''}"/></td>
            <td><input class="form-control form-control-sm bill-qty" type="number" value="${data.bilL_QTY || ''}"/></td>

            <td><input class="form-control form-control-sm usd-rate" type="number" value="${data.usD_RATE || ''}"/></td>
            <td><input class="form-control form-control-sm exch-rate" type="number" value="${data.excH_RATE || ''}"/></td>
            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate || ''}"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${data.amount || ''}"/></td>

            <td>
                <select class="form-control form-control-sm rcm-yn">
                    <option value="">-- Select --</option>
                    <option value="YES" ${(data.rcM_YN || '').toUpperCase() === 'YES' ? 'selected' : ''}>YES</option>
                    <option value="NO" ${(data.rcM_YN || '').toUpperCase() === 'NO' ? 'selected' : ''}>NO</option>
                </select>
            </td>

            <td>
                <select class="form-control form-control-sm input-yn">
                    <option value="">-- Select --</option>
                    <option value="YES" ${(data.inpuT_YN || '').toUpperCase() === 'YES' ? 'selected' : ''}>YES</option>
                    <option value="NO" ${(data.inpuT_YN || '').toUpperCase() === 'NO' ? 'selected' : ''}>NO</option>
                </select>
            </td>

            <td><select class="form-control form-control-sm tax-code"></select></td>

            <td><input class="form-control form-control-sm pack-per" type="number" value="${data.pacK_PER || ''}"/></td>
            <td><input class="form-control form-control-sm pack-amt" type="number" value="${data.pacK_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm disc-per" type="number" value="${data.disC_PER || ''}"/></td>
            <td><input class="form-control form-control-sm disc-amt" type="number" value="${data.disC_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm cgst-per" type="number" value="${data.cgsT_PER || ''}"/></td>
            <td><input class="form-control form-control-sm cgst-amt" type="number" value="${data.cgsT_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm sgst-per" type="number" value="${data.sgsT_PER || ''}"/></td>
            <td><input class="form-control form-control-sm sgst-amt" type="number" value="${data.sgsT_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm igst-per" type="number" value="${data.igsT_PER || ''}"/></td>
            <td><input class="form-control form-control-sm igst-amt" type="number" value="${data.igsT_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm cess-per" type="number" value="${data.cesS_PER || ''}"/></td>
            <td><input class="form-control form-control-sm cess-amt" type="number" value="${data.cesS_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm vat-per" type="number" value="${data.vaT_PER || ''}"/></td>
            <td><input class="form-control form-control-sm vat-amt" type="number" value="${data.vaT_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm oth-amt" type="number" value="${data.otH_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm net-amt" type="number" value="${data.neT_AMT || ''}"/></td>

            <td>
                <input class="form-control form-control-sm make-code" type="hidden" value="${data.make_Code || ''}"/>
                <input class="form-control form-control-sm make-name" type="text" value="${data.make || ''}"/>
            </td>
            <td>
                <select class="form-control form-control-sm dept-code"></select>
            </td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || ''}"/></td>

            <td><input class="form-control form-control-sm land-rate" type="number" value="${data.lanD_RATE || ''}"/></td>
            <td><input class="form-control form-control-sm land-amt" type="number" value="${data.lanD_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm poland-rate" type="number" value="${data.polanD_RATE || ''}"/></td>
            <td><input class="form-control form-control-sm po-rate" type="number" value="${data.pO_RATE || ''}"/></td>

            <td><input class="form-control form-control-sm po-type" type="text" value="${data.pO_TYPE || ''}"/></td>
            <td><input class="form-control form-control-sm po-no" type="number" value="${data.pO_NO || ''}"/></td>

            <td><input class="form-control form-control-sm kanta-type" type="text" value="${data.kantA_TYPE || ''}"/></td>
            <td><input class="form-control form-control-sm kanta-no" type="number" value="${data.kantA_NO || ''}"/></td>

            <td><input class="form-control form-control-sm req-type" type="text" value="${data.reQ_TYPE || ''}"/></td>
            <td><input class="form-control form-control-sm req-no" type="number" value="${data.reQ_NO || ''}"/></td>

            <td><input class="form-control form-control-sm ref-type" type="text" value="${data.reF_TYPE || ''}"/></td>
            <td><input class="form-control form-control-sm ref-no" type="number" value="${data.reF_NO || ''}"/></td>

            <td><input class="form-control form-control-sm dr-note-amt" type="number" value="${data.dr_notE_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm cr-note-amt" type="number" value="${data.cr_notE_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm qlty-diff-dr-amt" type="number" value="${data.qlty_diff_dR_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm rate-diff-dr-amt" type="number" value="${data.rate_diff_dR_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm qc-diff-dr-amt" type="number" value="${data.qc_diff_dR_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm qty-diff-dr-amt" type="number" value="${data.qty_diff_dR_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm other-dr-amt" type="number" value="${data.other_dR_AMT || ''}"/></td>

            <td class="action-col">
                <i class="fas fa-trash text-danger delete-row"></i>
                <i class="fas fa-plus-circle text-success add-row"></i>
            </td>

        </tr>
    `;
}
async function addNewRowBelow(data = null) {
    data = data || {};
    let rowHtml = createRowHtml(data || {});
    $("#tblItemRecordPBPE tbody").append(rowHtml);

    // IMPORTANT: now bind dropdowns AFTER row is in DOM
    const $lastRow = $("#tblItemRecordPBPE tbody tr:last");

    await loadItemList($lastRow.find(".item-name"), data.iteM_CODE || null);
    loadTaxTypeList($lastRow.find(".tax-code"), data.taX_CODE || null);
    loadDepartmentList($lastRow.find(".dept-code"), data.depT_CODE || null);
}

//-------------- DELETE ROW -----------
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

//-------- SELECT2 HELPER -------------
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

//-------- DATE WITH CHK HELPER -------------
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
function setDateControl(dateValue, dateInputId, checkBoxId) {
    if (!dateValue || dateValue === "") {
        const currentDate = getCurrentDateYMD();
        $(dateInputId).val(currentDate);
        $(dateInputId).prop('disabled', true);
        $(checkBoxId).prop('checked', false);
    } else {
        $(dateInputId).val(formatDateYMD(dateValue));
        $(dateInputId).prop('disabled', false);
        $(checkBoxId).prop('checked', true);
    }
}
//============================================ MRN No Change=============================

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

//----------------------- HEADER DATA BY MRN NO ---------------
function LoadMRNData(vType, vNo) {
    $.ajax({
        url: '/PurchaseBillPassEntry/GetPurchaseDetailsByMRN',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: function (response) {
            if (response.success) {
                console.log("MRN Data: ", response.data);
                const data = response.data;
                const currentDate = getCurrentDateYMD();

                $('#TxtBillNo').val(data.bilL_NO || '');
                setDateControl(data.bilL_DATE, '#DtBillDate', '#chkBillDate');

                $('#TxtChallanNo').val(data.chalL_NO || '');
                setDateControl(data.chalL_DATE, '#DtChDate', '#chkChDate');
                //==============WayBill Details
                $('#TxtWaybillNo').val(data.waybilL_NO || '');
                $('#TxtWayBillInvNo').val(data.ewB_INVNO || '');
                if (!data.ewB_DATE || data.ewB_DATE === "") {
                    $('#DtWaybillDate').val(formatDateYMD(data.ewB_DATE) || '');
                }
                else {
                    $('#DtWaybillDate').val(currentDate);
                }
                if (!data.ewB_EXPDATE || data.ewB_EXPDATE === "") {
                    $('#DtWaybillExpiry').val(formatDateYMD(data.ewB_EXPDATE) || '');
                }
                else {
                    $('#DtWaybillExpiry').val(currentDate);
                }

                $('#NumExRate').val(data.excH_RATE || '');

                loadPartyDrCrAcList('#ddlCreditAC', data.partY_CODE);
                //================Bill Details
                loadPartyListNatureSupplier('#ddlBillFrom', false, data.partY_CODE);
                $('#TxtAdd1PD').val(data.bilL_ADD1 || '');
                $('#TxtAdd2PD').val(data.bilL_ADD2 || '');
                $('#TxtAdd3PD').val(data.bilL_ADD3 || '');
                loadCityList('#ddlCityPD', data.bilL_CITY);
                $('#NumPincodeBL').val(data.bilL_PINCODE || '');
                $('#TxtGSTNo').val(data.bilL_GST || '');
                loadStateList('#ddlStateSF', data.bilL_STATE);

                //===============Ship Details
                loadPartyListNatureSupplier('#ddlShipFrom1', true, data.shiP_CODE);
                $('#TxtAdd1SF').val(data.shiP_ADD1 || '');
                $('#TxtAdd2SF').val(data.shiP_ADD2 || '');
                $('#TxtAdd3SF').val(data.shiP_ADD3 || '');
                loadCityList('#ddlCitySF', data.shiP_CITY);
                $('#TxtPincodeSF').val(data.shiP_PINCODE || '');
                $('#TxtGSTNoSF').val(data.shiP_GST || '');
                loadStateList('#ddlStatePD', data.shiP_STATE)

                $('#txtRemarks').val(data.remarks || '');

                //==============Logistic Details=======
                $('#txtVehicleNo').val(data.trucK_NO || '');
                $('#txtContainerNo').val(data.containeR_NO || '');
                $('#txtGRNo').val(data.gR_NO || '');
                setDateControl(data.gR_DATE, '#DtGRDate', '#chkGRDate');

                $('#NumFreightPay').val(data.frtpaY_AMT || '');
                $('#NumFrtTax1').val(data.frtpaY_TAXPER || '');
                $('#NumFrtTax2').val(data.frtpaY_TAX || '');
                $('#TxtFrtPayNarration').val(data.frtpaY_NAR || '');

                $('#ddlPayment').val(data.holD_PAY || '');
                $('#TxtReason').val(data.holD_REASON || '');
                setDateControl(data.holD_DATE, '#DtHoldDate', '#chkHoldDate');

                const paymentVal = $('#ddlPayment').val() || '';
                if (paymentVal.toUpperCase() === "HOLD") {
                    $('#ddlPayment').prop('disabled', true);
                    $('#DtHoldDate').prop('disabled', true);
                    $('#chkHoldDate').prop('disabled', true);
                }

                $('#NumTcs1').val(data.tcS_PER || '');
                $('#NumTcs2').val(data.tcS_AMT || '');

                GetPurchaseItemsByMRN(vType, vNo);
            }
            else {
                showToast(response.message, { type: "error" });
            }
        },
        error: function (error) {
            showToast(error, { type: "error" });
        }
    });
}

//----------------------- ITEM DETAILS BY MRN NO ---------------
async function GetPurchaseItemsByMRN(vType, vNo) {

    try {

        const response = await $.ajax({
            url: '/PurchaseBillPassEntry/GetPurchaseItemsByMRN',
            type: 'GET',
            dataType: 'json',
            data: {
                vType,
                vNo
            }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });
            return;
        }

        console.log("Purchase Item List By MRN No:", response.data);

        $("#tblItemRecordPBPE tbody").empty();

        for (const item of response.data) {

            item.rcM_YN = "NO";
            item.inpuT_YN = "YES";

            try {

                // Get PO Rates
                const rateData = await GetItemOrderRatesByPO(
                    item.pO_TYPE,
                    item.pO_NO,
                    item.iteM_CODE
                );

                item.polanD_RATE = rateData.landRate;
                item.pO_RATE = rateData.rate;

                // Add Row
                //const $row = await addNewRowBelow(item);
                await addNewRowBelow(item);
                let $row = $("#tblItemRecordPBPE tbody tr:last");

                // Calculations
                await calculateAmt($row, item.iteM_CODE);
                calculateTax($row, item.iteM_CODE);
                calculateLandAmount($row, item.iteM_CODE);

                // HSN & Qty Check
                const result = await GetHsnCodeAndQty(
                    item.iteM_CODE,
                    item.pO_TYPE,
                    item.pO_NO
                );

                if (result.hsnCode !== item.hsN_CODE) {
                    $row.find(".hsn-code")
                        .css("background-color", "#f8d7da");
                }

                const recdQty =
                    parseFloat($row.find(".recd-qty").val()) || 0;

                if (parseFloat(result.qty) !== recdQty) {
                    $row.find(".recd-qty")
                        .css("background-color", "#f8d7da");
                }

            }
            catch (err) {

                console.error(err);
                showToast(err.message || err, { type: "error" });

            }
        }

        // Calculate once after all rows are loaded
        calculateItemTotals();

        await CalcDrCrNote();

    }
    catch (xhr) {

        console.error(xhr);

        showToast(
            xhr.responseJSON?.message ||
            xhr.responseText ||
            "Unable to load Purchase Items.",
            { type: "error" }
        );
    }
}

function GetItemOrderRatesByPO(poType, poNo, itemCode) {

    return $.ajax({
        url: '/PurchaseBillPassEntry/GetItemOrderRatesByPO',
        type: 'GET',
        dataType: 'json',
        data: {
            poType: poType,
            poNo: poNo,
            itemCode: itemCode
        }
    }).then(function (res) {

        if (res.success) {
            return {
                landRate: res.landRate,
                rate: res.rate,
                exists: res.exists
            };
        } else {
            throw new Error(res.message || "No data found");
        }
    });
}

//--------------------- ITEM AMOUNTS CALCULATIONS ----------------
async function calculateAmt($row, itemCode) {
    //=====================
    //if (isReadOnly) {
    //    return;
    //}

    // ---------- INPUTS ----------
    let usdRate = parseFloat($row.find('.usd-rate').val()) || 0;

    let billQty = parseFloat($row.find('.bill-qty').val()) || 0;
    let rate = parseFloat($row.find('.rate').val()) || 0;
    let exRate = parseFloat($row.find('.exch-rate').val()) || 0;

    let pack = parseFloat($row.find('.pack-amt').val()) || 0;
    let packPer = parseFloat($row.find('.pack-per').val()) || 0;
    let disc = parseFloat($row.find('.disc-amt').val()) || 0;
    let discPer = parseFloat($row.find('.disc-per').val()) || 0;
    let cess = parseFloat($row.find('.cess-amt').val()) || 0;
    let cessPer = parseFloat($row.find('.cess-per').val()) || 0;
    let vat = parseFloat($row.find('.vat-amt').val()) || 0;
    let vatPer = parseFloat($row.find('.vat-per').val()) || 0;

    let taxCode = $row.find('.tax-code').val() || 0;

    let cgst = parseFloat($row.find('.cgst-amt').val()) || 0;
    let sgst = parseFloat($row.find('.sgst-amt').val()) || 0;
    let igst = parseFloat($row.find('.igst-amt').val()) || 0;
    let otherAmt = parseFloat($row.find('.oth-amt').val()) || 0;

    //let itemCode = $row.find('.item-name').val() ;
    let pob = 0;
    let packAmt = 0;
    let discount = 0;
    let cessAmt = 0;
    let vatAmt = 0;
    let net = 0;
    let basicAmt = 0;

    if (itemCode > 0) {
        // ------------RATE --------------
        console.log("rateAmt before calc: ", rate);
        console.log("exRate before calc: ", exRate);
        if (exRate > 0) {
            rate = usdRate * exRate;
            console.log("rateAmt: ", rate);
            $row.find('.rate').val(rate.toFixed(4));
        }
        // ---------- BASIC ----------
        basicAmt = billQty * rate;
        console.log("basicAmt: ", basicAmt);

        // ---------- DISCOUNT ----------
        discount = (discPer > 0) ? (basicAmt * discPer / 100) : disc;
        console.log("discount: ", discount);

        // ---------- PACKING ----------
        if (taxCode > 0) {
            let res = await GetPackOnBasic(taxCode);
            pob = res.success ? res.data : 0;
            console.log("pob: ", pob);
        }
        if (pob === 1) {
            packAmt = (packPer > 0) ? (basicAmt * packPer / 100) : pack;
        }
        else {
            packAmt = (packPer > 0) ? ((basicAmt - discount) * packPer / 100) : pack;
        }
        console.log("packAmt: ", packAmt);

        // ---------- TAXABLE VALUE ----------
        let grossAmt = basicAmt + packAmt - discount;
        console.log("grossAmt: ", grossAmt);

        // ---------- CESS/VAT ----------
        cessAmt = (cessPer > 0) ? grossAmt * cessPer / 100 : cess;
        console.log("cessAmt: ", cessAmt);

        vatAmt = (vatPer > 0) ? grossAmt * vatPer / 100 : vat;
        console.log("vatAmt: ", vatAmt);

        // ---------- NET AMOUNT ----------
        net = grossAmt + cgst + sgst + igst + cessAmt + vatAmt + otherAmt;
        console.log("grossAmt: ", grossAmt);

    }
    // ---------- UPDATE UI ----------
    $row.find('.amount').val(basicAmt.toFixed(2));
    $row.find('.pack-amt').val(packAmt.toFixed(4));
    $row.find('.disc-amt').val(discount.toFixed(4));

    $row.find('.cess-amt').val(cessAmt.toFixed(4));
    $row.find('.vat-amt').val(vatAmt.toFixed(4));

    $row.find('.net-amt').val(net.toFixed(4));
}

function GetPackOnBasic(code) {

    return $.ajax({
        url: '/PurchaseBillPassEntry/GetPackOnBasic',
        type: 'GET',
        dataType: 'json',
        data: {
            code: code
        }
    });
}

//--------------------- ITEM TAX CALCULATIONS ----------------
function calculateTax($row, itemCode) {

    //If specialusercontrol.lblAction.Tag = 2 Then Return
    //        If recal = False Then Return

    const amount = parseFloat($row.find(".amount").val()) || 0;
    const packAmt = parseFloat($row.find(".pack-amt").val()) || 0;
    const discAmt = parseFloat($row.find(".disc-amt").val()) || 0;

    const cgstPer = parseFloat($row.find(".cgst-per").val()) || 0;
    const sgstPer = parseFloat($row.find(".sgst-per").val()) || 0;
    const igstPer = parseFloat($row.find(".igst-per").val()) || 0;

    const cessAmt = parseFloat($row.find(".cess-amt").val()) || 0;
    const vatAmt = parseFloat($row.find(".vat-amt").val()) || 0;
    const otherAmt = parseFloat($row.find(".oth-amt").val()) || 0;

    let grossAmt = 0;

    let cgst = parseFloat($row.find(".cgst-amt").val()) || 0;
    let sgst = parseFloat($row.find(".sgst-amt").val()) || 0;
    let igst = parseFloat($row.find(".igst-amt").val()) || 0;

    if (itemCode > 0) {
        grossAmt = amount + packAmt - discAmt;
    }
    // Recalculate only if user is not editing these fields
    //if (currentField !== "cgst-amt" && currentField !== "sgst-amt") {
    cgst = grossAmt * cgstPer / 100;
    sgst = grossAmt * sgstPer / 100;

    $row.find(".cgst-amt").val(cgst.toFixed(4));
    $row.find(".sgst-amt").val(sgst.toFixed(4));
    //}

    //if (currentField !== "igst-amt") {
    igst = grossAmt * igstPer / 100;
    $row.find(".igst-amt").val(igst.toFixed(4));
    //}

    const netAmt =
        grossAmt +
        cgst +
        sgst +
        igst +
        cessAmt +
        vatAmt +
        otherAmt;

    $row.find(".net-amt").val(netAmt.toFixed(4));

    // Equivalent of ReadOnly property
    $row.find(".cgst-amt").prop("readonly", cgst <= 0);
    $row.find(".sgst-amt").prop("readonly", cgst <= 0);
    $row.find(".igst-amt").prop("readonly", igst <= 0);
}

function GetHsnCodeAndQty(itemCode, poType, poNo) {

    return new Promise(function (resolve, reject) {

        $.ajax({
            url: '/PurchaseBillPassEntry/GetHsnCodeAndQty',
            type: 'GET',
            dataType: 'json',
            data: {
                itemCode: itemCode,
                poType: poType,
                poNo: poNo
            },
            success: function (response) {

                if (response.success) {
                    resolve(response.data);
                }
                else {
                    reject(response.message);
                }
            },
            error: function (xhr, status, error) {
                reject(error || xhr.responseText);
            }
        });

    });

}

//--------------------- ITEM TOTAL CALCULATIONS ----------------
function calculateItemTotals() {

    let totals = {
        recQty: 0,
        billQty: 0,
        amount: 0,
        packing: 0,
        discount: 0,
        cgst: 0,
        sgst: 0,
        igst: 0,
        cess: 0,
        vat: 0,
        other: 0,
        netAmt: 0
    };

    $("#tblItemRecordPBPE tbody tr").each(function () {

        const row = $(this);

        totals.recQty += parseFloat(row.find(".recd-qty").val()) || 0;
        totals.billQty += parseFloat(row.find(".bill-qty").val()) || 0;
        totals.amount += parseFloat(row.find(".amount").val()) || 0;
        totals.packing += parseFloat(row.find(".pack-amt").val()) || 0;
        totals.discount += parseFloat(row.find(".disc-amt").val()) || 0;
        totals.cgst += parseFloat(row.find(".cgst-amt").val()) || 0;
        totals.sgst += parseFloat(row.find(".sgst-amt").val()) || 0;
        totals.igst += parseFloat(row.find(".igst-amt").val()) || 0;
        totals.cess += parseFloat(row.find(".cess-amt").val()) || 0;
        totals.vat += parseFloat(row.find(".vat-amt").val()) || 0;
        totals.other += parseFloat(row.find(".oth-amt").val()) || 0;
        totals.netAmt += parseFloat(row.find(".net-amt").val()) || 0;

    });

    //Display Totals
    $("#NumReceivedQty").val(totals.recQty.toFixed(2));
    $("#NumBillQty").val(totals.billQty.toFixed(2));
    $("#NumAmount").val(totals.amount.toFixed(2));
    $("#NumPacking").val(totals.packing.toFixed(2));
    $("#NumDiscount").val(totals.discount.toFixed(2));

    $("#NumCgst").val(totals.cgst.toFixed(2));
    $("#NumSgst").val(totals.sgst.toFixed(2));
    $("#NumIgst").val(totals.igst.toFixed(2));
    $("#NumVat").val(totals.vat.toFixed(2));
    $("#NumCess").val(totals.cess.toFixed(2));

    $("#NumOtherAmt").val(totals.other.toFixed(2));
    console.log("totals: ", totals);
    //TCS Amount
    const tcsAmt = parseFloat($("#NumTcs2").val()) || 0;

    //Sub Total
    const subTotal = totals.netAmt + tcsAmt;
    console.log("subTotal: ", subTotal);

    $("#NumSubTotal").val(subTotal.toFixed(2));

    //Round Off
    const rounded = Math.round(subTotal);
    const roundOff = rounded - subTotal;
    console.log("rounded: ", rounded);
    console.log("roundOff: ", roundOff);

    $("#NumRoundOff").val(roundOff.toFixed(2));
    $("#NumNetAmount").val(rounded.toFixed(2));

    //TDS 194Q
    const tdsPer = parseFloat($("#TxtTds194q1").val()) || 0;

    const tds194Q =
        ((totals.amount + totals.packing - totals.discount) * tdsPer) / 100;
    console.log("tds194Q: ", tds194Q);

    $("#TxtTds194q2").val(tds194Q.toFixed(2));
}

//--------------------- ITEM LAND AMOUnT CALCULATIONS ----------------
function calculateLandAmount($row, itemCode) {

    //$("#tblItemRecordPBPE tbody tr").each(function () {

    //const $row = $(this);

    //const itemCode = $row.find(".item-name").val() || 0;
    const billQty = parseFloat($row.find(".bill-qty").val()) || 0;
    const rate = parseFloat($row.find(".rate").val()) || 0;

    const packAmt = parseFloat($row.find(".pack-amt").val()) || 0;
    const discAmt = parseFloat($row.find(".disc-amt").val()) || 0;

    const cgst = parseFloat($row.find(".cgst-amt").val()) || 0;
    const sgst = parseFloat($row.find(".sgst-amt").val()) || 0;
    const igst = parseFloat($row.find(".igst-amt").val()) || 0;
    const cess = parseFloat($row.find(".cess-amt").val()) || 0;

    let packRate = 0;
    let discRate = 0;
    let taxRate = 0;

    if (itemCode > 0) {
        if (billQty > 0) {
            packRate = packAmt / billQty;
            discRate = discAmt / billQty;
            taxRate = (cgst + sgst + igst + cess) / billQty;
            console.log("taxRate: ", taxRate);
        }
    }

    const landRate = rate + packRate - discRate + taxRate;
    const landAmt = billQty * landRate;

    console.log("landRate and landAmt", landRate, landAmt)

    $row.find(".land-rate").val(landRate.toFixed(2));
    $row.find(".land-amt").val(landAmt.toFixed(2));

    //});

}

//------------------ CALCULATE DR/CR NOTE --------------

function GetCrDrNoteRequest() {
    const request = {
        vType: $("#ddlDocType").val(),
        vNo: $("#NumDocNo").val(),
        vDate: $("#DtDocDate").val(),
        billToPartyCode: parseInt($("#ddlBillFrom").val()) || 0,
        billToPartyName: $("#ddlBillFrom").find("option:selected").text().trim(),
        txtQualityDiffDebitAmt: parseFloat($("#TxtQualityDiffDebitAmt").val()) || 0,
        txtQualityDiffDebitTax: parseFloat($("#TxtQualityDiffDebitTax").val()) || 0,
        items: [],

        totalRcvdQty: parseFloat($('#NumReceivedQty').val()) || 0,
        totalBillQty: parseFloat($('#NumBillQty').val()) || 0,
        totalNetAmt: parseFloat($('#NumNetAmount').val()) || 0,
        totalTCSAmt: parseFloat($('#NumTcs2').val()) || 0,
        totalPackingAmt: parseFloat($('#NumTcs2').val()) || 0,
        isSealedVehicle: $('#ChkSealedVehicle').is(':checked'),

        mrnType: $("#TxtMRNNo1").val(),
        mrnNo: $("#TxtMRNNo2").val(),

        inputType: $("#ddlInputType").val(),
        FreightAmountPay: parseFloat($('#NumFreightPay').val()) || 0,
        FreightTax: parseFloat($('#NumFrtTax2').val()) || 0,
        FreightTaxPercent: parseFloat($('#NumFrtTax1').val()) || 0,
    };

    $("#tblItemRecordPBPE tbody tr").each(function () {

        const $row = $(this);

        request.items.push({
            itemCode: parseInt($row.find(".item-name").val()) || 0,
            itemName: $row.find(".item-name option:selected").text(),
            unit: $row.find(".uom-name").val() || "",

            amount: parseFloat($row.find(".amount").val()) || 0,

            recdQty: parseFloat($row.find(".recd-qty").val()) || 0,
            billQty: parseFloat($row.find(".bill-qty").val()) || 0,

            cgstPer: parseFloat($row.find(".cgst-per").val()) || 0,
            sgstPer: parseFloat($row.find(".sgst-per").val()) || 0,
            igstPer: parseFloat($row.find(".igst-per").val()) || 0,

            poType: $row.find(".po-type").val() || "",
            poNo: parseInt($row.find(".po-no").val()) || 0,

            landRate: parseFloat($row.find(".land-rate").val()) || 0,
            poRate: parseFloat($row.find(".po-rate").val()) || 0,
            poLandRate: parseFloat($row.find(".poland-rate").val()) || 0
        });

    });

    return request;
}

async function CalcDrCrNote() {

    const request = GetCrDrNoteRequest();

    try {

        const result = await $.ajax({
            url: "/PurchaseBillPassEntry/CalculateDebitNote",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(request)
        });

        if (result.warnings && result.warnings.length > 0) {
            console.log("result.warnings.length", result.warnings.length);
            result.warnings.forEach(function (message) {
                showToast(message, { type: "warning" });
            });
        }
        console.log("DR/CR result: ", result)
        //------------ Rate Debit --------------
        $("#TxtRateDiffDebitAmt").val((result.rateDiffDebitAmt || 0).toFixed(2));
        $("#TxtRateDiffDebitTax").val((result.rateDiffDebitTax || 0).toFixed(2));
        $("#TxtRateDiffDebitNarration").val(result.rateDiffDebitNarration);

        //------------ Quality Debit -------------
        $("#TxtQualityDiffDebitAmt").val((result.qualityDiffDebitAmt || 0).toFixed(2));
        $("#TxtQualityDiffDebitTax").val((result.qualityDiffDebitTax || 0).toFixed(2));
        $("#TxtQualityDiffDebitNarration").val(result.qualityDiffDebitNarration);

        //-------------- Weight Debit ---------------
        $("#TxtWeightDebitAmt").val((result.weightDiffDebitAmt || 0).toFixed(2));
        $("#TxtWeightDebitTax").val((result.weightDiffDebitTax || 0).toFixed(2));
        $("#TxtWeightDebitNarration").val(result.weightDiffDebitNarration);

        //-------------- QC Debit -------------
        $("#TxtQCDebitNoteAmt").val((result.qcDebitAmt || 0).toFixed(2));
        $("#TxtQCDebitNoteTax").val((result.qcDebitTax || 0).toFixed(2));
        $("#TxtQCDebitNarration").val(result.qcDebitNarration);

    }
    catch (ex) {

        console.error(ex);

        showToast(
            "Unable to calculate Debit Note.",
            { type: "error" });
    }
}