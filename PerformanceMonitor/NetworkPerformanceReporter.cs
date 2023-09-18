using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using PerformanceMonitor;

namespace PerformanceMonitor
{
    public sealed class NetworkPerformanceReporter
    {
        public static bool mobile = false;
        private static bool _stopping;
        private static TraceEventSession etwSession;
        public static Dictionary<string, Counters> dicData = new Dictionary<string, Counters>();
        private readonly Counters _mCounters = new Counters();

        public static NetworkPerformanceReporter Create()
        {
            var networkPerformancePresenter = new NetworkPerformanceReporter();
            networkPerformancePresenter.Initialise();
            return networkPerformancePresenter;
        }

        private void Initialise()
        {
            Task.Run(() => StartEtwSession());
        }

        private void StartEtwSession()
        {
            var etwtask = Task.Run(() =>
            {
                using (etwSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
                {
                    etwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                    etwSession.Source.Kernel.TcpIpRecv += KernelOnTcpIpRecv;
                    etwSession.Source.Kernel.TcpIpRecvIPV6 += KernelOnTcpIpRecvIpv6;
                    etwSession.Source.Kernel.TcpIpSend += KernelOnTcpIpSend;
                    etwSession.Source.Kernel.TcpIpSendIPV6 += KernelOnTcpIpSendIpv6;
                    etwSession.Source.Process();
                }
            });
            Task.WaitAll(etwtask);
            Console.WriteLine("\nStop logging...");
            Environment.Exit(0);
        }

        private void KernelOnTcpIpSendIpv6(TcpIpV6SendTraceData data)
        {
            if (_stopping) return;
            if (MainWindow.bmobile == "true" && mobile || MainWindow.bmobile == "false")
                lock (_mCounters)
                {
                    string strKey = string.Empty;
                    if (!string.IsNullOrEmpty(MainWindow.PortName))
                    {
                        strKey = data.ProcessName + "-" + data.dport;
                    }
                    else
                    {
                        strKey = (MainWindow.processLvl == CONSTANTS.PORT_WISE_LOG) ? (data.ProcessName + "-" + data.dport) : data.ProcessName;
                    }
                    if (string.IsNullOrEmpty(strKey)) { return; }
                    if (dicData.ContainsKey(strKey))
                        dicData[strKey].Sent =
                            dicData[strKey].Sent + data.size / 1024f / 1024f;
                    else
                        dicData.Add(strKey,
                            new Counters {Received = 0, Sent = data.size / 1024f / 1024f, Port = data.dport});
                }
        }

        private void KernelOnTcpIpSend(TcpIpSendTraceData data)
        {
            if (_stopping) return;
            if (MainWindow.bmobile == "true" && mobile || MainWindow.bmobile == "false")
                lock (_mCounters)
                {

                    string strKey = string.Empty;
                    if (!string.IsNullOrEmpty(MainWindow.PortName))
                    {
                        strKey = data.ProcessName + "-" + data.dport;
                    }
                    else
                    {
                        strKey = (MainWindow.processLvl == CONSTANTS.PORT_WISE_LOG) ? (data.ProcessName + "-" + data.dport) : data.ProcessName;
                    }
                    if (string.IsNullOrEmpty(strKey)) { return; }
                    if (dicData.ContainsKey(strKey))
                        dicData[strKey].Sent =
                            dicData[strKey].Sent + data.size / 1024f / 1024f;
                    else
                        dicData.Add(strKey,
                            new Counters { Received = 0, Sent = data.size / 1024f / 1024f , Port = data.dport});

                    
                }
        }

        private void KernelOnTcpIpRecvIpv6(TcpIpV6TraceData data)
        {
            if (_stopping) return;
            if (MainWindow.bmobile == "true" && mobile || MainWindow.bmobile == "false")
                lock (_mCounters)
                {
                    string strKey = string.Empty;
                    if (!string.IsNullOrEmpty(MainWindow.PortName))
                    {
                        strKey = data.ProcessName + "-" + data.dport;
                    }
                    else
                    {
                        strKey = (MainWindow.processLvl == CONSTANTS.PORT_WISE_LOG) ? (data.ProcessName + "-" + data.dport) : data.ProcessName;
                    }
                    if (string.IsNullOrEmpty(strKey)) { return; }
                    if (dicData.ContainsKey(strKey))
                        dicData[strKey].Received =
                            dicData[strKey].Received + data.size / 1024f / 1024f;
                    else
                        dicData.Add(strKey,
                            new Counters {Received = data.size / 1024f / 1024f, Sent = 0 , Port = data.dport});
                }
        }

        private void KernelOnTcpIpRecv(TcpIpTraceData data)
        {
            if (_stopping) return;
            if (MainWindow.bmobile == "true" && mobile || MainWindow.bmobile == "false")
                lock (_mCounters)
                {
                    string strKey = string.Empty;
                    if (!string.IsNullOrEmpty(MainWindow.PortName))
                    {
                        strKey = data.ProcessName + "-" + data.dport;
                    }
                    else
                    {
                        strKey = (MainWindow.processLvl == CONSTANTS.PORT_WISE_LOG) ? (data.ProcessName + "-" + data.dport) : data.ProcessName;
                    }
                    if (string.IsNullOrEmpty(strKey)) { return; }
                    if (dicData.ContainsKey(strKey))
                        dicData[strKey].Received =
                            dicData[strKey].Received + data.size / 1024f / 1024f;
                    else
                        dicData.Add(strKey,
                            new Counters { Received = data.size / 1024f / 1024f, Sent = 0 , Port = data.dport});

                }
        }

        public static void StopSessions()
        {
            _stopping = true;
            etwSession?.Dispose();
        }

        public class Counters
        {
            public double Received { get; set; }
            public double Sent { get; set; }

            public int Port { get; set; }
        }
		
    }
	
	internal class CONSTANTS
    {
        internal const int PORT_WISE_LOG = 2;
        internal const int APP_WISE_LOG  = 1;
    }
}