using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Archetype.Models;

namespace AST.ContentConveyor7.DataTypeConverters
{
    public static class ArchetypeDataTypeConverterExtensions
    {
        public static IEnumerable<ArchetypePropertyModel> GetConveyorCustomDtcAttributeProperties(this ArchetypeFieldsetModel fieldset)
        {
            return fieldset.Properties.Where(p => p.Alias.StartsWith("conveyor_dtc_"));
        }

        public static IEnumerable<ArchetypePropertyModel> GetArchetypeProperties(this ArchetypeFieldsetModel fieldset)
        {
            return fieldset.Properties.Where(p => !p.Alias.StartsWith("conveyor_dtc_"));
        }

        public static IEnumerable<ArchetypePropertyModel> RemoveConveyorCustomDtcAttributeProperties(this ArchetypeFieldsetModel fieldset)
        {
            var props = fieldset.Properties.ToList();
            props.RemoveAll(p => fieldset.GetConveyorCustomDtcAttributeProperties().Any(custom => custom.Alias == p.Alias));
            return props;
        }

        public static void AddCustomAttributesToArchetypeProperties(this ArchetypeFieldsetModel fieldset, Dictionary<string, string> conveyorDtcCustomAttributes)
        {
            var fsProps = fieldset.Properties.ToList();
            foreach (var propToAdd in conveyorDtcCustomAttributes)
            {
                fsProps.Add(new ArchetypePropertyModel() {Alias = propToAdd.Key, Value = propToAdd.Value});
            }
            fieldset.Properties = fsProps;
        }

        public static IEnumerable<XAttribute> GetCustomAttributesAddedByDtc(this XElement propertyTag)
        {
            var systemAttributes = new string[] {"propertyEditorAlias", "dataTypeName"};
            return propertyTag.Attributes().Where(n => !systemAttributes.Contains(n.Name.ToString()));
        }

    }
}
