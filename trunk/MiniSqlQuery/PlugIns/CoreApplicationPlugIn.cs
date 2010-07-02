#region License

// Copyright 2005-2009 Paul Kohler (http://pksoftware.net/MiniSqlQuery/). All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (Ms-PL)
// http://minisqlquery.codeplex.com/license
#endregion

using System;
using System.Diagnostics;
using System.Windows.Forms;
using MiniSqlQuery.Commands;
using MiniSqlQuery.Core;
using MiniSqlQuery.Core.Commands;

namespace MiniSqlQuery.PlugIns
{
	/// <summary>The core application plug in.</summary>
	public class CoreApplicationPlugIn : PluginLoaderBase
	{
		/// <summary>The _timer.</summary>
		private Timer _timer;

		/// <summary>Initializes a new instance of the <see cref="CoreApplicationPlugIn"/> class.</summary>
		public CoreApplicationPlugIn()
			: base("Mini SQL Query Core", "Plugin to setup the core features of Mini SQL Query.", 1)
		{
		}

		/// <summary>Iinitialize the plug in.</summary>
		public override void InitializePlugIn()
		{
			IApplicationServices services = Services;
			IHostWindow hostWindow = services.HostWindow;

			services.RegisterEditor<BasicEditor>(new FileEditorDescriptor("Default text editor", "default-editor"));
			services.RegisterEditor<QueryForm>(new FileEditorDescriptor("SQL Editor", "sql-editor", "sql"));
			services.RegisterEditor<BasicCSharpEditor>(new FileEditorDescriptor("C# Editor", "cs-editor", "cs"));
			services.RegisterEditor<BasicVbNetEditor>(new FileEditorDescriptor("VB/VB.NET Editor", "vb-editor", "vb"));
			services.RegisterEditor<BasicXmlEditor>(new FileEditorDescriptor("XML Editor", "xml-editor", "xml"));
			services.RegisterEditor<BasicHtmlEditor>(new FileEditorDescriptor("HTML Editor", "htm-editor", "htm", "html"));
			services.RegisterEditor<BasicEditor>(new FileEditorDescriptor("Text Editor", "txt-editor", "txt"));

			services.RegisterComponent<NewFileForm>("NewFileForm");
			services.RegisterComponent<OptionsForm>("OptionsForm");
			services.RegisterConfigurationObject<CoreMiniSqlQueryConfiguration>();

			ToolStripMenuItem fileMenu = hostWindow.GetMenuItem("File");
			ToolStripMenuItem editMenu = hostWindow.GetMenuItem("edit");
			ToolStripMenuItem queryMenu = hostWindow.GetMenuItem("query");
			ToolStripMenuItem helpMenu = hostWindow.GetMenuItem("help");

			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<NewQueryFormCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<NewFileCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItemSeparator());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<OpenFileCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<SaveFileCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<SaveFileAsCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<CloseActiveWindowCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItemSeparator());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<PrintCommand>());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItemSeparator());
			fileMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ExitApplicationCommand>());

			queryMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ExecuteTaskCommand>());
			queryMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<CancelTaskCommand>());
			queryMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<SaveResultsAsDataSetCommand>());
			queryMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<RefreshDatabaseConnectionCommand>());
			queryMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<CloseDatabaseConnectionCommand>());
			queryMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<DisplayDbModelDependenciesCommand>());

			// editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<DuplicateLineCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<PasteAroundSelectionCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ConvertTextToLowerCaseCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ConvertTextToUpperCaseCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ConvertTextToTitleCaseCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<SetLeftPasteAroundSelectionCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<SetRightPasteAroundSelectionCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ShowOptionsFormCommand>());
			editMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<OpenConnectionFileCommand>());

			// get the new item and make in invisible - the shortcut key is still available etc ;-)
			ToolStripItem item = editMenu.DropDownItems["SetLeftPasteAroundSelectionCommandToolStripMenuItem"];
			item.Visible = false;
			item = editMenu.DropDownItems["SetRightPasteAroundSelectionCommandToolStripMenuItem"];
			item.Visible = false;


			helpMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ShowHelpCommand>());
			helpMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ShowWebPageCommand>());
			helpMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ShowDiscussionsWebPageCommand>());
			helpMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<EmailAuthorCommand>());
			helpMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItemSeparator());
			helpMenu.DropDownItems.Add(CommandControlBuilder.CreateToolStripMenuItem<ShowAboutCommand>());

			CommandControlBuilder.MonitorMenuItemsOpeningForEnabling(hostWindow.Instance.MainMenuStrip);

			// toolstrip
			hostWindow.AddToolStripCommand<NewQueryFormCommand>(0);
			hostWindow.AddToolStripCommand<OpenFileCommand>(1);
			hostWindow.AddToolStripCommand<SaveFileCommand>(2);
			hostWindow.AddToolStripSeperator(3);
			hostWindow.AddToolStripCommand<ExecuteTaskCommand>(4);
			hostWindow.AddToolStripCommand<CancelTaskCommand>(5);
			hostWindow.AddToolStripSeperator(6);
			hostWindow.AddToolStripSeperator(null);
			hostWindow.AddToolStripCommand<RefreshDatabaseConnectionCommand>(null);

			hostWindow.AddPluginCommand<InsertGuidCommand>();

			// watch tool strip enabled properties
			// by simply iterating each one every second or so we avoid the need to track via events
			_timer = new Timer();
			_timer.Interval = 1000;
			_timer.Tick += TimerTick;
			_timer.Enabled = true;
		}

		/// <summary>The unload plug in.</summary>
		public override void UnloadPlugIn()
		{
			if (_timer != null)
			{
				_timer.Dispose();
			}
		}

		/// <summary>Called frequently to run through all the commands on the toolstrip and check their enabled state. 
		/// Marked as "DebuggerNonUserCode" because it can get in the way of a debug sesssion.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		[DebuggerNonUserCode]
		private void TimerTick(object sender, EventArgs e)
		{
			try
			{
				_timer.Enabled = false;
				foreach (ToolStripItem item in Services.HostWindow.ToolStrip.Items)
				{
					ICommand cmd = item.Tag as ICommand;
					if (cmd != null)
					{
						item.Enabled = cmd.Enabled;
					}
				}
			}
			finally
			{
				_timer.Enabled = true;
			}
		}
	}
}