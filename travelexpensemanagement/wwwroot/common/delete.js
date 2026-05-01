function deleteRecord(controller, docId, options = {}) {

    const {
        action = "Delete", // default action name
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

