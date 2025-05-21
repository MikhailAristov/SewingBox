using System;
using System.IO;
using System.Threading.Tasks;
using FtpLib;
using FunicularSwitch;

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
        /// <param name="authentication"></param>
        /// <param name="password"></param>
        /// <param name="base64PrivateKey"></param>
        /// <param name="privateKeyFilePath"></param>
        public static async Task Main(
            string pathOfFileToSend,
            string targetFolder,
            string host, 
            string user, 
            int port = 22, 
            string authentication = nameof(AuthenticationMode.Password), 
            string? password = null, 
            string? base64PrivateKey = null, 
            string? privateKeyFilePath = null)
        {

            // Validate host non-empty
            if (string.IsNullOrEmpty(host))
                throw new ArgumentException("Host may not be empty");

            // Validate port > 0
            if (port <= 0)
                throw new ArgumentException("Invalid port. Port has to be greater or equal to zero");

            // Parse and validate host URI
            Uri? uri;
            try
            {
                uri = new UriBuilder(host).Uri;
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid host uri");
            }

            // Parse and validate authentication mode based on enum
            if (!Enum.TryParse(typeof(AuthenticationMode), authentication, out var authenticationMode))
            {
                throw new ArgumentException(
                    $"Parameter authentication has invalid value. Valid values are: {nameof(AuthenticationMode.Password)}, {nameof(AuthenticationMode.PrivateKey)}");
            }

            // Switch based on mode
            FtpCredentials? credentials = null;
            switch ((AuthenticationMode)authenticationMode)
            {
                case AuthenticationMode.Password:
                    // Assert user and pwd are not null; does it make sense that user can be ""?
                    if (password == null)
                        throw new ArgumentException("No password provided");
                    if (user == null)
                        throw new ArgumentException("User not provided");
                    credentials = FtpCredentials.Password(user, password);
                    break;
                case AuthenticationMode.PrivateKey:
                    // Assert user is not null (redundant?)
                    if (user == null)
                        throw new ArgumentException("User not provided");
                    // Parse secret key from base64
                    if (base64PrivateKey != null)
                    {
                        var privateKey = Convert.FromBase64String(base64PrivateKey);
                        credentials = FtpCredentials.PrivateKey(user, privateKey);
                    }
                    // Read secret key from file
                    else if (privateKeyFilePath != null)
                    {
                        // Try load path
                        try
                        {
                            var privateKey = await File.ReadAllBytesAsync(privateKeyFilePath);
                            credentials = FtpCredentials.PrivateKey(user, privateKey);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException("Failed to read private key file", e);
                        }
                    }
                    // Does not actually set credentials if both base64PrivateKey and privateKeyFilePath are null
                    break;
                default:
                    // This can never be reached.
                    throw new ArgumentOutOfRangeException();
            }


            // Validate sent file path not empty
            if (string.IsNullOrEmpty(pathOfFileToSend))
            {
                throw new ArgumentException("No upload file path specified");
            }

            // Validate target folder is not empty and absolute
            if (string.IsNullOrEmpty(targetFolder))
            {
                throw new ArgumentException("No target folder specified"); // redundant to the next check
            }
            if (!targetFolder.StartsWith("/"))
            {
                throw new ArgumentException("Target folder has to be absolute path on ftp server");
            }

            // Try read the file to sent
            byte[] fileToUpload;
            try
            {
                fileToUpload = await File.ReadAllBytesAsync(pathOfFileToSend);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to upload file", e);
            }

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
        }
    }

    enum AuthenticationMode
    {
        Password,
        PrivateKey
    }
}
