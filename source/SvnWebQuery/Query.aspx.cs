﻿#region Apache License 2.0

// Copyright 2008-2009 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using SvnQuery;
using SvnWebQuery.Code;

namespace SvnWebQuery
{
    public partial class Query : Page
    {
        static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // overriding OnPreRender instead of OnInit or OnLoad because
            // the OnSearchButton postback will redirect always, 
            // discarding any work done in OnInit or OnLoad

            InitPageDescription();
            ExtractQueryParameters();
            InitQueryUserInterface();
            DisplayQueryResults();
        }

        /// <summary>
        /// Sets title and version information.
        /// </summary>
        void InitPageDescription()
        {
            Title = ApplicationIndex.Name + " Search";
            _repositoryLabel.Text = Title;
            _version.Text = Version;
        }

        /// <summary>
        /// Extracts query, first and last revision from url query parameters to hidden fields.
        /// </summary>
        void ExtractQueryParameters()
        {
            if (IsPostBack) return;
            _query.Value = NormalizeQuery(Context.Request.QueryString["q"] ?? "");
            _revFirst.Value = Context.Request.QueryString["f"] ?? Revision.HeadString;
            _revLast.Value = Context.Request.QueryString["l"] ?? Revision.HeadString;
        }

        /// <summary>
        /// Initializes the query control elements (querytext and revisionrange).
        /// </summary>
        void InitQueryUserInterface()
        {
            _inputQuery.Text = _query.Value;
            if (ApplicationIndex.IsSingleRevision)
            {
                revisionContainer.Visible = false;
            }
            else
            {
                _optAll.Checked = _revFirst.Value == "0" && _revLast.Value == Revision.HeadString;
                _optHead.Checked = _revFirst.Value == Revision.HeadString && _revLast.Value == Revision.HeadString;

                if (_optAll.Checked || _optHead.Checked)
                {
                    _revision.Style[HtmlTextWriterStyle.Display] = "none";
                    _revisionOptions.Style[HtmlTextWriterStyle.Display] = "";
                    _revision.Text = "$hidden$";
                }
                else
                {
                    _optHead.Checked = true; // default, if the user switches back from explicit revision range to simple revision range
                    _revision.Style[HtmlTextWriterStyle.Display] = "";
                    _revisionOptions.Style[HtmlTextWriterStyle.Display] = "none";
                    _revision.Text = NormalizeRevision(_revFirst.Value);
                    if (_revLast.Value != _revFirst.Value)
                    {
                        _revision.Text += " : " + NormalizeRevision(_revLast.Value);
                    }
                }
            }
        }

        void DisplayQueryResults()
        {
            if (string.IsNullOrEmpty(_query.Value))
                return;

            try
            {
                Result r = ApplicationIndex.Query(_query.Value, _revFirst.Value, _revLast.Value, IsPostBack);
                string htmlQuery = Server.HtmlEncode(r.Query);
                _hitsLabel.Text = string.Format("<b>{0}</b> hits for <b>{1}</b>", r.Hits.Count, htmlQuery);
                _statisticsLabel.Text =
                    string.Format("<span style='color:#808080'>{0} documents searched in {1}ms. Index revision {2}</span>",
                                  r.Index.TotalCount, (int)r.SearchTime.TotalMilliseconds, r.Index.Revision);

                _dataPager.Visible = (_dataPager.MaximumRows < r.Hits.Count);
                // Reset to page 0
                if (!IsPostBack)
                    _dataPager.SetPageProperties(0, _dataPager.MaximumRows, true);
                _resultsPanel.Visible = true;
            }
            catch (Exception x)
            {
                _messsageLabel.Text =
                    "An error occured. Most probably your query has some wildcards that lead to too many results. Try narrowing down your query.</br></br><b>Details: </b>" +
                    "<pre>" + x + "</pre>";
                _messsageLabel.Visible = true;
            }
        }

        protected void OnSearch(object sender, EventArgs e)
        {
            StringBuilder redirect = new StringBuilder();
            string queryText = NormalizeQuery(_inputQuery.Text);
            RevisionRange rr = GetRevisionRange();
            redirect.Append("Query.aspx");
            redirect.Append("?q=");
            redirect.Append(HttpUtility.UrlEncode(queryText));
            if (rr.First != Revision.Head)
            {
                redirect.Append("&f=");
                redirect.Append(rr.First);
            }
            if (rr.Last != Revision.Head)
            {
                redirect.Append("&l=");
                redirect.Append(rr.Last);
            }
            Context.Response.Redirect(redirect.ToString());
        }

        struct RevisionRange
        {
            public int First;
            public int Last;
        }

        RevisionRange GetRevisionRange()
        {
            string text = _revision.Text.ToLowerInvariant();
            if (text == "$hidden$")
            {
                return new RevisionRange
                       {
                           First = (_optHead.Checked ? Revision.Head : Revision.All),
                           Last = Revision.Head
                       };
            }

            Match m = Regex.Match(text, @"\d{1,8}");
            int first = -1;
            int last = -1;
            if (m.Success)
            {
                first = int.Parse(m.ToString());
                m = m.NextMatch();
                if (m.Success) last = int.Parse(m.ToString());
            }
            if (text.Contains("all"))
            {
                first = last = Revision.All;
                _revision.Text = "all";
            }
            else if (text.Contains("head") || first < 0)
            {
                last = Revision.Head;
                if (first < 0)
                {
                    first = last;
                    _revision.Text = "head";
                }
                else
                {
                    _revision.Text = first + " : head";
                }
            }
            else if (first > 0)
            {
                if (last > 0 && last < first)
                {
                    int swap = first;
                    first = last;
                    last = swap;
                }
                _revision.Text = first.ToString();
                if (last > 0) _revision.Text += " : " + last;
            }
            return new RevisionRange {First = first, Last = last > 0 ? last : first};
        }

        static string NormalizeRevision(string revision)
        {
            return revision == Revision.HeadString ? "head" : revision;
        }

        static string NormalizeQuery(string query)
        {
            return query.Trim().ToLower().Replace('\\', '/');
        }

        protected void DownloadResults_Click(object sender, EventArgs e)
        {
            Response.ContentType = "application/x-msdownload";
            string time = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            Response.AppendHeader("content-disposition", "attachment; filename=QueryResults_" + time + ".tsv");

            Response.Write(Join("Path", "File", "Author", "Modified", "Revision", "Size"));
            var result = ApplicationIndex.Query(_query.Value, _revFirst.Value, _revLast.Value, true);
            foreach (Hit hit in result.Hits.OrderBy(hit => hit.Path, StringComparer.InvariantCultureIgnoreCase))
            {
                Response.Write(Join(hit.Path, hit.File, hit.Author, hit.LastModification.ToString("g"), hit.RevisionFirst, hit.SizeInBytes.ToString()));
            }
            Response.End();
        }

        static string Join(params string[] strings)
        {
            return string.Join("\t", strings) + "\n";
        }

        protected void DownloadTargets_Click(object sender, EventArgs e)
        {
            Response.ContentType = "application/x-msdownload";
            string time = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            Response.AppendHeader("content-disposition", "attachment; filename=QueryResults_" + time + ".txt");

            var result = ApplicationIndex.Query(_query.Value, _revFirst.Value, _revLast.Value, true);
            foreach (Hit hit in result.Hits.OrderBy(hit => hit.Path, StringComparer.InvariantCultureIgnoreCase))
            {
                Response.Write(result.Index.RepositoryExternalUri + hit.Path + Environment.NewLine);
            }
            Response.End();
        }
    }
}