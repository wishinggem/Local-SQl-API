# Local-SQl-API
this is an API that runs local acting as a secure bridge between a webserver and an sql server without needing an externally hosted api

To initialise 
- place in a directory called 'SQLapi_res'
- Make this directory accessable by the account that the iis server runs (by default this would be Users)
- navigate to this directory through cmd
- then Exexute this command 'SQLApi.exe Initialise 'DataSource' 'Database' 'username' 'password'
  replace 'info' with require connection details of sql server
- this will create a new subdirectory to the root directory of the server set the access to this directory to the account the iis server runs

To interface with api
- implement the code in the Backend.cs file in Source into the backend scripting of the web app
- or include the dll required file for the dll file in the dll directory and to call use name space Chisato.Shell anmd call Shell.RunCommandSQLAPI(string query) and the result will be returned if any result

Note
- the api can currently only handle single result queries
- an api usually is recomended over this sort of aproach however if an api is not necceseraly required this will work in a very similar way
