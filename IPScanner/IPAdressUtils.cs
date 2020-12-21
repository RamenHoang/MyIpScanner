using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPScanner
{
    class IPAdressUtils
    {
        public static IPAddress getLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        Console.WriteLine(ni.Name);
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static IPAddress getSubnetMaskFromIP(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        public static string getDisplayIPAdrress()
        {
            string displayString = "", displayNetMask = "";

            IPAddress localIpAddress = getLocalIPAddress();
            byte[] localIpBytes = localIpAddress.GetAddressBytes();

            IPAddress localNetMask = getSubnetMaskFromIP(localIpAddress);
            byte[] localNetmaskBytes = localNetMask.GetAddressBytes();


            for (int i = 0; i < 4; i++)
            {
                byte partNet = (byte)(localIpBytes[i] & localNetmaskBytes[i]);

                if (partNet == localIpBytes[i])
                    displayString += partNet + ".";
                else
                    displayString += "X" + ".";

                displayNetMask += localNetmaskBytes[i] + ".";
            }

            displayString = displayString.Remove(displayString.Length - 1, 1);
            displayNetMask = displayNetMask.Remove(displayNetMask.Length - 1, 1);

            return displayString + "-" + displayNetMask;
        }
    }
}
