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
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using FlyTrace.LocationLib;
using System.Web.Security;
using System.Threading;
using FlyTrace.LocationLib.Data;

namespace FlyTrace.Service.Administration
{
  public partial class currentTrackers : System.Web.UI.Page
  {
    [DebuggerDisplay( "{RefreshTime} - {SpotId}" )]
    public class TrackerDisplayItem
    {
      public string SpotId { get; set; }

      public DateTime CurrentTs { get; set; }

      public string CurrentTsStr { get; set; }

      public string CurrentCoord { get; set; }

      public DateTime PrevTs { get; set; }

      public string PrevTsStr { get; set; }

      public string PrevCoord { get; set; }

      public string Error { get; set; }

      public string ErrorTag { get; set; }

      public DateTime AccessTime { get; set; }

      public string AccessTimeStr { get; set; }

      public DateTime CreateTime { get; set; }

      public string CreateTimeStr { get; set; }

      public DateTime RefreshTime { get; set; }

      public string RefreshTimeStr { get; set; }

      public int? Revision { get; set; }

      public string Tag { get; set; }
    }

    protected void Page_Load( object sender, EventArgs e )
    {
      if ( !Page.IsPostBack )
      {
        BindDataSource( );
      }

      DataSet statistics = ForeignRequestsManager.Singleton.GetStatistics( );
      if ( statistics == null )
        this.statPanel.Visible = false;
      else
      {
        bool isFirst = true;
        foreach ( DataTable table in statistics.Tables )
        {
          Label label = new Label( );
          this.statPanel.Controls.Add( label );
          label.Text = ( isFirst ? "<br />" : "" ) + table.TableName;
          label.Font.Bold = true;
          label.Font.Size = FontUnit.Larger;

          GridView gridView = new GridView( );
          this.statPanel.Controls.Add( gridView );
          gridView.DataSource = statistics.Tables[0];
          gridView.DataBind( );

          isFirst = false;
        }
      }
    }

    private string[] spotIds;

    private void BindDataSource( )
    {
      List<TrackerStateHolder> trackers;

      ForeignRequestsManager.Singleton.HolderRwLock.AttemptEnterReadLock( );
      try
      {
        trackers = ForeignRequestsManager.Singleton.Trackers.Values.ToList( );
      }
      finally
      {
        ForeignRequestsManager.Singleton.HolderRwLock.ExitReadLock( );
      }

      List<TrackerDisplayItem> list;
      list = new List<TrackerDisplayItem>( trackers.Count );

      foreach ( TrackerStateHolder holder in trackers )
      {
        TrackerDisplayItem item = new TrackerDisplayItem( );
        item.SpotId = holder.ForeignId.Id;

        DateTime accessTime =
          DateTime.FromFileTime( Interlocked.Read( ref holder.ThreadDesynchronizedAccessTimestamp ) ).ToUniversalTime( );

        item.AccessTime = accessTime;
        item.AccessTimeStr = accessTime.ToString( "u" ) + "<br />" + LocationLib.Tools.GetAgeStr( accessTime, true );

        // No need to lock on TrackerDataManager.snapshotAccessSync since it doesn't matter if one of the snapshots is
        // updated during the cycle. MemoryBarrier for atomic read of a single Snapshot below is enough, it makes sure
        // that Snapshot value (which is not volatile) is read once only:
        RevisedTrackerState tracker = holder.Snapshot;
        Thread.MemoryBarrier( );

        if ( tracker != null )
        {
          item.Revision = tracker.DataRevision;

          item.CreateTime = tracker.CreateTime;
          item.CreateTimeStr =
            item.CreateTime.ToString( "u" ) + "<br />" +
            LocationLib.Tools.GetAgeStr( item.CreateTime, true );

          item.RefreshTime = holder.RefreshTime.GetValueOrDefault( );
          item.RefreshTimeStr =
            item.RefreshTime.ToString( "u" ) + "<br />" +
            LocationLib.Tools.GetAgeStr( item.RefreshTime, true );

          if ( tracker.Position != null )
          {
            item.Tag = tracker.Tag;

            {
              TrackPointData currPoint = tracker.Position.CurrPoint;
              item.CurrentTs = currPoint.ForeignTime;
              item.CurrentCoord = string.Format( "{0}, {1}<br/>{2}", currPoint.Latitude, currPoint.Longitude, LocationLib.Tools.GetAgeStr( currPoint.ForeignTime, true ) );
              item.CurrentTsStr = string.Format( "{0}", currPoint.ForeignTime.ToString( "u" ) );
            }

            {
              TrackPointData prevPoint = tracker.Position.PreviousPoint;
              if ( prevPoint != null )
              {
                item.PrevTs = prevPoint.ForeignTime;
                item.PrevCoord = string.Format( "{0}, {1}<br/>{2}", prevPoint.Latitude, prevPoint.Longitude, LocationLib.Tools.GetAgeStr( prevPoint.ForeignTime, true ) );
                item.PrevTsStr = string.Format( "{0}", prevPoint.ForeignTime.ToString( "u" ) );
              }
            }
          }

          if ( tracker.Error != null )
          {
            item.Error = tracker.Error.ToString( );
            item.ErrorTag = tracker.Tag;
          }
        }
        list.Add( item );
      }

      IEnumerable<TrackerDisplayItem> sortedList = Sort( list );
      this.spotIds = sortedList.Select( i => i.SpotId ).ToArray( );
      trackersGridView.DataSource = sortedList;
      trackersGridView.DataBind( );
    }

    private IEnumerable<TrackerDisplayItem> Sort( List<TrackerDisplayItem> list )
    {
      if ( SortExpression == "SpotId" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.SpotId ); ;

        return list.OrderByDescending( i => i.SpotId );
      }

