using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(InterfaceReference<>), true)]
public class InterfaceReferenceDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        container.Add(new Label("Cat"));
        // var asInterface = (IInterfaceReference)property.boxedValue
        container.Add(new PropertyField(property.FindPropertyRelative("value")));
        return container;
    }
}
