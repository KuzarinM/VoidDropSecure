using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SecretDrop.Configurations;
using SecretDrop.Models;
using SecretDrop.Services.Implementations;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Pages
{
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ISecretFileStore _store;
        private readonly AppOptions _appOptions;
        private readonly ICaptchaService _captchaService;
        public IndexModel(ISecretFileStore store, IOptions<AppOptions> appOptions, ICaptchaService captchaService)
        {
            _store = store;
            _appOptions = appOptions.Value;
            _captchaService = captchaService;
        }

        public string PublickKey =>_appOptions.CaptchaSiteKey;
        public bool IsCaptchaEnabled => _appOptions.UseCaptcha;

        public void OnGet() { }

        // 1. Старт сессии загрузки
        public async Task<IActionResult> OnPostInit([FromBody] InitRequest req)
        {
            // Валидация капчи
            if (!await _captchaService.VerifyToken(req.CaptchaToken ?? ""))
            {
                return BadRequest("Captcha validation failed.");
            }

            var id = _store.InitUpload(req.Meta, req.Ttl, req.Limit, req.PasswordSalt, req.WrappedKey);
            return new JsonResult(new { id });
        }

        public async Task<IActionResult> OnPostChunkAsync(string id)
        {
            await _store.AppendDataAsync(id, Request.Body);
            return new OkResult();
        }
    }
}
