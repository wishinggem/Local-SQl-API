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
