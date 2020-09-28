using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    [Ignore("Needs a CA key to work, see bottom of setup")]
    public class SslServerTest : SimpleWebTestBase
    {
        protected override bool StartServer => false;

        [SetUp]
        public override void Setup()
        {
            transport = CreateRelayTransport();
            transport.receiveTimeout = timeout;
            transport.sendTimeout = timeout;

            onConnect.Clear();
            onDisconnect.Clear();
            onData.Clear();

            transport.OnServerConnected.AddListener((connId) => onConnect.Add(connId));
            transport.OnServerDisconnected.AddListener((connId) => onDisconnect.Add(connId));
            transport.OnServerDataReceived.AddListener((connId, data, ___) => onData.Add((connId, data)));
            transport.OnServerError.AddListener((connId, exception) => onError.Add((connId, exception)));

            transport.sslEnabled = true;
            transport.sslCertJson = "./Assets/SimpleWebTransport/cert.example.Json";
            transport.ServerStart();

            // to use these test you need to create a CA cert and use it to sign MirrorLocal
            // then add the cert to node so that it will accept it
            Environment.SetEnvironmentVariable("NODE_EXTRA_CA_CERTS", "path/to/CACert.pem");
        }

        [UnityTest]
        public IEnumerator ClientCanConnectOverWss()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("WssConnectAndClose.js");
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(2), "Should have 2 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.output[1], Is.EqualTo($"Closed after 2000ms"), "Should be connection close log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnect, Has.Count.EqualTo(1), "Connect should be called once");
        }
    }
}


