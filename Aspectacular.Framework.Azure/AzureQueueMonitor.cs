﻿using System;
using System.Collections.Generic;

using Microsoft.WindowsAzure.Storage.Queue;

namespace Aspectacular
{
    /// <summary>
    ///     Implements callback control inversion for Azure regular (non-ESB) storage queue, enabling either
    ///     1) blocking wait for messages via WaitForPayload(), or
    ///     2) pub/sub pattern (callback on message arrival) via RegisterMessageHandler().
    /// </summary>
    /// <remarks>
    ///     An attempt of reading a message from Azure queues (non-EBS, regular storage queues) are
    ///     non-blocking, meaning that if there are no messages, queue message retriever returns immediately
    ///     with null response. This means that in order to read a message, a loop is required.
    ///     This loop, if done with no delays between attempts, will hog CPU and leak money as each queue check costs money.
    ///     This class implements smart queue check loop, with increasing delays - up to specified limit, ensuring that CPU
    ///     is free most of the time and saving money on
    /// </remarks>
    public class AzureQueueMonitor : BlockingObjectPoll<List<CloudQueueMessage>>
    {
        protected readonly bool useAopProxyWhenAccessingQueue;
        public CloudQueue Queue { get; private set; }

        private readonly TimeSpan messageInvisibilityTime;

        /// <param name="queue">Azure queue to dequeue messages from.</param>
        /// <param name="messageInvisibilityTimeMillisec">
        ///     Time for queue element to be processed. If not deleted from queue within
        ///     this time, message is automatically placed back in the queue.
        /// </param>
        /// <param name="maxCheckDelaySeconds">Maximum delay, in seconds, between attempts to dequeue messages.</param>
        /// <param name="useAopProxyWhenAccessingQueue">
        ///     Set to true to use Aspectacular AOP proxy with process-wide set of aspects,
        ///     to call queue access functions. Set to false to call queue operations directly.
        /// </param>
        public AzureQueueMonitor(CloudQueue queue, int messageInvisibilityTimeMillisec, int maxCheckDelaySeconds = 60, bool useAopProxyWhenAccessingQueue = true)
            : base(null, maxCheckDelaySeconds * 1000)
        {
            this.Queue = queue;
            this.useAopProxyWhenAccessingQueue = useAopProxyWhenAccessingQueue;
            this.messageInvisibilityTime = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: messageInvisibilityTimeMillisec);
        }

        /// <summary>
        ///     Returns null if payload cannot be acquired, and non-null if payload is captured.
        /// </summary>
        /// <returns>Null if no messages were in the queue. List of dequeued messages otherwise.</returns>
        /// <remarks>Can be synchronized as it does "lock(this) {" before attempting to dequeue messages.</remarks>
        protected override List<CloudQueueMessage> PollEasy()
        {
            const int maxMessageCount = 32;

            List<CloudQueueMessage> messages;

            lock(this)
            {
                if(this.useAopProxyWhenAccessingQueue)
                    messages = (List<CloudQueueMessage>)this.Queue.GetProxy().Invoke(q => q.GetMessages(maxMessageCount, messageInvisibilityTime, null, null));
                else
                    messages = (List<CloudQueueMessage>)this.Queue.GetMessages(maxMessageCount, messageInvisibilityTime, null, null);
            }

            return messages == null || messages.Count == 0 ? null : messages;
        }

