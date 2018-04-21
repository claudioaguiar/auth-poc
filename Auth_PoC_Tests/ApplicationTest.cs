using Auth_PoC;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Tests
{
    [TestFixture]
    public class ApplicationTest
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;

        public ApplicationTest()
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(@"../../.."))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>()
                .UseConfiguration(configuration));
            _client = _server.CreateClient();
            _configuration = configuration;
        }

        [Test]
        public async Task UnAuthorizedAccess()
        {
            // No bearer token
            var response = await _client.GetAsync("/api/animals");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Test]
        public async Task AccessForbidden()
        {
            // No "policy.api" in "perms" claim
            var token = GetJwtToken(null);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/animals");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(requestMessage);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Test]
        public async Task GetFullAnimalList()
        {
            // Token with "policy.api" and "policy.api.animals.read"
            var token = GetJwtToken(
                new System.Collections.Generic.List<Claim> { new Claim("perms", "policy.api"),
                                                             new Claim("perms", "policy.api.animals.read") }
            );
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/animals");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(requestMessage);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JArray.Parse(responseString);
            Assert.AreEqual(true, responseJson.Count == 6);
        }

        [Test]
        public async Task GetPartialAnimalList()
        {
            // Token with "policy.api" only
            var token = GetJwtToken(new System.Collections.Generic.List<Claim> { new Claim("perms", "policy.api") });
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/animals");
            requestMessage.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(requestMessage);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JArray.Parse(responseString);
            Assert.AreEqual(true, responseJson.Count == 3);
        }

        private string GetJwtToken(System.Collections.Generic.List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                                 _configuration["Jwt:Audience"], claims, null,
                                 DateTime.Now.AddMinutes(30),
                                 new SigningCredentials(key,
                                                        SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}