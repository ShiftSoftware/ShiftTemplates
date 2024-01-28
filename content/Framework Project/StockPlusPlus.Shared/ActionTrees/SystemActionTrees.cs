
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace StockPlusPlus.Shared.ActionTrees;

[ActionTree("System", "System")]
public class SystemActionTrees
{
    public readonly static DecimalAction MaxUploadSizeInMegaBytes = new DecimalAction("Max Upload Size", null, 0, 100m);
    public readonly static ReadWriteDeleteAction UploadFiles = new ReadWriteDeleteAction("Upload Files");
}
