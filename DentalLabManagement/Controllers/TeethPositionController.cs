﻿using DentalLabManagement.BusinessTier.Constants;
using DentalLabManagement.BusinessTier.Payload.Product;
using DentalLabManagement.BusinessTier.Payload.ProductStage;
using DentalLabManagement.BusinessTier.Payload.TeethPosition;
using DentalLabManagement.BusinessTier.Services.Implements;
using DentalLabManagement.BusinessTier.Services.Interfaces;
using DentalLabManagement.DataTier.Paginate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalLabManagement.API.Controllers
{
    [ApiController]
    public class TeethPositionController : BaseController<TeethPositionController>
    {
        private readonly ITeethPositionServices _teethPositionServices;

        public TeethPositionController(ILogger<TeethPositionController> logger, ITeethPositionServices teethPositionServices) : base(logger)
        {
            _teethPositionServices = teethPositionServices;
        }

        [HttpPost(ApiEndPointConstant.TeethPosition.TeethPositonsEndPoint)]
        [ProducesResponseType(typeof(TeethPositionResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(UnauthorizedObjectResult))]
        public async Task<IActionResult> CreateTeethPosition(TeethPositionRequest teethPositionRequest)
        {
            var response = await _teethPositionServices.CreateTeethPosition(teethPositionRequest);
            if (response == null)
            {
                return BadRequest(NotFound());
            }
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.TeethPosition.TeethPositonsEndPoint)]
        [ProducesResponseType(typeof(IPaginate<TeethPositionResponse>), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(UnauthorizedObjectResult))]
        public async Task<IActionResult> GetTeethPositons([FromQuery] int? toothArch, [FromQuery] int page, [FromQuery] int size)
        {
            var response = await _teethPositionServices.GetTeethPositions(toothArch, page, size);
            return Ok(response);
        }
    }
}
