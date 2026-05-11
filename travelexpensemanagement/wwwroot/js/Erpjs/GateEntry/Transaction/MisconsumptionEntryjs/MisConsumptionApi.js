const MisConsumptionApi = {

    // ================= VOUCHER NUMBER =================
    getVNo: function (vtype) {
        return fetch(`/MiscConsumptionEntry/GetVNo?Vtype=${encodeURIComponent(vtype)}`)
            .then(res => {
                if (!res.ok) throw new Error("Failed to generate VNo");
                return res.json();
            });
    },

    // ================= VALID DATE CHECK =================
    checkValidDate: function (data) {
        return fetch('/MiscConsumptionEntry/CheckValidDate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        }).then(r => r.json());
    },

    // ================= PARTY ADDRESS =================
    getPartyAddress: function (partyId) {
        return fetch(`/MiscConsumptionEntry/GetAddressByPartyCode?PartyId=${encodeURIComponent(partyId)}`)
            .then(res => {
                if (!res.ok) throw new Error("Failed to fetch party address");
                return res.json();
            });
    },

    // ================= LOAD DROPDOWNS =================
    getDocType: function () {
        return fetch("/MiscConsumptionEntry/GetDropdown")
            .then(r => r.json());
    },

    getParty: function () {
        return fetch("/MiscConsumptionEntry/GetDropdown")
            .then(r => r.json());
    },

    getItemMaster: function () {
        return fetch("/MiscConsumptionEntry/DDLItemMaster")
            .then(r => r.json());
    },

    getDeptMaster: function () {
        return fetch("/MiscConsumptionEntry/DDLDeptMaster")
            .then(r => r.json());
    },

    getUnit: function () {
        return fetch("/MiscConsumptionEntry/DDLUnit")
            .then(r => r.json());
    },

    // ================= SAVE DATA =================
    saveData: function (payload) {
        return fetch('/MiscConsumptionEntry/SaveData', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        })
        .then(res => {
            if (!res.ok) throw new Error("Save failed");
            return res.json();
        });
    },

    // ================= LOAD FORM BY ID =================
    getFormById: function (rowId, vtype) {
        return fetch('/MiscConsumptionEntryList/GetDataByCode', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: new URLSearchParams({
                rowId: rowId,
                vtype: vtype
            })
        })
        .then(res => {
            if (!res.ok) throw new Error("Failed to load form data");
            return res.json();
        });
    },

    // ================= PENDING DOCUMENTS =================
    getPendingDocuments: function (partyId) {
        return fetch(`/MiscConsumptionEntryList/GetPendingDocumnents?PartyId=${encodeURIComponent(partyId)}`)
        .then(res => {
            if (!res.ok) throw new Error("Failed to load pending documents");
            return res.json();
        });
    }
};