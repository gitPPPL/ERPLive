$(document).ready(async function () {
    var today = new Date();

    var currentDate = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');

    $('#DtDatefrom').val(currentDate);
    $('#Dtto').val(currentDate);

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
        $('#tblExportTrackingReport tbody tr').each(function () {
            let found = false;
            $(this).find('td').each(function () {
                if ($(this).text().toLowerCase().indexOf(value) > -1) {
                    found = true;
                    return false;
                }
            });
            $(this).toggle(found);
        });

        const total = $('#tblExportTrackingReport tbody tr:visible').length;
        $('#lblTotalCount').text(`Total Sauda : ${total}`);
    });

    // Export To Excel
    $(document).on('click', '.btn-export', function (e) {
        e.preventDefault();

        const params = new URLSearchParams({
            FromDate: $('#DtDatefrom').val(),
            ToDate: $('#Dtto').val(),
            Status: ($('#ddlstatus').val() || '').trim(),
            WithContainerDetails: $('#chkContainerDetails').is(':checked') ? 1 : 0
        });

        window.location.href = `/ExportTrackingReport/ExportToExcel?${params.toString()}`;
    });
}


// ====================EXPORT TRACKING LIST====================
async function GetReportData() {
    try {

        const fromDate = $('#DtDatefrom').val();
        const toDate = $('#Dtto').val();
        const status = ($('#ddlstatus').val() || '').trim();
        const withContainerDetails = $('#chkContainerDetails').is(':checked') ? 1 : 0;

        const res = await $.ajax({
            url: '/ExportTrackingReport/GetReportData',
            type: 'GET',
            data: { FromDate: fromDate, ToDate: toDate, Status: status, WithContainerDetails: withContainerDetails }
        });
        if (res.success) {
            const withContainerDetails = $('#chkContainerDetails').is(':checked');
            $('.container-col').toggle(withContainerDetails);
            bindExportTrackingReport(res.data);
        } else {
            showToast(res.message, { type: "error" });
        }

    } catch (error) {
        console.error("Error:", error);
        showToast("Failed to load report data.", { type: "error" });
    }
}
function fmtDate(v) {
    if (!v) return '';
    const d = new Date(v);
    return isNaN(d) ? v : d.toLocaleDateString('en-GB');
}
function bindExportTrackingReport(data) {
    const tbody = $('#tblExportTrackingReport tbody');
    tbody.empty();

    console.log("Export Tracking Report List: ", data);

    const total = data ? data.length : 0;
    $('#lblTotalCount').text(`Total Sauda : ${total}`);

    if (!data || data.length === 0) {
        tbody.append(`<tr> <td colspan="123" class="text-center">No data found.</td>  </tr> `);
        return;
    }


    const withContainerDetails = $('#chkContainerDetails').is(':checked');

    data.forEach((item, index) => {

        const rowClass = item.isTrackingUpdated === 1 ? 'tracking-updated-row' : '';

        tbody.append(`
            <tr class="${rowClass}">
                <td>${item.saudA_NO ?? ''}</td>
                <td>${fmtDate(item.saudA_DATE)}</td>
                <td>${item.sI_NO ?? ''}</td>
                <td>${fmtDate(item.inV_DATE)}</td>
                <td>${item.pino ?? ''}</td>
                <td>${item.buyeR_ORD_NO ?? ''}</td>
                <td>${item.currency ?? ''}</td>
                <td>${item.partY_NAME ?? ''}</td>
                <td>${item.location ?? ''}</td>
                <td>${item.iteM_TYPE ?? ''}</td>

                ${withContainerDetails ? `
                    <td>${item.packing_slip_no ?? ''}</td>
                    <td>${item.container_no ?? ''}</td>
                    <td>${item.line_seal_no ?? ''}</td>
                    <td>${item.custom_seal_no ?? ''}</td>
                ` : ''}

                <td>${item.paymenT_TERM ?? ''}</td>
                <td>${item.incoterm ?? ''}</td>
                <td>${item.solD_BY ?? ''}</td>
                <td>${item.saudA_QTY ?? 0}</td>
                <td>${item.inV_QTY ?? 0}</td>
                <td>${item.exchangE_RATE ?? 0}</td>
                <td>${item.ratE_KG_IN_USD ?? 0}</td>
                <td>${item.basiC_AMOUNT ?? 0}</td>
                <td>${item.usD_AMOUNT ?? 0}</td>
                <td>${item.discount ?? 0}</td>
                <td>${item.actuaL_FREIGHT ?? 0}</td>
                <td>${item.insurance ?? 0}</td>
                <td>${item.commission ?? 0}</td>
                <td>${item.foB_VALUE ?? 0}</td>
                <td>${item.cgst ?? 0}</td>
                <td>${item.sgst ?? 0}</td>
                <td>${item.igst ?? 0}</td>
                <td>${item.transport ?? ''}</td>
                <td>${item.modE_OF_TRANSPORT ?? ''}</td>
                <td>${fmtDate(item.sblC_DUE_DATE)}</td>
                <td>${fmtDate(item.lC_DUE_DATE)}</td>
                <td>${item.placE_OF_RECEIPT ?? ''}</td>
                <td>${item.pol ?? ''}</td>
                <td>${item.pod ?? ''}</td>
                <td>${item.finaL_PLACE_OF_DELIVERY ?? ''}</td>
                <td>${item.finaL_COUNTRY ?? ''}</td>
                <td>${item.finaL_DESTINATION ?? ''}</td>
                <td>${item.bL_TYPE ?? ''}</td>
                <td>${item.custoM_CLEARANCE_PORT ?? ''}</td>
                <td>${item.firsT_NOTIFY_PARTY ?? ''}</td>
                <td>${item.seconD_NOTIFY_PARTY ?? ''}</td>
                <td>${item.commissioN_AGENT ?? ''}</td>
                <td>${item.lC_NUMBER ?? ''}</td>
                <td>${fmtDate(item.lC_DATE)}</td>
                <td>${item.banK_NAME ?? ''}</td>
                <td>${item.accounT_NUMBER ?? ''}</td>
                <td>${item.ifsC_CODE ?? ''}</td>
                <td>${item.aD_CODE ?? ''}</td>
                <td>${item.swifT_CODE ?? ''}</td>
                <td>${item.banK_BRANCH ?? ''}</td>
                <td>${item.naturE_KIND_OF_PACKING ?? ''}</td>
                <td>${item.technicaL_SPECIFICATION ?? ''}</td>
                <td>${item.speciaL_INSTRUCTION ?? ''}</td>
                <td>${item.forwarder ?? ''}</td>
                <td>${item.cha ?? ''}</td>
                <td>${item.shippinG_LINE ?? ''}</td>
                <td>${item.shiP_BILL_NO ?? ''}</td>
                <td>${fmtDate(item.shiP_BILL_DATE)}</td>
                <td>${item.bilL_BOOKING_NO ?? ''}</td>
                <td>${item.bL_NO ?? ''}</td>
                <td>${fmtDate(item.bL_DATE)}</td>
                <td>${fmtDate(item.bL_CONFIRM_DATE)}</td>
                <td>${fmtDate(item.bL_RECEIVED_DATE)}</td>
                <td>${item.bL_REMARKS ?? ''}</td>
                <td>${item.bL_LAST_ACTIVITY ?? ''}</td>
                <td>${item.vesseL_NO ?? ''}</td>
                <td>${item.vyoG_NO ?? ''}</td>
                <td>${fmtDate(item.soB_DATE)}</td>
                <td>${fmtDate(item.sI_SUBMIT_DATE)}</td>
                <td>${fmtDate(item.etA_POL_DATE)}</td>
                <td>${fmtDate(item.etA_POD_DATE)}</td>
                <td>${item.epcG_LIC_NO ?? ''}</td>
                <td>${item.exporT_PER_NO ?? ''}</td>
                <td>${item.egM_NO ?? ''}</td>
                <td>${fmtDate(item.egM_DATE)}</td>
                <td>${item.totaL_SB_DDBK ?? 0}</td>
                <td>${item.totaL_SB_RODTAP ?? 0}</td>
                <td>${item.adV_LIC_NO ?? ''}</td>
                <td>${item.taX_TYPE ?? ''}</td>
                <td>${item.adV_PAYMENT_AMT_USD ?? ''}</td>
                <td>${item.adV_RECEIVED_DATE ?? ''}</td>
                <td>${item.adV_PAYMENT_REF_NO ?? ''}</td>
                <td>${item.balancE_PAYMENT_AMT ?? ''}</td>
                <td>${item.balancE_PAYMENT_RECEIVED_DATE ?? ''}</td>
                <td>${item.balancE_PAYMENT_REF_NO ?? ''}</td>
                <td>${item.commissioN_BILL_NO ?? ''}</td>
                <td>${fmtDate(item.commissioN_BILL_DATE)}</td>
                <td>${item.commissioN_BILL_STATUS ?? ''}</td>
                <td>${item.brC_NUMBER ?? ''}</td>
                <td>${fmtDate(item.brC_DATE)}</td>
                <td>${item.documenT_LODGEMENT_NO ?? ''}</td>
                <td>${fmtDate(item.documenT_LODGEMENT_DATE)}</td>
                 <td class="action-col">
                     <div class="action-wrap">
                         <button type="button" class="act-btn view btn-export" title="Export to Excel"><i class="fa fa-file-excel"></i></button>
                     </div>
                 </td>
            </tr>
        `);
    });
}

