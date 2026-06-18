function showToast(message, options = {}) {
    const defaults = {
        type: "success", // success, error, warning, info
        duration: 2500,
        icon: "",
        bg: "",
        color: "#fff"
    };

    const settings = { ...defaults, ...options };

    // Auto icon and bg if not provided
    if (!settings.icon) {
        switch (settings.type) {
            case "success":
                settings.icon = "✅";
                settings.bg = settings.bg || "#28a745";
                break;
            case "error":
                settings.icon = "❌";
                settings.bg = settings.bg || "#dc2626";
                break;
            case "warning":
                settings.icon = "⚠️";
                settings.bg = settings.bg || "#f59e0b";
                break;
            case "info":
                settings.icon = "💡";
                settings.bg = settings.bg || "#2563eb";
                break;
        }
    }

    // Create toast element
    const container = $("#erp-toast-container");
    const toast = $(`
        <div class="erp-toast" style="background:${settings.bg}; color:${settings.color};">
            <i>${settings.icon}</i><span>${message}</span>
        </div>
    `);

    container.append(toast);

    // Animate in
    setTimeout(() => toast.addClass("show"), 50);

    // Auto remove after duration
    setTimeout(() => {
        toast.removeClass("show");
        setTimeout(() => toast.remove(), 900);
    }, settings.duration);
}


function setInvalid($el, message) {
    $el.addClass('is-invalid').focus();
    showToast(message, { type: "warning" });
}

function clearInvalid($el) {
    $el.removeClass('is-invalid');
}

// Auto clear validation on user interaction
$(document).on('input change', '.is-invalid', function () {
    clearInvalid($(this));
});
$(document).on('change', 'select', function () {
    clearInvalid($(this));
});

//Center toaster


//showToast("Row added successfully!", { type: "success" });
//showToast("Cannot delete the last row!", { type: "error" });
//showToast("Low stock warning!", { type: "warning" });
//showToast("Custom info message", { type: "info" });



