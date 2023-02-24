using System;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Messaging.Common;
using NUnit.Framework;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
    public class TestMessageInterceptor : IMessageInterceptor
    {
        public int PreSendCount;
        public int PostSendCount;
        public int PreReceiveCount;
        public int PreHandleCount;
        public int PostHandleCount;
        public int PostReceiveCount;

        public void Reset()
        {
            PreSendCount = 0;
            PostSendCount = 0;
            PreReceiveCount = 0;
            PostReceiveCount = 0;
            PreHandleCount = 0;
            PostHandleCount = 0;
        }

        public void AssertCounts(int preSend, int postSend, int preReceive, int postReceive, int preHandle, int postHandle)
        {
            Assert.AreEqual(preSend, PreSendCount, $"Message Interceptor PreSendCount value should be {preSend}");
            Assert.AreEqual(postSend, PostSendCount, $"Message Interceptor PostSendCount value should be {postSend}");
            Assert.AreEqual(preReceive, PreReceiveCount, $"Message Interceptor PreReceiveCount value should be {preReceive}");
            Assert.AreEqual(postReceive, PostReceiveCount, $"Message Interceptor PostReceiveCount value should be {postReceive}");
            Assert.AreEqual(preHandle, PreHandleCount, $"Message Interceptor PreHandleCount value should be {preHandle}");
            Assert.AreEqual(postHandle, PostHandleCount, $"Message Interceptor PostHandleCount value should be {postHandle}");
        }

        public void PreSend(IMessage message)
        {
            PreSendCount++;
        }

        public void PostSend(IMessage message, Exception e)
        {
            PostSendCount++;
        }

        public Task PreReceiveAsync(IMessage message)
        {
            PreReceiveCount++;
            return Task.CompletedTask;
        }

        public Task PreHandleAsync(string subscriberId, IMessage message)
        {
            PreHandleCount++;
            return Task.CompletedTask;
        }

        public Task PostHandleAsync(string subscriberId, IMessage message, Exception e)
        {
            PostHandleCount++;
            return Task.CompletedTask;
        }

        public Task PostReceiveAsync(IMessage message)
        {
            PostReceiveCount++;
            return Task.CompletedTask;
        }
    }
}
