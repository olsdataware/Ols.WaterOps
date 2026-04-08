namespace WaterOps.Resources.Interfaces;

public interface IViewModelFactory
{
    public IViewModel Create(Type viewModelType);
}
