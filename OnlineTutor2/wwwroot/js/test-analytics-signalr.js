// @ts-nocheck

class TestAnalyticsSignalR {
    constructor(testId, testType) {
        this.testId = testId;
        this.testType = testType;
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        
        console.log('🎯 Создан экземпляр TestAnalyticsSignalR:', { testId, testType });
    }

    async start() {
        try {
            console.log('🔌 Начало подключения SignalR...');
            
            if (typeof signalR === 'undefined') {
                throw new Error('SignalR библиотека не загружена!');
            }
            
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
                .configureLogging(signalR.LogLevel.Debug)
                .build();

            this.setupEventHandlers();
            
            console.log('🔗 Попытка подключения к Hub...');
            await this.connection.start();
            console.log('✅ Соединение установлено');
            
            console.log('📢 Присоединение к группе:', `${this.testType}_test_${this.testId}`);
            await this.connection.invoke("JoinTestAnalytics", this.testId, this.testType);
            console.log('✅ Успешно присоединились к группе');
            
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            console.log("✅ SignalR полностью готов. Test:", this.testId, "Type:", this.testType);
            this.showConnectionStatus('connected');
            
            console.log('🎉 SignalR готов к получению уведомлений!');
            
        } catch (err) {
            console.error("❌ Ошибка подключения SignalR:", err);
            console.error("📋 Детали ошибки:", {
                name: err.name,
                message: err.message,
                stack: err.stack
            });
            this.showConnectionStatus('error');
            this.scheduleReconnect();
        }
    }

    setupEventHandlers() {
        console.log('📡 Настройка обработчиков событий...');
        
        // ✅ Событие: студент начал тест
        this.connection.on("TestStarted", (data) => {
            console.log("📝 [EVENT] TestStarted получено:", data);
            
            // ✅ Используем имя студента
            const message = data.studentName 
                ? `${data.studentName} начал прохождение теста`
                : 'Студент начал прохождение теста';
            
            this.showNotification(message, "info");
            this.refreshAnalytics();
        });

        // ✅ Событие: студент продолжил тест
        this.connection.on("TestContinued", (data) => {
            console.log("🔄 [EVENT] TestContinued получено:", data);
            
            // ✅ Используем имя студента
            const message = data.studentName 
                ? `${data.studentName} продолжил прохождение теста`
                : 'Студент продолжил прохождение теста';
            
            this.showNotification(message, "info");
            this.refreshAnalytics();
        });

        // ✅ Событие: студент завершил тест
        this.connection.on("TestCompleted", (data) => {
            console.log("✅ [EVENT] TestCompleted получено:", data);
            
            // ✅ Используем имя студента и результаты
            let message = data.studentName 
                ? `${data.studentName} завершил тест!`
                : 'Студент завершил тест!';
            
            // Добавляем результат если доступен
            if (data.percentage !== undefined) {
                message += ` Результат: ${data.percentage.toFixed(1)}%`;
                
                if (data.score !== undefined && data.maxScore !== undefined) {
                    message += ` (${data.score}/${data.maxScore})`;
                }
            }
            
            this.showNotification(message, "success");
            this.refreshAnalytics();
        });

        this.connection.onreconnecting((error) => {
            console.warn("⚠️ SignalR переподключается...", error);
            this.isConnected = false;
            this.showConnectionStatus('reconnecting');
        });

        this.connection.onreconnected((connectionId) => {
            console.log("✅ SignalR переподключен. ConnectionId:", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.showConnectionStatus('connected');
            
            console.log('📢 Повторное присоединение к группе...');
            this.connection.invoke("JoinTestAnalytics", this.testId, this.testType)
                .then(() => console.log('✅ Повторно присоединились к группе'))
                .catch(err => console.error("❌ Ошибка повторного присоединения:", err));
        });

        this.connection.onclose((error) => {
            console.error("❌ SignalR отключен:", error);
            this.isConnected = false;
            this.showConnectionStatus('disconnected');
            this.scheduleReconnect();
        });
        
        console.log('✅ Обработчики событий настроены');
    }

    async refreshAnalytics() {
        console.log('🔄 Обновление аналитики...');
        if (window.testAnalyticsRealtime) {
            await window.testAnalyticsRealtime.update();
        } else {
            console.log("⚠️ Realtime объект не найден, перезагрузка страницы...");
            location.reload();
        }
    }

    showNotification(message, type = 'info') {
        console.log(`📣 Показываем уведомление: [${type}] ${message}`);
        
        const alertClass = type === 'success' ? 'alert-success' : 
            type === 'info' ? 'alert-info' : 
                type === 'warning' ? 'alert-warning' : 'alert-danger';
        
        const icon = type === 'success' ? 'fa-check-circle' : 
            type === 'info' ? 'fa-info-circle' : 
                type === 'warning' ? 'fa-exclamation-circle' : 'fa-times-circle';

        const studentIcon = (type === 'info') ? '<i class="fas fa-user-graduate me-1"></i>' : '';

        const notification = document.createElement('div');
        notification.className = 'signalr-notification';
        notification.innerHTML = `
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                <i class="fas ${icon} me-2"></i>
                ${studentIcon}
                <strong>${message}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        document.body.appendChild(notification);

        const displayTime = type === 'success' ? 7000 : 5000;
        
        setTimeout(() => {
            notification.remove();
        }, displayTime);
    }

    showConnectionStatus(status) {
        const statusElement = document.getElementById('signalr-status');
        if (!statusElement) {
            console.warn('⚠️ Элемент signalr-status не найден');
            return;
        }

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
        
        console.log('📊 Статус обновлен:', status);
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
                console.log('🛑 Остановка SignalR...');
                await this.connection.invoke("LeaveTestAnalytics", this.testId, this.testType);
                await this.connection.stop();
                console.log("⏹️ SignalR остановлен");
            } catch (err) {
                console.error("❌ Ошибка остановки SignalR:", err);
            }
        }
    }
}

window.testAnalyticsSignalR = null;
