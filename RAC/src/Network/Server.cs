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
using System.Threading;

namespace RAC.Network
{

    public class ConnectionSession : TcpSession
    {
        private BufferBlock<MessagePacket> reqQueue;
        private BufferBlock<MessagePacket> respQueue;
        //private NetCoreServer.Buffer cache;
        
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

        private List<byte> cache;
        int state = 0; // 0 = searching for 'f', 1 = completeting header, 2 = completetting content 
        int headerReadCount, contentReadCount;
        byte[] headerRead = new byte[MessagePacket.HEADER_SIZE - 1];
        byte[] contentRead;

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {

            // byte[] temp = new byte[size];
            // Array.Copy(buffer, (int)offset, temp, 0, (int)size);
            // cache.AddRange(temp);

            // DEBUG("Receiving the following message with length: " + size + " bytes \n" + System.Text.Encoding.Default.GetString(temp));
            // MessagePacket msg;
            // int handledSize = MessagePacket.ParseReceivedMessage(cache.ToArray(), this, out msg);

            // if (msg is not null)
            //     this.reqQueue.Post<MessagePacket>(msg);

            // if (handledSize == cache.Count)
            //     cache.Clear();
            // else
            //     cache.RemoveRange(0, handledSize);

            //Console.WriteLine("new stuffs");

            int leftToRead;
            for (long i = offset; i < offset + size; i++)
            {
                if (state == 0)
                {
                    //Console.WriteLine("0");
                    if (buffer[i] == '\f')
                        state = 1;
                }
                else if (state == 1)
                {
                    leftToRead = MessagePacket.HEADER_SIZE - 1 - headerReadCount;

                    if (i + leftToRead > size)
                    {
                        //Console.WriteLine("1.11");
                        System.Buffer.BlockCopy(buffer, (int)i, headerRead, headerReadCount, (int)(size - i));
                        //Console.WriteLine("1.12");
                        headerReadCount += (int)(size - i);
                    }
                    else
                    {
                        // finished reading header
                        //Console.WriteLine("1.21");
                        System.Buffer.BlockCopy(buffer, (int)i, headerRead, headerReadCount, leftToRead);
                        //Console.WriteLine("1.22");
                        headerReadCount += leftToRead;
                        
                        state = 2;
                    }

                    i += leftToRead - 1;
                }
                else if (state == 2)
                {

                    MsgSrc src = (MsgSrc)BitConverter.ToInt32(headerRead);
                    int contentlen = BitConverter.ToInt32(headerRead, (MessagePacket.NUM_FIELDS - 1) * 4);
                    
                    if (contentReadCount == 0)
                    {
                        // init buffer
                        contentRead = new byte[contentlen];
                    }

                    leftToRead = contentlen - contentReadCount;
                    //Console.WriteLine("2 " + contentReadCount + " " + contentlen + " " + leftToRead + " " + i + " " + size);
                    if (i + leftToRead > size)
                    {
                        //Console.WriteLine("2.11");
                        System.Buffer.BlockCopy(buffer, (int)i, contentRead, contentReadCount, (int)(size - i));
                        //Console.WriteLine("2.12");
                        contentReadCount += (int)(size - i);
                    }                    
                    else
                    {
                        // finished reading content
                        //Console.WriteLine("2.21");
                        System.Buffer.BlockCopy(buffer, (int)i, contentRead, contentReadCount, leftToRead);
                        //Console.WriteLine("2.22");
                        contentReadCount += leftToRead;

                        MessagePacket msg = new MessagePacket(src, contentlen, Encoding.UTF8.GetString(contentRead), this);
                        DEBUG("Recieveing msg:\n " + msg);
                        this.reqQueue.Post<MessagePacket>(msg);
                        
                        state = 0;
                        headerReadCount = 0;
                        contentReadCount = 0;
                    }
                    i += leftToRead - 1;
                }
            }



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
        public BufferBlock<MessagePacket> clientReqQueue;
        public BufferBlock<MessagePacket> clientRespQueue;
        // msg queue to handle cluster comm
        public BufferBlock<MessagePacket> clusterReqQueue;
        public BufferBlock<MessagePacket> clusterRespQueue;


        // no need for thread safety cuz one only write and the other only read
        public Cluster cluster = Global.cluster;

        public TcpHandler tcpHandler;

        public IPAddress address { get; }
        public int clientCommPort { get; }
        public TcpHandler server;

        public int clusterCommPort { get; }

        // ClusterListener - interserver communication
        public TcpHandler clusterListener;



        // threshold for stop reading if still no starter detected
        private const int readThreshold = 100;

        public Server(Node node)
        {
            this.address = IPAddress.Parse(node.address);
            this.clientCommPort = node.port;
            this.clusterCommPort = node.clusterPort;

            this.clientReqQueue = new BufferBlock<MessagePacket>();
            this.clientRespQueue = new BufferBlock<MessagePacket>();

            this.clusterReqQueue = new BufferBlock<MessagePacket>();
            this.clusterRespQueue = new BufferBlock<MessagePacket>();


        }

        public async Task HandleRequestAsync(BufferBlock<MessagePacket> queue)
        {

            while (await queue.OutputAvailableAsync())
            {
                MessagePacket msg = queue.Receive(); ;
                try
                {
                    DEBUG("Resparing response");


                    Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                    res.StageResponse(msg.connection);

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
                if (msg.endpointType == Dest.broadcast)
                {
                    this.cluster.BroadCast(msg);
                }
                else if (msg.endpointType == Dest.server)
                {
                    this.cluster.Send(msg, msg.to);

                }
                // reply to client, if connection found to be ended, do nothing
                else if (msg.endpointType == Dest.client)
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
                this.server = new TcpHandler(this.address, this.clientCommPort, ref this.clientReqQueue, ref this.clientRespQueue);

                this.clusterListener = new TcpHandler(this.address, this.clusterCommPort, ref this.clusterReqQueue, ref this.clusterRespQueue);

                // Start listening for client requests.
                this.server.Start();
                this.clusterListener.Start();

                LOG("Server Started");

                // Enter the listening loop.
                while (true)
                {
                    DEBUG("Waiting for a connection... ");
                    Thread.Sleep(10000);
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
                this.clientReqQueue.Complete();
                this.clientRespQueue.Complete();

                this.clusterReqQueue.Complete();
                this.clusterRespQueue.Complete();

            }
        }


    }
}
