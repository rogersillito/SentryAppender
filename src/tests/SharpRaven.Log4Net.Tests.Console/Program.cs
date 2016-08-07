using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace SharpRaven.Log4Net.Tests.Console {
	class Program {
		static void Main(string[] args)
		{
			/*
			 * This project has some tests for which you'll need to add your own DSN to check
			 * that the result is as you expect it.
			 */


			XmlConfigurator.Configure();

			TestSingleAlert();
			TestTwoLoggers();

			System.Console.WriteLine("Press a key to finish");
			System.Console.ReadKey();
		}


		private static void TestSingleAlert()
		{
			var log = LogManager.GetLogger(typeof(Program));

			log.ErrorFormat("foo {0}", "123");

			try {
				throw new ApplicationException("Testing logging from console app");
			} catch (ApplicationException aEx) {
				// Things to check:
				// - Did the event arrive?
				// - Are any command line parameters added to the event?
				// - Did the "Custom message" appear in the event on Sentry?
				log.Error("Custom message", aEx);
			}
		}


		private static void TestTwoLoggers()
		{
			var log1 = LogManager.GetLogger(typeof (Program));
			var log2 = LogManager.GetLogger("CustomLog");

			try
			{
				throw new ApplicationException("Testing two loggers");
			}
			catch (ApplicationException aEx)
			{
				// Things to check:
				// - Did two events arrive in different projects in Sentry?
				log1.Error("Program log", aEx);
				log2.Error("Custom log", aEx);
			}
		}
	}
}
