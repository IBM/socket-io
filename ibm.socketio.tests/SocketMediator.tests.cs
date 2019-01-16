using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IBM.Webclient;
using IBM.SocketIO.Tests.Mocks;
using IBM.SocketIO.Factories;
using IBM.SocketIO.Impl;
using IBM.SocketIO.Tests.Extensions;
using IBM.SocketIO.Tests.MockData;

namespace IBM.SocketIO.Tests
{
    [TestClass]
    public class SocketMediatorTests
    {
        [TestMethod]
        public async Task HandlesConnectionUpgradeFailureCorrectly()
        {
            // 1. Initial handshake
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeResponse = "ok";
            var secondaryHandshakeClient = new MockHttpClient(secondaryHandshakeResponse, HttpStatusCode.OK);

            var protocolUpgradeResponse = "";
            var protocolUpgradeClient = new MockHttpClient(protocolUpgradeResponse, HttpStatusCode.SwitchingProtocols);
            protocolUpgradeClient.AddHeader("Sec-Websocket-Accept", "RWZIQcMHYHEyvemvvKIkivC1Tvo=");

            var finalHandshakeResponse = "15:40/collections,";
            var finalHandshakeClient = new MockHttpClient(finalHandshakeResponse, HttpStatusCode.OK);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient)
                .Returns(secondaryHandshakeClient)
                .Returns(protocolUpgradeClient)
                .Returns(finalHandshakeClient);

            var initialProbeResponse = Encoding.UTF8.GetBytes("3probe");
            var secondaryProbeResponse = Encoding.UTF8.GetBytes("5");


            // 2. Connection upgrade and handshake
            var socketMock = new Mock<IClientSocket>();

            socketMock.Setup(s => s.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new HttpRequestException()));

            var socketFactoryMock = new Mock<IClientSocketFactory>();
            socketFactoryMock.Setup(f => f.CreateSocketClient()).Returns(socketMock.Object);


