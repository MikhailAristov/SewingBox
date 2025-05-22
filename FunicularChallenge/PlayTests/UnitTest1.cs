using FunicularSwitch;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using SendFileToFtp;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace PlayTests
{
    public class PlayTest
    {
        protected readonly ITestOutputHelper _output;
        protected PlayTest(ITestOutputHelper output) => _output = output;

        protected void Log(string? msg) => _output.WriteLine($"{msg}");
    }


    public class PlayTest1 : PlayTest
    {
        public PlayTest1(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TestURIBad()
        {
            var result = PlayingAround.GetHostUri("");
            Assert.True(result.IsError);
            Log(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestURIGood() => Assert.True(PlayingAround.GetHostUri("https://bluehands.de").IsOk);


        [Fact]
        public void TestPortZero()
        {
            var result = PlayingAround.GetPort(0);
            Assert.True(result.IsError);
            Log(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestPortBad()
        {
            var result = PlayingAround.GetPort(-12);
            Assert.True(result.IsError);
            Log(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestPortGood() => Assert.True(PlayingAround.GetPort(80).IsOk);


        [Fact]
        public void TestStringEmpty()
        {
            var result = PlayingAround.IsNotEmpty("");
            Assert.True(result.IsError);
            Log(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestStringFull() => Assert.True(PlayingAround.IsNotEmpty("string").IsOk);


        [Fact]
        public void TestManual()
        {
            TestString(null);
        }

        private void TestString(string? str)
        {
            var opt = string.IsNullOrWhiteSpace(str) ? Option<string?>.None : Option<string?>.Some(str);
            var result = opt.ToResult(() => $"empty string");
            Assert.True(result.IsError);
            Log(result.GetErrorOrDefault());
        }
    }

    public class PlayTestCredentials: PlayTest
    {
        public PlayTestCredentials(ITestOutputHelper output) : base(output) { }

        private async Task TestCredentialsAsync(string u, string? p, string? b, string? k, bool should_fail) {
            var result = await Program.GetCredentials(u, p, b, k);
            Assert.True(result.IsError == should_fail);
            if(should_fail)
                Log(result.GetErrorOrDefault());
        }

        [Fact]
        public async Task CredGood()
        {
            await TestCredentialsAsync("mikhail", "abcd123", "xxx", "C:/temp.bak", false);
        }

        [Fact]
        public async Task CredBadUser()
        {
            await TestCredentialsAsync("    ", "abcd123", "xxx", "C:/temp.bak", false);
        }
    }
}
