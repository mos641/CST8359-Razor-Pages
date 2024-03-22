using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Lab5.Data;
using Lab5.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Azure;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Lab5.Pages.AnswerImages
{
    public class CreateModel : PageModel
    {
        private readonly Lab5.Data.AnswerImageDataContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string earthContainerName = "earthimages";
        private readonly string computerContainerName = "computerimages";
        private readonly long _fileSizeLimit = 20000000;
        private readonly string[] permittedExtensions = { ".png", ".jpg", ".jpeg", ".gif" };

        public CreateModel(Lab5.Data.AnswerImageDataContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        public IActionResult OnGet()
        {
            return Page();
            
        }

        [BindProperty]
        public AnswerImage AnswerImage { get; set; }


        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            // perform validation checks
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                String message = "File extension must be one of ";
                foreach(string extension in permittedExtensions)
                {
                    message += extension + " ";
                }
                Debug.WriteLine(message);
                //throw new Exception(message);
                return RedirectToPage("./Error", message);
                // return RedirectToPage("../Error");
            }

            if (file.Length > _fileSizeLimit || file.Length == 0)
            {
                Debug.WriteLine("File size must be greater than 0 bytes and upto 20MB");
                //throw new Exception("File size must be greater than 0 bytes and upto 20MB");
                return RedirectToPage("./Error", "File size must be greater than 0 bytes and upto 20MB");
                // return RedirectToPage("../Error");
            }

            // decide on container name
            String container;
            if (AnswerImage.Question == Question.Earth)
            {
                container = earthContainerName;
            } else if (AnswerImage.Question == Question.Computer)
            {
                container = computerContainerName;
            } else
            {
                Debug.WriteLine("Invalid Question value " + AnswerImage.Question);
                // throw new Exception("Invalid Question value " + AnswerImage.Question);
                return RedirectToPage("./Error", "Invalid Question value " + AnswerImage.Question);
            }

            // create container client to attach to a container
            BlobContainerClient containerClient;
            try
            {
                // attempt to create a new container and make public
                containerClient = await _blobServiceClient.CreateBlobContainerAsync(container);
                containerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
            }
            catch (RequestFailedException)
            {
                // container already exists
                containerClient = _blobServiceClient.GetBlobContainerClient(container);
            }

            try
            {
                string filePath = Path.GetRandomFileName();
                // create the blob to hold the data
                var blockBlob = containerClient.GetBlobClient(filePath);
                // if blob exists already throw an error
                if (await blockBlob.ExistsAsync())
                {
                    Debug.WriteLine("Blob with specified url already exists");
                    // throw new Exception("Blob with specified url already exists");
                    return RedirectToPage("./Error", "Blob with specified url already exists");
                }

                AnswerImage.FileName = filePath;
                AnswerImage.Url = blockBlob.Uri.AbsoluteUri;
                
                using (var memoryStream = new MemoryStream())
                {
                    // copy the file data into memory
                    await file.CopyToAsync(memoryStream);

                    // navigate back to the beginning of the memory stream
                    memoryStream.Position = 0;

                    // send the file to the cloud
                    await blockBlob.UploadAsync(memoryStream);
                    memoryStream.Close();
                }


            }
            catch (RequestFailedException)
            {
                // blob already exists
                Debug.WriteLine("Image blob already exists");
                //throw new Exception("Image blob already exists");
                return RedirectToPage("./Error", "Image blob already exists");
                //return RedirectToPage("../Error");
            }

            _context.AnswersImages.Add(AnswerImage);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
