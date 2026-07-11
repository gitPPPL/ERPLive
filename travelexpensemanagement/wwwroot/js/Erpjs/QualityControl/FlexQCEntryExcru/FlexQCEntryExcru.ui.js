function generateSelect(options, selected = "") {
    return options.map(opt => `<option ${selected === opt ? 'selected' : ''}>${opt}</option>`).join('');
}
function collectInsertRows(tableSelector = '#tblFlakesQCEntry') {
    return $(tableSelector).find('tbody tr').map((_, tr) => {
        const $tr = $(tr);

        return {
            Refcode: parseInt($tr.find('td:first').text().trim() || "0"),
            ITEM_CODE: parseInt($tr.find('.ITEM_CODE').val() || "0"),
            Item_Name: $tr.find('.item-name-select option:selected').text().trim(),
            DEPT_CODE: parseInt($tr.find('.DEPT_CODE').val() || "0"),
            DEPT_NAME: $tr.find('.ddlItem').val()?.trim() || "",
            BatchNo: $tr.find('.BatchNo').val()?.trim() || "",
            BagNo: parseDecimal($tr.find('.bagNo').val()),           
            JUMBO_NO: $tr.find('.JUMBO_NO').val()?.trim() || "",
            WBWt: parseDecimal($tr.find('.wbWt').val()),
            GrWt: parseDecimal($tr.find('.grWt').val()),
            TrWt: parseDecimal($tr.find('.trWt').val()),
            NET_WT: parseDecimal($tr.find('.NET_WT').val()),
            MFI: parseDecimal($tr.find('.MFI').val()),
            ASH_CONTENT: parseDecimal($tr.find('.ASHContent').val()),
            PP: parseDecimal($tr.find('.pp').val()),
            HD: parseInt($tr.find('.HD').val() || "0"),
            LD: parseDecimal($tr.find('.LD').val()),
            COLOR_MIX: parseDecimal($tr.find('.COLOR_NAME').val()),
            MOIS_CONTENT: parseDecimal($tr.find('.MoisContent').val()),
            BOTTOM: parseDecimal($tr.find('.Bottom').val()),
            FOAM: parseDecimal($tr.find('.FOAM').val()),
            RUBBER: parseDecimal($tr.find('.RUBBER').val()),
            WRAPPER: parseDecimal($tr.find('.WRAPPER').val()),
            STATUS_CODE: parseInt($tr.find('.status').val() || "0"),
            STATUSS: $tr.find('.status option:selected').text().trim(),
            REMARKS: $tr.find('.Remarks').val()?.trim() || "",
            REfType: $tr.find('.Pord_Type').val()?.trim() || "",
            Ref_Type: $tr.find('.Pord_Type').val()?.trim() || "",
            Ref_No: parseInt($tr.find('.REF_NO').val() || "0")
        };

    }).get();
}
function parseDecimal(value) {
    const parsed = parseFloat(value?.trim() || '0');
    return isNaN(parsed) ? 0 : parsed;
}
function formatDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    if (isNaN(date)) return null;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
function getSelectedRowsData() {
    let selectedRowsData = [];

    $('#tblCopyFrommodal tbody tr').each(function () {
        const checkbox = $(this).find('input[type="checkbox"]');

        if (checkbox.prop('checked')) {

            const rowData = {
                code: '', 
                bagNo: $(this).find('td:nth-child(2)').text().trim(),
                item_Name: $(this).find('td:nth-child(13)').text().trim(),
                depT_NAME: $(this).find('td:nth-child(15)').text().trim(),
                batchNo: $(this).find('td:nth-child(5)').text().trim(),
                wbWt: $(this).find('td:nth-child(7)').text().trim(),
                grWt: $(this).find('td:nth-child(8)').text().trim(),
                trWt: $(this).find('td:nth-child(9)').text().trim(),
                neT_WT: $(this).find('td:nth-child(10)').text().trim(),
                rEfType: $(this).find('td:nth-child(11)').text().trim(),
                refcode: $(this).find('td:nth-child(12)').text().trim(),
                iteM_CODE: $(this).find('td:nth-child(13)').text().trim(),
                depT_CODE: $(this).find('td:nth-child(14)').text().trim(),
                // Optional fields
                mfi: '',
                asH_CONTENT: '',
                pp: '',
                hd: '',
                ld: '',
                coloR_MIX: '',
                moiS_CONTENT: '',
                bottom: '',
                statuss: '',
                remarks: ''
            };
            selectedRowsData.push(rowData);
        }
    });

    return selectedRowsData;
}
function populateTableRowsFromData(details = []) {
    if (!Array.isArray(details)) return;

    details.forEach(detail => {

        const data = mapDetailToRowData(detail);

        console.log("populateTableRowsFromData", data);

        const cells = [
            `<td style="display:none;">${data.code}</td>`,
            `<td><input type="text" class="form-control ITEM_CODE" value="${data.iteM_CODE}" readonly /></td>`,
            `<td><select class="form-control item-name-select" style="width:300px; background-color:#dbeafe; disabled">  <option value="">--Select--</option> ${itemNameOptions}  </select></td>`,
            `<td><input type="text" class="form-control ddlItem" value="${data.dept}" readonly style="width: 200px; background-color:#dbeafe;" /></td>`,
            `<td><input type="text" class="form-control BatchNo" value="${data.batchNo}" readonly style= "background-color:#dbeafe;" /></td>`,
            `<td><input type="text" class="form-control bagNo" value="${data.bagNo}" readonly style= "background-color:#dbeafe;" /></td>`,
            `<td><input type="text" class="form-control JUMBO_NO" value="${data.jumbO_NO}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control wbWt" value="${data.wbWt}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control grWt" value="${data.grWt}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control trWt" value="${data.trWt}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control NET_WT" value="${data.netWt}" readonly  style="width: 60px;background-color:#dbeafe;"/></td>`,
            `<td style="text-align:center; width:80px;">  <button type="button"  class="btn btn-sm btn-primary btn-row-action">  Copy  </button>  </td>`,
            `<td><input type="number" class="form-control MFI" value="${data.MFI}" style="width: 60px;background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control ASHContent" value="${data.asH_CONTENT}"  style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control pp" value="${data.pp}"  style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control HD" value="${data.HD}"  style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control LD" value="${data.LD}"  style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control COLOR_NAME" value="${data.colorMix}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control wrapper" value="${data.wrapper}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control FOAM" value="${data.foam}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control RUBBER" value="${data.rubber}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control MoisContent" value="${data.MoisContent}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control Bottom" value="${data.Bottom}" style="background-color:#d4edda;"  /></td>`,
            `<td><select class="form-control status" style="width:300px; background-color:#dbeafe; disabled">  <option value="">--Select--</option> ${DDLGridStatuslist}  </select></td>`,                       
            `<td><input type="text" class="form-control Remarks" value="${data.remarks}" style="background-color:#d4edda;" /></td>`,
            `<td><input type="text" class="form-control Pord_Type" value="${data.refType}" readonly  style="background-color:#dbeafe;"/></td>`,
            `<td><input type="number" class="form-control REF_NO" value="${data.refNo}" readonly style="background-color:#dbeafe;" /></td>`,
            `<td> <input type="text" class="form-control DEPT_CODE"  value="${data.depT_CODE}"  readonly style="background-color:#dbeafe;" />  </td>`
        ];

        const row = `<tr class="no-border-input">${cells.join('')}</tr>`;
        $tbody.append(row);
        if (data.itemName) {
            $tbody.find('tr:last .item-name-select').val(data.itemName);
        }

        if (data.statusCode) {
            $tbody.find('tr:last .status').val(data.statusCode);
        } 

    });

    if (mode == "view") {
        setFormReadOnly();
    }

}
function mapDetailToRowData(detail) {

    console.log("mapDetailToRowData", detail);



    return {
        code: detail.refcode ?? '',
        iteM_CODE: detail.iteM_CODE ?? '',
        itemName: detail.iteM_CODE ?? '',
        dept: detail.depT_NAME ?? '',
        batchNo: detail.batchNo ?? '',
        bagNo: detail.bagNo ?? '',
        jumbO_NO: detail.jumbO_NO ?? '',
        wbWt: detail.wbWt ?? '',
        grWt: detail.grWt ?? '',
        trWt: detail.trWt ?? '',
        netWt: detail.neT_WT ?? '',
        MFI: detail.mfi ?? '',
        asH_CONTENT: detail.asH_CONTENT ?? '',
        pp: detail.pp ?? '',
        HD: detail.hd ?? '',
        LD: detail.ld ?? '',
        colorMix: detail.coloR_MIX ?? '',
        wrapper: detail.wrapper ?? '',
        foam: detail.foam ?? '',
        rubber: detail.rubber ?? '',
        MoisContent: detail.moiS_CONTENT ?? '',
        Bottom: detail.bottom ?? '',
        statusCode: detail.statuS_CODE ?? '',  
        status: detail.statuss ?? '',
        remarks: detail.remarks ?? '',
        refType: detail.rEfType ?? '',
        refNo: detail.refcode ?? '',
        depT_CODE: detail.depT_CODE ?? ''
    };
}
function setFormReadOnly() {

    const form = $('#FlakesQCEntryForm'); form.find(':input').prop('disabled', true);
    form.find('textarea').css('background-color', '#f0f0f0');
    form.find('table tbody tr').css('background-color', '#f9f9f9');
    $('#tblFlakesQCEntry tbody tr').each(function () {
        $(this).find(':input').prop('disabled', true);
        $(this).css('background-color', '#f9f9f9');

        form.find('select').prop('disabled', true);
        $('#tblFlakesQCEntry tbody').find('select').prop('disabled', true);
    });

    $('#btn-saves').hide();
    $('#btn-Cancel').hide();
}
function calculateTotalForRow($row) {
    const hdpe = parseFloat($row.find('.HD').val()) || 0;
    const pvcPpm = parseFloat($row.find('.DNR').val()) || 0;
    const pcLowMelt = parseFloat($row.find('.PC_LOWMELT').val()) || 0;
    const wrapper = parseFloat($row.find('.CPRDN').val()) || 0;
    const metal = parseFloat($row.find('.TIME1_WIDTH').val()) || 0;
    const stone = parseFloat($row.find('.TIME2_WIDTH').val()) || 0;
    const rubber = parseFloat($row.find('.TIME3_WIDTH').val()) || 0;
    const glue = parseFloat($row.find('.GLUE_CONTENT').val()) || 0;
    const other = parseFloat($row.find('.OTHERS').val()) || 0;

    const total = hdpe + pvcPpm + pcLowMelt + wrapper + metal + stone + rubber + glue + other;

    $row.find('.TIME4_WIDTH').val(total.toFixed(2));

    return total;
}
function handleBack(redirectUrl, isReadOnly = false) {

    if (isReadOnly) {
        window.location.href = redirectUrl;
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: "Unsaved data will be lost.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, exit',
        cancelButtonText: 'Stay',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = redirectUrl;
        }
    });
}