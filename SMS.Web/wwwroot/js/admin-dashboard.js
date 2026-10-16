(() => {
    'use strict';
    if (typeof Chart === 'undefined') return;

    const navy = '#36566f';
    const gold = '#c6a34e';
    const format = new Intl.NumberFormat();
    const animation = window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : { duration: 650 };
    const tooltip = {
        backgroundColor: '#1a2939', padding: 12, cornerRadius: 8,
        callbacks: { label: context => `${context.label}: ${format.format(context.raw)}` }
    };
    const read = canvas => ({
        labels: JSON.parse(canvas.dataset.labels),
        values: JSON.parse(canvas.dataset.values)
    });

    const enrolment = document.getElementById('admin-enrolment-chart');
    if (enrolment) {
        const { labels, values } = read(enrolment);
        new Chart(enrolment, {
            type: 'bar',
            data: { labels, datasets: [{ label: 'Active students', data: values,
                backgroundColor: labels.map((_, index) => index === labels.length - 1 ? gold : navy),
                borderRadius: 5, maxBarThickness: 22 }] },
            options: {
                indexAxis: 'y', responsive: true, maintainAspectRatio: false, animation,
                plugins: { legend: { display: false }, tooltip },
                scales: {
                    x: { beginAtZero: true, border: { display: false },
                        grid: { color: '#edf1f5' }, ticks: { precision: 0, color: '#788391' },
                        title: { display: true, text: 'Active students', color: '#788391' } },
                    y: { border: { display: false }, grid: { display: false },
                        ticks: { color: '#526477', autoSkip: false } }
                }
            }
        });
    }

    for (const id of ['admin-placement-chart', 'admin-teacher-chart']) {
        const canvas = document.getElementById(id);
        if (!canvas) continue;
        const { labels, values } = read(canvas);
        new Chart(canvas, {
            type: 'doughnut',
            data: { labels, datasets: [{ data: values, backgroundColor: [navy, gold],
                borderColor: '#ffffff', borderWidth: 3, hoverOffset: 4 }] },
            options: { responsive: true, maintainAspectRatio: false, cutout: '76%', animation,
                plugins: { legend: { display: false }, tooltip } }
        });
    }
})();
