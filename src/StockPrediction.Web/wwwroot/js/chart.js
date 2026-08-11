// Chart rendering function
function renderPriceChart(labels, prices, lowerBounds, upperBounds) {
    const ctx = document.getElementById('priceChart').getContext('2d');

    // Destroy existing chart if it exists
    if (window.priceChartInstance) {
        window.priceChartInstance.destroy();
    }

    window.priceChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Predicted Price',
                    data: prices,
                    borderColor: 'rgba(0, 123, 255, 1)',
                    backgroundColor: 'rgba(0, 123, 255, 0.1)',
                    fill: true,
                    tension: 0.3,
                    pointRadius: 6,
                    pointBackgroundColor: 'rgba(0, 123, 255, 1)'
                },
                {
                    label: 'Upper Bound (95% CI)',
                    data: upperBounds,
                    borderColor: 'rgba(40, 167, 69, 0.3)',
                    backgroundColor: 'rgba(40, 167, 69, 0.05)',
                    fill: '+1',
                    tension: 0.3,
                    pointRadius: 0,
                    borderDash: [5, 5]
                },
                {
                    label: 'Lower Bound (95% CI)',
                    data: lowerBounds,
                    borderColor: 'rgba(220, 53, 69, 0.3)',
                    backgroundColor: 'rgba(220, 53, 69, 0.05)',
                    fill: '+1',
                    tension: 0.3,
                    pointRadius: 0,
                    borderDash: [5, 5]
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
                        boxWidth: 12,
                        padding: 20
                    }
                },
                title: {
                    display: true,
                    text: '5-Day Price Forecast with Confidence Intervals',
                    font: {
                        size: 16
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: false,
                    title: {
                        display: true,
                        text: 'Price ($)',
                        font: {
                            weight: 'bold'
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date',
                        font: {
                            weight: 'bold'
                        }
                    }
                }
            }
        }
    });
}

// File download function for CSV export
function downloadFile(base64Data, fileName) {
    const link = document.createElement('a');
    link.href = 'data:text/csv;base64,' + base64Data;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}