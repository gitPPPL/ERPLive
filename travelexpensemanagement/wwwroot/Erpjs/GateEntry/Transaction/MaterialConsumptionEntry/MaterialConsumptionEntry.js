const MaterialEvents = (function () {
    function init(rowId, vtype, $tbody, mode, isReadOnly) {

        (async () => {
            try {

                setCurrentDateTime();

                await MaterialAPI.LoadDropDowns();

                addRow($tbody);

                if (rowId) {
                    await MaterialAPI.LoadFormByID(rowId);
                    if (mode == 'view') {
                        console.log('Mode', mode);
                        setFormReadOnly();
                    }
                }
                 
                if (rowId && rowId !== 0) {
                    await MaterialAPI.LoadFormByID(rowId, vtype);
                    if (typeof isReadOnly !== 'undefined' && isReadOnly) {
                        setFormReadOnly();
                    }
                }
                
                // ================= EVENTS =================

                $("#ddlPartyName").on("change", async function () {
                    const partyId = $(this).val();

                    if (!partyId) {
                        $("#TxtAdd1PD, #TxtAdd2PD, #TxtAdd3PD").val("").prop("disabled", true);
                        return;
                    }

                    await MaterialAPI.fetchPartyAddressDetails(partyId);
                });

                $("#btn-pending").on("click", async function () {

                    const partyId = $("#ddlPartyName").val();

                    if (!partyId) {
                        toastr.warning("Please select a Party.");
                        return;
                    }

                    await MaterialAPI.loadPendingDocuments(partyId);
                });

                $(document).on("click", ".btn-add-action", () => addRow($tbody));
                $(document).on("click", ".btn-add-row", () => addRow($tbody));

                $(document).on("click", ".btn-delete-action", function () {
                    const $row = $(this).closest("tr");
                    const wasLast = $row.is(":last-child");

                    $row.remove();

                    if (wasLast) {
                        const $last = $tbody.find("tr:last");

                        if ($last.length && !$last.find(".btn-add-action").length) {
                            $last.find("td:last").prepend(`
                                <i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>
                            `);
                        }
                    }
                });

                // ================= SAVE =================

                $("#btn-save").click(async function (e) {

                    e.preventDefault();

                    const V_DATE = formatDate($("#DtDocDate").val());
                    const DocType = $.trim($('#ddlDocType').val());
                    const PartyCode = parseInt($('#ddlPartyName').val()) || 0;

                    if (!DocType) {
                        toastr.warning("Please select a Doc Type.");
                        $("#ddlDocType").focus();
                        return;
                    }

                    if (!V_DATE) {
                        toastr.warning("Please select a Voucher Date.");
                        $("#ddlDocType").focus();
                        return;
                    }

                    if (!PartyCode) {
                        toastr.warning("Please select a Party.");
                        $("#ddlPartyName").focus();
                        return;
                    }

                    const header = {
                        DOC_ID: $.trim($('#TxtCode').val()) ?? null,
                        V_TYPE: $('#ddlDocType').val() ?? null,
                        V_NO: parseInt($('#NumDocNo').val()) || null,
                        V_DATE: (() => {
                            const d = new Date($("#DtDocDate").val());
                            return !isNaN(d) ? d.toISOString() : null;
                        })(),
                        V_TIME: (() => {
                            const val = $('#TmDocTime').val();
                            if (!val) return null;
                            return val.length === 5 ? val + ':00' : val;
                        })(),
                        PARTY_CODE: parseInt($('#ddlPartyName').val()) || null,
                        TRUCK_NO: $.trim($('#TxtVehicleNo').val()) ?? null,
                        REMARKS: $.trim($('#TxtRemarks').val()) ?? null,
                        Add1: $.trim($('#TxtAdd1PD').val()) ?? null,
                        Add2: $.trim($('#TxtAdd2PD').val()) ?? null,
                        Add3: $.trim($('#TxtAdd3PD').val()) ?? null,
                        ITEM_TYPE: $.trim($('#ddlType').val()) ?? null,
                        action: $.trim($('#TxtCode').val()) ? 'UPDATE' : 'INSERT'
                    };

                    const payload = {
                        Header: header,
                        Deatils: collectTableRowData()
                    };

                    console.log('Data', payload);

                    $("#btn-save").prop("disabled", true);

                    try {
                        const response = await MaterialAPI.saveData(payload);

                        if (response.success) {
                            toastr.success("Saved successfully!");
                            setTimeout(() => window.location.href = '/MiscConsumptionEntryList/Index', 1000);
                        } else {
                            toastr.error(response.message || "Save failed.");
                        }

                    } catch (xhr) {

                        let errorMessage = "Something went wrong.";

                        if (xhr.status === 400) {
                            errorMessage = "Bad Request: " + xhr.responseText;
                        } else if (xhr.status === 500) {
                            errorMessage = "Server error: " + xhr.responseText;
                        } else {
                            errorMessage = "Unexpected error: " + xhr.statusText;
                        }

                        toastr.error(errorMessage);

                    } finally {
                        $("#btn-save").prop("disabled", false);
                    }
                });

                // ================= PENDING =================
                $('#PendngAddRow').on('click', function (e) {

                    e.preventDefault();

                    let selectedRows = getSelectedPendingDocuments();

                    if (selectedRows.length === 0) {
                        toastr.warning("Please select at least one row.");
                        return;
                    }

                    addSelectedToConsumptionTable(selectedRows);

                    console.log('Selected row data:', selectedRows);
                });

                $('#tblPendingDocument thead input[type="checkbox"]').on('change', function () {
                    const checked = $(this).is(':checked');
                    $('#tblPendingDocument tbody input.select-row').prop('checked', checked);
                });

            } catch (err) {
                console.error("Error initializing page:", err);
            }

        })();
    }

    return { init };

})();