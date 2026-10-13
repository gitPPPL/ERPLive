let sendToList = [];

$(document).ready(function () {
    var today = new Date();

    var currentDate = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');

    $('#DtDatefrom').val(currentDate);
    $('#Dtto').val(currentDate);

    const currentYear = new Date().getFullYear();
    const fromDate = `${currentYear}-01-01`;
    $('#Dtfromdate').val(fromDate);

    $('#Dttodate').val(currentDate);

    $('#btnviewdata').on('click', async function () {
        GetReportData();
    });

    $('#btn_listofdoc').on('click', async function () {
        LoadDropdown();

        GetDocumentListData();
    });

    $('#searchBox').on('keyup', function () {
        let value = $(this).val().toLowerCase().trim();
        $('#tblImportTrackingReport tbody tr').each(function () {
            let found = false;
            $(this).find('td').each(function () {
                if ($(this).text().toLowerCase().indexOf(value) > -1) {
                    found = true;
                    return false;
                }
            });
            $(this).toggle(found);
        });

        const total = $('#tblImportTrackingReport tbody tr:visible').length;
        $('#lblTotalCount').text(`Total Sauda : ${total}`);
    });

    $('#btn_search').on('click', async function () {
        const v_type = $('#ddlvtype').val();
        if (!v_type) {
            toastr.warning("Please select VOucher Type");
            return;
        }
        await loadSendTo();
        GetDocumentListData();
    });

    $('#btn_send').on('click', async function () {

        let selectedRows = [];

        $('#tbldocumentsendlist tbody tr').each(function () {

            let index = $(this).attr('id').replace('tr_', '');

            let checkbox = $('#chkRow_' + index);

            if (checkbox.is(':checked')) {

                let rowData = {

                    V_TYPE: $('#tdVType_' + index).text().trim(),

                    V_NO: Number($('#tdVNo_' + index).text().trim()) || 0,

                    SAUDA_TYPE: $('#tdSaudaType_' + index).text().trim(),

                    SAUDA_NO: Number($('#tdSaudaNo_' + index).text().trim()) || 0,

                    V_DATE: convertDate($('#tdVDate_' + index).text().trim()),

                    PARTY_CODE: Number($('#tdPartyCode_' + index).text().trim()) || 0,

                    PARTY_NAME: $('#tdPartyName_' + index).text().trim(),

                    CITY_NAME: $('#tdCityName_' + index).text().trim(),

                    BILL_NO: $('#tdBillNo_' + index).text().trim(),

                    BILL_DATE: convertDate($('#tdBillDate_' + index).text().trim()),

                    BILL_AMT: Number($('#tdInvAmt_' + index).text().trim()) || 0,

                    REMARKS: $('#tdRemarks_' + index).text().trim(),

                    TRUCK_NO: $('#tdTruckNo_' + index).text().trim(),

                    SEND_TO: Number($('#ddlSendTo_' + index).val()) || 0,

                    SEND_FLAG: Number($('#tdSendFlag_' + index).text().trim()) || 0,

                    SEND_DATE: convertDate($('#tdSendDate_' + index).text().trim()),

                    RECD_FLAG: Number($('#tdRecdFlag_' + index).text().trim()) || 0,

                    RECD_DATE: convertDate($('#tdRecdDate_' + index).text().trim()),

                    SEND_BY: null
                };


                selectedRows.push(rowData);
            }

        });


        if (selectedRows.length === 0) {

            toastr.warning("Please select at least one document.");
            return;

        }


        console.log("Selected Data:", selectedRows);


        $.ajax({

            url: '/ImportTrackingReport/SendDocument',

            type: 'POST',

            contentType: 'application/json; charset=utf-8',

            data: JSON.stringify(selectedRows),

            success: function (res) {

                if (res.validation === false) {

                    toastr.warning(res.message);
                    return;

                }


                if (res.success) {

                    toastr.success(res.message);
                    GetDocumentListData();

                }
                else {

                    toastr.error(res.message);

                }

            },

            error: function (xhr) {

                console.log(xhr.responseText);
                toastr.error("Error sending document.");

            }

        });

    });

    $(document).on('change', '#chkSelectAll', function () {
        const isChecked = $(this).is(':checked');
        $('#tbldocumentsendlist tbody .row-checkbox').prop('checked', isChecked);
    });

    $(document).on('change', '#tbldocumentsendlist tbody .row-checkbox', function () {
        const total = $('#tbldocumentsendlist tbody .row-checkbox').length;
        const checked = $('#tbldocumentsendlist tbody .row-checkbox:checked').length;
        $('#chkSelectAll').prop('checked', total > 0 && total === checked);
    });

    $(document).on('click', '.btnSendDocument', async function (e) {
        e.preventDefault();

        let selectedRows = [];
        $('.row-checkbox:checked').each(function () {
            const index = $(this).attr('id').split('_')[1];

            const rowData = {
                V_TYPE: $('#tdVType_' + index).text().trim(),
                V_NO: parseInt($('#tdVNo_' + index).text().trim()) || null,
                SAUDA_TYPE: $('#tdSaudaType_' + index).text().trim(),
                SAUDA_NO: parseInt($('#tdSaudaNo_' + index).text().trim()) || null,
                V_DATE: convertDate($('#tdVDate_' + index).text().trim()),
                PARTY_CODE: parseInt($('#tdPartyCode_' + index).text().trim()) || null,
                PARTY_NAME: $('#tdPartyName_' + index).text().trim(),
                CITY_CODE: parseInt($('#tdPartyCity_' + index).text().trim()) || null,
                CITY_NAME: $('#tdCityName_' + index).text().trim(),
                BILL_NO: $('#tdBillNo_' + index).text().trim(),
                BILL_DATE: convertDate($('#tdBillDate_' + index).text().trim()),
                BILL_AMT: parseFloat($('#tdInvAmt_' + index).text().trim()) || null,
                TRUCK_NO: $('#tdTruckNo_' + index).text().trim(),
                SEND_BY: null,
                SEND_TO: parseInt($('#ddlSendTo_' + index).val()) || null,
                SEND_DATE: convertDate($('#tdSendDate_' + index).text().trim()),
                RECD_DATE: convertDate($('#tdRecdDate_' + index).text().trim()),
                SEND_FLAG: parseInt($('#tdSendFlag_' + index).text().trim()) || null,
                RECD_FLAG: parseInt($('#tdRecdFlag_' + index).text().trim()) || null,
                REMARKS: $('#tdRemarks_' + index).text().trim()
            };

            selectedRows.push(rowData);
        });

        if (selectedRows.length === 0) {
            toastr.info("Please select at least one document.");
            return;
        }

        $.ajax({
            url: '/ImportTrackingReport/SendDocument',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(selectedRows),

            success: async function (res) {
                console.log("Response:", res);

                if (!res) {
                    toastr.error("No response received from server.");
                    return;
                }

                if (res.validation === false) {
                    toastr.warning(res.message);
                    return;
                }

                if (res.success) {
                    toastr.success(res.message || "Document sent successfully.");

                    // Refresh grid
                    await GetDocumentListData();
                } else {
                    toastr.error(res.message || "Failed to send document.");
                }
            },

            error: function (xhr) {
                console.log(xhr.responseText);
                toastr.error("Error sending document.");
            }
        });

    });

});

