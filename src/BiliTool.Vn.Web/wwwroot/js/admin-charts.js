// window object for admin charts
window.adminCharts = {
    colors: {
        primary: '#4338ca',
        secondary: '#0ea5e9',
        success: '#10b981',
        warning: '#f59e0b',
        danger: '#ef4444',
        border: '#e2e8f0',
        text: '#64748b'
    },

    renderSparkline: function (canvasId, dataPoints, colorType) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        let color = this.colors.primary;
        if (colorType === 'success') color = this.colors.success;
        if (colorType === 'secondary') color = this.colors.secondary;

        let gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 60);
        gradient.addColorStop(0, color + '40');
        gradient.addColorStop(1, color + '00');

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: dataPoints.map((_, i) => i.toString()),
                datasets: [{ data: dataPoints, borderColor: color, borderWidth: 2, backgroundColor: gradient, fill: true, tension: 0.4, pointRadius: 0, pointHoverRadius: 0 }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { enabled: false } },
                scales: { x: { display: false }, y: { display: false, min: 0 } },
                animation: { duration: 1000, easing: 'easeOutQuart' }
            }
        });
    },

    renderComboChart: function (canvasId, labels, lineData, barData) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        let barGradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 300);
        barGradient.addColorStop(0, this.colors.secondary + 'CC');
        barGradient.addColorStop(1, this.colors.secondary + '22');

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { type: 'line', label: ' Người Dùng Mới', data: lineData, borderColor: this.colors.primary, backgroundColor: this.colors.primary, borderWidth: 2, tension: 0.4, yAxisID: 'y1' },
                    { type: 'bar', label: ' Lượt Tính Toán', data: barData, backgroundColor: barGradient, borderRadius: 4, yAxisID: 'y' }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'top', labels: { usePointStyle: true, boxWidth: 8 } },
                    tooltip: { backgroundColor: '#0f172a', titleFont: { size: 14 }, padding: 12 }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: { type: 'linear', display: true, position: 'left', grid: { borderDash: [4, 4], color: this.colors.border } },
                    y1: { type: 'linear', display: true, position: 'right', grid: { display: false } }
                }
            }
        });
    },

    renderDonutChart: function (canvasId, normal, phototherapy, exchange) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Bình thường', 'Cần chiếu đèn', 'Thay máu'],
                datasets: [{ data: [normal, phototherapy, exchange], backgroundColor: [this.colors.success, this.colors.warning, this.colors.danger], borderWidth: 0, hoverOffset: 4 }]
            },
            options: {
                responsive: true, maintainAspectRatio: false, cutout: '75%',
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 20, usePointStyle: true } },
                    tooltip: { callbacks: { label: function(c) { return ' ' + c.label + ': ' + c.parsed + '%'; } } }
                }
            }
        });
    },

    // Generic Pie/Donut chart from labels[] + data[] + optional colors[]
    renderPieChart: function (canvasId, labels, data, colors) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        var bgColors = colors || ['#4338ca', '#0ea5e9', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{ data: data, backgroundColor: bgColors.slice(0, labels.length), borderWidth: 2, borderColor: '#fff', hoverOffset: 6 }]
            },
            options: {
                responsive: true, maintainAspectRatio: false, cutout: '65%',
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 12, usePointStyle: true, font: { size: 11 } } },
                    tooltip: { callbacks: { label: function(c) { var total = c.dataset.data.reduce((a,b)=>a+b,0); var pct = total>0 ? Math.round(c.raw/total*100) : 0; return ' ' + c.label + ': ' + c.raw + ' (' + pct + '%)'; } } }
                }
            }
        });
    },

    // Horizontal bar chart (Top Hospitals, Specialty)
    renderHorizontalBar: function (canvasId, labels, data, color) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        var barColor = color || this.colors.primary;
        new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: [{ data: data, backgroundColor: barColor + 'CC', borderColor: barColor, borderWidth: 1, borderRadius: 4 }] },
            options: {
                indexAxis: 'y', responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { backgroundColor: '#0f172a' } },
                scales: { x: { grid: { color: '#e2e8f0' } }, y: { grid: { display: false } } }
            }
        });
    },

    // Histogram (vertical bar for Bilirubin distribution)
    renderHistogram: function (canvasId, labels, data, color) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        var barColor = color || this.colors.secondary;
        var gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 300);
        gradient.addColorStop(0, barColor + 'EE');
        gradient.addColorStop(1, barColor + '44');

        new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: [{ data: data, backgroundColor: gradient, borderColor: barColor, borderWidth: 1, borderRadius: 6 }] },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { backgroundColor: '#0f172a', padding: 10 } },
                scales: { x: { grid: { display: false } }, y: { grid: { color: '#e2e8f0', borderDash: [4, 4] }, beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    },

    // Area chart (Bilirubin rolling mean, XN per day)
    renderAreaChart: function (canvasId, labels, data, color, label) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        var lineColor = color || this.colors.success;
        var gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 300);
        gradient.addColorStop(0, lineColor + '55');
        gradient.addColorStop(1, lineColor + '00');

        new Chart(ctx, {
            type: 'line',
            data: { labels: labels, datasets: [{ label: label || 'Số lượng', data: data, borderColor: lineColor, backgroundColor: gradient, fill: true, tension: 0.4, borderWidth: 2, pointRadius: 2, pointHoverRadius: 5 }] },
            options: {
                responsive: true, maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: { legend: { display: false }, tooltip: { backgroundColor: '#0f172a', padding: 10 } },
                scales: { x: { grid: { display: false }, ticks: { maxTicksLimit: 10 } }, y: { grid: { color: '#e2e8f0', borderDash: [4, 4] }, beginAtZero: true } }
            }
        });
    },

    // Hourly heatmap bar (0-23h with color gradient by intensity)
    renderHourlyBar: function (canvasId, data24h) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        var existingChart = Chart.getChart(ctx);
        if (existingChart) existingChart.destroy();

        var labels = Array.from({ length: 24 }, (_, i) => i + 'h');
        var max = Math.max(...data24h, 1);
        var colors = data24h.map(v => {
            var r = v / max;
            if (r > 0.75) return '#ef4444CC';
            if (r > 0.4)  return '#f59e0bCC';
            return '#0ea5e9CC';
        });

        new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: [{ data: data24h, backgroundColor: colors, borderRadius: 3 }] },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { callbacks: { label: c => ' ' + c.raw + ' lần đo' } } },
                scales: { x: { grid: { display: false }, ticks: { font: { size: 10 } } }, y: { grid: { color: '#e2e8f0', borderDash: [4, 4] }, beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    }
};
