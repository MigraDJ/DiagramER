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
    // Validate input to prevent XSS
    if (typeof message !== 'string') {
        message = String(message);
    }

    // Sanitize message - remove any HTML tags
    const div = document.createElement('div');
    div.textContent = message; // textContent automatically escapes HTML
    const safeMessage = div.innerHTML;

    // Create a simple notification element
    const notification = document.createElement('div');
    notification.innerHTML = safeMessage;
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
        max-width: 400px;
        word-break: break-word;
    `;

    document.body.appendChild(notification);

    // Add animation if not already present
    if (!document.head.querySelector('style[data-diagramer]')) {
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
        style.setAttribute('data-diagramer', 'true');
        document.head.appendChild(style);
    }

    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-in-out';
        setTimeout(() => {
            if (notification.parentNode) {
                document.body.removeChild(notification);
            }
        }, 300);
    }, duration);
}

export function showErrorNotification(message, duration = 4000) {
    // Validate and sanitize input - only show safe error messages
    if (typeof message !== 'string') {
        message = String(message);
    }

    // Remove sensitive information from error messages
    message = message
        .replace(/at (.*?)(?:\n|$)/g, '') // Remove stack traces
        .replace(/Error:/g, 'Error') // Normalize error prefix
        .substring(0, 200); // Limit message length

    // Sanitize message
    const div = document.createElement('div');
    div.textContent = message;
    const safeMessage = div.innerHTML;

    const notification = document.createElement('div');
    notification.innerHTML = safeMessage;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background-color: #dc3545;
        color: white;
        padding: 16px;
        border-radius: 4px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        z-index: 10000;
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Roboto", "Oxygen", "Ubuntu", "Cantarell", sans-serif;
        font-size: 14px;
        animation: slideIn 0.3s ease-in-out;
        max-width: 400px;
        word-break: break-word;
    `;

    document.body.appendChild(notification);

    if (!document.head.querySelector('style[data-diagramer]')) {
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideIn {
                from { transform: translateX(400px); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
            @keyframes slideOut {
                from { transform: translateX(0); opacity: 1; }
                to { transform: translateX(400px); opacity: 0; }
            }
        `;
        style.setAttribute('data-diagramer', 'true');
        document.head.appendChild(style);
    }

    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-in-out';
        setTimeout(() => {
            if (notification.parentNode) {
                document.body.removeChild(notification);
            }
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

async function ensureScriptLoaded(url, globalName, sriHash = null) {
    if (globalName && window[globalName]) {
        return;
    }

    await new Promise((resolve, reject) => {
        const key = globalName || url;
        const existing = document.querySelector(`script[data-diagramer-lib="${key}"]`);

        if (existing) {
            if (!globalName || window[globalName]) {
                resolve();
                return;
            }

            existing.addEventListener('load', () => resolve(), { once: true });
            existing.addEventListener('error', () => reject(new Error(`Failed to load script: ${url}`)), { once: true });
            return;
        }

        const script = document.createElement('script');
        script.src = url;
        script.async = true;
        script.dataset.diagramerLib = key;

        // Add Subresource Integrity (SRI) for security
        if (sriHash) {
            script.integrity = sriHash;
        }

        // CORS for security
        script.crossOrigin = 'anonymous';

        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load script: ${url}`));
        document.head.appendChild(script);
    });
}

async function ensureExportLibraries(format) {
    // SRI hashes for external libraries - verify integrity
    await ensureScriptLoaded(
        'https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js',
        'html2canvas',
        'sha384-S2YVLmvOEGmTjV9VH3Z6qJuKvGUqoKVPg8q/FWLZD5GKy7gH3Yc4I/QZY+5i5R1Q'
    );

    if (format === 'pdf') {
        await ensureScriptLoaded(
            'https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js',
            'jspdf',
            'sha384-1bOUX7/TQHGMc7WfIEaWKq8DlNfC/z3SyC7ZQ38LSVZxvXtl3Q7p3xvkDwrTsAP'
        );
    }
}

function downloadDataUrl(dataUrl, fileName) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

async function renderDiagramCanvas(elementId, format = 'png') {
    await ensureExportLibraries(format);

    const surface = document.getElementById(elementId);
    if (!surface) {
        throw new Error('Diagram surface not found');
    }

    const canvasArea = surface.closest('.canvas-area');
    if (!canvasArea) {
        throw new Error('Canvas area not found');
    }

    const zoomControls = canvasArea.querySelector('.zoom-controls');
    const previousVisibility = zoomControls ? zoomControls.style.visibility : null;
    const previousCanvasAreaBg = canvasArea.style.backgroundColor;

    if (zoomControls) {
        zoomControls.style.visibility = 'hidden';
    }

    // Use transparent background for all export formats
    const backgroundColor = 'transparent';

    try {
        // Temporarily set the canvas background to transparent for rendering
        canvasArea.style.backgroundColor = 'transparent';

        return await window.html2canvas(canvasArea, {
            backgroundColor,
            scale: 2,
            useCORS: true,
            width: canvasArea.clientWidth,
            height: canvasArea.clientHeight
        });
    } finally {
        if (zoomControls) {
            zoomControls.style.visibility = previousVisibility || '';
        }
        canvasArea.style.backgroundColor = previousCanvasAreaBg;
    }
}

