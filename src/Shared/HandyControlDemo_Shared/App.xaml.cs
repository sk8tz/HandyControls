﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
#if !NET40
using System.Runtime;
#endif
using System.Threading;
using System.Windows;
using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using HandyControl.Tools.Extension;
using HandyControlDemo.Data;
using HandyControlDemo.Properties.Langs;
using HandyControlDemo.Tools;

namespace HandyControlDemo
{
    public partial class App
    {
#pragma warning disable IDE0052
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private static Mutex AppMutex;
#pragma warning restore IDE0052

        public App()
        {
#if !NET40
            var cachePath = $"{AppDomain.CurrentDomain.BaseDirectory}Cache";
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            ProfileOptimization.SetProfileRoot(cachePath);
            ProfileOptimization.StartProfile("Profile");
#endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppMutex = new Mutex(true, "HandyControlDemo", out var createdNew);

            if (!createdNew)
            {
                var current = Process.GetCurrentProcess();

                foreach (var process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        Win32Helper.SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
                Shutdown();
            }
            else
            {
                var splashScreen = new SplashScreen("Resources/Img/Cover.png");
                splashScreen.Show(true);

                base.OnStartup(e);

                UpdateRegistry();
                ApplicationHelper.IsSingleInstance();

                ShutdownMode = ShutdownMode.OnMainWindowClose;
                GlobalData.Init();
                ConfigHelper.Instance.SetLang(GlobalData.Config.Lang);
                LangProvider.Culture = new CultureInfo(GlobalData.Config.Lang);

                if (GlobalData.Config.Theme != ApplicationTheme.Light)
                {
                    UpdateSkin(GlobalData.Config.Theme);
                }

                ConfigHelper.Instance.SetWindowDefaultStyle();
                ConfigHelper.Instance.SetNavigationWindowDefaultStyle();

#if NET40
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
#else
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            GlobalData.Save();
        }

        internal void UpdateSkin(ApplicationTheme theme)
        {
            ThemeManager.Current.ApplicationTheme = theme;

            var demoResources = new ResourceDictionary
            {
                Source = ApplicationHelper.GetAbsoluteUri("HandyControlDemo",
                    $"/Resources/Themes/Basic/Colors/{theme.ToString()}.xaml")
            };

            Resources.MergedDictionaries[0].MergedDictionaries.InsertOrReplace(1, demoResources);
        }

        private void UpdateRegistry()
        {
            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule != null)
            {
                var registryFilePath = $"{Path.GetDirectoryName(processModule.FileName)}\\Registry.reg";
                if (!File.Exists(registryFilePath))
                {
                    var streamResourceInfo = GetResourceStream(new Uri("pack://application:,,,/Resources/Registry.txt"));
                    if (streamResourceInfo != null)
                    {
                        using var reader = new StreamReader(streamResourceInfo.Stream);
                        var registryStr = reader.ReadToEnd();
                        var newRegistryStr = registryStr.Replace("#", processModule.FileName.Replace("\\", "\\\\"));
                        File.WriteAllText(registryFilePath, newRegistryStr);
                        Process.Start(new ProcessStartInfo("cmd", $"/c {registryFilePath}")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                }
            }
        }
    }
}
