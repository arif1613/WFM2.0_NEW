﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    [Serializable()]
    public enum DeliveryMethod
    {
        NotSpecified,
        Stream,
        Download,
        Physical
    }
}