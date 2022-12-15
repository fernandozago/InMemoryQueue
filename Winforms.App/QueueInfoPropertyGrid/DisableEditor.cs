using System.ComponentModel;
using System.Drawing.Design;

namespace GrpcClient4.QueueInfoPropertyGrid;
public class DisableEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
    {
        return UITypeEditorEditStyle.None;
    }

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        return "You can't edit this";
    }
}

