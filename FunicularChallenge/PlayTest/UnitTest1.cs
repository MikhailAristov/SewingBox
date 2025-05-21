using Microsoft.VisualStudio.TestTools.UnitTesting;
using SendFileToFtp;

namespace PlayTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            PlayingAround.Play("", "", -1).GetAwaiter().GetResult();
        }
    }
}
