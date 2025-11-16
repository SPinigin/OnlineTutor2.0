// @ts-nocheck

/**
 * Класс для работы с SignalR на Dashboard учителя
 */
class TeacherDashboardSignalR {
    constructor(teacherId) {
        this.teacherId = teacherId;
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        
        console.log('🎯 Создан Dashboard SignalR для учителя:', teacherId);
    }

    /**
     * Запуск SignalR подключения
     */
    async start() {
        try {
            console.log('🔌 Подключение Dashboard SignalR...');
            
            // Проверяем доступность библиотеки
            if (typeof signalR === 'undefined') {
                throw new Error('SignalR библиотека не загружена!');
            }
            
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

            // Настраиваем обработчики
            this.setupEventHandlers();
            
            // Подключаемся
            await this.connection.start();
            console.log('✅ SignalR подключен');
            
            // Присоединяемся к группе учителя
            await this.connection.invoke("JoinTeacherDashboard", this.teacherId);
            console.log('✅ Присоединились к группе учителя:', this.teacherId);
            
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.showConnectionStatus('connected');
            
        } catch (err) {
            console.error("❌ Ошибка подключения Dashboard SignalR:", err);
            this.showConnectionStatus('error');
            this.scheduleReconnect();
        }
    }

    /**
     * Настройка обработчиков событий
     */
    setupEventHandlers() {
        console.log('📡 Настройка обработчиков событий Dashboard...');
        
        // ✅ ГЛАВНОЕ СОБЫТИЕ: Активность студента по любому тесту
        this.connection.on("StudentTestActivity", (data) => {
            console.log("📬 [DASHBOARD] Получена активность:", data);
            this.handleTestActivity(data);
        });

        // Обработчик переподключения
        this.connection.onreconnecting((error) => {
            console.warn("⚠️ Dashboard SignalR переподключается...", error);
            this.isConnected = false;
            this.showConnectionStatus('reconnecting');
        });

        // Обработчик успешного переподключения
        this.connection.onreconnected((connectionId) => {
            console.log("✅ Dashboard SignalR переподключен:", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.showConnectionStatus('connected');
            
            // Заново присоединяемся к группе учителя
            this.connection.invoke("JoinTeacherDashboard", this.teacherId)
                .then(() => console.log('✅ Повторно присоединились к группе учителя'))
                .catch(err => console.error("❌ Ошибка повторного присоединения:", err));
        });

        // Обработчик закрытия соединения
        this.connection.onclose((error) => {
            console.error("❌ Dashboard SignalR отключен:", error);
            this.isConnected = false;
            this.showConnectionStatus('disconnected');
            this.scheduleReconnect();
        });
        
        console.log('✅ Обработчики Dashboard настроены');
    }

    /**
     * Обработка активности студента
     */
    handleTestActivity(data) {
        console.log('🎬 Обработка активности:', data.action, data);

        var message = '';
        var notificationType = 'info';
        var isTimeout = false;

        switch (data.action) {
            case 'started':
                message = data.studentName + ' начал "' + data.testTitle + '"';
                notificationType = 'info';
                break;
            
            case 'continued':
                message = data.studentName + ' продолжил "' + data.testTitle + '"';
                notificationType = 'info';
                break;
            
            case 'completed':
                var percentColor = data.percentage >= 80 ? '✅' : 
                    data.percentage >= 60 ? '⚠️' : '❌';
                
                // Проверяем автозавершение
                if (data.isAutoCompleted) {
                    message = '⏰ Время истекло! ' + data.studentName + 
                        ' автоматически завершил "' + data.testTitle + '" ' + 
                        percentColor + ' ' + data.percentage.toFixed(1) + '%';
                    notificationType = 'warning';
                    isTimeout = true;
                } else {
                    message = data.studentName + ' завершил "' + data.testTitle + '" ' + 
                        percentColor + ' ' + data.percentage.toFixed(1) + '%';
                    notificationType = data.percentage >= 60 ? 'success' : 'warning';
                }
                break;
        }

        // Показываем уведомление
        this.showNotification(message, notificationType, data, isTimeout);

        // Воспроизводим звук (опционально)
        this.playNotificationSound(data.action, isTimeout);

        // Добавляем в ленту активности
        this.addToActivityFeed(data);

        // Обновляем статистику
        if (typeof updateStats === 'function') {
            updateStats(data.action);
        }

        // Обновляем счетчики в таблице тестов
        this.updateTestCard(data);
    }

    /**
     * Показать уведомление
     */
    showNotification(message, type, data, isTimeout = false) {
        console.log('📣 Уведомление:', type, message);
        
        var alertClass = type === 'success' ? 'alert-success' : 
            type === 'info' ? 'alert-info' : 
                type === 'warning' ? 'alert-warning' : 'alert-danger';
        
        var icon = type === 'success' ? 'fa-check-circle' : 
            type === 'info' ? 'fa-info-circle' : 
                type === 'warning' ? 'fa-exclamation-circle' : 'fa-times-circle';

        var testTypeIcon = this.getTestTypeIcon(data.testType);
        
        // Специальный класс для автозавершения
        var timeoutClass = isTimeout ? ' notification-timeout' : '';

        var notification = document.createElement('div');
        notification.className = 'dashboard-notification';
        notification.innerHTML = 
            '<div class="alert ' + alertClass + timeoutClass + ' alert-dismissible fade show shadow-lg" role="alert">' +
            '<div class="d-flex align-items-start">' +
            '<i class="fas ' + icon + ' fs-4 me-3"></i>' +
            '<div class="flex-grow-1">' +
            '<div class="d-flex align-items-center mb-1">' +
            '<i class="fas ' + testTypeIcon + ' me-2"></i>' +
            '<strong>' + message + '</strong>' +
            '</div>' +
            '<small class="text-muted">' + new Date().toLocaleTimeString('ru-RU') + '</small>' +
            '</div>' +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            '</div>' +
            '</div>';

        document.body.appendChild(notification);

        // Увеличенное время показа для автозавершения
        var displayTime = isTimeout ? 10000 : (type === 'success' ? 8000 : 5000);
        
        setTimeout(function() {
            notification.remove();
        }, displayTime);
    }

    /**
     * Добавить активность в ленту
     */
    addToActivityFeed(data) {
        console.log('➕ Добавление активности в ленту');

        var activity = {
            testId: data.testId,
            testResultId: data.testResultId || null,
            testTitle: data.testTitle,
            testType: data.testType,
            studentId: data.studentId,
            studentName: data.studentName,
            status: data.action === 'completed' ? 'completed' : 'in_progress',
            percentage: data.percentage || 0,
            score: data.score || 0,
            maxScore: data.maxScore || 0,
            lastActivityAt: new Date().toISOString(),
            isAutoCompleted: data.isAutoCompleted || false
        };

        if (typeof prependActivity === 'function') {
            prependActivity(activity);
        } else {
            console.warn('⚠️ Функция prependActivity не найдена');
        }
    }

    /**
     * Обновить счетчики в таблице тестов
     */
    updateTestCard(data) {
        console.log('📊 Обновление счетчиков теста:', data.testId, data.testType);

        var completedBadge = document.querySelector(
            '.test-count-completed[data-test-id="' + data.testId + '"][data-test-type="' + data.testType + '"]'
        );
        var progressBadge = document.querySelector(
            '.test-count-progress[data-test-id="' + data.testId + '"][data-test-type="' + data.testType + '"]'
        );
        
        if (completedBadge && progressBadge) {
            if (data.action === 'completed') {
                // Увеличиваем завершенных
                var completed = parseInt(completedBadge.textContent);
                completedBadge.textContent = completed + 1;
                completedBadge.classList.add('badge-pulse');
                setTimeout(function() {
                    completedBadge.classList.remove('badge-pulse');
                }, 600);
                
                // Уменьшаем в процессе
                var inProgress = parseInt(progressBadge.textContent);
                if (inProgress > 0) {
                    progressBadge.textContent = inProgress - 1;
                }
                
                console.log('✅ Счетчики обновлены: завершено +1, в процессе -1');
                
            } else if (data.action === 'started') {
                // Увеличиваем в процессе
                var inProgress = parseInt(progressBadge.textContent);
                progressBadge.textContent = inProgress + 1;
                progressBadge.classList.add('badge-pulse');
                setTimeout(function() {
                    progressBadge.classList.remove('badge-pulse');
                }, 600);
                
                console.log('✅ Счетчики обновлены: в процессе +1');
            }
        } else {
            console.warn('⚠️ Бейджи счетчиков не найдены для теста:', data.testId, data.testType);
        }
    }

    /**
     * Воспроизведение звука уведомления (опционально)
     */
    playNotificationSound(action, isTimeout = false) {
        // Можно добавить позже звуковые уведомления
        // if (action === 'completed' && !isTimeout) {
        //     new Audio('/sounds/success.mp3').play().catch(e => {});
        // } else if (isTimeout) {
        //     new Audio('/sounds/timeout.mp3').play().catch(e => {});
        // }
    }

    /**
     * Получить иконку для типа теста
     */
    getTestTypeIcon(type) {
        var icons = {
            'spelling': 'fa-spell-check',
            'punctuation': 'fa-paragraph',
            'orthoepy': 'fa-volume-up',
            'regular': 'fa-question-circle'
        };
        return icons[type] || 'fa-clipboard-list';
    }

    /**
     * Получить цвет для типа теста
     */
    getTestTypeColor(type) {
        var colors = {
            'spelling': 'primary',
            'punctuation': 'info',
            'orthoepy': 'danger',
            'regular': 'secondary'
        };
        return colors[type] || 'secondary';
    }

    /**
     * Показать статус подключения
     */
    showConnectionStatus(status) {
        var statusElement = document.getElementById('signalr-status');
        if (!statusElement) {
            console.warn('⚠️ Элемент signalr-status не найден');
            return;
        }

        var statusConfig = {
            connected: { 
                icon: 'fa-circle text-success', 
                text: 'Онлайн', 
                class: 'status-connected' 
            },
            reconnecting: { 
                icon: 'fa-sync fa-spin text-warning', 
                text: 'Переподключение...', 
                class: 'status-reconnecting' 
            },
            disconnected: { 
                icon: 'fa-circle text-danger', 
                text: 'Оффлайн', 
                class: 'status-disconnected' 
            },
            error: { 
                icon: 'fa-exclamation-triangle text-danger', 
                text: 'Ошибка', 
                class: 'status-error' 
            }
        };

        var config = statusConfig[status];
        statusElement.className = 'signalr-status ' + config.class;
        statusElement.innerHTML = 
            '<i class="fas ' + config.icon + ' me-1"></i>' +
            '<span>' + config.text + '</span>';
        
        console.log('📊 Статус Dashboard:', status);
    }

    /**
     * Планирование переподключения
     */
    scheduleReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error("❌ Превышено максимальное количество попыток переподключения");
            
            var statusElement = document.getElementById('signalr-status');
            if (statusElement) {
                statusElement.innerHTML = 
                    '<i class="fas fa-times-circle text-danger me-1"></i>' +
                    '<span>Нет соединения</span> ' +
                    '<button class="btn btn-sm btn-outline-danger ms-2" onclick="location.reload()">' +
                    '<i class="fas fa-redo"></i> Обновить' +
                    '</button>';
            }
            return;
        }

