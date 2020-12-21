using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace IPScanner
{
    class Tester
    {
        public enum IPScannerProtocol
        {
            Tcp, Icmp
        }

        public enum IPTestStatus
        {
            Success, Failure, Error
        }

        public class TestResults
        {
            private volatile List<IPEndPoint> validEndPoints;
            public List<IPEndPoint> ValidEndPoints { get => validEndPoints; set => validEndPoints = value; }
            public int Valid { get => ValidEndPoints.Count; }
            private volatile int bad;
            public int Bad { get => bad; set => bad = value; }
            private volatile int error;
            public int Error { get => error; set => error = value; }
            private volatile int totalTested;
            public int TotalTested { get => totalTested; set => totalTested = value; }
            public TestResults()
            {
                validEndPoints = new List<IPEndPoint>();
                Clear();
            }
            public void Clear()
            {
                validEndPoints.Clear();
                bad = 0;
                error = 0;
                totalTested = 0;
            }
        }
        public int Timeout { get; set; }

        public IPScannerProtocol Protocol { get; set; }
        public delegate void IPTested(IPEndPoint address, IPTestStatus status);
        public delegate void TestFinished(TestResults results);
        private IPTested onIPTested;
        private TestFinished onTestFinished;
        public IPTested OnIPTested
        {
            get
            {
                return onIPTested;
            }
            set
            {
                if (!isRunning)
                    onIPTested = value;
            }
        }
        public TestFinished OnTestFinished
        {
            get
            {
                return onTestFinished;
            }
            set
            {
                if (!isRunning)
                    onTestFinished = value;
            }
        }
        private int threads;
        public int Threads
        {
            get
            {
                return threads;
            }
            set
            {
                if (!isRunning)
                    threads = value;
            }
        }

        private List<IPEndPoint> addresses;
        public List<IPEndPoint> Addresses
        {
            get
            {
                return addresses;
            }
            set
            {
                if (!isRunning)
                    addresses = value;
            }
        }
        private List<Thread> threadList;
        public TestResults Results { get; private set; }

        private volatile bool isRunning;
        private volatile bool stop;

        

        public Tester()
        {
            OnIPTested = (IPEndPoint o, IPTestStatus status) => { return; };
            OnTestFinished = (TestResults res) => { return; };
            addresses = new List<IPEndPoint>();
            threadList = new List<Thread>();
            Results = new TestResults();
            stop = false;
            isRunning = false;
            Timeout = 20000;
            Protocol = IPScannerProtocol.Tcp;
            Threads = 10;
        }
        public void Start()
        {
            if (isRunning)
                return;
            Results.Clear();
            stop = false;
            isRunning = true;
            if (Threads < 1)
                return;
            else if (Threads > Addresses.Count)
            {
                Threads = Addresses.Count;
            }
            var perThread = calc_perThread();
            var left = calc_left();
            for (var i = 0; i < Threads - 1; i++)
            {
                var thr = new Thread(new ParameterizedThreadStart(thr_Method));
                threadList.Add(thr);
                var ips = Addresses.GetRange(i * perThread, perThread);
                thr.Start(ips);
            }
            var thrlast = new Thread(new ParameterizedThreadStart(thr_Method));
            threadList.Add(thrlast);
            thrlast.Start(Addresses.GetRange((Threads - 1) * perThread, perThread + left));

            new Thread(new ThreadStart(threadWatcherMethod)).Start();
        }

        public void Stop()
        {
            stop = true;
        }
        
        private void thr_Method(object o)
        {
            List<IPEndPoint> addresses = (List<IPEndPoint>)o; 
            if (Protocol == IPScannerProtocol.Tcp) 
                for (var i = 0; i < addresses.Count && !stop; i++)
                {
                    IPTestStatus status = TestTCP(addresses[i]);
                    change_results(status, addresses[i]);
                }
            else if (Protocol == IPScannerProtocol.Icmp)
                for (var i = 0; i < addresses.Count && !stop; i++)
                {
                    IPTestStatus status = TestICMP(addresses[i]);
                    change_results(status, addresses[i]);
                }
        }

        private void threadWatcherMethod()
        {
            foreach (var thr in threadList)
            {
                thr.Join();
            }
            threadList.Clear();
            isRunning = false;
            OnTestFinished(Results);
        }

        private int calc_perThread()
        {
            return Addresses.Count / Threads;
        }

        private int calc_left()
        {
            return Addresses.Count % Threads;
        }

        private readonly object lock_ = new object();

        private void change_results(IPTestStatus iPTestStatus, IPEndPoint address)
        {
            lock (lock_)
            {
                Results.TotalTested++;
                switch (iPTestStatus)
                {
                    case IPTestStatus.Success: Results.ValidEndPoints.Add(address); break;
                    case IPTestStatus.Failure: Results.Bad++; break;
                    case IPTestStatus.Error: Results.Error++; break;
                }
            }
            OnIPTested(address, iPTestStatus);
        }

        public IPTestStatus TestTCP(IPEndPoint address)
        {
            IPTestStatus status;
            try
            {
                using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    IAsyncResult result = null;
                    try
                    {
                        result = socket.BeginConnect(address, null, null);
                        result.AsyncWaitHandle.WaitOne(Timeout, true);
                        status = socket.Connected ? IPTestStatus.Success : IPTestStatus.Failure;
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if (socket.Connected && result != null)
                        {
                            socket.EndConnect(result);
                        }
                        socket.Close();
                    }
                }
            }
            catch
            {
                status = IPTestStatus.Error;
            }
            return status;
        }

        public IPTestStatus TestICMP(IPEndPoint ip)
        {
            using (Ping pinger = new Ping())
            {
                IPTestStatus status;
                try
                {
                    PingReply reply = pinger.Send(ip.Address, Timeout);
                    status = reply.Status == IPStatus.Success ? IPTestStatus.Success : IPTestStatus.Failure;
                }
                catch (PingException)
                {
                    status = IPTestStatus.Failure;
                }
                catch (Exception)
                {
                    status = IPTestStatus.Error;
                }
                return status;
            }
        }
    }
}
