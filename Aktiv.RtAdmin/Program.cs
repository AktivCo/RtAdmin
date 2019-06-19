﻿using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using CommandLine.Text;

namespace Aktiv.RtAdmin
{
    public class RtAdmin
    {
        private static IServiceProvider serviceProvider;
        private static int retCode;

        static int Main(string[] args)
        {
            var arguments = Parser.Default.ParseArguments<CommandLineOptions>(args);

            //arguments.WithNotParsed(errs =>
            //{
            //    var helpText = HelpText.AutoBuild(arguments, onError =>
            //    {
            //        var nHelpText = new HelpText(SentenceBuilder.Create(), "Ошибочка вышла!!!")
            //        {
            //            AdditionalNewLineAfterOption = false,
            //            AddDashesToOption = true,
            //            MaximumDisplayWidth = 4000
            //        };
            //        nHelpText.AddOptions(arguments);
            //        return HelpText.DefaultParsingErrorsHandler(arguments, nHelpText);
            //    },
            //    onExample => onExample);
            //    Console.Error.WriteLine(helpText);
            //});

            arguments.WithParsed(options =>
            {
                serviceProvider = Startup.Configure(options.LogFilePath);

                var core = serviceProvider.GetService<RutokenCore>();
                var logger = serviceProvider.GetService<ILogger<RtAdmin>>();

                if (!string.IsNullOrWhiteSpace(options.PinFilePath))
                {
                    serviceProvider.GetService<PinsStore>()
                                   .Load(options.PinFilePath);
                }

                try
                {
                    while (true)
                    {
                        var slot = core.WaitToken();

                        var commandHandlerBuilder = serviceProvider.GetService<CommandHandlerBuilder>()
                                                                   .ConfigureWith(slot, options);

                        if (options.AdminPinLength != 0)
                        {
                            commandHandlerBuilder.WithNewAdminPin();
                        }

                        if (options.UserPinLength != 0)
                        {
                            commandHandlerBuilder.WithNewUserPin();
                        }

                        if (!options.Format && 
                            !string.IsNullOrWhiteSpace(options.TokenLabelUtf8))
                        {
                            commandHandlerBuilder.WithNewTokenName();
                        }

                        if (options.Format)
                        {
                            commandHandlerBuilder.WithFormat();
                        }

                        commandHandlerBuilder.Execute();

                        if (options.OneIterationOnly)
                        {
                            break;
                        }
                    }

                    retCode = (int)CKR.CKR_OK;
                }
                catch (Pkcs11Exception ex)
                {
                    logger.LogError($"Operation failed [Method: {ex.Method}, RV: {ex.RV}]");
                    retCode = (int)ex.RV;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Operation failed [Message: {ex.Message}]");
                    retCode = -1;
                }
                finally
                {
                    DisposeServices();
                }
            });

            return retCode;
        }

        private static void DisposeServices()
        {
            if (serviceProvider == null)
            {
                return;
            }

            var pkcs11 = serviceProvider.GetService<Pkcs11>();
            pkcs11.Dispose();

            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
