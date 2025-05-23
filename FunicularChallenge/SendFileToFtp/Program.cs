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
            string? privateKeyFilePath = null)
        {
            var host_uri = GetHostUri(host);
            var port_val = port.Validate(ValidatePort);
            var credentials = await GetCredentials(user, password, base64PrivateKey, privateKeyFilePath);
            var payload = await GetPayload(pathOfFileToSend);
            var destination = targetFolder.Validate(ValidateTargetDir);

            var result = await Result.Try(
                async () => {
                    // Set up the FTP Client
                    var ftpClient = new FtpClient();

                    // Try to connect
                    using var connection = await ftpClient.Connect(
                        host_uri.GetValueOrThrow(),
                        port_val.GetValueOrThrow(),
                        credentials.GetValueOrThrow());

                    // Check the connection
                    if(!connection.IsConnected)
                        throw new IOException($"Connection failed: {connection.ConnectError}");

                    // Try to upload the file
                    // NOTE: The magic uploader knows the target file name without ever being told :D
                    await ftpClient.UploadFile(connection,
                        payload.GetValueOrThrow(),
                        destination.GetValueOrThrow());
                },
                e => $"Failed to upload:{Environment.NewLine}{e.Message}"
            );

            // Finalize the output
            if(result.IsError)
            {
                Console.WriteLine(result.GetErrorOrDefault());
                return 1;
            }
            else
            {
                Console.WriteLine("File uploaded successfully");
                return 0;
            }
        }

        private static IEnumerable<string> ValidatePort(int port)
        {
            if(port <= 0)
                yield return $"Invalid port {port}. Port has to be greater than zero."; // not "greater or equal to"
        }

        public static IEnumerable<string> IsNotEmpty(string? str, string descriptor)
        {
            // IsNullOrWhiteSpace() is more robust than IsNullOrEmpty()
            if(string.IsNullOrWhiteSpace(str))
                yield return $"No {descriptor} specified";
        }

        private static IEnumerable<string> ValidatePassword(string? p)
        {
            if(p == null)
                yield return "No password given";
        }

        private static IEnumerable<string> ValidateTargetDir(string path)
        {
            if(string.IsNullOrWhiteSpace(path))
                yield return "No destination directory specified";
            if(!path.StartsWith('/'))
                yield return "Target path must be absolute relative to FTP server root";
        }

        public static Result<Uri> GetHostUri(string path)
        {
            return Result<Uri>.Try(
                () => new UriBuilder(path).Uri,
                e => $"Bad host address ({path}): {e.Message}"
            );
        }

        public static async Task<Result<FtpCredentials>> GetCredentials(
            string username,
            string? password,
            string? base64,
            string? key_path)
        {
            return ImmutableList.Create(
                // Option 1: Login with password
                GetPasswordCred(username, password),
                // Option 2: Login with private key
                GetPrivateKeyCred(username,
                    ImmutableList.Create(
                        // Option 2a: Key passed as base64
                        GetBase64Key(base64),
                        // Option 2b: Key passed as file path
                        await LoadKeyFile(key_path))
                    .FirstOk(() => ""))
            ).FirstOk(() => "");
        }

        private static Result<FtpCredentials> GetPasswordCred(string u, string? p) => p
            // Perform basic validation on the password
            .Validate(ValidatePassword)
            // Aggregate with user name
            .Aggregate(u.Validate(IsNotEmpty, "user name"))
            // Create an FTP credential using the password, if any
            .Bind<FtpCredentials>(t => FtpCredentials.Password(t.Item2, $"{t.Item1}"));

        private static Result<FtpCredentials> GetPrivateKeyCred(string u, Result<byte[]> k) => u
            // Validate user name
            .Validate(IsNotEmpty, "user name")
            // Append private key
            .Aggregate(k)
            // Bind to credentials
            .Bind<FtpCredentials>(t => FtpCredentials.PrivateKey(t.Item1, t.Item2));

        private static Result<byte[]> GetBase64Key(string? k) => k
            // Validate key is not empty
            .Validate(IsNotEmpty, "base64 private key")
            // Try to parse it, returning error on failure
            .TryBind<string?, byte[]>(
                b => Convert.FromBase64String($"{b}"),
                e => $"Bad base64 key: {e.Message}");

        private async static Task<Result<byte[]>> LoadKeyFile(string? p) => await p
            // Validate path is not empty
            .Validate(IsNotEmpty, "private key file path")
            // Try to load the file, return error on failure
            .TryBind<string?, byte[]>(
                async key_path => await File.ReadAllBytesAsync($"{key_path}"),
                e => $"Cannot load key from path <{p}>: {e.Message}");

        public static async Task<Result<byte[]>> GetPayload(string path)
        {
            if(string.IsNullOrWhiteSpace(path))
                return Result<byte[]>.Error("No source file path given");
            return await Result<byte[]>.Try(
                async () => await File.ReadAllBytesAsync(path),
                e => $"Failed to load <{path}>: {e.Message}");
        }
    }

    enum AuthenticationMode
    {
        Password,
        PrivateKey
    }
}
