using System;
using UnityEngine;

[Serializable]
public struct InterfaceReference<T> : IInterfaceReference
{
    [SerializeReference]
    public T value;
    public Type interfaceType => typeof(T);

    object IInterfaceReference.value { get => value; set => this.value = (T)value; }
}

public interface IInterfaceReference
{
    object value { get; set; }
    Type interfaceType { get; }
}
