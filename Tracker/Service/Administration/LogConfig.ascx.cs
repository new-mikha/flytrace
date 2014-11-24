/******************************************************************************
 * Flytrace, online viewer for GPS trackers.
 * Copyright (C) 2011-2014 Mikhail Karmazin
 * 
 * This file is part of Flytrace.
 * 
 * Flytrace is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * Flytrace is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace FlyTrace.Service.Administration
{
  public partial class LogConfig : System.Web.UI.UserControl
  {
    protected void Page_Load( object sender, EventArgs e )
    {

    }

    protected void updateConfigButton_Click( object sender, EventArgs e )
    {
      string configFilePath =
        Path.Combine(
          HttpRuntime.AppDomainAppPath,
          ConfigurationManager.AppSettings["LogConfig"] );

      if ( sender == this.updateConfigButton )
      {
        File.WriteAllText( configFilePath, this.logConfigTextBox.Text );
      }
      else if ( sender == this.restoreDefaultConfigButton )
      {
        string defFileSettingName = "DefaultLogConfig";
        string defFileRelPath = ConfigurationManager.AppSettings[defFileSettingName];
        if ( string.IsNullOrEmpty( defFileRelPath ) )
        {
          throw new ApplicationException( string.Format( "{0} setting is absent.", defFileSettingName ) );
        }

        string defConfigFilePath = Path.Combine( HttpRuntime.AppDomainAppPath, defFileRelPath );

        string defConfig = File.ReadAllText( defConfigFilePath );

        File.WriteAllText( configFilePath, defConfig );
      }
      else
      { // sender is this.rereadExistingConfigButton, so don't update existing config.

      }

      log4net.LogManager.ResetConfiguration( );
      log4net.Config.XmlConfigurator.Configure( );

      // Give log4net time to pickup the settings, otherwise it shows "Configured: No" on 
      // the web page and requires reload to show actual settings. Sleep in a web request 
      // processing is usually quite bad idea, but this page is allowed for admins only
      // so should be alright (TODO: looks like Page.RegisterAsyncTask can do the job safer)
      System.Threading.Thread.Sleep( 3000 ); 
    }

    protected void Page_PreRender( object sender, EventArgs e )
    {
      string configFilePath =
        Path.Combine(
          HttpRuntime.AppDomainAppPath,
          ConfigurationManager.AppSettings["LogConfig"] );

      bool shouldWatch = true;
      this.configFileNameLabel.Text = configFilePath;
      this.shouldWatchLabel.Text = string.Format( "'{0}' ({1})", shouldWatch, shouldWatch );

      try
      {
        if ( File.Exists( configFilePath ) )
        {
          this.logConfigTextBox.Text = File.ReadAllText( configFilePath );
        }
        else
        {
          this.logConfigTextBox.Text = "";
        }
        this.logConfigMultiView.SetActiveView( this.logConfigNormalView );
      }
      catch ( Exception exc )
      {
        this.logConfigMultiView.SetActiveView( this.logConfigErrorView );
        this.logConfigErrorLabel.Text = exc.ToString().Replace( "\n", "<br />" );
      }

      ILoggerRepository defaultRepository = log4net.LogManager.GetRepository();

      this.isConfiguredLabel.Visible = defaultRepository != null && defaultRepository.Configured;
      this.isNotConfiguredLabel.Visible = !this.isConfiguredLabel.Visible;

      if ( defaultRepository == null )
      {
        this.infoLabel.Text = "";
      }
      else
      {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine( "Appenders:" );
        foreach ( IAppender appender in defaultRepository.GetAppenders() )
        {
          sb.AppendFormat( "- {0}, {1}", appender.Name, appender.GetType().Name );
          if ( appender is FileAppender )
          {
            string filePath = ( appender as FileAppender ).File;
            sb.AppendFormat(
              ", {0}, {1}",
              filePath,
              File.Exists( filePath ) ? "EXISTS" : "NOT exists"
            );
          }
          sb.Append( "\n" );
        }
        sb.AppendLine();
        sb.AppendLine( "Loggers:" );
        defaultRepository.GetLogger( "" ); // that's enough to add root logger to the collection if it's not there:
        foreach ( Logger logger in defaultRepository.GetCurrentLoggers() )
        {
          string name = logger.Name;
          if ( string.IsNullOrEmpty( name ) )
          {
            name = "root";
          }
          sb.AppendFormat( "- {0}, {1}\n", name, logger.EffectiveLevel );
        }

        this.infoLabel.Text = sb.ToString().Replace( "\n", "<br />\n" );
      }
    }
  }
}