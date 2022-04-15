using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using NetCoreServer;

using RAC.Errors;
using static RAC.Errors.Log;
using RAC.Consensus;
namespace RAC.Network
{

    public class ConnectionSession : TcpSession
    {
        private BufferBlock<MessagePacket> reqQueue;
        private BufferBlock<MessagePacket> respQueue;
        //private NetCoreServer.Buffer cache;
        private List<byte> cache;
        public string clientIP { get; private set; }


        public ConnectionSession(TcpServer server,
        ref BufferBlock<MessagePacket> reqQueue,
        ref BufferBlock<MessagePacket> respQueue) : base(server)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;
            //cache = new NetCoreServer.Buffer();
            cache = new List<byte>();
        }

        protected override void OnConnecting()
        {
            this.clientIP = IPAddress.Parse(((IPEndPoint)this.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)this.Socket.RemoteEndPoint).Port.ToString();
            DEBUG("New client from " + this.clientIP + " connected");

        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            //cache.Append(buffer, (int)offset, (int)size);
            byte[] temp = new byte[size];
            Array.Copy(buffer, (int)offset, temp, 0, (int)size);
            cache.AddRange(temp);

            DEBUG("Receiving the following message with length: " + size + " bytes \n" + System.Text.Encoding.Default.GetString(cache.ToArray()));
            int handledSize = MessagePacket.ParseReceivedMessage(cache.ToArray(), this);

            if (handledSize == cache.Count)
                cache.Clear();
            else
                cache.RemoveRange(0, handledSize);
        }

        protected override void OnDisconnected()
        {
            DEBUG("Client " + this.clientIP + " disconnected");
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session caught an error with code {error}");
        }
    }

    // TODO: change this to clientBinding
    public class TcpHandler : TcpServer
    {
        public BufferBlock<MessagePacket> reqQueue;
        public BufferBlock<MessagePacket> respQueue;

        public TcpHandler(IPAddress address, int port,
        ref BufferBlock<MessagePacket> reqQueue,
        ref BufferBlock<MessagePacket> respQueue) : base(address, port)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;
        }


        protected override TcpSession CreateSession()
        {
            return new ConnectionSession(this, ref this.reqQueue, ref this.respQueue);
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error}");
        }
    }

    public class Server
    {
        // msg queue to handle client request
        public BufferBlock<MessagePacket> reqQueue;
        public BufferBlock<MessagePacket> respQueue;


        // no need for thread safety cuz one only write and the other only read
        public Cluster cluster = Global.cluster;

        public TcpHandler tcpHandler;

        public IPAddress address { get; }
        public int clientCommPort { get; }
        public TcpHandler server;

        public int clusterCommPort { get; }

        // ClusterListener - interserver communication
        public TcpHandler clusterListener;

        public RAC.Consensus.BFTNodes bftNode{ get; set; }


        // threshold for stop reading if still no starter detected
        private const int readThreshold = 100;

        public Server(Node node)
        {
            this.address = IPAddress.Parse(node.address);
            this.clientCommPort = node.port;
            this.clusterCommPort = node.clusterPort;

            this.reqQueue = new BufferBlock<MessagePacket>();
            this.respQueue = new BufferBlock<MessagePacket>();

            this.bftNode = new BFTNodes(cluster.selfNode.nodeid);

        }

        public async Task HandleRequestAsync(BufferBlock<MessagePacket> queue)
        {

            while (await queue.OutputAvailableAsync())
            {
                MessagePacket msg = queue.Receive(); ;
                try
                {
                    DEBUG("Resparing response");

                    if (msg.msgSrc == MsgSrc.bftnode)
                    {
                        bftNode.parseConsensusMessage(msg.content);
                    }
                    else
                    {
                        Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                        res.StageResponse(msg.connection);
                    }
                }
                catch (OperationCanceledException)
                {
                    ERROR("Last error caused by message: \n" + msg);
                    continue;
                }
                catch (Exception e)
                {
                    ERROR("Error thrown when handling the request", e, false);
                    ERROR("Last error caused by message: \n" + msg);
                }

            }

        }



        public async Task SendResponseAsync(BufferBlock<MessagePacket> queue)
        {
            while (await queue.OutputAvailableAsync())
            {
                MessagePacket msg = queue.Receive();

                // broadcast
                if (msg.to == Dest.broadcast)
                {
                    this.cluster.BroadCast(msg);
                }
                // reply to client, if connection found to be ended, do nothing
                else if (msg.to == Dest.client)
                {
                    if (msg.connection.IsConnected)
                    {

                        byte[] data = msg.Serialize();
                        msg.connection.SendAsync(data);
                    }
                    else
                    {
                        WARNING("Connection to client " + msg.connection.clientIP + " is lost, reply cannot be sent " + msg);
                    }

                }
                else
                {
                    ERROR("Destination DNE for msg: " + msg);
                }
            }
        }

        public void Run()
        {
            try
            {
                // TODO: change this to client
                this.server = new TcpHandler(this.address, this.clientCommPort, ref this.reqQueue, ref this.respQueue);

                this.clusterListener = new TcpHandler(this.address, this.clusterCommPort, ref this.reqQueue, ref this.respQueue);

                // Start listening for client requests.
                this.server.Start();
                this.clusterListener.Start();

                LOG("Server Started");

                // Enter the listening loop.
                while (true)
                {
                    DEBUG("Waiting for a connection... ");
                    Console.ReadLine();
                }
            }
            catch (SocketException e)
            {
                ERROR("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                LOG("Stopped listening");
                this.cluster.DisconnectAll();
                server.Stop();
                this.reqQueue.Complete();
                this.respQueue.Complete();

                // this.clusterReqQueue.Complete();
                // this.clusterReqQueue.Complete();
            }
        }


    }
}
