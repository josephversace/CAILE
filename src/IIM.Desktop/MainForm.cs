using IIM.Components;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Windows.Forms;

namespace IIM.Desktop;

public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainForm> _logger;
    private BlazorWebView blazorWebView;

    public MainForm(IServiceProvider serviceProvider, ILogger<MainForm> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        InitializeComponent();
        InitializeBlazor();

        _logger.LogInformation("IIM Desktop application started");
    }

    private void InitializeComponent()
    {
        // Form settings - minimal chrome, let Blazor handle everything
        this.Text = "Intelligent Investigation Machine";
        this.Size = new Size(1600, 900);
        this.MinimumSize = new Size(1024, 768);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Maximized;

        // Remove default Windows borders for a more modern look (optional)
        // this.FormBorderStyle = FormBorderStyle.None;

        // Set icon if available
        try
        {
            var iconPath = "app.ico";
            if (System.IO.File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not load application icon");
        }

        // Handle closing event for any cleanup
        this.FormClosing += MainForm_FormClosing;
        this.Load += MainForm_Load;

        // Add keyboard handler for app-wide shortcuts
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;
    }

    private void InitializeBlazor()
    {
        blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            Services = _serviceProvider,
            HostPage = "wwwroot/index.html"
        };

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();

        blazorWebView.RootComponents.Add<App>("#app");


        // Handle Blazor web view events
        blazorWebView.BlazorWebViewInitializing += BlazorWebView_Initializing;
        blazorWebView.BlazorWebViewInitialized += BlazorWebView_Initialized;

        this.Controls.Add(blazorWebView);
    }


    // Event Handlers
    private void MainForm_Load(object sender, EventArgs e)
    {
        _logger.LogInformation("Main form loaded");
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Let Blazor handle the confirmation dialog if needed
        // Or implement a simple check here
        _logger.LogInformation("Application closing");
    }

    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle global keyboard shortcuts
        // F11 for fullscreen
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullScreen();
            e.Handled = true;
        }
        // F12 for developer tools (debug only)
        else if (e.KeyCode == Keys.F12)
        {
#if DEBUG
            blazorWebView?.WebView?.CoreWebView2?.OpenDevToolsWindow();
            e.Handled = true;
#endif
        }
        // Escape to exit fullscreen
        else if (e.KeyCode == Keys.Escape && this.FormBorderStyle == FormBorderStyle.None)
        {
            ExitFullScreen();
            e.Handled = true;
        }
    }

    private void BlazorWebView_Initializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        _logger.LogDebug("BlazorWebView initializing");
    }

    private void BlazorWebView_Initialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        _logger.LogDebug("BlazorWebView initialized");

        // Inject any JavaScript interop handlers if needed
#if DEBUG
        // Enable context menu in debug mode
        e.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        e.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
        // Disable right-click context menu in production
        e.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        e.WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
    }

    // Helper Methods
    private void ToggleFullScreen()
    {
        if (this.WindowState == FormWindowState.Maximized && this.FormBorderStyle == FormBorderStyle.None)
        {
            ExitFullScreen();
        }
        else
        {
            EnterFullScreen();
        }
    }

    private void EnterFullScreen()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
    }

    private void ExitFullScreen()
    {
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.WindowState = FormWindowState.Maximized;
    }

    // Public methods that can be called from Blazor via JavaScript interop
    public void ShowNativeMessageBox(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public string ShowNativeFileDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*",
            Title = "Select File"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            return dialog.FileName;
        }

        return string.Empty;
    }
}