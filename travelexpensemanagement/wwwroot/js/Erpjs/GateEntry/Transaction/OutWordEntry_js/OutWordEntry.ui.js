
function getSelectedPendingRows() {
    return pendingData.filter(row => row.selected);
}

function setFormReadOnly() {
    const form = $('#OutwardEntryForm');

    // -------------------------
    // 1. Inputs (text, number, email etc.)
    // -------------------------
    form.find('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]):not([type="file"])')
        .prop('readonly', true);

    // -------------------------
    // 2. Disable checkboxes, radios, file, date, time
    // -------------------------
    form.find('input[type="checkbox"], input[type="radio"], input[type="file"], input[type="date"], input[type="time"]')
        .prop('disabled', true);

    // -------------------------
    // 3. Disable selects
    // -------------------------
    form.find('select').prop('disabled', true);

    // -------------------------
    // 4. Textareas
    // -------------------------
    form.find('textarea').prop('readonly', true);

    // -------------------------
    // 5. Buttons
    // -------------------------
    form.find('button').prop('disabled', true);
    $('#btn-save, #btnpendingorderno, #btn-pending').prop('disabled', true);

    // -------------------------
    // 6. Links
    // -------------------------
    form.find('a').css({ 'pointer-events': 'none', 'opacity': '0.5' });

    // -------------------------
    // 7. TABLE FIX (ALL ROWS AND CELLS)
    // -------------------------
    $('#tblOutwardEntry').addClass('table-readonly')
        .find('input, select, textarea, button')
        .each(function () {
            const $el = $(this);
            if ($el.is('select') || $el.is('button') ||
                $el.is(':checkbox') || $el.is(':radio') || $el.is('[type=file]') ||
                $el.is('[type=date]') || $el.is('[type=time]')) {
                $el.prop('disabled', true);
            } else {
                $el.prop('readonly', true); // text, number, textarea
            }
        });

    // Optional: disable pointer events on table
    $('#tblOutwardEntry.table-readonly').css('pointer-events', 'none');

    // -------------------------
    // 8. Modal Inputs
    // -------------------------
    $('#pendingorders').find('input, select, button').prop('disabled', true);

    // -------------------------
    // 9. Add/Delete buttons
    // -------------------------
    $('.btn-add-action, .btn-delete-action').css({ 'pointer-events': 'none', 'opacity': '0.5' });

    // -------------------------
    // 10. Form opacity and tab navigation
    // -------------------------
    form.css({ 'opacity': '0.95' });
    form.find('input, select, textarea, button, a').attr('tabindex', '-1');
}



async function LoadDropDowns() {
    await Promise.all([
        loadItemMaster(),
        loadDeptMaster(),
        loadUnit(),
        DDLVtype(),
        DDLParty(),
        DDLcity_mast(),
        DDLstate()
    ]);
}

function clearFields() {
    $("#TxtAdd1PD").val("");
    $("#TxtAdd2PD").val("");
    $("#TxtAdd3PD").val("");
    $("#ddlCity").val("");
    $("#NumPincode").val("");
    $("#TxtState").val("");
    $("#TxtGSTNo").val("");
}

function formatDate(dateStr) {
    if (!dateStr) return null;

    const date = new Date(dateStr);

    if (isNaN(date.getTime())) return null;

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
}

function generateSelect(optionsMap, selected = "") {
    return Object.entries(optionsMap)
        .map(([val, txt]) => `<option value="${val}"${val == selected ? " selected" : ""}>${txt}</option>`)
        .join("");
}

function collectTableRowData() {
    return Array.from(document.querySelectorAll('#tblOutwardEntry tbody tr')).map(row => {
        return {
            ITEM_CODE: parseInt(row.querySelector('select.itemName')?.value) || null,
            ITEM_NAME: row.querySelector('select.itemName option:checked')?.text || '',
            DEPT_CODE: parseInt(row.querySelector('select.department')?.value) || null,
            NOS: parseInt(row.querySelector('input.no')?.value) || null,
            QTY: parseFloat(row.querySelector('input.quantity')?.value) || null,
            UOM_CODE: parseInt(row.querySelector('select.unit')?.value) || null,
            UOM_NAME: row.querySelector('select.unit option:checked')?.text || '',
            REMARKS: row.querySelector('input.remarks')?.value || '',
            REF_TYPE: row.querySelector('input.ref-type')?.value || '',
            REF_NO: parseInt(row.querySelector('input.ref-no')?.value) || null
        };
    });
}

