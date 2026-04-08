namespace WaterOps.Resources.Interfaces;

public interface IViewModel
{
    public string? Title { get; set; }
    public bool IsDirty { get; set; }

    public Task Initialize(object? parameter = null);
    public Task Save();
    public Task Print();
    public Task Delete();
}
