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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using log4net.Layout;
using log4net.Config;
using log4net.Appender;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;

public struct Foo
{
  public string FeedKind;
  public string Type;
  public string UserMessage;
}

namespace LocationRequestTest
{
  using FlyTrace.LocationLib;
  using FlyTrace.LocationLib.Data;
  using FlyTrace.Service;
  using System.Threading;

  public partial class MainForm : Form
  {
    public MainForm( )
    {
      InitializeComponent( );
    }

    private void MainForm_Load( object sender, EventArgs e )
    {
      SetDefaultFeeds( );

      TextWriterAppender textWriterAppender = new TextWriterAppender( );
      textWriterAppender.Layout = new SimpleLayout( );
      textWriterAppender.Writer = new StreamWriter( new LogStream( this.resultTextBox ) );

      BasicConfigurator.Configure( textWriterAppender );

      tabControl2.SelectedIndex = 1;
    }



    private class LogStream : Stream
    {
      private TextBox logTextBox;

      public LogStream( TextBox logTextBox )
      {
        this.logTextBox = logTextBox;
      }

      public override bool CanRead
      {
        get { return false; }
      }

      public override bool CanSeek
      {
        get { return false; }
      }

      public override bool CanWrite
      {
        get { return true; }
      }

      public override void Flush( )
      {

      }

      public override long Length
      {
        get { return -1; }
      }

      public override long Position
      {
        get
        {
          return -1;
        }
        set
        {
          throw new NotImplementedException( );
        }
      }

      public override int Read( byte[] buffer, int offset, int count )
      {
        throw new NotImplementedException( );
      }

      public override long Seek( long offset, SeekOrigin origin )
      {
        throw new NotImplementedException( );
      }

      public override void SetLength( long value )
      {
        throw new NotImplementedException( );
      }

      public override void Write( byte[] buffer, int offset, int count )
      {
        string text = Encoding.UTF8.GetString( buffer, offset, count );

        if ( this.logTextBox.InvokeRequired )
        {
          this.logTextBox.BeginInvoke( new Action<string>( this.logTextBox.AppendText ), text );
        }
        else
        {
          this.logTextBox.AppendText( text );
        }

        Debug.Write( text );
      }
    }

    private void requestLocationButton_Click( object sender, EventArgs e )
    {
      Cursor currCursor = Cursor;
      Cursor = Cursors.WaitCursor;

      try
      {
        string feedId = this.feedIdTextBox.Text.Trim( );

        List<FeedKind> attemptsOrder = new List<FeedKind>( );
        ReadDestComboBox( dest1ComboBox, attemptsOrder );
        ReadDestComboBox( dest2ComboBox, attemptsOrder );
        ReadDestComboBox( dest3ComboBox, attemptsOrder );
        ReadDestComboBox( dest4ComboBox, attemptsOrder );
        ReadDestComboBox( dest5ComboBox, attemptsOrder );
        ReadDestComboBox( dest6ComboBox, attemptsOrder );

        string appFolder = Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).CodeBase ).Replace( "file:\\", "" );

        LocationRequest locationRequest;
        if ( this.inetSourceRadioButton.Checked )
          locationRequest = new LocationRequest( feedId, appFolder, attemptsOrder.ToArray( ) );
        else
        {
          if ( attemptsOrder.Count == 0 )
            throw new ApplicationException( "Select a value in 1st destination combo" );

          string sampleXml = this.sampleXml;
          if ( sampleXml == null )
            sampleXml = ( new SampleXmlForm( ) ).SampleXml; // bad, bad style :)

          locationRequest = new LocationRequest( "testxml", sampleXml, attemptsOrder[0], appFolder );
        }

        if ( this.resultTextBox.Text.Length > 0 )
        {
          AddResultText( "\r\n" ); // 0uhmDCNY4d1mUHv9A5FZdLbRFMLFVN3Gr
          AddResultText( "-------------------------------------------------\r\n" );
        }
        AddResultText( string.Format( "Requesting {0}...\r\n", feedId ) );

        locationRequest.BeginReadLocation( LocationRequestAsyncCallback, locationRequest );
      }
      catch ( Exception exc )
      {
        AddResultText( exc.ToString( ) + "\r\n" );
      }

      AddResultText( "OnClick: DONE\r\n" );

