using Microsoft.AspNetCore.Mvc;
using FhirConverterApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Liquid.Converter.Tool;

namespace FhirConverterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConverterController : ControllerBase
    {
        [HttpPost("convert")]
        public IActionResult Convert([FromBody] FhirApiModel options)
        {
            try
            {
                string output = ApiLogicMapper.Convert(options);
                return Content(output, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Conversion failed: {ex.Message}");
            }
        }
    }
}
