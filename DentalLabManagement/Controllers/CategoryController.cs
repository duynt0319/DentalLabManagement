﻿using DentalLabManagement.API.Constants;
using DentalLabManagement.BusinessTier.Payload.NewFolder;
using DentalLabManagement.BusinessTier.Services.Interfaces;
using static DentalLabManagement.API.Constants.ApiEndPointConstant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DentalLabManagement.BusinessTier.Payload.Account;
using DentalLabManagement.BusinessTier.Services.Implements;
using DentalLabManagement.DataTier.Paginate;

namespace DentalLabManagement.API.Controllers
{
    [ApiController]
    public class CategoryController : BaseController<CategoryController>
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ILogger<CategoryController> logger, ICategoryService categoryService) : base(logger)
        {
            _categoryService = categoryService;
        }

        [HttpPost(ApiEndPointConstant.Category.CategoryEndpoint)]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(UnauthorizedObjectResult))]
        public async Task<IActionResult> CreateCategory(CategoryRequest categoryRequest)
        {
            var response = await _categoryService.CreateCategory(categoryRequest);
            if (response == null)
            {
                return BadRequest(NotFound());
            }
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Category.CategoriesEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetAccountsResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(UnauthorizedObjectResult))]
        public async Task<IActionResult> ViewAllCategories([FromQuery] string? name, [FromQuery] int page, [FromQuery] int size)
        {
            var categories = await _categoryService.GetCategories(name, page, size);
            return Ok(categories);
        }
    }
}
