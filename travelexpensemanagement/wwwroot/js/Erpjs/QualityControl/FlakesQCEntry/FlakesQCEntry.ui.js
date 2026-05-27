function generateSelect(options, selected = "") {
    return options.map(opt => `<option ${selected === opt ? 'selected' : ''}>${opt}</option>`).join('');
}
function collectInsertRows(tableSelector = '#tblFlakesQCEntry') {
    return $(tableSelector).find('tbody tr').map((_, tr) => {
        const $tr = $(tr);
        return {
            BLUEP: parseDecimal($tr.find('.BLUEP').val()),
            BULKDENSITY: parseDecimal($tr.find('.BULKDENSITY').val()),
            DEPT_CODE: parseInt($tr.find('.DEPT_CODE').val()?.trim() || '0'),
            TIME5_WIDTH: parseDecimal($tr.find('.COLOR_NAME').val()),
            DEPT_NAME: $tr.find('.ddlItem').val()?.trim() || '',
            CPRDN: parseDecimal($tr.find('.CPRDN').val()),
            DNR: $tr.find('.DNR').val()?.trim() || '',
            GRADE: $tr.find('.GRADE').val()?.trim() || '',
            GLUE_CONTENT: parseDecimal($tr.find('.GLUE_CONTENT').val()),
            WBWt: parseDecimal($tr.find('.wbWt').val()),
            GrWt: parseDecimal($tr.find('.grWt').val()),

            ITEM_CODE: parseInt($tr.find('.ITEM_CODE').val()?.trim() || '0'),
            MOISTURE: parseDecimal($tr.find('.MOISTURE').val()),
            NET_WT: parseDecimal($tr.find('.NET_WT').val()),
            OTHERP: parseDecimal($tr.find('.OTHERP').val()),
            OTHERS: parseDecimal($tr.find('.OTHERS').val()),
            OVERSIZED: parseDecimal($tr.find('.OVERSIZED').val()),
            PC_LOWMELT: parseDecimal($tr.find('.PC_LOWMELT').val()),
            PH_FLAKES: parseDecimal($tr.find('.PH_FLAKES').val()),
            Pord_No: parseInt($tr.find('.REF_NO').val()?.trim() || '0'),
            Pord_Type: $tr.find('.Pord_Type').val()?.trim() || '',
            PRKG: parseDecimal($tr.find('.NET_WT').val()),
            PTYPE_NAME: $tr.find('.BatchNo').val()?.trim() || '',
            REMARKS: $tr.find('.REMARKS').val()?.trim() || '',
            RESULT1: parseDecimal($tr.find('.grWt').val()),
            RESULT2: parseDecimal($tr.find('.trWt').val()),
            WASTE: parseDecimal($tr.find('.HD').val()),
            WIDTH: parseDecimal($tr.find('.bagNo').val()),
            YELLOW160C: parseDecimal($tr.find('.YELLOW160C').val()),
            YELLOWP: parseDecimal($tr.find('.YELLOWP').val()),
            TIME1_WIDTH: parseDecimal($tr.find('.TIME1_WIDTH').val()),
            TIME2_WIDTH: parseDecimal($tr.find('.TIME2_WIDTH').val()),
            TIME3_WIDTH: parseDecimal($tr.find('.TIME3_WIDTH').val()),
            TIME4_WIDTH: parseDecimal($tr.find('.TIME4_WIDTH').val()),
            Refcode: parseInt($tr.find('.REF_NO').val()?.trim() || '0'),
            REfType: $tr.find('.Pord_Type').val()?.trim() || '',
        };
    }).get();
}
function parseDecimal(value) {
    const parsed = parseFloat(value?.trim() || '0');
    return isNaN(parsed) ? 0 : parsed;
}
function mapDetailToRowData(detail) {
    return {
        code: detail.refcode || '',
        iCode: detail.iteM_CODE || '',
        itemName: detail.iteM_CODE || '',
        dept: detail.depT_NAME || '',
        batchNo: detail.batchNo || '',
        bagNo: detail.bagNo || '',
        wbWt: detail.wbWt || '',
        grWt: detail.grWt || '',
        trWt: detail.trWt || '',
        netWt: detail.neT_WT || '',
        hdpe: detail.waste || '',
        pvcPpm: detail.dnr || '',
        pcLowMelt: detail.pC_LOWMELT || '',
        wrapper: detail.cprdn || '',
        metal: detail.timE1_WIDTH || '',
        stone: detail.timE2_WIDTH || '',
        rubber: detail.timE3_WIDTH || '',
        glue: detail.gluE_CONTENT || '',
        other: detail.others || '',
        total: detail.timE4_WIDTH || '',
        grd: detail.grade || '',
        yellow: detail.yellowp || '',
        blue: detail.bluep || '',
        otherPercent: detail.otherp || '',
        colorMix: detail.timE5_WIDTH || '',
        yellow160c: detail.yelloW160C || '',
        moisture: detail.moisture || '',
        bulkDensity: detail.bulkdensity || '',
        ph: detail.pH_FLAKES || '',
        overSized: detail.oversized || '',
        remarks: detail.remarks || '',
        refType: detail.rEfType || '',
        refNo: detail.refcode || '',
        deptCode: detail.depT_CODE || '',
        placeCode: detail.placeCode || '',
        placeName: detail.placeName || '',

    };
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
    var selectedRowsData = [];
    $('#tblCopyFrommodal tbody tr').each(function () {
        var checkbox = $(this).find('input[type="checkbox"]');
        if (checkbox.prop('checked')) {
            var rowData = {};
            rowData.bagNo = $(this).find('td:nth-child(2)').text().trim();
            rowData.itemName = $(this).find('td:nth-child(3)').text().trim();
            rowData.prodPlace = $(this).find('td:nth-child(4)').text().trim();
            rowData.lotNo = $(this).find('td:nth-child(5)').text().trim();
            rowData.wbQty = $(this).find('td:nth-child(6)').text().trim();
            rowData.grossQty = $(this).find('td:nth-child(7)').text().trim();
            rowData.tareQty = $(this).find('td:nth-child(8)').text().trim();
            rowData.qty = $(this).find('td:nth-child(9)').text().trim();
            rowData.vType = $(this).find('td:nth-child(10)').text().trim();
            rowData.vNo = $(this).find('td:nth-child(11)').text().trim();
            rowData.itemCode = $(this).find('td:nth-child(12)').text().trim();
            rowData.deptCode = $(this).find('td:nth-child(13)').text().trim();
            rowData.DEPT_NAME = $(this).find('td:nth-child(14)').text().trim();
            selectedRowsData.push(rowData);
        }
    });
    return selectedRowsData;
}
function addRow(data = {}) {

    $tbody.find('.btn-add-action').remove();

    const cells = [

        `<td style="display:none;">${data.code || ''}</td>`,

        `<td>  <input type="text"
                       class="form-control ITEM_CODE"
                       value="${data.iCode || ''}"
                       readonly
                       style="width:200px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <select class="form-control item-name-select" disabled
                        style="width:300px;background-color:#dbeafe;">
                    <option value="">--Select--</option>
                    ${itemNameOptions}
                </select>
            </td>`,

        `<td style="width:100px">
                <input type="text"
                       class="form-control ddlItem" 
                       value="${data.dept || ''}"
                       readonly
                       style="width:200px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="text"
                       class="form-control BatchNo"
                       value="${data.batchNo || ''}"
                       readonly
                       style="width:60px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="text"
                       class="form-control bagNo"
                       value="${data.bagNo || ''}"
                       readonly
                       style="width:60px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control wbWt"
                       value="${data.wbWt || ''}"
                       readonly
                       style="width:60px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control grWt"
                       value="${data.grWt || ''}"
                       readonly
                       style="width:60px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control trWt"
                       value="${data.trWt || ''}"
                       readonly
                       style="width:60px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control NET_WT"
                       value="${data.netWt || ''}"
                       readonly
                       style="width:60px;background-color:#dbeafe;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control HD"
                       value="${data.hdpe || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control DNR"
                       value="${data.pvcPpm || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control PC_LOWMELT"
                       value="${data.pcLowMelt || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control CPRDN"
                       value="${data.wrapper || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control TIME1_WIDTH"
                       value="${data.metal || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control TIME2_WIDTH"
                       value="${data.stone || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control TIME3_WIDTH"
                       value="${data.rubber || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control GLUE_CONTENT"
                       value="${data.glue || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control OTHERS"
                       value="${data.other || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control TIME4_WIDTH"
                       value="${data.total || ''}"
                       readonly
                       style="width:80px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="text"
                       class="form-control GRADE"
                       value="${data.grd || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control YELLOWP"
                       value="${data.yellow || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control BLUEP"
                       value="${data.blue || ''}"
                       style="width:60px;background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control OTHERP"
                       value="${data.otherPercent || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="text"
                       class="form-control COLOR_NAME"
                       value="${data.colorMix || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control YELLOW160C"
                       value="${data.yellow160c || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control MOISTURE"
                       value="${data.moisture || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control BULKDENSITY"
                       value="${data.bulkDensity || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control PH_FLAKES"
                       value="${data.ph || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="number"
                       class="form-control OVERSIZED"
                       value="${data.overSized || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td>
                <input type="text"
                       class="form-control REMARKS"
                       value="${data.remarks || ''}"
                       style="background-color:#d4edda;" />
            </td>`,

        `<td style="display:none;">
                <input type="text"
                       class="form-control Pord_Type"
                       value="${data.refType || ''}" />
            </td>`,

        `<td style="display:none;">
                <input type="text"
                       class="form-control REF_NO"
                       value="${data.refNo || ''}" />
            </td>`,

        `<td style="display:none;">
                <input type="text"
                       class="form-control DEPT_CODE"
                       value="${data.deptCode || ''}" />
            </td>`
    ];

    const row = `<tr class="no-border-input">${cells.join('')}</tr>`;

    $tbody.append(row);

    if (data.itemName) {
        $tbody.find('tr:last .item-name-select')
            .val(data.itemName)
            .trigger('change');
    }
}
function populateTableRowsFromData(details = []) {
    if (!Array.isArray(details)) return;
    $tbody.empty();
    details.forEach(detail => {
        const data = mapDetailToRowData(detail);
        const cells = [
            `<td style="display:none;">${data.code}</td>`,
            `<td><input type="text" class="form-control ITEM_CODE" value="${data.iCode}" readonly /></td>`,

            `<td><select class="form-control item-name-select" style="width:300px; background-color:#dbeafe; disabled">
                    <option value="">--Select--</option>
                    ${itemNameOptions}
                </select></td>`,
            `<td><input type="text" class="form-control ddlItem" value="${data.placeName}" readonly style="width: 200px; background-color:#dbeafe;" /></td>`,
            `<td><input type="text" class="form-control BatchNo" value="${data.batchNo}" readonly style= "background-color:#dbeafe;" /></td>`,
            `<td><input type="text" class="form-control bagNo" value="${data.bagNo}" readonly style= "background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control wbWt" value="${data.wbWt}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control grWt" value="${data.grWt}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control trWt" value="${data.trWt}" readonly style="width: 60px;background-color:#dbeafe;" /></td>`,
            `<td><input type="number" class="form-control NET_WT" value="${data.netWt}" readonly  style="width: 60px;background-color:#dbeafe;"/></td>`,
            `<td><input type="number" class="form-control HD" value="${data.hdpe}" style="width: 60px;background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control DNR" value="${data.pvcPpm}"  style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control PC_LOWMELT" value="${data.pcLowMelt}" style="background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control CPRDN" value="${data.wrapper}" style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control TIME1_WIDTH" value="${data.metal}" style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control TIME2_WIDTH" value="${data.stone}" style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control TIME3_WIDTH" value="${data.rubber}" style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control GLUE_CONTENT" value="${data.glue}" style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control OTHERS" value="${data.other}" style="width: 60px;background-color:#d4edda;"/></td>`,
            `<td><input type="number" class="form-control TIME4_WIDTH" value="${data.total}"   readonly style="width: 80px;background-color:#d4edda;"    /></td>`,
            `<td><input type="text" class="form-control GRADE" value="${data.grd}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control YELLOWP" value="${data.yellow}" style="background-color:#d4edda;"   /></td>`,
            `<td><input type="number" class="form-control BLUEP" value="${data.blue}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control OTHERP" value="${data.otherPercent}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="text" class="form-control COLOR_NAME" value="${data.colorMix}" style="background-color:#d4edda;"  /></td>`,
            `<td><input type="number" class="form-control YELLOW160C" value="${data.yellow160c}" style="background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control MOISTURE" value="${data.moisture}"  style="background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control BULKDENSITY" value="${data.bulkDensity}"  style="background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control PH_FLAKES" value="${data.ph}" style="background-color:#d4edda;" /></td>`,
            `<td><input type="number" class="form-control OVERSIZED" value="${data.overSized}" style="background-color:#d4edda;" /></td>`,
            `<td><input type="text" class="form-control Remarks" value="${data.remarks}" style="background-color:#d4edda;" /></td>`,
            `<td style="display:none;"><input type="text" class="form-control Pord_Type" value="${data.refType}" readonly  style="background-color:#d4edda;"/></td>`,
            `<td style="display:none;"><input type="text" class="form-control REF_NO" value="${data.refNo}" readonly style="background-color:#d4edda;" /></td>`,
            `<td style="display:none;"><input type="text" class="form-control DEPT_CODE" value="${data.placeCode}" readonly style="background-color:#d4edda;" /></td>`
        ];

        const row = `<tr class="no-border-input">${cells.join('')}</tr>`;
        $tbody.append(row);

        if (data.itemName) {
            $tbody.find('tr:last .item-name-select').val(data.itemName);
        }
    });
}
function addSelectedRowsToTable(selectedRowsData) {
    console.log('Selected row data ', selectedRowsData);
    selectedRowsData.forEach(data => {
        addRow({
            iCode: data.itemCode || '',
            itemName: data.itemCode || '',
            dept: data.DEPT_NAME || '',
            batchNo: data.lotNo || '',
            bagNo: data.bagNo || '',
            wbWt: data.wbQty || '',
            grWt: data.grossQty || '',
            trWt: data.tareQty || '',
            netWt: data.qty || '',
            hdpe: '',
            pvcPpm: '',
            pcLowMelt: '',
            wrapper: '',
            metal: '',
            stone: '',
            rubber: '',
            glue: '',
            other: '',
            total: '',
            grd: '',
            yellow: '',
            blue: '',
            otherPercent: '',
            colorMix: '',
            yellow160c: '',
            moisture: '',
            bulkDensity: '',
            ph: '',
            overSized: '',
            remarks: '',
            refType: data.vType || '',
            refNo: data.vNo || '',
            deptCode: data.deptCode || ''
        });
    });
}
function setFormReadOnly() {
    const form = $('#FlakesQCEntryForm');
    form.find(':input').prop('disabled', true);
    form.find('textarea').css('background-color', '#f0f0f0');
    form.find('table tbody tr').css('background-color', '#f9f9f9');

    $('#tblFlakesQCEntry tbody tr').each(function () {
        $(this).find(':input').prop('disabled', true);
        $(this).css('background-color', '#f9f9f9');
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