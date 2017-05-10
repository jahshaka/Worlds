﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jahshaka.Core.Data;
using Jahshaka.Web.ViewModels.Asset;
using Jahshaka.Web.ViewModels.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace Jahshaka.Web.Controllers
{
    [Authorize]
    public class AssetController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _appDbContext;
        private IHostingEnvironment _environment;

        public AssetController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext appDbContext,
            IHostingEnvironment environment
        )
        {
            _userManager = userManager;
            _appDbContext = appDbContext;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var assets = _appDbContext.Assets
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var viewModel = new ListAssetViewModel()
            {
                Assets = assets.ToViewModel()
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAssetViewModel model)
        {
            if(ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);

                    var filePath = Path.Combine(_environment.WebRootPath, "uploads");
                    string[] file = model.Upload.FileName.Split('.');
                    string ext = file[file.Length - 1];

                    /*var supportedTypes = new[] { "jpg","png", "gif", "jpeg", "bmp", "svg" };

                    if (!supportedTypes.Contains(ext))
                    {
                        ModelState.AddModelError(string.Empty, "Unsupported file extension.");
                        return View(model);
                    }*/

                    var asset = new Asset()
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Name = model.Name,
                        Type = model.Type,
                        IsPublic = model.IsPublic,
                        CreatedAt = DateTime.UtcNow
                    };

                    string fileName = asset.Id.ToString() + "." + ext;

                    using (var stream = new FileStream(Path.Combine(filePath, fileName), FileMode.Create))
                    {
                        await model.Upload.CopyToAsync(stream);
                    }

                    asset.Url = fileName;

                    _appDbContext.Add(asset);

                    await _appDbContext.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex.Message);
                    ModelState.AddModelError(string.Empty, "Something when wrong! Please try again.");
                }

            }

            return View(model);
        }
    }
}