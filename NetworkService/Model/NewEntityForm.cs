using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkService.Validation;

namespace NetworkService.Model
{
    public class NewEntityForm : ValidationBase
    {
        private ServerType selectedType;
        public ServerType SelectedType
        {
            get { return selectedType; }
            set
            {
                selectedType = value;
                OnPropertyChanged("SelectedType");
                Validate();
            }
        }

        protected override void ValidateSelf()
        {
            if (SelectedType == null)
                ValidationErrors["SelectedType"] = "Server type is required.";
        }
    }
}
