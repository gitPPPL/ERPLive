

    let itemMap = { };
    let UnitMap = { };
    let DeptMap = {};
    let pendingData = [];
    let currentPage = 1;
    let rowsPerPage = 10;
    const urlParams = new URLSearchParams(window.location.search);
    const rowId = urlParams.get('id');
    const vtype = urlParams.get('VType');
    const $tbody = $("#tblOutwardEntry tbody");
    const form = $('#OutwardEntryForm');
    const mode = urlParams.get('mode');
    const isReadOnly = (mode === 'view');
    var globalVars = window.globalVariables || {};
    var database = window.database || "";
    let PubUserLevel = globalVars.UserLevel;
    let CompCode = globalVars.CompCode;
    let LoginDate = globalVars.LoginDate;
    var controllerName = window.location.pathname.split('/')[1];

    $(document).ready(function () {
        (async () => {
            try {

                checkPermissionForEntryPage(controllerName);



                await LoadDropDowns();
                SetFYDate('DtDocDate', LoginDate);

                if (PubUserLevel == 1) {
                    $('#DtDocDate').prop('disabled', false);
                    $('#DtTxtDocDate').prop('disabled', false);
                }
                else {
                    $('#DtDocDate').prop('disabled', true);
                    $('#DtTxtDocDate').prop('disabled', true);
                }          


                if (rowId) {

                    $('#ddlDocType').prop('disabled', true);
                    $('#ddlType').prop('disabled', true);

                    await LoadFormByID(rowId, vtype);
                    if (mode == 'view') {
                        setFormReadOnly();
                        form.addClass('erppage-readonly');
                    } 

                }
                else {
                    const selectedVType = $("#ddlDocType").val();
                    if (selectedVType) {
                        await GetVNo(selectedVType, "GATE1");
                    }             
                
                    let now = new Date();
                    $('#DtTxtDocDate').val(now.toTimeString().slice(0, 8));
                    var today = new Date().toISOString().split('T')[0];
                    $('#DtExpectedDateReturn').val(today);

                }

                $("#btn-save").click(async function (e) {
                    e.preventDefault();

                    const V_DATE = formatDate($("#DtDocDate").val());
                    const V_NO = parseInt($('#NumDocNo').val()) || null;
                    const RETURN_DATE = formatDate($("#DtExpectedDateReturn").val());
                    const RESPONSIBLE_PERSON = $.trim($('#txtResponsiblePerson').val());
                    const DocType = $.trim($('#ddlDocType').val());
                    const ITEM_TYPE = $.trim($('#ddlType option:selected').text());
                    const PartyCode = parseInt($('#ddlPartyName').val()) || 0;

                    if (!validateRequiredField('#NumDocNo', 'Please Enter a Doc No.')) return;
                    if (!validateRequiredField('#ddlDocType', 'Please select a Doc Type.')) return;
                    if (!validateRequiredField('#DtDocDate', 'Please select a Doc Date.')) return;                             
                    if (!validateRequiredField('#ddlPartyName', 'Please select a Party.')) return; 
                    if (!validateRequiredField('#TxtVehicleNo', 'Please Fill Vehicle No.')) return; 

                    const checkdate = await checkValidDate(); 

                    if (!checkdate) {
                        return; 
                    }

                    if (DocType === "OURT") {

                        if (!validateRequiredField('#DtExpectedDateReturn', 'Please Select Exp.Dt of Return.')) return;    

                        if (RETURN_DATE < V_DATE) {                 
                            showToast("Invalid Return Date. Return date should not be less than Doc date.", { type: "warning" });
                            return;
                        }    
                                            
                        if (!validateRequiredField('#txtResponsiblePerson', 'Please enter Responsible Person Name.')) return;                         
                    }
     

                    if (CompCode != 2)
                    {
                        if ((DocType === "OUSL" && ITEM_TYPE !== "Sale") || (DocType !== "OUSL" && ITEM_TYPE === "Sale")) {
                            showToast("Please check DocType : " + DocType + " and Type : " + ITEM_TYPE  +" ", "Warning", { type: "warning" });
                            return;
                        }
                    }
                    else
                    {
                        if (DocType !== "OUNR" && ITEM_TYPE === "Sale") {
                            showToast("Please check Sale Type and Doctype.", "Warning", { type: "warning" });
                            return;
                        }
                    }

                    if ((DocType === "OUES" && ITEM_TYPE !== "E-Commerce Sale") || (DocType !== "OUES" && ITEM_TYPE === "E-Commerce Sale")) {
                        showToast("Please check DocType : "+ DocType +" and Type : "+ ITEM_TYPE +"", "Warning", { type: "warning" });
                        return;
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

                            if (!itemName) {
                                toastr.warning(`Please select Item Name in row ${index + 1}`);
                                $row.find("select.itemName").focus();
                                isValid = false;
                                return false;
                            }

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

                    if (!isValid) {
                        return;
                    }

                    if (!hasAtLeastOneItem) {
                        toastr.warning("Please Add One Row In Detail Section. ");
                        return;
                    }

                    const header = {
                        RETURN_DATE: RETURN_DATE,
                        RESPONSIBLE_PERSONB: RESPONSIBLE_PERSON,
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

                    $("#btn-save").prop("disabled", true);

                    $.ajax({
                        url: '/OutwardEntry/SavedData',
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify(payload),

                        success: function (response) {

                            console.log("Response:", response);

                            if (response.success === true || response.success === "true") {

                                if (response.message === "Save Successfully") {

                                    showToast("Saved successfully!", { type: "success" });

                                    setTimeout(function () {
                                        window.location.href =  '/OutwardEntry/Index?id=' + V_NO + '&VType=' + encodeURIComponent(DocType) +
                                            '&mode=view';
                                    }, 3000);

                                } else {

                                    showToast(response.message || "Operation completed.", {
                                        type: "warning"
                                    });
                                }

                            } else {

                                showToast(response.message || "Failed to save data.", {
                                    type: "error"
                                });
                            }
                        },

                        error: function (xhr, status, error) {

                            let errorMessage = "Something went wrong.";

                            if (xhr.status === 400) {
                                errorMessage = "Bad Request: " + (xhr.responseText || error);

                            } else if (xhr.status === 500) {
                                errorMessage = "Server Error: " + (xhr.responseText || error);

                            } else {
                                errorMessage = "Unexpected Error: " + (error || xhr.statusText);
                            }

                            console.error("AJAX Error:", xhr);

                            showToast(errorMessage, { type: "error" });
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
          
                });


                $("#ddlDocType").change(function () {
                    if (!rowId) GetVNo(this.value, "GATE1");
                    $('#ddlDocType').prop('disabled', true);
                    if (this.value === "OURT") {
                        document.getElementById("Conditionnaldesignid").style.display = "contents";
                    } else {
                        document.getElementById("Conditionnaldesignid").style.display = "none";
                    }

                });

                $("#ddlPartyName").on("change", async function () {
                    const partyId = $(this).val();
                    if (mode != 'view' && !rowId) {
                        $('#ddlDocType').prop('disabled', true);
                       await loadPartyAddresses(partyId);
                       await fetchPartyDetails(partyId);
                    } 
                });

                $("#ddlPartyNameByAddress").on("change", function () {
                    const partyId = $("#ddlPartyName").val();
                    const addId = $(this).val();
                
                    GetDataByPartyandAddressidCodeAsync(partyId, addId);

                });

                $("#btnpendingorderno").click(function () {
                    const selectedValue = $('#ddlPartyName').val();             
                    const v_date = $('#DtDocDate').val();
                    const typeText = $('#ddlType option:selected').text();
                    if (!validateRequiredField('#ddlType', 'Please Select  Type.')) return;
                    if (!validateRequiredField('#ddlPartyName', 'Please Select Party Name.')) return;                 

                    FetchPendindorderno(selectedValue, typeText, v_date);
                });
                 
                $("#Btn_selectedData").click(async function () {

                    const selectedRows = getSelectedPendingRows();
                    let condiiton = false;
                    if (!selectedRows.length) {
                        toastr.info("Please select at least one row");
                        return;
                    }

                    try {

                        const deptCode = await $.ajax({
                            url: "/OutwardEntryList/GetDeptCode",
                            type: "GET"
                        });

                        const $tbody = $("#tblOutwardEntry tbody");

                        const modalElement = document.getElementById('pendingorders');
                        if (modalElement) {
                            const modalInstance = bootstrap.Modal.getInstance(modalElement);
                            if (modalInstance) modalInstance.hide();
                        }
                        $('#ddlType').prop('disabled', true);
                        $('#ddlPartyName').prop('disabled', true);
                        for (const row of selectedRows) {

                            const itemCode = (row.ItemCode || "").trim();
                            const voucherNo = (row.VoucherNo || "").trim();

                            const exists = $tbody.find("tr").toArray().some(tr => {

                                const existingItemCode = $(tr).find(".itemName").val();
                                const existingRefNo = $(tr).find(".ref-no").val()?.trim() || "";

                                return existingItemCode === itemCode && existingRefNo === voucherNo;
                            });

                            if (exists) {
                                toastr.warning(`Item ${itemCode} with Ref No ${voucherNo} already exists.`);
                                continue;
                            }

                            addRow($tbody, {
                                code: itemCode,
                                itemName: itemCode,
                                department: deptCode || "",
                                unit: row.UnitCode || "",
                                quantity: row.PQty ? (row.PQty) : "",
                                no: row.nos ? parseInt(row.nos) : "",
                                remarks: row.remarks || "",
                                refType: row.Vouchertype || "",
                                refNo: voucherNo
                            });
                             condiiton = true;
                        }

                        // Load header from first row

                        if (condiiton == true) {
                            const $firstRow = $tbody.find("tr:first");
                            if ($firstRow.length) {

                                const refType = $firstRow.find(".ref-type").val() || "";
                                const refNo = $firstRow.find(".ref-no").val() || "";
                                if (refNo != '' && refType != '') {

                                    const typeText = $('#ddlType option:selected').text();

                                    await fetchPendingOrderHeaderData(refType, refNo, typeText);
                                }
                            }
                        }


                     
                         
                    } catch (err) {
                        console.error(err);
                        toastr.error("Failed to fetch data");
                    }
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


                $('#ddlREFNO').on("change", function () {
                    var refno = this.value;
                    if (refno) {
                        fetchDatabyRefNo(refno);
                    }
                });


            } catch (err) {
                console.error("Error initializing page:", err);
            }
        })();
    });

