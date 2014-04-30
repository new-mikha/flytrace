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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using log4net;
using System.IO;

namespace FlyTrace
{
  public partial class manageUsers : System.Web.UI.Page
  {
    ILog log = LogManager.GetLogger( "manageUsers" );

    // Most of the code here has been taken from
    // http://www.asp.net/web-forms/tutorials/security/admin/building-an-interface-to-select-one-user-account-from-many-cs
    //
    protected void Page_Load( object sender, EventArgs e )
    {
      if ( !Page.IsPostBack )
      {
        HttpCookie pageSizeCookie = Request.Cookies[PageSizeCookieName];
        if ( pageSizeCookie != null && !string.IsNullOrEmpty( pageSizeCookie.Value ) )
        {
          this.dropDownListPageSize.SelectedValue = pageSizeCookie.Value;
        }

        BindUserAccounts( );
        BindFilteringUI( );
      }
    }

    protected int NumOfSelectedRows = 0;

    protected void Page_PreRender( object sender, EventArgs e )
    {
      // If there was a problem deleting user, or any other stuff - there could be records that are still selected.
      // If so, we need to set the counter of the selected values to the correct value that's used in <%...%> block in 
      // the page, as well as set the right row style:
      for ( int iRow = 0; iRow < this.userAccounts.Rows.Count; iRow++ )
      {
        GridViewRow row = this.userAccounts.Rows[iRow];
        CheckBox selectionCheckBox = ( ( CheckBox ) row.FindControl( "selectionCheckBox" ) );
        string userName = this.userAccounts.DataKeys[iRow].Value.ToString( );

        if ( selectionCheckBox.Checked )
        {
          row.CssClass = "SelectedRow";
          NumOfSelectedRows++;
        }
      }
    }

    private const string allStr = "All";

