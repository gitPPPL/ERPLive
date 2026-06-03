    let incommingQCPagination;
    $(document).ready(function () {
        incommingQCPagination = Pagination.create({
            pageSize: 10,
            paginationContainer: '#pageNumbers',
            infoContainer: '#pageInfoText',
            loader: function (params) {
                $.ajax({
                    url: '/IncommingQCList/GetQCIncommingQCEntryList',
                    type: 'GET',
                    dataType: 'json',
                    data: {
                        searchTerm: $('#searchBox').val(),
                        pageNumber: params.pageNumber,
                        pageSize: params.pageSize
                    },
                    success: function (res) {
                        console.log('Incoming QC List:', res);
                        params.callback({
                            data: res.items || [],
                            totalCount: res.totalCount || 0
                        });
                    },
                    error: function (xhr) {
                        toastr.error(
                            'Error loading data: ' +
                            (xhr.responseJSON?.message || xhr.statusText)
                        );
                    }
                });
            },
            render: function (docs) {
                const tbody = $('#tblIncommingQCList tbody');
                tbody.empty();
                if (!docs.length) {
                    tbody.append(`
                        <tr>
                            <td colspan="24" class="text-center text-muted">
                                No records found.
                            </td>
                        </tr>
                    `);
                    return;
                }
                docs.forEach(doc => {
                    tbody.append(`
                        <tr>
                            <td style="display:none;"> ${doc.searchCode || ''} </td>
                            <td>${doc.v_TYPE || ''}</td>
                            <td>${doc.docTypeName || ''}</td>
                            <td>${doc.v_NO || ''}</td>
                            <td>${doc.v_DATE || ''}</td>
                            <td>${doc.mrN_NO || ''}</td>
                            <td>${doc.mrN_TYPE || ''}</td>
                            <td>${doc.mrnDate || ''}</td>
                            <td>${doc.partyName || ''}</td>
                            <td>${doc.billNo || ''}</td>
                            <td>${doc.billDate || ''}</td>
                            <td>${doc.transportName || ''}</td>
                            <td>${doc.truckNo || ''}</td>
                            <td>${doc.inV_QTY || ''}</td>
                            <td>${doc.recD_QTY || ''}</td>
                            <td>${doc.shorT_QTY || ''}</td>
                            <td>${doc.remarks || ''}</td>
                            <td>${doc.deducT_AMT || ''}</td>
                            <td>${doc.deducT_NARR || ''}</td>
                            <td>${doc.puR_TYPE || ''}</td>
                            <td>${doc.wastE_WGT || ''}</td>
                            <td class="action-col">
                                <button class="act-btn edit btn-edit" title="Edit" style="cursor:pointer;" data-vtype="${doc.v_TYPE}" data-vno="${doc.v_NO}" onclick="editEntry(this)"> <i class="fa fa-edit"></i> </button>
                                <button class="act-btn view btn-view" title="View" style="cursor:pointer;" data-vtype="${doc.v_TYPE}" data-vno="${doc.v_NO}" onclick="viewEntry(this)"> <i class="fa fa-eye"></i> </button>
                                <button class="act-btn delete btn-delete" title="Delete" style="cursor:pointer;" data-vtype="${doc.v_TYPE}" data-vno="${doc.v_NO}" onclick="deleteDocStage(this)"> <i class="fa fa-trash"></i> </button>
                            </td>
                        </tr>
                    `);
                });
            }
        });
    // First Load
    incommingQCPagination.load();
    // Search
    $('#searchBox').keyup(function () {
        incommingQCPagination.load();
        });
    });
    // Edit
    function editEntry(el) {
        const vType = $(el).data("vtype");
    const vNo = $(el).data("vno");
    window.location.href = `/IncommingQC/Index?vType=${encodeURIComponent(vType)}&vNo=${encodeURIComponent(vNo)}`;
    }
    // View
    function viewEntry(el) {
        const vType = $(el).data("vtype");
    const vNo = $(el).data("vno");
    window.location.href = `/IncommingQC/Index?vType=${encodeURIComponent(vType)}&vNo=${encodeURIComponent(vNo)}&mode=view`;
    }
    // Delete
    function deleteDocStage(el) {
        const vType = $(el).data("vtype");
        const code = $(el).data("vno");
        deleteRecordbytype("IncommingQCList", code, vType, {
        action: "Delete",
        text: "This will permanently delete the Transit entry.",
        successCallback: function () {
           if (incommingQCPagination) {
               incommingQCPagination.load();
                }
            }
        });
    }
    // Change Page Size
    function changeRowsPerPage() {
        incommingQCPagination.setPageSize(
            parseInt($('#pageSizeSelect').val())
        );
    incommingQCPagination.load();
    }