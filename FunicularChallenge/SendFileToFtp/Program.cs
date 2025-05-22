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
        public static async Task Main(
            string pathOfFileToSend,
            string targetFolder,
            string host,
            string user,
            int port = 22,
            string? password = null,
            string? base64PrivateKey = null,
            string? privateKeyFilePath = null)
        {

            var cred = GetCredentials(user, password, base64PrivateKey, privateKeyFilePath);

            // Validate sent file path not empty
            if(string.IsNullOrEmpty(pathOfFileToSend))
            {
                throw new ArgumentException("No upload file path specified");
            }

            // Validate target folder is not empty and absolute
            if(string.IsNullOrEmpty(targetFolder))
            {
                throw new ArgumentException("No target folder specified"); // redundant to the next check
            }
            if(!targetFolder.StartsWith("/"))
            {
                throw new ArgumentException("Target folder has to be absolute path on ftp server");
            }

            // Try read the file to sent
            byte[] fileToUpload;
            try
            {
                fileToUpload = await File.ReadAllBytesAsync(pathOfFileToSend);
            }
            catch(Exception e)
            {
                throw new ArgumentException("Failed to upload file", e);
            }
            /*
            // Set up the FTP Client
            var ftpClient = new FtpClient();
            // Try to connect
            using var connection = await ftpClient.Connect(uri, port, credentials);
            if (connection.IsConnected)
            {
                // Does not handle runtime connection errors -- add try?
                await ftpClient.UploadFile(connection, fileToUpload, targetFolder);
                Console.WriteLine("File uploaded successfully");
            }
            else
            {
                throw new Exception($"Connect failed: {connection.ConnectError}");
            }
            */
        }

        public static async Task<Result<FtpCredentials>> GetCredentials(
            string username,
            Option<string?> password,
            string? base64,
            string? key_path
        )
        {
            static IEnumerable<string> ValidateUsername(string u)
            {
                if(string.IsNullOrWhiteSpace(u))
                    yield return $"Bad user name: {u}";
            }

            // Validate user
            var user = username.Validate(ValidateUsername);


#pragma warning disable CS8604 // Possible null reference argument.
            return ImmutableList.Create(

                // OPTION 1: Password
                password
                    // Convert password from option to result
                    .ToResult(() => "No password given")
                    // Create an FTP credential using the password, if any
                    .Bind<FtpCredentials>(p => FtpCredentials.Password(user.GetValueOrThrow(), p)),

                // OPTION 2: Private key
                ImmutableList.Create(

                    // OPTION 2A: Try to parse the base64 key, if any
                    Result.Try(
                        () => Convert.FromBase64String($"{base64}"),
                        e => $"Bad base64 key: {e.Message}"
                    ),

                    // OPTION 2B: Try to load the key from file path, if any
                    await Result.Try(
                        async () => await File.ReadAllBytesAsync($"{key_path}"),
                        e => $"Cannot load key from path <{key_path}>: {e.Message}"
                    ))

                    // Select the first successfully loaded private key, if any
                    .FirstOk(() => "No private key found")
                    // Create an FTP credential with the loaded key, if any
                    .Bind<FtpCredentials>(bytes => FtpCredentials.PrivateKey(user.GetValueOrThrow(), bytes))

            ).FirstOk(() => "One of the following MUST be specified: password, base64PrivateKey, or privateKeyFilePath!");
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }

        enum AuthenticationMode
    {
        Password,
        PrivateKey
    }
}
