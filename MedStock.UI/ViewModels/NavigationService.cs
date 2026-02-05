using System;
using Microsoft.Extensions.DependencyInjection;

namespace MedStock.UI.ViewModels
{
    public sealed class NavigationService : INavigationService
    {
        private readonly IServiceProvider _provider;

        public NavigationService(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public ViewModelBase Current { get; private set; } = null!;

        public event Action<ViewModelBase>? CurrentChanged;

        public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        {
            var vm = _provider.GetRequiredService<TViewModel>();
            Current = vm;
            CurrentChanged?.Invoke(vm);
        }
    }
}
