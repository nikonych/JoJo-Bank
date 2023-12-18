using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Bank.Data;

namespace Bank.ViewModels
{
    public sealed class CreditViewModel : INotifyPropertyChanged
    {
        private string loanTerm;
        private string resultText;

        public string BorrowerName { get; set; }
        public string ContactInfo { get; set; }
        public string ClientID { get; set; }
        public string FullName { get; set; }
        public string Income { get; set; }
        public string LoanAmount { get; set; }
        public string LoanPurpose { get; set; }

        public CreditViewModel()
        {
            Users = new ObservableCollection<UserInfo>
            {
                new UserInfo { FullName = "Иван Иванов", Income = "50000", ContactInfo = "ivanov@example.com" },
                new UserInfo { FullName = "Мария Петрова", Income = "60000", ContactInfo = "mpetrova@example.com" },
                new UserInfo { FullName = "Алексей Сидоров", Income = "55000", ContactInfo = "asidorov@example.com" }
            };
            
        }


        public string LoanTerm
        {
            get => loanTerm;
            set
            {
                if (loanTerm != value)
                {
                    loanTerm = value;
                    OnPropertyChanged(nameof(LoanTerm));
                    CalculateResult();
                }
            }
        }

        public string ResultText
        {
            get => resultText;
            set
            {
                if (resultText != value)
                {
                    resultText = value;
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }
        private ObservableCollection<UserInfo> users;

        public ObservableCollection<UserInfo> Users
        {
            get
            {
                Console.WriteLine(users.Count);
                return users;
            }
            set
            {
                if (users != value)
                {
                    users = value;
                    OnPropertyChanged(nameof(Users));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CalculateResult()
        {
            if (decimal.TryParse(LoanAmount, out decimal amount) &&
                int.TryParse(LoanTerm, out int term))
            {
                var interestRate = GetInterestRate(term);
                var monthlyInterestRate = interestRate / 12;
                var monthlyPayment = (amount * monthlyInterestRate) /
                                     (1 - (decimal)Math.Pow(1 + (double)monthlyInterestRate, -term));

                ResultText = $"Процентная ставка: {interestRate:P2}\n" +
                             $"Ежемесячный платеж: {monthlyPayment:C2}";
            }
            else
            {
                ResultText = "Некорректный ввод данных";
            }
        }

        private decimal GetInterestRate(int loanTerm)
        {
            if (loanTerm <= 12) // до 1 года
            {
                return 0.10m; // 10%
            }
            else if (loanTerm <= 36) // от 1 до 3 лет
            {
                return 0.12m; // 12%
            }
            else // свыше 3 лет
            {
                return 0.15m; // 15%
            }
        }
    }
    
    
}
