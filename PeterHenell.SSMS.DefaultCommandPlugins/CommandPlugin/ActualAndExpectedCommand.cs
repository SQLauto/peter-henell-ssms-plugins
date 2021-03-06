﻿using PeterHenell.SSMS.Plugins.DataAccess;
using PeterHenell.SSMS.Plugins.Forms;
using PeterHenell.SSMS.Plugins.Shell;
using PeterHenell.SSMS.Plugins.Utils;
using System;
using System.Data;
using System.Text;
using System.Linq;
using System.Data.SqlClient;
using System.Collections.Generic;
using PeterHenell.SSMS.Plugins.ExtensionMethods;
using PeterHenell.SSMS.Plugins.Plugins;
using System.Threading;
using PeterHenell.SSMS.Plugins.DataAccess.DTO;

namespace PeterHenell.SSMS.Plugins.Commands
{
    public class ActualAndExpectedCommand : CommandPluginBase
    {
        public readonly static string COMMAND_NAME = "ActualAndExpectedCommand";

        public ActualAndExpectedCommand() :
            base(COMMAND_NAME,
                 CommandPluginBase.MenuGroups.TSQLTTools,
                 "tSQLt - Create #Actual and #Expected tables from selected query",
                 "global::Ctrl+Alt+L")
        {

        }

        public override void ExecuteCommand(CancellationToken token)
        {
            var options = new MockOptionsDictionary();

            var ok = new Action<string, MockOptionsDictionary>((result, checkedOptions) =>
            {
                int numRows = 0;
                if (!int.TryParse(result, out numRows))
                {
                    ShellManager.ShowMessageBox("Please input a valid number");
                    return;
                }
                else
                {
                    if (numRows <= 0)
                    {
                        numRows = 0;
                    }
                    else if (numRows > 1000)
                    {
                        numRows = 1000;
                    }
                }

                string selectedText = ShellManager.GetSelectedText();
                var sb = new StringBuilder();
                using (var ds = new DataSet())
                {
                    QueryManager.Run(ConnectionManager.GetConnectionStringForCurrentWindow(), token, (queryManager) =>
                       {
                           queryManager.Fill(string.Format("SET ROWCOUNT {0}; {1}", numRows, selectedText), ds);
                       });
                    if (ds.Tables.Count == 1)
                    {

                        sb.AppendDropTempTableIfExists("#Actual");
                        sb.AppendLine();
                        sb.AppendDropTempTableIfExists("#Expected");
                        sb.AppendLine();

                        sb.AppendTempTablesFor(ds.Tables[0], "#Actual");
                        sb.Append("INSERT INTO #Actual");

                        ShellManager.AddTextToTopOfSelection(sb.ToString());

                        sb.Clear();
                        sb.AppendColumnNameList(ds.Tables[0]);
                        ShellManager.AppendToEndOfSelection(
                                string.Format("{0}SELECT {1}INTO #Expected{0}FROM #Actual{0}WHERE 1=0;{0}", Environment.NewLine, sb.ToString())
                                );
                        ShellManager.AppendToEndOfSelection(
                            TsqltManager.GenerateInsertFor(ds.Tables[0], ObjectMetadata.FromQualifiedString("#Expected"), false, false));
                    }
                    else
                    {
                        return;
                    }
                }

                //var meta = ObjectMetadata.FromQualifiedString(selectedText);
                //ObjectMetadataAccess da = new ObjectMetadataAccess(ConnectionManager.GetConnectionStringForCurrentWindow());
                //var table = da.SelectTopNFrom(meta, numRows);

                //StringBuilder sb = new StringBuilder();
                //sb.Append(TsqltManager.GetFakeTableStatement(selectedText));
                //sb.AppendLine();
                //sb.Append(TsqltManager.GenerateInsertFor(table, meta, options.EachColumnInSelectOnNewRow, options.EachColumnInValuesOnNewRow));
                //shellManager.ReplaceSelectionWith(sb.ToString());

            });

            var diagManager = new DialogManager.InputWithCheckboxesDialogManager<MockOptionsDictionary>();
            diagManager.Show("How many rows to select? (0=max)", "1", options, ok, cancelCallback);
        }

        public class MockOptionsDictionary : Dictionary<string, bool>
        {
            public MockOptionsDictionary()
            {
                EachColumnInSelectOnNewRow = false;
                EachColumnInValuesOnNewRow = false;
            }

            public bool EachColumnInSelectOnNewRow
            {
                get
                {
                    return this["Each Column in new row in INSERT?"];
                }
                set
                {
                    if (this.ContainsKey("Each Column in new row in INSERT?"))
                    {
                        this["Each Column in new row in INSERT?"] = value;
                        return;
                    }
                    this.Add("Each Column in new row in INSERT?", value);
                }
            }

            public bool EachColumnInValuesOnNewRow
            {
                get
                {
                    return this["Each Column in new row in VALUES?"];
                }
                set
                {
                    if (this.ContainsKey("Each Column in new row in VALUES?"))
                    {
                        this["Each Column in new row in VALUES?"] = value;
                        return;
                    }
                    this.Add("Each Column in new row in VALUES?", value);
                }
            }
        }


        private void cancelCallback()
        {
        }

        //public string Name { get { return COMMAND_NAME; } }
        //public string Caption { get { return "tSQLt - Create #Actual and #Expected tables from selected query"; } }
        //public string Tooltip { get { return "Generate the two temporary tables #Actual and #Expected based on current query"; } }
        //public ICommandImage Icon { get { return m_CommandImage; } }
        //public string[] DefaultBindings { get { return new[] { "global::Ctrl+Alt+L" }; } }
        //public bool Visible { get { return true; } }
        //public bool Enabled { get { return true; } }

        //public void Execute()
        //{

        //}

        //public string MenuGroup
        //{
        //    get { return "TSQLT - Tools"; }
        //}

        //public void Init(ISsmsFunctionalityProvider4 provider)
        //{
        //    this.provider = provider;
        //    this.shellManager = new ShellManager(provider);
        //}
    }
}