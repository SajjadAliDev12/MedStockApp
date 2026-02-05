using System;

namespace MedStock.UI.ViewModels
{
    public interface INavigationService
    {
        ViewModelBase Current { get; }
        event Action<ViewModelBase> CurrentChanged;

        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    }
}
