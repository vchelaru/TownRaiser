﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownRaiser.Interfaces
{ 
    public interface IUpdatesStatus
    {
        event EventHandler<UpdateStatusEventArgs> UpdateStatus;
        float GetHealthRatio();
    }
    public class UpdateStatusEventArgs: EventArgs
    {
        public bool WasEntityDestroyed { get; set; }
    }
}