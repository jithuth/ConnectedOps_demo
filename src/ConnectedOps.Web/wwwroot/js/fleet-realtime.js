/**
 * ConnectedOps Live Telemetry & Real-Time Fleet Streaming (SignalR)
 */
(function () {
    'use strict';

    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    if (typeof signalR === 'undefined') {
        console.warn('[ConnectedOps] SignalR client library not loaded. Live streaming inactive.');
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/fleet")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    const tenantId = getCookie('ConnectedOps.ActiveTenantId');

    function updateConnectionStatus(isConnected, text) {
        const indicator = document.getElementById('signalr-status-indicator');
        if (indicator) {
            if (isConnected) {
                indicator.className = 'badge bg-success-subtle text-success border border-success-subtle d-inline-flex align-items-center gap-1';
                indicator.innerHTML = '<span class="spinner-grow spinner-grow-sm" style="width: 6px; height: 6px;" role="status"></span> Live Stream';
            } else {
                indicator.className = 'badge bg-secondary-subtle text-secondary border d-inline-flex align-items-center gap-1';
                indicator.innerHTML = `<i class="bi bi-cloud-slash"></i> ${text || 'Disconnected'}`;
            }
        }
    }

    // Handlers
    connection.on("ReceiveEvBatteryTelemetry", function (data) {
        console.log("[SignalR] Received EV Battery Telemetry:", data);
        const socEl = document.getElementById(`soc-val-${data.vehicleId}`);
        if (socEl) {
            socEl.textContent = `${data.stateOfChargePercent.toFixed(1)}%`;
        }
        const socBar = document.getElementById(`soc-bar-${data.vehicleId}`);
        if (socBar) {
            socBar.style.width = `${data.stateOfChargePercent}%`;
        }
    });

    connection.on("ReceivePredictiveAlert", function (alert) {
        console.log("[SignalR] Received Predictive Alert:", alert);
        showToastAlert(
            `AI Predictive Alert: ${alert.vehicleName}`,
            `${alert.componentTitle} (Probability: ${alert.failureProbability.toFixed(0)}%, RUL: ${alert.estimatedRulDays} days)`,
            'danger'
        );
    });

    connection.on("ReceiveRouteDispatchUpdate", function (dispatch) {
        console.log("[SignalR] Route Dispatched:", dispatch);
        showToastAlert(
            `Route Dispatched: ${dispatch.runNumber}`,
            dispatch.message,
            'success'
        );
    });

    connection.on("ReceiveChargingSessionUpdate", function (session) {
        console.log("[SignalR] Charging Session Update:", session);
    });

    function showToastAlert(title, message, variant) {
        const container = document.getElementById('fleet-toast-container');
        if (!container) return;

        const toastId = 'toast-' + Math.random().toString(36).substring(2, 9);
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-${variant || 'primary'} border-0 shadow-lg mb-2" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong class="d-block mb-1"><i class="bi bi-bell-fill me-1"></i> ${title}</strong>
                        <div class="small">${message}</div>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;
        container.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        if (toastEl && typeof bootstrap !== 'undefined') {
            const toast = new bootstrap.Toast(toastEl, { delay: 6000 });
            toast.show();
        }
    }

    async function start() {
        try {
            await connection.start();
            console.log("[ConnectedOps] Connected to FleetHub SignalR.");
            updateConnectionStatus(true);

            if (tenantId) {
                await connection.invoke("JoinTenantGroup", tenantId);
            }
        } catch (err) {
            console.warn("[ConnectedOps] SignalR connection failed:", err);
            updateConnectionStatus(false, 'Connecting...');
            setTimeout(start, 5000);
        }
    }

    connection.onreconnecting(() => updateConnectionStatus(false, 'Reconnecting...'));
    connection.onreconnected(() => {
        updateConnectionStatus(true);
        if (tenantId) {
            connection.invoke("JoinTenantGroup", tenantId);
        }
    });
    connection.onclose(() => updateConnectionStatus(false, 'Closed'));

    document.addEventListener('DOMContentLoaded', start);
})();
