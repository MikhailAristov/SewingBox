using FunicularSwitch;
using FunicularSwitch.Extensions;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace SendFileToFtp
{
    public class PlayingAround
    {
        static void Main(string[] args) {
            Play("", "", -1).GetAwaiter().GetResult();
        }


        public static async Task<Result<Uri>> Play(
            string pathOfFileToSend,
            string host,
            int port = 22)
        {
            return GetHostUri(host);
        }

        public static Result<Uri> GetHostUri(string address)
        {
            return Result<Uri>.Try(
                () => new UriBuilder(address).Uri,
                e => e.Message
            );
        }


        public static Result<int> GetPort(int p) => p > 0
            ? Result.Ok(p)
            : Result.Error<int>("Invalid port. Port has to be greater or equal to zero");

        public static Result<string> IsNotEmpty(string s) => string.IsNullOrWhiteSpace(s)
            ? Result.Error<string>("Empty string")
            : Result.Ok(s);
    }
}