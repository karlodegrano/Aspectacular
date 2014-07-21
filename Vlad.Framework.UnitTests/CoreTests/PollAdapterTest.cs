﻿using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Aspectacular;

// ReSharper disable JoinDeclarationAndInitializer
namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for PollAdapterTest
    /// </summary>
    [TestClass]
    public class PollAdapterTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        public static Pair<bool, DateTimeOffset> PollTime(DateTimeOffset targetTime)
        {
            var time = DateTimeOffset.Now;
            return new Pair<bool, DateTimeOffset>(time >= targetTime && (time - targetTime).Milliseconds <= 500, time);
        }

        [TestMethod]
        public void TestSmartPollingBlocking()
        {
            DateTimeOffset threeSecondDelay = DateTimeOffset.Now.AddSeconds(3);

            const int maxDelayMillisec = 500;
            var pollmeister = new BlockingPoll<DateTimeOffset>(() => PollTime(threeSecondDelay), maxDelayMillisec);

            Pair<bool, DateTimeOffset> result = pollmeister.WaitForPayload();
            this.TestContext.WriteLine("Empty poll calls: {0:#,#0}", pollmeister.EmptyPollCallCount);
            
            Assert.IsTrue(result.First);
            int discrepMillisecBetweenHopedAndActual = (result.Second - threeSecondDelay).Milliseconds;
            Assert.IsTrue(discrepMillisecBetweenHopedAndActual <= maxDelayMillisec);
            Assert.IsTrue(pollmeister.EmptyPollCallCount <= 12);
            Assert.IsTrue(pollmeister.PollCallCountWithPayload == 1);
        }


        [TestMethod]
        public void TestSmartPollingCallback()
        {
            DateTimeOffset threeSecondDelay = DateTimeOffset.Now.AddSeconds(3);

            const int maxDelayMillisec = 500;
            var pollmeister = new BlockingPoll<DateTimeOffset>(() => PollTime(threeSecondDelay), maxDelayMillisec);

            DateTimeOffset? message = null;
            pollmeister.StartNotificationLoop(payload => message = payload);
            Threading.Sleep(3100);
            pollmeister.Stop();

            this.TestContext.WriteLine("Empty poll calls: {0:#,#0}, Calls with payload: {1:#,#0}", pollmeister.EmptyPollCallCount, pollmeister.PollCallCountWithPayload);

            Assert.IsTrue(message != null);
            int discrepMillisecBetweenHopedAndActual = (message.Value - threeSecondDelay).Milliseconds;
            Assert.IsTrue(discrepMillisecBetweenHopedAndActual <= maxDelayMillisec + 100);
            Assert.IsTrue(pollmeister.EmptyPollCallCount <= 12);
            Assert.IsTrue(pollmeister.PollCallCountWithPayload >= 1);
        }
    }
}
// ReSharper restore JoinDeclarationAndInitializer