function addRow($tbody, data = {}) {

    const $emptyRow = $tbody.find("tr").filter(function () {
        return !$(this).find("select.itemName").val();
    }).first();

    $tbody.find(".act-btn.edit").hide();

    const selectItems = generateSelect(itemMap, data.itemName || "");
    const selectDept = generateSelect(DeptMap, data.department || "");
    const selectunit = generateSelect(UnitMap, data.unit || "");


    console.log("Item", itemMap);


     const row = $(`
    <tr class="no-border-input">
      <td style="display:none;">${data.code || ""}</td>

      <!-- Disabled Selects -->
      <td>
        <select class="form-control itemName" disabled>
          <option value="">-- Select --</option>${selectItems}
        </select>
      </td>

      <td>
        <select class="form-control department" >
          <option value="">-- Select --</option>${selectDept}
        </select>
      </td>

      <td>
        <select class="form-control unit" disabled>
          <option value="">-- Select --</option>${selectunit}
        </select>
      </td>

      <!-- Enabled Inputs -->
      <td>
        <input type="number" class="form-control no" value="${data.no || ''}" oninput="if(this.value.length > 10) this.value = this.value.slice(0,12)" />
      </td>
      <td>
      <input type="number" class="form-control quantity" value="${data.quantity || ''}" oninput="if(this.value.length > 14) this.value = this.value.slice(0,14)" />
      </td>

      <td>
        <input type="text" class="form-control remarks" maxlength="225" value="${data.remarks || ''}"/>
      </td>

      <!-- Readonly -->
      <td>
        <input type="text" class="form-control ref-type" value="${data.refType || ''}" readonly/>
      </td>

      <td>
        <input type="text" class="form-control ref-no" value="${data.refNo || ''}" readonly/>
      </td>

      <td class="action-col">
        <button class="act-btn delete" title="Delete Row" style="cursor:pointer;">
          <i class="fa fa-trash btn-delete-action"></i>
        </button> 
      </td>
    </tr>
    `);

    // Append or insert row
    if ($emptyRow.length) {
        $emptyRow.before(row);
    } else {
        $tbody.append(row);
    }

    // Enforce max 18 digits on number inputs
    row.find("input[type=number]").on("input", function () {
        if (this.value.length > 18) {
            this.value = this.value.slice(0, 18);
        }
    });
}

function renderPendingTable() {
    const $tbody = $("#tblpendingordermodal tbody");
    $tbody.empty();
    const start = (currentPage - 1) * rowsPerPage;
    const end = start + rowsPerPage;
    const pageData = pendingData.slice(start, end);
    pageData.forEach((row, i) => {
        PendingaddRow($tbody, row, start + i);
    });

    updatePaginationInfo();
}

function PendingaddRow($tbody, data = {}, index) {

    let formattedDate = '';

    if (data.VoucherDate) {
        // remove time part
        let datePart = data.VoucherDate.split(' ')[0]; // "21-05-2026"

        let parts = datePart.split('-');

        if (parts.length === 3) {
            let day = parts[0].padStart(2, '0');
            let month = parts[1].padStart(2, '0');
            let year = parts[2];

            formattedDate = `${day}-${month}-${year}`;
        }
    }

    const row = `
        <tr>
          <td>
            <input type="checkbox" class="row-checkbox"
                   data-index="${index}"
                   ${data.selected ? "checked" : ""}
                   />
          </td>

          <td><input type="text" class="form-control Vouchertype" value="${data.Vouchertype || ''}" readonly/></td>

          <td><input type="text" class="form-control VoucherNo" value="${data.VoucherNo || ''}" readonly/></td>

          <td><input type="text" class="form-control VoucherDate" value="${formattedDate}" readonly/></td>

          <td><input type="text" class="form-control ItemCode" value="${data.ItemCode || ''}" readonly/></td>

          <td><input type="text" class="form-control ItemName" value="${data.ItemName || ''}" readonly/></td>

          <td><input type="text" class="form-control Qty" value="${data.Qty || ''}" readonly/></td>

          <td><input type="number" class="form-control PQty" value="${data.PQty || ''}" readonly/></td>

          <td><input type="text" class="form-control remarks" value="${data.remarks || ''}" readonly/></td>

          <td class="hidden-col"><input type="text" class="form-control nos" value="${data.nos || ''}" readonly/></td>

          <td class="hidden-col"><input type="text" class="form-control UnitName" value="${data.UnitName || ''}" readonly/></td>

          <td class="hidden-col"><input type="text" class="form-control UnitCode" value="${data.UnitCode || ''}" readonly/></td>

          <td class="hidden-col"><input type="text" class="form-control SRno" value="${data.SRno || ''}" readonly/></td>
        </tr>`;

    $tbody.append(row);
}

function nextPage() {
    if (currentPage * rowsPerPage < pendingData.length) {
        currentPage++;
        renderPendingTable();
    }
}

function prevPage() {
    if (currentPage > 1) {
        currentPage--;
        renderPendingTable();
    }
}

function changeRowsPerPage() {
    rowsPerPage = parseInt($("#pageSizeSelect").val());
    currentPage = 1;
    renderPendingTable();
}

function updatePaginationInfo() {
    const total = pendingData.length;

    const start = total === 0 ? 0 : (currentPage - 1) * rowsPerPage + 1;
    const end = Math.min(currentPage * rowsPerPage, total);

    $("#pageInfoText").text(`Results: ${start} - ${end} of ${total}`);

    $("#prevBtn").prop("disabled", currentPage === 1);
    $("#nextBtn").prop("disabled", end >= total);

    renderPageNumbers();
}

function renderPageNumbers() {
    const totalPages = Math.ceil(pendingData.length / rowsPerPage);
    const $container = $("#pageNumbers");

    $container.empty();

    for (let i = 1; i <= totalPages; i++) {
        const btn = $(`<button class="page-num">${i}</button>`);

        if (i === currentPage) {
            btn.addClass("active");
        }

        btn.click(() => {
            currentPage = i;
            renderPendingTable();
        });

        $container.append(btn);
    }
}

function getSelectedPendingRows() {
    return pendingData.filter(row => row.selected);
}      
