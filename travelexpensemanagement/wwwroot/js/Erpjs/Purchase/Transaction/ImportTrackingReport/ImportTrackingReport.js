$(document).ready(async function () {
    var today = new Date();
    
    var currentDate = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');

    $('#DtDatefrom').val(currentDate);
    $('#Dtto').val(currentDate);

    const currentYear = new Date().getFullYear();
    const fromDate = `${currentYear}-01-01`;
    $('#Dtfromdate').val(fromDate);
    $('#Dttodate').val(currentDate);

    await WireEvents();
});

// ====================EVENTS====================
async function WireEvents() {
    // View data click
    $('#btnviewdata').on('click', async function () {
        GetReportData();
    });

    // Report Search
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

    // Grid view/Update icon click
    $(document).on('click', '#tblImportTrackingReport .btn-edit', function () {
        const saudaNo = $(this).data('sauda-no');
        if (!saudaNo) return;

        window.open('/ImportTrackingEntry/Index?saudaNo=' + encodeURIComponent(saudaNo), '_blank');
    });

    // List of documents modal button click
    $('#listdocumentsendModal').on('shown.bs.modal', async function () {
        $('#Dtfromdate').val($('#DtDatefrom').val());
        $('#Dttodate').val($('#Dtto').val());

        $('#chkSelectAll').prop('checked', false);
        $('#txtsearchby').val('');
        $('#rdAll').prop('checked', true);

        await BindDropdowns();
        await bindDropdown("ImportTrackingReport", "sendto", '#ddlsendto', 'Select Send To', null, null, false, $('#ddlvtype').val(), false);
        await GetDocumentListData();
    });

    // Modal vtype change
    $('#ddlvtype').on('change', async function () {
        const vtype = $(this).val();

        if (!vtype) {
            $('#ddlsendto').empty().append('<option value="">Select Send To</option>');
            return;
        }

        await bindDropdown("ImportTrackingReport", "sendto", '#ddlsendto', 'Select Send To', null, null, false, vtype, false);
    });

    // Modal search
    $('#btn_search').on('click', async function () {
        const docType = $('#ddldoctype').val();
        const vType = $('#ddlvtype').val();

        if (!vType && !docType) {
            showToast("Data not found", {type:"info"});
            $('#tbldocumentsendlist tbody').empty();
            $('#ddldoctype').focus();
            return;
        }

        $('#chkSelectAll').prop('checked', false);
        $('#ddlsendto').val('');
        await GetDocumentListData();
    });

    // Top "Select All" 
    $(document).on('change', '#ChkSelectAll', function () {
        $('#tbldocumentsendlist tbody .row-checkbox').prop('checked', this.checked);
    });

    // Row checkbox 
    $(document).on('change', '#tbldocumentsendlist tbody .row-checkbox', function () {
        const total = $('#tbldocumentsendlist tbody .row-checkbox').length;
        const checked = $('#tbldocumentsendlist tbody .row-checkbox:checked').length;
        $('#ChkSelectAll').prop('checked', total > 0 && total === checked);
    });

    // Row Send button 
    $(document).on('click', '.btnSendDocument', async function (e) {
        e.preventDefault();
        const $btn = $(this);
        const $tr = $btn.closest('tr');
        const row = collectRow($tr);

        if (!row.SEND_TO) {
            showToast("Please select 'Send To' User.", { type:"warning"});
            $tr.find('.sendToDropdown').focus();
            return;
        }

        $btn.prop('disabled', true);
        await postSendDocuments([row]);
        $btn.prop('disabled', false);
    });

    // Top Send button 
    $(document).on('click', '#btn_send', async function (e) {
        e.preventDefault();
        const sendTo = $('#ddlsendto').val();

        if (!sendTo) {
            showToast("Please select 'Send To' User.", { type:"warning"});
            return;
        }

        const rows = [];
        $('#tbldocumentsendlist tbody .row-checkbox:checked').each(function () {
            const $tr = $(this).closest('tr');
            $tr.find('.sendToDropdown').val(sendTo);  
            rows.push(collectRow($tr));
        });

        if (rows.length === 0) {
            showToast("Please select at least one document.", { type:"info"});
            return;
        }

        const $btn = $(this);
        $btn.prop('disabled', true);
        await postSendDocuments(rows);
        $btn.prop('disabled', false);
    });

    // Export To Excel
    $(document).on('click', '.btn-export', function (e) {
        e.preventDefault();

        const params = new URLSearchParams({
            FromDate: $('#DtDatefrom').val(),
            ToDate: $('#Dtto').val()
        });

        window.location.href = `/ImportTrackingReport/ExportToExcel?${params.toString()}`;
    });
}


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
            showToast(res.message, { type:"error"});
        }

    } catch (error) {
        console.error("Error:", error);
        showToast("Failed to load report data.", { type:"error"});
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

        const rowClass = item.isTrackingUpdated === 1 ? 'tracking-updated-row' : '';

        tbody.append(`
                <tr class="${rowClass}">
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
                        <button type="button" class="act-btn edit btn-edit" title="View/Update Tracking" data-sauda-no="${item.saudaNo ?? ''}"><i class="fa fa-eye"></i></button>
                        <button type="button" class="act-btn view btn-export" title="Export to Excel"><i class="fa fa-file-excel"></i></button>
                       </div>
                    </td>
                </tr>
            `);
    });
}


