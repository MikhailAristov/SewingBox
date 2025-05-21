using FunicularSwitch;
using SendFileToFtp;
using Xunit.Abstractions;

namespace PlayTests
{
    public class PlayTest1
    {
        private readonly ITestOutputHelper _output;

        public PlayTest1(ITestOutputHelper output) => _output = output;

        [Fact]
        public void TestURIBad()
        {
            var result = PlayingAround.GetHostUri("");
            Assert.True(result.IsError);
            _output.WriteLine(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestURIGood() => Assert.True(PlayingAround.GetHostUri("https://bluehands.de").IsOk);


        [Fact]
        public void TestPortZero()
        {
            var result = PlayingAround.GetPort(0);
            Assert.True(result.IsError);
            _output.WriteLine(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestPortBad()
        {
            var result = PlayingAround.GetPort(-12);
            Assert.True(result.IsError);
            _output.WriteLine(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestPortGood() => Assert.True(PlayingAround.GetPort(80).IsOk);


        [Fact]
        public void TestStringEmpty()
        {
            var result = PlayingAround.IsNotEmpty("");
            Assert.True(result.IsError);
            _output.WriteLine(result.GetErrorOrDefault());
        }

        [Fact]
        public void TestStringFull() => Assert.True(PlayingAround.IsNotEmpty("string").IsOk);
    }
}