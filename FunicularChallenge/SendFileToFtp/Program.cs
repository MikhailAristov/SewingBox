using FtpLib;
using FunicularSwitch;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

//Welcome!
//Try to refactor the SendFileToFtp program (FtpLib is an 'external' library and cannot be changed). 
//Make Main return 0 in success and 1 in error case and give useful error information to console in case of invalid inputs or ftp errors. 
//Use the Result type from Funicular.Switch nuget package, introduction is found on github (https://github.com/bluehands/Funicular-Switch). 
//Useful helper methods are Map, Bind, Match to build the pipeline, Validate to perform checks on input and Try to turn exceptions into Result(s).

//Goal is to collect as much error information as possible and to respect 'Single Level of Abstraction' and 'Dou not Repeat Yourself' principles.
namespace SendFileToFtp
{
    public class Program
    {
        /// <summary>
        /// Sends file to ftp server
        /// </summary>
        /// <param name="pathOfFileToSend"></param>
        /// <param name="targetFolder"></param>
        /// <param name="host"></param>
        /// <param name="user"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        /// <param name="base64PrivateKey"></param>
        /// <param name="privateKeyFilePath"></param>
        public static async Task<int> Main(
            string pathOfFileToSend,
            string targetFolder,
            string host,
            string user,
            int port = 22,
            string? password = null,
            string? base64PrivateKey = null,
            string? privateKeyFilePath = null) => await Result
            // Aggregate the inputs into a single tuple
            .Aggregate(
                // Convert host name to URI
                GetHostUri(host),
                // Validate port number
                port.Validate(ValidatePort),
                // Compose FTP credentials
                await GetCredentials(user, password, base64PrivateKey, privateKeyFilePath),
                // Load the file to send
                await LoadFile(pathOfFileToSend, "file to send"),
                // Validate target directory
                targetFolder
                    .Validate(IsNotEmpty, "target folder")
                    .Validate(ValidateTargetDir))
                
            // Feed the inputs into the main function to try to upload the file
            .Try<(Uri, int, FtpCredentials, byte[], string)>(
                async t => await UploadFile(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5),
                e => $"Failed to upload:{Environment.NewLine}{e.Message}")

            // Assess the outcome
            .Match(
                ok => {
                    Console.WriteLine("File uploaded successfully");
                    return 0;
                },
                error => {
                    Console.WriteLine(error);
                    return 1;
                });

        public static IEnumerable<string> IsNotEmpty(string? str, string descriptor)
        {
            // IsNullOrWhiteSpace() is more robust than IsNullOrEmpty()
            if(string.IsNullOrWhiteSpace(str))
                yield return $"No {descriptor} specified";
        }

        public static Result<Uri> GetHostUri(string path) => path
            .Validate(IsNotEmpty, "host address")
            .TryBind<string, Uri>(
                p => new UriBuilder(p).Uri,
                e => $"Bad host address ({path}): {e.Message}");

        private static IEnumerable<string> ValidatePort(int port)
        {
            if(port <= 0) // not "greater or equal to"
                yield return $"Invalid port {port}. Port has to be greater than zero.";
        }

        public static async Task<Result<FtpCredentials>> GetCredentials(string user, string? pwd, string? k64, string? kPath)
        {
            return ImmutableList.Create(
                // Option 1: Login with password
                GetPasswordCred(user, pwd),
                // Option 2: Login with private key
                GetPrivateKeyCred(user,
                    ImmutableList.Create(
                        // Option 2a: Key passed as base64
                        LoadKeyFromBase64(k64),
                        // Option 2b: Key passed as file path
                        await LoadFile(kPath, "private key file"))
                    .FirstOk(() => "")) // Error message empty, because list guaranteed non-empty
            ).FirstOk(() => ""); // Ditto
        }

        private static Result<FtpCredentials> GetPasswordCred(string user, string? pwd) => pwd
            // Perform basic validation on the password
            .Validate(ValidatePassword)
            // Aggregate with user name
            .Aggregate(user.Validate(IsNotEmpty, "user name"))
            // Create an FTP credential using the password, if any
            .Bind<FtpCredentials>(t => FtpCredentials.Password(t.Item2, $"{t.Item1}"));

        private static IEnumerable<string> ValidatePassword(string? pwd)
        {
            if(pwd == null)
                yield return "No password given";
        }

        private static Result<FtpCredentials> GetPrivateKeyCred(string user, Result<byte[]> key) => user
            // Validate user name
            .Validate(IsNotEmpty, "user name")
            // Append private key
            .Aggregate(key)
            // Bind to credentials
            .Bind<FtpCredentials>(t => FtpCredentials.PrivateKey(t.Item1, t.Item2));

        private static Result<byte[]> LoadKeyFromBase64(string? k64) => k64
            // Validate key is not empty
            .Validate(IsNotEmpty, "base64 private key")
            // Try to parse it, returning error on failure
            .TryBind<string?, byte[]>(
                b => Convert.FromBase64String($"{b}"),
                e => $"Bad base64 key: {e.Message}");


        public async static Task<Result<byte[]>> LoadFile(string? filePath, string descriptor) => await filePath
            // Validate that the path is not empty
            .Validate(IsNotEmpty, descriptor)
            // Try to load the file and return its contents
            .TryBind<string?, byte[]>(
                async p => await File.ReadAllBytesAsync($"{p}"),
                e => $"Failed to load the {descriptor} <{filePath}>:{Environment.NewLine}{e.Message}");

        private static IEnumerable<string> ValidateTargetDir(string path)
        {
            if(!path.StartsWith('/'))
                yield return "Target path must be absolute relative to FTP server root";
        }

        public static async Task UploadFile(Uri host, int port, FtpCredentials cred, byte[] payload, string dest)
        {
            // Set up the FTP Client
            var ftpClient = new FtpClient();

            // Try to connect
            using var connection = await ftpClient.Connect(host, port, cred);

            // Check the connection
            if(!connection.IsConnected)
                throw new IOException($"Connection failed: {connection.ConnectError}");

            // The magic uploader knows the target file name without ever being told what it is :D
            await ftpClient.UploadFile(connection, payload, dest);
        }
    }

    enum AuthenticationMode
    {
        Password,
        PrivateKey
    }
}
