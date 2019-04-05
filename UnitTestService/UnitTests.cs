using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebAPIHotel;

namespace UnitTestService
{
    [TestClass]
    public class UnitTests
    {
        static SQLHandler handler = new SQLHandler();
        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            handler.databaseSetup();
        }
    }
}
