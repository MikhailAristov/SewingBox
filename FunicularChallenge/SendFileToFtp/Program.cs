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

        private static IEnumerable<string> ValidateUsername(string u)
        {
            // IsNullOrWhiteSpace() is more robust than IsNullOrEmpty()
            if(string.IsNullOrWhiteSpace(u))
                yield return $"No user name specified";
        }

        private static IEnumerable<string> ValidatePassword(string? p)
        {
            if(p == null)
                yield return $"No password given";
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

            // Validate user beforehand because we will need it at two separate points in the pipeline later.
            var user = username.Validate(ValidateUsername);

            // Generate and return the FTP credentials
            return ImmutableList.Create(

                // OPTION 1: Password
                password
                    // Perform basic validation on the password
                    .Validate(ValidatePassword)
                    // Aggregate with user name
                    .Aggregate(user)
                    // Create an FTP credential using the password, if any
                    .Bind<FtpCredentials>(t => FtpCredentials.Password(t.Item2, $"{t.Item1}")),

                // OPTION 2: Private key
                ImmutableList.Create(

                    // OPTION 2A: Try to parse the base64 key, if any
                    Result.Try(
                        () => {
                            if(string.IsNullOrWhiteSpace(base64))
                                throw new ArgumentException("No base64 key given");
                            return Convert.FromBase64String($"{base64}");
                        },
                        e => $"Bad base64 key: {e.Message}"
                    ),

                    // OPTION 2B: Try to load the key from file path, if any
                    await Result.Try(
                        async () => await File.ReadAllBytesAsync($"{key_path}"),
                        e => $"Cannot load key from path <{key_path}>: {e.Message}"
                    ))

                    // Select the first successfully loaded private key, if any
                    .FirstOk(() => "No private key found")
                    // Aggregate with user name
                    .Aggregate(user)
                    // Create an FTP credential with the loaded key, if any
                    .Bind<FtpCredentials>(t => FtpCredentials.PrivateKey(t.Item2, t.Item1))

            ).FirstOk(() => "One of the following MUST be specified: password, base64PrivateKey, or privateKeyFilePath!");
        }

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
