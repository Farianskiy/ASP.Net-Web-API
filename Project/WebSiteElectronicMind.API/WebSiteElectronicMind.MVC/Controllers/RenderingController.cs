using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSiteElectronicMind.MVC.Models.RenderingToPDF;

namespace WebSiteElectronicMind.MVC.Controllers
{
    public class RenderingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RenderingController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult GeneratorPDF()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePdf()
        {
            var client = _httpClientFactory.CreateClient();

            // Сбор данных из формы
            var request = new TableFull.RenderingRequest
            {
                Shield = new TableFull.InfoShield
                {
                    NameShield = Request.Form["Shield.NameShield"],
                    FullNameShield = Request.Form["Shield.FullNameShield"],
                    TypeShield = Request.Form["Shield.TypeShield"]
                },
                Electrical = new TableFull.InfoElectrical
                {
                    NominalVoltage = int.TryParse(Request.Form["Electrical.NominalVoltage"], out var nominalVoltage) ? nominalVoltage : 0,
                    NominalShield = int.TryParse(Request.Form["Electrical.NominalShield"], out var nominalShield) ? nominalShield : 0,
                    TypeGrounding = Request.Form["Electrical.TypeGrounding"]
                },
                Cable = new TableFull.InfoCable
                {
                    SupplyCable = Request.Form["Cable.SupplyCable"],
                    CableOL = Request.Form["Cable.CableOL"]
                },
                DegreeProtection = Request.Form["DegreeProtection"],
                Omentum = new TableFull.InfoOmentum
                {
                    TypeOmentum = Request.Form["Omentum.TypeOmentum"],
                    QuantityOmentum = Request.Form["Omentum.QuantityOmentum"],
                    TypeOmentumOL = Request.Form["Omentum.TypeOmentumOL"],
                    QuantityOmentumOL = Request.Form["Omentum.QuantityOmentumOL"]
                },
                PowerCable = Request.Form["PowerCable"],
                Comment = Request.Form["Comment"],
                Build = new TableFull.InfoBuild
                {
                    FullNameEngineer = Request.Form["Build.FullNameEngineer"],
                    NumberOrderCustomer = int.TryParse(Request.Form["Build.NumberOrderCustomer"], out var numberOrderCustomer) ? numberOrderCustomer : 0,
                    NumberBuild = Request.Form["Build.NumberBuild"]
                },
                RenderingTableList = new List<TableFull.RenderingTable>()
            };

            // Сбор динамических данных RenderingTableList
            int i = 0;
            while (Request.Form[$"RenderingTable[{i}].Name"].Any())
            {
                request.RenderingTableList.Add(new TableFull.RenderingTable
                {
                    Name = Request.Form[$"RenderingTable[{i}].Name"],
                    NameOfScheme = Request.Form[$"RenderingTable[{i}].NameOfScheme"],
                    Type = Request.Form[$"RenderingTable[{i}].Type"],
                    NumberingLetter = Request.Form[$"RenderingTable[{i}].NumberingLetter"],
                    NumberingDigit = Request.Form[$"RenderingTable[{i}].NumberingDigit"],
                    Phase = Request.Form[$"RenderingTable[{i}].Phase"],
                    Level = int.TryParse(Request.Form[$"RenderingTable[{i}].Level"], out var level) ? level : 0,
                });
                i++;
            }

            // Конвертация запроса в JSON
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Отправка данных на API
            var apiUrl = "http://localhost:7014/RenderingControllers/generator-Pdf";
            var response = await client.PostAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Message = "Error: " + errorMessage });
            }

            var result = await response.Content.ReadAsAsync<dynamic>();

            string filePath = result.path;

            var downloadResponse = await client.GetAsync($"http://localhost:7014/Position/download-files?filePath={Uri.EscapeDataString(filePath)}");

            if (!downloadResponse.IsSuccessStatusCode)
            {
                var errorMessage = await downloadResponse.Content.ReadAsStringAsync();
                return StatusCode((int)downloadResponse.StatusCode, new { Message = "Error: " + errorMessage });
            }

            var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

            return File(fileBytes, "application/pdf", "output.pdf");
        }

    }
}
