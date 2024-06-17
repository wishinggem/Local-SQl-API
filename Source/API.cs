using System.Diagnostics;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.IO;
using Chisato.SQL;
using Chisato.Authentication;
using Chisato.File;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace SQLApi
{
    internal class Program
    {
        private static string jsonInputFilePath = "C:\\SQLAPI\\JsonInput.json";

        static void Main(string[] args) //cli args are passed in format "QueryType", '{username: \"user\", password: \"password\", .....}'
        {
            string type = args[0];

            QueryType queryType = QueryType.Read;
            bool init = false;

            switch (type)
            {
                case "Create":
                    queryType = QueryType.Create;
                    break;

                case "Update":
                    queryType = QueryType.Update;
                    break;

                case "Delete":
                    queryType = QueryType.Delete;
                    break;

                case "Read":
                    queryType = QueryType.Read;
                    break;

                case "Initialise":
                    init = true;
                    InitAPI();
                    break;

                default:
                    break;
            }

            if (init)
            {
                return;
            }


            if (!init)
            {
                QueryAuthentication auth = new QueryAuthentication();
                bool unauthenticated = false;
                UserCreate userC = null;
                UserUpdate userU = null;
                UserDelete userD = null;
                LoginUser login = null;

                switch (queryType)
                {
                    case QueryType.Create:
                        userC = JsonHandler.DeserializeJsonFile<UserCreate>(jsonInputFilePath);

                        if (auth.IsAuthenticated(new List<string> { userC.username, userC.password }))
                        {
                            Console.WriteLine("Bad Perameter");
                            unauthenticated = true;
                        }

                        break;
                    case QueryType.Update:
                        userU = JsonHandler.DeserializeJsonFile<UserUpdate>(jsonInputFilePath);

                        if (auth.IsAuthenticated(new List<string> { userU.username, userU.newUsername, userU.newPassword }))
                        {
                            Console.WriteLine("Bad Perameter");
                            unauthenticated = true;
                        }

                        break;
                    case QueryType.Delete:
                        userD = JsonHandler.DeserializeJsonFile<UserDelete>(jsonInputFilePath);

                        if (auth.IsAuthenticated(new List<string> { userD.username }))
                        {
                            Console.WriteLine("Bad Perameter");
                            unauthenticated = true;
                        }

                        break;
                    case QueryType.Read:
                        login = JsonHandler.DeserializeJsonFile<LoginUser>(jsonInputFilePath);

                        if (auth.IsAuthenticated(new List<string> { login.username, login.password }))
                        {
                            Console.WriteLine("Bad Perameter");
                            unauthenticated = true;
                        }

                        break;
                    default:
                        break;
                }

                if (!unauthenticated)
                {
                    API api = new API();
                    SQLServer apiServer = api.InitialiseServerConnecton();
                    string query = "";
                    PasswordHandler crypt = new PasswordHandler();

                    switch (queryType)
                    {
                        case QueryType.Create:

                            string salt = "";
                            string hash = crypt.Hash(userC.password, out salt);

                            apiServer.RunQuerySingle($"INSERT INTO Users VALUES ({Guid.NewGuid().ToString()}, '{userC.username}', '{"Unused"}', '{hash}', {salt})");

                            break;
                        case QueryType.Update:

                            string newSalt = "";
                            string newHash = crypt.Hash(userU.newPassword, out newSalt);

                            string userID = apiServer.RunQuerySingle($"SELECT userID FROM Users WHERE username = '{userU.username}'");

                            apiServer.RunQuerySingle($"UPDATE Users SET username = '{userU.newUsername}', email = 'Unused', password = '{newHash}', salt = '{newSalt}' WHERE userID = {userID}");

                            break;
                        case QueryType.Delete:

                            apiServer.RunQuerySingle($"DELETE FROM Users WHERE username = '{userD.username}'");

                            break;
                        case QueryType.Read:

                            string existingSalt = apiServer.RunQuerySingle($"SELECT salt FROM Users WHERE username = '{login.username}'");
                            string existingHash = apiServer.RunQuerySingle($"SELECT password FROM Users WHERE username = '{login.username}'");

                            bool match = crypt.CompareHash(login.password, existingHash, existingSalt);

                            if (match)
                            {
                                Console.WriteLine("authenticated");
                            }

                            break;
                        default:
                            break;
                    }
                }
            }
        }
            

        private static void InitAPI()
        {
            Directory.CreateDirectory("C:\\SQLAPI");
            File.Create(jsonInputFilePath);
        }
    }

    public class LoginUser
    {
        public string username;
        public string password;
    }

    public class UserCreate
    {
        public string username;
        public string password;
    }

    public class UserDelete
    {
        public string username;
    }

    public class UserUpdate
    {
        public string username;
        public string newUsername;
        public string newPassword;
    }

    public class API
    {
        private SQLServer server;

        public SQLServer InitialiseServerConnecton()
        {
            server = new SQLServer("", "", "", "");
            return server;
        }
    }

    public class QueryAuthentication
    {
        public List<string> sqlCommands = new List<string> { "Select", "Update", "Delete", "Group By", "Order By", "Having" };

        public bool IsAuthenticated(List<string> userInput)
        {
            foreach (string input in userInput)
            {
                foreach (string command in sqlCommands)
                {
                    if (input.ToLower().Contains(command.ToLower()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public enum QueryType
    {
        Create,
        Update,
        Delete,
        Read
    }
}

namespace Chisato
{
    namespace SQL
    {
        public class SQLServer
        {
            SqlConnectionStringBuilder builder;

            public SQLServer(string userID, string password, string dataSource, string database)
            {
                builder = new SqlConnectionStringBuilder();
                builder.DataSource = dataSource;
                builder.UserID = userID;
                builder.Password = password;
                builder.InitialCatalog = database;
            }

            public string RunQuerySingle(string sql)
            {
                if (builder == null)
                {
                    return "Sql Connection Builder Failed";
                }

                try
                {
                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        List<string> resultList = new List<string>();
                        connection.Open();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        resultList.Add(reader[i].ToString());
                                    }
                                }
                            }
                        }

                        connection.Close();
                        string sqlResult = "";
                        foreach (string item in resultList)
                        {
                            sqlResult += string.Concat(item);
                        }
                        return sqlResult;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            public List<string> RunQueryList(string sql)
            {
                if (builder == null)
                {
                    return new List<string> { "Sql Connection Builder Failed" };
                }

                try
                {
                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        List<string> resultList = new List<string>();
                        connection.Open();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        resultList.Add(reader[i].ToString());
                                    }
                                }
                            }
                        }
                        connection.Close();
                        return resultList;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            public List<int> RunQueryIntList(string sql)
            {
                if (builder == null)
                {
                    return new List<int> { 0 };
                }

                try
                {
                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        List<int> resultList = new List<int>();
                        connection.Open();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        resultList.Add(int.Parse(reader[i].ToString()));
                                    }
                                }
                            }
                        }
                        connection.Close();
                        return resultList;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            public string ParseStringList(List<string> list)
            {
                string res = "";
                foreach (var item in list)
                {
                    res += string.Concat(item, ", ");
                }
                return res;
            }
        }
    }

    namespace File
    {
        public static class JsonHandler
        {
            public static void SerializeJsonFile<T>(string filePath, T objectToWrite, bool append = false)
            {
                TextWriter writer = null;
                try
                {
                    var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
                    writer = new StreamWriter(filePath, append);
                    writer.Write(contentsToWriteToFile);
                }
                finally
                {
                    if (writer != null)
                        writer.Close();
                }
            }

            public static T DeserializeJsonFile<T>(string filePath) where T : new()
            {
                TextReader reader = null;
                try
                {
                    reader = new StreamReader(filePath);
                    var fileContents = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(fileContents);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
        }

        public static class FileHandler
        {
            public static void RenameFile(this FileInfo fileInfo, string newName)
            {
                fileInfo.MoveTo(fileInfo.Directory.FullName + "\\" + newName);
            }
        }
    }

    namespace Authentication
    {
        public class PasswordHandler()
        {
            public string Hash(string password, out string salt)
            {
                byte[] saltBytes = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(saltBytes);
                }
                salt = Convert.ToBase64String(saltBytes);

                string saltedPassword = string.Concat(password, saltBytes);
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                    string hash = Convert.ToBase64String(hashBytes);
                    return hash;
                }
            }

            public string HashWithSalt(string password, string salt)
            {
                string saltedPassword = string.Concat(password, salt);
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                    string hash = Convert.ToBase64String(hashBytes);
                    return hash;
                }
            }

            public bool CompareHash(string password, string existingHash, string salt)
            {
                string saltedPassword = String.Concat(password, salt);

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                    string newHash = Convert.ToBase64String(hashBytes);
                    // Compare the new hash with the existing hash
                    return newHash == existingHash;
                }
            }
        }
    }
}