      if ( SortExpression == "Tag" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.Tag ); ;

        return list.OrderByDescending( i => i.Tag );
      }

      if ( SortExpression == "CurrentTs" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.CurrentTs ); ;

        return list.OrderByDescending( i => i.CurrentTs );
      }

      if ( SortExpression == "PrevTs" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.PrevTs ); ;

        return list.OrderByDescending( i => i.PrevTs );
      }

      if ( SortExpression == "AccessTime" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.AccessTime ); ;

        return list.OrderByDescending( i => i.AccessTime );
      }

      if ( SortExpression == "CreateTime" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.CreateTime ); ;

        return list.OrderByDescending( i => i.CreateTime );
      }

      if ( SortExpression == "RefreshTime" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.RefreshTime ); ;

        return list.OrderByDescending( i => i.RefreshTime );
      }

      if ( SortExpression == "Revision" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.Revision ); ;

        return list.OrderByDescending( i => i.Revision );
      }

      if ( SortExpression == "Error" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return list.OrderBy( i => i.Error ); ;

        return list.OrderByDescending( i => i.Error );
      }

      throw new ApplicationException( string.Format( "Unknown field to sort {0}", SortExpression ) );
    }

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( "../default.aspx", true );
    }

    protected void applyFilterButton_Click( object sender, EventArgs e )
    {
    }

    protected void trackersGridView_RowDataBound( object sender, GridViewRowEventArgs e )
    {
      if ( e.Row.DataItemIndex < 0 ) return;

      this.autoSelectionChange = true;
      try
      {
        string spotId = this.spotIds[e.Row.DataItemIndex];

        CheckBox selectionCheckBox = ( CheckBox ) e.Row.FindControl( "selectionCheckBox" );

        if ( SelectedSpotIds.Contains( spotId ) )
        {
          selectionCheckBox.Checked = true;
          e.Row.CssClass = "SelectedRow";
        }
        else
        {
          selectionCheckBox.Checked = false;
          e.Row.CssClass = "";
        }
      }
      finally
      {
        this.autoSelectionChange = false;
      }
    }

    private string SortExpression
    {
      get
      {
        object sortExpressionObj = ViewState["CurrentSortField"];
        if ( sortExpressionObj == null )
          return "AccessTime";

        return sortExpressionObj.ToString( );
      }
      set
      {
        ViewState["CurrentSortField"] = value;
      }
    }

    private SortDirection SortDirection
    {
      get
      {
        object sortDirectionObj = ViewState["CurrentSortDir"];
        if ( sortDirectionObj == null )
          return SortDirection.Descending;

        return ( SortDirection ) sortDirectionObj;
      }

      set
      {
        ViewState["CurrentSortDir"] = value;
      }
    }

    protected void trackersGridView_Sorting( object sender, GridViewSortEventArgs e )
    {
      // "Automatic bidirectional sorting only works with the SQL data source."
      // (here: http://stackoverflow.com/questions/250037/gridview-sorting-sortdirection-always-ascending/399880#399880 )
      { // So switch direction manually if necessary:
        if ( SortExpression != e.SortExpression )
        {
          SortExpression = e.SortExpression;

          // Sort dates in descending order by default:
          if ( SortExpression == "SpotId" )
          {
            SortDirection = SortDirection.Ascending;
          }
          else
          {
            SortDirection = SortDirection.Descending;
          }
        }
        else
        {
          if ( SortDirection == SortDirection.Ascending )
          {
            SortDirection = SortDirection.Descending;
          }
          else
          {
            SortDirection = SortDirection.Ascending;
          }
        }
      }

      foreach ( DataControlField column in this.trackersGridView.Columns )
      {
        column.HeaderText =
          column.HeaderText
          .Replace( "^", "" )
          .Replace( " ͮ_", "" )
          .Trim( );

        if ( column.SortExpression == SortExpression )
        {
          if ( SortDirection == SortDirection.Ascending )
          {
            column.HeaderText += " ^";
          }
          else
          {
            column.HeaderText += "  ͮ_";
          }
        }
      }

      BindDataSource( );
    }

    protected void refreshButton_Click( object sender, EventArgs e )
    {
      BindDataSource( );
    }

    private bool autoSelectionChange = false;

    private List<string> SelectedSpotIds
    {
      get
      {
        object selectedSpotIdsObj = ViewState["SelectedSpotIds"];
        if ( selectedSpotIdsObj == null )
        {
          selectedSpotIdsObj = new List<string>( );
          ViewState["SelectedSpotIds"] = selectedSpotIdsObj;
        }

        return ( List<string> ) selectedSpotIdsObj; ;
      }
    }

    protected void selectionCheckBox_CheckedChanged( object sender, EventArgs e )
    {
      if ( this.autoSelectionChange ) return;

      CheckBox selectionCheckBox = ( CheckBox ) sender;
      Control parent = selectionCheckBox.Parent;
      while ( parent != null )
      {
        if ( parent is GridViewRow )
        {
          GridViewRow gridViewRow = parent as GridViewRow;
          string spotId = trackersGridView.DataKeys[gridViewRow.DataItemIndex].Value.ToString( );
          if ( selectionCheckBox.Checked )
          {
            gridViewRow.CssClass = "SelectedRow";
            if ( !SelectedSpotIds.Contains( spotId ) )
            {
              SelectedSpotIds.Add( spotId );
            }
          }
          else
          {
            gridViewRow.CssClass = "";
            SelectedSpotIds.Remove( spotId );
          }
          break;
        }

        parent = parent.Parent;
      }
    }
  }
}