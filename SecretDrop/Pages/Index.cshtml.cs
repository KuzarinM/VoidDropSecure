using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecretDrop.Models;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Pages
{
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ISecretFileStore _store;
        public IndexModel(ISecretFileStore store) => _store = store;

        public void OnGet() { }

        // 1. Старт сессии загрузки
        public IActionResult OnPostInit([FromBody] InitRequest req)
        {
            // ВАЖНО: Передаем req.PasswordSalt и req.WrappedKey в сервис!
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
