
function getSelectedPendingRows() {
    return pendingData.filter(row => row.selected);
}

function setFormReadOnly() {
    $('#OutwardEntryForm input:not([type="hidden"])').prop('readonly', true);
    $('#OutwardEntryForm input[type="checkbox"], input[type="radio"], input[type="file"]').prop('disabled', true);
    $('#OutwardEntryForm input[type="time"], #OutwardEntryForm input[type="date"]').prop('disabled', true);
    $('#OutwardEntryForm select').prop('disabled', true);
    $('#OutwardEntryForm textarea').prop('readonly', true);
    $('#OutwardEntryForm button').prop('disabled', true);
    $('#OutwardEntryForm a').css({
        'pointer-events': 'none',
        'opacity': '0.5'
    });
    $('#tblOutwardEntry tbody').find('input, select, textarea, button').prop('disabled', true);
    $('#btnpendingorderno, #btn-pending').prop('disabled', true);
    $('#pendingorders').find('input, select, button').prop('disabled', true);
    $('.btn-add-action, .btn-delete-action').css({
        'pointer-events': 'none',
        'opacity': '0.5'
    });

    $('#tablePagination').css({
        'pointer-events': 'none',
        'opacity': '0.5'
    });
    $('#OutwardEntryForm')
        .find('input, select, textarea, button, a')
        .attr('tabindex', '-1');
    $('#OutwardEntryForm').css({
        'opacity': '0.95'
    });
}

async function LoadDropDowns() {
    await Promise.all([
        loadItemMaster(),
        loadDeptMaster(),
        loadUnit(),
        DDLVtype(),
        DDLParty(),
        DDLcity_mast()
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

    $tbody.find(".btn-add-action").remove();

    const selectItems = generateSelect(itemMap, data.itemName || "");
    const selectDept = generateSelect(DeptMap, data.department || "");
    const selectunit = generateSelect(UnitMap, data.unit || "");

    const row = `
    <tr class="no-border-input">
      <td style="display:none;">${data.code || ""}</td>

      <!-- Disabled Selects -->
      <td>
        <select class="form-control itemName" disabled>
          <option value="">-- Select --</option>${selectItems}
        </select>
      </td>

      <td>
        <select class="form-control department" disabled>
          <option value="">-- Select --</option>${selectDept}
        </select>
      </td>

      <td>
        <select class="form-control unit" disabled>
          <option value="">-- Select --</option>${selectunit}
        </select>
      </td>

      <!-- Enabled -->
      <td>
        <input type="number" class="form-control no" value="${data.no || ''}"/>
      </td>

      <td>
        <input type="number" class="form-control quantity" value="${data.quantity || ''}"/>
      </td>

      <td>
        <input type="text" class="form-control remarks" value="${data.remarks || ''}"/>
      </td>

      <!-- Readonly -->
      <td>
        <input type="text" class="form-control ref-type"
               value="${data.refType || ''}" readonly/>
      </td>

      <td>
        <input type="text" class="form-control ref-no"
               value="${data.refNo || ''}" readonly/>
      </td>

      <td>
        <i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>
        <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
      </td>
    </tr>`;

    if ($emptyRow.length) {
        $emptyRow.before(row);
    }
    else {
        $tbody.append(row);
    }
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
    const row = `
            <tr>
              <td>
                <input type="checkbox" class="row-checkbox"
                       data-index="${index}"
                       ${data.selected ? "checked" : ""}/>
              </td>
              <td><input type="text" class="form-control Vouchertype" value="${data.Vouchertype || ''}"/></td>
              <td><input type="text" class="form-control VoucherNo" value="${data.VoucherNo || ''}"/></td>
              <td><input type="text" class="form-control VoucherDate" value="${data.VoucherDate || ''}"/></td>
              <td><input type="text" class="form-control ItemCode" value="${data.ItemCode || ''}"/></td>
              <td><input type="text" class="form-control ItemName" value="${data.ItemName || ''}"/></td>
              <td><input type="text" class="form-control Qty" value="${data.Qty || ''}"/></td>
              <td><input type="number" class="form-control PQty" value="${data.PQty || ''}"/></td>
              <td><input type="text" class="form-control remarks" value="${data.remarks || ''}"/></td>
              <td><input type="text" class="form-control nos" value="${data.nos || ''}"/></td>
              <td><input type="text" class="form-control UnitName" value="${data.UnitName || ''}"/></td>
              <td><input type="text" class="form-control UnitCode" value="${data.UnitCode || ''}"/></td>
              <td><input type="text" class="form-control SRno" value="${data.SRno || ''}"/></td>
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
