using System.Collections.Concurrent;
using System.ComponentModel;

namespace GrpcClient4.QueueInfoPropertyGrid;

[TypeConverter(typeof(ExpandableObjectConverter))]
public class ConsumerCollection : ICustomTypeDescriptor
{
    private readonly ConcurrentDictionary<string, (QueueInfoReplyConsumer consumerInfo, EmployeeCollectionPropertyDescriptor descriptor)> List = new();
    private readonly PropertyDescriptorCollection _propertyDescriptorCollection = new(null);

    public QueueInfoReplyConsumer this[string refId]
    {
        get => List[refId].consumerInfo;
    }

    public int IndexOf(EmployeeCollectionPropertyDescriptor value)
    {
        return _propertyDescriptorCollection.IndexOf(value);
    }

    public void AddOrUpdate(QueueInfoReplyConsumer emp)
    {
        bool added = true;
        List.AddOrUpdate(emp.Id, (emp, new EmployeeCollectionPropertyDescriptor(this, emp.Id)), (a, b) =>
        {
            added = false;
            b.consumerInfo = emp;
            return b;
        });

        if (added)
        {
            _propertyDescriptorCollection.Add(List[emp.Id].descriptor);
        }
    }

    public void RemoveRemovedConsumers(IEnumerable<string> existingConsumers)
    {
        foreach (var item in List.Select(x => x.Key).Except(existingConsumers))
        {
            List.Remove(item, out var value);
            _propertyDescriptorCollection.Remove(value.descriptor);
        }
    }

    public override string ToString()
    {
        return List.Count.ToString("N0");
    }

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        return _propertyDescriptorCollection;
    }

    public PropertyDescriptorCollection GetProperties()
    {
        return _propertyDescriptorCollection;
    }

    public string? GetClassName()
    {
        return TypeDescriptor.GetClassName(this, true);
    }

    public AttributeCollection GetAttributes()
    {
        return TypeDescriptor.GetAttributes(this, true);
    }

    public string? GetComponentName()
    {
        return TypeDescriptor.GetComponentName(this, true);
    }

    public TypeConverter GetConverter()
    {
        return TypeDescriptor.GetConverter(this, true);
    }

    public EventDescriptor? GetDefaultEvent()
    {
        return TypeDescriptor.GetDefaultEvent(this, true);
    }

    public PropertyDescriptor? GetDefaultProperty()
    {
        return TypeDescriptor.GetDefaultProperty(this, true);
    }

    public object? GetEditor(Type editorBaseType)
    {
        return TypeDescriptor.GetEditor(this, editorBaseType, true);
    }

    public EventDescriptorCollection GetEvents(Attribute[]? attributes)
    {
        return TypeDescriptor.GetEvents(this, attributes, true);
    }

    public EventDescriptorCollection GetEvents()
    {
        return TypeDescriptor.GetEvents(this, true);
    }

    public object GetPropertyOwner(PropertyDescriptor? pd)
    {
        return this;
    }
}

