// =========================================================================
// CAFÉ MANAGEMENT SYSTEM - CLIENT SIDE SCRIPTS
// Tự động đóng thông báo (Alerts) sau 3 - 5 giây
// =========================================================================

document.addEventListener('DOMContentLoaded', function () {
    initAutoDismissAlerts();
});

/**
 * Tự động ẩn các thông báo alert sau khoảng 4 giây (3-5s).
 * Tạm dừng khi rê chuột vào (mouseenter) và tiếp tục đếm khi rời chuột (mouseleave).
 */
function initAutoDismissAlerts() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(function (alert) {
        setupAlertAutoDismiss(alert, 4000);
    });
}

function setupAlertAutoDismiss(alert, duration) {
    if (!alert || alert.dataset.autoDismissInitialized) return;
    alert.dataset.autoDismissInitialized = "true";

    let timer = null;

    function startDismissTimer(ms) {
        clearTimeout(timer);
        timer = setTimeout(function () {
            dismissAlert(alert);
        }, ms);
    }

    // Bắt đầu đếm ngược mặc định (4000ms = 4s)
    startDismissTimer(duration || 4000);

    // Tạm dừng khi người dùng rê chuột vào để đọc thông báo
    alert.addEventListener('mouseenter', function () {
        clearTimeout(timer);
    });

    // Rời chuột: đếm thêm 2s rồi ẩn
    alert.addEventListener('mouseleave', function () {
        startDismissTimer(2000);
    });
}

function dismissAlert(alert) {
    if (!alert || !alert.parentElement) return;

    // Sử dụng Bootstrap 5 Alert API nếu có
    if (typeof bootstrap !== 'undefined' && bootstrap.Alert) {
        try {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
            return;
        } catch (e) {
            // fallback
        }
    }

    // Fallback animation
    alert.classList.remove('show');
    alert.style.transition = 'opacity 0.4s ease, transform 0.4s ease, max-height 0.4s ease, margin 0.4s ease, padding 0.4s ease';
    alert.style.opacity = '0';
    alert.style.transform = 'translateY(-10px)';
    alert.style.maxHeight = '0';
    alert.style.margin = '0';
    alert.style.padding = '0';
    alert.style.overflow = 'hidden';

    setTimeout(function () {
        if (alert.parentElement) {
            alert.remove();
        }
    }, 450);
}

// Cung cấp hàm toàn cục nếu cần gọi động từ AJAX/JS
window.autoDismissAlert = setupAlertAutoDismiss;
