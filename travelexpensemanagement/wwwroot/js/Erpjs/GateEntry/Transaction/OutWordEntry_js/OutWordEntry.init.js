

    let itemMap = { };
    let UnitMap = { };
    let DeptMap = { };
    let PubUserLevel='@PubUserLevel';
    let CompCode='@CompCode';
    let LoginDate = '@logindate';
    let pendingData = [];
    let currentPage = 1;
    let rowsPerPage = 10;
    const urlParams = new URLSearchParams(window.location.search);
    const rowId = urlParams.get('id');
    const vtype = urlParams.get('VType');
    const $tbody = $("#tblOutwardEntry tbody");
    const mode = urlParams.get('mode');
    $(document).ready(function () {

        (async () => {
            try {
                await LoadDropDowns();
                addRow($tbody);

                if (rowId) {
                    $('#ddlDocType').prop('disabled', true);
                    $('#DtDocDate').prop('disabled', true);
                    $('#DtTxtDocDate').prop('disabled', true);
                    await LoadFormByID(rowId, vtype);
                    if (mode == 'view') {
                        setFormReadOnly();
                    }
                }
                else {
                    const selectedVType = $("#ddlDocType").val();
                    if (selectedVType) {
                        await GetVNo(selectedVType, "GATE1");
                    }

                    if (PubUserLevel == 1) {
                        $('#DtDocDate').prop('disabled', false);
                        $('#DtTxtDocDate').prop('disabled', false);
                    }
                    else {
                        $('#DtDocDate').prop('disabled', true);
                        $('#DtTxtDocDate').prop('disabled', true);
                    }

                    let today = new Date().toISOString().split('T')[0];
                    $('#DtTxtDocDate').attr('min', LoginDate);
                    $('#DtDocDate').val(today);
                    let now = new Date();
                    $('#DtTxtDocDate').val(now.toTimeString().slice(0, 8));
                }

                $("#btn-save").click(function (e) {
                    e.preventDefault();

                    const V_DATE = formatDate($("#DtDocDate").val());
                    const V_NO = parseInt($('#NumDocNo').val()) || null;
                    const RETURN_DATE = formatDate($("#DtExpectedDateReturn").val());
                    const RESPONSIBLE_PERSON = $.trim($('#txtResponsiblePerson').val());
                    const DocType = $.trim($('#ddlDocType').val());
                    const ITEM_TYPE = $.trim($('#ddlType option:selected').text());
                    const PartyCode = parseInt($('#ddlPartyName').val()) || 0;

                    if (!DocType) {
                        toastr.warning("Please select a Doc Type.");
                        $("#ddlDocType").focus();
                        return;
                    }

                    if (!V_DATE) {
                        toastr.warning("Please select a Voucher Date.");
                        $("#DtDocDate").focus();
                        return;
                    }

                    if (DocType === "OURT") {

                        if (!RETURN_DATE) {
                            toastr.warning("Please select Return Date.");
                            $("#DtExpectedDateReturn").focus();
                            return;
                        }

                        if (new Date(RETURN_DATE) < new Date(V_DATE)) {
                            showToast("Invalid Return Date. Return date should not be less than Doc date.", { type: "info" });
                            $('#DtExpectedDateReturn').addClass('is-invalid').focus();
                            return;
                        }

                        if (!RESPONSIBLE_PERSON) {
                            toastr.warning("Please enter Responsible Person Name.");
                            $("#txtResponsiblePerson").focus();
                            return;
                        }
                    }

                    if (!V_NO) {
                        toastr.warning("Invalid Voucher No.");
                        $("#NumDocNo").focus();
                        return;
                    }

                    if (!PartyCode) {
                        toastr.warning("Please select a Party.");
                        $("#ddlPartyName").focus();
                        return;
                    }

                    if (CompCode == 2) {
                        if (DocType === "DocType" || ITEM_TYPE === "Sale") {
                            toastr.warning(`Please check DocType = ${DocType} and Type = ${ITEM_TYPE}`);
                            $("#ddlDocType").focus();
                            return;
                        }
                        else {
                            // Fixed Logic
                            if (DocType === "OUES" && ITEM_TYPE !== "E-Commerce Sale") {
                                toastr.warning(`Sale Type and Doctype mismatch.`);
                                $("#ddlDocType").focus();
                                return;
                            }

                        }
                    }

                    const rows = $("#tblOutwardEntry tbody tr");
                    let isValid = true;
                    let hasAtLeastOneItem = false;

                    rows.each(function (index) {
                        const $row = $(this);
                        const itemName = $row.find("select.itemName").val();
                        const dept = $row.find("select.department").val();
                        const unit = $row.find("select.unit").val();
                        const qty = $row.find("input.quantity").val();
                        const nos = $row.find("input.no").val();
                        const refType = $.trim($row.find("input.ref-type").val());
                        const refNo = $.trim($row.find("input.ref-no").val());

                        if (itemName) {

                            hasAtLeastOneItem = true;

                            if (!dept) {
                                toastr.warning(`Please select Department in row ${index + 1}`);
                                $row.find("select.department").focus();
                                isValid = false;
                                return false;
                            }

                            if (!unit) {
                                toastr.warning(`Please select Unit in row ${index + 1}`);
                                $row.find("select.unit").focus();
                                isValid = false;
                                return false;
                            }

                            if (!qty || parseFloat(qty) <= 0) {
                                toastr.warning(`Please enter valid Quantity in row ${index + 1}`);
                                $row.find("input.quantity").focus();
                                isValid = false;
                                return false;
                            }

                            if (!nos || parseInt(nos) <= 0) {
                                toastr.warning(`Please enter valid Nos in row ${index + 1}`);
                                $row.find("input.no").focus();
                                isValid = false;
                                return false;
                            }

                            if (ITEM_TYPE != "Others") {

                                if (!refType) {
                                    toastr.warning(`Please enter Ref Type in row ${index + 1}`);
                                    $row.find("input.ref-type").focus();
                                    isValid = false;
                                    return false;
                                }

                                if (!refNo) {
                                    toastr.warning(`Please enter Ref No in row ${index + 1}`);
                                    $row.find("input.ref-no").focus();
                                    isValid = false;
                                    return false;
                                }

                            }
                        }
                    });

                    if (!hasAtLeastOneItem) {
                        toastr.warning("Please add at least one item.");
                        return;
                    }

                    if (!isValid) {
                        return;
                    }

                    const header = {
                        RETURN_DATE: RETURN_DATE,
                        RESPONSIBLE_PERSON: RESPONSIBLE_PERSON,
                        DOC_ID: $.trim($('#TxtCode').val()) || null,
                        V_TYPE: $('#ddlDocType').val() || null,
                        V_NO: parseInt($('#NumDocNo').val()) || null,
                        V_DATE: (() => { const d = new Date($("#DtDocDate").val()); return !isNaN(d) ? d.toISOString() : null; })(),
                        V_TIME: $.trim($('#DtTxtDocDate').val()) || null,
                        PARTY_CODE: parseInt($('#ddlPartyName').val()) || null,
                        PARTY_NAME: $('#ddlPartyName option:selected').text() || null,
                        TRUCK_NO: $.trim($('#TxtVehicleNo').val()) || null,
                        WAYBILL_NO: $.trim($('#TxtWayBillNo').val()) || null,
                        REMARKS: $.trim($('#TxtRemarks').val()) || null,
                        Add1: $.trim($('#TxtAdd1PD').val()) || null,
                        Add2: $.trim($('#TxtAdd2PD').val()) || null,
                        Add3: $.trim($('#TxtAdd3PD').val()) || null,
                        PARTY_CITY: $('#ddlCity').val() || null,
                        PARTY_GST: $.trim($('#TxtGSTNo').val()) || null,
                        PARTY_PINCODE: $.trim($('#NumPincode').val()) || null,
                        ITEM_TYPE: $.trim($('#ddlType').val()) || null,
                        PARTY_ADDRESSID: parseInt($('#ddlPartyNameByAddress').val()) || null,
                        action: $.trim($('#TxtCode').val()) ? 'UPDATE' : 'INSERT'
                    };

                    const payload = {
                        Header: header,
                        detailsOutwardEntry: collectTableRowData().filter(x => x.ITEM_CODE)
                    };

                    $("#btn-save").prop("disabled", true);

                    $.ajax({
                        url: '/OutwardEntry/SavedData',
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify(payload),

                        success: function (response) {

                            if (response.success) {

                                toastr.success("Saved successfully!");

                                setTimeout(() => {
                                    window.location.href = '/OutwardEntryList/Index';
                                }, 1000);

                            } else {

                                toastr.error(response.message || "Save failed.");
                            }
                        },

                        error: function (xhr) {

                            let errorMessage = "Something went wrong.";

                            if (xhr.status === 400) {
                                errorMessage = "Bad Request: " + xhr.responseText;
                            }
                            else if (xhr.status === 500) {
                                errorMessage = "Server error: " + xhr.responseText;
                            }
                            else {
                                errorMessage = "Unexpected error: " + xhr.statusText;
                            }

                            toastr.error(errorMessage);
                        },

                        complete: function () {

                            $("#btn-save").prop("disabled", false);
                        }
                    });
                });

                $(document).on("click", ".btn-add-action", () => addRow($tbody));

                $(document).on("click", ".btn-delete-action", function () {
                    const $row = $(this).closest("tr");
                    const wasLast = $row.is(":last-child");
                    $row.remove();
                    if (wasLast) {
                        const $last = $tbody.find("tr:last");
                        if ($last.length && !$last.find(".btn-add-action").length) {
                            $last.find("td:last").prepend(
                                `<i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>`
                            );
                        }
                    }
                });

                $("#ddlDocType").change(function () {
                    if (!rowId) GetVNo(this.value, "GATE1");
                    $('#ddlDocType').prop('disabled', true);
                    if (this.value === "OURT") {
                        document.getElementById("Conditionnaldesignid").style.display = "flex";
                    } else {
                        document.getElementById("Conditionnaldesignid").style.display = "none";
                    }

                });

                $("#ddlPartyName").on("change", function () {
                    const partyId = $(this).val();
                    $('#ddlDocType').prop('disabled', true);
                    loadPartyAddresses(partyId);
                    fetchPartyDetails(partyId);
                });

                $("#ddlPartyNameByAddress").on("change", function () {
                    const partyId = $("#ddlPartyName").val();
                    const addId = $(this).val();
                    fetchPartyAddressDetails(partyId, addId);
                });

                $("#btnpendingorderno").click(function () {
                    const selectedValue = $('#ddlPartyName').val();
                    const BILL_NO = $('#TxtWayBillNo').val();
                    const v_date = $('#DtDocDate').val();
                    const typeText = $('#ddlType option:selected').text();

                    FetchPendindorderno(selectedValue, typeText, v_date, BILL_NO);
                });

                $("#Btn_selectedData").click(function () {
                    const selectedRows = getSelectedPendingRows();
                    if (selectedRows.length === 0) {
                        toastr.info("Please select at least one row");
                        return;
                    }
                    const $tbody = $("#tblOutwardEntry tbody");
                    selectedRows.forEach(row => {
                        addRow($tbody, {
                            itemName: row.ItemCode,
                            department: row.DeptCode || "",
                            unit: row.UnitCode,
                            quantity: parseFloat(row.Qty) || "",
                            no: parseInt(row.nos) || "",
                            remarks: row.remarks || "",
                            refType: row.Vouchertype || "",
                            refNo: row.VoucherNo || ""
                        });
                    });
                });

                $(document).on("change", ".row-checkbox", function () {
                    const index = $(this).data("index");
                    pendingData[index].selected = $(this).is(":checked");
                });

                $("#selectAllPR").on("change", function () {
                    const isChecked = $(this).is(":checked");
                    pendingData.forEach(row => row.selected = isChecked);
                    renderPendingTable();
                });

            } catch (err) {
                console.error("Error initializing page:", err);
            }
        })();
    });

