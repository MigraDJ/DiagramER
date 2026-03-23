using Microsoft.JSInterop;

namespace DiagramER.Services
{
    public class ThemeService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private string _currentTheme = "auto";
        private IJSObjectReference? _jsModule;
        private bool _disposed = false;
        private bool _isCircuitConnected = true;

        public event Action? OnThemeChanged;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (_disposed || !_isCircuitConnected)
                    return;

                _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/theme.js");
                _currentTheme = await _jsModule.InvokeAsync<string>("getTheme");
                await ApplyThemeAsync(_currentTheme);
            }
            catch (JSDisconnectedException)
            {
                _isCircuitConnected = false;
                Console.WriteLine("Circuit disconnected during theme initialization");
            }
            catch (OperationCanceledException)
            {
                _isCircuitConnected = false;
                Console.WriteLine("Theme initialization cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing theme service: {ex.Message}");
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            try
            {
                if (_disposed || !_isCircuitConnected)
                    return;

                if (_jsModule != null && (theme == "light" || theme == "dark" || theme == "auto"))
                {
                    _currentTheme = theme;
                    await _jsModule.InvokeVoidAsync("setTheme", theme);
                    await ApplyThemeAsync(theme);
                    OnThemeChanged?.Invoke();
                }
            }
            catch (JSDisconnectedException)
            {
                _isCircuitConnected = false;
                Console.WriteLine("Circuit disconnected while setting theme");
            }
            catch (OperationCanceledException)
            {
                _isCircuitConnected = false;
                Console.WriteLine("Theme change cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting theme: {ex.Message}");
            }
        }

        public async Task ApplyThemeAsync(string theme)
        {
            try
            {
                if (_disposed || !_isCircuitConnected || _jsModule == null)
                    return;

                await _jsModule.InvokeVoidAsync("applyTheme", theme);
            }
            catch (JSDisconnectedException)
            {
                _isCircuitConnected = false;
                Console.WriteLine("Circuit disconnected while applying theme");
            }
            catch (OperationCanceledException)
            {
                _isCircuitConnected = false;
                Console.WriteLine("Theme application cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
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
                    // Expected during application shutdown or browser close
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
                    Console.WriteLine($"Error disposing theme service: {ex.Message}");
                }
            }
        }
    }
}
