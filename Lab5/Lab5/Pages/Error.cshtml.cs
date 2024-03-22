using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lab5.Pages
{
    public class ErrorModel : PageModel
    {
        public void OnGet(string errorMessage = null)
        {
            if (errorMessage != null)
            {
                ViewData["errorMessage"] = errorMessage;
            }

        }
    }
}
