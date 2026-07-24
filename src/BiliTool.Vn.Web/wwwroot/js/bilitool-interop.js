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

        const ages = labels.map(label => Number.parseFloat(String(label).replace(',', '.')) || 0);
        const toPoints = data => data.map((value, index) => value == null ? null : ({ x: ages[index], y: value }));
        const extendSinglePoint = (points, hours) => {
            const valid = points.filter(Boolean);
            if (valid.length !== 1) return points;
            const point = valid[0];
            const span = Math.max(6, Math.min(24, hours * 0.25 || 6));
            return [{ x: Math.max(0, point.x - span), y: point.y }, { x: point.x + span, y: point.y }];
        };
        const babyPoints = toPoints(values);
        const photoPoints = extendSinglePoint(toPoints(nguongChieuDen), ages[0] || 24);
        const exchangePoints = extendSinglePoint(toPoints(nguongThayCuuMau), ages[0] || 24);

        const endpointLabels = {
            id: 'bilitoolEndpointLabels',
            afterDatasetsDraw(chart) {
                const { ctx: chartCtx, chartArea } = chart;
                chart.data.datasets.forEach((dataset, datasetIndex) => {
                    const meta = chart.getDatasetMeta(datasetIndex);
                    if (meta.hidden || !meta.data.length) return;
                    const point = meta.data[meta.data.length - 1];
                    if (!point || point.x > chartArea.right + 2) return;
                    chartCtx.save();
                    chartCtx.font = '700 11px "Be Vietnam Pro", sans-serif';
                    chartCtx.fillStyle = dataset.borderColor;
                    chartCtx.textAlign = 'right';
                    const labelY = dataset.shortLabel === 'CHỈ SỐ BÉ'
                        ? Math.min(chartArea.bottom - 6, point.y + 20)
                        : Math.max(chartArea.top + 14, point.y - 9);
                    chartCtx.fillText(dataset.shortLabel, chartArea.right - 8, labelY);
                    chartCtx.restore();
                });
            }
        };

        window.bilitoolChart = new Chart(ctx, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: 'Ngưỡng chiếu đèn',
                        shortLabel: 'CHIẾU ĐÈN',
                        data: photoPoints,
                        borderColor: '#d89218',
                        backgroundColor: 'rgba(245, 183, 66, .12)',
                        borderDash: [8, 5],
                        borderWidth: 2.5,
                        pointBackgroundColor: '#ffffff',
                        pointBorderColor: '#d89218',
                        pointBorderWidth: 2,
                        pointRadius: photoPoints.length <= 2 ? 4 : 2,
                        pointHoverRadius: 6,
                        fill: '+1',
                        tension: 0.25,
                        order: 2
                    },
                    {
                        label: 'Ngưỡng thay máu',
                        shortLabel: 'THAY MÁU',
                        data: exchangePoints,
                        borderColor: '#c8493d',
                        backgroundColor: 'rgba(200, 73, 61, .08)',
                        borderDash: [8, 5],
                        borderWidth: 2.5,
                        pointBackgroundColor: '#ffffff',
                        pointBorderColor: '#c8493d',
                        pointBorderWidth: 2,
                        pointRadius: exchangePoints.length <= 2 ? 4 : 2,
                        pointHoverRadius: 6,
                        fill: 'end',
                        tension: 0.25,
                        order: 3
                    },
                    {
                        label: `Chỉ số của bé (${unit})`,
                        shortLabel: 'CHỈ SỐ BÉ',
                        data: babyPoints,
                        borderColor: '#087f8c',
                        backgroundColor: 'rgba(8, 127, 140, .14)',
                        borderWidth: 4,
                        pointBackgroundColor: '#ffffff',
                        pointBorderColor: '#087f8c',
                        pointBorderWidth: 4,
                        pointRadius: 7,
                        pointHoverRadius: 10,
                        fill: false,
                        tension: 0.32,
                        order: 1
                    }
                ]
            },
            plugins: [endpointLabels],
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                animation: { duration: 650, easing: 'easeOutQuart' },
                layout: { padding: { top: 12, right: 14, bottom: 2, left: 4 } },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'start',
                        labels: {
                            color: '#425466',
                            font: { family: 'Be Vietnam Pro', size: 11, weight: '700' },
                            usePointStyle: true,
                            pointStyle: 'line',
                            boxWidth: 34,
                            boxHeight: 8,
                            padding: 18
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(7, 42, 57, .96)',
                        padding: 13,
                        cornerRadius: 10,
                        titleFont: { family: 'Be Vietnam Pro', weight: 'bold' },
                        bodyFont: { family: 'Be Vietnam Pro' },
                        displayColors: true,
                        callbacks: {
                            title: items => `${items[0].parsed.x.toFixed(0)} giờ tuổi`,
                            label: item => `${item.dataset.label}: ${item.parsed.y.toFixed(decimals)} ${unit}`
                        }
                    }
                },
                scales: {
                    x: {
                        type: 'linear',
                        title: {
                            display: true,
                            text: 'Tuổi (giờ)',
                            color: '#526575',
                            font: { family: 'Be Vietnam Pro', weight: '700' }
                        },
                        ticks: { color: '#738495', font: { family: 'Be Vietnam Pro', size: 11 }, maxTicksLimit: 8 },
                        grid: { color: 'rgba(107, 132, 150, .10)', drawBorder: false }
                    },
                    y: {
                        title: {
                            display: true,
                            text: `Bilirubin (${unit})`,
                            color: '#526575',
                            font: { family: 'Be Vietnam Pro', weight: '700' }
                        },
                        ticks: { color: '#738495', font: { family: 'Be Vietnam Pro', size: 11 }, padding: 8 },
                        grid: { color: 'rgba(107, 132, 150, .12)', drawBorder: false },
                        beginAtZero: true,
                        grace: '12%'
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

    scrollToTop: function () {
        window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
    },

    focusCalculatorForm: function () {
        requestAnimationFrame(() => {
            window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
            const firstInput = document.querySelector('.panel-nhap-lieu input:not([type="hidden"]), .panel-nhap-lieu select');
            firstInput?.focus({ preventScroll: true });
        });
    },

    // In trang
    printPage: function () {
        window.print();
    },

    exportKetQuaPdf: async function () {
        const source = document.querySelector('.ket-qua-card');
        const exportButton = document.querySelector('.action-buttons button:last-child');
        if (!source || typeof html2canvas !== 'function' || !window.jspdf?.jsPDF) {
            console.error('Không thể khởi tạo báo cáo PDF.');
            return false;
        }

        const language = document.documentElement.lang || 'vi';
        const labels = language.startsWith('en')
            ? { title: 'Neonatal bilirubin clinical report', subtitle: 'Clinical decision support - AAP 2022 and NICE CG98', generated: 'Generated', disclaimer: 'Clinical decision support only. This report does not replace physician assessment or local hospital protocols.', loading: 'Creating PDF...' }
            : language.startsWith('fr')
                ? { title: 'Rapport clinique de bilirubine néonatale', subtitle: 'Aide à la décision clinique - AAP 2022 et NICE CG98', generated: 'Généré', disclaimer: 'Outil d’aide à la décision clinique. Ce rapport ne remplace pas l’évaluation médicale ni les protocoles locaux.', loading: 'Création du PDF...' }
                : { title: 'Báo cáo lâm sàng bilirubin sơ sinh', subtitle: 'Hỗ trợ quyết định lâm sàng - AAP 2022 và NICE CG98', generated: 'Thời điểm xuất', disclaimer: 'Công cụ hỗ trợ quyết định lâm sàng. Báo cáo không thay thế đánh giá của bác sĩ hoặc quy trình chuyên môn tại cơ sở điều trị.', loading: 'Đang tạo PDF...' };

        const now = new Date();
        const timestamp = new Intl.DateTimeFormat(language, {
            year: 'numeric', month: '2-digit', day: '2-digit',
            hour: '2-digit', minute: '2-digit', second: '2-digit'
        }).format(now);
        const filenameTimestamp = [
            now.getFullYear(),
            String(now.getMonth() + 1).padStart(2, '0'),
            String(now.getDate()).padStart(2, '0'),
            String(now.getHours()).padStart(2, '0'),
            String(now.getMinutes()).padStart(2, '0')
        ].join('');

        const report = document.createElement('div');
        report.className = 'pdf-report-shell';
        report.setAttribute('aria-hidden', 'true');
        report.innerHTML = `
            <header class="pdf-report-brand">
                <div class="pdf-report-brand__identity">
                    <img src="/icons/bilitool-icon.svg" alt="" width="54" height="54">
                    <div>
                        <strong>BiliTool.Vn</strong>
                        <span>AAP 2022 + NICE CG98</span>
                    </div>
                </div>
                <div class="pdf-report-brand__meta">
                    <span>${labels.generated}</span>
                    <strong>${timestamp}</strong>
                </div>
            </header>
            <section class="pdf-report-heading">
                <span>CLINICAL REPORT</span>
                <h1>${labels.title}</h1>
                <p>${labels.subtitle}</p>
            </section>
            <main class="pdf-report-content"></main>
            <footer class="pdf-report-footer">
                <div class="pdf-report-footer__mark">
                    <img src="/icons/bilitool-icon.svg" alt="" width="28" height="28">
                    <span><strong>BiliTool.Vn</strong><small>bilitool.vn</small></span>
                </div>
                <p>${labels.disclaimer}</p>
                <span class="pdf-report-footer__protocol">AAP 2022 · NICE CG98</span>
            </footer>`;

        const loadingOverlay = document.createElement('div');
        loadingOverlay.className = 'pdf-export-loading';
        loadingOverlay.setAttribute('role', 'status');
        loadingOverlay.setAttribute('data-html2canvas-ignore', 'true');
        loadingOverlay.innerHTML = `
            <img src="/icons/bilitool-icon.svg" alt="" width="58" height="58">
            <strong>${labels.loading}</strong>
            <span class="pdf-export-loading__spinner" aria-hidden="true"></span>`;

        const reportCard = source.cloneNode(true);
        reportCard.classList.add('pdf-export-card');
        reportCard.removeAttribute('style');
        reportCard.querySelectorAll('.action-buttons').forEach(element => element.remove());
        reportCard.querySelectorAll('[id]').forEach(element => element.removeAttribute('id'));
        reportCard.querySelectorAll('svg filter').forEach(element => element.remove());
        reportCard.querySelectorAll('svg [filter]').forEach(element => element.removeAttribute('filter'));
        reportCard.querySelectorAll('*').forEach(element => {
            element.style.animation = 'none';
            element.style.transition = 'none';
        });
        report.querySelector('.pdf-report-content').appendChild(reportCard);
        document.body.appendChild(report);
        document.body.appendChild(loadingOverlay);
        const ignoredSiblings = Array.from(document.body.children)
            .filter(element => element !== report && element !== loadingOverlay)
            .map(element => ({ element, previous: element.getAttribute('data-html2canvas-ignore') }));
        ignoredSiblings.forEach(({ element }) => element.setAttribute('data-html2canvas-ignore', 'true'));

        const originalButtonHtml = exportButton?.innerHTML;
        if (exportButton) {
            exportButton.disabled = true;
            exportButton.innerHTML = `<i class="fas fa-spinner fa-spin"></i> ${labels.loading}`;
        }

        const filename = `BiliTool-BaoCao-Bilirubin-${filenameTimestamp}.pdf`;

        try {
            await Promise.all(Array.from(report.querySelectorAll('img')).map(image => image.complete
                ? Promise.resolve()
                : new Promise(resolve => {
                    image.addEventListener('load', resolve, { once: true });
                    image.addEventListener('error', resolve, { once: true });
                })));
            await document.fonts?.ready;
            await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
            const reportTop = report.getBoundingClientRect().top;
            const protectedRanges = Array.from(report.querySelectorAll(
                '.bili-nomogram-container, .khuyen-nghi-box, .nice-alert, .nice-thresholds-box, .nice-repeat-box, .nice-references, .khoang-cach-grid, .pdf-report-footer'
            )).map(element => {
                const rect = element.getBoundingClientRect();
                return {
                    top: rect.top - reportTop,
                    bottom: rect.bottom - reportTop
                };
            });
            const canvas = await html2canvas(report, {
                scale: 2,
                useCORS: true,
                allowTaint: false,
                backgroundColor: '#ffffff',
                logging: false,
                width: report.scrollWidth,
                height: report.scrollHeight,
                windowWidth: 1440
            });
            const pdf = new window.jspdf.jsPDF({
                unit: 'mm',
                format: 'a4',
                orientation: 'portrait',
                compress: true
            });
            const pageWidthMm = 210;
            const pageHeightMm = 297;
            const marginLeftMm = 8;
            const marginTopMm = 7;
            const marginBottomMm = 9;
            const contentWidthMm = pageWidthMm - (marginLeftMm * 2);
            const contentHeightMm = pageHeightMm - marginTopMm - marginBottomMm;
            const pageSliceHeightPx = Math.max(1, Math.floor(canvas.width * contentHeightMm / contentWidthMm));
            const canvasScaleY = canvas.height / report.scrollHeight;
            let offsetY = 0;
            let pageIndex = 0;

            while (offsetY < canvas.height) {
                let sliceHeightPx = Math.min(pageSliceHeightPx, canvas.height - offsetY);
                if (offsetY + sliceHeightPx < canvas.height) {
                    const proposedCutCss = (offsetY + sliceHeightPx) / canvasScaleY;
                    const crossingRange = protectedRanges.find(range => range.top < proposedCutCss && range.bottom > proposedCutCss);
                    if (crossingRange) {
                        const protectedCutPx = Math.floor((crossingRange.top - 4) * canvasScaleY) - offsetY;
                        if (protectedCutPx >= pageSliceHeightPx * 0.42) {
                            sliceHeightPx = protectedCutPx;
                        }
                    }
                }
                const pageCanvas = document.createElement('canvas');
                pageCanvas.width = canvas.width;
                pageCanvas.height = sliceHeightPx;
                const context = pageCanvas.getContext('2d');
                context.fillStyle = '#ffffff';
                context.fillRect(0, 0, pageCanvas.width, pageCanvas.height);
                context.drawImage(canvas, 0, offsetY, canvas.width, sliceHeightPx, 0, 0, canvas.width, sliceHeightPx);

                if (pageIndex > 0) {
                    pdf.addPage('a4', 'portrait');
                }
                const sliceHeightMm = sliceHeightPx * contentWidthMm / canvas.width;
                pdf.addImage(pageCanvas.toDataURL('image/jpeg', 0.97), 'JPEG', marginLeftMm, marginTopMm, contentWidthMm, sliceHeightMm, undefined, 'FAST');
                offsetY += sliceHeightPx;
                pageIndex += 1;
            }

            const pdfBlob = pdf.output('blob');
            const downloadUrl = URL.createObjectURL(pdfBlob);
            const downloadLink = document.createElement('a');
            downloadLink.href = downloadUrl;
            downloadLink.download = filename;
            document.body.appendChild(downloadLink);
            downloadLink.click();
            downloadLink.remove();
            setTimeout(() => URL.revokeObjectURL(downloadUrl), 1000);
            return true;
        } catch (error) {
            console.error('Lỗi xuất PDF:', error);
            return false;
        } finally {
            report.remove();
            loadingOverlay.remove();
            ignoredSiblings.forEach(({ element, previous }) => {
                if (previous === null) {
                    element.removeAttribute('data-html2canvas-ignore');
                } else {
                    element.setAttribute('data-html2canvas-ignore', previous);
                }
            });
            if (exportButton) {
                exportButton.disabled = false;
                exportButton.innerHTML = originalButtonHtml;
            }
        }
    }
};
