using Npgsql;
using Task.Integration.Data.Models;
using Task.Integration.Data.Models.Models;

namespace Task.Connector
{
    public class ConnectorDb : IConnector
    {
        private string _connectionString;
        private NpgsqlConnection _connection;

        public interface ILogger  
        { 
            void Log(string message);
            void LogError(string message);
        }

        public class ConsoleLogger : ILogger
        {
            public void Log(string message)
            {
                Console.WriteLine($"INFO: {message}");
            }

            public void LogError(string message)
            {
                Console.WriteLine($"ERROR: {message}");
            }
        }

        public class UserToCreate
        {
            public string Login { get; set; }
            public string FullName { get; set; } // Пример замены LastName, FirstName и других
            public string PhoneNumber { get; set; }
            public bool IsLead { get; set; }
        }


        public ConnectorDb()
        {
            // Конструктор без параметров
        }

        public ILogger Logger { get; set; }
        Integration.Data.Models.ILogger IConnector.Logger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void StartUp(string connectionString)
        {
            _connectionString = connectionString;

            try
            {
                // Создаем и открываем подключение
                _connection = new NpgsqlConnection(_connectionString);
                _connection.Open();
                
                // Логируем успешное подключение
                Logger?.Log("Database connection established.");
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Logger?.LogError($"Failed to connect to the database. Error: {ex.Message}");

                // Пробрасываем исключение дальше
                throw new InvalidOperationException("Database connection could not be established.", ex);
            }
        }

        public void CreateUser(UserToCreate user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            if (string.IsNullOrEmpty(user.Login))
            {
                throw new ArgumentException("User login cannot be null or empty", nameof(user.Login));
            }

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    var query = @"INSERT INTO ""User"" (login, full_name, phone_number, is_lead) 
                                VALUES (@login, @fullName, @phoneNumber, @isLead)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("login", user.Login);
                        command.Parameters.AddWithValue("fullName", user.FullName); // Используйте доступные свойства
                        command.Parameters.AddWithValue("phoneNumber", user.PhoneNumber);
                        command.Parameters.AddWithValue("isLead", user.IsLead);

                        var result = command.ExecuteNonQuery();
                        if (result > 0)
                        {
                            Logger?.Log($"User {user.Login} created successfully.");
                        }
                        else
                        {
                            Logger?.Log($"User {user.Login} creation failed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Log($"An error occurred while creating user {user.Login}: {ex.Message}");
                    throw;
                }
            }
        }


        public IEnumerable<Property> GetAllProperties()
        {
            var properties = new List<Property>();

            // Пример строки подключения к базе данных PostgreSQL
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand("SELECT name, description FROM Properties", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader.GetString(0);
                                var description = reader.GetString(1);

                                // Создание нового объекта Property и добавление его в список
                                var property = new Property(name, description);
                                properties.Add(property);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Logger?.Log($"Error while retrieving properties: {ex.Message}");
                    throw; // Перебрасывание исключения дальше
                }
            }

            return properties;
        }



        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            var userProperties = new List<UserProperty>();

            // Пример строки подключения к базе данных PostgreSQL
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // SQL-запрос для получения свойств пользователя по логину
                    string query = @"
                        SELECT p.name, up.value
                        FROM UserProperties up
                        JOIN Properties p ON up.property_id = p.id
                        WHERE up.user_login = @UserLogin";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("UserLogin", userLogin);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader.GetString(0);
                                var value = reader.GetString(1);