        this.reconnectAttempts++;
        var delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
        
        console.log('🔄 Попытка переподключения Dashboard ' + this.reconnectAttempts + '/' + 
            this.maxReconnectAttempts + ' через ' + delay + 'мс');
        
        var self = this;
        setTimeout(function() {
            self.start();
        }, delay);
    }

    /**
     * Остановка SignalR
     */
    async stop() {
        if (this.connection) {
            try {
                console.log('🛑 Остановка Dashboard SignalR...');
                await this.connection.invoke("LeaveTeacherDashboard", this.teacherId);
                await this.connection.stop();
                console.log("⏹️ Dashboard SignalR остановлен");
            } catch (err) {
                console.error("❌ Ошибка остановки Dashboard SignalR:", err);
            }
        }
    }
}

// ===== ГЛОБАЛЬНЫЕ ФУНКЦИИ =====

/**
 * Открыть модальное окно с результатом теста
 */
window.showTestResultModal = function(testType, testResultId, studentName) {
    console.log('📄 Открытие модального окна результата:', { testType, testResultId, studentName });

    // Получаем или создаем модальное окно
    var modal = document.getElementById('testResultModal');
    if (!modal) {
        console.error('❌ Модальное окно testResultModal не найдено!');
        return;
    }

    // Устанавливаем заголовок
    var modalTitle = document.getElementById('testResultModalTitle');
    if (modalTitle) {
        modalTitle.textContent = 'Результат: ' + studentName;
    }

    // Показываем индикатор загрузки
    var modalBody = document.getElementById('testResultModalBody');
    if (modalBody) {
        modalBody.innerHTML = 
            '<div class="text-center py-5">' +
            '<div class="spinner-border text-primary" role="status">' +
            '<span class="visually-hidden">Загрузка...</span>' +
            '</div>' +
            '<p class="text-muted mt-3">Загрузка результата...</p>' +
            '</div>';
    }

    // Открываем модальное окно
    var bsModal = new bootstrap.Modal(modal);
    bsModal.show();

    // Загружаем содержимое
    loadTestResult(testType, testResultId);
};

