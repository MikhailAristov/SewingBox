using FunicularSwitch;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SendFileToFtp
{
    public class PlayingAround
    {
        static void Main(string[] args) {
            
        }


        public static Result<Uri> GetHostUri(string address)
        {
            return Result<Uri>.Try(
                () => new UriBuilder(address).Uri,
                e => e.Message
            );
        }


        public static Result<int> GetPort(Option<int> port)
        {
            IEnumerable<string> ValidatePort(int port)
            {
                if(port <= 0)
                    yield return $"Invalid port {port}. Port has to be greater than zero.";
            }

            return port
                .ToResult(() => $"Bad port: {port.GetValueOrDefault()}.")
                .Bind(p => p.Validate(ValidatePort));
        }

        public static Result<string> IsNotEmpty(string s) => string.IsNullOrWhiteSpace(s)
            ? Result.Error<string>("Empty string")
            : Result.Ok(s);
    }
}