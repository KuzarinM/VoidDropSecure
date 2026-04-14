using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SecretDrop.Configurations;
using SecretDrop.Models;
using SecretDrop.Services.Implementations;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Pages
{
    [IgnoreAntiforgeryToken]
    public class RequestModel : PageModel
    {
        private readonly IDropSessionStore _dropStore;
        private readonly ISecretFileStore _fileStore;
        private readonly ICaptchaService _captchaService;
        private readonly AppOptions _appOptions;

        public RequestModel(IDropSessionStore dropStore, ISecretFileStore fileStore, IOptions<AppOptions> options, ICaptchaService captchaService)
        {
            _dropStore = dropStore;
            _fileStore = fileStore;
            _appOptions = options.Value;
            _captchaService = captchaService;
        }

        // Данные для View
        public string? SessionId { get; set; }
        public string? ReceiverPublicKey { get; set; }
        public bool IsSenderMode => !string.IsNullOrEmpty(SessionId);

        public string PublickKey => _appOptions.CaptchaSiteKey;
        public bool IsCaptchaEnabled => _appOptions.UseCaptcha;
        public IActionResult OnGet(string? id)
        {
            if (string.IsNullOrEmpty(id)) return Page(); // Mode: Creator

            // Mode: Sender
            var session = _dropStore.GetSession(id);
            if (session == null || session.IsCompleted) return RedirectToPage("/Error");

            SessionId = id;
            ReceiverPublicKey = session.PublicKeyJson;
            return Page();
        }

        // 1. Creator создает сессию
        public async Task<IActionResult> OnPostCreateSession([FromBody] CreateSessionRequest req)
        {
            // 1. Валидация капчи
            if (!await _captchaService.VerifyToken(req.CaptchaToken ?? ""))
            {
                return BadRequest("Captcha validation failed.");
            }

            // 2. Создание сессии (используем req.PublicKey вместо publicKey)
            var id = _dropStore.CreateSession(req.PublicKey);
            return new JsonResult(new { id });
        }

        // 2. Creator поллит статус
        public IActionResult OnGetCheck(string id)
        {
            var session = _dropStore.GetSession(id);
            if (session == null) return NotFound();
            if (!session.IsCompleted) return new StatusCodeResult(202); // 202 Accepted (Processing)

            return new JsonResult(new
            {
                fileId = session.FileId,
                encryptedKey = session.EncryptedFileKey
            });
        }

        // 3. Sender загружает файл (Init -> Chunk -> Complete)
        // Init и Chunk используем стандартные из Index.cshtml (или дублируем логику), 
        // но здесь нам нужен метод завершения "Сделки".

        public IActionResult OnPostCompleteSession([FromBody] CompleteRequest req)
        {
            _dropStore.CompleteSession(req.SessionId, req.FileId, req.EncryptedKey, req.MetaJson);
            return new OkResult();
        }
    }
}
