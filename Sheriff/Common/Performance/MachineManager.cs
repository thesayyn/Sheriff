using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sheriff.Common.Performance
{
    class MachineManager
    {

        public static float GetGlobalRamUsage()
        {
            PerformanceCounter counter = new PerformanceCounter("Process", "Working Set", "_Total");
            counter.NextValue();
            Thread.Sleep(200);
            return counter.NextValue();
        }
        public static float GetGlobalCpuUsage()
        {
            PerformanceCounter counter = new PerformanceCounter("Process", "% User Time", "_Total");
            counter.NextValue();
            Thread.Sleep(200);
            return counter.NextValue() / Environment.ProcessorCount;
        }

        public static float GetRamUsage(Process process)
        {
            PerformanceCounter counter = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);
            counter.NextValue();
            Thread.Sleep(200);
            return counter.NextValue();
        }

        public static float GetCpuUsage(Process process)
        {
            PerformanceCounter counter = new PerformanceCounter("Process", "% User Time", process.ProcessName);
            counter.NextValue();
            Thread.Sleep(200);
            return counter.NextValue() / Environment.ProcessorCount;
        }

        public static double GetAppDomainCpuUsage(AppDomain hostDomain)
        {

            if (Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds > 0)
                return hostDomain.MonitoringTotalProcessorTime.TotalMilliseconds * 100 / Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
            return 0;
        }

        public static double GetAppDomainMemoryUsage(AppDomain hostDomain)
        {
            if (AppDomain.MonitoringSurvivedProcessMemorySize > 0)
                return (double)hostDomain.MonitoringSurvivedMemorySize * 100 / (double)AppDomain.MonitoringSurvivedProcessMemorySize;
            return 0;
        }

    }
}
