using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionToolbarRuntime
    {
        public event EventHandler SelectClicked;
        public event EventHandler TrainClicked;
        public event EventHandler BuildClicked;


        partial void CustomInitialize()
        {
            this.SelectButtonInstance.Click += (notused) => this.SelectClicked(this, null);
            this.TrainButtonInstance.Click += (notused) => this.TrainClicked(this, null);
            this.BuildButtonInstance.Click += (notused) => this.BuildClicked(this, null);
        }
    }
}
