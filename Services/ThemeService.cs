using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace DiagramER.Services
{
    public class ThemeService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<ThemeService>? _logger;
        private string _currentTheme = "auto";
        private IJSObjectReference? _jsModule;
        private bool _disposed = false;
        private bool _isCircuitConnected = true;

        public event Action? OnThemeChanged;

        public ThemeService(IJSRuntime jsRuntime, ILogger<ThemeService>? logger = null)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            if (_disposed || !_isCircuitConnected)
                return;

            try
            {
                _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/theme.js");
                _currentTheme = await _jsModule.InvokeAsync<string>("getTheme");
                await ApplyThemeAsync(_currentTheme);
            }
            catch (JSDisconnectedException)
            {
                _isCircuitConnected = false;
                LogError("Circuit disconnected during theme initialization");
            }
            catch (OperationCanceledException)
            {
                _isCircuitConnected = false;
                LogError("Theme initialization cancelled");
            }
            catch (Exception ex)
            {
                LogError($"Error initializing theme service: {ex.Message}");
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            if (_disposed || !_isCircuitConnected)
                return;

            if (!IsValidTheme(theme) || _jsModule == null)
                return;

            try
            {
                _currentTheme = theme;
                await _jsModule.InvokeVoidAsync("setTheme", theme);
                await ApplyThemeAsync(theme);
                OnThemeChanged?.Invoke();
            }
            catch (JSDisconnectedException)
            {
                _isCircuitConnected = false;
                LogError("Circuit disconnected while setting theme");
            }
            catch (OperationCanceledException)
            {
                _isCircuitConnected = false;
                LogError("Theme change cancelled");
            }
            catch (Exception ex)
            {
                LogError($"Error setting theme: {ex.Message}");
            }
        }

        public async Task ApplyThemeAsync(string theme)
        {
            if (_disposed || !_isCircuitConnected || _jsModule == null)
                return;

            try
            {
                await _jsModule.InvokeVoidAsync("applyTheme", theme);
            }
            catch (JSDisconnectedException)
            {
                _isCircuitConnected = false;
                LogError("Circuit disconnected while applying theme");
            }
            catch (OperationCanceledException)
            {
                _isCircuitConnected = false;
                LogError("Theme application cancelled");
            }
            catch (Exception ex)
            {
                LogError($"Error applying theme: {ex.Message}");
            }
        }

        public string GetCurrentTheme() => _currentTheme;

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            _isCircuitConnected = false;

            if (_jsModule is not null)
            {
                try
                {
                    await _jsModule.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // Expected during application shutdown
                }
                catch (OperationCanceledException)
                {
                    // Expected during application shutdown
                }
                catch (ObjectDisposedException)
                {
                    // Expected during application shutdown
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing theme service: {ex.Message}");
                }
            }
        }

        private bool IsValidTheme(string theme) => theme is "light" or "dark" or "auto";

        private void LogError(string message)
        {
            _logger?.LogError(message);
        }
    }
}
