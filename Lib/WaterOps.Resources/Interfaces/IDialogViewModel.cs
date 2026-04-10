using WaterOps.Resources.Enums;

namespace WaterOps.Resources.Interfaces;

public interface IDialogViewModel
{
    Task<DialogResult> Result { get; }
}
