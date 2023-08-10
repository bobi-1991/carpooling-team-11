﻿using AutoMapper;
using Carpooling.BusinessLayer.Exceptions;
using Carpooling.BusinessLayer.Services.Contracts;
using Carpooling.Models;
using Carpooling.PaginationHelper;
using CarPooling.Data.Data;
using CarPooling.Data.Exceptions;
using CarPooling.Data.Models;
using CarPooling.Data.Models.Pagination;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace Carpooling.Controllers
{
    public class TravelsController : Controller
    {
        private readonly IUserService userService;
        private readonly ICarService carService;
        private readonly IFeedbackService feedbackService;
        private readonly ITravelService travelService;
        private readonly UserManager<User> userManager;
        private readonly IMapper mapper;
        private readonly CarPoolingDbContext dbContext;
        public TravelsController(IUserService userService, ICarService carService, IFeedbackService feedbackService,
            ITravelService travelService, UserManager<User> userManager, IMapper mapper, CarPoolingDbContext dbContext)
        {
            this.userService = userService;
            this.carService = carService;
            this.feedbackService = feedbackService;
            this.travelService = travelService;
            this.userManager = userManager;
            this.mapper = mapper;
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? pg, string searchQuery, string sortBy)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Challenge();
                }
                var loggedUser = await userManager.GetUserAsync(User);
                var userRoles = await userManager.GetRolesAsync(loggedUser);
                var getAllTravels = await travelService.GetAllTravelAsync();
                if (string.IsNullOrEmpty(sortBy))
                {
                    sortBy = "id"; 
                }
                getAllTravels = await travelService.FilterTravelsAndSortForMVCAsync(sortBy);
                if (!userRoles.Contains("Administrator"))
                {
                    getAllTravels = getAllTravels.Where(x=>x.IsCompleted==false && x.ArrivalTime > DateTime.Now).ToList();
                }
                var pageSize = 5;
                
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    getAllTravels = SearchTravels(getAllTravels.AsQueryable(), searchQuery);
                }
                if (pg < 1)
                {
                    pg = 1;
                }

                int recsCount = getAllTravels.Count();

                var pager = new Pager(recsCount, pg ?? 1, pageSize);

                int recSkip = (pager.CurrentPage - 1) * pageSize;
                var data = getAllTravels
                   .Skip(recSkip)
                   .Take(pager.PageSize)
                   .ToList();
                ViewBag.Pager = pager;
                return this.View(data);
            }
            catch (EmptyListException e)
            {
                this.Response.StatusCode = StatusCodes.Status404NotFound;
                this.ViewData["ErrorMessage"] = e.Message;

                return this.View("Error");
            }
        }
        private IQueryable<Travel> SearchTravels(IQueryable<Travel> travels, string searchQuery)
        {
            if (!string.IsNullOrEmpty(searchQuery))
            {
                travels = travels.Where(travel =>
                    travel.StartLocation.Details.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    travel.StartLocation.City.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    travel.EndLocation.Details.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    travel.EndLocation.City.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    travel.Driver.UserName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );
            }
            return travels;
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                this.ViewBag.id = id;
                var travel = await this.travelService.GetByIdAsync(id);

                return this.View(travel);
            }
            catch (EntityNotFoundException e)
            {
                this.Response.StatusCode = StatusCodes.Status404NotFound;
                this.ViewData["ErrorMessage"] = e.Message;

                return this.View("Error");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }
            var travelViewModel = new TravelViewModel();
            return this.View(travelViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(TravelViewModel travelViewModel)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }
            if (!this.ModelState.IsValid)
            {
                return this.View(travelViewModel);
            }
            try
            {
                var user = await userManager.Users.Include(c => c.Cars)
                    .SingleAsync(x => x.UserName.Equals(User.Identity.Name));
                var travel = mapper.Map<Travel>(travelViewModel);
                var createdTravel = await travelService.CreateTravelForMVCAsync(user, travel);
                return this.RedirectToAction("Details", "Travels", new { id = travel.Id });
            }
            catch (UnauthorizedOperationException ex)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                this.ViewData["ErrorMessage"] = ex.Message;
                return View(travelViewModel);
            }
            catch (EntityUnauthorizatedException ex)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                this.ViewData["ErrorMessage"] = ex.Message;
                return View(travelViewModel);
            }
            catch (EntityNotFoundException ex)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                this.ViewData["ErrorMessage"] = ex.Message;
                return View("Error");
            }
            catch(ArgumentException ex)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                this.ViewData["ErrorMessage"] = ex.Message;
                return View("Error");
            }
        }

    }
}
