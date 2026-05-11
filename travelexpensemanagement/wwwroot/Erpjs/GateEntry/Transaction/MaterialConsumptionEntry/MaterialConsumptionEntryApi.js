const MaterialAPI = (function () {

    // ================= DROPDOWNS =================
    async function LoadDropDowns() {
        await Promise.all([
            DDLVtype(),
            DDLParty(),
            loadItemMaster(),
            loadDeptMaster(),
            loadUnit()
        ]);
    }

    async function loadItemMaster() {
        const res = await fetch("/MiscConsumptionEntry/DDLItemMaster");
        const data = await res.json();
        itemMap = {};
        data.forEach(i => itemMap[i.value] = i.text);
    }

    async function loadDeptMaster() {
        const res = await fetch("/MiscConsumptionEntry/DDLDeptMaster");
        const data = await res.json();
        DeptMap = {};
        data.forEach(i => DeptMap[i.value] = i.text);
    }

    async function loadUnit() {
        const res = await fetch("/MiscConsumptionEntry/DDLUnit");
        const data = await res.json();
        UnitMap = {};
        data.forEach(i => UnitMap[i.value] = i.text);
    }

    async function DDLVtype() {
        const res = await fetch("/MiscConsumptionEntry/DDlVType");
        const list = await res.json();
        const ddl = $("#ddlDocType");

        ddl.empty();

        list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));

        if (list.length > 0) {
            const defaultVal = list[0].value;
            ddl.val(defaultVal);
            GetVNo(defaultVal);
        }
    }

    async function DDLParty() {
        const res = await fetch("/MiscConsumptionEntry/DDlParty");
        const list = await res.json();
        const ddl = $("#ddlPartyName");

        ddl.empty().append('<option value="">-- Select Party --</option>');
        list.forEach(it => ddl.append(`<option value="${it.value}">${it.text}</option>`));
    }

    async function GetVNo(Vtype) {
        const res = await fetch(`/MiscConsumptionEntry/GetVNo?Vtype=${encodeURIComponent(Vtype)}`);
        const data = await res.json();

        if (data.v_NO) {
            $('#NumDocNo').val(data.v_NO);
        } else {
            console.warn('No document number received');
        }
    }

    // ================= PARTY =================
    async function fetchPartyAddressDetails(partyId) {
        try {
            const res = await fetch(`/MiscConsumptionEntry/GetAddressByPartyCode?PartyId=${encodeURIComponent(partyId)}`);
            if (!res.ok) throw new Error(`Server error: ${res.status}`);

            const details = await res.json();

            if (details.length) {
                const d = details[0];
                $("#TxtAdd1PD").val(d.add1);
                $("#TxtAdd2PD").val(d.add2);
                $("#TxtAdd3PD").val(d.add3);
            } else {
                $("#TxtAdd1PD, #TxtAdd2PD, #TxtAdd3PD").val("");
            }

        } catch (e) {
            console.error("Error fetching party address:", e);
        }
    }

    // ================= SAVE =================
    function saveData(payload) {
        return $.ajax({
            url: '/MiscConsumptionEntry/SavedData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        });
    }

    // ================= LOAD FORM =================
    function LoadFormByID(rowId, vtype) {
        return $.ajax({
            url: '/MiscConsumptionEntryList/GetDataByCode',
            method: 'POST',
            data: { rowId, vtype }
        });
    }

    // ================= PENDING =================
    function loadPendingDocuments(partyId) {
        return $.ajax({
            url: '/MiscConsumptionEntryList/GetPendingDocumnents',
            type: 'GET',
            data: { PartyId: partyId }
        });
    }

    // ================= EXPORT =================
    return {
        LoadDropDowns,
        fetchPartyAddressDetails,
        saveData,
        LoadFormByID,
        loadPendingDocuments,
        GetVNo
    };

})();
