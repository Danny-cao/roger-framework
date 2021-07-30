using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQBus.Bus;
using System;

namespace RabbitMQBus.Test.Bus
{
    [TestClass]
    public class RabbitMQBusContextBuilderTest
    {

        [TestCleanup]
        public void TestCleanup()
        {
            Environment.SetEnvironmentVariable("eventbus-exchangename", null);
            Environment.SetEnvironmentVariable("eventbus-hostname", null);
            Environment.SetEnvironmentVariable("eventbus-port", null);
            Environment.SetEnvironmentVariable("eventbus-username", null);
            Environment.SetEnvironmentVariable("eventbus-password", null);
        }

        [TestMethod]
        public void RabbitMQBusContextBuilderShouldHaveDefaultValues()
        {
            var target = new RabbitMQBusContextBuilder();

            Assert.AreEqual("RogerFramework.EventBus", target.ExchangeName);
            Assert.AreEqual("localhost", target.HostName);
            Assert.AreEqual(5672, target.Port);
            Assert.AreEqual("guest", target.UserName);
            Assert.AreEqual("guest", target.Password);
        }

        [TestMethod]
        public void WithExchangeShouldChangeExchangeName()
        {
            var target = new RabbitMQBusContextBuilder();

            RabbitMQBusContextBuilder result = target.WithExchange("Webshop.Exchange");

            Assert.AreEqual("Webshop.Exchange", target.ExchangeName);
            Assert.AreEqual(target, result);
        }

        [TestMethod]
        public void WithAddressShouldChangeHostNameAndPort()
        {
            var target = new RabbitMQBusContextBuilder();

            RabbitMQBusContextBuilder result = target.WithAddress("localhost", 1234);

            Assert.AreEqual("localhost", target.HostName);
            Assert.AreEqual(1234, target.Port);
            Assert.AreEqual(target, result);
        }

        [TestMethod]
        public void WithCredentialsShouldChangeUserNameAndPassword()
        {
            var target = new RabbitMQBusContextBuilder();

            RabbitMQBusContextBuilder result = target.WithCredentials("Username", "Password");

            Assert.AreEqual("Username", target.UserName);
            Assert.AreEqual("Password", target.Password);
            Assert.AreEqual(target, result);
        }

        [TestMethod]
        public void ReadFromEnvironmentVariablesShouldOverwriteWithExchange()
        {
            Environment.SetEnvironmentVariable("eventbus-exchangename", "Webshop.Exchange");


            var target = new RabbitMQBusContextBuilder().WithExchange("BadWebshop.Exchange");

            Assert.AreEqual("BadWebshop.Exchange", target.ExchangeName);


            RabbitMQBusContextBuilder result = target.ReadFromEnvironmentVariables();

            Assert.AreEqual("Webshop.Exchange", target.ExchangeName);
            Assert.AreEqual("localhost", target.HostName);
            Assert.AreEqual(5672, target.Port);
            Assert.AreEqual("guest", target.UserName);
            Assert.AreEqual("guest", target.Password);
            Assert.AreEqual(target, result);
        }

        [TestMethod]
        public void ReadFromEnvironmentVariablesShouldReadEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("eventbus-hostname", "Webshop.host");
            Environment.SetEnvironmentVariable("eventbus-port", "1234");
            Environment.SetEnvironmentVariable("eventbus-username", "Username@username");
            Environment.SetEnvironmentVariable("eventbus-password", "Password1234");

            var target = new RabbitMQBusContextBuilder();

            RabbitMQBusContextBuilder result = target.ReadFromEnvironmentVariables();

            Assert.AreEqual("RogerFramework.EventBus", target.ExchangeName);
            Assert.AreEqual("Webshop.host", target.HostName);
            Assert.AreEqual(1234, target.Port);
            Assert.AreEqual("Username@username", target.UserName);
            Assert.AreEqual("Password1234", target.Password);
            Assert.AreEqual(target, result);
        }
    }
}
