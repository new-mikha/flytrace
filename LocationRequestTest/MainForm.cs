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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;
using System.IO;

using FlyTrace.LocationLib.ForeignAccess;
using FlyTrace.LocationLib.ForeignAccess.Test;
using log4net.Layout;
using log4net.Config;
using log4net.Appender;

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
  using System.Threading;
  using FlyTrace.LocationLib.ForeignAccess.Spot;
  using FlyTrace.Service;
  using Microsoft.Win32;

  public partial class MainForm : Form
  {
    public MainForm( )
    {
      InitializeComponent( );
    }

    private void MainForm_Load( object sender, EventArgs e )
    {
      TextWriterAppender textWriterAppender = new TextWriterAppender( );
      textWriterAppender.Layout = new SimpleLayout( );
      textWriterAppender.Writer = new StreamWriter( new LogStream( this.resultTextBox ) );

      BasicConfigurator.Configure( textWriterAppender );

      tabControl2.SelectedIndex = 1;
    }

    protected override void WndProc( ref Message m )
    {
      base.WndProc( ref m );
    }

    private void SystemEvents_TimeChanged( object sender, EventArgs e )
    {
      this.label3.Text = DateTime.Now.ToLongTimeString( );
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

        string appFolder = Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).CodeBase ).Replace( "file:\\", "" );

        LocationRequest locationRequest;
        if ( this.inetSourceRadioButton.Checked )
        {
          RequestParams requestParams = default( RequestParams );
          requestParams.Id = feedId;

          locationRequest =
            new SpotLocationRequest( requestParams, appFolder, null );
        }
        else
        {
          string sampleXml = this.sampleXml;
          if ( sampleXml == null )
            sampleXml = ( new SampleXmlForm( ) ).SampleXml; // bad, bad style :)

          RequestParams requestParams = default( RequestParams );
          requestParams.Id = "testxml";

          locationRequest = new TestLocationRequest( requestParams, sampleXml );
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
          sb.AppendFormat( "Result for {0}:\r\n", locationRequest.Id );

          TrackerState trackerRequestResult = locationRequest.EndReadLocation( ar );

          sb.AppendFormat( "\tRefreshTime: {0} ({1})\r\n", trackerRequestResult.CreateTime,
            Tools.GetAgeStr( trackerRequestResult.CreateTime, true ) );

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
            sb.AppendFormat( "\tFullTrack is of {0} points:\r\n", trackerRequestResult.Position.FullTrack.Count( ) );
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

    FlyTrace.Service.Internals.RevisionPersister revisionPersister = 
      new FlyTrace.Service.Internals.RevisionPersister( );

    private void InitRevGen( )
    {

      string initWarnings;
      bool result = this.revisionPersister.Init( revgenFilePathTextBox.Text, out initWarnings );
      AddResultTextLine( "Init: " + result.ToString( ) + ", revision: " + this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
      AddResultTextLine( string.Format( "Init warnings: {0}", initWarnings ) );
    }

    private void revgenShutdownButton_Click( object sender, EventArgs e )
    {
      try
      {
        this.revisionPersister.Shutdown( );
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
        AddResultText( this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
        AddResultText( ", incrementing by 1..." );

        this.revisionPersister.ThreadUnsafeRevision++;
        AddResultText( "Done, new revision: " );
        AddResultTextLine( this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
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
        AddResultTextLine( "Current revision: " + this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
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
        AddResultText( this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
        AddResultText( ", incrementing by 10..." );
        for ( int i = 0; i < 10; i++ )
          this.revisionPersister.ThreadUnsafeRevision++;
        AddResultText( "Done, new revision: " );
        AddResultTextLine( this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
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
        AddResultText( this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
        AddResultText( ", incrementing by 100..." );
        for ( int i = 0; i < 100; i++ )
          this.revisionPersister.ThreadUnsafeRevision++;
        AddResultText( "Done, new revision: " );
        AddResultTextLine( this.revisionPersister.ThreadUnsafeRevision.ToString( ) );
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
        if ( !this.revisionPersister.IsActive )
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
                  this.revisionPersister.Shutdown( );
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
                  resultRev = ++this.revisionPersister.ThreadUnsafeRevision;
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

    private void button6_Click( object sender, EventArgs e )
    {
      ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones( );

      string str = string.Join(
        "\r\n",
        timeZones.Select( tz => tz.Id ).ToArray( )
        );

      // MessageBox.Show(str);

      //TimeZoneInfo.AdjustmentRule rule = new 
      //TimeZoneInfo.CreateCustomTimeZone("syd", TimeSpan.FromHours(10), "Sydney", "Sydney", "Sydney");
    }
  }
}