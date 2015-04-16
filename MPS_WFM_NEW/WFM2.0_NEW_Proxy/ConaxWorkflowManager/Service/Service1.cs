using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using System.Threading;

namespace Service
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Runner runner = new Runner();

            Thread t = new Thread(new ThreadStart(runner.Run));
            t.Start();

            //while (true)
            //{
            //    if (!runner.IsMaster)
            //        Environment.Exit(0);

            //    Thread.Sleep(1000);
            //}

            System.Console.ReadLine();
        }

        protected override void OnStop()
        {
        }
    }
}
