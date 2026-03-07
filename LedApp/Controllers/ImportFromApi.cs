using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace LedApp.Controllers
{
    public class ImportFromApi : Controller
    {
        
        
        Uri baseAddress = new Uri("https://fakestoreapi.com/");
        private readonly HttpClient _httpClient;
        public ImportFromApi()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = baseAddress;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
           // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-Authorization", "sk_test_8146250gNZ8gddde480e07ac91c10c2651077176aed27");

            // Acquire the access token.
          //  string[] scopes = new string[] { "user.read" };
            //string authorizationHeader = IAuthorizationHeaderProvider.GetAuthorizationHeaderForUserAsync(scopes);
            //string data = "";
            dynamic datacv;
            HttpResponseMessage response = _httpClient.GetAsync(_httpClient.BaseAddress+ "products").Result;
            if (response.IsSuccessStatusCode )
            {
               var data = await response.Content.ReadAsStringAsync();
                datacv = JsonConvert.DeserializeObject(data);
                foreach (var item in datacv)
                {
                    var s = item.title;
                    ViewBag.S = s;
                }
                
                ViewBag.Data = datacv;
            }
            
            return View();
        }
    }
}
