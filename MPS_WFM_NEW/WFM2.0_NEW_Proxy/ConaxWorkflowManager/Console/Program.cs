using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;


namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Starting ConaxWorkflowManager as Console application");
            Assembly ass = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(ass.Location);
            
            System.Console.WriteLine("Version " + fvi.FileVersion);

            Runner runner = new Runner();
            Thread statThread = new Thread(runner.Run);
            statThread.Start();

            //while (true) {
            //    if (!runner.IsMaster)                
            //        Environment.Exit(0);
                
            //    Thread.Sleep(1000);
            //}
            System.Console.ReadLine();
        }
    }
}
