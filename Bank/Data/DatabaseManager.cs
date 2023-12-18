using Npgsql;
using System;
using System.Collections.Generic;
using NpgsqlTypes;


namespace Bank.Data;

public class DatabaseManager
{
    private string connectionString;

    public DatabaseManager(string host, string database, string username, string password)
    {
        connectionString = $"Host={host};Username={username};Password={password};Database={database}";
    }

    public void TestConnection()
    {
        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Successfully connected to the database.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to the database: {ex.Message}");
        }
    }

    public bool CheckEmployeeCredentials(string username, string password)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            using (var command =
                   new NpgsqlCommand(
                       "SELECT COUNT(*) FROM BankEmployees WHERE username = @username AND password = @password",
                       connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password",
                    password); // В реальном приложении используйте хеширование паролей
                var result = (long)command.ExecuteScalar();
                return result > 0;
            }
        }
    }


    public void SaveUser(string fullName, string contactInfo, decimal income)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            using (var command =
                   new NpgsqlCommand(
                       "INSERT INTO Users (fullName, contactInfo, income) VALUES (@fullName, @contactInfo, @income)",
                       connection))
            {
                command.Parameters.AddWithValue("@fullName", fullName);
                command.Parameters.AddWithValue("@contactInfo", contactInfo);
                command.Parameters.AddWithValue("@income", income);
                command.ExecuteNonQuery();
            }
        }
    }

    public void SaveCredit(string borrowerName, string loanAmount, string loanTerm, string loanPurpose,
        decimal interestRate)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            // Предполагаем, что borrowerName связан с userId через таблицу Users
            var getUserIdCommand = new NpgsqlCommand("SELECT userId FROM Users WHERE fullName = @fullName", connection);
            getUserIdCommand.Parameters.AddWithValue("@fullName", borrowerName);
            var userId = getUserIdCommand.ExecuteScalar();

            if (userId != null)
            {
                var command = new NpgsqlCommand(@"
                INSERT INTO Credits (userId, amount, term, purpose, interestRate)
                VALUES (@userId, @amount, @term, @purpose, @interestRate)", connection);

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@amount", Decimal.Parse(loanAmount));
                command.Parameters.AddWithValue("@term", Int32.Parse(loanTerm));
                command.Parameters.AddWithValue("@purpose", loanPurpose);
                command.Parameters.AddWithValue("@interestRate", interestRate);

                command.ExecuteNonQuery();
            }
        }
    }

    public bool HasExistingLoans(string borrowerName)
    {
        using var connection = new NpgsqlConnection(connectionString);

        try
        {
            connection.Open();

            using var cmd = new NpgsqlCommand(@"
                    SELECT COUNT(*) FROM Credits
                    WHERE applicationId IN (
                        SELECT applicationId FROM CreditApplication
                        WHERE userId = (
                            SELECT userId FROM User
                            WHERE fullName = @borrowerName
                        )
                    )", connection);

            // Устанавливаем значение параметра borrowerName и его тип явно
            cmd.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "@borrowerName",
                NpgsqlDbType = NpgsqlDbType.Text, // Указываем тип параметра как текст
                Value = borrowerName // Устанавливаем значение параметра
            });

            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                int loanCount = Convert.ToInt32(result);
                return loanCount > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            // Обработка ошибки подключения к базе данных или выполнения запроса
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
            return false;
        }
    }


    public void SaveOrUpdateUser(string borrowerName, string contactInfo, decimal income)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            var command = new NpgsqlCommand(@"
            INSERT INTO Users (fullName, contactInfo, income)
            VALUES (@fullName, @contactInfo, @income)
            ON CONFLICT (fullName)
            DO UPDATE SET contactInfo = EXCLUDED.contactInfo, income = EXCLUDED.income", connection);

            command.Parameters.AddWithValue("@fullName", borrowerName);
            command.Parameters.AddWithValue("@contactInfo", contactInfo);
            command.Parameters.AddWithValue("@income", income);

            command.ExecuteNonQuery();
        }
    }
    
    public List<UserInfo> GetUsers()
    {
        List<UserInfo> users = new List<UserInfo>();

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            using (var command = new NpgsqlCommand("SELECT fullname, income, contactinfo FROM Users", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fullName = reader["fullname"].ToString();
                        string income = reader["income"].ToString();
                        string contactInfo = reader["contactinfo"].ToString();

                        UserInfo user = new UserInfo {FullName = fullName, Income = income, ContactInfo = contactInfo};

                        users.Add(user);
                    }
                }
            }
        }

        return users;
    }
    
    public List<CreditApplication> GetCreditApplications()
    {
        List<CreditApplication> applications = new List<CreditApplication>();

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            using (var command = new NpgsqlCommand("SELECT applicationid, userid, requestedamount, purpose, status FROM CreditApplications", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string applicationId = reader["applicationid"].ToString();
                        string userId = reader["userid"].ToString();
                        string requestedAmount = reader["requestedamount"].ToString();
                        string purpose = reader["purpose"].ToString();
                        string status = reader["status"].ToString();

                        CreditApplication application = new CreditApplication
                        {
                            ApplicationId = applicationId,
                            UserId = userId,
                            RequestedAmount = requestedAmount,
                            Purpose = purpose,
                            Status = status
                        };

                        applications.Add(application);
                    }
                }
            }
        }

        return applications;
    }

    public bool AddCreditApplication(string document, string fullname, string contactinfo, string income,
        decimal requestedAmount, string purpose)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            // Проверяем, есть ли пользователь с заданным документом в таблице Users
            using (var userCheckCommand =
                   new NpgsqlCommand("SELECT userId FROM Users WHERE document = @document", connection))
            {
                userCheckCommand.Parameters.AddWithValue("@document", document);
                var userId = userCheckCommand.ExecuteScalar();

                if (userId != null)
                {
                    // Пользователь существует, создаем заявку на кредит
                    using (var command = new NpgsqlCommand(@"
                INSERT INTO CreditApplications (userId, requestedAmount, purpose, status)
                VALUES (@userId, @requestedAmount, @purpose, @status)", connection))
                    {
                        command.Parameters.AddWithValue("@userId", (int)userId);
                        command.Parameters.AddWithValue("@requestedAmount", requestedAmount);
                        command.Parameters.AddWithValue("@purpose", purpose);
                        command.Parameters.AddWithValue("@status", "Ожидание");

                        command.ExecuteNonQuery();
                    }

                    return true;
                }
                else
                {
                    // Пользователь не существует, создаем нового пользователя
                    using (var createUserCommand = new NpgsqlCommand(@"
                INSERT INTO Users (document, fullname, contactinfo, income, username)
                VALUES (@document, @fullname, @contactinfo, @income, @username)
                RETURNING userId", connection))
                    {
                        createUserCommand.Parameters.AddWithValue("@document", document);
                        createUserCommand.Parameters.AddWithValue("@fullname", fullname);
                        createUserCommand.Parameters.AddWithValue("@contactinfo", contactinfo);
                        createUserCommand.Parameters.AddWithValue("@income", income);
                        createUserCommand.Parameters.AddWithValue("@username", fullname);
                        userId = createUserCommand.ExecuteScalar();

                        if (userId != null)
                        {
                            // Пользователь успешно создан, создаем заявку на кредит
                            using (var command = new NpgsqlCommand(@"
                        INSERT INTO CreditApplications (userId, requestedAmount, purpose, status)
                        VALUES (@userId, @requestedAmount, @purpose, @status)", connection))
                            {
                                command.Parameters.AddWithValue("@userId", (int)userId);
                                command.Parameters.AddWithValue("@requestedAmount", requestedAmount);
                                command.Parameters.AddWithValue("@purpose", purpose);
                                command.Parameters.AddWithValue("@status", "Ожидание");

                                command.ExecuteNonQuery();
                            }

                            return true;
                        }
                        else
                        {
                            Console.WriteLine("Ошибка при создании пользователя.");
                            return false;
                        }
                    }
                }
            }
        }
    }
}

public class UserInfo
{

    public string FullName { get; set; }
    public string Income { get; set; }
    public string ContactInfo { get; set; }
}

public class CreditApplication
{
    public string ApplicationId { get; set; }
    public string UserId { get; set; }
    public string RequestedAmount { get; set; }
    public string Purpose { get; set; }
    public string Status { get; set; }
}
