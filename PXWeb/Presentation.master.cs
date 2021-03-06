﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PCAxis.Web.Core.Management;
using System.Globalization;
using PCAxis.Web.Controls.CommandBar.Plugin;
using PCAxis.Web.Controls;

namespace PXWeb
{
    public partial class Presentation1 : System.Web.UI.MasterPage
    {
        public string HeadTitle
        {
            get
            {
                return Master.HeadTitle;
            }
            set
            {
                Master.HeadTitle = value;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (PCAxis.Web.Core.Management.PaxiomManager.PaxiomModel == null)
            {
                List<LinkManager.LinkItem> linkItems = new List<LinkManager.LinkItem>();
                linkItems.Add(new LinkManager.LinkItem(PxUrl.LANGUAGE_KEY, PxUrlObject.Language));
                linkItems.Add(new LinkManager.LinkItem(PxUrl.DB_KEY, PxUrlObject.Database));
                linkItems.Add(new LinkManager.LinkItem(PxUrl.PATH_KEY, PxUrlObject.Path));
                linkItems.Add(new LinkManager.LinkItem(PxUrl.TABLE_KEY, PxUrlObject.Table));
                
                string rurl = PCAxis.Web.Core.Management.LinkManager.CreateLink("Selection.aspx", linkItems.ToArray());
                Response.Redirect(rurl);
            }

            if (!IsPostBack)
            {
                imgShowSaveQueryExpander.ImageUrl = Page.ClientScript.GetWebResourceUrl(typeof(BreadcrumbCodebehind), "PCAxis.Web.Controls.spacer.gif");
                imgUnsafeMessage.ImageUrl = Page.ClientScript.GetWebResourceUrl(typeof(BreadcrumbCodebehind), "PCAxis.Web.Controls.spacer.gif");

                if (PCAxis.Web.Core.Management.PaxiomManager.PaxiomModel != null)
                {
                    Master.SetBreadcrumb(PCAxis.Web.Controls.Breadcrumb.BreadcrumbMode.Presentation);
                    Master.SetNavigationFlowMode(PCAxis.Web.Controls.NavigationFlow.NavigationFlowMode.Third);
                    Master.SetNavigationFlowVisibility(PXWeb.Settings.Current.Navigation.ShowNavigationFlow);
                    InitializeCommandBar();
                    InitializeInformationAndFootnotes();
                    InitializeTableQuery();
                    InitializeSaveQueryFunction();
                    this.panelTabs.Visible = ShowTabs();
                }
                //lnkSaveQueryInformation.NavigateUrl = PanelLink.BuildLink("savequery1");
                //lnkHideInformation.NavigateUrl = PanelLink.BuildLink("");
            }
            ShowMessages(PCAxis.Web.Core.Management.PaxiomManager.OperationsTracker.IsUnsafe);
            divUnsafeMessage.Visible = PCAxis.Web.Core.Management.PaxiomManager.OperationsTracker.IsUnsafe;
            SetSaveQueryLinks();
            CommandBar1.PxActionEvent += new PCAxis.Web.Controls.PxActionEventHandler(HandlePxAction);
        }

        /// <summary>
        /// Show/hide message area
        /// </summary>
        /// <param name="show"></param>
        public void ShowMessages(bool show)
        {
            divMessages.Visible = show;
        }

        private IPxUrl _pxUrl = null;

        private IPxUrl PxUrlObject
        {
            get
            {
                if (_pxUrl == null)
                {
                    _pxUrl = RouteInstance.PxUrlProvider.Create(null);
                }

                return _pxUrl;
            }
        }

        /// <summary>
        /// Handle PX-actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandlePxAction(object sender, PCAxis.Web.Controls.PxActionEventArgs e)
        {
            //Log visitor statistics
            if (PXWeb.Settings.Current.Features.General.UserStatisticsEnabled)
            {
                PCAxis.Web.Controls.VisitorStatisticsHelper.LogEvent(PCAxis.Web.Controls.ActionContext.Presentation, "userid", PxUrlObject.Language, PxUrlObject.Database, e);
            }
        }

        /// <summary>
        /// Get text in the currently selected language
        /// </summary>
        /// <param name="key">Key identifying the string in the language file</param>
        /// <returns>Localized string</returns>
        public string GetLocalizedString(string key)
        {
            string lang = LocalizationManager.CurrentCulture.Name;
            return PCAxis.Web.Core.Management.LocalizationManager.GetLocalizedString(key, new CultureInfo(lang));
        }

        /// <summary>
        /// Sets the commandbar filter
        /// </summary>
        /// <param name="filter">the filter</param>
        public void SetCommandBarFilter(ICommandBarPluginFilter filter)
        {
            CommandBar1.CommandBarFilter = filter;
            if (PXWeb.Settings.Current.Features.General.SavedQueryEnabled)
            {
                SavedQueryFeature.OutputFilter = filter;
            }
        }

        /// <summary>
        /// Sets the selected presentation view in the CommandBar
        /// </summary>
        /// <param name="presView">Presentation view</param>
        public void SetCommandBarPresentationView(string presView)
        {
            CommandBar1.SelectedPresentationView = presView;
        }


        /// <summary>
        /// Initializes CommandBar
        /// </summary>
        private void InitializeCommandBar()
        {
            CommandBarSettings.InitializeCommandBar(CommandBar1);
        }

        /// <summary>
        /// Initializes the Information and Footnote controls
        /// </summary>
        private void InitializeInformationAndFootnotes()
        {
            Footnotes.Visible = true;
            Information.Visible = true;
            Information.ShowInformationTypes = PXWeb.Settings.Current.General.Global.ShowInformationTypes.GetSelectedInformationTypes();
            Footnotes.ShowMandatoryOnly = false;
            Footnotes.ShowNoFootnotes = false;

            switch (PXWeb.Settings.Current.General.Global.TableInformationLevel)
            {
                case PCAxis.Paxiom.InformationLevelType.AllFootnotes:
                    Information.Visible = false;
                    break;
                case PCAxis.Paxiom.InformationLevelType.AllInformation:
                    break;
                case PCAxis.Paxiom.InformationLevelType.MandantoryFootnotesOnly:
                    Footnotes.ShowMandatoryOnly = true;
                    Information.Visible = false;
                    break;
                case PCAxis.Paxiom.InformationLevelType.None:
                    Footnotes.Visible = false;
                    Information.Visible = false;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// If Show metadata as link = true then the tabs shall not appear
        /// </summary>
        /// <returns></returns>
        private bool ShowTabs()
        {
            return ! PXWeb.Settings.Current.Selection.MetadataAsLinks;
        }
        /// <summary>
        /// Initialize the Table query web component
        /// </summary>
        private void InitializeTableQuery()
        {
            string helpPage;
            string db;

            if (!PXWeb.Settings.Current.Features.General.ApiEnabled || !PXWeb.Settings.Current.Features.Api.ShowQueryInformation)
            {
                TableQueryInformation.Visible = false;
                return;
            }

            db = PxUrlObject.Database;

            if (string.IsNullOrEmpty(db))
            {
                TableQueryInformation.Visible = false;
                return;
            }

            PaxiomManager.QueryModel.Response.Format = PXWeb.Settings.Current.Features.Api.DefaultExampleResponseFormat;
            DatabaseInfo dbi = PXWeb.Settings.Current.General.Databases.GetDatabase(db);

            if (dbi.Type == PCAxis.Web.Core.Enums.DatabaseType.CNMM)
            {
                TableQueryInformation.DatabaseType = PCAxis.Web.Core.Enums.DatabaseType.CNMM;
            }
            TableQueryInformation.URLRoot = PXWeb.Settings.Current.Features.Api.UrlRoot;
            TableQueryInformation.Database = db;
            TableQueryInformation.Path = PxUrlObject.Path;
            TableQueryInformation.Table = PxUrlObject.Table;

            helpPage = PCAxis.Web.Controls.Configuration.ConfigurationHelper.GetPxPage("apihelp");
            if (string.IsNullOrEmpty(helpPage))
            {
                helpPage = "~/ApiHelp.aspx";
            }

            TableQueryInformation.RoutePrefix = PXWeb.Settings.Current.Features.Api.RoutePrefix;
            TableQueryInformation.MoreInfoURL = helpPage;
            TableQueryInformation.MoreInfoIsExternalPage = true;
        }

        /// <summary>
        /// Initialize the save query usercontroll component
        /// </summary>
        private void InitializeSaveQueryFunction()
        {
            if (!PXWeb.Settings.Current.Features.General.SavedQueryEnabled)
            {
                SavedQueryFeature.Visible = false;
                lnkSaveQueryInformation.Visible = false;
                imgShowSaveQueryExpander.Visible = false;
                return;
            }
            //else
            //{
            //    SavedQueryFeature.OutputFilter = CommandBarFilterFactory.GetFilter(CommandBarPluginFilterType.TableLayout1.ToString());
            //}
            
        }

        /// <summary>
        /// If the save query feature shall be enabled or not
        /// </summary>
        /// <param name="enable"></param>
        public void EnableSaveQueryFeature(bool enable)
        {
            SavedQueryFeature.Enabled = enable;
        }

        const string timeTypeQueryStringParamName = "timeType";
        const string timeValueQueryStringParamName = "timeValue";
        const string loadedSavedQueryIdQueryStringParamName = "loadedQueryId";
        const string timeTypeItem = "item";
        const string timeTypeTop = "top";
        const string timeTypeFrom = "from";

        public bool ShowSavedQueryInfo
        {
            get
            {
                return Request.QueryString.AllKeys.Contains(timeTypeQueryStringParamName);
            }
        }

        protected string LoadedSavedQueryInfoHeader
        {
            get
            {
                return string.Format(GetLocalizedString("MasterPageSaveQueryInfoHeader"), LoadedSavedQueryId);
            }
        }

        protected string TimeTypeLoadedSavedQuery
        {
            get
            {
                if (!ShowSavedQueryInfo) return "";

                var timeType = Request.QueryString[timeTypeQueryStringParamName];

                switch (timeType)
                {
                    case timeTypeItem:
                        return GetLocalizedString("MasterPageSaveQueryInfoFixedTimeSelection");
                    case timeTypeTop:
                        return string.Format(GetLocalizedString("MasterPageSaveQueryInfoLatest"), TimeValueLoadedSavedQuery);
                    case timeTypeFrom:
                        return string.Format(GetLocalizedString("MasterPageSaveQueryInfoFromTime"), TimeValueLoadedSavedQuery);
                    default:
                        return "";
                }
            }
        }
        
        /// <summary>
        /// Get the TimeValueLoadedSavedQuery value
        /// </summary>
        private string TimeValueLoadedSavedQuery
        {
            get
            {
                if (!ShowSavedQueryInfo) return null;
                if (!Request.QueryString.AllKeys.Contains(timeValueQueryStringParamName)) return null;

                var timeType = Request.QueryString[timeTypeQueryStringParamName];
                var timeValueQueryString = Request.QueryString[timeValueQueryStringParamName];

                string lastSelectedTimeValue = "";

                var timeVar = PaxiomManager.PaxiomModel.Meta.Variables.First(x => x.IsTime);

                if (PCAxis.Web.Core.Management.PaxiomManager.QueryModel != null)
                {
                    var timeQueryFilter = PCAxis.Web.Core.Management.PaxiomManager.QueryModel.Query.FirstOrDefault(x => x.Code == timeVar.Code);

                    if (timeQueryFilter != null)
                    {
                        //The time vales are shown in descending order in GUI
                        lastSelectedTimeValue = timeQueryFilter.Selection.Values.First();
                    }
                }

                switch (timeType)
                {
                    case timeTypeItem:
                        return "";
                    case timeTypeTop:
                        return timeValueQueryString;
                    case timeTypeFrom:    
                        return lastSelectedTimeValue;
                    default:
                        return "";
                }
            }
        }

        protected string LoadedSavedQueryId
        {
            get
            {
                if (!ShowSavedQueryInfo) return null;
                if (!Request.QueryString.AllKeys.Contains(loadedSavedQueryIdQueryStringParamName)) return null;

                return Request.QueryString[loadedSavedQueryIdQueryStringParamName];
            }
        }

        private void SetSaveQueryLinks()
        {
            if (this.Visible)
            {
                if (!string.IsNullOrEmpty(QuerystringManager.GetQuerystringParameter(PanelLink.DISPLAY_PANEL)))
                {
                    if (QuerystringManager.GetQuerystringParameter(PanelLink.DISPLAY_PANEL).Equals("savequery1"))
                    {
                        lnkSaveQueryInformation.NavigateUrl = PanelLink.BuildLink("");
                        imgShowSaveQueryExpander.CssClass = "px-settings-collapseimage";
                        return;
                    }
                }
                lnkSaveQueryInformation.NavigateUrl = PanelLink.BuildLink("savequery1");
                imgShowSaveQueryExpander.CssClass = "px-settings-expandimage";
            }
        }

    }
}
