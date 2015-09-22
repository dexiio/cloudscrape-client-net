using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CloudScrapeAPI;

namespace CloudScrapeDemo
{
    public partial class CloudScrapeExample : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            const string API_KEY = "Enter API Key";
            const string ACCOUNT_ID = "Enter account ID";
            const string EXECUTION_ID = "Enter execution ID";

            CloudScrape objCloudScrape = new CloudScrape();
            objCloudScrape.Init(API_KEY, ACCOUNT_ID);
            var exec = objCloudScrape.Executions();

            var objCloudScrapeExecutionDTO = exec.GetResult(EXECUTION_ID);
        }
    }
}