// ====================Document Send List====================
async function BindDropdowns() {
    await Promise.all([
        bindDropdown("ImportTrackingReport", "doctype", '#ddldoctype', '', 'EXIMTracking', null, true, null, false),
        bindDropdown("ImportTrackingReport", "vtype", '#ddlvtype', 'Select V Type', 'EXIM', null, true, null, false),
    ]);
}
async function GetDocumentListData() {
    try {
        const DocType = $('#ddldoctype').val();
        const vType = $('#ddlvtype').val();

        if (!DocType && !vType) {
            showToast("Data not found", { type:"info"});
            $('#tbldocumentsendlist tbody').empty();
            return;
        }

        const res = await $.ajax({
            url: '/ImportTrackingReport/GetDocumentListData',
            type: 'GET',
            data: {
                FromDate: $('#Dtfromdate').val(),
                ToDate: $('#Dttodate').val(),
                vType: vType,
                DocStatus: $('input[name="PrintType"]:checked').val() || '',
                Searchby: $('#ddlsearchby').val() || '',
                SearchText: $('#txtsearchby').val() || ''
            }
        });

        if (res.success) {
            bindDocTabeList(res.data);
        } else {
            showToast(res.message, { type:"error"});
        }
    } catch (error) {
        console.error("Error:", error);
        showToast("Failed to load report data.", { type:"error"});
    }
}
function bindDocTabeList(data) {
    const tbody = $('#tbldocumentsendlist tbody');
    tbody.empty();
    $('#chkSelectAll').prop('checked', false);

    if (!data || data.length === 0) {
        tbody.append(`<tr><td colspan="15" class="text-center">No data found.</td></tr>`);
        return;
    }

    // Build Send To options once from the top Send To dropdown
    let sendToOptions = `<option value="">--Select--</option>`;
    $('#ddlsendto option').each(function () {
        if (this.value) sendToOptions += `<option value="${this.value}">${$(this).text()}</option>`;
    });

    data.forEach((item, index) => {
        const rowClass = item.recD_DATE ? 'row-received' : (item.senD_DATE ? 'row-sent' : '');

        tbody.append(`
            <tr id="tr_${index}" class="${rowClass}"
                data-vtype="${item.v_TYPE ?? ''}" data-vno="${item.v_NO ?? ''}"
                data-saudatype="${item.saudA_TYPE ?? ''}"
                data-partycode="${item.partY_CODE ?? ''}" data-partycity="${item.partY_CITY ?? ''}"
                data-truckno="${item.trucK_NO ?? ''}">
                <td>${item.saudA_NO ?? ''}</td>
                <td>${item.v_DATE ?? ''}</td>
                <td>${item.partyName ?? ''}</td>
                <td>${item.cityName ?? ''}</td>
                <td>${item.bilL_NO ?? ''}</td>
                <td>${item.bilL_DATE ?? ''}</td>
                <td class="text-end">${item.inV_AMT ?? 0}</td>
                <td><input type="text" class="erppage-input remarks-input" value="${item.remarks ?? ''}" /></td>
                <td><select id="ddlSendTo_${index}" class="erppage-input sendToDropdown">${sendToOptions}</select></td>
                <td>${item.senD_FLAG ?? ''}</td>
                <td>${item.senD_DATE ?? ''}</td>
                <td>${item.recD_FLAG ?? ''}</td>
                <td>${item.recD_DATE ?? ''}</td>
                <td class="text-center"><input type="checkbox" id="chkRow_${index}" class="row-checkbox"></td>
                <td class="text-center">
                    <button type="button" id="btnSend_${index}" class="erppage-btn erppage-btn-common btnSendDocument" data-index="${index}">Send</button>
                </td>
            </tr>`);

        if (item.receiverCode) {
            $('#ddlSendTo_' + index).val(item.receiverCode);
        }
    });
}


