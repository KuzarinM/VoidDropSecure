using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecretDrop.Models;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Pages
{
    [IgnoreAntiforgeryToken]
    public class RequestModel : PageModel
    {
        private readonly IDropSessionStore _dropStore;
        private readonly ISecretFileStore _fileStore;

        public RequestModel(IDropSessionStore dropStore, ISecretFileStore fileStore)
        {
            _dropStore = dropStore;
            _fileStore = fileStore;
        }

        // ƒанные дл€ View
        public string? SessionId { get; set; }
        public string? ReceiverPublicKey { get; set; }
        public bool IsSenderMode => !string.IsNullOrEmpty(SessionId);

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
        public IActionResult OnPostCreateSession([FromBody] string publicKey)
        {
            var id = _dropStore.CreateSession(publicKey);
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
        // но здесь нам нужен метод завершени€ "—делки".

        public IActionResult OnPostCompleteSession([FromBody] CompleteRequest req)
        {
            _dropStore.CompleteSession(req.SessionId, req.FileId, req.EncryptedKey, req.MetaJson);
            return new OkResult();
        }
    }
}
