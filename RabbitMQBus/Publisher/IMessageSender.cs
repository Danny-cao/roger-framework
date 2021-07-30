using System;
using System.Threading.Tasks;

namespace RabbitMQBus.Publisher
{
    public interface IMessageSender: IDisposable
    {
        Task SendMessageAsync(EventMessage message);
    }
}