                                // Создание нового объекта UserProperty и добавление его в список
                                var userProperty = new UserProperty(name, value);
                                userProperties.Add(userProperty);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Logger?.Log($"Error while retrieving user properties for {userLogin}: {ex.Message}");
                    throw; // Перебрасывание исключения дальше
                }
            }

            return userProperties;
        }


        public bool IsUserExists(string userLogin)
        {
            // Пример строки подключения к базе данных PostgreSQL
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // SQL-запрос для проверки существования пользователя
                    string query = "SELECT COUNT(1) FROM Users WHERE user_login = @UserLogin";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("UserLogin", userLogin);

                        // Выполняем запрос и проверяем, есть ли строки
                        var result = (long)command.ExecuteScalar();
                        
                        // Если результат больше 0, пользователь существует
                        return result > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Logger?.Log($"Error while checking if user exists: {ex.Message}");
                    throw; // Перебрасывание исключения дальше
                }
            }
        }


        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            // Пример строки подключения к базе данных PostgreSQL
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Используем транзакцию для обеспечения целостности данных
                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var property in properties)
                        {
                            // SQL-запрос для обновления свойства пользователя
                            string query = "UPDATE UserProperties SET value = @Value WHERE user_login = @UserLogin AND property_name = @PropertyName";

                            using (var command = new NpgsqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("Value", property.Value);
                                command.Parameters.AddWithValue("UserLogin", userLogin);
                                command.Parameters.AddWithValue("PropertyName", property.Name);

                                // Выполняем запрос для обновления свойства
                                command.ExecuteNonQuery();
                            }
                        }

                        // Фиксируем транзакцию
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Logger?.Log($"Error while updating user properties: {ex.Message}");
                    throw; // Перебрасываем исключение дальше
                }
            }
        }


        public IEnumerable<Permission> GetAllPermissions()
        {
            var permissions = new List<Permission>();
            // Пример строки подключения к базе данных PostgreSQL
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // SQL-запрос для получения всех прав
                    string query = "SELECT id, name, description FROM Permissions";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Создание объекта Permission из данных в базе данных
                                var permission = new Permission(
                                    reader.GetString(reader.GetOrdinal("id")),
                                    reader.GetString(reader.GetOrdinal("name")),
                                    reader.GetString(reader.GetOrdinal("description"))
                                );

                                permissions.Add(permission);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Logger?.Log($"Error while retrieving all permissions: {ex.Message}");
                    throw; // Перебрасываем исключение дальше
                }
            }

            return permissions;
        }


        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
            
            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Проверяем, существует ли пользователь
                    string userCheckQuery = "SELECT COUNT(*) FROM Users WHERE login = @login";
                    using (var userCheckCommand = new NpgsqlCommand(userCheckQuery, connection))
                    {
                        userCheckCommand.Parameters.AddWithValue("login", userLogin);
                        var userExists = (long)userCheckCommand.ExecuteScalar() > 0;

                        if (!userExists)
                        {
                            Logger?.Log($"User with login '{userLogin}' does not exist.");
                            throw new Exception($"User with login '{userLogin}' does not exist.");
                        }
                    }

                    // Добавляем права пользователю
                    foreach (var rightId in rightIds)
                    {
                        // Проверяем, существует ли право
                        string rightCheckQuery = "SELECT COUNT(*) FROM Permissions WHERE id = @rightId";
                        using (var rightCheckCommand = new NpgsqlCommand(rightCheckQuery, connection))
                        {
                            rightCheckCommand.Parameters.AddWithValue("rightId", rightId);
                            var rightExists = (long)rightCheckCommand.ExecuteScalar() > 0;

                            if (!rightExists)
                            {
                                Logger?.Log($"Permission with ID '{rightId}' does not exist.");
                                continue; // Или выбросите исключение, если нужно
                            }
                        }

                        // Вставляем запись в таблицу связи
                        string insertQuery = "INSERT INTO UserRequestRight (user_login, right_id) VALUES (@userLogin, @rightId)";
                        using (var insertCommand = new NpgsqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("userLogin", userLogin);
                            insertCommand.Parameters.AddWithValue("rightId", rightId);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Log($"Error while adding user permissions: {ex.Message}");
                    throw; // Перебрасываем исключение дальше
                }
            }
        }


        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Проверяем, существует ли пользователь
                    string userCheckQuery = "SELECT COUNT(*) FROM Users WHERE login = @login";
                    using (var userCheckCommand = new NpgsqlCommand(userCheckQuery, connection))
                    {
                        userCheckCommand.Parameters.AddWithValue("login", userLogin);
                        var userExists = (long)userCheckCommand.ExecuteScalar() > 0;

                        if (!userExists)
                        {
                            Logger?.Log($"User with login '{userLogin}' does not exist.");
                            throw new Exception($"User with login '{userLogin}' does not exist.");
                        }
                    }

                    // Удаляем права у пользователя
                    foreach (var rightId in rightIds)
                    {
                        // Проверяем, существует ли право
                        string rightCheckQuery = "SELECT COUNT(*) FROM Permissions WHERE id = @rightId";
                        using (var rightCheckCommand = new NpgsqlCommand(rightCheckQuery, connection))
                        {
                            rightCheckCommand.Parameters.AddWithValue("rightId", rightId);
                            var rightExists = (long)rightCheckCommand.ExecuteScalar() > 0;

                            if (!rightExists)
                            {
                                Logger?.Log($"Permission with ID '{rightId}' does not exist.");
                                continue; // Или выбросите исключение, если нужно
                            }
                        }

                        // Удаляем запись из таблицы связи
                        string deleteQuery = "DELETE FROM UserRequestRight WHERE user_login = @userLogin AND right_id = @rightId";
                        using (var deleteCommand = new NpgsqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("userLogin", userLogin);
                            deleteCommand.Parameters.AddWithValue("rightId", rightId);
                            deleteCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Log($"Error while removing user permissions: {ex.Message}");
                    throw; // Перебрасываем исключение дальше
                }
            }
        }


        public IEnumerable<string> GetUserPermissions(string userLogin)
        {
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
            var permissions = new List<string>();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Проверяем, существует ли пользователь
                    string userCheckQuery = "SELECT COUNT(*) FROM Users WHERE login = @login";
                    using (var userCheckCommand = new NpgsqlCommand(userCheckQuery, connection))
                    {
                        userCheckCommand.Parameters.AddWithValue("login", userLogin);
                        var userExists = (long)userCheckCommand.ExecuteScalar() > 0;

                        if (!userExists)
                        {
                            Logger?.Log($"User with login '{userLogin}' does not exist.");
                            throw new Exception($"User with login '{userLogin}' does not exist.");
                        }
                    }

                    // Получаем права пользователя
                    string getPermissionsQuery = @"
                        SELECT p.id
                        FROM UserRequestRight urr
                        JOIN Permissions p ON urr.right_id = p.id
                        WHERE urr.user_login = @userLogin";
                    
                    using (var getPermissionsCommand = new NpgsqlCommand(getPermissionsQuery, connection))
                    {
                        getPermissionsCommand.Parameters.AddWithValue("userLogin", userLogin);
                        using (var reader = getPermissionsCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                permissions.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Log($"Error while retrieving user permissions: {ex.Message}");
                    throw; // Перебрасываем исключение дальше
                }
            }

            return permissions;
        }


        public void CreateUser(Integration.Data.Models.Models.UserToCreate user)
        {
            var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Проверяем, существует ли уже пользователь с таким логином
                    string userCheckQuery = "SELECT COUNT(*) FROM Users WHERE login = @login";
                    using (var userCheckCommand = new NpgsqlCommand(userCheckQuery, connection))
                    {
                        userCheckCommand.Parameters.AddWithValue("login", user.Login);
                        var userExists = (long)userCheckCommand.ExecuteScalar() > 0;

                        if (userExists)
                        {
                            Logger?.Log($"User with login '{user.Login}' already exists.");
                            throw new Exception($"User with login '{user.Login}' already exists.");
                        }
                    }

                    // Вставляем нового пользователя
                    string insertUserQuery = @"
                        INSERT INTO Users (login, last_name, first_name, middle_name, telephone_number, is_lead)
                        VALUES (@login, @lastName, @firstName, @middleName, @telephoneNumber, @isLead)";
                    
                    using (var insertUserCommand = new NpgsqlCommand(insertUserQuery, connection))
                    {
                        insertUserCommand.Parameters.AddWithValue("login", user.Login);
                        insertUserCommand.Parameters.AddWithValue("lastName", user.LastName);
                        insertUserCommand.Parameters.AddWithValue("firstName", user.FirstName);
                        insertUserCommand.Parameters.AddWithValue("middleName", user.MiddleName);
                        insertUserCommand.Parameters.AddWithValue("telephoneNumber", user.TelephoneNumber);
                        insertUserCommand.Parameters.AddWithValue("isLead", user.IsLead);
                        insertUserCommand.ExecuteNonQuery();
                    }

                    // Вставляем пароль пользователя
                    string insertPasswordQuery = @"
                        INSERT INTO Passwords (login, password_hash)
                        VALUES (@login, @passwordHash)";
                    
                    using (var insertPasswordCommand = new NpgsqlCommand(insertPasswordQuery, connection))
                    {
                        insertPasswordCommand.Parameters.AddWithValue("login", user.Login);
                        insertPasswordCommand.Parameters.AddWithValue("passwordHash", user.PasswordHash); // Убедитесь, что у вас есть PasswordHash в модели
                        insertPasswordCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Log($"Error while creating user: {ex.Message}");
                    throw; // Перебрасываем исключение дальше
                }
            }
        }

    }
}