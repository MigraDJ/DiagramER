// wwwroot/js/theme.js

const THEME_STORAGE_KEY = 'diagram-er-theme';
const THEMES = {
    LIGHT: 'light',
    DARK: 'dark',
    AUTO: 'auto'
};

export function getTheme() {
    const saved = localStorage.getItem(THEME_STORAGE_KEY);
    return saved || THEMES.AUTO;
}

export function setTheme(theme) {
    if (Object.values(THEMES).includes(theme)) {
        localStorage.setItem(THEME_STORAGE_KEY, theme);
    }
}

export function applyTheme(theme) {
    const html = document.documentElement;

    if (theme === THEMES.LIGHT) {
        html.setAttribute('data-bs-theme', 'light');
        html.style.colorScheme = 'light';
    } else if (theme === THEMES.DARK) {
        html.setAttribute('data-bs-theme', 'dark');
        html.style.colorScheme = 'dark';
    } else if (theme === THEMES.AUTO) {
        // Check system preference
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const systemTheme = prefersDark ? THEMES.DARK : THEMES.LIGHT;
        html.setAttribute('data-bs-theme', systemTheme);
        html.style.colorScheme = 'light dark';

        // Listen for system theme changes
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        mediaQuery.addEventListener('change', (e) => {
            if (localStorage.getItem(THEME_STORAGE_KEY) === THEMES.AUTO) {
                const newTheme = e.matches ? THEMES.DARK : THEMES.LIGHT;
                html.setAttribute('data-bs-theme', newTheme);
                html.style.colorScheme = 'light dark';
            }
        });
    }
}

export function getSystemTheme() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? THEMES.DARK : THEMES.LIGHT;
}

export function getEffectiveTheme() {
    const saved = localStorage.getItem(THEME_STORAGE_KEY) || THEMES.AUTO;
    if (saved === THEMES.AUTO) {
        return getSystemTheme();
    }
    return saved;
}
