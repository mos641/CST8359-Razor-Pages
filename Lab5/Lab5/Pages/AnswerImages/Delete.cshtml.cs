using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lab5.Data;
using Lab5.Models;
using Azure.Storage.Blobs;
using Azure;
using System.Diagnostics;

namespace Lab5.Pages.AnswerImages
{
    public class DeleteModel : PageModel
    {
        private readonly Lab5.Data.AnswerImageDataContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string earthContainerName = "earthimages";
        private readonly string computerContainerName = "computerimages";

        public DeleteModel(Lab5.Data.AnswerImageDataContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        [BindProperty]
        public AnswerImage AnswerImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.AnswersImages == null)
            {
                return NotFound();
            }

            var answerimage = await _context.AnswersImages.FirstOrDefaultAsync(m => m.AnswerImageId == id);

            if (answerimage == null)
            {
                return NotFound();
            }
            else
            {
                AnswerImage = answerimage;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.AnswersImages == null)
            {
                return NotFound();
            }
            var answerimage = await _context.AnswersImages.FindAsync(id);

            if (answerimage != null)
            {
                String container;
                if (answerimage.Question == Question.Earth)
                {
                    container = earthContainerName;
                }
                else if (answerimage.Question == Question.Computer)
                {
                    container = computerContainerName;
                }
                else
                {
                    Debug.WriteLine("No Question set");
                    // throw new Exception("No Question set");
                    return RedirectToPage("/Error", "No Question set");
                }

                BlobContainerClient containerClient;
                try
                {
                    // get the container and store container object
                    containerClient = _blobServiceClient.GetBlobContainerClient(container);
                }
                catch (RequestFailedException)
                {
                    // container was not found
                    Debug.WriteLine("Container does not exist");
                    // throw new Exception("Container does not exist");
                    return RedirectToPage("/Error", "Container does not exist");
                }

                try
                {
                    await containerClient.DeleteBlobAsync(answerimage.FileName);
                } catch (RequestFailedException)
                {
                    Debug.WriteLine("Specified blob does not exist");
                    // throw new Exception("Specified blob does not exist");
                    return RedirectToPage("/Error", "Specified blob does not exist " + answerimage.FileName, answerimage.Question );
                }

                AnswerImage = answerimage;
                _context.AnswersImages.Remove(AnswerImage);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
