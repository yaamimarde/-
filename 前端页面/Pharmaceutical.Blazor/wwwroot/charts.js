window.renderTopMovingChart = (labels, values) => {
    const canvas = document.getElementById('topMovingChart');
    if (!canvas) return;

    if (window._topMovingChart) {
        window._topMovingChart.destroy();
    }

    window._topMovingChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                label: '30天出库量',
                data: values,
                backgroundColor: 'rgba(37, 99, 235, 0.7)',
                borderColor: 'rgba(37, 99, 235, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
        }
    });
};

window.renderKpiChart = (labels, values) => {
    const canvas = document.getElementById('kpiChart');
    if (!canvas) return;

    if (window._kpiChart) {
        window._kpiChart.destroy();
    }

    window._kpiChart = new Chart(canvas, {
        type: 'doughnut',
        data: {
            labels,
            datasets: [{
                data: values,
                backgroundColor: ['#ef4444', '#f59e0b', '#22c55e', '#3b82f6']
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom' } }
        }
    });
};
