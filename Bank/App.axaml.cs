using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Bank.Data;
using Bank.ViewModels;
using Bank.Views;

namespace Bank;

public partial class App : Application
{
    
    public static DatabaseManager DatabaseManager { get; private set; }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Инициализация DatabaseManager
        DatabaseManager = new DatabaseManager("localhost", "bank", "postgres", "root");
        DatabaseManager.TestConnection();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LoginWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}