﻿using System;
using System.Linq;
using System.Threading;
using MMBot;
using System.ServiceProcess;

namespace mmbot
{

    class Program 
    {

        static void Main(string[] args)
        {
            var options = new Options();
            CommandLine.Parser.Default.ParseArguments(args, options);

            if (options.ShowHelp)
            {
                return;
            }

            if (options.RunAsService)
            {
                ServiceBase.Run(new ServiceBase[] { new Service(options) });
            }
            else
            {
                if (options.LastParserState != null && options.LastParserState.Errors.Any())
                {
                    return;
                }

                if (options.Parameters != null && options.Parameters.Any())
                {
                    options.Parameters.ForEach(Console.WriteLine);
                }

                if (options.Init)
                {
                    Initializer.InitializeCurrentDirectory();
                    return;
                }

                SetupRobot(options);
            }
        }

     

        private static void SetupRobot(Options options)
        {
            var childAppDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"));
            var wrapper = childAppDomain.CreateInstanceAndUnwrap(typeof (RobotWrapper).Assembly.FullName,
                typeof (RobotWrapper).FullName) as RobotWrapper;

            wrapper.Start(options); //Blocks, waiting on a reset event.
            AppDomain.Unload(childAppDomain);
            SetupRobot(options);
        }
    }

    
    public class RobotWrapper : MarshalByRefObject
    {
        private Options _options;
        
        public Options Options
        {
            get { return _options; }
        }

        public void Start(Options options)
        {
            _options = options;
            var robot = Initializer.StartBot(options).Result;
            var resetEvent = new AutoResetEvent(false);
            robot.ResetRequested += (sender, args) => resetEvent.Set();
            resetEvent.WaitOne();
        }

        
    }
}
