window.CostAllocation = {

    open: function (data) {

        $("#TxtPartyNameCA").val(data?.partyName || "");
        $("#NumRefVno").val(data?.refNo || "");
        $("#TxtTotalAmtAllocated").val(data?.amount || "");
        $("#NumSrNo").val(data?.srNo || "");
        $("#DtSrDate").val(data?.date || "");
        $("#NumBalAmount").val(data?.balance || "");

        const modal = new bootstrap.Modal(
            document.getElementById("costallocationModal")
        );

        modal.show();
    },

    close: function () {

        bootstrap.Modal.getInstance(
            document.getElementById("costallocationModal")
        ).hide();

    }

};