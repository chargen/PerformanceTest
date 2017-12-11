﻿using AzurePerformanceTest;
using PerformanceTest;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Nightly
{
    public partial class BenchmarkHistory : System.Web.UI.Page
    {
        public DateTime _startTime = DateTime.Now;
        Dictionary<string, string> _defaultParams = null;

        private AzureExperimentManager expManager = null;
        private AzureSummaryManager summaryManager = null;
        private Timeline timeline;
        private Tags tags;
        private string filename;

        public TimeSpan RenderTime
        {
            get { return DateTime.Now - _startTime; }
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {

                try
                {
                    _defaultParams = new Dictionary<string, string>();

                    string summaryName = Request.Params.Get("summary");
                    summaryName = summaryName == null ? Properties.Settings.Default.SummaryName : summaryName;
                    _defaultParams.Add("summary", summaryName);

                    var connectionString = await Helpers.GetConnectionString();
                    expManager = AzureExperimentManager.Open(connectionString);
                    summaryManager = new AzureSummaryManager(connectionString, Helpers.GetDomainResolver());

                    timeline = await Helpers.GetTimeline(summaryName, expManager, summaryManager, true);
                    tags = await Helpers.GetTags(summaryName, summaryManager);

                    filename = Request.Params.Get("filename");
                    txtFilename.Text = filename;

                    BuildEntries();
                }
                catch (Exception ex)
                {
                    Label l = new Label();
                    l.Text = "Error loading dataset: " + ex.Message;
                    phMain.Controls.Add(l);
                    l = new Label();
                    l.Text = "Stacktrace: " + ex.StackTrace;
                    phMain.Controls.Add(l);
                }
            }
        }

        public string Filename { get { return filename; } }

        protected async void BuildEntries()
        {
            var last_job = timeline.GetLastExperiment();
            int last_tag_id = 0;
            string last_tag_name = "";
            foreach (KeyValuePair<string, int> kvp in tags)
            {
                if (kvp.Value > last_tag_id)
                {
                    last_tag_name = kvp.Key;
                    last_tag_id = kvp.Value;
                }
            }

            TableRow tr;
            TableCell tc;
            HyperLink h;

            int i = 0;
            foreach (var exp in timeline.Experiments)
            {
                tr = new TableRow();

                if (i++ % 2 == 0) tr.BackColor = Color.LightGreen;
                else tr.BackColor = Color.LightGray;

                string id = exp.Id.ToString();

                tc = new TableCell();
                h = new HyperLink();
                h.Text = id;
                h.NavigateUrl = "Default.aspx?job=" + id;
                tc.Controls.Add(h);
                tr.Cells.Add(tc);

                tc = new TableCell();
                tc.Text = exp.SubmissionTime.ToString();
                tc.HorizontalAlign = HorizontalAlign.Right;
                if (exp.IsFinished)
                    tc.ForeColor = Color.Black;
                else
                    tc.ForeColor = Color.Gray;
                tr.Cells.Add(tc);


                ExperimentResults eresults = await expManager.GetResults(exp.Id);
                IEnumerable<BenchmarkResult> results = eresults.Benchmarks.Where(entry => entry.BenchmarkFileName == filename);

                tc = new TableCell();
                tc.Text = results.Count() == 0 ? "None" : results.First().Status.ToString();
                tc.HorizontalAlign = HorizontalAlign.Left;
                tr.Cells.Add(tc);

                tc = new TableCell();
                tc.Text = results.Count() == 0 ? "" : results.First().CPUTime.ToString();
                tc.HorizontalAlign = HorizontalAlign.Right;
                tr.Cells.Add(tc);

                tblEntries.Rows.Add(tr);
            }
        }

    }
}