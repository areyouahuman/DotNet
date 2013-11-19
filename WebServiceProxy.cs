using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Ayah.WebServiceIntegrationLibrary.Properties;

namespace Ayah.WebServiceIntegrationLibrary
{
    public static class WebServiceProxy
    {
        private const string GameScriptFormat = "<div id='AYAH'></div><script src='{0}'></script>";
        private const string PublisherUrlFormat = "https://{0}/ws/script/{1}";
        private const string ScoreGameUrlFormat = "https://{0}/ws/scoreGame";
        private const string ConversionIFrameMarkupFormat = 
            "<iframe style=\"border:none;\" height=\"0\" width=\"0\" src=\"http://{0}/ws/recordConversion/{1}\"></iframe>";
        private const string SessionSecretKey = "session_secret";
        private const string ScoringKey = "scoring_key";
        private const string InvalidAuthorizationResultFormat = "An invalid authorization result was returned from the Score Game service: {0}";
        private const string StatusCodeKey = "status_code";
        private const string HumanStatusCode = "1";
        private const string NoSessionSecretForConversionMessage = "No session secret set to record conversion.";

        /// <summary>
        /// Returns the markup for the PlayThru.
        /// </summary>
        /// <returns>The markup to include the div element and javascript for an AYAH game.</returns>
        public static string GetPublisherHTML()
        {
            string url = String.Format(WebServiceProxy.PublisherUrlFormat,
                Settings.Default.AyahWebServiceHost, HttpUtility.UrlEncode(Settings.Default.AyahPublisherKey));

            return String.Format(WebServiceProxy.GameScriptFormat, url);
        }

        /// <summary>
        /// Check whether the user is a human.
        /// </summary>
        /// <param name="sessionSecret">The ID of the game session to score.</param>
        /// <remarks>Wrapper for the Score Game web service.</remarks>
        /// <returns><c>true</c> if the game appears to have been completed by a human and <c>false</c> otherwise.</returns>
        public static bool ScoreResult(string sessionSecret)
        {
            bool result = false;
            if (!String.IsNullOrWhiteSpace(sessionSecret))
            {
                try
                {
                    NameValueCollection scoreResultParams = CreateScoreResultParameters(sessionSecret);
                    string decodedAuthResult = GetScoreResult(scoreResultParams);
                    result = ParseResult(decodedAuthResult);
                }
                catch (Exception exception)
                {
                    LogError(exception.ToString());
                    return false;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a collection to store parameters for the call to score a game.
        /// </summary>
        /// <param name="sessionSecret">The ID of the game session to score.</param>
        /// <returns>A <see cref="NameValueCollection"/> containing parameters for the Score Game web call.</returns>
        private static NameValueCollection CreateScoreResultParameters(string sessionSecret)
        {
            NameValueCollection scoreResultParams = new NameValueCollection();
            scoreResultParams.Add(WebServiceProxy.SessionSecretKey, sessionSecret);
            scoreResultParams.Add(WebServiceProxy.ScoringKey, Settings.Default.AyahScoringKey);
            return scoreResultParams;
        }

        /// <summary>
        /// Sends the given parameters to the Score Game web service and returns the result.
        /// </summary>
        /// <param name="scoreResultParams">The parameters to send to the Score Game web service.</param>
        /// <returns>The UTF-8 encoded authorization result string.</returns>
        private static string GetScoreResult(NameValueCollection scoreResultParams)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] authResult = webClient.UploadValues(GetScoreGameUrl(), scoreResultParams);
                return Encoding.UTF8.GetString(authResult, 0, authResult.Length);
            }
        }

        /// <summary>
        /// Gets the URL to the Score Game web service.
        /// </summary>
        /// <returns>The URL to the Score Game web service.</returns>
        private static string GetScoreGameUrl()
        {
            return String.Format(WebServiceProxy.ScoreGameUrlFormat, Settings.Default.AyahWebServiceHost);
        }

        /// <summary>
        /// Using the given authorization result from the Score Game web service, determines whether the player passed or not.
        /// </summary>
        /// <param name="authResult">The UTF-8 encoded string returned from the Score Game web service.</param>
        /// <returns><c>true</c> if the user was determined to be a human and <c>false</c> otherwise.</returns>
        private static bool ParseResult(string authResult)
        {
            Dictionary<string, object> statusCodeDictionary = null;
            try
            {
                statusCodeDictionary =
                   new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(authResult);
            }
            catch (Exception exception)
            {
                LogError(exception.ToString());
                return false;
            }

            if (!statusCodeDictionary.ContainsKey(WebServiceProxy.StatusCodeKey))
            {
                LogError(String.Format(WebServiceProxy.InvalidAuthorizationResultFormat, authResult));
                return false;
            }
                
            return statusCodeDictionary[WebServiceProxy.StatusCodeKey].ToString() == WebServiceProxy.HumanStatusCode;
        }

        /// <summary>
        /// Logs the given error message to the event log location configured for this machine.
        /// </summary>
        /// <param name="errorMessage">An error message to log to the event log.</param>
        private static void LogError(string errorMessage)
        {
            using (EventLog eventLog = new EventLog(Settings.Default.AyahErrorLog))
            {
                eventLog.Source = "AYAHWebServiceProxy";
                eventLog.WriteEntry(errorMessage, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Returns the markup to record a conversion for the given session secret.
        /// </summary>
        /// <param name="sessionSecret">The ID of the play session that converted to a registration.</param>
        /// <remarks>Called on the goal page.</remarks>
        /// <returns>IFrame markup that records a conversion using the session secret.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called when the session secret is null or whitespace.</exception>
        public static string RecordConversion(string sessionSecret)
        {
            if (!String.IsNullOrWhiteSpace(sessionSecret))
            {
                return String.Format(WebServiceProxy.ConversionIFrameMarkupFormat,
                    Settings.Default.AyahWebServiceHost, sessionSecret);
            }
            else
            {
                LogError(WebServiceProxy.NoSessionSecretForConversionMessage);
                return String.Empty;
            }
        }
    }
}
