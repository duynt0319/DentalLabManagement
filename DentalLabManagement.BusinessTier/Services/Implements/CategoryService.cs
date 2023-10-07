﻿using DentalLabManagement.BusinessTier.Payload.Account;
using DentalLabManagement.BusinessTier.Payload.NewFolder;
using DentalLabManagement.BusinessTier.Services.Interfaces;
using DentalLabManagement.BusinessTier.Utils;
using DentalLabManagement.DataTier.Models;
using DentalLabManagement.DataTier.Paginate;
using DentalLabManagement.DataTier.Repository.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace DentalLabManagement.BusinessTier.Services.Implements
{
    public class CategoryService : BaseService<CategoryService>, ICategoryService
    {

        public CategoryService(IUnitOfWork<DentalLabManagementContext> unitOfWork, ILogger<CategoryService> logger) : base(unitOfWork, logger)
        {

        }

        public async Task<Category?> CreateCategory(CategoryRequest categoryRequest)
        {
            Category category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync
                (predicate: x => x.CategoryName.Equals(categoryRequest.CategoryName));
            if (category != null)
            {
                throw new HttpRequestException("Category is already exist");
            }
            Category newCategory = new Category()
            {
                CategoryName= categoryRequest.CategoryName
            };
            await _unitOfWork.GetRepository<Category>().InsertAsync(newCategory);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            if (isSuccessful)
            {
                return newCategory;
            }
            return null;
        }

        public async Task<IPaginate<GetCategoriesResponse>> GetCategories(string? searchCategoryName, int page, int size)
        {
            searchCategoryName = searchCategoryName?.Trim().ToLower();
            IPaginate<GetCategoriesResponse> categories = await _unitOfWork.GetRepository<Category>().GetPagingListAsync(
                selector: x => new GetCategoriesResponse(x.Id, x.CategoryName),
                predicate: string.IsNullOrEmpty(searchCategoryName) ? x => true : x => x.CategoryName.ToLower().Contains(searchCategoryName),
                page: page,
                size: size
                );
            return categories;
        }


    }
}
