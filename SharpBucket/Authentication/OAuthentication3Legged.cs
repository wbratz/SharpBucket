﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RestSharp;
using RestSharp.Authenticators;

namespace SharpBucket.Authentication
{
    /// <summary>
    /// This class helps you authenticated with the BitBucket REST API via the 3 legged OAuth authentication.
    /// </summary>
    [Obsolete("Use OAuth1ThreeLeggedAuthentication instead")]
    public sealed class OAuthentication3Legged : OauthAuthentication
    {
        private string OAuthToken;
        private string OauthTokenSecret;
        private const string requestUrl = "oauth/request_token";
        private const string userAuthorizeUrl = "oauth/authenticate";
        private const string accessUrl = "oauth/access_token";
        private readonly string callback;

        public OAuthentication3Legged(string consumerKey, string consumerSecret, string callback, string baseUrl)
            : base(consumerKey, consumerSecret, baseUrl)
        {
            this.callback = callback;
        }

        public OAuthentication3Legged(string consumerKey, string consumerSecret, string oAuthToken, string oauthTokenSecret, string baseUrl)
            : base(consumerKey, consumerSecret, baseUrl)
        {
            OAuthToken = oAuthToken;
            OauthTokenSecret = oauthTokenSecret;
        }

        public override string GetResponse(string url, Method method, object body, IDictionary<string, object> requestParameters)
        {
            this.EnsureClientIsBuild();
            return base.GetResponse(url, method, body, requestParameters);
        }

        public override async Task<string> GetResponseAsync(string url, Method method, object body, IDictionary<string, object> requestParameters, CancellationToken token)
        {
            this.EnsureClientIsBuild();
            return await base.GetResponseAsync(url, method, body, requestParameters, token);
        }

        public override T GetResponse<T>(string url, Method method, object body, IDictionary<string, object> requestParameters)
        {
            this.EnsureClientIsBuild();
            return base.GetResponse<T>(url, method, body, requestParameters);
        }

        public override async Task<T> GetResponseAsync<T>(string url, Method method, object body, IDictionary<string, object> requestParameters, CancellationToken token)
        {
            this.EnsureClientIsBuild();
            return await base.GetResponseAsync<T>(url, method, body, requestParameters, token);
        }

        private void EnsureClientIsBuild()
        {
            if (Client == null)
            {
                Client = new RestClient(_baseUrl)
                {
                    Authenticator = OAuth1Authenticator.ForProtectedResource(ConsumerKey, ConsumerSecret, OAuthToken, OauthTokenSecret)
                };
            }
        }

        /// <summary>
        /// Sets the authentication tokens.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <exception cref="System.Net.WebException">REST client encountered an error:  + response.ErrorMessage</exception>
        private void SetAuthTokens(IRestClient client, string method)
        {
            var request = new RestRequest(method, Method.POST);
            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                throw new WebException("REST client encountered an error: " + response.ErrorMessage, response.ErrorException);
            }

            var qs = HttpUtility.ParseQueryString(response.Content);
            OAuthToken = qs["oauth_token"];
            OauthTokenSecret = qs["oauth_token_secret"];
        }

        /// <summary>
        /// Start the OAuth authentication process.
        /// The method returns the the URL where the user can authorize your application to act on his/her behalf.
        /// More info:
        /// https://confluence.atlassian.com/display/BITBUCKET/OAuth+on+Bitbucket#OAuthonBitbucket-Step3.RedirecttheusertoBitbuckettoauthorizeyourapplication
        /// </summary>
        /// <returns></returns>
        public string StartAuthentication()
        {
            var restClient = new RestClient(_baseUrl)
            {
                Authenticator = OAuth1Authenticator.ForRequestToken(ConsumerKey, ConsumerSecret, callback)
            };

            SetAuthTokens(restClient, requestUrl);

            Contract.Assert(
                !String.IsNullOrWhiteSpace(OAuthToken) &&
                !String.IsNullOrWhiteSpace(OauthTokenSecret));

            var request = new RestRequest(userAuthorizeUrl);
            request.AddParameter("oauth_token", OAuthToken);

            return restClient.BuildUri(request).ToString();
        }

        /// <summary>
        /// The method is used to obtain the credentials that let you access resources on BitBucket.
        /// More info:
        /// https://confluence.atlassian.com/display/BITBUCKET/OAuth+on+Bitbucket#OAuthonBitbucket-Step4.RequestanAccessToken
        /// </summary>
        /// <param name="pin">The pin / verifier that was obtained in the previous step.</param>
        public void AuthenticateWithPin(string pin)
        {
            var restClient = new RestClient(_baseUrl)
            {
                Authenticator = OAuth1Authenticator.ForAccessToken(ConsumerKey, ConsumerSecret, OAuthToken, OauthTokenSecret, pin)
            };

            SetAuthTokens(restClient, accessUrl);

            Contract.Assert(
                !String.IsNullOrWhiteSpace(OAuthToken) &&
                !String.IsNullOrWhiteSpace(OauthTokenSecret));
        }
    }
}