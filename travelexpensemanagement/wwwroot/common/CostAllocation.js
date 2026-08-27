window.CostAllocation = {

    costCategories: [],
    costSubCategories: [],
    costCenters: [],

    // =========================================================
    // OPEN
    // =========================================================
    open: async function (data) {

        $("#TxtPartyNameCA").val(data?.partyName || "");
        $("#TxtPartyCodeCA").val(data?.partyCode || "");
        $("#NumRefVno").val(data?.refNo || "");
        $("#txtRefVTypeCA").val(data?.refType || "");
        $("#TxtTotalAmtAllocated").val(data?.amount || "");
        $("#NumSrNo").val("");
        $("#DtSrDate").val(formatDateYMD(data?.date) || "");

        // Clear old table rows when opening
        $("#tblpurchaseallocationmodal tbody").empty();

        await this.loadDropDowns();

        const modal = new bootstrap.Modal(
            document.getElementById("costallocationModal")
        );

        modal.show();

        $("#btnSaveCA").off("click").on("click", async function () {
            const saved = await CostAllocation.save();
            if (saved) {
                CostAllocation.close();
            }
        });

        await this.checkAllocation(data);

    },

    // =========================================================
    // CLOSE
    // =========================================================
    close: function () {

        bootstrap.Modal.getInstance(
            document.getElementById("costallocationModal")
        ).hide();

    },
    // =========================================================
    // GENERATE VNO
    // =========================================================
    async getVNo(vType) {
        try {
            const res = await fetch(`/CostAllocation/GetVNo?vType=${encodeURIComponent(vType)}`);
            if (!res.ok)
                throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            if (!data.v_NO)
                throw new Error("Response missing v_NO");
            $("#NumSrNo").val(data.v_NO);
            return data.v_NO;
        }
        catch (e) {
            showToast("Error loading Voucher Number : " + e.message, { type: "warning" });
            return null;
        }
    },

    // =========================================================
    // CHECK ALLOCATION
    // =========================================================
    checkAllocation: async function checkAllocation(data) {

        try {

            const url = `/CostAllocation/IsCostAllocationExists?refType=${encodeURIComponent(data.refType)}&refNo=${data.refNo}&drAc=${data.drAct}`;

            const response = await fetch(url);
            const result = await response.json();

            if (!result.status) {
                showToast(result.message, { type: "warning" });
                return;
            }

            if (result.exists) {
                const totNetAmt = parseFloat($("#TxtTotalAmtAllocated").val() || 0);
                const resultBalAmt = parseFloat(result.balAmt || 0);

                let balAmt = totNetAmt - resultBalAmt;

                $("#NumBalAmount").val(balAmt);
                await this.loadData(data);
                this.setReadOnly(true);
            }
            else {
                // New Allocation
                await this.getVNo("COAL");
                console.log("CA data: ", result.data);
                if (result.data) {
                    // Add initial row with returned data
                    this.addRow(result.data);
                }
                else {
                    this.addRow();
                }
                
                this.setReadOnly(false);
            }

        }
        catch (e) {

            showToast(e.message, { type: "warning" });

        }

    },
    // =========================================================
    // LOAD DATA
    // =========================================================
    async loadData(data) {

        try {

            const res = await fetch(`/CostAllocation/GetAllocation?refType=${data.refType}&refNo=${data.refNo}`);

            const result = await res.json();
            console.log("CA result: ", result);

            if (result.status) {
                // VIEW MODE
                $("#NumSrNo").val(result.data.voucherNo);
                $("#DtSrDate").val(formatDateYMD(result.data.voucherDate));
                //$("#NumBalAmount").val(result.balanceAmount);
                // Add existing rows directly to table
                const rows = result.data.rows || [];

                rows.forEach(row => {
                    this.addRow(row);
                });
            }
            else {

                // ADD MODE
                await this.getVNo("COAL");
                if (result.defaultRow) {
                    this.addRow(result.defaultRow);
                } else {
                    this.addRow();
                }
            }

        }
        catch (e) {
            showToast(e.message, { type: "warning" });
        }
    },

    // =========================================================
    // ADD NEW ROW
    // =========================================================

    //addRow: function (rowData = {}) {

    //    const $tbody = $("#tblpurchaseallocationmodal tbody");
    //    const $lastRow = $tbody.find("tr:last");

    //    // =====================================================
    //    // FIRST ROW
    //    // =====================================================
    //    if (!$lastRow.length) {

    //        const rowHtml = `
    //        <tr>

    //            <td>
    //                <select class="form-control cost-category">
    //                    ${this.buildOptions(
    //            this.costCategories,
    //            rowData.costCategoryId || ""
    //        )}
    //                </select>
    //            </td>

    //            <td>
    //                <select class="form-control cost-subcategory">
    //                    ${this.buildOptions(
    //            this.costSubCategories,
    //            rowData.costSubCategoryId || ""
    //        )}
    //                </select>
    //            </td>

    //            <td>
    //                <select class="form-control cost-center">
    //                    ${this.buildOptions(
    //            this.costCenters,
    //            rowData.costCenterId || ""
    //        )}
    //                </select>
    //            </td>

    //            <td>
    //                <input type="number"
    //                       class="form-control allocation-amount"
    //                       value="${rowData.allocationAmount ?? ''}">
    //            </td>

    //            <td>
    //                <input type="text"
    //                       class="form-control narration"
    //                       value="${rowData.narration ?? ''}">
    //            </td>

    //            <td class="action-col">
    //                <div class="action-wrap">

    //                    <button type="button"
    //                            class="act-btn add btn-add-action"
    //                            onclick="CostAllocation.addRow()">
    //                        <i class="fa fa-plus-circle"></i>
    //                    </button>

    //                    <button type="button"
    //                            class="act-btn delete btn-delete-action"
    //                            onclick="CostAllocation.removeRow(this)">
    //                        <i class="fa fa-trash"></i>
    //                    </button>

    //                </div>
    //            </td>

    //        </tr>
    //    `;

    //        $tbody.append(rowHtml);

    //        const $newRow = $tbody.find("tr:last");

    //        this.initSelect2($newRow.find(".cost-category"));
    //        this.initSelect2($newRow.find(".cost-subcategory"));
    //        this.initSelect2($newRow.find(".cost-center"));

    //        this.bindEvents();

    //        return;
    //    }


    //    // =====================================================
    //    // DESTROY SELECT2 BEFORE CLONING
    //    // =====================================================

    //    $lastRow.find("select").each(function () {

    //        if ($(this).hasClass("select2-hidden-accessible")) {
    //            $(this).select2("destroy");
    //        }

    //    });


    //    // =====================================================
    //    // REMOVE + FROM CURRENT LAST ROW
    //    // =====================================================

    //    $lastRow.find(".btn-add-action").remove();


    //    // =====================================================
    //    // CLONE CLEAN ROW
    //    // =====================================================

    //    const $newRow = $lastRow.clone(false);


    //    // =====================================================
    //    // CLEAR NEW ROW VALUES
    //    // =====================================================

    //    $newRow.find(".cost-category").val("");
    //    $newRow.find(".cost-subcategory").val("");
    //    $newRow.find(".cost-center").val("");
    //    $newRow.find(".allocation-amount").val("");
    //    $newRow.find(".narration").val("");


    //    // =====================================================
    //    // ADD NEW ROW
    //    // =====================================================

    //    $tbody.append($newRow);


    //    // =====================================================
    //    // ADD + ONLY TO NEW LAST ROW
    //    // =====================================================

    //    $newRow.find(".action-wrap").prepend(`
    //    <button type="button"
    //            class="act-btn add btn-add-action"
    //            onclick="CostAllocation.addRow()">
    //        <i class="fa fa-plus-circle"></i>
    //    </button>
    //`);


    //    // =====================================================
    //    // REINITIALIZE SELECT2 ON BOTH ROWS
    //    // =====================================================

    //    this.initSelect2($lastRow.find(".cost-category"));
    //    this.initSelect2($lastRow.find(".cost-subcategory"));
    //    this.initSelect2($lastRow.find(".cost-center"));

    //    this.initSelect2($newRow.find(".cost-category"));
    //    this.initSelect2($newRow.find(".cost-subcategory"));
    //    this.initSelect2($newRow.find(".cost-center"));


    //    this.bindEvents();
    //},
    addRow: function (rowData = {}) {

        const $tbody = $("#tblpurchaseallocationmodal tbody");

        // =====================================================
        // CREATE NEW ROW HTML
        // =====================================================

        const rowHtml = `
        <tr>

            <td>
                <select class="form-control cost-category">
                    ${this.buildOptions(
            this.costCategories,
            rowData.costCategoryId || ""
        )}
                </select>
            </td>

            <td>
                <select class="form-control cost-subcategory">
                    ${this.buildOptions(
            this.costSubCategories,
            rowData.costSubCategoryId || ""
        )}
                </select>
            </td>

            <td>
                <select class="form-control cost-center">
                    ${this.buildOptions(
            this.costCenters,
            rowData.costCenterId || ""
        )}
                </select>
            </td>

            <td>
                <input type="number"
                       class="form-control allocation-amount"
                       value="${rowData.allocationAmount ?? ''}">
            </td>

            <td>
                <input type="text"
                       class="form-control narration"
                       value="${rowData.narration ?? ''}">
            </td>

            <td class="action-col">
                <div class="action-wrap">

                    <button type="button"
                            class="act-btn add btn-add-action"
                            onclick="CostAllocation.addRow()">
                        <i class="fa fa-plus-circle"></i>
                    </button>

                    <button type="button"
                            class="act-btn delete btn-delete-action"
                            onclick="CostAllocation.removeRow(this)">
                        <i class="fa fa-trash"></i>
                    </button>

                </div>
            </td>

        </tr>
    `;

        // =====================================================
        // APPEND ROW
        // =====================================================

        $tbody.append(rowHtml);

        const $newRow = $tbody.find("tr:last");

        // =====================================================
        // ONLY LAST ROW GETS +
        // =====================================================

        $tbody.find(".btn-add-action").remove();

        $newRow.find(".action-wrap").prepend(`
        <button type="button"
                class="act-btn add btn-add-action"
                onclick="CostAllocation.addRow()">
            <i class="fa fa-plus-circle"></i>
        </button>
    `);

        // =====================================================
        // INITIALIZE SELECT2 FOR NEW ROW
        // =====================================================

        this.initSelect2($newRow.find(".cost-category"));
        this.initSelect2($newRow.find(".cost-subcategory"));
        this.initSelect2($newRow.find(".cost-center"));

        this.bindEvents();
    },

    // =========================================================
    // REMOVE ROW
    // =========================================================

    removeRow: async function (btn) {

        const $tbody =
            $("#tblpurchaseallocationmodal tbody");

        const $rows =
            $tbody.find("tr");

        // Don't delete if only one row exists
        if ($rows.length === 1)
            return;

        const result = await Swal.fire({

            title: "Are you sure?",

            text: "Do you want to delete this row?",

            icon: "warning",

            showCancelButton: true,

            confirmButtonText: "Yes, delete it",

            cancelButtonText: "Cancel",

            target:
                document.getElementById(
                    "costallocationModal"
                ),

            zIndex: 99999
        });

        if (!result.isConfirmed)
            return;

        const $row =
            $(btn).closest("tr");

        const wasLastRow =
            $row.is($tbody.find("tr:last"));

        $row.remove();

        // If deleted row was last row,
        // add + button to new last row
        if (wasLastRow) {

            $tbody
                .find("tr:last .action-wrap")
                .prepend(`
                    <button type="button"
                            class="act-btn add btn-add-action"
                            onclick="CostAllocation.addRow()">
                        <i class="fa fa-plus-circle"></i>
                    </button>
                `);
        }

        this.bindEvents();
    },

    // =========================================================
    // LOAD DROPDOWNS
    // =========================================================
    async loadDropDowns() {

        try {

            const res = await fetch("/CostAllocation/GetDropDowns");

            if (!res.ok)
                throw new Error("Unable to load dropdowns data.");

            const data = await res.json();

            this.costCategories = data.costCategories || [];
            this.costSubCategories = data.costSubCategories || [];
            this.costCenters = data.costCenters || [];

        }
        catch (e) {

            showToast(e.message, { type: "warning" });

        }

    },
    // =========================================================
    // BUILD OPTIONS
    // =========================================================
    buildOptions: function (list, selectedValue) {
        let html = `<option value="">--Select--</option>`;
        list.forEach(x => {
            html += `<option value="${x.code}" ${x.code == selectedValue ? "selected" : ""}>${x.name}</option>`;
        });
        return html;
    },

    // =========================================================
    // SELECT2
    // =========================================================
    initSelect2: function initSelect2($ddl) {

        $ddl.each(function () {

            if ($(this).hasClass("select2-hidden-accessible")) {
                if ($(this).data("select2")) {
                    $(this).select2("destroy");
                }
            }

            $(this).select2({
                placeholder: '-- Select --',
                allowClear: true,
                width: '100%',
                dropdownParent: $('#costallocationModal')
            });

        });

        $ddl.off('select2:open').on('select2:open', function () {

            setTimeout(function () {

                const searchBox = document.querySelector(
                    '.select2-container--open .select2-search__field'
                );

                if (searchBox) {
                    searchBox.focus();
                }

            }, 0);

        });

    },


    // =========================================================
    // READ ONLY
    // =========================================================
    setReadOnly: function setReadOnly(isReadOnly) {
        $("#btnEditCA").prop("disabled", !isReadOnly).css("opacity", !isReadOnly ? "0.5" : "1");;
        $("#btnSaveCA").prop("disabled", isReadOnly).css("opacity", isReadOnly ? "0.5" : "1");

        const $table = $("#tblpurchaseallocationmodal");
        $table.find("input").prop("readonly", isReadOnly);
        $table.find("select").prop("disabled", isReadOnly);
        $(".btn-add-action, .btn-delete-action").prop("disabled", isReadOnly).css("opacity", isReadOnly ? "0.5" : "1");
        //$("#lblFlg").text("VIEW");
    },

    // =========================================================
    // EVENTS
    // =========================================================
    bindEvents: function () {

        //Allocation amount change
        $("#tblpurchaseallocationmodal")
            .off("input", ".allocation-amount")
            .on("input", ".allocation-amount", function () {

                const $rows = $("#tblpurchaseallocationmodal tbody tr");

                let total = 0;

                $rows.each(function () {
                    total += parseFloat($(this).find(".allocation-amount").val()) || 0;
                });

                const totalAmt = parseFloat($("#TxtTotalAmtAllocated").val()) || 0;
                $("#NumBalAmount").val((totalAmt - total).toFixed(2));

            });

        // Duplicate Category + SubCategory + Cost Center check
        $("#tblpurchaseallocationmodal")
            .off("change", ".cost-center")
            .on("change", ".cost-center", function () {

                const $row = $(this).closest("tr");

                if (!CostAllocation.validateDuplicateCombination($row)) {
                    $(this).val("").trigger("change");
                    return;
                }

            });
    },

    // =========================================================
    // VALIDATE SAVE
    // =========================================================
    validateSave: function () {

        let isValid = true;

        $("#tblpurchaseallocationmodal tbody tr").each(function () {

            if (!validateRequiredField($(this).find(".cost-category"), 'Cost category')
                || !validateRequiredField($(this).find(".cost-subcategory"), 'Cost subcategory')
                || !validateRequiredField($(this).find(".cost-center"), 'Cost center')
            ) {
                isValid = false;
                return false;
            }
        });

        return isValid;
    },

    // =========================================================
    // SAVE
    // =========================================================
    async save() {

        if (!this.validateSave())
            return false;

        // Check balance amount
        const balAmt = $("#NumBalAmount").val().trim();

        if (balAmt === "" || parseFloat(balAmt) !== 0) {
            showToast("All amount are not allocated OR Over allocated.", {
                type: "warning"
            });
            return false;
        }

        const rows = [];

        $("#tblpurchaseallocationmodal tbody tr").each(function () {

            rows.push({
                costCategoryId: parseInt($(this).find(".cost-category").val()) || 0,
                costSubCategoryId: parseInt($(this).find(".cost-subcategory").val()) || 0,
                costCenterId: parseInt($(this).find(".cost-center").val()) || 0,
                allocationAmount: parseFloat($(this).find(".allocation-amount").val()) || 0,
                narration: $(this).find(".narration").val() || ''
            });

        });

        const payload = {
            VoucherNo: parseInt($("#NumSrNo").val()) || 0,
            VoucherDate: $("#DtSrDate").val(),
            refType: $("#txtRefVTypeCA").val() || '',
            refNo: parseInt($("#NumRefVno").val()) || 0,
            totalAmount: parseFloat($("#TxtTotalAmtAllocated").val()) || 0,
            rows: rows
        };

        const response = await fetch("/CostAllocation/SaveCA", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (result.status) {
            showToast("Cost Allocation saved successfully.", { type: "success" });
            return true;
        }

        showToast(result.message, { type: "warning" });
        return false;
    },

    // =========================================================
    // DUPLICATE VALIDATION
    // =========================================================
    validateDuplicateCombination: function ($currentRow) {

        const category = $currentRow.find(".cost-category").val();
        const subCategory = $currentRow.find(".cost-subcategory").val();
        const costCenter = $currentRow.find(".cost-center").val();

        if (!category || !subCategory || !costCenter)
            return true;

        let duplicate = false;

        $("#tblpurchaseallocationmodal tbody tr").each(function () {

            if ($(this).is($currentRow))
                return;

            const rowCategory = $(this).find(".cost-category").val();
            const rowSubCategory = $(this).find(".cost-subcategory").val();
            const rowCostCenter = $(this).find(".cost-center").val();

            if (category == rowCategory && subCategory == rowSubCategory && costCenter == rowCostCenter) {
                duplicate = true;
                return false;
            }
        });

        if (duplicate) {
            showToast("This combination already selected.", { type: "warning" });
            return false;
        }
        return true;
    },
};