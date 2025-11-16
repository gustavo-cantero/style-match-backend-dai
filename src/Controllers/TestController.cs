using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;
using StyleMatch.Services;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController(ConfigurationModel config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var res = await OutfitService.GenerateOutfitAsync(
            [
            @"D:\Git temp\style-match-backend-dai\src\Garment\019a328a-18c7-768d-8ed3-a22b01cf011e",
            @"C:\Users\g.cantero\Desktop\TP\D_NQ_NP_2X_657808-MLA84349797939_052025-F.webp"
            ],
            config.OpenAIKey
            );

        return File(res, "image/png");
    }
}
