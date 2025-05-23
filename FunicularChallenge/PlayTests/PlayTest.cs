using FtpLib;
using FunicularSwitch;
using SendFileToFtp;
using Xunit.Abstractions;

namespace PlayTests
{
    public class PlayTest
    {
        protected readonly ITestOutputHelper _output;
        protected PlayTest(ITestOutputHelper output) => _output = output;

        protected void Log(string? msg) => _output.WriteLine($"{msg}");
    }

    public class PlayTestCredentials(ITestOutputHelper output) : PlayTest(output)
    {
        private async Task<Result<FtpCredentials>> TestCredentialsAsync(
            string u, string? p, string? b, string? k, bool should_fail = true)
        {
            var result = await Program.GetCredentials(u, p, b, k);
            Assert.True(result.IsError == should_fail);
            if(should_fail)
                Log(result.GetErrorOrDefault());
            return result;
        }

        [Fact]
        public async Task CredGood() => await TestCredentialsAsync("mikhail", "abcd123", "YWJjZDEyMw==", "D:/temp.bak", false);

        [Fact]
        public async Task CredNoPassword()
        {
            var result = (await TestCredentialsAsync("mikhail", null, "YWJjZDEyMw==", "D:/temp.bak", false)).GetValueOrDefault();
            Assert.IsType<FtpCredentials.PrivateKey_>(result);
        }

        [Fact]
        public async Task CredBadUser() => await TestCredentialsAsync("    ", "abcd123", "YWJjZDEyMw==", "D:/temp.bak");

        [Fact]
        public async Task CredEmptyCredentials() => await TestCredentialsAsync("mikhail", null, null, null);

        [Fact]
        public async Task CredOnlyPassword() => await TestCredentialsAsync("mikhail", "abcd123", null, null, false);

        [Fact]
        public async Task CredOnlyBase64()
        {
            var result = (await TestCredentialsAsync("mikhail", null, "YWJjZDEyMw==", null, false)).GetValueOrDefault();
            Assert.IsNotType<FtpCredentials.Password_>(result);
            Assert.IsType<FtpCredentials.PrivateKey_>(result);
        }
        [Fact]
        public async Task CredBadBase64() => await TestCredentialsAsync("mikhail", null, "xyz", null);

        [Fact]
        public async Task CredOnlyPath() => await TestCredentialsAsync("mikhail", null, null, "D:/temp.bak", false);

        [Fact]
        public async Task CredCheckPriority()
        {
            // If both password and private key are valid, password is preferred
            var result = (await TestCredentialsAsync("mikhail", "abcd123", "YWJjZDEyMw==", null, false)).GetValueOrDefault();
            Assert.IsType<FtpCredentials.Password_>(result);
            Assert.IsNotType<FtpCredentials.PrivateKey_>(result);
        }

    }

    public class PlayTestPayload(ITestOutputHelper output) : PlayTest(output)
    {
        private async Task<Result<byte[]>> TestPayload(string path, bool should_fail = true)
        {
            var result = await Program.LoadFile(path, "file to send");
            Assert.True(result.IsError == should_fail);
            if(should_fail)
                Log(result.GetErrorOrDefault());
            return result;
        }

        [Fact]
        public async Task PayloadGood() => await TestPayload("D:/temp.bak", false);

        [Fact]
        public async Task PayloadEmptyPath() => await TestPayload("     ");

        [Fact]
        public async Task PayloadBadPath() => await TestPayload("D:/iImQu11m1Uw3DJW5PLQy.bak");

    }
}