// ====================Send Documents====================
function collectRow($tr) {
    const td = $tr.find('td');
    return {
        V_TYPE: String($tr.data('vtype') ?? ''),
        V_NO: parseInt($tr.data('vno')) || null,
        SAUDA_TYPE: String($tr.data('saudatype') ?? ''),
        SAUDA_NO: parseInt(td.eq(0).text().trim()) || null,
        V_DATE: convertDate(td.eq(1).text().trim()),
        PARTY_CODE: parseInt($tr.data('partycode')) || null,
        PARTY_NAME: td.eq(2).text().trim(),
        CITY_CODE: parseInt($tr.data('partycity')) || null,
        CITY_NAME: td.eq(3).text().trim(),
        BILL_NO: td.eq(4).text().trim(),
        BILL_DATE: convertDate(td.eq(5).text().trim()),
        BILL_AMT: parseFloat(td.eq(6).text().trim()) || null,
        TRUCK_NO: String($tr.data('truckno') ?? ''),
        SEND_TO: parseInt($tr.find('.sendToDropdown').val()) || null,
        REMARKS: $tr.find('.remarks-input').val() || ''
    };
}
async function postSendDocuments(rows) {
    try {
        const res = await $.ajax({
            url: '/ImportTrackingReport/SendDocument',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(rows)
        });

        if (res.validation) { showToast(res.message, { type: "warning" }); return; }
        if (res.message && !res.success) { showToast(res.message, { type:"error" }); }

        if (res.sent > 0) showToast(res.sent + " document(s) sent.", { type: "success" });
        if (res.already > 0) showToast(res.already + " document(s) already sent.", { type:"warning"});

        await GetDocumentListData();   
    } catch (xhr) {
        console.error(xhr);
        showToast("Error sending document.", { type:"error"});
    }
}

// ====================Toast above modal====================
$(document).on('shown.bs.modal', '#listdocumentsendModal', function () {
    const el = document.getElementById('erp-toast-container');
    if (!el) return;
    el._origParent = el._origParent || el.parentNode;
    document.body.appendChild(el);                        // out of any stacking context
    el.style.setProperty('position', 'fixed', 'important');
    el.style.setProperty('z-index', '2147483647', 'important');
});

$(document).on('hidden.bs.modal', '#listdocumentsendModal', function () {
    const el = document.getElementById('erp-toast-container');
    if (!el || !el._origParent) return;
    el._origParent.appendChild(el);
    el.style.removeProperty('position');
    el.style.removeProperty('z-index');
});