/**
 * Загрузка результата теста
 */
function loadTestResult(testType, testResultId) {
    console.log('📡 Загрузка результата теста:', testType, testResultId);

    fetch('/TeacherDashboard/GetTestResult?testType=' + testType + '&testResultId=' + testResultId)
        .then(response => {
            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }
            return response.text();
        })
        .then(html => {
            console.log('✅ Результат загружен');
            var modalBody = document.getElementById('testResultModalBody');
            if (modalBody) {
                modalBody.innerHTML = html;
                
                // Инициализируем скрипты внутри загруженного HTML
                initializeResultScripts();
            }
        })
        .catch(error => {
            console.error('❌ Ошибка загрузки результата:', error);
            var modalBody = document.getElementById('testResultModalBody');
            if (modalBody) {
                modalBody.innerHTML = 
                    '<div class="text-center py-5">' +
                    '<i class="fas fa-exclamation-triangle text-warning fs-1 mb-3"></i>' +
                    '<h5 class="text-muted">Ошибка загрузки результата</h5>' +
                    '<p class="text-muted">' + error.message + '</p>' +
                    '<button class="btn btn-primary" onclick="loadTestResult(\'' + testType + '\', ' + testResultId + ')">' +
                    '<i class="fas fa-redo"></i> Попробовать снова' +
                    '</button>' +
                    '</div>';
            }
        });
}