// ====================IMPORT TRACKING LIST====================
function convertDate(date) {
    if (!date) return null;

    let parts = date.split('/');

    if (parts.length === 3) {
        return `${parts[2]}-${parts[1]}-${parts[0]}`;
    }

    return date;
}
async function GetReportData() {
    try {

        const fromDate = $('#DtDatefrom').val();
        const toDate = $('#Dtto').val();

        const res = await $.ajax({
            url: '/ImportTrackingReport/GetReportData',
            type: 'GET',
            data: { FromDate: fromDate, ToDate: toDate }
        });
        if (res.success) {
            bindImportTrackingReport(res.data, res.totalUpdatedRecords);
        } else {
            toastr.error(res.message);
        }

    } catch (error) {
        console.error("Error:", error);
        toastr.error("Failed to load report data.");
    }
}
function fmtDate(v) {
    if (!v) return '';
    const d = new Date(v);
    return isNaN(d) ? v : d.toLocaleDateString('en-GB');
}
function bindImportTrackingReport(data, totalUpdatedRecords) {
    const tbody = $('#tblImportTrackingReport tbody');
    tbody.empty();

    const total = data ? data.length : 0;
    $('#lblTotalCount').text(`Total Sauda : ${total}`); 

    if (!data || data.length === 0) {
        tbody.append(`<tr> <td colspan="123" class="text-center">No data found.</td>  </tr> `);
        return;
    }

    $('#lblTotalUpdated').text(`Tracking Updated : ${totalUpdatedRecords}`); 

    data.forEach((item, index) => {
        tbody.append(`
                <tr>
                    <td class="hidden-col">${index + 1}</td>
                    <td class="freeze-item" data-column="1" style="left:0px;width:111px;min-width:111px;max-width:111px;">${item.saudaNo ?? ''}</td>
                    <td class="freeze-item" data-column="2" style="left:111px;width:97px;min-width:97px;max-width:97px;">${fmtDate(item.saudaDate)}</td>
                    <td class="freeze-item" data-column="3" style="left:208px;width:166px;min-width:166px;max-width:166px;">${item.supplierName ?? ''}</td>
                    <td class="freeze-item" data-column="4" style="left:374px;width:153px;min-width:153px;max-width:153px;">${item.itemName ?? ''}</td>
                    <td class="freeze-item" data-column="5" style="left:527px;width:84px;min-width:84px;max-width:84px;">${item.deliveryLocation ?? ''}</td>
                    <td>${item.country ?? ''}</td>
                    <td>${item.expectedLoading ?? ''}</td>
                    <td>${item.paymentTerm ?? ''}</td>
                    <td>${item.frtTerm ?? ''}</td>
                    <td>${item.delTerm ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.noofContainer ?? 0}</td>
                    <td>${item.saudaQty ?? 0}</td>
                    <td>${item.trackingQty ?? 0}</td>
                    <td>${item.mrnQty ?? 0}</td>
                    <td>${item.mrnNo ?? ''}</td>
                    <td>${fmtDate(item.mrnDate)}</td>
                    <td>${item.containerNo ?? ''}</td>
                    <td>${item.containerSize ?? ''}</td>
                    <td>${item.invNo ?? ''}</td>
                    <td>${fmtDate(item.invDate)}</td>
                    <td>${item.invAmt ?? 0}</td>
                    <td>${item.pod ?? ''}</td>
                    <td>${item.pol ?? ''}</td>
                    <td>${item.vslName ?? ''}</td>
                    <td>${item.icdName ?? ''}</td>
                    <td>${item.freeDays ?? 0}</td>
                    <td>${item.etd ?? ''}</td>
                    <td>${fmtDate(item.etaPortDate)}</td>
                    <td>${fmtDate(item.railDate)}</td>
                    <td>${fmtDate(item.icdReachDate)}</td>
                    <td>${item.pino ?? ''}</td>
                    <td>${item.offerno ?? ''}</td>
                    <td>${item.chaName ?? ''}</td>
                    <td>${fmtDate(item.scandoC_VERIFYDATE)}</td>
                    <td>${item.etA_PLACE ?? ''}</td>
                    <td>${fmtDate(item.chahandoveR_DATE)}</td>
                    <td>${item.bL_NO ?? ''}</td>
                    <td>${fmtDate(item.bL_DATE)}</td>
                    <td>${fmtDate(item.bL_RECDDATE)}</td>
                    <td>${fmtDate(item.bL_HANDOVERDATE)}</td>
                    <td>${item.chA_CHARGEDETAIL ?? ''}</td>
                    <td>${fmtDate(item.chklisT_APPROVDATE)}</td>
                    <td>${item.bE_NO ?? ''}</td>
                    <td>${fmtDate(item.bE_DATE)}</td>
                    <td>${fmtDate(item.etA_BEDATE)}</td>
                    <td>${fmtDate(item.igM_DATE)}</td>
                    <td>${item.samplE_COLLECTED ?? ''}</td>
                    <td>${fmtDate(item.samplE_COLDATE)}</td>
                    <td>${fmtDate(item.samplE_TESTREPORTDATE)}</td>
                    <td>${item.assesmenT_TYPE ?? ''}</td>
                    <td>${fmtDate(item.assesmenT_DATE)}</td>
                    <td>${item.dutY_AMT ?? ''}</td>
                    <td>${fmtDate(item.dutY_PAYDATE)}</td>
                    <td>${item.dutY_PAYMODE ?? ''}</td>
                    <td>${item.advancE_LICNO ?? ''}</td>
                    <td>${item.doC_HANDOVERDTBANK ?? ''}</td>
                    <td>${fmtDate(item.shiplinE_PAYDATE)}</td>
                    <td>${item.shippingLine ?? ''}</td>
                    <td>${item.hollagE_CHARGES ?? ''}</td>
                    <td>${fmtDate(item.dO_DATE)}</td>
                    <td>${item.securitY_AMT ?? ''}</td>
                    <td>${item.refunD_AMT ?? ''}</td>
                    <td>${fmtDate(item.receipT_DATE)}</td>
                    <td>${fmtDate(item.tentativE_HODATE)}</td>
                    <td>${fmtDate(item.containeR_HODATE)}</td>
                    <td>${item.advPaymentAmount ?? ''}</td>
                    <td>${item.advPaymentReference ?? ''}</td>
                    <td>${fmtDate(item.advPaymentDate)}</td>
                    <td>${item.baPaymentAmount ?? ''}</td>
                    <td>${item.balPaymentReference ?? ''}</td>
                    <td>${fmtDate(item.balPaymentDate)}</td>
                    <td>${item.difF_DAYS ?? ''}</td>
                    <td>${item.inT_RATE ?? ''}</td>
                    <td>${item.otH_CHARGES ?? ''}</td>
                    <td>${item.sblC_AVLAMT ?? ''}</td>
                    <td>${item.supplier ?? ''}</td>
                    <td>${item.agent ?? ''}</td>
                    <td>${item.bank ?? ''}</td>
                    <td>${item.shiplinE_NAME ?? ''}</td>
                    <td>${item.sblC_NO ?? ''}</td>
                    <td>${fmtDate(item.sblC_APPLDATE)}</td>
                    <td>${fmtDate(item.sblC_PAYDATE)}</td>
                    <td>${fmtDate(item.sblC_DUEDATE)}</td>
                    <td>${item.sblC_DISBAMT ?? ''}</td>
                    <td>${fmtDate(item.sblC_DISBDATE)}</td>
                    <td>${item.sblC_INTRATESOFR ?? ''}</td>
                    <td>${fmtDate(item.dO_VALIDDATE)}</td>
                    <td>${fmtDate(item.seC_DATE)}</td>
                    <td>${fmtDate(item.refunD_RECDATE)}</td>
                    <td>${item.detentioN_AMT ?? ''}</td>
                    <td>${fmtDate(item.detentioN_DATE)}</td>
                    <td>${item.detentioN_REASON ?? ''}</td>
                    <td>${item.detreasoN_APPROVBY ?? ''}</td>
                    <td>${fmtDate(item.custoM_OOCDATE)}</td>
                    <td>${fmtDate(item.factorY_DATE)}</td>
                    <td>${item.lC_NO ?? ''}</td>
                    <td>${fmtDate(item.lC_DATE)}</td>
                    <td>${item.lC_BENIFICIARY ?? ''}</td>
                    <td>${item.lC_ISSUEBANK ?? ''}</td>
                    <td>${fmtDate(item.lC_EXPIRYDATE)}</td>
                    <td>${item.lC_AMT ?? ''}</td>
                    <td>${fmtDate(item.tentativE_HORETDATE)}</td>
                    <td>${fmtDate(item.movE_DATE)}</td>
                    <td>${fmtDate(item.tenconT_HORETDATE)}</td>
                    <td>${fmtDate(item.latesT_SHIPDATE)}</td>
                    <td>${item.qltY_CLAIMAMT ?? ''}</td>
                    <td>${fmtDate(item.boE_SUBMITDATE)}</td>
                    <td>${item.qC_AMT ?? ''}</td>
                    <td>${item.enhancE_RATE ?? ''}</td>
                    <td>${item.passinG_RATE ?? ''}</td>
                    <td>${item.undeR_PROTEST ?? ''}</td>
                    <td>${item.chA_CHARGETYPE ?? ''}</td>
                    <td>${item.concoR_EXPS ?? 0}</td>
                    <td>${item.chA_AMT ?? 0}</td>
                    <td>${item.cfS_CHARGES ?? 0}</td>
                    <td>${fmtDate(item.cfS_DSTUFFDATE)}</td>
                    <td>${item.cfS_AMT ?? 0}</td>
                    <td>${fmtDate(item.cfS_PAYDATE)}</td>
                    <td>${item.qC_AMTRECD ?? 0}</td>
                    <td>${fmtDate(item.actuaL_CONTHODATE)}</td>
                    <td class="action-col">
                        <div class="action-wrap">
                        <button type="button" class="act-btn edit btn-edit" title="View/Update Tracking"><i class="fa fa-eye"></i></button>
                       </div>
                    </td>
                </tr>
            `);
    });
}

