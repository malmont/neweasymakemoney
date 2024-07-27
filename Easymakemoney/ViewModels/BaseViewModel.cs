using System;

namespace Easymakemoney.ViewModels
{
    public partial class BaseViewModel : ObservableObject 
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private String? _title;

        protected virtual void OnAppearing()
        {

             loadViewModel();
        }

        public virtual void loadViewModel()
        {

        }



    }




}

