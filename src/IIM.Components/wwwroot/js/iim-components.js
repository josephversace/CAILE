// IIM Components JavaScript Module
window.IIM = window.IIM || {};

(function(IIM) {
    'use strict';

    // Configuration
    IIM.config = {
        animationDuration: 300,
        messageLimit: 100,
        autoSaveInterval: 30000, // 30 seconds
        maxFileSize: 100 * 1024 * 1024, // 100MB
        supportedFormats: ['.pdf', '.docx', '.txt', '.json', '.csv', '.png', '.jpg', '.mp3', '.wav'],
        apiEndpoint: 'https://localhost:7001/api',
        wsEndpoint: 'wss://localhost:7001/ws'
    };

    // State Management
    IIM.state = {
        currentSession: null,
        sessions: [],
        models: [],
        activeModel: null,
        isProcessing: false,
        connectionStatus: 'disconnected'
    };

    // Initialize IIM Components
    IIM.init = function() {
        console.log('Initializing IIM Components...');
        
        // Initialize HUD theme if available
        if (window.App && window.App.init) {
            console.log('HUD framework detected, initializing...');
        }
        
        // Setup event listeners
        IIM.setupEventListeners();
        
        // Initialize WebSocket connection
        IIM.initWebSocket();
        
        // Load saved state
        IIM.loadState();
        
        // Setup auto-save
        IIM.setupAutoSave();
        
        // Initialize tooltips and popovers
        IIM.initializeUIComponents();
        
        console.log('IIM Components initialized successfully');
    };

    // Setup Event Listeners
    IIM.setupEventListeners = function() {
        // Sidebar toggle
        document.addEventListener('click', function(e) {
            if (e.target.matches('[data-iim-toggle="sidebar"]')) {
                IIM.toggleSidebar();
            }
        });

        // Session selection
        document.addEventListener('click', function(e) {
            if (e.target.closest('.iim-session-item')) {
                const sessionId = e.target.closest('.iim-session-item').dataset.sessionId;
                IIM.selectSession(sessionId);
            }
        });

        // Model selection
        document.addEventListener('click', function(e) {
            if (e.target.closest('.iim-model-card')) {
                const modelId = e.target.closest('.iim-model-card').dataset.modelId;
                IIM.selectModel(modelId);
            }
        });

        // File upload
        document.addEventListener('drop', function(e) {
            if (e.target.closest('.iim-drop-zone')) {
                e.preventDefault();
                IIM.handleFileDrop(e);
            }
        });

        document.addEventListener('dragover', function(e) {
            if (e.target.closest('.iim-drop-zone')) {
                e.preventDefault();
                e.target.closest('.iim-drop-zone').classList.add('dragover');
            }
        });

        document.addEventListener('dragleave', function(e) {
            if (e.target.closest('.iim-drop-zone')) {
                e.target.closest('.iim-drop-zone').classList.remove('dragover');
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', function(e) {
            // Ctrl/Cmd + K - Quick search
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                IIM.openQuickSearch();
            }
            
            // Ctrl/Cmd + N - New session
            if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
                e.preventDefault();
                IIM.createNewSession();
            }
            
            // Ctrl/Cmd + S - Save
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault();
                IIM.saveCurrentSession();
            }
        });
    };

    // WebSocket Management
    IIM.initWebSocket = function() {
        try {
            IIM.ws = new WebSocket(IIM.config.wsEndpoint);
            
            IIM.ws.onopen = function() {
                console.log('WebSocket connected');
                IIM.updateConnectionStatus('connected');
            };
            
            IIM.ws.onmessage = function(event) {
                const data = JSON.parse(event.data);
                IIM.handleWebSocketMessage(data);
            };
            
            IIM.ws.onerror = function(error) {
                console.error('WebSocket error:', error);
                IIM.updateConnectionStatus('error');
            };
            
            IIM.ws.onclose = function() {
                console.log('WebSocket disconnected');
                IIM.updateConnectionStatus('disconnected');
                // Attempt reconnection after 5 seconds
                setTimeout(IIM.initWebSocket, 5000);
            };
        } catch (error) {
            console.error('Failed to initialize WebSocket:', error);
        }
    };

    IIM.handleWebSocketMessage = function(data) {
        switch(data.type) {
            case 'model_status':
                IIM.updateModelStatus(data.modelId, data.status);
                break;
            case 'processing_update':
                IIM.updateProcessingStatus(data);
                break;
            case 'new_message':
                IIM.appendMessage(data.message);
                break;
            case 'session_update':
                IIM.updateSession(data.session);
                break;
            default:
                console.log('Unknown WebSocket message type:', data.type);
        }
    };

    // Session Management
    IIM.createNewSession = function() {
        const session = {
            id: IIM.generateId(),
            name: 'New Investigation',
            created: new Date().toISOString(),
            messages: [],
            context: [],
            status: 'active'
        };
        
        IIM.state.sessions.unshift(session);
        IIM.state.currentSession = session.id;
        IIM.renderSessions();
        IIM.clearWorkspace();
        
        // Notify Blazor
        if (window.DotNet) {
            DotNet.invokeMethodAsync('IIM.Components', 'OnSessionCreated', session);
        }
    };

    IIM.selectSession = function(sessionId) {
        IIM.state.currentSession = sessionId;
        const session = IIM.state.sessions.find(s => s.id === sessionId);
        
        if (session) {
            IIM.loadSession(session);
            
            // Update UI
            document.querySelectorAll('.iim-session-item').forEach(item => {
                item.classList.toggle('active', item.dataset.sessionId === sessionId);
            });
            
            // Notify Blazor
            if (window.DotNet) {
                DotNet.invokeMethodAsync('IIM.Components', 'OnSessionSelected', sessionId);
            }
        }
    };

    IIM.loadSession = function(session) {
        // Clear current workspace
        IIM.clearWorkspace();
        
        // Load messages
        session.messages.forEach(msg => {
            IIM.appendMessage(msg, false);
        });
        
        // Load context
        IIM.updateContext(session.context);
        
        // Update header
        IIM.updateWorkspaceHeader(session);
    };

    IIM.saveCurrentSession = function() {
        const session = IIM.state.sessions.find(s => s.id === IIM.state.currentSession);
        if (session) {
            // Save to local storage
            localStorage.setItem(`iim_session_${session.id}`, JSON.stringify(session));
            
            // Show save indicator
            IIM.showNotification('Session saved', 'success');
            
            // Notify Blazor
            if (window.DotNet) {
                DotNet.invokeMethodAsync('IIM.Components', 'OnSessionSaved', session);
            }
        }
    };

    // Model Management
    IIM.selectModel = function(modelId) {
        IIM.state.activeModel = modelId;
        
        // Update UI
        document.querySelectorAll('.iim-model-card').forEach(card => {
            card.classList.toggle('active', card.dataset.modelId === modelId);
        });
        
        // Load model if needed
        IIM.loadModel(modelId);
        
        // Notify Blazor
        if (window.DotNet) {
            DotNet.invokeMethodAsync('IIM.Components', 'OnModelSelected', modelId);
        }
    };

    IIM.loadModel = function(modelId) {
        const model = IIM.state.models.find(m => m.id === modelId);
        if (model && model.status !== 'loaded') {
            IIM.updateModelStatus(modelId, 'loading');
            
            // Simulate model loading (replace with actual API call)
            fetch(`${IIM.config.apiEndpoint}/models/${modelId}/load`, {
                method: 'POST'
            }).then(response => {
                if (response.ok) {
                    IIM.updateModelStatus(modelId, 'loaded');
                } else {
                    IIM.updateModelStatus(modelId, 'error');
                }
            }).catch(error => {
                console.error('Failed to load model:', error);
                IIM.updateModelStatus(modelId, 'error');
            });
        }
    };

    IIM.updateModelStatus = function(modelId, status) {
        const model = IIM.state.models.find(m => m.id === modelId);
        if (model) {
            model.status = status;
            
            // Update UI
            const card = document.querySelector(`[data-model-id="${modelId}"]`);
            if (card) {
                const statusElement = card.querySelector('.iim-model-status');
                if (statusElement) {
                    statusElement.textContent = status;
                    statusElement.className = `iim-model-status ${status}`;
                }
            }
        }
    };

    // File Handling
    IIM.handleFileDrop = function(event) {
        const files = Array.from(event.dataTransfer.files);
        
        files.forEach(file => {
            if (IIM.validateFile(file)) {
                IIM.processFile(file);
            }
        });
    };

    IIM.validateFile = function(file) {
        // Check file size
        if (file.size > IIM.config.maxFileSize) {
            IIM.showNotification(`File ${file.name} is too large (max ${IIM.config.maxFileSize / 1024 / 1024}MB)`, 'error');
            return false;
        }
        
        // Check file format
        const extension = '.' + file.name.split('.').pop().toLowerCase();
        if (!IIM.config.supportedFormats.includes(extension)) {
            IIM.showNotification(`File format ${extension} is not supported`, 'error');
            return false;
        }
        
        return true;
    };

    IIM.processFile = function(file) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('sessionId', IIM.state.currentSession);
        
        // Show processing indicator
        IIM.showProcessingIndicator(true);
        
        fetch(`${IIM.config.apiEndpoint}/files/process`, {
            method: 'POST',
            body: formData
        }).then(response => response.json())
          .then(data => {
              IIM.addToContext(data);
              IIM.showNotification(`File ${file.name} processed successfully`, 'success');
          })
          .catch(error => {
              console.error('File processing failed:', error);
              IIM.showNotification(`Failed to process ${file.name}`, 'error');
          })
          .finally(() => {
              IIM.showProcessingIndicator(false);
          });
    };

    // UI Helpers
    IIM.toggleSidebar = function() {
        const sidebar = document.querySelector('.iim-sidebar');
        if (sidebar) {
            sidebar.classList.toggle('collapsed');
            
            // Save preference
            localStorage.setItem('iim_sidebar_collapsed', sidebar.classList.contains('collapsed'));
        }
    };

    IIM.showNotification = function(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `iim-notification iim-notification-${type}`;
        notification.textContent = message;
        
        // Add to DOM
        document.body.appendChild(notification);
        
        // Animate in
        setTimeout(() => notification.classList.add('show'), 10);
        
        // Remove after 3 seconds
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    };

    IIM.showProcessingIndicator = function(show) {
        IIM.state.isProcessing = show;
        
        const indicator = document.querySelector('.iim-processing-indicator');
        if (indicator) {
            indicator.style.display = show ? 'flex' : 'none';
        }
        
        // Disable/enable input
        const sendButton = document.querySelector('.iim-send-button');
        if (sendButton) {
            sendButton.disabled = show;
        }
    };

    IIM.clearWorkspace = function() {
        const messagesArea = document.querySelector('.iim-messages-area');
        if (messagesArea) {
            messagesArea.innerHTML = '';
        }
    };

    IIM.appendMessage = function(message, animate = true) {
        const messagesArea = document.querySelector('.iim-messages-area');
        if (!messagesArea) return;
        
        const messageElement = IIM.createMessageElement(message);
        
        if (animate) {
            messageElement.style.opacity = '0';
            messagesArea.appendChild(messageElement);
            setTimeout(() => {
                messageElement.style.opacity = '1';
            }, 10);
        } else {
            messagesArea.appendChild(messageElement);
        }
        
        // Scroll to bottom
        messagesArea.scrollTop = messagesArea.scrollHeight;
    };

    IIM.createMessageElement = function(message) {
        const div = document.createElement('div');
        div.className = `iim-message ${message.role}`;
        div.innerHTML = `
            <div class="iim-message-avatar">
                ${message.role === 'user' ? 'U' : 'AI'}
            </div>
            <div class="iim-message-content">
                ${IIM.formatMessageContent(message.content)}
            </div>
        `;
        return div;
    };

    IIM.formatMessageContent = function(content) {
        // Basic markdown-like formatting
        return content
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/```(.*?)```/gs, '<pre><code>$1</code></pre>')
            .replace(/`(.*?)`/g, '<code>$1</code>')
            .replace(/\n/g, '<br>');
    };

    // Utility Functions
    IIM.generateId = function() {
        return 'iim-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
    };

    IIM.loadState = function() {
        // Load sessions from local storage
        const savedSessions = localStorage.getItem('iim_sessions');
        if (savedSessions) {
            try {
                IIM.state.sessions = JSON.parse(savedSessions);
            } catch (error) {
                console.error('Failed to load sessions:', error);
            }
        }
        
        // Load sidebar preference
        const sidebarCollapsed = localStorage.getItem('iim_sidebar_collapsed') === 'true';
        if (sidebarCollapsed) {
            const sidebar = document.querySelector('.iim-sidebar');
            if (sidebar) {
                sidebar.classList.add('collapsed');
            }
        }
    };

    IIM.setupAutoSave = function() {
        setInterval(() => {
            if (IIM.state.currentSession) {
                IIM.saveCurrentSession();
            }
        }, IIM.config.autoSaveInterval);
    };

    IIM.updateConnectionStatus = function(status) {
        IIM.state.connectionStatus = status;
        
        const indicator = document.querySelector('.iim-status-indicator');
        if (indicator) {
            indicator.classList.remove('warning', 'error');
            if (status === 'error') {
                indicator.classList.add('error');
            } else if (status === 'disconnected') {
                indicator.classList.add('warning');
            }
        }
    };

    IIM.initializeUIComponents = function() {
        // Initialize Bootstrap tooltips if available
        if (typeof bootstrap !== 'undefined') {
            const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        }
    };

    // Export public API
    IIM.api = {
        init: IIM.init,
        createSession: IIM.createNewSession,
        selectSession: IIM.selectSession,
        selectModel: IIM.selectModel,
        sendMessage: function(content) {
            // Public API for sending messages
            if (window.DotNet) {
                return DotNet.invokeMethodAsync('IIM.Components', 'SendMessage', content);
            }
        },
        getState: function() {
            return IIM.state;
        }
    };

})(window.IIM);

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.IIM.init);
} else {
    window.IIM.init();
}