            var mediator = new SocketMediator("ws://localhost:7200/collections");
            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(socketMock, socketFactoryMock, factoryMock);
        }

        [TestMethod]
        public async Task LoadsAndStreamsDataCorrectly()
        {
            // 1. Initial handshake
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeResponse = "ok";
            var secondaryHandshakeClient = new MockHttpClient(secondaryHandshakeResponse, HttpStatusCode.OK);

            var protocolUpgradeResponse = "";
            var protocolUpgradeClient = new MockHttpClient(protocolUpgradeResponse, HttpStatusCode.SwitchingProtocols);
            protocolUpgradeClient.AddHeader("Sec-Websocket-Accept", "RWZIQcMHYHEyvemvvKIkivC1Tvo=");

            var finalHandshakeResponse = "15:40/collections,";
            var finalHandshakeClient = new MockHttpClient(finalHandshakeResponse, HttpStatusCode.OK);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient)
                .Returns(secondaryHandshakeClient)
                .Returns(protocolUpgradeClient)
                .Returns(finalHandshakeClient);

            var initialProbeResponse = Encoding.UTF8.GetBytes("3probe");
            var secondaryProbeResponse = Encoding.UTF8.GetBytes("5");


            // 2. Connection upgrade and handshake
            var socketMock = new Mock<IClientSocket>();

            socketMock.Setup(s => s.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // The ws client handshake sends "2probe" and the server returns "3probe"
            socketMock.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // "3probe" response
            List<byte> initialProbeBytes = new List<byte>(initialProbeResponse);
            var originalBytes = initialProbeBytes.Count;

            var dataProvider = new MockSocketDataProvider(initialProbeBytes);

            socketMock.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .Returns((ArraySegment<byte> buffer, CancellationToken token) =>
                {
                    var writeResult = dataProvider.GetDataChunk(buffer.Array.Length);
                    writeResult.buffer.CopyTo(buffer.Array, 0);

                    return MockSocketTaskFactory.CreateTask(initialProbeBytes.Count, writeResult.bytesWritten);
                });

            // This captures the mediator sending the value "5", can return the same thing
            socketMock.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var socketFactoryMock = new Mock<IClientSocketFactory>();
            socketFactoryMock.Setup(f => f.CreateSocketClient()).Returns(socketMock.Object);


            var mediator = new SocketMediator("ws://localhost:7200/collections");
            await mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object);

            Mock.VerifyAll(socketMock, socketFactoryMock, factoryMock);
        }

        [TestMethod]
        public async Task Handles2ProbeFailureCorrectly()
        {
            // 1. Initial handshake
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeResponse = "ok";
            var secondaryHandshakeClient = new MockHttpClient(secondaryHandshakeResponse, HttpStatusCode.OK);

            var protocolUpgradeResponse = "";
            var protocolUpgradeClient = new MockHttpClient(protocolUpgradeResponse, HttpStatusCode.SwitchingProtocols);
            protocolUpgradeClient.AddHeader("Sec-Websocket-Accept", "RWZIQcMHYHEyvemvvKIkivC1Tvo=");

            var finalHandshakeResponse = "15:40/collections,";
            var finalHandshakeClient = new MockHttpClient(finalHandshakeResponse, HttpStatusCode.OK);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient)
                .Returns(secondaryHandshakeClient)
                .Returns(protocolUpgradeClient)
                .Returns(finalHandshakeClient);

            // 2. Connection upgrade and handshake
            var socketMock = new Mock<IClientSocket>();

            socketMock.Setup(s => s.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // The ws client handshake sends "2probe" and the server returns "3probe"
            socketMock.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // "3probe" response
            var initialProbeResponse = Encoding.UTF8.GetBytes("HahaKeed");
            List<byte> initialProbeBytes = new List<byte>(initialProbeResponse);
            var originalBytes = initialProbeBytes.Count;

            var dataProvider = new MockSocketDataProvider(initialProbeBytes);

            socketMock.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .Returns((ArraySegment<byte> buffer, CancellationToken token) =>
                {
                    var writeResult = dataProvider.GetDataChunk(buffer.Array.Length);
                    writeResult.buffer.CopyTo(buffer.Array, 0);

                    return MockSocketTaskFactory.CreateTask(initialProbeBytes.Count, writeResult.bytesWritten);
                });

            var socketFactoryMock = new Mock<IClientSocketFactory>();
            socketFactoryMock.Setup(f => f.CreateSocketClient()).Returns(socketMock.Object);


            var mediator = new SocketMediator("ws://localhost:7200/collections");
            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(socketMock, socketFactoryMock, factoryMock);
        }

        [TestMethod]
        public async Task Handles2ProbeSendFailureCorrectly()
        {
            // 1. Initial handshake
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeResponse = "ok";
            var secondaryHandshakeClient = new MockHttpClient(secondaryHandshakeResponse, HttpStatusCode.OK);

            var protocolUpgradeResponse = "";
            var protocolUpgradeClient = new MockHttpClient(protocolUpgradeResponse, HttpStatusCode.SwitchingProtocols);
            protocolUpgradeClient.AddHeader("Sec-Websocket-Accept", "RWZIQcMHYHEyvemvvKIkivC1Tvo=");

            var finalHandshakeResponse = "15:40/collections,";
            var finalHandshakeClient = new MockHttpClient(finalHandshakeResponse, HttpStatusCode.OK);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient)
                .Returns(secondaryHandshakeClient)
                .Returns(protocolUpgradeClient)
                .Returns(finalHandshakeClient);

            // 2. Connection upgrade and handshake
            var socketMock = new Mock<IClientSocket>();

            socketMock.Setup(s => s.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // The ws client handshake sends "2probe" and the server returns "3probe"
            socketMock.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new HttpRequestException()));

            var socketFactoryMock = new Mock<IClientSocketFactory>();
            socketFactoryMock.Setup(f => f.CreateSocketClient()).Returns(socketMock.Object);


            var mediator = new SocketMediator("ws://localhost:7200/collections");
            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(socketMock, socketFactoryMock, factoryMock);
        }

        [TestMethod]
        public async Task HandleFirstHandshakeFailureCorrectly()
        {
            var initialHandshakeClient = new MockHttpClient("", HttpStatusCode.RequestTimeout);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient);

            var socketFactoryMock = new Mock<IClientSocketFactory>();

            var mediator = new SocketMediator("ws://localhost:7200/collections");

            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(factoryMock);
        }

        [TestMethod]
        public async Task HandleSecondHandshakeFailureCorrectly()
        {
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeClient = new MockHttpClient("", HttpStatusCode.RequestTimeout);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient)
                .Returns(secondaryHandshakeClient);

            var socketFactoryMock = new Mock<IClientSocketFactory>();

            var mediator = new SocketMediator("ws://localhost:7200/collections");

            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(factoryMock);
        }

        [TestMethod]
        public async Task HandleThirdHandshakeFailureCorrectly()
        {
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeResponse = "ok";
            var secondaryHandshakeClient = new MockHttpClient(secondaryHandshakeResponse, HttpStatusCode.OK);

            var protocolUpgradeResponse = "";
            var protocolUpgradeClient = new MockHttpClient(protocolUpgradeResponse, HttpStatusCode.RequestTimeout);
            protocolUpgradeClient.AddHeader("Sec-Websocket-Accept", "RWZIQcMHYHEyvemvvKIkivC1Tvo=");

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                .Returns(initialHandshakeClient)
                .Returns(secondaryHandshakeClient)
                .Returns(protocolUpgradeClient);

            var socketFactoryMock = new Mock<IClientSocketFactory>();

            var mediator = new SocketMediator("ws://localhost:7200/collections");

            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(factoryMock);
        }

        [TestMethod]
        public async Task HandleFinalHandshakeFailureCorrectly()
        {
            var initialHandshakeResponse = "96:0{ \"sid\":\"i4VXx68U4C_w6qiFAAAm\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}2:40";
            var initialHandshakeClient = new MockHttpClient(initialHandshakeResponse, HttpStatusCode.OK);

            var secondaryHandshakeResponse = "ok";
            var secondaryHandshakeClient = new MockHttpClient(secondaryHandshakeResponse, HttpStatusCode.OK);

            var protocolUpgradeResponse = "";
            var protocolUpgradeClient = new MockHttpClient(protocolUpgradeResponse, HttpStatusCode.SwitchingProtocols);
            protocolUpgradeClient.AddHeader("Sec-Websocket-Accept", "RWZIQcMHYHEyvemvvKIkivC1Tvo=");

            var finalHandshakeResponse = "1:6";
            var finalHandshakeClient = new MockHttpClient(finalHandshakeResponse, HttpStatusCode.RequestTimeout);

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.SetupSequence(f => f.CreateHttpClient())
                    .Returns(initialHandshakeClient)
                    .Returns(secondaryHandshakeClient)
                    .Returns(protocolUpgradeClient)
                    .Returns(finalHandshakeClient);

            var socketFactoryMock = new Mock<IClientSocketFactory>();

            var mediator = new SocketMediator("ws://localhost:7200/collections");

            await AsyncAssert.Throws<HttpRequestException>(
                () => mediator.InitConnection(factoryMock.Object, socketFactoryMock.Object));

            Mock.VerifyAll(factoryMock);
        }
    }
}
