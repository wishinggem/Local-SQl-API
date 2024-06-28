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
using System.Text.Json.Serialization;

namespace SQLApi
{
    internal class Program
    {
        private static string credsFilePath = "C:\\SQLAPI\\Creds.json";
        private static string queryFilePath = "C:\\SQLAPI\\Query.json";
        private static string parentDir = "C:\\SQLAPI";

        static void Main(string[] args) //cli args are passed in format "QueryType", '{username: \"user\", password: \"password\", .....}'
        {
            string password = "";
            string username = "";
            string dataSource = "";
            string database = "";
            string query = "";
            if (args.Length >= 1)
            {
                query = args[0];
            }
            bool init = false;

            switch (query)
            {
                case "Initialise":
                    init = true;
                    if (args.Length > 1)
                    {
                        dataSource = args[1];
                        database = args[2];
                        username = args[3];
                        password = args[4];
                    }
                    InitAPI(dataSource, database, username, password);
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
                bool unauthenticated = false;

                

                if (!unauthenticated)
                {
                    API api = new API();
                    SQLServer apiServer = api.InitialiseServerConnecton(credsFilePath);

                    string queryLoad = JsonHandler.DeserializeJsonFile<Query>(queryFilePath).query;

                    string response = apiServer.RunQuerySingle(queryLoad);
                    Query res = new Query();
                    res.query = response;
                    JsonHandler.SerializeJsonFile<Query>(queryFilePath, res);
                }
            }
        }
            

        private static void InitAPI(string dataSource, string database, string username, string password)
        {
            ServerCreds creds = new ServerCreds();
            creds.dataSource = dataSource;
            creds.database = database;
            creds.username = username;
            creds.password = password;
            Directory.CreateDirectory(parentDir);
            File.Create(queryFilePath);
            JsonHandler.SerializeJsonFile(credsFilePath, creds);
        }
    }

    [Serializable]
    public class Query
    {
        public string query;
    }

    [Serializable]
    public class ServerCreds
    {
        public string dataSource;
        public string database;
        public string username;
        public string password;
    }

    public class API
    {
        public SQLServer InitialiseServerConnecton(string credsPath)
        {
            ServerCreds creds = JsonHandler.DeserializeJsonFile<ServerCreds>(credsPath);
            return new SQLServer(creds.username, creds.password, creds.dataSource, creds.database);
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
