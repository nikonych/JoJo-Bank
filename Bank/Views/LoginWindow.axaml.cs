using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Bank.Data;

namespace Bank.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    public void Login_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text;
        var password = PasswordBox.Text;

        var databaseManager = new DatabaseManager("localhost", "bank", "postgres", "root");
        bool isValidUser = databaseManager.CheckEmployeeCredentials(username, password);

        if (isValidUser)
        {   
            Console.WriteLine("good");
            // Создаем и открываем MainWindow
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Закрываем текущее окно авторизации
            this.Close();
        }
        else
        {
            // Отображаем сообщение об ошибке
            ErrorTextBlock.Text = "Неправильное имя пользователя или пароль";
            ErrorTextBlock.IsVisible = true;

        }
    }

}