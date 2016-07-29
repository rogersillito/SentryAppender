using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Core;
using log4net.Repository;

using Moq;

using NUnit.Framework;

using SharpRaven.Data;

namespace SharpRaven.Log4Net.Tests {
	[TestFixture]
	public class SentryAppenderTests {

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

		[SetUp]
		public void SetUp()
		{
			_ravenClientMock = new Mock<IRavenClient>();
			_loggerRepositoryMock = new Mock<ILoggerRepository>();

			_sentryAppender = new SentryAppenderUnderTest();
			_sentryAppender.SetRavenClient(_ravenClientMock.Object);
		}


		[Test]
		public void Append_WithExceptionAndMessage() {
			var exception = new Exception("ExceptionAndMessage");

			_sentryAppender.DoAppendUnderTest(CreateLoggingEvent("Custom message", exception));

			_ravenClientMock.Verify(
				rc => rc.CaptureException(It.Is<Exception>(e => Object.ReferenceEquals(e, exception)),
					It.Is<SentryMessage>(sm => sm.Message == "Custom message"),
					It.IsAny<ErrorLevel>(),
					It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string[]>(),
					It.IsAny<object>()),
				Times.Once);
		}

		[Test]
		public void Append_WithJustException() {
			var exception = new Exception("JustException");

			_sentryAppender.DoAppendUnderTest(CreateLoggingEvent(exception));

			_ravenClientMock.Verify(
				rc => rc.CaptureException(It.Is<Exception>(e => Object.ReferenceEquals(e, exception)),
					It.Is<SentryMessage>(sm => sm == null),
					It.IsAny<ErrorLevel>(),
					It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string[]>(),
                    It.IsAny<object>()),
				Times.Once);
		}


		private LoggingEvent CreateLoggingEvent(object message, Exception exception = null)
		{
			return new LoggingEvent(typeof(SentryAppenderTests),
				_loggerRepositoryMock.Object,
				"test",
				Level.Error,
				message,
				exception);
		}


	}
}
