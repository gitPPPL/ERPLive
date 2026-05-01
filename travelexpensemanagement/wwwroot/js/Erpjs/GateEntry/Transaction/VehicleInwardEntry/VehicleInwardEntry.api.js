const VehicleApi = {
    //========DocType&DocNo========
    GetDocType: function () {
        $.ajax({
            url: '/VehicleInwardEntry/GetDocType',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    VehicleUI.bindDocType(response);
                } else {
                    showToast("Invalid response status.", { type: "error" });
                }
            },
            error: function (xhr, status, error) {
                // toastr.error("Document Type Load failed: " + error);
                showToast("Document Type Load failed: " + error, { type: "error" });
            }
        });
    }, 
    GetDocid: function GetDocid(VType) {
        $.ajax({
            url: '/VehicleInwardEntry/GetMaxVNo',
            type: 'GET',
            data: { V_type: VType },
            success: function (response) {
                VehicleUI.bindDocNo(response)
            },
            error: function (xhr, status, error) {
                showToast('Error fetching Doc ID:', error, { type: "error" });
            }
        });
    },
    GetDocData: async function GetDocData(MasterTblId, readOnly) {
        try {
            const response = await $.ajax({
                url: '/VehicleInwardEntry/GetTransportInwardRecordsById',
                type: 'GET',
                data: { id: MasterTblId }
            });
            if (response.status) {
                await VehicleUI.fillFormFields(response.data);
                VehicleUI.setReadOnly(readOnly);
            } else {
                showToast('No data returned.', { type: "error" });
            }
        } catch (error) {
            showToast('Failed to load data.', { type: "error" });
            console.error(error);
        }
    },
    //=========Dropdown=================
    GetCustomerList: function GetCustomerList(selectedValue = null) {
        $.ajax({
            url: '/VehicleInwardEntry/GetPartyList',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    VehicleUI.bindCustomerDropdown(response.data, selectedValue)
                } else {
                    showToast("Party Load failed", { type: "error" });
                }
            },
            error: function (xhr, status, error) {
                showToast("Party Load failed: " + error, { type: "error" });
            }
        });
    },
    GetTransportList: function GetTransportList(selectedValue = null) {
        $.ajax({
            url: '/VehicleInwardEntry/GetTransportationList',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    VehicleUI.bindTransportDropdown(response.data, selectedValue);
                }
                else {
                    showToast("Transport Name Load failed", { type: "error" });
                }
            },
            error: function (xhr, status, error) {
                // toastr.error("Transport Name Load failed", xhr.error);
                showToast("Transport Name Load failed", { type: "error" });
            }
        });
    },
    GetDONo: function GetDONo(selectedValue = null) {
        $.ajax({
            url: '/VehicleInwardEntry/GetDONo',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    VehicleUI.bindDoNoDropdown(response.data, selectedValue);
                } else {
                    showToast("Do No Load failed", { type: "error" });
                }
            },
            error: function (xhr, status, error) {
                showToast("Do No Load failed: " + error, { type: "error" });
            }
        });
    },
    getDriverDetails: function getDriverDetails(mobileNo) {
        $.ajax({
            url: '/VehicleInwardEntry/GetDriverDetails',
            type: 'GET',
            dataType: 'json',
            data: { mobileNo: mobileNo },
            success: function (response) {
                if (response.status) {
                    const data = response.driverDetails;
                    VehicleUI.bindDriverDetails(data);
                }
            },
            error: function (xhr, status, error) {
                showToast("Driver Details Load failed" + xhr.error, { type: "error" });
            }
        });
    },
    getVehicleDetailsFromDB: function getVehicleDetailsFromDB(vehicleNo) {
        return $.ajax({
            url: '/VehicleInwardEntry/GetVehicleInfoFromDB',
            type: 'GET',
            dataType: 'json',
            data: { vehicleNo: vehicleNo }
        }).then(function (response) {
            if (response.success && response.vehicleInfo) {
                const data = response.vehicleInfo;
                VehicleUI.bindVehicleInfoFromDB(data);
                return true;
            }
            return false;
        }).catch(function () {
            return false;
        });
    },
    getVehicleDetailsFromApi: async function GetVehicledetailFromApi() {
        try {
            const rcNumber = $('#TxtVehicleNo').val();
            if (!rcNumber) {
                showToast("Vehicle No Not Found", { type: "info" });
                return;
            }
            const response = await $.ajax({
                url: `/VehicleInwardEntry/GetVehcleinfo`,
                data: { rc_number: rcNumber },
                type: 'GET',
                dataType: 'json',
            });
            if (response && response.status) {
                const res = response.vehicleInfo;
                VehicleUI.bindVehicleInfoFromApi(res);
            } else {
                showToast("Vehicle not found or invalid.", { type: "error" });
            }
        } catch (err) {
            console.error("Error:", err);
            showToast("Error fetching vehicle details.", { type: "error" });
        }
    },
    //============Date Validation==============
    checkValidDate: async function checkValidDate() {
        const data = {
            vdate: $("#DtDocDate").val(),
            vtype: $("#ddlDocType").val(),
            vno: $("#NumDocNo").val()
        };
        try {
            const response = await fetch('/VehicleInwardEntry/CheckValidDate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });
            const result = await response.json();
            if (result.status === false) {
                // toastr.warning(result.message);
                showToast(result.message, { type: "warning" });
                return false;
            }
            return true;
        } catch (error) {
            console.error("Error:", error);
            return false;
        }
    },
    //==========Save&Update===================
    SaveData: function SaveData(saveDt) {
        $.ajax({
            url: '/VehicleInwardEntry/SaveOrUpdateTransportInward',
            type: 'POST',
            contentType: false,
            processData: false,
            data: saveDt,
            success: function (response) {
                if (response?.status) {
                    showToast("Inserted successfully!", { type: "success" });
                    setTimeout(() => {
                        window.location.href = '/VehicleInwardEntryList/Index';
                    }, 1500);
                } else {
                    showToast(response?.message || "Save failed. Please try again.", { type: "error" });
                }
            },
            error: function () {
                showToast("Error occurred while saving. Please contact admin.", { type: "error" });
            }
        });
    },
    UpdateData: function UpdateData(UpdateDt) {
        $.ajax({
            url: '/VehicleInwardEntry/SaveOrUpdateTransportInward',
            type: 'POST',
            contentType: false,
            processData: false,
            data: UpdateDt,
            success: function (response) {
                if (response?.status) {
                    showToast("Updated successfully!", { type: "success" });
                    setTimeout(() => {
                        window.location.href = '/VehicleInwardEntryList/Index';
                    }, 1500);
                } else {
                    showToast("Update failed: " + (response?.message || "Unknown error."), { type: "error" });
                }
            },
            error: function (xhr, status, error) {
                showToast("Data not updated: " + error, { type: "error" });
            }
        });
    }
};