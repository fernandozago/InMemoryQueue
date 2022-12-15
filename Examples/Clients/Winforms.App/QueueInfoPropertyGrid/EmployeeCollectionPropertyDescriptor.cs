using System.ComponentModel;

namespace GrpcClient4.QueueInfoPropertyGrid;
public class EmployeeCollectionPropertyDescriptor : PropertyDescriptor
{
    private readonly ConsumerCollection _collection;
    private readonly Guid _refId;

    public EmployeeCollectionPropertyDescriptor(ConsumerCollection collection, Guid refId)
        : base("#" + refId.ToString(), null)
    {
        _collection = collection;
        _refId = refId;
    }

    public override AttributeCollection Attributes
    {
        get
        {
            return new AttributeCollection(null);
        }
    }

    public override bool CanResetValue(object component)
    {
        return false;
    }

    public override Type ComponentType
    {
        get
        {
            return this._collection.GetType();
        }
    }

    public override string DisplayName => 
        $"Consumer {_collection.IndexOf(this) + 1}";

    public override string Description => 
        $"Consumer {_collection[_refId].Name} connected";

    public override object GetValue(object? component) =>
        _collection[_refId];

    public override bool IsReadOnly =>
        true;

    public override string Name =>
        "#" + _refId.ToString();

    public override Type PropertyType =>
        _collection[_refId].GetType();

    public override void ResetValue(object component) 
    { 
    }

    public override bool ShouldSerializeValue(object component) =>
        true;

    public override void SetValue(object? component, object? value)
    {

    }
}

