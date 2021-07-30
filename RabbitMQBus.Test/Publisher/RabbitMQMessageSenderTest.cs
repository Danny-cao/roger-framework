using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQBus.Bus;
using RabbitMQBus.Publisher;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQBus.Test.Publisher
{
    [TestClass]
    public class RabbitMQMessageSenderTest
    {
        private string _exchangeName;
        private string _topic;
        private byte[] _body;
        private EventMessage _eventMessage;

        private Mock<IBasicProperties> _propsMock;
        private Mock<IModel> _channelMock;
        private Mock<IConnection> _connectionMock;
        private RabbitMQBusContext _context;

        [TestInitialize]
        public void TestInitialize()
        {
            _exchangeName = "Webshop.ExchangeName";
            _topic = "Webshop.Topic";
            _body = Encoding.Unicode.GetBytes("Pindakaas");
            _eventMessage = new EventMessage
            {
                Topic = _topic,
                Body = _body,
            };

            _propsMock = new Mock<IBasicProperties>();
            _channelMock = new Mock<IModel>();
            _channelMock.Setup(c => c.CreateBasicProperties()).Returns(_propsMock.Object);
            _connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            _connectionMock.Setup(conn => conn.CreateModel()).Returns(_channelMock.Object);

            _context = new RabbitMQBusContext(_exchangeName, _connectionMock.Object);
        }

        [TestMethod]
        public async Task RabbitMQtMessageSenderShouldDeclareExchange()
        {
            var target = new RabbitMQMessageSender(_context);

            await target.SendMessageAsync(_eventMessage);

            _channelMock.Verify(c => c.ExchangeDeclare(_exchangeName, ExchangeType.Topic, It.IsAny<bool>(), It.IsAny<bool>(), null));
        }

        [TestMethod]
        public void DisposeShouldDispose()
        {
            var target = new RabbitMQMessageSender(_context);

            target.Dispose();

            _channelMock.Verify(c => c.Dispose());
        }

        [TestMethod]
        public async Task SendMessageAsyncShouldSendCorrectMessageToCorrectExchangeWithCorrectTopic()
        {
            var target = new RabbitMQMessageSender(_context);

            await target.SendMessageAsync(_eventMessage);

            _channelMock.Verify(c => c.BasicPublish(_exchangeName, _topic, false, _propsMock.Object, _body));
        }

        [TestMethod]
        public async Task SendMessageAsyncShouldSendCorrectHeaderInfo()
        {
            _propsMock.SetupProperty(p => p.CorrelationId);
            _propsMock.SetupProperty(p => p.Timestamp);
            _propsMock.SetupProperty(p => p.Type);

            Guid guid = Guid.NewGuid();
            long timestamp = DateTime.Now.Ticks;
            _eventMessage.CorrelationId = guid;
            _eventMessage.Timestamp = timestamp;
            _eventMessage.EventType = "String";

            var target = new RabbitMQMessageSender(_context);

            await target.SendMessageAsync(_eventMessage);

            Assert.AreEqual(guid.ToString(), _propsMock.Object.CorrelationId);
            Assert.AreEqual(new AmqpTimestamp(timestamp), _propsMock.Object.Timestamp);
            Assert.AreEqual("String", _propsMock.Object.Type);
        }
    }
}
