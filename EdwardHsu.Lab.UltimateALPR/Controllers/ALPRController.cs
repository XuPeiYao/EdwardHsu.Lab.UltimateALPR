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
using Microsoft.Extensions.Logging.EventSource;

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


            var tag = "*[ULTALPR_SDK INFO]: result: ";
            string result = null;
            var temp = "";
            Action<string> cliHandle = line =>
            {

                result += line + "\n";
            };


            var cmd = Cli.Wrap(System.IO.Path.Combine(env.ContentRootPath, @"openalpr_64\alpr.exe"))
                .WithArguments(args =>
                {
                    var argBuilder = args
                                     .Add(System.IO.Path.Combine(env.ContentRootPath, filename), false);

                })
                .WithWorkingDirectory(env.ContentRootPath)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(cliHandle))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(cliHandle))
                .WithValidation(CommandResultValidation.None);

            await cmd.ExecuteAsync();

            System.IO.File.Delete(filename);


            if (string.IsNullOrWhiteSpace((result)))
            {
                Console.WriteLine("CLI Error");
                return NotFound();
            }

            return Content(Convert(OpenALPR(result)), "application/json");
        }

        string OpenALPR(string alprStr)
        {
            var mline = alprStr.Replace("\r", "").Split("\n").Skip(1);

            Regex r = new(@"\W+-\W(?<code>[^ \t]+)\W+");

            mline = mline.Where(x => r.IsMatch(x)).ToList();

            List<string> codes = new();
            foreach (var line in mline)
            {
                codes.Add(r.Match(line).Groups["code"].Value);
            }

            var obj = new Root()
            {
                duration = 0,
                frame_id = 0,
                plates = codes.Select(x => new Plate()
                {
                    car = new Car()
                    {
                        confidence = 0,
                        warpedBox = new List<double>()
                        {
                            0, 0, 0, 0, 0, 0, 0, 0
                        }
                    },
                    confidences = new List<double>()
                    {
                        0, 0, 0, 0, 0, 0, 0, 0
                    },
                    text = x,
                    warpedBox = new List<double>()
                    {
                        0, 0, 0, 0, 0, 0, 0, 0
                    }
                }).ToList()
            };

            return System.Text.Json.JsonSerializer.Serialize(obj);
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

        private static readonly Regex r1 = new(@"^(?<prefix>[A-Z]{2,3})(?<suffix>[I\d]{3,4})$");
        private static readonly Regex r2 = new(@"^(?<prefix>[\d]{2,4})(?<suffix>([A-Z]{2}|E2|FV|H2))$");

        private static string Convert(string jsonString)
        {
            Root data = System.Text.Json.JsonSerializer.Deserialize<Root>(jsonString);

            if (data.plates.Count == 0)
            {
                return null;
            }

            var result = data.plates.Select(plate =>
            {
                var text = plate.text;

                if (r1.IsMatch(text))
                {
                    var match = r1.Match(text);
                    var prefix = match.Groups["prefix"].Value;
                    var suffix = match.Groups["suffix"].Value.Replace("I", "1");
                    return $"{prefix}-{suffix}";
                }

                if (r2.IsMatch(text))
                {
                    var match = r2.Match(text);
                    var prefix = match.Groups["prefix"].Value;
                    var suffix = match.Groups["suffix"].Value;
                    return $"{prefix}-{suffix}";
                }

                return text;
            }).ToList();

            if (result.Count < 3)
            {
                result.AddRange(Enumerable.Range(0, 3 - result.Count).Select(x => string.Empty));
            }

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                result
            });
        }
    }
}
