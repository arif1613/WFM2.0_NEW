﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public enum RequestResultState
    {
        Successful,
        Failed,
        Exception,
        Revoke,
        Retry,
        Ignored
    }
}
