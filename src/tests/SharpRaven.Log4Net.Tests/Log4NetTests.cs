using System;
using System.Configuration;

using NUnit.Framework;

using log4net;

namespace SharpRaven.Log4Net.Tests
{
    [TestFixture]
    public class Log4NetTests
    {
        private ILog log;

        private static void DivideByZero(int stackFrames = 10)
        {
            if (stackFrames == 0)
            {
                var a = 0;
                var b = 1 / a;
            }
            else
                DivideByZero(--stackFrames);
        }


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            // This is needed when running tests under NUnit since NUnit uses log4net internally
            log4net.Config.XmlConfigurator.Configure();

            this.log = LogManager.GetLogger(GetType());
        }


        [Test]
        [Explicit(
            "Only run this test if you're going to check for the logged error in Sentry or debug the SentryAppender.")]
        public void ErrorFormatWithMessage_MessageIsLogged()
        {
            this.log.ErrorFormat("This is a {0} message.", "test");
        }


        [Test]
        [Explicit(
            "Only run this test if you're going to check for the logged error in Sentry or debug the SentryAppender.")]
        public void ErrorWithException_ExceptionIsLogged()
        {
            var exception = Assert.Throws<DivideByZeroException>(() => DivideByZero());
            this.log.Error("This is a test exception", exception);
        }


        [Test]
        [Explicit(
            "Only run this test if you're going to check for the logged error in Sentry or debug the SentryAppender.")]
        public void ErrorWithMessage_MessageIsLogged()
        {
            this.log.Error("This is a test message.");
        }
    }
}
