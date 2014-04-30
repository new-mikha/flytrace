<%--
  Flytrace, online viewer for GPS trackers.
  Copyright (C) 2011-2014 Mikhail Karmazin
  
  This file is part of Flytrace.
  
  Flytrace is free software: you can redistribute it and/or modify
  it under the terms of the GNU Affero General Public License as
  published by the Free Software Foundation, either version 3 of the
  License, or (at your option) any later version.
  
  Flytrace is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU Affero General Public License for more details.
  
  You should have received a copy of the GNU Affero General Public License
  along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
--%>

<%@ Page Title="Home - FlyTrace" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="default.aspx.cs" Inherits="FlyTrace.DefaultPage" StylesheetTheme="Default" %>

<%@ Register TagPrefix="flyTrace" TagName="DefaultAnonymous" Src="~/defaultAnonymous.ascx" %>
<%@ Register TagPrefix="flyTrace" TagName="DefaultLoggedIn" Src="~/defaultLoggedIn.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript">
        var _gaq = _gaq || [];
        _gaq.push(['_setAccount', 'UA-44990201-1']);
        _gaq.push(['_setDomainName', 'flytrace.com']);
        _gaq.push(['_setAllowLinker', true]);
        _gaq.push(['_trackPageview']);

        (function ()
        {
            var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
            ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
            var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
        })();
    </script>
    <script type="text/javascript">
        function toggleHelp()
        {
            $('#helpPanel').slideToggle('fast');
            $('#collapsedHelpTitleEnding').toggle();
            $('#expandedHelpTitleEnding').toggle();
        }
    </script>
