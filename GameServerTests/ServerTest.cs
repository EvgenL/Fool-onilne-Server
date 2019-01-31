using System;
using Evgen.Byffer;
using GameServer;
using GameServer.RoomLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameServerTests
{
    [TestClass]
    public class ServerTest
    {

        [TestMethod]
        public void TestLoad_100000clients()
        {
            Server.Instance.ServerStart();

            var clients = CreateDummyClients(100000);

            Assert.IsTrue(true);
        }

        private DummyClient[] CreateDummyClients(int amount)
        {
            DummyClient[] clients = new DummyClient[amount];

            for (int i = 0; i < 100000; i++)
            {
                DummyClient client = new DummyClient();
                client.Start();
            }

            return clients;
        }

        [TestMethod]
        private void Test_Buffer()
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteString("Русский текст");

            string str = buffer.ReadString();
            Assert.Equals("Русский текст", str);
        }
    }
}