      Cursor = currCursor;
    }

    private void ReadDestComboBox( ComboBox destComboBox, List<FeedKind> attemptsOrder )
    {
      if ( destComboBox.SelectedItem != null )
        attemptsOrder.Add( ( FeedKind ) Enum.Parse( typeof( FeedKind ), destComboBox.SelectedItem.ToString( ) ) );
    }

    private void AddResultTextLine( string text )
    {
      AddResultText( text + "\r\n" );
    }

    private void AddResultText( string text )
    {
      this.resultTextBox.Text += text;
      this.resultTextBox.SelectionStart = this.resultTextBox.Text.Length;
      this.resultTextBox.SelectionLength = 0;
      this.resultTextBox.ScrollToCaret( );
    }

    private void LocationRequestAsyncCallback( IAsyncResult ar )
    {
      if ( this.InvokeRequired )
      {
        this.Invoke( new AsyncCallback( LocationRequestAsyncCallback ), ar );
      }
      else
      {
        try
        {
          LocationRequest locationRequest = ( LocationRequest ) ar.AsyncState;

          StringBuilder sb = new StringBuilder( );
          sb.AppendFormat( "Result for {0}:\r\n", locationRequest.TrackerForeignId );

          TrackerState trackerRequestResult = locationRequest.EndReadLocation( ar );

          sb.AppendFormat( "\tRefreshTime: {0} ({1})\r\n", trackerRequestResult.RefreshTime, Tools.GetAgeStr( trackerRequestResult.RefreshTime ) );

          if ( trackerRequestResult.Position == null )
          {
            sb.AppendLine( "Location: NULL" );
          }
          else
          {
            sb.AppendLine( "Location:" );
            sb.AppendFormat( "\tLocation Type: {0}\r\n", trackerRequestResult.Position.Type );
            sb.AppendFormat( "\tCurrent: {0}\r\n", trackerRequestResult.Position.CurrPoint );
            sb.AppendFormat( "\tUser message: {0}\r\n", trackerRequestResult.Position.UserMessage );
            sb.AppendFormat( "\tPrev: {0}\r\n", trackerRequestResult.Position.PreviousPoint );
            sb.AppendLine( "\tFullTrack:" );
            foreach ( FlyTrace.LocationLib.Data.TrackPointData tpd in trackerRequestResult.Position.FullTrack )
            {
              sb.AppendFormat( "\t\tPoint: {0}\r\n", tpd );
            }
          }

          if ( trackerRequestResult.Error == null )
          {
            sb.AppendLine( "Error: NULL" );
          }
          else
          {
            sb.AppendLine( "Error:" );
            sb.AppendFormat( "\tError type: {0}\r\n", trackerRequestResult.Error.Type );
            sb.AppendFormat( "\tError message: {0}\r\n", trackerRequestResult.Error.Message );
          }

          AddResultText( sb.ToString( ) );
        }
        catch ( Exception exc )
        {
          AddResultText( exc.ToString( ) + "\r\n" );
        }

        AddResultText( "Async: DONE\r\n" );
      }
    }

    private void defaultFeeds_Click( object sender, EventArgs e )
    {
      SetDefaultFeeds( );
    }

    private void SetDefaultFeeds( )
    {
      dest1ComboBox.SelectedIndex = 2;
      dest2ComboBox.SelectedIndex = 1;
      dest3ComboBox.SelectedIndex = 0;
      dest4ComboBox.SelectedIndex = -1;
      dest5ComboBox.SelectedIndex = -1;
      dest6ComboBox.SelectedIndex = -1;
    }

    private void clearFeeds_Click( object sender, EventArgs e )
    {
      dest1ComboBox.SelectedIndex = -1;
      dest2ComboBox.SelectedIndex = -1;
      dest3ComboBox.SelectedIndex = -1;
      dest4ComboBox.SelectedIndex = -1;
      dest5ComboBox.SelectedIndex = -1;
      dest6ComboBox.SelectedIndex = -1;

    }

    private void feedIdTextBox_Enter( object sender, EventArgs e )
    {
      this.inetSourceRadioButton.Checked = true;
    }

    private string sampleXml = null;

    private void editSampleXmlButton_Click( object sender, EventArgs e )
    {
      bool inetSourceRadioButtonWasChecked = this.inetSourceRadioButton.Checked;
      this.xmlSourceRadioButton.Checked = true;

      SampleXmlForm xmlSampleForm = new SampleXmlForm( );

      if ( this.sampleXml != null )
        xmlSampleForm.SampleXml = this.sampleXml;

      if ( xmlSampleForm.ShowDialog( this ) == System.Windows.Forms.DialogResult.OK )
      {
        this.sampleXml = xmlSampleForm.SampleXml;
      }
      else if ( inetSourceRadioButtonWasChecked )
      {
        this.inetSourceRadioButton.Checked = true;
      }
    }

    private void clearResultButton_Click( object sender, EventArgs e )
    {
      resultTextBox.Text = "";
    }

    private void revgenInitButton_Click( object sender, EventArgs e )
    {
      try
      {
        InitRevGen( );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }

    private void InitRevGen( )
    {
      string initWarnings;
      bool result = RevisionGenerator.Init( revgenFilePathTextBox.Text, out initWarnings );
      AddResultTextLine( "Init: " + result.ToString( ) + ", revision: " + RevisionGenerator.Revision.ToString( ) );
      AddResultTextLine( string.Format( "Init warnings: {0}", initWarnings ) );
    }

    private void revgenShutdownButton_Click( object sender, EventArgs e )
    {
      try
      {
        RevisionGenerator.Shutdown( );
        AddResultTextLine( "Shutdown done" );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }

    private void revgenIncrement1Button_Click( object sender, EventArgs e )
    {
      try
      {
        AddResultText( "Start revision: " );
        AddResultText( RevisionGenerator.Revision.ToString( ) );
        AddResultText( ", incrementing by 1..." );
        RevisionGenerator.IncrementRevision( );
        AddResultText( "Done, new revision: " );
        AddResultTextLine( RevisionGenerator.Revision.ToString( ) );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }

    private void revgenGetCurrentButton_Click( object sender, EventArgs e )
    {
      try
      {
        AddResultTextLine( "Current revision: " + RevisionGenerator.Revision.ToString( ) );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }

    private void revgenInrcement10Button_Click( object sender, EventArgs e )
    {
      try
      {
        AddResultText( "Start revision: " );
        AddResultText( RevisionGenerator.Revision.ToString( ) );
        AddResultText( ", incrementing by 10..." );
        for ( int i = 0; i < 10; i++ )
          RevisionGenerator.IncrementRevision( );
        AddResultText( "Done, new revision: " );
        AddResultTextLine( RevisionGenerator.Revision.ToString( ) );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }

    private void revgenIncrement100Button_Click( object sender, EventArgs e )
    {
      try
      {
        AddResultText( "Start revision: " );
        AddResultText( RevisionGenerator.Revision.ToString( ) );
        AddResultText( ", incrementing by 100..." );
        for ( int i = 0; i < 100; i++ )
          RevisionGenerator.IncrementRevision( );
        AddResultText( "Done, new revision: " );
        AddResultTextLine( RevisionGenerator.Revision.ToString( ) );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }

    private void revgenShutownAndIncrementButton_Click( object sender, EventArgs e )
    {
      try
      {
        AddResultTextLine( "ShutownAndIncrement..." );
        if ( !RevisionGenerator.IsActive )
          InitRevGen( );

        ManualResetEvent stEvent = new ManualResetEvent( false );

        Exception exc = null;

        int resultRev = -1;

        Thread th1 =
          new Thread(
            new ThreadStart(
              ( )
                =>
              {
                stEvent.WaitOne( );
                try
                {
                  RevisionGenerator.Shutdown( );
                }
                catch ( Exception exc1 )
                {
                  exc = exc1;
                }

              }
            )
          );

        Thread th2 =
          new Thread(
            new ThreadStart(
              ( )
                =>
              {
                stEvent.WaitOne( );

                try
                {
                  resultRev = RevisionGenerator.IncrementRevision( );
                }
                catch ( Exception exc1 )
                {
                  exc = exc1;
                }
              }
            )
          );

        th1.Start( );
        th2.Start( );

        stEvent.Set( );

        th1.Join( );
        th2.Join( );

        AddResultTextLine( "Result revison: " + resultRev );
        AddResultTextLine( "Exception: " + ( exc == null ? "none" : exc.Message ) );
      }
      catch ( Exception exc )
      {
        AddResultTextLine( exc.Message );
      }
    }
  }
}