﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Nancy.Hosting.Self;
using NzbDrone.Api;
using NzbDrone.Common;
using NzbDrone.Common.SysTray;
using NzbDrone.Model;

namespace NzbDrone
{
    public class Router
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ApplicationServer _applicationServer;
        private readonly ServiceProvider _serviceProvider;
        private readonly ConsoleProvider _consoleProvider;
        private readonly EnvironmentProvider _environmentProvider;
        private readonly SysTrayProvider _sysTrayProvider;
        private readonly ProcessProvider _processProvider;

        public Router(ApplicationServer applicationServer, ServiceProvider serviceProvider,
                        ConsoleProvider consoleProvider, EnvironmentProvider environmentProvider, SysTrayProvider sysTrayProvider, ProcessProvider processProvider)
        {
            _applicationServer = applicationServer;
            _serviceProvider = serviceProvider;
            _consoleProvider = consoleProvider;
            _environmentProvider = environmentProvider;
            _sysTrayProvider = sysTrayProvider;
            _processProvider = processProvider;
        }

        public void Route(IEnumerable<string> args)
        {
            Route(GetApplicationMode(args));
        }

        public void Route(ApplicationMode applicationMode)
        {
            if (!_environmentProvider.IsUserInteractive)
            {
                applicationMode = ApplicationMode.Service;
            }

            logger.Info("Application mode: {0}", applicationMode);

            switch (applicationMode)
            {
                case ApplicationMode.Nancy:
                    {

                        var nancyHost = new NancyHost(new Uri("http://localhost:8282"), new NancyBootstrapper());
                        nancyHost.Start();


                        _processProvider.Start("http://localhost:8282");

                        _consoleProvider.WaitForClose();

                        break;
                    }

                case ApplicationMode.Service:
                    {
                        logger.Trace("Service selected");
                        _serviceProvider.Run(_applicationServer);
                        break;
                    }

                case ApplicationMode.Console:
                    {
                        logger.Trace("Console selected");
                        _applicationServer.Start();
                        if (ConsoleProvider.IsConsoleApplication)
                            _consoleProvider.WaitForClose();

                        else
                        {
                            _sysTrayProvider.Start();
                        }

                        break;
                    }
                case ApplicationMode.InstallService:
                    {
                        logger.Trace("Install Service selected");
                        if (_serviceProvider.ServiceExist(ServiceProvider.NZBDRONE_SERVICE_NAME))
                        {
                            _consoleProvider.PrintServiceAlreadyExist();
                        }
                        else
                        {
                            _serviceProvider.Install(ServiceProvider.NZBDRONE_SERVICE_NAME);
                            _serviceProvider.Start(ServiceProvider.NZBDRONE_SERVICE_NAME);
                        }
                        break;
                    }
                case ApplicationMode.UninstallService:
                    {
                        logger.Trace("Uninstall Service selected");
                        if (!_serviceProvider.ServiceExist(ServiceProvider.NZBDRONE_SERVICE_NAME))
                        {
                            _consoleProvider.PrintServiceDoestExist();
                        }
                        else
                        {
                            _serviceProvider.UnInstall(ServiceProvider.NZBDRONE_SERVICE_NAME);
                        }

                        break;
                    }
                default:
                    {
                        _consoleProvider.PrintHelp();
                        break;
                    }
            }
        }

        public static ApplicationMode GetApplicationMode(IEnumerable<string> args)
        {
            if (args == null) return ApplicationMode.Console;

            var cleanArgs = args.Where(c => c != null && !String.IsNullOrWhiteSpace(c)).ToList();
            if (cleanArgs.Count == 0) return ApplicationMode.Console;
            if (cleanArgs.Count != 1) return ApplicationMode.Help;

            var arg = cleanArgs.First().Trim('/', '\\', '-').ToLower();

            if (arg == "i") return ApplicationMode.InstallService;
            if (arg == "u") return ApplicationMode.UninstallService;
            if (arg == "n") return ApplicationMode.Nancy;

            return ApplicationMode.Help;
        }
    }
}
