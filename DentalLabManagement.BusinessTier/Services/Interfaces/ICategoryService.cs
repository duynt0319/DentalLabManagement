﻿using DentalLabManagement.BusinessTier.Payload.Account;
using DentalLabManagement.BusinessTier.Payload.Category;
using DentalLabManagement.BusinessTier.Payload.NewFolder;
using DentalLabManagement.DataTier.Models;
using DentalLabManagement.DataTier.Paginate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalLabManagement.BusinessTier.Services.Interfaces
{
    public interface ICategoryService
    {
        public Task<CategoryResponse> CreateCategory(CategoryRequest categoryRequest);

        public Task<IPaginate<GetCategoriesResponse>> GetCategories(string? searchCategoryName, int page, int size);

        public Task<GetCategoriesResponse> GetCategoryById(int id);
        public Task<CategoryResponse> UpdateCategoryInformation(int categoryId, UpdateCategoryRequest updateCategoryRequest);
        
    }
}
