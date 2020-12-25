using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPScanner
{
    class Formatter
    {
        public static bool TryParse(string ipEndPoint, out IPEndPoint result)
        {
            result = new IPEndPoint(0, 0);
            var temp = new string[2];
            if (!ipEndPoint.Contains(':'))
            {
                temp[0] = ipEndPoint;
                temp[1] = "80";
            }
            else
                temp = ipEndPoint.Split(':');
            IPAddress address;
            if (!IPAddress.TryParse(temp[0], out address))
            {
                MessageBox.Show("Please enter a valid IP or check ranges!");
                return false;
            }
            int port;
            if (!int.TryParse(temp[1], out port))
            {
                MessageBox.Show("Please enter a valid port or check ranges!");
                return false;
            }
            if (IPEndPoint.MinPort <= port && port <= IPEndPoint.MaxPort)
            {
                result = new IPEndPoint(address, port);
                return true;
            }
            else
            {
                MessageBox.Show("Port out of range!");
                return false;
            }
        }
        public static bool Format(string ipEndpoint, string range1, string range2, out List<IPEndPoint> ipEndpoints)
        {
            ipEndpoints = new List<IPEndPoint>();
            int r1, r2;
            if (ipEndpoint.Contains("X"))
            {
                if (!int.TryParse(range1, out r1))
                {
                    MessageBox.Show("Range 1 is invalid!");
                    return false;
                }
                if (!int.TryParse(range2, out r2))
                {
                    MessageBox.Show("Range 2 is invalid!");
                    return false;
                }
            }
            else
            {
                r1 = 0;
                r2 = r1;
            }
            var success = true;
            for (var i = r1; i <= r2; i++)
            {
                IPEndPoint ep;
                if (!TryParse(ipEndpoint.Replace("X", "" + i), out ep))
                {
                    success = false;
                    break;
                }
                ipEndpoints.Add(ep);
            }
            return success;
        }
    }
}
