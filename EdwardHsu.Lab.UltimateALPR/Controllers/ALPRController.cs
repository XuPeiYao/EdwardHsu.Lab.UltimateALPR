using CliWrap;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EdwardHsu.Lab.UltimateALPR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ALPRController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Go(
            IFormFile image, [FromServices] IWebHostEnvironment env)
        {
            if (image == null)
            {
                return BadRequest();
            }

            using var stream = image.OpenReadStream();

            var imageExt = System.IO.Path.GetExtension(image.FileName).ToUpper();

            if (imageExt != ".JPG" || imageExt != ".JPEG")
            {

            }

            string filename = Guid.NewGuid() + System.IO.Path.GetExtension(image.FileName);
            using (var fileStream = System.IO.File.Create(filename))
            {
                await stream.CopyToAsync(fileStream);
            }


            var    tag    = "*[ULTALPR_SDK INFO]: result: ";
            string result = null;
            var    temp   = "";
            Action<string> cliHandle = line =>
            {
                temp += "\r\n" + line;

                if (line?.StartsWith(tag) != true)
                {
                    return;
                }
                line = line.Replace(tag, string.Empty);
                result = line;
            };

            var cmd = Cli.Wrap(System.IO.Path.Combine(env.ContentRootPath,@"Runtime/recognizer"))
                .WithArguments(args =>
                {
                    var argBuilder = args
                                     .Add(@"--image " + System.IO.Path.Combine(env.ContentRootPath, filename), false)
                                     .Add(@"--assets " + System.IO.Path.Combine(env.ContentRootPath, @"Models"), false);

                    var licenseTokenFile = System.IO.Path.Combine(env.ContentRootPath, "license.key");
                    if (System.IO.File.Exists(licenseTokenFile))
                    {
                        argBuilder.Add("--tokenfile " + licenseTokenFile);
                    }
                })
                .WithWorkingDirectory(env.ContentRootPath)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(cliHandle))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(cliHandle))
                .WithValidation(CommandResultValidation.None);

            await cmd.ExecuteAsync();

            System.IO.File.Delete(filename);


            if(string.IsNullOrWhiteSpace((result)))
            {
                return NotFound();
            }

            return Content(Convert(result), "application/json");
        }


        public class Car
        {
            public double confidence { get; set; }
            public List<double> warpedBox { get; set; }
        }
        public class Plate
        {
            public Car car { get; set; }
            public List<double> confidences { get; set; }
            public string text { get; set; }
            public List<double> warpedBox { get; set; }
        }
        public class Root
        {
            public int duration { get; set; }
            public int frame_id { get; set; }
            public List<Plate> plates { get; set; }
        }
        private string Convert(string jsonString)
        {
            Root data = System.Text.Json.JsonSerializer.Deserialize<Root>(jsonString);
            if (data.plates.Count == 0)
            {
                return null;
            }
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                result = data.plates.Select(plate => {
                    var text = plate.text;
                    var r1   = new Regex(@"^(?<prefix>[A-Z]{3})(?<suffix>[\d]{4})$");
                    if (r1.IsMatch(text))
                    {
                        var prefix = r1.Match(text).Groups["prefix"];
                        var suffix = r1.Match(text).Groups["suffix"];
                        return $"{prefix}-{suffix}";
                    }
                    var r2 = new Regex(@"^(?<prefix>[\d]{4})(?<suffix>[\d]{2})$");
                    if (r2.IsMatch(text))
                    {
                        var prefix = r2.Match(text).Groups["prefix"];
                        var suffix = r2.Match(text).Groups["suffix"];
                        return $"{prefix}-{suffix}";
                    }
                    return text;
                })
            });
        }




    }
}