        /// <summary>
        ///     Launches polling loop that invokes message-processing callback when messages arrived.
        ///     This is an alternative to using blocking WaitForPayload() method.
        /// </summary>
        /// <param name="messageProcessCallback">
        ///     Optional message-processing delegate. If null, Process() method must be overridden
        ///     in a subclass.
        /// </param>
        /// <remarks>
        ///     This method starts polling thread, on which both polling function and message processing functions are called.
        ///     This means that next attempt to dequeue messages won't occur until message processing callback function is done.
        ///     Payload processing callback may start its own thread(s) to process messages asynchronously and quickly return
        ///     control to the polling thread.
        /// </remarks>
        public void RegisterMessageHandler(Action<CloudQueue, List<CloudQueueMessage>> messageProcessCallback = null)
        {
            if(messageProcessCallback == null)
                // ReSharper disable once RedundantBaseQualifier
                base.RegisterCallbackHandler(payloadProcessCallback: null);
            else
                this.RegisterCallbackHandler(payload => messageProcessCallback(this.Queue, payload));
        }
    }

    public static class AzureQueueExtensions
    {
        /// <summary>
        ///     Blocks until either messages arrive, or smart polling is terminated.
        ///     Returns null if application is exiting or stop is signaled, otherwise non-empty collection of messages.
        ///     Uses smart polling with delays between attempts to dequeue messages, ensuring lows CPU utilization and not leaking
        ///     money for Azure storage transactions.
        /// </summary>
        /// <param name="queue">Azure queue to dequeue messages from.</param>
        /// <param name="messageInvisibilityTimeMillisec">
        ///     Time for queue element to be processed. If not deleted from queue within
        ///     this time, message is automatically placed back in the queue.
        /// </param>
        /// <param name="maxCheckDelaySeconds">Maximum delay, in seconds, between attempts to dequeue messages.</param>
        /// <param name="useAopProxyWhenAccessingQueue">
        ///     Set to true to use Aspectacular AOP proxy with process-wide set of aspects,
        ///     to call queue access functions. Set to false to call queue operations directly.
        /// </param>
        /// <returns>Returns null if application is exiting or stop is signaled, otherwise non-empty collection of messages.</returns>
        public static List<CloudQueueMessage> WaitForMessages(this CloudQueue queue, int messageInvisibilityTimeMillisec, int maxCheckDelaySeconds = 60, bool useAopProxyWhenAccessingQueue = true)
        {
            if(queue == null)
                throw new ArgumentNullException("queue");

            using(var qmon = new AzureQueueMonitor(queue, messageInvisibilityTimeMillisec, maxCheckDelaySeconds, useAopProxyWhenAccessingQueue))
            {
                List<CloudQueueMessage> messages = qmon.WaitForPayload();
                return messages;
            }
        }

        /// <summary>
        ///     Launches polling loop that invokes message-processing callback when messages arrived.
        ///     This is an alternative to using blocking WaitForPayload() method.
        ///     Uses smart polling with delays between attempts to dequeue messages, ensuring lows CPU utilization and not leaking
        ///     money for Azure storage transactions.
        /// </summary>
        /// <param name="queue">Azure queue to dequeue messages from.</param>
        /// <param name="messageProcessCallback">
        ///     Optional message-processing delegate. If null, Process() method must be overridden
        ///     in a subclass.
        /// </param>
        /// <param name="messageInvisibilityTimeMillisec">
        ///     Time for queue element to be processed. If not deleted from queue within
        ///     this time, message is automatically placed back in the queue.
        /// </param>
        /// <param name="maxCheckDelaySeconds">Maximum delay, in seconds, between attempts to dequeue messages.</param>
        /// <param name="useAopProxyWhenAccessingQueue">
        ///     Set to true to use Aspectacular AOP proxy with process-wide set of aspects,
        ///     to call queue access functions. Set to false to call queue operations directly.
        /// </param>
        /// <returns>
        ///     IDisposable queue wrapper object to be used later for calling Stop() or Dispose() methods to terminate queue
        ///     polling.
        /// </returns>
        /// <remarks>
        ///     This method starts polling thread, on which both polling function and message processing functions are called.
        ///     This means that next attempt to dequeue messages won't occur until message processing callback function is done.
        ///     Payload processing callback may start its own thread(s) to process messages asynchronously and quickly return
        ///     control to the polling thread.
        /// </remarks>
        public static AzureQueueMonitor RegisterMessageHandler(this CloudQueue queue, Action<CloudQueue, List<CloudQueueMessage>> messageProcessCallback,
            int messageInvisibilityTimeMillisec, int maxCheckDelaySeconds = 60, bool useAopProxyWhenAccessingQueue = true)
        {
            if(queue == null)
                throw new ArgumentNullException("queue");
            if(messageProcessCallback == null)
                throw new ArgumentNullException("messageProcessCallback");

            var qmon = new AzureQueueMonitor(queue, messageInvisibilityTimeMillisec, maxCheckDelaySeconds, useAopProxyWhenAccessingQueue);
            qmon.RegisterMessageHandler(messageProcessCallback);
            return qmon;
        }
    }
}