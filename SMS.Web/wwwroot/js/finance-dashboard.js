(() => {
    'use strict';
    if (typeof Chart === 'undefined') return;
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const read = (canvas, key) => JSON.parse(canvas.dataset[key]);
    const options = canvas => ({
        responsive: true,
        maintainAspectRatio: false,
        animation: reducedMotion ? false : { duration: 1200, easing: 'easeOutQuart' },
        animations: reducedMotion ? false : {
            y: { from: context => context.chart.scales.y.getPixelForValue(0) }
        },
        plugins: {
            legend: { position: 'bottom' },
            tooltip: { callbacks: { label: context => `${context.dataset.label}: ${canvas.dataset.currency} ${Number(context.raw).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` } }
        },
        scales: {
            x: { grid: { display: false } },
            y: { beginAtZero: true, title: { display: true, text: canvas.dataset.currency } }
        }
    });
    const doughnut = document.getElementById('finance-collections-chart');
    if (doughnut) {
        const values = read(doughnut, 'values');
        const total = values.reduce((sum, value) => sum + value, 0);
        const format = value => Number(value).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        const centreTotal = {
            id: 'financeCentreTotal',
            afterDraw(chart) {
                const { ctx, chartArea } = chart;
                if (!chartArea) return;
                const x = (chartArea.left + chartArea.right) / 2;
                const y = (chartArea.top + chartArea.bottom) / 2;
                const fontSize = Math.max(12, Math.min(20, chart.width / 22));
                ctx.save();
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = '#36566f';
                ctx.font = `600 ${fontSize}px sans-serif`;
                ctx.fillText(format(total), x, y - 10, (chartArea.right - chartArea.left) * .5);
                ctx.font = '12px sans-serif';
                ctx.fillText(`${doughnut.dataset.currency} collected`, x, y + 14);
                ctx.restore();
            }
        };
        new Chart(doughnut, {
            type: 'doughnut',
            data: { labels: read(doughnut, 'labels'), datasets: [{
                data: values,
                backgroundColor: ['#c6a34e', '#36566f', '#4f9179', '#8993a0', '#9563b5'],
                borderColor: '#ffffff', borderWidth: 3
            }] },
            plugins: [centreTotal],
            options: {
                responsive: true, maintainAspectRatio: false, cutout: '72%',
                animation: reducedMotion ? false : { duration: 1200, easing: 'easeOutQuart', animateRotate: true, animateScale: true },
                plugins: {
                    legend: { display: false },
                    tooltip: { callbacks: { label: context => `${context.label}: ${doughnut.dataset.currency} ${format(context.raw)} (${(Number(context.raw) / total * 100).toFixed(1)}%)` } }
                }
            }
        });
    }
    const trend = document.getElementById('finance-trend-chart');
    if (trend) {
        const methods = read(trend, 'methods');
        const reversed = read(trend, 'reversed');
        const colors = ['#c6a34e', '#36566f', '#4f9179', '#8993a0', '#9563b5'];
        const format = value => `${trend.dataset.currency} ${Number(value).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
        const settings = options(trend);
        settings.interaction = { mode: 'index', intersect: false };
        settings.scales.x.stacked = true;
        settings.scales.y.stacked = true;
        settings.plugins.tooltip.callbacks.footer = items => {
            if (!items.length) return '';
            const index = items[0].dataIndex;
            const total = methods.reduce((sum, method) => sum + Number(method.values[index]), 0);
            return [`Weekly total: ${format(total)}`, `Reversed: ${format(reversed[index])}`, `Net: ${format(total - reversed[index])}`];
        };
        new Chart(trend, {
            type: 'bar',
            data: { labels: read(trend, 'labels'), datasets: methods.map((method, index) => ({
                label: method.name, data: method.values,
                backgroundColor: colors[method.id - 1] || colors[index % colors.length],
                borderWidth: 0
            })) },
            options: settings
        });
    }
})();
