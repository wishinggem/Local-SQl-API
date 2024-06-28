// this can be copy pasted to the backend if using asp.net and then all you need to do is include the namespace Chisato.Shell and then call Shell.RunCommandSQLAPI(query); and the query will be ran and the response will be returned as a string

namespace Chisato.Shell
{
    public static class Shell
    {
        public static string RunCommand(string cmd)
        {
            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    Arguments = "/c " + cmd,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            return output;
        }

        public static string RunCommandSQLAPI(string query)
        {
            string queryFilePath = "C:\\SQLAPI\\Query.json";
            string apiCmd = "C:\\SQLapi_res\\SQLApi.exe";
            Query querySave = new Query();
            querySave.query = query;

            JsonHandler.SerializeJsonFile(queryFilePath, querySave);

            RunCommand(apiCmd);

            Thread.Sleep(2); //make thread wait for a response

            string output = JsonHandler.DeserializeJsonFile<Query>(queryFilePath).query;

            return output;
        }
    }

    [Serializable]
    public class Query
    {
        public string query;
    }
}