// ====================View/Update Tracking====================


// ====================Document Send====================
async function LoadDropdown() {
    try {
        await Promise.all([
            cmbDoctype(),
            cboVType()
        ]);
        await loadSendTo();

    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbDoctype() {
    try {
        const res = await fetch('/ImportTrackingReport/cmbDoctype');
        const data = await res.json();
        const ddl = $('#ddldoctype');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function cboVType() {
    try {
        const res = await fetch('/ImportTrackingReport/cboVType');
        const data = await res.json();
        const ddl = $('#ddlvtype');
        ddl.empty().append('');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function loadSendTo() {
    try {
        const textbox = $('#txtsearchby').val();
        const v_type = $('#ddlvtype').val();

        const res = await fetch(`/ImportTrackingReport/SendTo?Textbox=${encodeURIComponent(textbox)}&v_type=${encodeURIComponent(v_type)}`);

        if (!res.ok) {
            throw new Error("Failed to load data.");
        }

        const data = await res.json();
        sendToList = data;

        // Fill dropdown
        const ddl = $('#ddlsendto');
        ddl.empty();
        ddl.append('<option value="">-- Select Send To --</option>');

        $.each(sendToList, function (index, item) {
            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

        // If using Select2
        ddl.trigger('change');

    } catch (error) {
        console.error("Error loading Send To list:", error);
    }
}

async function GetDocumentListData() {
    try {

        const DocType = $('#ddldoctype').val();
        const vType = $('#ddlvtype').val();
        const fromDate = $('#Dtfromdate').val();
        const toDate = $('#Dttodate').val();
        const DocStatus = $('#ddlPrintType').val();
        const Searchby = $('#ddlsearchby').val();
        const SearchText = $('#txtsearchby').val();

        if (DocType == "" && vType == "") {
            toastr.info("Data not found");
        }

        const res = await $.ajax({
            url: '/ImportTrackingReport/GetDocumentListData',
            type: 'GET',
            data: {
                FromDate: fromDate,
                ToDate: toDate,
                DocStatus: DocStatus,
                Searchby: Searchby,
                SearchText: SearchText
            }
        });

        if (res.success) {
            bindDocTabeList(res.data);
        }
        else {
            toastr.error(res.message);
        }
    }
    catch (error) {
        console.error("Error:", error);
        toastr.error("Failed to load report data.");
    }
}

function bindDocTabeList(data) {

    console.table("data", data);


    const tDocbody = $('#tbldocumentsendlist tbody');
    tDocbody.empty();

    $('#chkSelectAll').prop('checked', false);

    if (!data || data.length === 0) {
        tDocbody.append(` <tr>  <td colspan="21" class="text-center">No data found.</td> </tr> `);
        return;
    }

    // Build dropdown HTML once
    let sendToOptions = `<option value="">--Select--</option>`;

    sendToList.forEach(user => {
        sendToOptions += `<option value="${user.value}">${user.text}</option>`;
    });

    data.forEach((item, index) => {

        tDocbody.append(`
                <tr id="tr_${index}">
                    <td id="tdVType_${index}">${item.v_TYPE ?? ''}</td>
                    <td id="tdVNo_${index}">${item.v_NO ?? ''}</td>
                    <td id="tdSaudaType_${index}">${item.saudA_TYPE ?? ''}</td>
                    <td id="tdSaudaNo_${index}">${item.saudA_NO ?? ''}</td>
                    <td id="tdVDate_${index}">${item.v_DATE ?? ''}</td>
                    <td id="tdPartyName_${index}">${item.partyName ?? ''}</td>
                    <td id="tdPartyCode_${index}">${item.partY_CODE ?? ''}</td>
                    <td id="tdCityName_${index}">${item.cityName ?? ''}</td>
                    <td id="tdPartyCity_${index}">${item.partY_CITY ?? ''}</td>
                    <td id="tdBillNo_${index}">${item.bilL_NO ?? ''}</td>
                    <td id="tdBillDate_${index}">${item.bilL_DATE ?? ''}</td>
                    <td id="tdInvAmt_${index}" class="text-end">${item.inV_AMT ?? 0}</td>
                    <td id="tdRemarks_${index}">${item.remarks ?? ''}</td>
                    <td id="tdTruckNo_${index}">${item.trucK_NO ?? ''}</td>

                    <td>
                        <select id="ddlSendTo_${index}" class="erppage-input sendToDropdown">  ${sendToOptions} </select>
                    </td>

                    <td id="tdSendFlag_${index}">${item.senD_FLAG ?? ''}</td>
                    <td id="tdSendDate_${index}">${item.senD_DATE ?? ''}</td>
                    <td id="tdRecdFlag_${index}">${item.recD_FLAG ?? ''}</td>
                    <td id="tdRecdDate_${index}">${item.recD_DATE ?? ''}</td>

                    <td class="text-center">
                      <input type="checkbox"  id="chkRow_${index}"  class="row-checkbox"  value="${item.v_TYPE}_${item.v_NO}"  ${(item.senD_FLAG ?? '').toLowerCase() === 'send' ? 'checked' : ''}>
                    </td>


                    <td class="text-center">
                        <button  type="button" id="btnSend_${index}" class="erppage-btn erppage-btn-common btnSendDocument"  data-index="${index}" data-vtype="${item.v_TYPE}"
                        data-vno="${item.v_NO}"> Send
                        </button>
                    </td>
                </tr>
            `);


        if (item.senderCode) {
            $('#ddlSendTo_' + index).val(item.senderCode);
        }

    });
}
