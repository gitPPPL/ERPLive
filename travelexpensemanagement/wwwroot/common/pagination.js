var Pagination = Pagination || {};

Pagination.create = function (config) {

    let state = {
        currentPage: 1,
        pageSize: config.pageSize || 10,
        totalCount: 0
    };
    function loadData() {
        config.loader({
            pageNumber: state.currentPage,
            pageSize: state.pageSize,
            callback: function (res) {

                state.totalCount = res.totalCount || 0;
                config.render(res.data || []);
                renderPagination();
                renderInfo();
            }
        });
    }

    function renderPagination() {

        const totalPages = Math.ceil(state.totalCount / state.pageSize) || 1;

        let html = '';
        let maxVisible = 5;

        let startPage = Math.max(1, state.currentPage - Math.floor(maxVisible / 2));
        let endPage = startPage + maxVisible - 1;

        if (endPage > totalPages) {
            endPage = totalPages;
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        // First + dots
        if (startPage > 1) {
            html += `<span class="page-number" onclick="Pagination.goToPage(1)">1</span>`;
            if (startPage > 2) html += `<span class="dots">...</span>`;
        }

        // Middle
        for (let i = startPage; i <= endPage; i++) {
            html += `<span class="page-number ${i === state.currentPage ? 'active' : ''}"
                        onclick="Pagination.goToPage(${i})">${i}</span>`;
        }

        // Last + dots
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) html += `<span class="dots">...</span>`;
            html += `<span class="page-number" onclick="Pagination.goToPage(${totalPages})">${totalPages}</span>`;
        }

        $(config.paginationContainer).html(html);

        // Prev / Next
        $('#prevBtn').prop('disabled', state.currentPage === 1);
        $('#nextBtn').prop('disabled', state.currentPage === totalPages);
    }

    function renderInfo() {
        let start = (state.currentPage - 1) * state.pageSize + 1;
        let end = Math.min(state.currentPage * state.pageSize, state.totalCount);

        if (state.totalCount === 0) {
            start = 0;
            end = 0;
        }

        $(config.infoContainer).text(`Results: ${start} - ${end} of ${state.totalCount}`);
    }

    // Global handlers
    Pagination.goToPage = function (page) {
        state.currentPage = page;
        loadData();
    };

    Pagination.prev = function () {
        if (state.currentPage > 1) {
            state.currentPage--;
            loadData();
        }
    };

    Pagination.next = function () {
        const totalPages = Math.ceil(state.totalCount / state.pageSize);
        if (state.currentPage < totalPages) {
            state.currentPage++;
            loadData();
        }
    };

  

    return {
        load: loadData,
        setPageSize: function (size) {
            state.pageSize = size;
            state.currentPage = 1;
            loadData();
        }
    };
};