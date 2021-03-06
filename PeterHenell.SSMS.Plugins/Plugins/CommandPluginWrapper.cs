﻿using PeterHenell.SSMS.Plugins.Forms;
using PeterHenell.SSMS.Plugins.Shell;
using RedGate.SIPFrameworkShared;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeterHenell.SSMS.Plugins.Plugins
{
    /// <summary>
    /// This class wrapps the PluginCommandBase so that we do not need to reference the RedGate.SIPFrameworkShared 
    /// in the plugin projects.
    /// </summary>
    public class CommandPluginWrapper : ISharedCommandWithExecuteParameter
    {
        public string Name { get; private set; }
        public string Tooltip { get; private set; }
        public bool Visible { get { return true; } }
        public ISsmsFunctionalityProvider4 Provider { get; private set; }
        public RedGate.SIPFrameworkShared.ICommandImage Icon { get; private set; }
        public string Caption { get; private set; }
        public string[] DefaultBindings { get; private set; }
        public bool Enabled { get { return true; } }
        public string MenuGroup { get; private set; }

        public CommandPluginBase Plugin { get; private set; }
        public Config.PluginConfiguration Options
        {
            get
            {
                return Plugin.PluginOptions;
            }
            set { Plugin.PluginOptions = value; }
        }

        public CommandPluginWrapper(CommandPluginBase loadedPlugin)
        {
            this.Plugin = loadedPlugin;

            this.Icon = new CommandImageNone();
            this.Name = loadedPlugin.Name;
            this.Caption = loadedPlugin.Caption;
            this.Tooltip = loadedPlugin.Caption;
            this.MenuGroup = loadedPlugin.MenuGroup;
            this.DefaultBindings = new string[] { loadedPlugin.ShortcutBinding };
        }

        public void Init(ISsmsFunctionalityProvider4 provider)
        {
            this.Provider = provider;
            var shellManager = new ShellManager(provider);
            Plugin.Init(shellManager);
        }

        public void Execute(object parameter)
        {
            var start = new Action(() =>
               {
                   try
                   {
                       Plugin.ExecuteCommand();
                   }
                   catch (SqlException sex)
                   {
                       if (sex.Message.Contains("cancelled"))
                       {
                           // ignore cancelled errors
                           return;
                       }
                       ShellManager.ShowMessageBox(sex.ToString());
                   }
                   catch (System.Exception ex)
                   {
                       ShellManager.ShowMessageBox(ex.ToString());
                   }
               });
            var stop = new Action(() =>
            {
                Plugin.TryAbortCommand();
            });

            BackgroundRunnerForm f = new BackgroundRunnerForm(Plugin.Caption, "Running", start, stop);
            f.Show();
        }

        public void Execute()
        {
            try
            {
                Plugin.ExecuteCommand();
            }
            catch (System.Exception ex)
            {
                ShellManager.ShowMessageBox(ex.ToString());
            }
        }

        public override string ToString()
        {
            return string.Format("[{0} - {1} - {2}]", Name, Caption, MenuGroup);
        }
    }
}
