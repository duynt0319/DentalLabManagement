﻿using DentalLabManagement.BusinessTier.Constants;
using DentalLabManagement.BusinessTier.Payload.ProductStage;
using DentalLabManagement.BusinessTier.Payload.TeethPosition;
using DentalLabManagement.BusinessTier.Services.Interfaces;
using DentalLabManagement.DataTier.Models;
using DentalLabManagement.DataTier.Paginate;
using DentalLabManagement.DataTier.Repository.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalLabManagement.BusinessTier.Services.Implements
{
    public class TeethPositionService : BaseService<TeethPositionService>, ITeethPositionServices
    {
        public TeethPositionService(IUnitOfWork<DentalLabManagementContext> unitOfWork, ILogger<TeethPositionService> logger) : base(unitOfWork, logger)
        {

        }

        public async Task<TeethPositionResponse> CreateTeethPosition(TeethPositionRequest teethPositionRequest)
        {
            TeethPosition teethPosition = await _unitOfWork.GetRepository<TeethPosition>().SingleOrDefaultAsync
                (predicate: x => x.PositionName.Equals(teethPositionRequest.PositionName));
            if (teethPosition != null) throw new HttpRequestException(MessageConstant.TeethPosition.TeethPositionExisted);
            teethPosition = new TeethPosition()
            {
                ToothArch = teethPositionRequest.ToothArch,
                PositionName = teethPositionRequest.PositionName,
                Description = teethPositionRequest.Description,
            };

            await _unitOfWork.GetRepository<TeethPosition>().InsertAsync(teethPosition);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            if (!isSuccessful) throw new HttpRequestException(MessageConstant.TeethPosition.CreateTeethPositionFailed);
            return new TeethPositionResponse(teethPosition.Id, teethPosition.ToothArch, teethPosition.PositionName, teethPosition.Description);

        }       

        public async Task<IPaginate<TeethPositionResponse>> GetTeethPositions(int? toothArch, int page, int size)
        {
            IPaginate<TeethPositionResponse> response = await _unitOfWork.GetRepository<TeethPosition>().GetPagingListAsync(
                selector: x => new TeethPositionResponse(x.Id, x.ToothArch, x.PositionName, x.Description),
                predicate: string.IsNullOrEmpty(toothArch.ToString()) ? x => true : x => x.ToothArch.Equals(toothArch),
                page: page,
                size : size
                );
            return response;
        }

        public async Task<TeethPositionResponse> GetTeethPositionById(int id)
        {
            if (id < 1) throw new HttpRequestException(MessageConstant.TeethPosition.EmptyTeethPositionIdMessage);
            TeethPosition teethPosition = await _unitOfWork.GetRepository<TeethPosition>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(id));
            if (teethPosition == null) throw new HttpRequestException(MessageConstant.TeethPosition.IdNotFoundMessage);
            return new TeethPositionResponse(teethPosition.Id, teethPosition.ToothArch, teethPosition.PositionName, teethPosition.Description);
        }

        public async Task<TeethPositionResponse> UpdateTeethPosition(int id, UpdateTeethPositionRequest updateTeethPositionRequest)
        {
            if (id < 1) throw new HttpRequestException(MessageConstant.TeethPosition.EmptyTeethPositionIdMessage);
            TeethPosition updateTeethPosition = await _unitOfWork.GetRepository<TeethPosition>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(id));
            if (updateTeethPosition == null) throw new HttpRequestException(MessageConstant.TeethPosition.IdNotFoundMessage);
            updateTeethPositionRequest.TrimString();

            updateTeethPosition.ToothArch = (updateTeethPositionRequest.ToothArch < 1 || updateTeethPositionRequest.ToothArch > 4)
                ? throw new HttpRequestException(MessageConstant.TeethPosition.ToothArchError) : updateTeethPositionRequest.ToothArch;
            updateTeethPosition.PositionName = string.IsNullOrEmpty(updateTeethPositionRequest.PositionName)
                ? updateTeethPosition.PositionName : updateTeethPositionRequest.PositionName;
            updateTeethPosition.Description = string.IsNullOrEmpty(updateTeethPositionRequest.Description)
                ? updateTeethPosition.Description : updateTeethPositionRequest.Description;

            _unitOfWork.GetRepository<TeethPosition>().UpdateAsync(updateTeethPosition);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            if (!isSuccessful) throw new HttpRequestException(MessageConstant.TeethPosition.UpdateTeethPositionFailedMessage);
            return new TeethPositionResponse
                (updateTeethPosition.Id, updateTeethPosition.ToothArch, updateTeethPosition.PositionName, updateTeethPosition.Description);
        }
    }
}
