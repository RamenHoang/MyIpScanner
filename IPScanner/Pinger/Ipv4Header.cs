﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace IPScanner
{
    class Ipv4Header : ProtocolHeader

    {

        private byte ipVersion;              // actually only 4 bits

        private byte ipLength;                // actually only 4 bits

        private byte ipTypeOfService;

        private ushort ipTotalLength;

        private ushort ipId;

        private ushort ipOffset;

        private byte ipTtl;

        private byte ipProtocol;

        private ushort ipChecksum;

        private IPAddress ipSourceAddress;

        private IPAddress ipDestinationAddress;



        static public int Ipv4HeaderLength = 20;



        /// <summary>

        /// Simple constructor that initializes the members to zero.

        /// </summary>

        public Ipv4Header() : base()

        {

            ipVersion = 4;

            ipLength = (byte)Ipv4HeaderLength;    // Set the property so it will convert properly

            ipTypeOfService = 0;

            ipId = 0;

            ipOffset = 0;

            ipTtl = 1;

            ipProtocol = 0;

            ipChecksum = 0;

            ipSourceAddress = IPAddress.Any;

            ipDestinationAddress = IPAddress.Any;

        }



        /// <summary>

        /// Gets and sets the IP version. This should be 4 to indicate the IPv4 header.

        /// </summary>

        public byte Version

        {

            get

            {

                return ipVersion;

            }

            set

            {

                ipVersion = value;

            }

        }



        /// <summary>

        /// Gets and sets the length of the IPv4 header. This property takes and returns

        /// the number of bytes, but the actual field is the number of 32-bit DWORDs

        /// (the IPv4 header is a multiple of 4-bytes).

        /// </summary>

        public byte Length

        {

            get

            {

                return (byte)(ipLength * 4);

            }

            set

            {

                ipLength = (byte)(value / 4);

            }

        }



        /// <summary>

        /// Gets and sets the type of service field of the IPv4 header. Since it

        /// is a byte, no byte order conversion is required.

        /// </summary>

        public byte TypeOfService

        {

            get

            {

                return ipTypeOfService;

            }

            set

            {

                ipTypeOfService = value;

            }

        }



        /// <summary>

        ///  Gets and sets the total length of the IPv4 header and its encapsulated

        ///  payload. Byte order conversion is required.

        /// </summary>

        public ushort TotalLength

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder((short)ipTotalLength);

            }

            set

            {

                ipTotalLength = (ushort)IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is required.

        /// </summary>

        public ushort Id

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder((short)ipId);

            }

            set

            {

                ipId = (ushort)IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        /// Gets and sets the offset field of the IPv4 header which indicates if

        /// IP fragmentation has occurred.

        /// </summary>

        public ushort Offset

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder((short)ipOffset);

            }

            set

            {

                ipOffset = (ushort)IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        /// Gets and sets the time-to-live (TTL) value of the IP header. This field

        /// determines how many router hops the packet is valid for.

        /// </summary>

        public byte Ttl

        {

            get

            {

                return ipTtl;

            }

            set

            {

                ipTtl = value;

            }

        }



        /// <summary>

        /// Gets and sets the protocol field of the IPv4 header. This field indicates

        /// what the encapsulated protocol is.

        /// </summary>

        public byte Protocol

        {

            get

            {

                return ipProtocol;

            }

            set

            {

                ipProtocol = value;

            }

        }











        /// <summary>

        /// Gets and sets the checksum field of the IPv4 header. For the IPv4 header, the

        /// checksum is calculated over the header and payload. Note that this field isn't

        /// meant to be set by the user as the GetProtocolPacketBytes method computes the

        /// checksum when the packet is built.

        /// </summary>

        public ushort Checksum

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder((short)ipChecksum);

            }

            set

            {

                ipChecksum = (ushort)IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        /// Gets and sets the source IP address of the IPv4 packet. This is stored

        /// as an IPAddress object which will be serialized to the appropriate

        /// byte representation in the GetProtocolPacketBytes method.

        /// </summary>

        public IPAddress SourceAddress

        {

            get

            {

                return ipSourceAddress;

            }

            set

            {

                ipSourceAddress = value;

            }

        }



        /// <summary>

        /// Gets and sets the destination IP address of the IPv4 packet. This is stored

        /// as an IPAddress object which will be serialized to the appropriate byte

        /// representation in the GetProtocolPacketBytes method.

        /// </summary>

        public IPAddress DestinationAddress

        {

            get

            {

                return ipDestinationAddress;

            }

            set

            {

                ipDestinationAddress = value;

            }

        }



        /// <summary>

        /// This routine creates an instance of the Ipv4Header class from a byte

        /// array that is a received IGMP packet. This is useful when a packet

        /// is received from the network and the header object needs to be

        /// constructed from those values.

        /// </summary>

        /// <param name="ipv4Packet">Byte array containing the binary IPv4 header</param>

        /// <param name="bytesCopied">Number of bytes used in header</param>

        /// <returns>Returns the Ipv4Header object created from the byte array</returns>

        static public Ipv4Header Create(byte[] ipv4Packet, ref int bytesCopied)

        {

            Ipv4Header ipv4Header = new Ipv4Header();



            // Make sure byte array is large enough to contain an IPv4 header

            if (ipv4Packet.Length < Ipv4Header.Ipv4HeaderLength)

                return null;



            // Decode the data in the array back into the class properties

            ipv4Header.ipVersion = (byte)((ipv4Packet[0] >> 4) & 0xF);

            ipv4Header.ipLength = (byte)(ipv4Packet[0] & 0xF);

            ipv4Header.ipTypeOfService = ipv4Packet[1];

            ipv4Header.ipTotalLength = BitConverter.ToUInt16(ipv4Packet, 2);

            ipv4Header.ipId = BitConverter.ToUInt16(ipv4Packet, 4);

            ipv4Header.ipOffset = BitConverter.ToUInt16(ipv4Packet, 6);

            ipv4Header.ipTtl = ipv4Packet[8];

            ipv4Header.ipProtocol = ipv4Packet[9];

            ipv4Header.ipChecksum = BitConverter.ToUInt16(ipv4Packet, 10);



            ipv4Header.ipSourceAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 12));

            ipv4Header.ipDestinationAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 16));



            bytesCopied = ipv4Header.Length;



            return ipv4Header;

        }



        /// <summary>

        /// This routine takes the properties of the IPv4 header and marhalls them into

        /// a byte array representing the IPv4 header that is to be sent on the wire.

        /// </summary>

        /// <param name="payLoad">The encapsulated headers and data</param>

        /// <returns>A byte array of the IPv4 header and payload</returns>

        public override byte[] GetProtocolPacketBytes(byte[] payLoad)

        {

            byte[] ipv4Packet, byteValue;

            int index = 0;



            // Allocate space for the IPv4 header plus payload

            ipv4Packet = new byte[Ipv4HeaderLength + payLoad.Length];



            ipv4Packet[index++] = (byte)((ipVersion << 4) | ipLength);

            ipv4Packet[index++] = ipTypeOfService;



            byteValue = BitConverter.GetBytes(ipTotalLength);

            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);

            index += byteValue.Length;



            byteValue = BitConverter.GetBytes(ipId);

            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);

            index += byteValue.Length;



            byteValue = BitConverter.GetBytes(ipOffset);

            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);

            index += byteValue.Length;



            ipv4Packet[index++] = ipTtl;

            ipv4Packet[index++] = ipProtocol;

            ipv4Packet[index++] = 0; // Zero the checksum for now since we will

            ipv4Packet[index++] = 0; // calculate it later



            // Copy the source address

            byteValue = ipSourceAddress.GetAddressBytes();

            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);

            index += byteValue.Length;



            // Copy the destination address

            byteValue = ipDestinationAddress.GetAddressBytes();

            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);

            index += byteValue.Length;



            // Copy the payload

            Array.Copy(payLoad, 0, ipv4Packet, index, payLoad.Length);

            index += payLoad.Length;



            // Compute the checksum over the entire packet (IPv4 header + payload)

            Checksum = ComputeChecksum(ipv4Packet);



            // Set the checksum into the built packet

            byteValue = BitConverter.GetBytes(ipChecksum);

            Array.Copy(byteValue, 0, ipv4Packet, 10, byteValue.Length);



            return ipv4Packet;

        }
    }
}
