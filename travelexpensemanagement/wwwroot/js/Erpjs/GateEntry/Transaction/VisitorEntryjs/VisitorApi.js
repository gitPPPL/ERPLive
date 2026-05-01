const VisitorAPI = {

    loadEmpList: function () {
        return $.ajax({
            url: '/VisitorEntry/GetEmpList',
            type: 'GET',
            dataType: 'json'
        });
    },

    getVisitorByMobile: function (mobile) {
        return $.ajax({
            url: '/VisitorEntry/GetVisitorByMobile',
            type: 'GET',
            data: { mobileNo: mobile }
        });
    },

    saveVisitor: function (payload) {
        return $.ajax({
            url: '/VisitorEntry/SaveVisitorEntry',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        });
    },

    getVisitorById: function (docId) {
        return $.ajax({
            url: '/VisitorEntryList/GetVisitorByVno',
            type: 'GET',
            data: { docId: docId }
        });
    },

    getVNo: function () {
        return fetch('/VisitorEntry/GenerateVNo')
            .then(res => {
                if (!res.ok) {
                    throw new Error("Failed to generate VNo");
                }
                return res.json();
            });
    },

    checkValidDate: function (data) {
        return fetch('/VisitorEntry/CheckValidDate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        }).then(r => r.json());
    }
};