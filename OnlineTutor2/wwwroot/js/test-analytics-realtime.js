//@ts-nocheck

class TestAnalyticsRealtime {
    constructor(testId, updateInterval = 30000, testType = 'spelling') {
        this.testId = testId;
        this.testType = testType;
        this.updateInterval = updateInterval;
        this.isUpdating = false;
        this.intervalId = null;
    }

    start() {
        console.log('🔄 Автообновление запущено');
        this.update();
        this.intervalId = setInterval(() => this.update(), this.updateInterval);
    }

    stop() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
            console.log('⏸️ Автообновление остановлено (Polling)');
        }
    }

    async update() {
        if (this.isUpdating) return;
        
        this.isUpdating = true;
        this.showUpdateIndicator();

        try {
            const urlMap = {
                'spelling': `/TestAnalytics/GetSpellingAnalyticsData?testId=${this.testId}`,
                'punctuation': `/TestAnalytics/GetPunctuationAnalyticsData?testId=${this.testId}`,
                'orthoepy': `/TestAnalytics/GetOrthoeopyAnalyticsData?testId=${this.testId}`,
                'regular': `/TestAnalytics/GetRegularTestAnalyticsData?testId=${this.testId}`
            };

            const url = urlMap[this.testType];
            if (!url) {
                throw new Error(`Неизвестный тип теста: ${this.testType}`);
            }

            console.log('📡 Загрузка данных:', url);
            const response = await fetch(url);
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();
            console.log('✅ Данные получены:', data);
            
            this.updateUI(data);
            this.hideUpdateIndicator(true);
        } catch (error) {
            console.error('❌ Ошибка обновления:', error);
            this.hideUpdateIndicator(false);
        } finally {
            this.isUpdating = false;
        }
    }

    updateUI(data) {
        this.updateStatistics(data.statistics);
        this.updateStudentResults(data.studentResults);
        this.updateQuestionAnalytics(data.questionAnalytics);
    }

    updateStatistics(stats) {
        console.log('📊 Обновление статистики:', stats);
        const updates = [
            { selector: '.stats-card-compact:nth-child(1) h5', value: stats.totalStudents },
            { selector: '.stats-card-compact:nth-child(2) h5', value: `${stats.averagePercentage.toFixed(1)}%` },
            { selector: '.stats-card-compact:nth-child(3) h5', value: stats.averageCompletionTime },
            { selector: '.stats-card-compact:nth-child(4) h5', value: stats.studentsCompleted },
            { selector: '.stats-card-compact:nth-child(5) h5', value: stats.studentsInProgress },
            { selector: '.stats-card-compact:nth-child(6) h5', value: stats.studentsNotStarted }
        ];

        updates.forEach(update => {
            const element = document.querySelector(update.selector);
            if (element && element.textContent !== update.value.toString()) {
                this.animateChange(element, update.value);
            }
        });
    }

    updateStudentResults(students) {
        console.log('👥 Обновление списка студентов:', students.length);
        const tbody = document.querySelector('#students table tbody');
        if (!tbody) return;

        students.forEach(student => {
            const row = tbody.querySelector(`tr[data-student-id="${student.studentId}"]`);
            if (!row) {
                console.log('⚠️ Строка не найдена для студента:', student.studentId);
                this.addStudentRow(tbody, student);
                return;
            }

            // Обновляем статус
            const statusCell = row.querySelector('td:nth-child(2)');
            this.updateStatusBadge(statusCell, student);

            // Обновляем попытки
            const attemptsCell = row.querySelector('td:nth-child(3)');
            if (attemptsCell) {
                attemptsCell.innerHTML = `<span class="badge bg-info badge-sm">${student.attemptsUsed}/${student.maxAttempts || '∞'}</span>`;
            }

            // Обновляем лучший результат
            const bestCell = row.querySelector('td:nth-child(4)');
            if (bestCell && student.bestResult) {
                this.updateResultCell(bestCell, student.bestResult);
            }

            // Обновляем последний результат
            const latestCell = row.querySelector('td:nth-child(5)');
            if (latestCell && student.latestResult) {
                this.updateResultCell(latestCell, student.latestResult);
            }

            // Обновляем время
            const timeCell = row.querySelector('td:nth-child(6)');
            if (timeCell && student.totalTimeSpent) {
                const minutes = String(student.totalTimeSpent.minutes).padStart(2, '0');
                const seconds = String(student.totalTimeSpent.seconds).padStart(2, '0');
                timeCell.innerHTML = `<span>${minutes}:${seconds}</span>`;
            }
        });
    }

    updateStatusBadge(cell, student) {
        if (!cell) return;

        let badgeClass, icon, text;
        if (student.hasCompleted) {
            badgeClass = 'bg-success';
            icon = 'fa-check';
            text = 'Завершил';
        } else if (student.isInProgress) {
            badgeClass = 'bg-warning';
            icon = 'fa-clock';
            text = 'В процессе';
        } else {
            badgeClass = 'bg-danger';
            icon = 'fa-times';
            text = 'Не начал';
        }

        cell.innerHTML = `
            <span class="badge ${badgeClass} badge-sm">
                <i class="fas ${icon} d-md-none"></i>
                <span class="d-none d-md-inline">${text}</span>
            </span>
        `;
    }

    updateResultCell(cell, result) {
        const percentage = result.percentage;
        const grade = this.getGradeFromPercentage(percentage);
        const gradeColor = this.getGradeColor(grade);
        const badgeColor = percentage >= 80 ? 'success' : percentage >= 60 ? 'warning' : 'danger';

        cell.innerHTML = `
            <div class="d-flex flex-column align-items-center gap-1">
                <div class="d-flex align-items-center gap-1">
                    <span class="badge bg-${badgeColor} badge-sm">${percentage.toFixed(1)}%</span>
                    <span class="grade-badge-compact grade-${gradeColor}">${grade}</span>
                </div>
                <small class="text-muted d-none d-md-block">${result.score}/${result.maxScore}</small>
                ${result.completedAt ? `<small class="text-muted d-none d-lg-block">${result.completedAt}</small>` : ''}
            </div>
        `;
    }

    updateQuestionAnalytics(questions) {
        console.log('❓ Обновление аналитики вопросов:', questions.length);
        
        questions.forEach(q => {
            const card = document.querySelector(`.question-analytics-card[data-question-id="${q.questionId}"]`);
            if (!card) return;

            const percentElement = card.querySelector('.h4');
            if (percentElement) {
                const newPercent = q.successRate.toFixed(0) + '%';
                if (percentElement.textContent !== newPercent) {
                    this.animateChange(percentElement, newPercent);
                    percentElement.className = `h4 mb-0 text-${q.successRate >= 80 ? 'success' : q.successRate >= 60 ? 'warning' : 'danger'}`;
                }
            }

            const countElement = card.querySelector('small.text-muted');
            if (countElement) {
                countElement.textContent = `${q.correctAnswers}/${q.totalAnswers}`;
            }
        });
    }

    addStudentRow(tbody, student) {
        // Создание новой строки для студента (если он только начал тест)
        const row = document.createElement('tr');
        row.setAttribute('data-student-id', student.studentId);
        // ... добавьте HTML разметку строки
        tbody.appendChild(row);
        row.classList.add('table-row-new');
        setTimeout(() => row.classList.remove('table-row-new'), 1000);
    }

    animateChange(element, newValue) {
        element.classList.add('value-changed');
        element.textContent = newValue;
        setTimeout(() => element.classList.remove('value-changed'), 600);
    }

    showUpdateIndicator() {
        let indicator = document.getElementById('updateIndicator');
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.id = 'updateIndicator';
            indicator.innerHTML = `
                <div class="alert alert-info alert-dismissible fade show position-fixed" 
                     style="top: 70px; right: 20px; z-index: 1050; min-width: 250px;">
                    <i class="fas fa-sync fa-spin me-2"></i>
                    <span>Обновление данных...</span>
                </div>
            `;
            document.body.appendChild(indicator);
        }
    }

    hideUpdateIndicator(success) {
        const indicator = document.getElementById('updateIndicator');
        if (indicator) {
            const icon = success ? 'fa-check-circle text-success' : 'fa-exclamation-circle text-warning';
            const text = success ? 'Данные обновлены' : 'Ошибка обновления';
            
            indicator.innerHTML = `
                <div class="alert alert-${success ? 'success' : 'warning'} alert-dismissible fade show position-fixed" 
                     style="top: 70px; right: 20px; z-index: 1050; min-width: 250px;">
                    <i class="fas ${icon} me-2"></i>
                    <span>${text}</span>
                </div>
            `;
            
            setTimeout(() => {
                if (indicator.parentNode) {
                    indicator.remove();
                }
            }, 2000);
        }
    }

    getGradeFromPercentage(percentage) {
        if (percentage >= 91) return 5;
        if (percentage >= 76) return 4;
        if (percentage >= 61) return 3;
        return 2;
    }

    getGradeColor(grade) {
        return {
            5: 'excellent',
            4: 'good',
            3: 'satisfactory',
            2: 'unsatisfactory'
        }[grade] || 'unsatisfactory';
    }
}

// Глобальный экземпляр
window.testAnalyticsRealtime = null;
