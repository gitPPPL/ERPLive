function deleteRecord(controller, docId, options = {}) {
    const {
        action = "Delete", 
        title = "Are you sure?",
        text = "This action cannot be undone.",
        successCallback = null
    } = options;

    Swal.fire({
        title: title,
        text: text,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {

        if (!result.isConfirmed) return;

        $.ajax({
            url: `/${controller}/${action}`,
            type: 'POST',
            data: { docId: docId },

            success: function (response) {
                if (response.success) {

                    Swal.fire('Deleted!', response.message || 'Deleted successfully', 'success')
                        .then(() => {
                            if (typeof successCallback === "function") {
                                successCallback();
                            }
                        });

                } else {
                    Swal.fire('Failed', response.message, 'warning');
                }
            },

            error: function () {
                Swal.fire('Error!', 'Something went wrong.', 'error');
            }
        });
    });
}

function deleteRecordbytype(controller, docId, doctype, options = {}) {
    const {
        action = "Delete",
        title = "Are you sure?",
        text = "This action cannot be undone.",
        successCallback = null
    } = options;

    Swal.fire({
        title: title,
        text: text,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (!result.isConfirmed) return;
        var requestData = {
            vNo: docId,
            docType: doctype
        };
         
        $.ajax({
            url: `/${controller}/${action}`,
            type: 'POST',
            data: requestData,

            success: function (response) {

                console.log("Response:", response);

                if (response.status) {

                    Swal.fire('Deleted!', response.message || 'Deleted successfully', 'success')
                        .then(() => {
                            if (typeof successCallback === "function") {
                                successCallback();
                            }
                        });

                } else {
                    Swal.fire('Failed', response.message, 'warning');
                }
            },

            error: function (xhr) {
                console.log("ERROR:", xhr.responseText);
                Swal.fire('Error!', 'Something went wrong.', 'error');
            }
        });
    });
}


//function deleteVisitor(docId) {
//    deleteRecord("VisitorEntry", docId, {
//        action: "DeleteVisitorEntry",
//        text: "This will permanently delete the visitor entry.",
//        successCallback: loadAllVisitors
//    });
//}