    private void BindFilteringUI( )
    {
      string[] filterOptions = { allStr, "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
      this.filteringUI.DataSource = filterOptions;
      this.filteringUI.DataBind( );
    }

    private bool IsCustomFilter
    {
      get
      {
        object o = ViewState["IsCustomFilter"];
        if ( o == null )
          return false;
        else
          return ( bool ) o;
      }

      set
      {
        ViewState["IsCustomFilter"] = value;
      }
    }

    private string UsernameToMatch
    {
      get
      {
        object o = ViewState["UsernameToMatch"];
        if ( o == null )
          return "";
        else
          return ( string ) o;
      }

      set
      {
        ViewState["UsernameToMatch"] = value;
        this.areUsersRead = false;
      }
    }

    private int PageIndex
    {
      get
      {
        object o = ViewState["PageIndex"];
        if ( o == null )
          return 0;
        else
          return ( int ) o;
      }
      set
      {
        ViewState["PageIndex"] = value;
      }
    }

    private int PageSize
    {
      get
      {
        return Convert.ToInt32( this.dropDownListPageSize.SelectedValue );
      }
    }

    private bool areUsersRead = false;

    IEnumerable<MembershipUser> usersOnCurrPage;

    TrackerDataSet.UserStatDataTable groupsCounts;

    TrackerDataSet.UserStatDataTable uniqTrackersCounts;

    private int filteredUsersCount;

    private void ReadUsersFromDb( )
    {
      if ( this.areUsersRead )
        return;

      string strFilter;
      if ( IsCustomFilter )
        strFilter = "%" + UsernameToMatch + "%";
      else
        strFilter = UsernameToMatch + "%";

      this.currentFilterLabel.Text = strFilter;

      MembershipUserCollection muc = Membership.FindUsersByName( strFilter );
      this.filteredUsersCount = muc.Count;
      IEnumerable<MembershipUser> allUsersSortedByName = muc.Cast<MembershipUser>( );

      var userStatTableAdapter = new TrackerDataSetTableAdapters.UserStatTableAdapter( );
      this.groupsCounts = userStatTableAdapter.GetGroupsCounts( strFilter );
      this.uniqTrackersCounts = userStatTableAdapter.GetTrackersCounts( strFilter );

      IEnumerable<MembershipUser> allUsersSortedAsRequested = SortUsers( allUsersSortedByName );

      if ( PageSize == 0 )
      {
        this.usersOnCurrPage = allUsersSortedAsRequested;
      }
      else
      {
        this.usersOnCurrPage =
          allUsersSortedAsRequested
          .Skip( this.PageIndex * this.PageSize )
          .Take( this.PageSize );
      }

      this.areUsersRead = true;
    }

    private IEnumerable<MembershipUser> SortUsers( IEnumerable<MembershipUser> allUsersSortedByName )
    {
      if ( SortExpression == "UserName" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return allUsersSortedByName;

        return allUsersSortedByName.OrderByDescending( u => u.UserName );
      }

      if ( SortExpression == "Email" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return allUsersSortedByName.OrderBy( u => u.Email );

        return allUsersSortedByName.OrderByDescending( u => u.Email );
      }

      if ( SortExpression == "CreationDate" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return allUsersSortedByName.OrderBy( u => u.CreationDate );

        return allUsersSortedByName.OrderByDescending( u => u.CreationDate );
      }

      if ( SortExpression == "LastActivityDate" )
      {
        if ( SortDirection == SortDirection.Ascending )
          return allUsersSortedByName.OrderBy( u => u.LastActivityDate );

        return allUsersSortedByName.OrderByDescending( u => u.LastActivityDate );
      }

      if ( SortExpression == "IsLockedOut" )
      {
        IEnumerable<MembershipUser> result = allUsersSortedByName.OrderBy( u => u.IsLockedOut );
        if ( SortDirection == SortDirection.Descending )
          result = result.Reverse( );

        return result;
      }

      if ( SortExpression == "IsOnline" )
      {
        IEnumerable<MembershipUser> result = allUsersSortedByName.OrderBy( u => u.IsOnline );
        if ( SortDirection == SortDirection.Descending )
          result = result.Reverse( );

        return result;
      }

      if ( SortExpression == "Groups" )
      {
        return SortUsersByStat( allUsersSortedByName, this.groupsCounts );
      }

      if ( SortExpression == "Trackers" )
      {
        return SortUsersByStat( allUsersSortedByName, this.uniqTrackersCounts );
      }

      throw new ApplicationException( string.Format( "Unknown field to sort {0}", SortExpression ) );
    }

    private IEnumerable<MembershipUser> SortUsersByStat
    (
      IEnumerable<MembershipUser> allUsersSortedByName,
      TrackerDataSet.UserStatDataTable userStatsToOrder
    )
    {
      var result =
        from membershipUser in allUsersSortedByName
        join userStat in userStatsToOrder
          on membershipUser.UserName equals userStat.UserName
          into userStatGroup // we need left outer join
        from statCount in userStatGroup.DefaultIfEmpty( )
        orderby statCount == null ? 0 : statCount.StatCount
        select membershipUser;

      if ( SortDirection == SortDirection.Descending )
        result = result.Reverse( );

      return result;
    }

    private void BindUserAccounts( )
    {
      ReadUsersFromDb( );
      this.userAccounts.DataSource = this.usersOnCurrPage;
      this.userAccounts.DataBind( );

      this.usersCountLabel.Text = this.filteredUsersCount.ToString( );

      // Enable/disable the paging interface
      bool visitingFirstPage = ( this.PageIndex == 0 );
      this.lnkFirst.Enabled = !visitingFirstPage;
      this.lnkPrev.Enabled = !visitingFirstPage;

      int effectivePageSize;
      if ( PageSize == 0 )
        effectivePageSize = this.filteredUsersCount;
      else
        effectivePageSize = PageSize;

      int lastPageIndex = ( this.filteredUsersCount - 1 ) / effectivePageSize;

      if ( PageIndex > lastPageIndex )
        PageIndex = lastPageIndex;

      bool visitingLastPage = ( this.PageIndex >= lastPageIndex );
      this.lnkNext.Enabled = !visitingLastPage;
      this.lnkLast.Enabled = !visitingLastPage;

      this.dropDownListPageNumber.Items.Clear( );
      for ( int iPage = 0; iPage <= lastPageIndex; iPage++ )
      {
        this.dropDownListPageNumber.Items.Add( ( iPage + 1 ).ToString( ) );
      }
      this.dropDownListPageNumber.SelectedIndex = this.PageIndex;

      this.literalTotalRecords.Text = ( lastPageIndex + 1 ).ToString( );
    }

    protected void filteringUI_ItemCommand( object source, RepeaterCommandEventArgs e )
    {
      if ( e.CommandName == allStr )
        this.UsernameToMatch = string.Empty;
      else
        this.UsernameToMatch = e.CommandName;
      PageIndex = 0;
      this.customFilterTextBox.Text = "";
      IsCustomFilter = false;
      BindUserAccounts( );
      BindFilteringUI( );
    }

    protected void filteringUI_ItemDataBound( object sender, RepeaterItemEventArgs e )
    {
      LinkButton linkButton = e.Item.FindControl( "lnkFilter" ) as LinkButton;
      if ( linkButton != null )
      {
        string currSelection = UsernameToMatch;
        if ( currSelection == "" )
          currSelection = allStr;

        linkButton.Enabled =
          IsCustomFilter ||
          !string.Equals( e.Item.DataItem as string, currSelection );

        linkButton.Font.Bold = !linkButton.Enabled;
      }
    }

    protected void lnkFirst_Click( object sender, EventArgs e )
    {
      PageIndex = 0;
      BindUserAccounts( );
    }

    protected void lnkPrev_Click( object sender, EventArgs e )
    {
      PageIndex -= 1;
      BindUserAccounts( );
    }

    protected void lnkNext_Click( object sender, EventArgs e )
    {
      PageIndex += 1;
      BindUserAccounts( );
    }

    protected void lnkLast_Click( object sender, EventArgs e )
    {
      ReadUsersFromDb( );

      int effectivePageSize;
      if ( PageSize == 0 )
        effectivePageSize = this.filteredUsersCount;
      else
        effectivePageSize = PageSize;

      // Navigate to the last page index
      PageIndex = ( this.filteredUsersCount - 1 ) / effectivePageSize;
      BindUserAccounts( );
    }

    protected void dropDownListPageNumber_SelectedIndexChanged( object sender, EventArgs e )
    {
      this.PageIndex = this.dropDownListPageNumber.SelectedIndex;
      BindUserAccounts( );
    }

    private const string PageSizeCookieName = "UsersListPageSize";

    protected void dropDownListPageSize_SelectedIndexChanged( object sender, EventArgs e )
    {
      PageIndex = 0;
      BindUserAccounts( );

      HttpCookie pageSizeCookie = new HttpCookie( PageSizeCookieName, PageSize.ToString( ) );
      pageSizeCookie.Expires = DateTime.Now.AddYears( 1 );
      pageSizeCookie.Path = Path.GetDirectoryName( Request.FilePath ).Replace( '\\', '/' );
      Response.Cookies.Add( pageSizeCookie );

      //Response.Cookies[PageSizeCookieName].Value = PageSize.ToString();
    }

    protected void applyCustomFilterButton_Click( object sender, EventArgs e )
    {
      string trimmedFilter = this.customFilterTextBox.Text.Trim( );
      if ( trimmedFilter == "" )
      {
        this.UsernameToMatch = "";
        this.IsCustomFilter = false;
      }
      else
      {
        this.UsernameToMatch = trimmedFilter;
        this.IsCustomFilter = true;
      }
      BindUserAccounts( );
      BindFilteringUI( );
    }

    protected void deleteSelectedButton_Click( object sender, EventArgs e )
    {

      try
      {
        for ( int iRow = 0; iRow < this.userAccounts.Rows.Count; iRow++ )
        {
          GridViewRow row = this.userAccounts.Rows[iRow];
          CheckBox selectionCheckBox = ( ( CheckBox ) row.FindControl( "selectionCheckBox" ) );

          if ( selectionCheckBox.Checked )
          {
            string userName = this.userAccounts.DataKeys[iRow].Value.ToString( );
            if ( userName.ToLower( ) == User.Identity.Name.ToLower( ) )
            {
              this.errorLiteral.Text = "You can't delete your own account, so it was left as-is.";
              // continue
            }
            else
            {
              Membership.DeleteUser( userName );
            }
          }
        }
        BindUserAccounts( );
      }
      catch ( Exception exc )
      {
        this.errorLiteral.Text = exc.Message;
        this.log.Error( "Error while deleting list of users", exc );
      }
    }

    protected void userAccounts_RowDeleting( object sender, GridViewDeleteEventArgs e )
    {
      try
      {
        string userName = this.userAccounts.DataKeys[e.RowIndex].Value.ToString( );

        if ( userName.ToLower( ) == User.Identity.Name.ToLower( ) )
        {
          this.errorLiteral.Text = "You can't delete your own account";
        }
        else
        {
          Membership.DeleteUser( userName );
          BindUserAccounts( );
        }
      }
      catch ( Exception exc )
      {
        this.errorLiteral.Text = exc.Message;
        this.log.Error( "Error while deleting list of users", exc );
      }
    }

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( "../default.aspx", true );
    }

