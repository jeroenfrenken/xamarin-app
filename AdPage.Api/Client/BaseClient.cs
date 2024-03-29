using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AdPage.Api.Model;
using Newtonsoft.Json;
using Plugin.Settings;
using Plugin.Settings.Abstractions;
using RestSharp;

namespace AdPage.Api.Client
{
    public class BaseClient
    {
        private const string ApiSettings = "API_SETTINGS";
        
        private RestClient _client;

        private static ISettings AuthenticationSettings => CrossSettings.Current;
        
        private string ApiKey { get; set; }
        
        public BaseClient()
        {
            _client = new RestClient("https://app.fastpages.io/api");
            ApiKey = AuthenticationSettings.GetValueOrDefault(ApiSettings, string.Empty);
        }
        
        private void HandleException<T>(IRestResponse<T> result)
        {
            if (result.ErrorException != null)
            {
                throw new Exception(result.ErrorException.Message);
            }
            
            if (result.ErrorMessage != null)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        public void setApiKey(string apiKey)
        {
            ApiKey = apiKey;
            AuthenticationSettings.AddOrUpdateValue(ApiSettings, apiKey);
        }

        private Task<T> AsyncCall<T>(string url, Method method) where T : new()
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            var request = new RestRequest(url, method, DataFormat.Json);
            request.AddHeader("x-api-key", ApiKey);
            _client.ExecuteAsync<T>(request,
                response =>
                {
                    HandleException(response);
                    taskCompletionSource.SetResult(response.Data);
                });
            return taskCompletionSource.Task;
        }

        public Task<UserDto> GetUser()
        {
            return AsyncCall<UserDto>("/user", Method.GET);
        }

        public Task<UserWithTokenDto> AuthenticateUser(UserLoginDto loginDto)
        {
            var taskCompletionSource = new TaskCompletionSource<UserWithTokenDto>();
            var request = new RestRequest("/authentication/login", Method.POST, DataFormat.Json);
            request.AddParameter("application/json", JsonConvert.SerializeObject(loginDto), ParameterType.RequestBody);
            _client.ExecuteAsync<UserWithTokenDto>(request,
                response =>
                {
                    HandleException(response);
                    taskCompletionSource.SetResult(response.Data);
                });
            return taskCompletionSource.Task;
        }

        public Task<List<ProjectDto>> GetProjects()
        {
            return AsyncCall<List<ProjectDto>>("/project", Method.GET);
        }

        public Task<List<Dictionary<string, string>>> GetProjectLeads(string uuid)
        {
            return AsyncCall<List<Dictionary<string, string>>>($"/project/{uuid}/leads?format", Method.GET);
        }

        public Task<bool> DeleteLead(string projectUuid, string leadUuid)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            _client.AddDefaultHeader("x-api-key", ApiKey);
            _client.ExecuteAsync(new RestRequest($"/project/{projectUuid}/leads/{leadUuid}", Method.DELETE),
                response =>
                {
                    taskCompletionSource.SetResult(response.StatusCode == HttpStatusCode.OK);
                });
            return taskCompletionSource.Task;
        }
    }
}