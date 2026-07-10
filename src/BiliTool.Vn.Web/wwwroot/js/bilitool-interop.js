// BiliTool.Vn - JavaScript Interop Helpers
window.bilitool = {
    // Sao chép văn bản vào clipboard
    copyToClipboard: async function (text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
            } else {
                // Fallback cho trình duyệt cũ hoặc HTTP
                const textarea = document.createElement('textarea');
                textarea.value = text;
                textarea.style.position = 'fixed';
                textarea.style.opacity = '0';
                document.body.appendChild(textarea);
                textarea.focus();
                textarea.select();
                document.execCommand('copy');
                document.body.removeChild(textarea);
            }
            return true;
        } catch (err) {
            console.error('Lỗi sao chép:', err);
            return false;
        }
    },

    // Khởi tạo biểu đồ xu hướng bilirubin
    initXuHuongChart: function (canvasId, labels, values, nguongChieuDen, nguongThayCuuMau, unitSuffix) {
        const unit = unitSuffix || 'mg/dL';
        const decimals = unit === 'μmol/L' ? 0 : 1;
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        // Hủy biểu đồ cũ nếu có
        if (window.bilitoolChart) {
            window.bilitoolChart.destroy();
        }

        window.bilitoolChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: `Bilirubin (${unit})`,
                        data: values,
                        borderColor: '#1a6b9a',
                        backgroundColor: 'rgba(26,107,154,0.1)',
                        borderWidth: 2.5,
                        pointBackgroundColor: '#1a6b9a',
                        pointRadius: 6,
                        pointHoverRadius: 8,
                        fill: true,
                        tension: 0.3
                    },
                    {
                        label: 'Ngưỡng chiếu đèn',
                        data: nguongChieuDen,
                        borderColor: '#f39c12',
                        borderDash: [6, 3],
                        borderWidth: 2,
                        pointRadius: 0,
                        fill: false
                    },
                    {
                        label: 'Ngưỡng thay máu',
                        data: nguongThayCuuMau,
                        borderColor: '#c0392b',
                        borderDash: [6, 3],
                        borderWidth: 2,
                        pointRadius: 0,
                        fill: false
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            font: { family: 'Be Vietnam Pro', size: 12 },
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        backgroundColor: '#1a2a3a',
                        titleFont: { family: 'Be Vietnam Pro', weight: 'bold' },
                        bodyFont: { family: 'Be Vietnam Pro' },
                        callbacks: {
                            label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y.toFixed(decimals)} ${unit}`
                        }
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: 'Tuổi (giờ)',
                            font: { family: 'Be Vietnam Pro', weight: 'bold' }
                        },
                        grid: { color: 'rgba(0,0,0,0.05)' }
                    },
                    y: {
                        title: {
                            display: true,
                            text: `Bilirubin (${unit})`,
                            font: { family: 'Be Vietnam Pro', weight: 'bold' }
                        },
                        grid: { color: 'rgba(0,0,0,0.05)' },
                        beginAtZero: false
                    }
                }
            }
        });
    },

    // Cập nhật biểu đồ với dữ liệu mới
    updateChart: function (labels, values) {
        if (!window.bilitoolChart) return;
        window.bilitoolChart.data.labels = labels;
        window.bilitoolChart.data.datasets[0].data = values;
        window.bilitoolChart.update('active');
    },

    // Toggle sidebar trên mobile
    toggleMobileSidebar: function () {
        const sidebar = document.querySelector('.sidebar');
        sidebar?.classList.toggle('mobile-open');
    },

    // Scroll mượt đến phần kết quả
    scrollToKetQua: function () {
        const el = document.querySelector('.ket-qua-card');
        el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    },

    // In trang
    printPage: function () {
        window.print();
    },

    exportKetQuaPdf: function () {
        var element = document.querySelector('.ket-qua-card');
        var opt = {
          margin:       0.5,
          filename:     'KetQuaBilirubin.pdf',
          image:        { type: 'jpeg', quality: 0.98 },
          html2canvas:  { scale: 2 },
          jsPDF:        { unit: 'in', format: 'letter', orientation: 'portrait' }
        };
        // Ẩn nút in và sao chép trước khi xuất
        var actions = element.querySelector('.action-buttons');
        if (actions) actions.style.display = 'none';

        html2pdf().from(element).set(opt).save().then(() => {
            if (actions) actions.style.display = 'flex';
        });
    }
};