    protected void userAccounts_RowDataBound( object sender, GridViewRowEventArgs e )
    {
      MembershipUser user = e.Row.DataItem as MembershipUser;
      if ( user == null )
        return;

      {
        Label numberOfGroupsLabel = ( Label ) ( e.Row.FindControl( "numberOfGroupsLabel" ) );
        if ( numberOfGroupsLabel != null )
        {
          TrackerDataSet.UserStatRow groupCountRow = this.groupsCounts.FindByUserName( user.UserName );
          if ( groupCountRow != null )
          {
            numberOfGroupsLabel.Text = groupCountRow.StatCount.ToString( );
          }
        }
      }

      {
        Label numberOfUniqTrackersLabel = ( Label ) ( e.Row.FindControl( "numberOfUniqTrackers" ) );
        if ( numberOfUniqTrackersLabel != null )
        {
          TrackerDataSet.UserStatRow trackersCountRow = this.uniqTrackersCounts.FindByUserName( user.UserName );
          if ( trackersCountRow != null )
          {
            numberOfUniqTrackersLabel.Text = trackersCountRow.StatCount.ToString( );
          }

        }
      }
    }

    private string SortExpression
    {
      get
      {
        object sortExpressionObj = ViewState["CurrentSortField"];
        if ( sortExpressionObj == null )
          return "UserName";

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
          return SortDirection.Ascending;

        return ( SortDirection ) sortDirectionObj;
      }

      set
      {
        ViewState["CurrentSortDir"] = value;
      }
    }

    protected void userAccounts_Sorting( object sender, GridViewSortEventArgs e )
    {
      // "Automatic bidirectional sorting only works with the SQL data source."
      // (here: http://stackoverflow.com/questions/250037/gridview-sorting-sortdirection-always-ascending/399880#399880 )
      { // So switch direction manually if necessary:
        if ( SortExpression != e.SortExpression )
        {
          SortExpression = e.SortExpression;

          // Sort dates in descending order by default:
          if ( SortExpression == "CreationDate" ||
               SortExpression == "LastActivityDate" )
          {
            SortDirection = SortDirection.Descending;
          }
          else
          {
            SortDirection = SortDirection.Ascending;
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

      foreach ( DataControlField column in this.userAccounts.Columns )
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

      BindUserAccounts( );
    }
  }
}