// wwwroot/js/diagramer.js

export function initDragDrop(dotnetHelper) {
    const canvas = document.querySelector('.canvas-area');
    const dragItems = document.querySelectorAll('[data-drag-type]');

    // Track the drag type being dragged
    let currentDragType = null;

    // Setup drag items
    dragItems.forEach(item => {
        item.addEventListener('dragstart', (e) => {
            currentDragType = item.dataset.dragType;
            e.dataTransfer.effectAllowed = 'copy';
            e.dataTransfer.setData('text/plain', currentDragType);
            item.style.opacity = '0.7';
        });

        item.addEventListener('dragend', (e) => {
            item.style.opacity = '1';
            currentDragType = null;
        });
    });

    // Setup canvas
    if (canvas) {
        canvas.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
            canvas.style.backgroundColor = '#e3f2fd';
        });

        canvas.addEventListener('dragleave', (e) => {
            if (e.target === canvas) {
                canvas.style.backgroundColor = '';
            }
        });

        canvas.addEventListener('drop', (e) => {
            e.preventDefault();
            canvas.style.backgroundColor = '';

            const dragType = e.dataTransfer.getData('text/plain');
            if (dragType && dragType === 'table') {
                // Calculate position relative to canvas
                const rect = canvas.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                // Call Blazor method
                dotnetHelper.invokeMethodAsync('OnDragDropTable', x, y);
            }
        });
    }
}

export function copyToClipboard(text) {
    // Use modern Clipboard API
    if (navigator.clipboard && navigator.clipboard.writeText) {
        return navigator.clipboard.writeText(text).then(() => {
            return true;
        }).catch(() => {
            // Fallback to older method
            return copyToClipboardFallback(text);
        });
    } else {
        // Fallback for older browsers
        return Promise.resolve(copyToClipboardFallback(text));
    }
}

function copyToClipboardFallback(text) {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);

    try {
        textarea.select();
        const successful = document.execCommand('copy');
        document.body.removeChild(textarea);
        return successful;
    } catch (err) {
        document.body.removeChild(textarea);
        return false;
    }
}

export function showNotification(message, duration = 2000) {
    // Create a simple notification element
    const notification = document.createElement('div');
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background-color: #4caf50;
        color: white;
        padding: 16px;
        border-radius: 4px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        z-index: 10000;
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Roboto", "Oxygen", "Ubuntu", "Cantarell", sans-serif;
        font-size: 14px;
        animation: slideIn 0.3s ease-in-out;
    `;

    document.body.appendChild(notification);

    // Add animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
    `;
    if (!document.head.querySelector('style[data-diagramer]')) {
        style.setAttribute('data-diagramer', 'true');
        document.head.appendChild(style);
    }

    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-in-out';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, duration);
}

export function downloadFile(byteArray, fileName, mimeType) {
    // Convert byte array to blob
    const blob = new Blob([new Uint8Array(byteArray)], { type: mimeType });

    // Create a download link
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;

    // Trigger download
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Clean up
    URL.revokeObjectURL(url);
}

export function getFiles(fileInput) {
    return fileInput?.files || [];
}

export function readFileAsText(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            resolve(e.target?.result || '');
        };
        reader.onerror = (e) => {
            reject(new Error('Failed to read file'));
        };
        reader.readAsText(file);
    });
}

export function triggerFileInput() {
    const input = document.getElementById('jsonFileInput');
    if (input) {
        // Reset the value so the same file can be selected again
        input.value = '';
        input.click();
    } else {
        console.error('File input element not found');
    }
}

export function readUploadedFile() {
    return new Promise((resolve, reject) => {
        try {
            const input = document.getElementById('jsonFileInput');

            if (!input) {
                console.error('File input element not found');
                reject(new Error('File input element not found'));
                return;
            }

            if (!input.files || input.files.length === 0) {
                console.warn('No file selected');
                resolve('');
                return;
            }

            const file = input.files[0];

            if (!file) {
                console.warn('File is null');
                resolve('');
                return;
            }

            const reader = new FileReader();

            reader.onload = (e) => {
                const result = e.target?.result;
                if (typeof result === 'string') {
                    resolve(result);
                } else {
                    resolve('');
                }
            };

            reader.onerror = (e) => {
                console.error('FileReader error:', e);
                reject(new Error('Failed to read file: ' + e.target?.error?.message || 'Unknown error'));
            };

            reader.onabort = () => {
                console.warn('File reading aborted');
                reject(new Error('File reading was aborted'));
            };

            reader.readAsText(file);
        } catch (error) {
            console.error('Error in readUploadedFile:', error);
            reject(error);
        }
    });
}
