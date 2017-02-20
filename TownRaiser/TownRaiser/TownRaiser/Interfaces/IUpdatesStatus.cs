using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.CustomEvents;

namespace TownRaiser.Interfaces
{ 
    public interface IUpdatesStatus
    {
        event EventHandler<UpdateStatusEventArgs> UpdateStatus;
        float GetHealthRatio();
        string NameDisplay { get; }
    }
    
}
