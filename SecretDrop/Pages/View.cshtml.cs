using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Pages
{
    public class ViewModel : PageModel
    {
        private readonly ISecretFileStore _store;
        public ViewModel(ISecretFileStore store) => _store = store;

        public void OnGet(string id) { }

        public IActionResult OnGetMeta(string id)
        {
            var meta = _store.GetMeta(id);
            if (meta == null) return NotFound();

            return new JsonResult(meta);
        }

        public IActionResult OnGetDownload(string id)
        {
            var fs = _store.GetFileStream(id);
            if (fs == null) return NotFound();
            return File(fs, "application/octet-stream");
        }
    }
}
