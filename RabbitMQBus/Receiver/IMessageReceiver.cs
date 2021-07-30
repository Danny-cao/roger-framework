using System;
using System.Collections.Generic;

namespace RabbitMQBus.Receiver
{
    public interface IMessageReceiver : IDisposable
    {
        string QueueName { get; }
        IEnumerable<string> TopicExpressions { get; }

        void StartReceivingMessages();
        void StartHandlingMessages(EventMessageReceivedCallback callback);
    }

    public delegate void EventMessageReceivedCallback(EventMessage eventMessage);
}
