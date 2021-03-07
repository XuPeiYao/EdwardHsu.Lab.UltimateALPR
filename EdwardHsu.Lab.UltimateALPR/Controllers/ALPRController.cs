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
using System.Threading.Tasks;

namespace EdwardHsu.Lab.UltimateALPR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ALPRController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Go(IFormFile image, [FromServices] IWebHostEnvironment env)
        {
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
            Action<string> cliHandle = line =>
            {
                if (line?.StartsWith(tag) != true)
                {
                    return;
                }
                line = line.Replace(tag, string.Empty);
                result = line;
            };

            var cmd = Cli.Wrap(@"/alpr/ultimateALPR-SDK-master/binaries/linux/x86_64/recognizer")
                .WithArguments(args => args
                    .Add(@"--image " + System.IO.Path.Combine(env.ContentRootPath, filename), false)
                    .Add(@"--assets /alpr/ultimateALPR-SDK-master/assets", false)
                )
                .WithWorkingDirectory("/alpr/ultimateALPR-SDK-master/binaries/linux/x86_64")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(cliHandle))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(cliHandle))
                .WithValidation(CommandResultValidation.None);

            await cmd.ExecuteAsync();

            System.IO.File.Delete(filename);

            return Content(result, "application/json");
        }
    }
}
