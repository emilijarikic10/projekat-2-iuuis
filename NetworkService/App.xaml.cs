using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;


namespace NetworkService
{
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            foreach (Process process in Process.GetProcessesByName("MeteringSimulator"))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
            }

            base.OnExit(e);
        }
    }
}
