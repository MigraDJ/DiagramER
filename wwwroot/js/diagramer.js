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

export function showErrorNotification(message, duration = 4000) {
    const notification = document.createElement('div');
    notification.textContent = message;
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
    `;

    document.body.appendChild(notification);

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
    if (!document.head.querySelector('style[data-diagramer]')) {
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

async function ensureScriptLoaded(url, globalName) {
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
        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load script: ${url}`));
        document.head.appendChild(script);
    });
}

async function ensureExportLibraries(format) {
    await ensureScriptLoaded('https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js', 'html2canvas');

    if (format === 'pdf') {
        await ensureScriptLoaded('https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js', 'jspdf');
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

async function renderDiagramCanvas(elementId) {
    await ensureExportLibraries('png');

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
    if (zoomControls) {
        zoomControls.style.visibility = 'hidden';
    }

    const backgroundColor = (getComputedStyle(document.documentElement).getPropertyValue('--bs-body-bg') || '#ffffff').trim() || '#ffffff';
    try {
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
    const canvas = await renderDiagramCanvas(elementId);

    if (window.showSaveFilePicker) {
        const handle = await window.showSaveFilePicker(getPickerOptions());
        const format = inferFormatFromHandle(handle);
        const blob = await getExportBlob(canvas, format);
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
    const blob = await getExportBlob(canvas, format);
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