</asp:Content>
<asp:Content ID="Content3" runat="server" ContentPlaceHolderID="AnonymousContentPlaceHolder">
    <flyTrace:DefaultAnonymous ID="DefaultAnonymous1" runat="server" />
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="LoggedInContentPlaceHolder" runat="server">
    <flyTrace:DefaultLoggedIn ID="defaultLoggedIn" runat="server" />
    <asp:Button ID="showOtherGroupsButton" runat="server" Text="Show Others' Public Groups"
        OnClick="showOtherGroupsButton_click" />
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="CommonContentPlaceHolder">
    <asp:ScriptManager runat="server" />
    <script type="text/javascript" language="javascript">
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(onEndRequest);

        function onEndRequest(sender, args)
        {
            var lostInfoErr = document.getElementById("lostInfoErr");
            if (lostInfoErr != undefined)
            {
                if (args.get_error() == undefined)
                {
                    lostInfoErr.style.display = "none";
                }
                else
                {
                    lostInfoErr.style.display = "block";
                }
                args.set_errorHandled(true);
            }
        }
    </script>
    <asp:Panel runat="server" ID="othersGroupsPanel">
        <table class="GroupTable">
            <tr>
                <td class="VisualGroupTitle">
                    Others' Public Groups with 2 or more pilots
                </td>
            </tr>
            <tr>
                <td>
                    <asp:SqlDataSource ID="allNonUserGroupsDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>"
                        SelectCommand="<%$ Resources: Resources, SelectPublicGroupsSql %>">
                        <SelectParameters>
                            <flytrace_tools:EvalParameter Name="UserId" Expression="NullableUserId" />
                            <asp:Parameter Name="IsAdmin" DbType="Boolean" DefaultValue='<%$ FtCode: 
                                    FlyTrace.Global.IsAdmin || 
                                    FlyTrace.Global.IsSpotIdReader 
                                    %>' />
                        </SelectParameters>
                    </asp:SqlDataSource>
                    <asp:UpdatePanel runat="server" UpdateMode="Always">
                        <ContentTemplate>
                            <asp:Timer runat="server" Interval="30000">
                            </asp:Timer>
                            <div id="lostInfoErr" style="text-align: center; width: 100%;display:none">
                                <div class="InfoMessage" style="display: inline-block;">
                                    <b>Can't connect to the server, the times below might be not actual.</b>
                                </div>
                            </div>
                            <asp:GridView EnableViewState="False" Width="100%" ID="groupsGridView" runat="server"
                                AutoGenerateColumns="False" DataSourceID="allNonUserGroupsDataSource" DataKeyNames="Id"
                                GridLines="None" AllowSorting="True" OnDataBound="groupsGridView_DataBound" Style="margin-top: 0px"
                                OnRowDataBound="groupsGridView_RowDataBound">
                                <EmptyDataTemplate>
                                    Other people don't have yet Pilot Groups with 2 or more trackers.
                                </EmptyDataTemplate>
                                <Columns>
                                    <asp:TemplateField HeaderText="Owner" SortExpression="UserName">
                                        <ItemTemplate>
                                            <asp:Label ID="userNameLabel" runat="server" Text='<%# Bind("UserName") %>'></asp:Label>
                                        </ItemTemplate>
                                        <HeaderStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>
                                    <asp:TemplateField>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Group" SortExpression="Name">
                                        <ItemTemplate>
                                            <asp:MultiView ID="MultiView1" runat="server" ActiveViewIndex='<%$ FtCode: 
                                            FlyTrace.Global.IsAdmin || 
                                                FlyTrace.Global.IsSpotIdReader  
                                            ? 
                                            1
                                            : 
                                            0 %>'>
                                                <asp:View ID="View1" runat="server">
                                                    <asp:Label ID="groupNameLabel" runat="server" Text='<%# Bind("Name") %>'></asp:Label>
                                                </asp:View>
                                                <asp:View ID="View2" runat="server">
                                                    <a href='manageGroup.aspx?group=<%# Eval("Id")%>'>
                                                        <asp:Label ID="groupNameAndLinkLabel" runat="server" Text='<%# Bind("Name") %>'></asp:Label>
                                                    </a>
                                                </asp:View>
                                            </asp:MultiView>
                                        </ItemTemplate>
                                        <HeaderStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>
                                    <asp:TemplateField>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center"
                                        DataField="TrackersCount" HeaderText="Pilots" SortExpression="TrackersCount">
                                        <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
                                        <ItemStyle HorizontalAlign="Center"></ItemStyle>
                                    </asp:BoundField>
                                    <asp:TemplateField>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center"
                                        HeaderText="Updated" SortExpression="NewestCoordTs">
                                        <HeaderStyle HorizontalAlign="Center"></HeaderStyle>
                                        <ItemTemplate>
                                            <asp:Panel runat="server" ID="agePanel" Visible="false" EnableViewState="false">
                                                <asp:Label runat="server" ID="ageLabel" EnableViewState="false" Font-Bold="true" />
                                                <br />
                                            </asp:Panel>
                                            <asp:Label runat="server" ID="updateTsLabel" EnableViewState="false" />
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left"></ItemStyle>
                                    </asp:TemplateField>
                                    <asp:TemplateField>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center"
                                        DataField="ViewsNum" HeaderText="Views" SortExpression="ViewsNum"></asp:BoundField>
                                    <asp:TemplateField>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center"
                                        DataField="PageUpdatesNum" HeaderText="Updates" SortExpression="PageUpdatesNum"
                                        Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'></asp:BoundField>
                                    <asp:TemplateField Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center"
                                        DataField="DisplayUserMessages" HeaderText="UsrMsgs" SortExpression="DisplayUserMessages"
                                        Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'></asp:BoundField>
                                    <asp:TemplateField Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'>
                                        <ItemTemplate>
                                            |
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField ShowHeader="False">
                                        <ItemTemplate>
                                            <a href='map.aspx?group=<%# Eval("Id")%>' target="_blank">Public map link</a>
                                            <asp:Label ID="unlistedLabel" runat="server" Text='<%# ((bool)Eval("IsPublic")) ? "" : "(unlisted)"  %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <AlternatingRowStyle CssClass="AlternatingRowStyle" />
                                <RowStyle CssClass="RowStyle" />
                            </asp:GridView>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </td>
            </tr>
        </table>
    </asp:Panel>
    <br />
</asp:Content>
