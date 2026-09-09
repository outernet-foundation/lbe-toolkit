using UnityEngine;

namespace Outernet.LBEToolkit
{
    public class ToggleGroupAttribute : PropertyAttribute
    {
        public string toggleProperty;
        public bool invert;
        public bool disable;
        public int enumValue;

        public ToggleGroupAttribute(string toggleProperty, bool invert = false, bool disable = false, int enumValue = 0)
            : base(applyToCollection: true)
        {
            this.toggleProperty = toggleProperty;
            this.invert = invert;
            this.disable = disable;
            this.enumValue = enumValue;
        }
    }
}