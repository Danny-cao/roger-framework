using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQBus.Bus;
using RabbitMQBus.Publisher;
using RabbitMQBus.Receiver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQBus.Test.Receiver
{
    [TestClass]
    public class RabbitMQMessageReceiverTest
    {
        private string _exchangeName;
        private string _queueName;
        private string[] _topicExpressions;
        private string _topic;
        private byte[] _body;
        private EventMessage _eventMessage;

        private Mock<IBasicProperties> _propMock;
        private Mock<IBasicProperties> _propsFromChannelMock;
        private Mock<IModel> _channelMock;
        private Mock<IConnection> _connectionMock;
        private RabbitMQBusContext _context;
        private EventingBasicConsumer _consumer;

        [TestInitialize]
        public void TestInitialize()
        {
            _exchangeName = "Webshop.ExchangeName";
            _queueName = "Webshop.ListenQueue";
            _topicExpressions = new string[] { "Webshop.Topic", "Webshop.OtherTopic" };

            _topic = "Webshop.Topic";
            _body = Encoding.Unicode.GetBytes("Pindakaas");
            _eventMessage = new EventMessage
            {
                Topic = _topic,
                Body = _body,
            };

            _propMock = new Mock<IBasicProperties>(MockBehavior.Loose);
            _propsFromChannelMock = new Mock<IBasicProperties>();
            _channelMock = new Mock<IModel>();
            _channelMock.Setup(c => c.CreateBasicProperties()).Returns(_propsFromChannelMock.Object);
            _channelMock.Setup(c => c.BasicConsume(_queueName, true, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                                                   It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>()))
                .Callback((string a, bool b, string c, bool d, bool e, IDictionary<string, object> args,
                           IBasicConsumer consumer) => _consumer = (EventingBasicConsumer)consumer);


            _connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            _connectionMock.Setup(conn => conn.CreateModel()).Returns(_channelMock.Object);

            _context = new RabbitMQBusContext(_exchangeName, _connectionMock.Object);
        }

        [TestMethod]
        public void RabbitMQMessageReceiverShouldCreateAChannel()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);

            _connectionMock.Verify(conn => conn.CreateModel());
        }

        [TestMethod]
        public void StartReceivingMessagesShouldDeclareExchangeAndQueueAndBindings()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);

            target.StartReceivingMessages();

            _channelMock.Verify(c => c.ExchangeDeclare(_exchangeName, ExchangeType.Topic,
                                                     It.IsAny<bool>(), It.IsAny<bool>(), null));
            _channelMock.Verify(c => c.QueueDeclare(_queueName, true, false, false, null));
            _channelMock.Verify(c => c.QueueBind(_queueName, _exchangeName, "Webshop.Topic", null));
            _channelMock.Verify(c => c.QueueBind(_queueName, _exchangeName, "Webshop.OtherTopic", null));
        }

        [TestMethod]
        public void StartReceivingMessagesShouldNotBeCallableTwice()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();

            Action act = () =>
            {
                target.StartReceivingMessages();
            };

            var ex = Assert.ThrowsException<BusException>(act);
            Assert.AreEqual("Cannot call 'StartReceivingMessages' multiple times.", ex.Message);
        }

        [TestMethod]
        public void StartHandlingMessagesShouldCreateConsumer()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();

            var callback = new EventMessageReceivedCallback(em => { });
            target.StartHandlingMessages(callback);

            Assert.IsNotNull(_consumer);
        }

        [TestMethod]
        public void StartHandlingMessagesShouldCallCallbackOnReceivingMessages()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();
            bool hasBeenCalled = false;
            target.StartHandlingMessages(em => { hasBeenCalled = true; });


            _consumer.HandleBasicDeliver("consumerTag", 1, false, _exchangeName, _topic, _propMock.Object, _body);

            Assert.AreEqual(true, hasBeenCalled);
        }

        [TestMethod]
        public void StartHandlingMessagesShouldCallCallbackWithCorrectEventMessage()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();
            EventMessage message = null;
            target.StartHandlingMessages(em => { message = em; });

            var guid = Guid.NewGuid();
            var timestamp = new AmqpTimestamp(1627600269);
            _propMock.SetupProperty(p => p.CorrelationId, guid.ToString());
            _propMock.SetupProperty(p => p.Timestamp, timestamp);
            _propMock.SetupProperty(p => p.Type, "Webshop.Aankoop");
            byte[] body = Encoding.Unicode.GetBytes("{Pindakaas data in Json}");
            var buffer = new ReadOnlyMemory<byte>(body);


            _consumer.HandleBasicDeliver("consumerTag", 1, false, _exchangeName, _topic, _propMock.Object, buffer);


            Assert.AreEqual(_topic, message.Topic);
            Assert.AreEqual(guid, message.CorrelationId);
            Assert.AreEqual(1627600269, message.Timestamp);
            Assert.AreEqual("Webshop.Aankoop", message.EventType);
            CollectionAssert.AreEqual(body, message.Body);
        }

        [TestMethod]
        public void StartHandlingMessagesShouldBeAbleToHandleCallbackWithEmptyValues()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();
            EventMessage message = null;
            target.StartHandlingMessages(em => { message = em; });

            // Act
            _consumer.HandleBasicDeliver("consumerTag", 1, false, _exchangeName,
                                         routingKey: null, _propMock.Object, body: null);

            // Assert
            Assert.AreEqual(null, message.Topic);
            Assert.AreEqual(Guid.Empty, message.CorrelationId);
            Assert.AreEqual(0, message.Timestamp);
            Assert.AreEqual(null, message.EventType);
            Assert.IsTrue(message.Body.Length == 0);
        }

        [TestMethod]
        public void StartReceivingMessagesShouldNotBeCallableWithoutCallingStartReceivingMessagesFirst()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);

            Action act = () =>
            {
                target.StartHandlingMessages(em => { });
            };

            var ex = Assert.ThrowsException<BusException>(act);
            Assert.AreEqual("Before calling 'StartHandlingMessages', call 'StartReceivingMessages' first to declare queue and topics.",
                ex.Message);
        }

        [TestMethod]
        public void StartHandlingMessagesNotCallableTwice()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();
            target.StartHandlingMessages(em => { });

            Action act = () =>
            {
                target.StartHandlingMessages(em => { });
            };

            var ex = Assert.ThrowsException<BusException>(act);
            Assert.AreEqual("Cannot call 'StartHandlingMessages' multiple times.",
                ex.Message);
        }

        [TestMethod]
        public void MessageReceiverDisposeShouldDispose()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);

            target.Dispose();

            _channelMock.Verify(c => c.Dispose());
        }

        [TestMethod]
        public void StartReceivingMessagesNotCallableAfterDispose()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.StartReceivingMessages();
            target.Dispose();

            Action act = () =>
            {
                target.StartHandlingMessages(em => { });
            };

            var ex = Assert.ThrowsException<ObjectDisposedException>(act);
            Assert.AreEqual("Cannot access a disposed object.\r\nObject name: 'RabbitMQMessageReceiver'.",
                ex.Message);
        }

        [TestMethod]
        public void StartHandlingMessagesNotCallableAfterDispose()
        {
            var target = new RabbitMQMessageReceiver(_context, _queueName, _topicExpressions);
            target.Dispose();

            Action act = () =>
            {
                target.StartReceivingMessages();
            };

            var ex = Assert.ThrowsException<ObjectDisposedException>(act);
            Assert.AreEqual("Cannot access a disposed object.\r\nObject name: 'RabbitMQMessageReceiver'.",
                ex.Message);
        }

    }
}