async function getExportBlob(canvas, format) {
    if (format === 'png') {
        return await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
    }

    if (format === 'jpg') {
        const jpgCanvas = document.createElement('canvas');
        jpgCanvas.width = canvas.width;
        jpgCanvas.height = canvas.height;
        const ctx = jpgCanvas.getContext('2d');
        if (!ctx) {
            throw new Error('Unable to create JPG export context');
        }

        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, jpgCanvas.width, jpgCanvas.height);
        ctx.drawImage(canvas, 0, 0);
        return await new Promise(resolve => jpgCanvas.toBlob(resolve, 'image/jpeg', 0.92));
    }

    if (format === 'pdf') {
        await ensureExportLibraries('pdf');
        const { jsPDF } = window.jspdf;
        const pdf = new jsPDF({
            orientation: canvas.width >= canvas.height ? 'landscape' : 'portrait',
            unit: 'px',
            format: [canvas.width, canvas.height]
        });

        pdf.addImage(canvas.toDataURL('image/png'), 'PNG', 0, 0, canvas.width, canvas.height);
        return pdf.output('blob');
    }

    throw new Error(`Unsupported export format: ${format}`);
}

function getDefaultFileName(format) {
    if (format === 'pdf') {
        return 'diagram.pdf';
    }

    if (format === 'jpg') {
        return 'diagram.jpg';
    }

    return 'diagram.png';
}

function getPickerOptions() {
    return {
        suggestedName: 'diagram.png',
        types: [
            {
                description: 'PNG Image',
                accept: { 'image/png': ['.png'] }
            },
            {
                description: 'JPEG Image',
                accept: { 'image/jpeg': ['.jpg', '.jpeg'] }
            },
            {
                description: 'PDF Document',
                accept: { 'application/pdf': ['.pdf'] }
            }
        ]
    };
}

function inferFormatFromHandle(handle) {
    const name = (handle.name || '').toLowerCase();
    if (name.endsWith('.pdf')) {
        return 'pdf';
    }

    if (name.endsWith('.jpg') || name.endsWith('.jpeg')) {
        return 'jpg';
    }

    return 'png';
}

export async function exportDiagramWithSavePicker(elementId) {
    if (window.showSaveFilePicker) {
        const handle = await window.showSaveFilePicker(getPickerOptions());
        const format = inferFormatFromHandle(handle);
        const blob = await getExportBlob(await renderDiagramCanvas(elementId, format), format);
        if (!blob) {
            throw new Error('Failed to create export file');
        }

        const writable = await handle.createWritable();
        await writable.write(blob);
        await writable.close();
        return;
    }

    const formatInput = window.prompt('Enter export format: png, jpg, or pdf', 'png');
    if (!formatInput) {
        return;
    }

    const format = formatInput.toLowerCase();
    const blob = await getExportBlob(await renderDiagramCanvas(elementId, format), format);
    if (!blob) {
        throw new Error('Failed to create export file');
    }

    const url = URL.createObjectURL(blob);
    try {
        downloadDataUrl(url, getDefaultFileName(format));
    } finally {
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
}

// Cookie management functions with security enhancements
const MAX_COOKIE_SIZE = 3584; // 3.5KB - safe limit for browsers

export function setCookie(name, value, hours = 2) {
    // Validate cookie size to prevent DoS attacks
    const encodedValue = encodeURIComponent(value);
    const cookieString = name + "=" + encodedValue;

    if (cookieString.length > MAX_COOKIE_SIZE) {
        console.warn(`Cookie "${name}" exceeds maximum size (${cookieString.length}/${MAX_COOKIE_SIZE}). Not storing.`);
        return false;
    }

    const date = new Date();
    date.setTime(date.getTime() + (hours * 60 * 60 * 1000));
    const expires = "expires=" + date.toUTCString();

    // Enhanced security flags: Secure (HTTPS only), SameSite (CSRF protection)
    document.cookie = cookieString + ";" + expires + ";path=/;Secure;SameSite=Strict";
    return true;
}

export function getCookie(name) {
    const nameEQ = name + "=";
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        cookie = cookie.trim();
        if (cookie.indexOf(nameEQ) === 0) {
            try {
                return decodeURIComponent(cookie.substring(nameEQ.length));
            } catch (e) {
                console.error(`Failed to decode cookie "${name}": ${e.message}`);
                return null;
            }
        }
    }
    return null;
}

export function deleteCookie(name) {
    document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;Secure;SameSite=Strict";
}

export function checkCookieNotificationShown() {
    return getCookie('diagramer_cookie_notification_shown') === 'true';
}

export function setCookieNotificationShown() {
    // Set cookie notification flag to persist for the session/browser
    document.cookie = 'diagramer_cookie_notification_shown=true;path=/;Secure;SameSite=Strict';
}
