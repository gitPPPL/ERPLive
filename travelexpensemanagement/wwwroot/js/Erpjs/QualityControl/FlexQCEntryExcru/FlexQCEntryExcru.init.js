const $tbody = $('#tblFlakesQCEntry tbody');
const refTypeOptions = ["PO", "Indent", "Manual"];
const urlParams = new URLSearchParams(window.location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
const isReadOnly = urlParams.get('readOnly') === 'true';
let itemNameOptions = '';
let DDLGridStatuslist = '';

var globalVars = window.globalVariables || {};
let LoginDate = globalVars.LoginDate;
var database = window.database || "";

$(document).ready(function () {

    SetFYDate('DtDocDate', LoginDate);

    const today = new Date().toISOString().split('T')[0]; 
    $("#DtFrom").val(today);
    $("#DtTo").val(today);

    LoadDropDown().then(() => {
        if (rowId) {
            LoadFormByID(rowId);
            document.getElementById("DtDocDate").disabled = true;
            document.getElementById("ddlShift").disabled = true;
            document.getElementById("ddlProdPlace").disabled = true;

            if (mode === "view") {
                setFormReadOnly();
                $('#FlakesQCEntryForm').after('<span class="badge bg-secondary ms-2">Read‑Only Mode</span>');
            }
        }
        else {
            GetVNo();     
            document.getElementById('DtTime').value = ((d => `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`)(new Date()));
        }
    });

    $('#btn_Copy').on('click', function () {
        const docDate = $('#DtDocDate').val();
        const DeptCode = $('#ddlProdPlace').val();
        const ShiftType = $('#ddlShift').val();
        if (!validateRequiredField('#DtDocDate', 'Doc Date')) return;
        if (!validateRequiredField('#ddlShift', 'SHIFT')) return;
        if (!validateRequiredField('#ddlProdPlace', 'Prod Place')) return;
        fetchData(DeptCode, ShiftType, formatDate(docDate));
    });

    $('#CopyData').on('click', function () {
        const selectedRows = getSelectedRowsData();
        populateTableRowsFromData(selectedRows);

        if (selectedRows.length > 0) {
            var modalEl = document.getElementById('CopyFromModal');
            var modalInstance = bootstrap.Modal.getInstance(modalEl);
            if (modalInstance) {
                modalInstance.hide();
            }
  
            document.getElementById("DtDocDate").disabled = true;
            document.getElementById("ddlShift").disabled = true;
            document.getElementById("ddlProdPlace").disabled = true;
        } else {
            toastr.warning('No rows selected');
        }
    });

    $('#selectAllPR').on('change', function () {
        const isChecked = $(this).prop('checked');
        $('#tblCopyFrommodal tbody input[type="checkbox"]').each(function () {
            $(this).prop('checked', isChecked);
        });
    });

    $('#tblCopyFrommodal').on('change', 'tbody input[type="checkbox"]', function () {
        const totalCheckboxes = $('#tblCopyFrommodal tbody input[type="checkbox"]').length;
        const checkedCheckboxes = $('#tblCopyFrommodal tbody input[type="checkbox"]:checked').length;
        $('#selectAllPR').prop('checked', totalCheckboxes === checkedCheckboxes);
    });

    $tbody.on("change", ".item-name-select", function () {
        const $select = $(this);
        const selectedValue = $select.val();
        const $row = $select.closest("tr");
        const $iCodeInput = $row.find("td:nth-child(2) input.form-control");
        $iCodeInput.val(selectedValue);
    });

    $("#btn-saves").click(function (e) {
        e.preventDefault();
        const DOC_ID = $.trim($('#TxtCode').val());
        const V_NO = parseFloat($.trim($('#NumDocNo').val())) || 0;
        const V_DATE = formatDate($("#DtDocDate").val());
        const SHIFT = $.trim($('#ddlShift option:selected').text()) || "";
        const QCTIME = $.trim($('#DtTime').val()) || null;
        const QC_INCHARGE = parseInt($('#ddlQCIncharge').val()) || 0;
        const QC_INCHARGENAME = $.trim($('#ddlQCIncharge option:selected').text()) || "";
        const CHEMIST = parseInt($('#ddlChemist').val()) || 0;
        const CHEMISTNAME = $.trim($('#ddlChemist option:selected').text()) || "";
        const EMP_CODE = parseInt($('#ddlInspBy').val()) || 0;
        const PLACE_CODE = parseInt($('#ddlProdPlace').val()) || 0;
        const REMARKS = $.trim($('#TxtRemarks').val());
        const action = (!DOC_ID || DOC_ID.trim() === '') ? 'INSERT' : 'UPDATE';
        const Header = {
            DOC_ID,
            V_NO,
            V_DATE,
            SHIFT,
            QCTIME,
            QC_INCHARGE,
            QC_INCHARGENAME,
            CHEMIST,
            CHEMISTNAME,
            EMP_CODE,
            PLACE_CODE,
            REMARKS,
            action
        };

        const Deatils = collectInsertRows();
        const payload = {
            Header,
            Deatils
        };

        if (!validateRequiredField('#NumDocNo', 'Doc_No')) return;
        if (!validateRequiredField('#DtDocDate', 'V_Date')) return;
        if (!validateRequiredField('#ddlShift', 'SHIFT')) return;
        if (!validateRequiredField('#DtTime', 'Time')) return;
        if (!validateRequiredField('#ddlQCIncharge', 'QC Incharge')) return;
        if (!validateRequiredField('#ddlChemist', 'Chemist')) return;
        if (!validateRequiredField('#ddlProdPlace', 'ProdPlace')) return;

        const table = document.getElementById("tblFlakesQCEntry");
        const tbody = table.querySelector("tbody");

        if (tbody.rows.length === 0) {
            toastr.warning("Fill data in Details");
            return;
        }

        $("#btn-saves").prop("disabled", true);

        $.ajax({
            url: '/FlexQCEntryExcru/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    toastr.success("Saved successfully!");

                    setTimeout(function () { window.location.href = '/FlexQCEntryExcru/Index?id=' + V_NO  + '&mode=view'; }, 3000);     

                  //  setTimeout(() => window.location.href = '/FlexQCEntryExcruList/Index', 1000);
                } else {
                    toastr.error(response.message || "Save failed.");
                }
            },
            error: function (xhr) {
                let errorMessage = "Something went wrong.";
                if (xhr.status === 400) {
                    errorMessage = "Bad Request: " + xhr.responseText;
                } else if (xhr.status === 500) {
                    errorMessage = "Server error: " + xhr.responseText;
                } else {
                    errorMessage = "Unexpected error: " + xhr.statusText;
                }
                toastr.error("Error: " + errorMessage);
            },
            complete: function () {
                $("#btn-saves").prop("disabled", false);
            }
        });
    });

    $('#chkFullName').on('change', function () {
        var check = $(this).is(':checked');
        if (check == true) {
            DDLItem(check);
        }
        else {
            DDLItem();
        }

    });

    $('#btn_summary').on('click', function () {
        summaryReport("Summary");
    });

    $('#btn_detail').on('click', function () {
        summaryReport();
    });

    $(document).on('click', '.btn-row-action', function () {

        const $currentRow = $(this).closest('tr');
        const $previousRow = $currentRow.prev('tr');

        if ($previousRow.length === 0)
        {
            console("No previous row found.");
            return;
        }

        const previousStatus = $previousRow.find('.status').val();
        const previousRemarks = $previousRow.find('.Remarks').val();
        const currentStatus = $currentRow.find('.status').val();
        const currentRemarks = $currentRow.find('.Remarks').val();

        if ( (!currentStatus || currentStatus.trim() === '') && (!currentRemarks || currentRemarks.trim() === ''))
        {
            $currentRow.find('.status').val(previousStatus);
            $currentRow.find('.Remarks').val(previousRemarks);
        }
    });

});