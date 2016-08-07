using System;

using log4net.Core;
using log4net.Repository;

using Moq;

using NUnit.Framework;

using SharpRaven.Data;
using SharpRaven.Log4Net.Extra;

namespace SharpRaven.Log4Net.Tests {
    [TestFixture]
    public class SentryAppenderTests
    {
        #region Derived test class
        /// <summary>Derived SentryAppender that gives us access to the raven client so that we can test</summary>
        public class SentryAppenderUnderTest : SentryAppender
        {
            public void SetRavenClient(IRavenClient ravenClient)
            {
                RavenClient = ravenClient;
            }

            public void DoAppendUnderTest(LoggingEvent loggingEvent)
            {
                DoAppend(loggingEvent);
            }
        }
        #endregion

        private Mock<IRavenClient> _ravenClientMock;
        private Mock<ILoggerRepository> _loggerRepositoryMock;
        private SentryAppenderUnderTest _sentryAppender;
        private SentryEvent _testEvent;

        [SetUp]
        public void SetUp()
        {
            _ravenClientMock = new Mock<IRavenClient>();
            _loggerRepositoryMock = new Mock<ILoggerRepository>();

            _sentryAppender = new SentryAppenderUnderTest();
            _sentryAppender.SetRavenClient(_ravenClientMock.Object);

            // Capture should return non-null message ID or null if something failed
            _ravenClientMock.Setup(foo => foo.Capture(It.IsAny<SentryEvent>()))
                            .Callback<SentryEvent>(foo => _testEvent = foo)
                            .Returns("fake message ID");
        }

        [Test]
        public void Append_TestGeneralEventProperties()
        {
            _sentryAppender.DoAppendUnderTest(CreateLoggingEvent("TestMessage"));

            Assert.That(_testEvent.Level, Is.EqualTo(ErrorLevel.Error));
        }

        [Test]
        public void Append_TestExtraProperties()
        {
            _sentryAppender.DoAppendUnderTest(CreateLoggingEvent("TestMessage"));

            var envExtra = _testEvent.Extra?.GetType().GetProperty("Environment")?.GetValue(_testEvent.Extra, null) as EnvironmentExtra;

            Assert.That(envExtra.MachineName, Is.EqualTo(Environment.MachineName));
            Assert.That(envExtra.Version, Is.EqualTo(Environment.Version.ToString()));
            Assert.That(envExtra.OSVersion, Is.EqualTo(Environment.OSVersion.ToString()));

            string[] commandLineArgs = null;
            try
            {
                commandLineArgs = Environment.GetCommandLineArgs();

                Assert.That(envExtra.CommandLineArgs, Is.EquivalentTo(commandLineArgs));
            }
            catch (NotSupportedException)
            {
                Assert.That(envExtra.CommandLineArgs, Is.Null);
            }
        }

        [Test]
        public void Append_WithExceptionAndMessage()
        {
            var exception = new Exception("ExceptionAndMessage");

            _sentryAppender.DoAppendUnderTest(CreateLoggingEvent("Custom message", exception));

            _ravenClientMock.Verify(foo => foo.Capture(It.IsNotNull<SentryEvent>()), Times.Once);

            Assert.That(_testEvent.Exception, Is.EqualTo(exception));
            Assert.That(_testEvent.Message.Message, Is.EqualTo("Custom message"));
        }

        [Test]
        public void Append_WithJustException()
        {
            var exception = new Exception("JustException");

            _sentryAppender.DoAppendUnderTest(CreateLoggingEvent(exception));

            _ravenClientMock.Verify(foo => foo.Capture(It.IsNotNull<SentryEvent>()), Times.Once);

            Assert.That(_testEvent.Exception, Is.EqualTo(exception));
            Assert.That(_testEvent.Message.Message, Is.EqualTo("JustException"));
        }

        [Test]
        public void Append_WithJustMessage()
        {
            _sentryAppender.DoAppendUnderTest(CreateLoggingEvent("JustMessage"));

            _ravenClientMock.Verify(foo => foo.Capture(It.IsNotNull<SentryEvent>()), Times.Once);

            Assert.That(_testEvent.Exception, Is.Null);
            Assert.That(_testEvent.Message.Message, Is.EqualTo("JustMessage"));
        }

        [Test]
        public void Append_WithNullMessage()
        {
            _sentryAppender.DoAppendUnderTest(CreateLoggingEvent(null));

            _ravenClientMock.Verify(foo => foo.Capture(It.IsNotNull<SentryEvent>()), Times.Once);

            Assert.That(_testEvent.Exception, Is.Null);
            Assert.That(_testEvent.Message.Message, Is.EqualTo(String.Empty));
        }

        [Test]
        public void Append_WithNullEvent()
        {
            _sentryAppender.DoAppendUnderTest(null);

            // Nothing captured because there was nothing to capture
            _ravenClientMock.Verify(foo => foo.Capture(It.IsNotNull<SentryEvent>()), Times.Never);
        }

        private LoggingEvent CreateLoggingEvent(object message, Exception exception = null)
        {
            return new LoggingEvent(typeof(SentryAppenderTests),
                _loggerRepositoryMock.Object,
                "testLoggerName",
                Level.Error,
                message,
                exception);
        }
    }
}
