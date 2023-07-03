using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MT.Models;
using MT.Services;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Data;

namespace MT.Controllers
{
    public class HomeController : Controller
    {
        // Path where Excel doucment is temporarily stored
        private static string filePath = ".\\wwwroot\\Uploads\\Test.xlsx";
        private IExcelService service;
        List<Table> tablez = new List<Table>();

        public HomeController()
        {
            var MainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
            if (Directory.Exists(MainPath))
            {
                Directory.Delete(MainPath, true);
            }
        }

        // Get index view
        [Authorize]
        [HttpGet]
        public IActionResult Index(IFormCollection form)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcelFile(IFormFile FormFile)
        {

            //get file name
            var filename = ContentDispositionHeaderValue.Parse(FormFile.ContentDisposition).FileName.Trim('"');

            //get path
            var MainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");

            if (Directory.Exists(MainPath))
            {
                Directory.Delete(MainPath, true);
            }

            var filePath = Path.Combine(MainPath, FormFile.FileName);

            string extension = Path.GetExtension(filename);

            string conString = string.Empty;

            // Get extension
            switch (extension)
            {
                case ".xls": //Excel 97-03.
                    conString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties='Excel 8.0;HDR=YES'";
                    break;
                case ".xlsx": //Excel 07 and above.
                    conString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties='Excel 8.0;HDR=YES'";
                    break;
            }

            // Check extension
            if (extension != ".xlsx")
            {
                ViewBag.Message = "Uploaded file is not an xlsx document";
            }
            else
            {
                ViewBag.Message = "File uploaded";

            }

            // Create directory "Uploads" if it doesn't exists
            if (!Directory.Exists(MainPath))
            {
                Directory.CreateDirectory(MainPath);
            }

            // Get file path 
            filePath = Path.Combine(MainPath, "Test.xlsx");

         
            using (System.IO.Stream stream = new FileStream(filePath, FileMode.Create))
            {
                await FormFile.CopyToAsync(stream);
            }

            // Intantiate Excel Service and get tables from Excel
            service = new ExcelService();
            tablez = service.FFinalTables;
            
            // Store tables as temporary data to have them in the user's session state
            TempData.Put("tables", tablez);
            TempData.Keep("tables");

            return View("Index");
        }

        // Get table view
        [Authorize]
        [HttpGet]
        public IActionResult TableView(IFormCollection form)
        {
            TempData.Keep("tables");
            List<Table> tables = new List<Table>();
            tables = TempData.Get<List<Table>>("tables");

            return View(tables); 
        }
       
        // Insert tables to database
        [HttpPost]
        public IActionResult TableView(IFormFile FormFile)
        {
            var tablez = TempData.Get<List<Table>>("tables");
            
            try
            {
                foreach (Table table in tablez)
                {
                    var transformator = new DbService(table);         
                    transformator.CreateDb("Test");
                    transformator.CreateTable();
                    transformator.TableInsert();
                    ViewBag.Message = "Success";
                }
            }
            catch
            {
                ViewBag.Message = "Error";
            }

            return View(tablez);
        }

        // Get preview
        [HttpGet]
        public IActionResult Preview(string name)
        {           
            var tables = TempData.Get<List<Table>>("tables");
            Table tablez = tables.Where(t => t.tableName == name).First();

            return View(tablez);
        }

        // Get Edit view
        [HttpGet]
        public IActionResult Edit(string name)
        {
            var tables = TempData.Get<List<Table>>("tables");
            Table tablez = tables.Where(t => t.tableName == name).First();

            return View(tablez);
        }

        // Set a new name for a table
        [HttpPost]
        public IActionResult EditTable(string name, string old)
        {
            var tables = TempData.Get<List<Table>>("tables");
            tables.Where(t => t.tableName == old).First().tableName = name;

            TempData.Put("tables", tables);
            TempData.Keep("tables");


            return View("TableView", tables);
        }

        // Delete a table from the preview 
        [HttpPost]
        public bool Delete(string name)
        {
            try
            {
                var tables = TempData.Get<List<Table>>("tables");
                tables.Remove(tables.Where(t => t.tableName == name).First());
                TempData.Put("tables", tables);

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        // Export data model to a JSON file
        [HttpGet]
        public IActionResult Export()
        {
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(TempData.Get<List<Table>>("tables"), Formatting.Indented);

            string fileName = "jsonExport.json";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

            var content = new System.IO.MemoryStream(bytes);
            return File(content, "application/json", fileName);
        }
    }


    // Helper class to store and retrieve temporary data
    public static class TempDataExtensions
    {
        public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class
        {
            tempData[key] = JsonConvert.SerializeObject(value);
        }

        public static T Get<T>(this ITempDataDictionary tempData, string key) where T : class
        {
            object o;
            tempData.TryGetValue(key, out o);
            return o == null ? null : JsonConvert.DeserializeObject<T>((string)o);
        }
    }
}