/**
 * Инициализация скриптов внутри результата
 */
function initializeResultScripts() {
    console.log('🔧 Инициализация скриптов результата');

    // Анимация круговой диаграммы
    var circle = document.querySelector('#testResultModal .result-circle circle:nth-child(2)');
    if (circle) {
        circle.style.transition = 'stroke-dasharray 1s ease';
    }

    // Фильтр "Только ошибки"
    var filterCheckbox = document.querySelector('#testResultModal #showErrorsOnly');
    if (filterCheckbox) {
        function applyFilter() {
            var questions = document.querySelectorAll('#testResultModal .question-result');
            
            questions.forEach(function(question) {
                var isCorrect = question.getAttribute('data-correct') === 'true';
                
                if (filterCheckbox.checked) {
                    question.style.display = isCorrect ? 'none' : 'block';
                } else {
                    question.style.display = 'block';
                }
            });
        }

        filterCheckbox.addEventListener('change', applyFilter);
        applyFilter();
    }

    // Инициализация аккордеонов (для классических тестов)
    var accordions = document.querySelectorAll('#testResultModal .accordion-button');
    if (accordions.length > 0) {
        console.log('✅ Найдено аккордеонов:', accordions.length);
    }
}

// Глобальный экземпляр
window.dashboardSignalR = null;
