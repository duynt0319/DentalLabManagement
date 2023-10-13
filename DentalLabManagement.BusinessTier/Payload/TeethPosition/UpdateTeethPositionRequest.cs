﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalLabManagement.BusinessTier.Payload.TeethPosition
{
    public class UpdateTeethPositionRequest
    {
        public int ToothArch { get; set; }
        public string PositionName { get; set; }
        public string Description { get; set; }

        public void TrimString()
        {
            PositionName = PositionName.Trim();
            Description = Description.Trim();
        }
    }
}
