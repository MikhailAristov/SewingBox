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

    public class PlayTestCredentials: PlayTest
    {
        public PlayTestCredentials(ITestOutputHelper output) : base(output) { }

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
            var result = await TestCredentialsAsync("mikhail", null, "YWJjZDEyMw==", "D:/temp.bak", false);
            Assert.True(result.GetValueOrDefault() is FtpCredentials.PrivateKey_);
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
            var result = await TestCredentialsAsync("mikhail", null, "YWJjZDEyMw==", null, false);
            Assert.False(result.GetValueOrDefault() is FtpCredentials.Password_);
            Assert.True(result.GetValueOrDefault() is FtpCredentials.PrivateKey_);
        }

        [Fact]
        public async Task CredOnlyPath() => await TestCredentialsAsync("mikhail", null, null, "D:/temp.bak", false);

        [Fact]
        public async Task CredCheckPriority()
        {
            // If both password and private key are valid, password is preferred
            var result = await TestCredentialsAsync("mikhail", "abcd123", "YWJjZDEyMw==", null, false);
            Assert.True(result.GetValueOrDefault() is FtpCredentials.Password_);
            Assert.False(result.GetValueOrDefault() is FtpCredentials.PrivateKey_);
        }

    }

    public class PlayTestPayload : PlayTest
    {
        public PlayTestPayload(ITestOutputHelper output) : base(output) { }

        private async Task<Result<byte[]>> TestPayload(string path, bool should_fail = true)
        {
            var result = await Program.GetPayload(path);
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
