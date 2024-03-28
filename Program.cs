using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace SimplePerformanceCheck
{
    internal class Program
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        private static IntPtr ThisConsole = GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int MAXIMIZE = 3;
        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            ShowWindow(ThisConsole, MAXIMIZE);

            GetStarted();
        }

        #region Performance evaluating methods

        //lets get started
        private static void GetStarted()
        {
            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("To check performance of a process with minimal paramaters in less than a minute.");
            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("Let's get started!");
            Console.WriteLine();

            Console.WriteLine(" \u25A0 Press 1, to get list of processes, their associated name & Id.");
            Console.WriteLine(" \u25A0 Press 2, if process name already known.");
            Console.WriteLine(" \u25A0 Press any key, to exit.");
            string processName = Console.ReadLine();

            if (!string.IsNullOrEmpty(processName))
            {
                if (processName.Trim() == "1")
                {
                    GetListOfProcesses();
                    GetProcessPerformance();
                }
                else if (processName.Trim() == "2")
                {
                    GetProcessPerformance();
                }
            }

            Console.WriteLine();
            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("The performance check is been completed.");
            Console.WriteLine("*********************************************************************************");

            Console.ReadLine();
        }


        //WMI - Windows Management Instrumentation class in the Windows operating system
        //provides performance data related to the processor (CPU) usage on a Windows system
        //Method - lists out all properties that are responsible for performance 
        private static void Properties_Win32PerfFormattedDataPerfOSProcessor()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor");
            var collection = searcher.Get().Cast<ManagementObject>().ToList();
            foreach (ManagementObject mo in collection)
            {
                foreach (PropertyData prop in mo.Properties)
                {
                    Console.WriteLine("{0}: {1}", prop.Name, prop.Value);
                }
            }
            Console.WriteLine("--------------------------------------------");

            ////used to get total CPU usage 
            //var cpuTimes = searcher.Get()
            //    .Cast<ManagementObject>()
            //    .Select(mo => new
            //    {
            //        Name = mo["Name"],
            //        Usage = mo["PercentProcessorTime"]
            //    }).ToList();

            //var query = cpuTimes.Where(x => x.Name.ToString() == "_Total").Select(x => x.Usage);
            //var cpuUsage = query.SingleOrDefault();
            //Console.WriteLine($"CPU Usage: {cpuUsage}");
        }


        //WMI - Windows Management Instrumentation class in the Windows operating system
        //service that performs specific functions at the backend
        private static void Properties_Win32Service()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Service");
            var collection = searcher.Get().Cast<ManagementObject>().ToList();
            foreach (ManagementObject mo in collection)
            {
                foreach (PropertyData prop in mo.Properties)
                {
                    Console.WriteLine("{0}: {1}", prop.Name, prop.Value);
                }
                Console.WriteLine("--------------------------------------------");
            }

        }


        //accesses performance counter data on Windows systems
        //components: CPU, memory, disk, network usage
        //displays performance details after 2 seconds, 20 times 
        private static void PerformanceCounterForTotalProcessorTime()
        {
            var perfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            int repeat = 20;
            for (int each = 0; each < repeat; each++)
            {
                Thread.Sleep(2000);
                Console.WriteLine(perfCounter.NextValue() + "%");
            }

            Console.Read();
        }


        //accesses performance counter data on Windows systems
        //components: CPU, memory, disk, network usage
        //processes(instances) are checked for processor time
        private static void PerformanceCounterForEachInstance()
        {
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
            string[] instances = cat.GetInstanceNames();
            foreach (string instance in instances)
            {
                var perfCounter = new PerformanceCounter("Process", "% Processor Time", instance, true);
                Console.WriteLine($"{instance} : {perfCounter.NextValue()}%");

            }
        }


        //to get simple statistics on the performance of any process
        private static void GetProcessPerformance()
        {
            try
            {
                #region process selection
                Spacing();
                Console.WriteLine($"Enter the process name :");
                string processName = Console.ReadLine();
                #endregion



                #region process instance evaluation
                Spacing();
                var processes = Process.GetProcessesByName(processName);
                Console.WriteLine();
                Console.WriteLine("Process instances are as follows: ");
                foreach (var eachProcess in processes)
                    Console.WriteLine($" \u25A0 {eachProcess.ProcessName} [Id: {eachProcess.Id} ]");

                if (processes.Length <= 0)
                {
                    WriteColoredLine(" \u25A0 No instances found.");
                    return;
                }
                #endregion



                #region process instance selection
                Console.WriteLine();
                Console.WriteLine($"Select one instance and enter the id :");
                string processInstanceId = Console.ReadLine();//"14808";
                int processInstanceIdValue = Convert.ToInt32(processInstanceId);

                var doesInstanceExists = processes.Any(pr => pr.Id == processInstanceIdValue);
                if (doesInstanceExists)
                {
                    //get & display performance details
                    Spacing();
                    var item = Process.GetProcessById(processInstanceIdValue);
                    Console.WriteLine($"{item.ProcessName} [Id: {item.Id} ]");
                    Console.WriteLine();

                    DisplayPeformanceDetails(item, processInstanceIdValue);
                }
                else
                    WriteColoredLine("Incorrect instance selection.");

                #endregion
            }
            catch
            {
                WriteColoredText("Verify input or validate if service is operational.");
            }
        }


        //display performance details of a process
        private static void DisplayPeformanceDetails(Process item, int processId)
        {

            WriteForegroundColorText(" StartTime            PhysicalMemoryUsage   UserProcessorTime   PrivilegedProcessorTime   TotalProcessorTime    PagedSystemMemorySize   PagedMemorySize");
            Console.WriteLine("────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");

            long physicalMemoryUsage = 0;//physical memory allocated for the associated process
            TimeSpan userProcessorTime = default;//amount of time the CPU was busy executing code in user space
            TimeSpan privilegedProcessorTime = default;//time that a processor is busy executing the threads of a process
            TimeSpan totalProcessorTime = default;//userprocessortime + privilegedprocessortime
            long pagedSystemMemorySize = 0;//In paging, memory is divided into fixed-size blocks called pages, and processes are allocated memory in terms of these pages.
            long pagedMemorySize = 0;//the process of storing a portion of an executing process on disk or secondary memory

            int repeat = 30;
            for (int each = 0; each < repeat; each++)
            {
                try
                {
                    if (each != 0)
                        item = Process.GetProcessById(processId);

                    if (physicalMemoryUsage == 0)
                        DisplayInitialPerformanceValues(item);
                    else
                        DisplayNextPerformanceValues(item, physicalMemoryUsage, userProcessorTime, privilegedProcessorTime, totalProcessorTime, pagedSystemMemorySize, pagedMemorySize);

                    physicalMemoryUsage = item.WorkingSet64;
                    userProcessorTime = item.UserProcessorTime;
                    privilegedProcessorTime = item.PrivilegedProcessorTime;
                    totalProcessorTime = item.TotalProcessorTime;
                    pagedSystemMemorySize = item.PagedSystemMemorySize64;
                    pagedMemorySize = item.PagedMemorySize64;
                }
                catch
                {
                    WriteColoredText("Validate if service is operational.");
                }

                Thread.Sleep(2000);

                Console.WriteLine();
                Console.WriteLine("────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");

            }
        }


        //get the list of processes & their Ids
        private static void GetListOfProcesses()
        {
            Console.WriteLine($"Processes are as follows:");
            var processes = Process.GetProcesses().ToList();

            foreach (var item in processes)
            {
                try
                {
                    Console.WriteLine($"Started at {item.StartTime} : {item.ProcessName} [Id: {item.Id} ]");
                }
                catch (Exception e)
                {
                    WriteColoredLine($"{e.Message} [ {item.ProcessName} ]", ConsoleColor.Red);
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Number of processes: {processes.ToList().Count}");
        }

        #endregion

        #region Other methods

        private static void Spacing()
        {
            Console.WriteLine();
            Console.WriteLine("───────────────────────────────────────────────────────────────────");
        }

        private static void DisplayInitialPerformanceValues(Process process)
        {
            Console.Write($" {process.StartTime}  ");
            Console.Write($"{ConvertBytesToKilobytes(process.WorkingSet64)} kb       ");
            Console.Write($"{process.UserProcessorTime.ToString(@"hh\:mm\:ss\.fffffff")}    ");
            Console.Write($"{process.PrivilegedProcessorTime.ToString(@"hh\:mm\:ss\.fffffff")}          ");
            Console.Write($"{process.TotalProcessorTime.ToString(@"hh\:mm\:ss\.fffffff")}      ");
            Console.Write($"{ConvertBytesToKilobytes(process.PagedSystemMemorySize64)} kb           ");
            Console.Write($"{ConvertBytesToKilobytes(process.PagedMemorySize64)} kb");
        }

        private static void DisplayNextPerformanceValues(Process process, long physicalMemoryUsage, TimeSpan userProcessorTime, TimeSpan privilegedProcessorTime, TimeSpan totalProcessorTime, long pagedSystemMemorySize, long pagedMemorySize)
        {
            Console.Write($" {process.StartTime}  ");
            ConsoleWriteline(process.WorkingSet64, physicalMemoryUsage, $"{ConvertBytesToKilobytes(process.WorkingSet64)} kb       ");
            ConsoleWriteline(process.UserProcessorTime, userProcessorTime, $"{process.UserProcessorTime.ToString(@"hh\:mm\:ss\.fffffff")}    ");
            ConsoleWriteline(process.PrivilegedProcessorTime, privilegedProcessorTime, $"{process.PrivilegedProcessorTime.ToString(@"hh\:mm\:ss\.fffffff")}          ");
            ConsoleWriteline(process.TotalProcessorTime, totalProcessorTime, $"{process.TotalProcessorTime.ToString(@"hh\:mm\:ss\.fffffff")}      ");
            ConsoleWriteline(process.PagedSystemMemorySize64, pagedSystemMemorySize, $"{ConvertBytesToKilobytes(process.PagedSystemMemorySize64)} kb           ");
            ConsoleWriteline(process.PagedMemorySize64, pagedMemorySize, $"{ConvertBytesToKilobytes(process.PagedMemorySize64)} kb");
        }

        private static void ConsoleWriteline<T>(T value1, T value2, string toBeDisplayed) where T : IComparable<T>
        {
            if (value1.CompareTo(value2) > 0)
                WriteColoredText(toBeDisplayed);
            else if (value1.CompareTo(value2) < 0)
                WriteColoredText(toBeDisplayed, ConsoleColor.Green);
            else
                Console.Write(toBeDisplayed);
        }

        private static string ConvertBytesToKilobytes(long bytes)
        {
            return (bytes / 1024.0).ToString(@"F5");
        }

        private static void WriteColoredText(string message, ConsoleColor color = ConsoleColor.Red)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }

        private static void WriteColoredLine(string message, ConsoleColor color = ConsoleColor.Red)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void WriteForegroundColorText(string message, ConsoleColor color = ConsoleColor.Blue)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        #endregion
    }
}
