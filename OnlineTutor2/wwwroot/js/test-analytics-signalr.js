//@ts-nocheck

class TestAnalyticsSignalR {
    constructor(testId, testType) {
        this.testId = testId;
        this.testType = testType;
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
    }

    async start() {
        try {
            // Создаем подключение
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/testAnalytics")
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.elapsedMilliseconds < 60000) {
                            return Math.random() * 10000;
                        } else {
                            return null;
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Обработчики событий
            this.setupEventHandlers();

            // Подключаемся
            await this.connection.start();
            
            // Присоединяемся к группе
            await this.connection.invoke("JoinTestAnalytics", this.testId, this.testType);
            
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            console.log("✅ SignalR подключен. Test:", this.testId, "Type:", this.testType);
            this.showConnectionStatus('connected');
            
        } catch (err) {
            console.error("❌ Ошибка подключения SignalR:", err);
            this.showConnectionStatus('error');
            this.scheduleReconnect();
        }
    }

    setupEventHandlers() {
        // ✅ Событие: студент начал тест
        this.connection.on("TestStarted", (data) => {
            console.log("📝 Студент начал тест:", data);
            this.showNotification("Студент начал прохождение теста", "info");
            this.refreshAnalytics();
        });

        // ✅ Событие: студент продолжил тест
        this.connection.on("TestContinued", (data) => {
            console.log("🔄 Студент продолжил тест:", data);
            this.showNotification("Студент продолжил прохождение теста", "info");
            this.refreshAnalytics();
        });

        // ✅ Событие: студент завершил тест
        this.connection.on("TestCompleted", (data) => {
            console.log("✅ Студент завершил тест:", data);
            this.showNotification("Студент завершил тест!", "success");
            this.refreshAnalytics();
        });

        // Обработчик переподключения
        this.connection.onreconnecting((error) => {
            console.warn("⚠️ SignalR переподключается...", error);
            this.isConnected = false;
            this.showConnectionStatus('reconnecting');
        });

        this.connection.onreconnected((connectionId) => {
            console.log("✅ SignalR переподключен:", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.showConnectionStatus('connected');
            
            // Заново присоединяемся к группе
            this.connection.invoke("JoinTestAnalytics", this.testId, this.testType)
                .catch(err => console.error("Ошибка повторного присоединения к группе:", err));
        });

        this.connection.onclose((error) => {
            console.error("❌ SignalR отключен:", error);
            this.isConnected = false;
            this.showConnectionStatus('disconnected');
            this.scheduleReconnect();
        });
    }

    async refreshAnalytics() {
        // Используем существующий метод обновления данных
        if (window.testAnalyticsRealtime) {
            await window.testAnalyticsRealtime.update();
        } else {
            // Если нет realtime объекта, перезагружаем страницу
            console.log("Realtime объект не найден, перезагрузка страницы...");
            location.reload();
        }
    }

    showNotification(message, type = 'info') {
        const alertClass = type === 'success' ? 'alert-success' : 
            type === 'info' ? 'alert-info' : 
                type === 'warning' ? 'alert-warning' : 'alert-danger';
        
        const icon = type === 'success' ? 'fa-check-circle' : 
            type === 'info' ? 'fa-info-circle' : 
                type === 'warning' ? 'fa-exclamation-circle' : 'fa-times-circle';

        const notification = document.createElement('div');
        notification.className = 'signalr-notification';
        notification.innerHTML = `
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                <i class="fas ${icon} me-2"></i>
                <strong>${message}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        document.body.appendChild(notification);

        // Автоматически удаляем через 5 секунд
        setTimeout(() => {
            notification.remove();
        }, 5000);
    }

    showActivityIndicator() {
        const indicator = document.getElementById('signalr-activity-indicator');
        if (indicator) {
            indicator.classList.add('active');
            setTimeout(() => {
                indicator.classList.remove('active');
            }, 2000);
        }
    }

    showConnectionStatus(status) {
        const statusElement = document.getElementById('signalr-status');
        if (!statusElement) return;

        const statusConfig = {
            connected: { icon: 'fa-circle text-success', text: 'Онлайн', class: 'status-connected' },
            reconnecting: { icon: 'fa-sync fa-spin text-warning', text: 'Переподключение...', class: 'status-reconnecting' },
            disconnected: { icon: 'fa-circle text-danger', text: 'Оффлайн', class: 'status-disconnected' },
            error: { icon: 'fa-exclamation-triangle text-danger', text: 'Ошибка', class: 'status-error' }
        };

        const config = statusConfig[status];
        statusElement.className = `signalr-status ${config.class}`;
        statusElement.innerHTML = `
            <i class="fas ${config.icon} me-1"></i>
            <span>${config.text}</span>
        `;
    }

    scheduleReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error("❌ Превышено максимальное количество попыток переподключения");
            this.showNotification("Не удалось подключиться. Обновите страницу.", "error");
            return;
        }

        this.reconnectAttempts++;
        const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
        
        console.log(`🔄 Попытка переподключения ${this.reconnectAttempts}/${this.maxReconnectAttempts} через ${delay}мс`);
        
        setTimeout(() => {
            this.start();
        }, delay);
    }

    async stop() {
        if (this.connection) {
            try {
                await this.connection.invoke("LeaveTestAnalytics", this.testId, this.testType);
                await this.connection.stop();
                console.log("⏹️ SignalR отключен");
            } catch (err) {
                console.error("Ошибка отключения SignalR:", err);
            }
        }
    }
}

// Глобальный экземпляр
window.testAnalyticsSignalR = null;
