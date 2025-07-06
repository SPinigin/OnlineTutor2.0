class TestAnalytics {
    constructor() {
        this.modal = null;
        this.initializeModal();
    }

    // Инициализация модального окна
    initializeModal() {
        $(document).ready(() => {
            this.modal = $('#studentDetailsModal');

            this.modal.on('shown.bs.modal', function () {
                $(this).find('button, input, select, textarea').blur();
                $(this).focus();
            });

            this.modal.on('hide.bs.modal', function () {
                $(this).find('button, input, select, textarea').blur();
                $('body').focus();
            });

            this.modal.on('hidden.bs.modal', () => {
                console.log('Modal closed, resetting state');
                this.resetModalState();
                this.modal.find('*').blur();
                $('body').focus();
            });
        });
    }

    // Сброс состояния модального окна
    resetModalState() {
        console.log('Resetting modal state');

        this.modal.find('button, input, select, textarea').blur();

        const visibilityElements = ['studentModalLoading', 'studentModalContent', 'studentModalError'];
        visibilityElements.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.style.display = 'none';
            }
        });

        const textElements = {
            'studentModalName': 'Детальная информация об ученике',
            'studentFullName': '-',
            'studentSchool': '-',
            'studentClass': '-',
            'studentAttempts': '-',
            'studentTotalTime': '-',
            'studentErrorMessage': 'Произошла ошибка при загрузке данных'
        };

        Object.entries(textElements).forEach(([id, defaultText]) => {
            const element = document.getElementById(id);
            if (element) {
                element.textContent = defaultText;
            }
        });

        const htmlElements = ['studentBestResult', 'studentAttemptsList', 'studentMistakes'];
        htmlElements.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.innerHTML = id === 'studentBestResult' ? '-' : '';
            }
        });
    }

    // Показать загрузку
    showLoading() {
        const loadingElement = document.getElementById('studentModalLoading');
        const contentElement = document.getElementById('studentModalContent');
        const errorElement = document.getElementById('studentModalError');

        if (loadingElement) loadingElement.style.display = 'block';
        if (contentElement) contentElement.style.display = 'none';
        if (errorElement) errorElement.style.display = 'none';
    }

    // Показать ошибку
    showError(message) {
        console.log('Showing error:', message);

        const loadingElement = document.getElementById('studentModalLoading');
        const contentElement = document.getElementById('studentModalContent');
        const errorElement = document.getElementById('studentModalError');
        const errorMessageElement = document.getElementById('studentErrorMessage');

        if (loadingElement) loadingElement.style.display = 'none';
        if (contentElement) contentElement.style.display = 'none';
        if (errorMessageElement) errorMessageElement.textContent = message;
        if (errorElement) errorElement.style.display = 'block';
    }

    // ОБЩИЙ метод для отображения основной информации о студенте
    displayBasicStudentInfo(data) {
        const loadingElement = document.getElementById('studentModalLoading');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }

        const setTextContent = (id, value) => {
            const element = document.getElementById(id);
            if (element) {
                element.textContent = value;
            }
        };

        setTextContent('studentFullName', data.fullName || 'Не указано');
        setTextContent('studentSchool', data.school || 'Не указано');
        setTextContent('studentClass', data.className || 'Не указан');
        setTextContent('studentAttempts', `${data.attemptsUsed || 0}/${data.maxAttempts || 0}`);
        setTextContent('studentTotalTime', data.totalTimeSpent || 'Не определено');

        // Заполняем лучший результат
        const bestResultElement = document.getElementById('studentBestResult');
        if (bestResultElement) {
            if (data.bestResult) {
                const grade = this.getGradeFromPercentage(data.bestResult.percentage);
                bestResultElement.innerHTML =
                    `<span class="badge bg-${this.getPercentageColor(data.bestResult.percentage)}">${data.bestResult.percentage.toFixed(1)}%</span>
                     <span class="grade-badge grade-${this.getGradeColorClass(grade)}">${grade}</span>
                     <small class="text-muted">(${data.bestResult.score}/${data.bestResult.maxScore} баллов)</small>`;
            } else {
                bestResultElement.innerHTML = '<span class="text-muted">Нет результатов</span>';
            }
        }

        // Заполняем историю попыток (общая для всех типов)
        this.displayAttemptsList(data.attempts || []);
    }

    // Отображение списка попыток (ОБЩИЙ метод)
    displayAttemptsList(attempts) {
        const container = document.getElementById('studentAttemptsList');
        if (!container) return;

        if (!attempts || attempts.length === 0) {
            container.innerHTML = '<p class="text-muted"><i class="fas fa-info-circle"></i> Попыток пока нет</p>';
            return;
        }

        let html = '<div class="table-responsive"><table class="table table-sm table-striped">';
        html += '<thead class="table-light"><tr><th>Попытка</th><th>Результат</th><th>Баллы</th><th>Время</th><th>Дата</th></tr></thead><tbody>';

        attempts.forEach(attempt => {
            const grade = this.getGradeFromPercentage(attempt.percentage);
            const duration = attempt.duration || '-';
            const completedDate = attempt.completedAt ? new Date(attempt.completedAt).toLocaleDateString('ru-RU') : '-';

            html += `<tr>
                <td><span class="badge bg-info">${attempt.attemptNumber}</span></td>
                <td>
                    <span class="badge bg-${this.getPercentageColor(attempt.percentage)}">${attempt.percentage.toFixed(1)}%</span>
                    <span class="grade-badge grade-${this.getGradeColorClass(grade)} ms-1">${grade}</span>
                </td>
                <td><small>${attempt.score}/${attempt.maxScore}</small></td>
                <td><small>${duration}</small></td>
                <td><small>${completedDate}</small></td>
            </tr>`;
        });

        html += '</tbody></table></div>';
        container.innerHTML = html;
    }

    // Вспомогательные методы (ОБЩИЕ)
    getGradeFromPercentage(percentage) {
        if (percentage >= 91) return 5;
        if (percentage >= 76) return 4;
        if (percentage >= 51) return 3;
        return 2;
    }

    getPercentageColor(percentage) {
        if (percentage >= 80) return 'success';
        if (percentage >= 60) return 'warning';
        return 'danger';
    }

    getGradeColorClass(grade) {
        switch (grade) {
            case 5: return 'excellent';
            case 4: return 'good';
            case 3: return 'satisfactory';
            case 2: return 'unsatisfactory';
            default: return 'unsatisfactory';
        }
    }
}

// Создаем глобальный экземпляр
window.testAnalytics = new TestAnalytics();
