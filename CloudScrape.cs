using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace CloudScrapeAPI
{
    /// <summary>
    /// Cloud Response details
    /// </summary>
    class CloudResponse
    {
        public string Content { get; set; }
        public WebHeaderCollection Headers { get; set; }
        public HttpStatusCode statusCode { get; set; }
        public string StatusDescription { get; set; }
    }

    /// <summary>
    /// Cloud Scrape class
    /// </summary>
    class CloudScrape
    {

        private CloudScrapeClient client;

        /// <summary>
        /// Initial method 
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="accountId"></param>
        public void Init(string apiKey, string accountId)
        {
            client = new CloudScrapeClient(apiKey, accountId);
        }

        /// <summary>
        /// Default Client
        /// </summary>
        /// <returns></returns>
        public CloudScrapeClient DefaultClient()
        {
            CheckState();

            return client;
        }

        /// <summary>
        /// Execute Method
        /// </summary>
        /// <returns></returns>
        public CloudScrapeExecutions Executions()
        {
            CheckState();

            return client.Executions();
        }

        /// <summary>
        /// Run Method
        /// </summary>
        /// <returns></returns>
        public CloudScrapeRuns Runs()
        {
            CheckState();

            return client.Runs();
        }

        /// <summary>
        /// Check State
        /// </summary>
        private void CheckState()
        {
            if (client == null)
            {
                throw new Exception("You must call init first before using the API");
            }
        }
    }

    /// <summary>
    /// Cloud Scrape Client
    /// </summary>
    class CloudScrapeClient
    {
        private string apiKey;
        private string accountId;
        private string accessKey;

        private string _endPoint = "https://app.cloudscrape.com/api/";
        private string _userAgent = "CS-ASP-CLIENT/1.0";
        private int _requestTimeout = 60*60*1000;

        private CloudScrapeExecutions objExecutions;
        private CloudScrapeRuns objRuns;

        /// <summary>
        /// Endpoint / base url of requests
        /// </summary>
        public string EndPoint
        {
            get { return _endPoint; }
            set { _endPoint = value; }
        }

        /// <summary>
        /// User agent of requests
        /// </summary>
        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        /// <summary>
        /// Set request timeout. Defaults to 1 hour
        /// Note: If you are using the sync methods and some requests are running for very long you need to increase this value.
        /// </summary>
        public int RequestTimeout
        {
            get { return _requestTimeout; }
            set { _requestTimeout = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="accountId"></param>
        public CloudScrapeClient(string apiKey, string accountId)
        {
            this.apiKey = apiKey;
            this.accountId = accountId;
            this.accessKey = accountId + apiKey;
            this.objExecutions = new CloudScrapeExecutions(this);
            this.objRuns = new CloudScrapeRuns(this);
        }

        /// <summary>
        /// Make a call to the CloudScrape API
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public CloudResponse Request(string url, string method = "GET", string body = null)
        {
            string userPassword = CreateMD5(accessKey).ToLower();

            var req = System.Net.HttpWebRequest.Create(EndPoint + url) as HttpWebRequest;

            req.Headers.Add("X-CloudScrape-Access", userPassword);
            req.Headers.Add("X-CloudScrape-Account", accountId);
            req.UserAgent = _userAgent;
            req.Timeout = _requestTimeout;
            req.Accept = "application/json";
            req.ContentType = "application/json";
            req.Method = method;

            if (body != null)
            {
                using (var streamWriter = new StreamWriter(req.GetRequestStream()))
                {
                    streamWriter.Write(body);
                    streamWriter.Flush();
                }
            }

            CloudResponse objCloudResponse = null;
            HttpWebResponse response = null;
            StreamReader readStream = null;

            try
            {

                response = (HttpWebResponse)req.GetResponse();

                objCloudResponse = new CloudResponse();

                objCloudResponse.statusCode = response.StatusCode;
                objCloudResponse.Headers = response.Headers;
                objCloudResponse.StatusDescription = response.StatusDescription;

                WebHeaderCollection obj = response.Headers;
                Stream receiveStream = response.GetResponseStream();
                readStream = new StreamReader(receiveStream, Encoding.UTF8);
                objCloudResponse.Content = readStream.ReadToEnd();

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }

                if (readStream != null)
                {
                    readStream.Close();
                }
            }

            return objCloudResponse;
        }

        /// <summary>
        /// MD5 Encryption
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Execute request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public string RequestJson(string url, string method = "GET", string body = null)
        {
            CloudResponse response = this.Request(url, method, body);
            return response.Content;
        }

        /// <summary>
        /// Get response boolean value
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public bool RequestBoolean(string url, string method = "GET", string body = null)
        {
            this.Request(url, method, body);
            return true;
        }

        /// <summary>
        /// Interact with executions.
        /// </summary>
        /// <returns></returns>
        public CloudScrapeExecutions Executions()
        {
            return this.objExecutions;
        }

        /// <summary>
        /// Interact with runs
        /// </summary>
        /// <returns></returns>
        public CloudScrapeRuns Runs()
        {
            return this.objRuns;
        }

    }

    /// <summary>
    /// Execute Cloud Scrape request
    /// </summary>
    class CloudScrapeExecutions
    {

        private CloudScrapeClient client;
        private JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();

        public CloudScrapeExecutions(CloudScrapeClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Get execution
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns></returns>
        public CloudScrapeExecutionDTO Get(string executionId)
        {
            string strResponse = this.client.RequestJson("executions/" + executionId);

            var result = jsonSerializer.Deserialize<CloudScrapeExecutionDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Delete execution permanently
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns></returns>
        public bool Remove(string executionId)
        {
            return this.client.RequestBoolean("executions/" + executionId, "DELETE");
        }

        /// <summary>
        /// Get the entire result of an execution.
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns></returns>
        public CloudScrapeResultDTO GetResult(string executionId)
        {
            string strResponse = this.client.RequestJson("executions/" + executionId + "/result");
            var result = jsonSerializer.Deserialize<CloudScrapeResultDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Get a file from a result set
        /// </summary>
        /// <param name="executionId"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public CloudScrapeFileDTO GetResultFile(string executionId, string fileId)
        {
            var response = this.client.Request("executions/" + executionId + "/file/" + fileId);
            return new CloudScrapeFileDTO(response.Headers["Content-Type"], response.Content);
        }

        /// <summary>
        /// Stop running execution
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns></returns>
        public bool Stop(string executionId)
        {
            return this.client.RequestBoolean("executions/" + executionId + "/stop", "POST");
        }

        /// <summary>
        /// Resume stopped execution
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns></returns>
        public bool Resume(string executionId)
        {
            return this.client.RequestBoolean("executions/" + executionId + "/continue", "POST");
        }
    }

    /// <summary>
    /// Run Cloud Scrape robots
    /// </summary>
    class CloudScrapeRuns
    {

        private CloudScrapeClient client;
        private JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client"></param>
        public CloudScrapeRuns(CloudScrapeClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Get Run Detail
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public CloudScrapeRunDTO Get(string runId)
        {

            string strResponse = this.client.RequestJson("runs/" + runId);
            var result = jsonSerializer.Deserialize<CloudScrapeRunDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Permanently delete run
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool Remove(string runId)
        {
            return this.client.RequestBoolean("runs/" + runId, "DELETE");
        }

        /// <summary>
        /// Start new execution of the run
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public CloudScrapeExecutionDTO Execute(string runId)
        {
            string strResponse = this.client.RequestJson("runs/" + runId + "/execute", "POST");
            var result = jsonSerializer.Deserialize<CloudScrapeExecutionDTO>(strResponse);
            return result;
        }

        /// <summary>
        ///  Start new execution of the run, and wait for it to finish before returning the result.
        ///  The execution and result will be automatically deleted from CloudScrape completion
        ///  both successful and failed.
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public CloudScrapeResultDTO ExecuteSync(string runId)
        {
            string strResponse = this.client.RequestJson("runs/" + runId + "/execute/wait", "POST");
            var result = jsonSerializer.Deserialize<CloudScrapeResultDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Starts new execution of run with given inputs
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public CloudScrapeExecutionDTO ExecuteWithInput(string runId, string inputs)
        {
            string strResponse = this.client.RequestJson("runs/" + runId + "/execute/inputs", "POST", inputs);
            var result = jsonSerializer.Deserialize<CloudScrapeExecutionDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Starts new execution of run with given inputs, and wait for it to finish before returning the result.
        /// The inputs, execution and result will be automatically deleted from CloudScrape upon completion - both successful and failed.
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public CloudScrapeResultDTO ExecuteWithInputSync(string runId, string inputs)
        {
            string strResponse = this.client.RequestJson("runs/" + runId + "/execute/inputs/wait", "POST", inputs);
            var result = jsonSerializer.Deserialize<CloudScrapeResultDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Get the result from the latest execution of the given run.
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public CloudScrapeResultDTO GetLatestResult(string runId)
        {
            string strResponse = this.client.RequestJson("runs/" + runId + "/latest/result");
            var result = jsonSerializer.Deserialize<CloudScrapeResultDTO>(strResponse);
            return result;
        }

        /// <summary>
        /// Get executions for the given run.
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public CloudScrapeExecutionListDTO GetExecutions(string runId, int offset = 0, int limit = 30)
        {
            string strResponse = this.client.RequestJson("runs/" + runId + "/executions?offset=" + offset + "&limit=" + limit);
            var result = jsonSerializer.Deserialize<CloudScrapeExecutionListDTO>(strResponse);
            return result;
        }
    }

    /// <summary>
    /// Cloud Scrape Execution DTO class
    /// </summary>
    class CloudScrapeExecutionDTO
    {
        const string QUEUED = "QUEUED";
        const string PENDING = "PENDING";
        const string RUNNING = "RUNNING";
        const string FAILED = "FAILED";
        const string STOPPED = "STOPPED";
        const string OK = "OK";

        /// <summary>
        /// The ID of the execution
        /// </summary>
        public string _id;

        /// <summary>
        /// State of the executions. See const definitions on class to see options
        /// </summary>
        public string _state;

        /// <summary>
        /// Time the executions was started - in milliseconds since unix epoch
        /// </summary>
        public int _starts;

        /// <summary>
        /// Time the executions finished - in milliseconds since unix epoch.
        /// </summary>
        public int _finished;

    }

    /// <summary>
    /// Cloud Scrape Execution List DTO
    /// </summary>
    class CloudScrapeExecutionListDTO
    {
        /// <summary>
        /// off set
        /// </summary>
        public int offset;

        /// <summary>
        /// total rows
        /// </summary>
        public int totalRows;

        /// <summary>
        /// array of rows
        /// </summary>
        public CloudScrapeExecutionDTO[] rows;
    }

    /// <summary>
    /// Cloud Scrape Result DTO
    /// </summary>
    class CloudScrapeResultDTO
    {
        /**
         * Header fields
         * @var string[]
         */
        public string[] headers;

        /**
         * An array of arrays containing each row - with each value inside it.
         * @var mixed[][]
         */
        public string[][] rows;

        /**
         * Total number of rows available
         * @var int
         */
        public int totalRows;
    }

    /// <summary>
    /// Cloud Scrape File DTO
    /// </summary>
    class CloudScrapeFileDTO
    {
        /// <summary>
        /// The type of file
        /// </summary>
        public string mimeType;

        /// <summary>
        /// The contents of the file
        /// </summary>
        public string contents;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mimeType"></param>
        /// <param name="contents"></param>
        public CloudScrapeFileDTO(string mimeType, string contents)
        {
            this.mimeType = mimeType;
            this.contents = contents;
        }


    }

    /// <summary>
    /// Cloud Scrape Run DTO
    /// </summary>
    class CloudScrapeRunDTO
    {
        /// <summary>
        /// The ID of the run
        /// </summary>
        public string _id;

        /// <summary>
        /// Name of the run
        /// </summary>
        public string name;
    }
}
