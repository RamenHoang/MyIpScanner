using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace IPScanner
{
    class RawSocketPing
    {
        public Socket pingSocket;                    // Raw socket handle

        public AddressFamily pingFamily;             // Indicates IPv4 or IPv6 ping

        public int pingTtl;                                 // Time-to-live value to set on ping

        public ushort pingId;                              // ID value to set in ping packet

        public ushort pingSequence;               // Current sending sequence number

        public int pingPayloadLength;          // Size of the payload in ping packet

        public int pingCount;                          // Number of times to send ping request

        public int pingOutstandingReceives;    // Number of outstanding receive operations

        public int pingReceiveTimeout;             // Timeout value to wait for ping response

        private IPEndPoint destEndPoint;                // Destination being pinged

        public IPEndPoint responseEndPoint;        // Contains the source address of the ping response

        public EndPoint castResponseEndPoint;   // Simple cast time used for the responseEndPoint

        private byte[] pingPacket;                         // Byte array of ping packet built

        private byte[] pingPayload;                       // Payload in the ping packet

        private byte[] receiveBuffer;                      // Buffer used to receive ping response

        private IcmpHeader icmpHeader;                // ICMP header built (for IPv4)

        private ArrayList protocolHeaderList;      // List of protocol headers to assemble into a packet

        private AsyncCallback receiveCallback;     // Async callback function called when a receive completes

        private DateTime pingSentTime;            // Timestamp of when ping request was sent

        public ManualResetEvent pingReceiveEvent;   // Event to signal main thread that receive completed

        public ManualResetEvent pingDoneEvent;        // Event to indicate all outstanding receives are done


        private int failCount = 0;

        //    this ping class can be disposed



        /// <summary>

        /// Base constructor that initializes the member variables to default values. It also

        /// creates the events used and initializes the async callback function.

        /// </summary>

        public RawSocketPing()

        {

            pingSocket = null;

            pingFamily = AddressFamily.InterNetwork;

            pingTtl = 8;

            pingPayloadLength = 8;

            pingSequence = 0;

            pingReceiveTimeout = 4000;

            pingOutstandingReceives = 0;

            destEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

            protocolHeaderList = new ArrayList();

            pingReceiveEvent = new ManualResetEvent(false);

            pingDoneEvent = new ManualResetEvent(false);

            receiveCallback = new AsyncCallback(PingReceiveCallback);

            icmpHeader = null;
        }



        /// <summary>

        /// Constructor that overrides several members of the ping packet such as TTL,

        /// payload length, ping ID, etc.

        /// </summary>

        /// <param name="af">Indicates whether we're doing IPv4 or IPv6 ping</param>

        /// <param name="ttlValue">Time-to-live value to set on ping packet</param>

        /// <param name="payloadSize">Number of bytes in ping payload</param>

        /// <param name="sendCount">Number of times to send a ping request</param>

        /// <param name="idValue">ID value to put into ping header</param>

        public RawSocketPing(

            AddressFamily af,

            int ttlValue,

            int payloadSize,

            int sendCount,

            ushort idValue,

            int timeOut

            ) : this()

        {

            pingFamily = af;

            pingTtl = ttlValue;

            pingPayloadLength = payloadSize;

            pingCount = sendCount;

            pingId = idValue;

            pingReceiveTimeout = timeOut;

        }



        /// <summary>

        /// This routine is called when the calling application is done with the ping class.

        /// This routine closes any open resource such as events and socket handles. This

        /// routine closes the socket handle and then blocks on the pingDoneEvent. This is

        /// necessary as if there is an outstanding async ReceiveFrom pending, it will complete

        /// with an error once the socket handle is closed. It is cleaner to wait for any

        /// async operations to complete before disposing of resources it may depend on.

        /// </summary>

        public void Close()

        {

            try

            {

                // Close the socket handle which will cause any async operations on it to complete with an error.


                if (pingSocket != null)

                {

                    pingSocket.Close();

                    pingSocket = null;

                }



                // Wait for the async handler to signal no more outstanding operations

                if (pingDoneEvent.WaitOne(10000, false) == false)

                {


                }



                // Close the opened events

                if (pingReceiveEvent != null)

                    pingReceiveEvent.Close();

                if (pingDoneEvent != null)

                    pingDoneEvent.Close();

            }

            catch (Exception err)

            {


                throw;

            }

        }



        /// <summary>

        /// Since ICMP raw sockets don't care about the port (as the ICMP protocol has no port

        /// field), we require the caller to just update the IPAddress of the destination

        /// although internally we keep it as an IPEndPoint since the SendTo method requires

        /// that (and the port is simply set to zero).

        /// </summary>

        public IPAddress PingAddress

        {

            get

            {

                return destEndPoint.Address;

            }

            set

            {

                destEndPoint = new IPEndPoint(value, 0);

            }

        }



        /// <summary>

        /// This routine initializes the raw socket, sets the TTL, allocates the receive

        /// buffer, and sets up the endpoint used to receive any ICMP echo responses.

        /// </summary>

        public void InitializeSocket()

        {

            IPEndPoint localEndPoint;



            if (destEndPoint.AddressFamily == AddressFamily.InterNetwork)

            {

                // Create the raw socket


                pingSocket = new Socket(destEndPoint.AddressFamily, SocketType.Raw, ProtocolType.Icmp);

                localEndPoint = new IPEndPoint(IPAddress.Any, 0);



                // Socket must be bound locally before socket options can be applied


                pingSocket.Bind(localEndPoint);




                pingSocket.SetSocketOption(

                    SocketOptionLevel.IP,

                    SocketOptionName.IpTimeToLive,

                    pingTtl

                    );



                // Allocate the buffer used to receive the response


                receiveBuffer = new byte[Ipv4Header.Ipv4HeaderLength + IcmpHeader.IcmpHeaderLength + pingPayloadLength];

                responseEndPoint = new IPEndPoint(IPAddress.Any, 0);

                castResponseEndPoint = (EndPoint)responseEndPoint;

            }



        }

        /// <summary>

        /// This routine builds the appropriate ICMP echo packet depending on the

        /// protocol family requested.

        /// </summary>

        public void BuildPingPacket()

        {

            // Initialize the socket if it hasn't already been done





            if (pingSocket == null)

            {

                InitializeSocket();

            }



            // Clear any existing headers in the list


            protocolHeaderList.Clear();



            if (destEndPoint.AddressFamily == AddressFamily.InterNetwork)

            {

                // Create the ICMP header and initialize the members


                icmpHeader = new IcmpHeader();



                icmpHeader.Id = pingId;

                icmpHeader.Sequence = pingSequence;

                icmpHeader.Type = IcmpHeader.EchoRequestType;

                icmpHeader.Code = IcmpHeader.EchoRequestCode;



                // Build the data payload of the ICMP echo request


                pingPayload = new byte[pingPayloadLength];



                for (int i = 0; i < pingPayload.Length; i++)

                {

                    pingPayload[i] = (byte)'e';

                }

                // Add ICMP header to the list of headers


                protocolHeaderList.Add(icmpHeader);

            }
        }

        /// <summary>

        /// This is the asynchronous callback that is fired when an async ReceiveFrom.

        /// An asynchronous ReceiveFrom is posted by calling BeginReceiveFrom. When this

        /// function is invoked, it calculates the elapsed time between when the ping

        /// packet was sent and when it was completed.

        /// </summary>

        /// <param name="ar">Asynchronous context for operation that completed</param>

        static void PingReceiveCallback(IAsyncResult ar)

        {

            RawSocketPing rawSock = (RawSocketPing)ar.AsyncState;

            TimeSpan elapsedTime;

            int bytesReceived = 0;

            ushort receivedId = 0;



            try

            {

                // Keep a count of how many async operations are outstanding -- one just completed

                //    so decrement the count.

                Interlocked.Decrement(ref rawSock.pingOutstandingReceives);



                // If we're done because ping is exiting and the socket has been closed,

                //    set the done event

                if (rawSock.pingSocket == null)

                {

                    if (rawSock.pingOutstandingReceives == 0)

                        rawSock.pingDoneEvent.Set();

                    return;

                }



                // Complete the receive op by calling EndReceiveFrom. This will return the number

                //    of bytes received as well as the source address of who sent this packet.

                bytesReceived = rawSock.pingSocket.EndReceiveFrom(ar, ref rawSock.castResponseEndPoint);



                // Calculate the elapsed time from when the ping request was sent and a response was

                //    received.

                elapsedTime = DateTime.Now - rawSock.pingSentTime;



                rawSock.responseEndPoint = (IPEndPoint)rawSock.castResponseEndPoint;



                // Here we unwrap the data received back into the respective protocol headers such

                //    that we can find the ICMP ID in the ICMP or ICMPv6 packet to verify that

                //    the echo response we received was really a response to our request.

                if (rawSock.pingSocket.AddressFamily == AddressFamily.InterNetwork)

                {

                    Ipv4Header v4Header;

                    IcmpHeader icmpv4Header;

                    byte[] pktIcmp;

                    int offset = 0;



                    // Remember, raw IPv4 sockets will return the IPv4 header along with all

                    //    subsequent protocol headers

                    v4Header = Ipv4Header.Create(rawSock.receiveBuffer, ref offset);

                    pktIcmp = new byte[bytesReceived - offset];

                    Array.Copy(rawSock.receiveBuffer, offset, pktIcmp, 0, pktIcmp.Length);

                    icmpv4Header = IcmpHeader.Create(pktIcmp, ref offset);



                    /*Console.WriteLine("Icmp.Id = {0}; Icmp.Sequence = {1}",

                        icmpv4Header.Id,

                        icmpv4Header.Sequence

                        );*/



                    receivedId = icmpv4Header.Id;

                }



                if (receivedId == rawSock.pingId)

                {

                    string elapsedString;



                    // Print out the usual statistics for ping

                    if (elapsedTime.Milliseconds < 1)

                        elapsedString = "<1";

                    else

                        elapsedString = "=" + elapsedTime.Milliseconds.ToString();

                }



                // Post another async receive if the count indicates for us to do so.

                if (rawSock.pingCount > 0)

                {

                    rawSock.pingSocket.BeginReceiveFrom(

                        rawSock.receiveBuffer,

                        0,

                        rawSock.receiveBuffer.Length,

                        SocketFlags.None,

                        ref rawSock.castResponseEndPoint,

                        rawSock.receiveCallback,

                        rawSock

                        );



                    // Keep track of outstanding async operations

                    Interlocked.Increment(ref rawSock.pingOutstandingReceives);

                }

                else

                {

                    // If we're done then set the done event

                    if (rawSock.pingOutstandingReceives == 0)

                        rawSock.pingDoneEvent.Set();

                }

                // If this is indeed the response to our echo request then signal the main thread

                //    that we received the response so it can send additional echo requests if

                //    necessary. This is done after another async ReceiveFrom is already posted.

                if (receivedId == rawSock.pingId)

                {

                    rawSock.pingReceiveEvent.Set();

                }

            }

            catch (SocketException err)

            {

                Console.WriteLine("Socket error occurred in async callback: {0}", err.Message);

            }

        }



        /// <summary>

        /// This function performs the actual ping. It sends the ping packets created to

        /// the destination and posts the async receive operation to receive the response.

        /// Once a ping is sent, it waits for the receive handler to indicate a response

        /// was received. If not it times out and indicates this to the user.

        /// </summary>

        public Boolean DoPing()

        {

            // If the packet hasn't already been built, then build it.



            if (protocolHeaderList.Count == 0)

            {

                BuildPingPacket();

            }






            try

            {

                // Post an async receive first to ensure we receive a response to the echo request

                //    in the event of a low latency network.

                pingSocket.BeginReceiveFrom(

                    receiveBuffer,

                    0,

                    receiveBuffer.Length,

                    SocketFlags.None,

                    ref castResponseEndPoint,

                    receiveCallback,

                    this

                    );

                // Keep track of how many outstanding async operations there are

                Interlocked.Increment(ref pingOutstandingReceives);



                // Send an echo request and wait for the response

                while (pingCount > 0)

                {

                    Interlocked.Decrement(ref pingCount);



                    // Increment the sequence count in the ICMP header

                    if (destEndPoint.AddressFamily == AddressFamily.InterNetwork)

                    {

                        icmpHeader.Sequence = (ushort)(icmpHeader.Sequence + (ushort)1);



                        // Build the byte array representing the ping packet. This needs to be done

                        //    before ever send because we change the sequence number (which will affect

                        //    the calculated checksum).

                        pingPacket = icmpHeader.BuildPacket(protocolHeaderList, pingPayload);

                    }


                    // Mark the time we sent the packet

                    pingSentTime = DateTime.Now;



                    // Send the echo request

                    pingSocket.SendTo(pingPacket, destEndPoint);



                    // Wait for the async handler to indicate a response was received

                    if (pingReceiveEvent.WaitOne((int)(pingReceiveTimeout/4), false) == false)

                    {

                        // timeout occurred
                        failCount += 1;

                    }

                    else

                    {

                        // Reset the event

                        pingReceiveEvent.Reset();
                        return true;
                    }



                    // Sleep for a short time before sending the next request

                    //Thread.Sleep(1000);

                }

                if (failCount == 4)
                    return false;

                return true;

            }

            catch (SocketException err)

            {

                Console.WriteLine("Socket error occurred: {0}", err.Message);

                throw;

            }

        }
    }
}
