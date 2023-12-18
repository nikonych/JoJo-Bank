using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Bank.Data;
using Bank.ViewModels;

namespace Bank.Views;

public partial class MainWindow : Window
{
    private DispatcherTimer messageTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CreditViewModel();
        if (DataContext is CreditViewModel viewModel)
        {
            var databaseManager = new DatabaseManager("localhost", "bank", "postgres", "root");
            viewModel.Users = new ObservableCollection<UserInfo>(databaseManager.GetUsers());
            var users = databaseManager.GetUsers();

            for (int i = 0; i < users.Count; i++)
            {
                // Добавляем новую строку в Grid для каждого пользователя
                UsersListGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Для FullName
                AddTextBlockToGrid(users[i].FullName, i + 1, 0);

                // Для Income
                AddTextBlockToGrid(users[i].Income, i + 1, 1);

                // Для ContactInfo
                AddTextBlockToGrid(users[i].ContactInfo, i + 1, 2);
            }

            var applications = databaseManager.GetCreditApplications();
            for (int i = 0; i < applications.Count; i++)
            {
                // Добавляем новую строку в Grid для каждой заявки
                CreditApplicationsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Для каждого поля заявки
                AddTextBlockToGrid1(applications[i].ApplicationId, i + 1, 0);
                AddTextBlockToGrid1(applications[i].UserId, i + 1, 1);
                AddTextBlockToGrid1(applications[i].RequestedAmount, i + 1, 2);
                AddTextBlockToGrid1(applications[i].Purpose, i + 1, 3);
                AddTextBlockToGrid1(applications[i].Status, i + 1, 4);
            }
        }


        // Инициализируем таймер
        messageTimer = new DispatcherTimer();
        messageTimer.Tick += MessageTimer_Tick;
        messageTimer.Interval = TimeSpan.FromSeconds(5); // Устанавливаем интервал в 5 секунд
    }

    private void AddTextBlockToGrid1(string text, int row, int column)
    {
        var border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
        var textBlock = new TextBlock { Text = text, Margin = new Thickness(5) };
        border.Child = textBlock;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        CreditApplicationsGrid.Children.Add(border);
    }

    private void AddTextBlockToGrid(string text, int row, int column)
    {
        var border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
        var textBlock = new TextBlock { Text = text, Margin = new Thickness(5) };
        border.Child = textBlock;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        UsersListGrid.Children.Add(border);
    }


    private void ShowMessage(bool status)
    {
        if (status)
        {
            // В случае успеха
            StatusTextBox.Text = "Успешно";
            StatusTextBox.Foreground = Brushes.Green;
        }
        else
        {
            // В случае ошибки
            StatusTextBox.Text = "Ошибка";
            StatusTextBox.Foreground = Brushes.Red;
        }

        // Запускаем таймер
        messageTimer.Start();
    }

    private void MessageTimer_Tick(object sender, EventArgs e)
    {
        // Скрываем сообщение
        StatusTextBox.Text = string.Empty;

        // Останавливаем таймер
        messageTimer.Stop();
    }

    public void IssueLoan_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CreditViewModel viewModel)
        {
            var databaseManager = new DatabaseManager("localhost", "bank", "postgres", "root");
            // Получите значения из полей формы и передайте их методу AddCreditApplication
            string document = viewModel.ClientID;
            string fullName = viewModel.FullName;
            string contactInfo = viewModel.ContactInfo;
            string income = viewModel.Income;
            decimal requestedAmount = decimal.Parse(viewModel.LoanAmount);
            string purpose = viewModel.LoanPurpose;

            // Вызов метода AddCreditApplication
            bool isGood =
                databaseManager.AddCreditApplication(document, fullName, contactInfo, income, requestedAmount, purpose);
            ShowMessage(isGood);
        }

        ;
    }


    private decimal GetInterestRate(int loanTerm)
    {
        if (loanTerm <= 12) // до 1 года
        {
            return 0.10m;
        }
        else if (loanTerm <= 36) // от 1 до 3 лет
        {
            return 0.12m;
        }
        else // свыше 3 лет
        {
            return 0.15m;
        }
    }

    private void Calculate_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CreditViewModel viewModel)
        {
            // Предполагаем, что LoanAmount и LoanTerm являются числовыми значениями
            if (decimal.TryParse(viewModel.LoanAmount, out decimal loanAmount) &&
                int.TryParse(viewModel.LoanTerm, out int loanTerm))
            {
                decimal interestRate = GetInterestRate(loanTerm);

                var monthlyInterestRate = interestRate / 12;
                var monthlyPayment = (loanAmount * monthlyInterestRate) /
                                     (1 - (decimal)Math.Pow(1 + (double)monthlyInterestRate, -loanTerm));
                var totalAmount = monthlyPayment * loanTerm;

                viewModel.ResultText = $"Процентная ставка: {interestRate:P2}\n" +
                                       $"Ежемесячный платеж: {monthlyPayment:C2}\n" +
                                       $"Итоговая сумма к возврату: {totalAmount:C2}";
            }
            else
            {
                viewModel.ResultText = "Ошибка: неверный формат суммы или срока кредита.";
            }
        }
    }


    private void LoanTypeRadioButton_Click(object sender, RoutedEventArgs e)
    {
        RadioButton radioButton = (RadioButton)sender;

        // Определяем выбранный вид кредита и устанавливаем описание в соответствии с ним
        if (radioButton == ConsumerLoanRadioButton)
        {
            LoanDescriptionTextBlock.Text =
                "Потребительский кредит: Предоставляется для финансирования личных потребностей и покупок, таких как бытовая техника, электроника, отпуск, медицинские расходы и т.д. Заемщик не обязан предоставлять банку обоснование цели использования средств.";
        }
        else if (radioButton == MortgageLoanRadioButton)
        {
            LoanDescriptionTextBlock.Text = "Ипотечный кредит: Используется для покупки недвижимости.";
        }
        else if (radioButton == AutoLoanRadioButton)
        {
            LoanDescriptionTextBlock.Text = "Автокредит: Выдается для приобретения автомобиля.";
        }
        else if (radioButton == BusinessLoanRadioButton)
        {
            LoanDescriptionTextBlock.Text =
                "Бизнес-кредит: Направлен на финансирование предпринимательской деятельности.";
        }
    }

    private void ClearFields_Click(object sender, RoutedEventArgs e)
    {
        // Очистить поля ввода
        ClientIDTextBox.Text = string.Empty;
        IncomeTextBox.Text = string.Empty;
        LoanAmountTextBox.Text = string.Empty;
        LoanTermTextBox.Text = string.Empty;
        LoanPurposeTextBox.Text = string.Empty;

        // Сбросить выбор типа кредита
        ConsumerLoanRadioButton.IsChecked = true;

        // Очистить текстовый блок с результатом
        // ResultText.Text = string.Empty;
    }

    private void OnCalculateButtonClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}