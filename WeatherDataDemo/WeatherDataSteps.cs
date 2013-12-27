using System;
using System.Collections.Generic;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NUnit.Framework;
using TechTalk.SpecFlow;
using AlteryxGalleryAPIWrapper;

namespace WeatherDataDemo
{
    [Binding]
    public class WeatherDataSteps
    {
        private string alteryxurl;
        private string _sessionid;
        private string _appid;
        private string _userid;
        private string _appName;
        private string jobid;
        private string outputid;
        private string validationId;

        private Client Obj = new Client("https://devgallery.alteryx.com/api");

        private RootObject jsString = new RootObject();

        [Given(@"alteryx running at""(.*)""")]
        public void GivenAlteryxRunningAt(string url)
        {
            alteryxurl = url;
        }

        [Given(@"I am logged in using ""(.*)"" and ""(.*)""")]
        public void GivenIAmLoggedInUsingAnd(string user, string password)
        {
            // Authenticate User and Retreive Session ID
            _sessionid = Obj.Authenticate(user, password).sessionId;
        }

        [Given(@"I publish the application ""(.*)""")]
        public void GivenIPublishTheApplication(string p0)
        {
            //Publish the app & get the ID of the app
            string apppath = @"..\..\docs\Download_Weather_Data.yxzp";
            Action<long> progress = new Action<long>(Console.Write);
            var pubResult = Obj.SendAppAndGetId(apppath, progress);
            _appid = pubResult.id;
            validationId = pubResult.validation.validationId;
            ScenarioContext.Current.Set(Obj, System.Guid.NewGuid().ToString());
        }

        [Given(@"I check if the application is ""(.*)""")]
        public void GivenICheckIfTheApplicationIs(string status)
        {
            // validate a published app can be run 
            // two step process. First, GetValidationStatus which indicates when validation disposition is available. 
            // Second, GetValidation, which gives actual status Valid, UnValid, etc.

            String validStatus = "";
            while (validStatus != "Completed")
            {
                var validationStatus = Obj.GetValidationStatus(validationId); // url/api/apps/jobs/{VALIDATIONID}/
                validStatus = validationStatus.status;
                string disposition = validationStatus.disposition;
            }
            var finalValidation = Obj.GetValidation(_appid, validationId);
                // url/api/apps/{APPID}/validation/{VALIDATIONID}/
            var finaldispostion = finalValidation.validation.disposition;
            StringAssert.IsMatch(status, finaldispostion.ToString());
        }
        
        [When(@"I run weather data app by choosing the location ""(.*)"" and select the date ""(.*)""")]
        public void WhenIRunWeatherDataAppByChoosingTheLocationAndSelectTheDate(string location, string date)           
        {
            //url + "/apps/studio/?search=" + appName + "&limit=20&offset=0"
            //Search for App & Get AppId & userId 

            string response = Obj.SearchApps("weather");
            var appresponse =
                new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                    response);
            int count = appresponse["recordCount"];
            //for (int i = 0; i <= count - 1; i++)
            //{
            //  _appid = appresponse["records"][0]["id"];
            _userid = appresponse["records"][0]["owner"]["id"];
            _appName = appresponse["records"][0]["primaryApplication"]["fileName"];
            //}       
            //jsString.appPackage.id = _appid;
            jsString.appPackage.id = "52bd508d0b58150c6008d4a9";
            jsString.userId = _userid;
            jsString.appName = _appName;

            //url +"/apps/" + appPackageId + "/interface/
            //Get the app interface - not required
            string appinterface = Obj.GetAppInterface(_appid);
            dynamic interfaceresp = JsonConvert.DeserializeObject(appinterface);
            string unrestrictapp = Obj.UnRestrictApp(appinterface, _appid);

            //Construct the payload to be posted.
            string header = String.Empty;
            List<JsonPayload.Question> questionAnsls = new List<JsonPayload.Question>();
            questionAnsls.Add(new JsonPayload.Question("Date", date));
            var solve = new List<JsonPayload.datac>();
            solve.Add(new JsonPayload.datac() {key = location, value = "true"});
            string SolveFor = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(solve);
                      
            JsonPayload.Question questionAns = new JsonPayload.Question();
            questionAns.name = "Location";
            questionAns.answer = SolveFor;
            jsString.questions.Add(questionAns);
                                     
            jsString.jobName = "Job Name";

            jsString.questions.AddRange(questionAnsls);
          
            var postData = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(jsString);
            string postdata = postData.ToString();


            string resjobqueue = Obj.QueueJob(postdata);
            var jobqueue =
                new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                    resjobqueue);
            jobid = jobqueue["id"];

            //Get the job status

            string status = "";
            while (status != "Completed")
            {
                string jobstatusresp = Obj.GetJobStatus(jobid);
                var statusresp =
                    new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                        jobstatusresp);
                status = statusresp["status"];
            }
        }

        [Then(@"I see the weather data app has the text message (.*)")]
        public void ThenISeeTheWeatherDataAppHasTheTextMessage(string result)
        {
            //url + "/apps/jobs/" + jobId + "/output/"
            string getmetadata = Obj.GetOutputMetadata(jobid);
            dynamic metadataresp = JsonConvert.DeserializeObject(getmetadata);

            // outputid = metadataresp[0]["id"];
            int count = metadataresp.Count;
            for (int j = 0; j <= count - 1; j++)
            {
                outputid = metadataresp[j]["id"];
            }

            string getjoboutput = Obj.GetJobOutput(jobid, outputid, "html");
            string htmlresponse = getjoboutput;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlresponse);
            string output = doc.DocumentNode.SelectSingleNode("//div[@class='DefaultText']").InnerText;
            
            StringAssert.Contains(result, output);
        }

        [AfterScenario()]
        public void AfterScenario()
        {
            try
            {
                if (ScenarioContext.Current.Count > 0)
                {
                    foreach (var item in ScenarioContext.Current)
                    {
                        Obj.Dispose();                       
                    }
                }
                else
                {
                    throw new Exception("No Object found");
                }

            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
